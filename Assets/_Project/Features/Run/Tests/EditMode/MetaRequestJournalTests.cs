using System;
using System.IO;
using FateDice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    [Category("RecoveryRequests")] public sealed class MetaRequestJournalTests
    {
        string directory;
        MetaProgressionConfig config;
        LocalPlayerDataStore disk;
        LocalMetaProgressionService service;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "FateDiceRequestJournal", Guid.NewGuid().ToString("N"));
            config = ScriptableObject.CreateInstance<MetaProgressionConfig>(); config.startingCurrency = 100;
            disk = new LocalPlayerDataStore(Path.Combine(directory, "player.json"), "request-player");
            service = new LocalMetaProgressionService(disk, PrototypeAuthoring.CreateDefaults, config, new FixedSeedSource(33));
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceRequestJournal")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        MetaPendingRequest Start(string id = "start") => MetaPendingRequest.Create(disk.ProfileId, new StartRunRequest { requestId = id });
        [Test] public void ReadAndUnknownQueryNeverCreateProfileOrPendingFiles()
        {
            Assert.That(disk.RequestJournal.Read(), Is.Null);
            Assert.That(service.QueryRequestAsync(Start()).GetAwaiter().GetResult().outcome, Is.EqualTo(MetaRequestOutcome.Unknown));
            Assert.That(Directory.Exists(directory), Is.False);
        }
        [Test] public void JournalAndRequestCopiesPreserveExactOriginalInput()
        {
            var input = new LoadoutRequest { requestId = "loadout", expectedRevision = 7, expectedRecoveryGeneration = Guid.NewGuid().ToString("N"), loadout = service.Profile.loadout.Copy() };
            var pending = MetaPendingRequest.Create(disk.ProfileId, input); string exact = MetaJson.Encode(pending);
            disk.RequestJournal.Begin(pending);
            input.requestId = "mutated"; input.expectedRevision = 99; input.loadout.dice[0] = "mutated";
            var read = disk.RequestJournal.Read(); read.payload = "mutated";
            Assert.That(MetaJson.Encode(disk.RequestJournal.Read()), Is.EqualTo(exact));
            var copy = (LoadoutRequest)pending.Request(); copy.loadout.dice[0] = "mutated again";
            Assert.That(((LoadoutRequest)pending.Request()).expectedRevision, Is.EqualTo(7));
            Assert.That(((LoadoutRequest)pending.Request()).loadout.dice[0], Is.Not.EqualTo(copy.loadout.dice[0]));
            Assert.That(disk.Exists, Is.False);
        }
        [Test] public void OldClearAndDifferentSubmissionCannotReplaceANewerRequest()
        {
            var first = Start(); disk.RequestJournal.Begin(first);
            var before = File.ReadAllBytes(disk.Path + ".pending"); disk.RequestJournal.Begin(first.Copy());
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path + ".pending"));
            Assert.Throws<MetaCommandException>(() => disk.RequestJournal.Begin(Start("other")));
            disk.RequestJournal.Clear(first); var next = Start("next"); disk.RequestJournal.Begin(next);
            before = File.ReadAllBytes(disk.Path + ".pending");
            Assert.Throws<MetaCommandException>(() => disk.RequestJournal.Clear(first));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path + ".pending")); Assert.That(disk.Exists, Is.False);
        }
        [TestCase("json")][TestCase("checksum")][TestCase("version")][TestCase("foreign")]
        public void InvalidPendingRecordIsPreservedAndBlocksReplacement(string corruption)
        {
            disk.RequestJournal.Begin(Start()); string path = disk.Path + ".pending";
            if (corruption == "json") File.WriteAllText(path, "{");
            if (corruption == "checksum" || corruption == "version")
            {
                var e = MetaJson.Decode<LocalSaveEnvelope>(File.ReadAllText(path));
                if (corruption == "checksum") e.checksum = "wrong"; else e.schema = 99;
                File.WriteAllText(path, MetaJson.Encode(e));
            }
            var journal = corruption == "foreign" ? new LocalMetaRequestJournal(disk.Path, "other") : disk.RequestJournal;
            var before = File.ReadAllBytes(path);
            Assert.Throws<InvalidDataException>(() => journal.Read());
            Assert.Throws<InvalidDataException>(() => journal.Begin(MetaPendingRequest.Create(journal.ProfileId, new StartRunRequest { requestId = "new" })));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path)); Assert.That(disk.Exists, Is.False);
        }
        [Test] public void ProfileWriteLockFailureDoesNotDispatchOrDropPendingIntent()
        {
            Directory.CreateDirectory(directory);
            using (var held = new FileStream(disk.Path + ".write-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var coordinator = new MetaRequestCoordinator(service);
                Assert.Catch<IOException>(() => coordinator.StartAsync(new StartRunRequest { requestId = "blocked" }));
                Assert.That(coordinator.HasPending, Is.False); Assert.That(disk.Exists, Is.False);
            }
        }
        [Test] public void RestoreDoesNotRebasePendingRequestAndQueryFencesItsRetry()
        {
            var run = service.StartRunAsync(new StartRunRequest { requestId = "fixture" }).GetAwaiter().GetResult();
            var p = service.Profile;
            var pending = MetaPendingRequest.Create(disk.ProfileId, new GrowthRequest { requestId = "old-grow",
                expectedRevision = p.revision, expectedRecoveryGeneration = p.recoveryGeneration, characterId = p.loadout.characterId });
            disk.RequestJournal.Begin(pending); var journal = File.ReadAllBytes(disk.Path + ".pending");
            var session = new RunSession(run, service.RunStore); session.RecordElapsed(1); session.SaveCheckpoint();
            var backup = disk.InspectBackup(); File.WriteAllText(disk.Path, "{corrupt"); var restored = disk.RestoreBackup(backup);
            var coordinator = new MetaRequestCoordinator(service); coordinator.CheckAsync().GetAwaiter().GetResult();
            Assert.That(coordinator.Resolution.outcome, Is.EqualTo(MetaRequestOutcome.SupersededByRecovery));
            CollectionAssert.AreEqual(journal, File.ReadAllBytes(disk.Path + ".pending"));
            Assert.Throws<MetaCommandException>(() => coordinator.RetryAsync());
            Assert.That(Assert.Throws<MetaCommandException>(() => service.GrowAsync((GrowthRequest)pending.Request()).GetAwaiter().GetResult()).Kind, Is.EqualTo(MetaFailureKind.Stale));
            Assert.That(service.Profile.growthCurrency, Is.EqualTo(restored.profile.growthCurrency));
            Assert.That(service.Profile.characters[0].rank, Is.Zero);
            coordinator.Acknowledge(pending); Assert.That(disk.RequestJournal.Read(), Is.Null);
        }
        [Test] public void RestoredReceiptCanResolveTheOriginalRequestWithoutDispatch()
        {
            var pending = Start(); disk.RequestJournal.Begin(pending); pending.DispatchAsync(service).GetAwaiter().GetResult();
            var session = new RunSession(service.RunStore.Load(), service.RunStore); session.RecordElapsed(1); session.SaveCheckpoint();
            var backup = disk.InspectBackup(); File.WriteAllText(disk.Path, "{corrupt"); var restored = disk.RestoreBackup(backup);
            var before = File.ReadAllBytes(disk.Path);
            var coordinator = new MetaRequestCoordinator(service); coordinator.CheckAsync().GetAwaiter().GetResult();
            Assert.That(coordinator.Resolution.outcome, Is.EqualTo(MetaRequestOutcome.Committed));
            Assert.That(coordinator.Resolution.snapshot.run.recoveryGeneration, Is.EqualTo(restored.profile.recoveryGeneration));
            Assert.That(coordinator.Resolution.runId, Is.EqualTo(restored.run.runId));
            coordinator.Acknowledge(pending); CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path));
        }
    }
}
