using System;
using System.IO;
using FateDice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    [Category("RecoveryStability")]
    public sealed class RecoveryStabilityTests
    {
        string directory;
        MetaProgressionConfig config;
        LocalPlayerDataStore disk;
        LocalMetaProgressionService service;
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "FateDiceRecoveryTests", Guid.NewGuid().ToString("N"));
            config = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            disk = new LocalPlayerDataStore(Path.Combine(directory, "player.json"), "recovery-player");
            service = new LocalMetaProgressionService(disk, PrototypeAuthoring.CreateDefaults, config, new FixedSeedSource(33));
            service.StartRunAsync(new StartRunRequest { requestId = "start" }).GetAwaiter().GetResult();
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
            var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceRecoveryTests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        void ReplacePayload(PlayerSaveDocument doc, int envelopeSchema = 1)
        {
            string payload = MetaJson.Encode(doc);
            File.WriteAllText(disk.Path, MetaJson.Encode(new LocalSaveEnvelope { schema = envelopeSchema,
                payload = payload, checksum = MetaJson.Hash(payload) }));
        }
        [TestCase("envelope", MetaFailureKind.UnsupportedSchema)]
        [TestCase("document", MetaFailureKind.UnsupportedSchema)]
        [TestCase("profile", MetaFailureKind.UnsupportedSchema)]
        [TestCase("foreign", MetaFailureKind.ForeignProfile)]
        [TestCase("authority", MetaFailureKind.Corrupt)]
        [TestCase("checksum", MetaFailureKind.Corrupt)]
        [TestCase("json", MetaFailureKind.Corrupt)]
        [TestCase("receipt", MetaFailureKind.Corrupt)]
        public void FileErrorsHaveDistinctCodesWithoutChangingTheirBytes(string failure, MetaFailureKind expected)
        {
            var doc = disk.Read();
            switch (failure)
            {
                case "document": doc.schema = 99; break;
                case "profile": doc.profile.schema = 99; break;
                case "foreign": doc.profile.profileId = "other"; break;
                case "authority": doc.profile.authority = "Production"; break;
                case "receipt": doc.operations.Add(doc.operations[0].Copy()); break;
            }
            ReplacePayload(doc, failure == "envelope" ? 99 : 1);
            if (failure == "json") File.WriteAllText(disk.Path, "{");
            if (failure == "checksum") File.WriteAllText(disk.Path, MetaJson.Encode(new LocalSaveEnvelope { payload = "{}", checksum = "wrong" }));
            var bytes = File.ReadAllBytes(disk.Path);
            var error = Assert.Throws<InvalidDataException>(() => service.ReadRecoverySnapshot());
            Assert.That(MetaFailure.Classify(error), Is.EqualTo(expected));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [Test] public void CommandRejectionIsNotAStaleConflict()
        {
            var error = Assert.Throws<MetaCommandException>(() => service.GrowAsync(new GrowthRequest {
                requestId = "grow", expectedRevision = service.Profile.revision, characterId = "missing" }).GetAwaiter().GetResult());
            Assert.That(MetaFailure.Classify(error), Is.EqualTo(MetaFailureKind.Rejected));
            Assert.That(MetaFailure.Classify(new IOException("an arbitrary translated message")), Is.EqualTo(MetaFailureKind.TemporaryIO));
        }
        [Test] public void ProfileAndDocumentConflictsAreTypedAndLeaveTheFileAlone()
        {
            var bytes = File.ReadAllBytes(disk.Path); var candidate = disk.Read();
            var docError = Assert.Throws<MetaCommandException>(() => disk.Write(candidate.revision - 1, candidate));
            var profileError = Assert.Throws<MetaCommandException>(() => service.StartRunAsync(new StartRunRequest {
                requestId = "stale", expectedRevision = 0 }).GetAwaiter().GetResult());
            Assert.That(MetaFailure.Classify(docError), Is.EqualTo(MetaFailureKind.Stale));
            Assert.That(MetaFailure.Classify(profileError), Is.EqualTo(MetaFailureKind.Stale));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [Test] public void MissingRecoveryFileDoesNotCreateANewProfile()
        {
            File.Move(disk.Path, Path.Combine(directory, "preserved.json"));
            var error = Assert.Throws<FileNotFoundException>(() => service.ReadRecoverySnapshot());
            Assert.That(MetaFailure.Classify(error), Is.EqualTo(MetaFailureKind.TemporaryIO));
            Assert.That(disk.Exists, Is.False);
        }
        [Test] public void RecoverySnapshotIsAnIndependentCopyAndDoesNotWrite()
        {
            var bytes = File.ReadAllBytes(disk.Path); var snapshot = service.ReadRecoverySnapshot();
            snapshot.profile.growthCurrency++; snapshot.operations.Clear(); snapshot.run.hp--;
            var second = service.ReadRecoverySnapshot();
            Assert.That(second.profile.growthCurrency, Is.EqualTo(0)); Assert.That(second.operations.Count, Is.EqualTo(1));
            Assert.That(second.run.hp, Is.EqualTo(snapshot.run.hp + 1));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
        }
        [Test] public void ACheckpointKeepsOneValidatedPreviousDocumentForRecovery()
        {
            var previous = File.ReadAllBytes(disk.Path);
            var run = new RunSession(service.RunStore.Load(), service.RunStore);
            run.RecordElapsed(3); Assert.That(run.SaveCheckpoint(), Is.True);
            Assert.That(File.Exists(disk.Path + ".backup"), Is.True, "A validated last-good document must survive the next write.");
            CollectionAssert.AreEqual(previous, File.ReadAllBytes(disk.Path + ".backup"));
        }
        ProfileBackupPreview MakeBackup()
        {
            var run = new RunSession(service.RunStore.Load(), service.RunStore);
            run.RecordElapsed(3); Assert.That(run.SaveCheckpoint(), Is.True);
            return disk.InspectBackup();
        }
        void CorruptPrimary() => File.WriteAllText(disk.Path, "{corrupt profile");
        [Test] public void BackupInspectionDoesNotWriteAndHealthyPrimaryCannotBeRolledBack()
        {
            Assert.That(disk.InspectBackup(), Is.Null);
            var preview = MakeBackup(); var before = File.ReadAllBytes(disk.Path); var backup = File.ReadAllBytes(disk.BackupPath);
            Assert.That(preview, Is.Not.Null); Assert.That(preview.SavedAtUtc, Is.LessThanOrEqualTo(DateTime.UtcNow));
            Assert.Throws<MetaCommandException>(() => disk.RestoreBackup(preview));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            Assert.That(Directory.Exists(disk.RecoveryArchiveDirectory), Is.False);
        }
        [Test] public void RestorePreservesWholeDocumentAndFencesOldSessionRequestAndABACandidate()
        {
            var initial = disk.Read(); var staleSession = new RunSession(initial.run, service.RunStore);
            var oldRequest = new LoadoutRequest { requestId = "late-loadout", expectedRevision = initial.profile.revision,
                loadout = initial.profile.loadout.Copy() };
            var preview = MakeBackup(); var backup = File.ReadAllBytes(disk.BackupPath);
            CorruptPrimary(); var damaged = File.ReadAllBytes(disk.Path);
            var restored = disk.RestoreBackup(preview);
            Assert.That(restored.revision, Is.EqualTo(initial.revision), "Exercise a document revision ABA rollback.");
            Assert.That(restored.profile.revision, Is.EqualTo(oldRequest.expectedRevision));
            Assert.That(restored.profile.recoveryGeneration, Is.Not.EqualTo(initial.profile.recoveryGeneration));
            Assert.That(restored.run.recoveryGeneration, Is.EqualTo(restored.profile.recoveryGeneration));
            var comparable = restored.Copy(); comparable.profile.recoveryGeneration = initial.profile.recoveryGeneration;
            comparable.run.recoveryGeneration = initial.run.recoveryGeneration;
            Assert.That(MetaJson.Encode(comparable), Is.EqualTo(MetaJson.Encode(initial)), "No mixing receipts, run, loadout, growth or start policy from other points.");
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            var archives = Directory.GetFiles(disk.RecoveryArchiveDirectory); Assert.That(archives.Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(damaged, File.ReadAllBytes(archives[0]));
            var after = File.ReadAllBytes(disk.Path);
            staleSession.RecordElapsed(20);
            Assert.That(Assert.Throws<MetaCommandException>(() => staleSession.SaveCheckpoint()).Kind, Is.EqualTo(MetaFailureKind.Stale));
            Assert.That(Assert.Throws<MetaCommandException>(() => service.SaveLoadoutAsync(oldRequest).GetAwaiter().GetResult()).Kind, Is.EqualTo(MetaFailureKind.Stale));
            Assert.That(Assert.Throws<MetaCommandException>(() => disk.Write(initial.revision, initial)).Kind, Is.EqualTo(MetaFailureKind.Stale));
            CollectionAssert.AreEqual(after, File.ReadAllBytes(disk.Path));
            var currentSession = new RunSession(restored.run, service.RunStore); currentSession.RecordElapsed(4);
            Assert.That(currentSession.SaveCheckpoint(), Is.True);
            var p = service.Profile;
            service.SaveLoadoutAsync(new LoadoutRequest { requestId = "new-generation", expectedRevision = p.revision,
                expectedRecoveryGeneration = p.recoveryGeneration, loadout = p.loadout.Copy() }).GetAwaiter().GetResult();
            Assert.That(disk.Read().run.recoveryGeneration, Is.EqualTo(restored.profile.recoveryGeneration));
        }
        [TestCase("json", MetaFailureKind.Corrupt)]
        [TestCase("schema", MetaFailureKind.UnsupportedSchema)]
        [TestCase("foreign", MetaFailureKind.ForeignProfile)]
        public void InvalidBackupCannotTouchTheDamagedOriginal(string corruption, MetaFailureKind kind)
        {
            var preview = MakeBackup(); var backupDoc = new LocalPlayerDataStore(disk.BackupPath, disk.ProfileId).Read();
            if (corruption == "schema") backupDoc.schema = 22;
            if (corruption == "foreign") backupDoc.profile.profileId = "other-player";
            string payload = MetaJson.Encode(backupDoc);
            File.WriteAllText(disk.BackupPath, corruption == "json" ? "{" : MetaJson.Encode(new LocalSaveEnvelope {
                payload = payload, checksum = MetaJson.Hash(payload) }));
            CorruptPrimary(); var before = File.ReadAllBytes(disk.Path); var backup = File.ReadAllBytes(disk.BackupPath);
            Assert.That(MetaFailure.Classify(Assert.Throws<InvalidDataException>(() => disk.InspectBackup())), Is.EqualTo(kind));
            Assert.Throws<InvalidDataException>(() => disk.RestoreBackup(preview));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            Assert.That(Directory.Exists(disk.RecoveryArchiveDirectory), Is.False);
        }
        [Test] public void ChangedBackupMustBeConfirmedAgain()
        {
            var preview = MakeBackup();
            File.Copy(disk.Path, disk.BackupPath, true);
            CorruptPrimary(); var before = File.ReadAllBytes(disk.Path);
            Assert.That(Assert.Throws<MetaCommandException>(() => disk.RestoreBackup(preview)).Kind, Is.EqualTo(MetaFailureKind.Stale));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(disk.Path));
            Assert.That(Directory.Exists(disk.RecoveryArchiveDirectory), Is.False);
        }
        [Test] public void DamagedCurrentCannotReplaceTheLastGoodBackup()
        {
            var snapshot = service.RunStore.Load(); MakeBackup(); var backup = File.ReadAllBytes(disk.BackupPath);
            CorruptPrimary(); var primary = File.ReadAllBytes(disk.Path);
            Assert.Throws<InvalidDataException>(() => service.RunStore.Save(snapshot));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(disk.Path));
        }
        [Test] public void BackupWriteFailureKeepsPrimaryAndLastGoodBytes()
        {
            MakeBackup(); var primary = File.ReadAllBytes(disk.Path); var backup = File.ReadAllBytes(disk.BackupPath);
            var run = new RunSession(service.RunStore.Load(), service.RunStore); run.RecordElapsed(7);
            using (var held = new FileStream(disk.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.Catch<IOException>(() => run.SaveCheckpoint());
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
        }
        [Test] public void DamagedOriginalPreservationFailureAbortsRestore()
        {
            var preview = MakeBackup(); var backup = File.ReadAllBytes(disk.BackupPath);
            CorruptPrimary(); var primary = File.ReadAllBytes(disk.Path);
            File.WriteAllText(disk.RecoveryArchiveDirectory, "prevents creation of the archive directory");
            Assert.Catch<IOException>(() => disk.RestoreBackup(preview));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
        }
        [Test] public void PrimaryReplaceFailureStillPreservesDamagedOriginalAndBackup()
        {
            var preview = MakeBackup(); var backup = File.ReadAllBytes(disk.BackupPath);
            CorruptPrimary(); var primary = File.ReadAllBytes(disk.Path);
            using (var held = new FileStream(disk.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.Catch<IOException>(() => disk.RestoreBackup(preview));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(disk.Path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(disk.BackupPath));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(Directory.GetFiles(disk.RecoveryArchiveDirectory)[0]));
        }
    }
}
