using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionV4StoreTests
    {
        string directory;
        string Checkpoint => Path.Combine(directory, "checkpoint.json");
        [SetUp] public void Setup() => directory = Path.Combine(Path.GetTempPath(), "DiceWarriorV4Tests", Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            var allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DiceWarriorV4Tests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void NewApplicationResumesLegacyAndOnlyExplicitRestartInstallsV4WithRestorableOriginal(int version)
        {
            var old = version == 3 ? CompetitionGrowthTests.Definition() : CompetitionRulesTests.Definition();
            if (version < 3) old.rulesVersion = version == 1 ? CompetitionContent.LegacyVersion : CompetitionContent.RulesVersion;
            var store = new CompetitionStore(Checkpoint); var first = new CompetitionSession(store, old);
            first.Start(new Profile(), 123, "legacy"); first.Execute(first.Prepare("travel"));
            string raw = File.ReadAllText(Checkpoint); string before = LoopJson.Encode(first.Snapshot);
            var updated = new CompetitionSession(store, CompetitionV4Tests.Definition());
            Assert.That(LoopJson.Encode(updated.Snapshot), Is.EqualTo(before)); Assert.That(File.ReadAllText(Checkpoint), Is.EqualTo(raw));
            Assert.Throws<InvalidOperationException>(() => updated.Start(new Profile(), 456, "unconfirmed"));
            updated.Restart(new Profile(), 456, "v4", "scholar");
            Assert.That(updated.Snapshot.state.definition.rulesVersion, Is.EqualTo(CompetitionV4Content.Version));
            Assert.That(Directory.GetFiles(directory, "*.restarted-*").Select(File.ReadAllText), Does.Contain(raw));
            Assert.That(LoopJson.Encode(store.InspectBackup()), Is.EqualTo(before));
            updated.RestoreBackup();
            Assert.That(updated.Snapshot.state.definition.rulesVersion, Is.EqualTo(old.rulesVersion));
            Assert.That(LoopJson.Encode(updated.Snapshot.state), Is.EqualTo(LoopJson.Encode(first.Snapshot.state)));
        }

        [Test] public void StoredV4FullGameResumesAndRemainsWellBelowEnvelopeLimit()
        {
            var session = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV4Tests.Definition());
            session.Start(new Profile(), 123, "stored", "vanguard"); long max = 0;
            for (int i = 0; !session.Snapshot.state.finished && i < 300; ++i)
            {
                var c = CompetitionRulesTests.NextCommand(session.Snapshot.state); session.Execute(session.Prepare(c.kind, c.arg));
                max = Math.Max(max, new FileInfo(Checkpoint).Length);
                if (i == 10)
                {
                    string before = LoopJson.Encode(session.Snapshot);
                    session = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV4Tests.Definition());
                    Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
                }
            }
            Assert.That(session.Snapshot.state.finished, Is.True); Assert.That(max, Is.LessThan(4 * 1024 * 1024));
            TestContext.WriteLine("Largest actual UTF-8 envelope: " + max + " bytes; commands: " + session.Snapshot.commands.Count);
        }

        [Test] public void LockedAndOversizedWritesKeepPublicSnapshotAndRawCheckpoint()
        {
            var store = new CompetitionStore(Checkpoint); var session = new CompetitionSession(store, CompetitionV4Tests.Definition());
            session.Start(new Profile(), 123, "atomic-v4"); string raw = File.ReadAllText(Checkpoint), before = LoopJson.Encode(session.Snapshot);
            var command = session.Prepare("travel");
            using (var held = new FileStream(Checkpoint, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.Throws<IOException>(() => session.Execute(command));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before)); Assert.That(File.ReadAllText(Checkpoint), Is.EqualTo(raw));
            var huge = session.Snapshot; ++huge.revision; huge.state.participants[0].message = new string('가', 1500000);
            Assert.Throws<IOException>(() => store.Commit(huge, session.Snapshot.revision, session.Snapshot.generation));
            Assert.That(File.ReadAllText(Checkpoint), Is.EqualTo(raw));
            session.Execute(command); string committed = LoopJson.Encode(session.Snapshot); session.Execute(command);
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(committed)); Assert.That(session.Snapshot.commands.Count, Is.EqualTo(1));
        }

        [Test] public void ChecksumMismatchAndDamagedBackupNeverReplacePublishedOrStoredProgress()
        {
            var store = new CompetitionStore(Checkpoint); var session = new CompetitionSession(store, CompetitionV4Tests.Definition());
            session.Start(new Profile(), 123, "corruption-v4"); session.Execute(session.Prepare("travel"));
            byte[] original = File.ReadAllBytes(Checkpoint), backup = File.ReadAllBytes(Checkpoint + ".backup");
            string published = LoopJson.Encode(session.Snapshot);
            var envelope = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Checkpoint));
            string checksum = (string)envelope["checksum"];
            envelope["checksum"] = (checksum[0] == 'A' ? "B" : "A") + checksum.Substring(1);
            File.WriteAllText(Checkpoint, envelope.ToString(Newtonsoft.Json.Formatting.None));
            byte[] mismatched = File.ReadAllBytes(Checkpoint);
            Assert.Throws<IOException>(() => session.Reload(), "Valid JSON with a wrong checksum must not be published.");
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(published));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(mismatched));
            Assert.That(File.ReadAllBytes(Checkpoint + ".backup"), Is.EqualTo(backup));

            // Restore only this fixture's primary, then damage its backup before any recovery write.
            File.WriteAllBytes(Checkpoint, original);
            File.WriteAllText(Checkpoint + ".backup", "damaged-backup-fixture");
            byte[] damagedBackup = File.ReadAllBytes(Checkpoint + ".backup");
            Assert.Throws<IOException>(() => session.InspectBackup());
            Assert.Throws<IOException>(() => session.RestoreBackup(), "An unreadable backup cannot advance the generation or replace the primary.");
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(published));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(Checkpoint + ".backup"), Is.EqualTo(damagedBackup));
            Assert.That(File.Exists(Checkpoint + ".generation"), Is.False);
            Assert.That(Directory.GetFiles(directory, "*.damaged-*"), Is.Empty);
        }

        [Test] public void CommandCapacityStopsBeforeAnyV4ActionOrWrite()
        {
            var store = new CompetitionStore(Checkpoint); var session = new CompetitionSession(store, CompetitionV4Tests.Definition());
            session.Start(new Profile(), 123, "limit-v4"); var full = session.Snapshot;
            for (int i = 1; i <= BackendProtocol.MaxCommands; ++i)
            {
                string id = "fixture-" + i;
                full.commands.Add(new CompetitionCommandEntry { sequence = i, kind = "travel", arg = "", clientCommandId = id, targetId = "fixture" });
                full.receipts.Add(new Receipt { id = id, fingerprint = new string('0', 64), revision = i });
            }
            // A format-valid boundary fixture; it does not claim C6 command replay authority.
            full.revision = BackendProtocol.MaxCommands; CompetitionStore.ValidateDocument(full);
            var memory = new SnapshotStore(full); session = new CompetitionSession(memory, CompetitionV4Tests.Definition());
            string before = LoopJson.Encode(session.Snapshot);
            Assert.Throws<InvalidOperationException>(() => session.Execute(session.Prepare("travel")));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before)); Assert.That(memory.writes, Is.Zero);
        }
        sealed class SnapshotStore : ICompetitionStore
        {
            readonly CompetitionDocument value; public int writes;
            public SnapshotStore(CompetitionDocument value) { this.value = value; }
            public CompetitionDocument Read() => LoopJson.Copy(value);
            public void Commit(CompetitionDocument candidate, long revision, int generation, bool replaceActive = false) { ++writes; throw new IOException("Unexpected commit"); }
            public CompetitionDocument InspectBackup() => throw new NotSupportedException();
            public CompetitionDocument RestoreBackup() => throw new NotSupportedException();
        }
    }
}
