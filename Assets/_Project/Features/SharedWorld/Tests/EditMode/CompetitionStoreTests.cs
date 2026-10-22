using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionStoreTests
    {
        string directory;
        string FilePath => Path.Combine(directory, "checkpoint.json");
        static CompetitionDefinition Definition()
        {
            string file = Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-1.json");
            if (!File.Exists(file)) file = "Assets/_Project/Features/SharedWorld/Content/shared-world-preview-1.json";
            return LoopJson.Decode<CompetitionDefinition>(File.ReadAllText(file));
        }
        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "DiceWarriorCompetitionTests", Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown()
        {
            string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DiceWarriorCompetitionTests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        sealed class MemoryStore : ICompetitionStore
        {
            public CompetitionDocument value = new CompetitionDocument();
            public bool fail, failAfterWrite;
            public CompetitionDocument Read() => LoopJson.Copy(value);
            public void Commit(CompetitionDocument candidate, long revision, int generation, bool replaceActive = false)
            {
                if (fail) throw new IOException("Injected write failure");
                if (value.revision != revision || value.generation != generation) throw new InvalidOperationException("Stale checkpoint");
                value = LoopJson.Copy(candidate);
                if (failAfterWrite) throw new IOException("Injected lost acknowledgement");
            }
            public CompetitionDocument InspectBackup() => throw new NotSupportedException();
            public CompetitionDocument RestoreBackup() => throw new NotSupportedException();
        }
        [Test] public void FailedStartDoesNotPublishCandidateOrOverwriteProfile()
        {
            var store = new MemoryStore { fail = true };
            var session = new CompetitionSession(store, Definition());
            var profile = new Profile { growth = 4, currency = 123 };
            Assert.Throws<IOException>(() => session.Start(profile, 33, "save-failure"));
            Assert.That(session.Snapshot.state, Is.Null);
            Assert.That(store.Read().revision, Is.Zero);
            Assert.That(profile.currency, Is.EqualTo(123));
        }
        [Test] public void FailedCommandCanRetrySameIdAndNeverPublishesUncommittedState()
        {
            var store = new MemoryStore(); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 33, "retry");
            var command = session.Prepare("travel"); string before = LoopJson.Encode(session.Snapshot);
            store.fail = true;
            Assert.Throws<IOException>(() => session.Execute(command));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
            Assert.That(LoopJson.Encode(store.Read()), Is.EqualTo(before));
            store.fail = false; session.Execute(command);
            string committed = LoopJson.Encode(session.Snapshot);
            session.Execute(command);
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(committed));
            Assert.That(session.Snapshot.commands.Count, Is.EqualTo(1));
            command.arg = "changed";
            Assert.Throws<InvalidOperationException>(() => session.Execute(command));
        }
        [Test] public void SnapshotAndStartInputsCannotMutatePublishedState()
        {
            var store = new MemoryStore(); var definition = Definition(); var session = new CompetitionSession(store, definition);
            var profile = new Profile { growth = 2 }; session.Start(profile, 19, "copies");
            string committed = LoopJson.Encode(session.Snapshot);
            profile.growth = 100; definition.world.places[1].name = "changed";
            session.Snapshot.state.participants[0].hp = 0;
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(committed));
        }
        [Test] public void LostWriteAcknowledgementRecoversExistingReceiptWithoutRepeatingAction()
        {
            var store = new MemoryStore(); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 33, "lost-ack"); var command = session.Prepare("travel");
            string before = LoopJson.Encode(session.Snapshot); store.failAfterWrite = true;
            Assert.Throws<IOException>(() => session.Execute(command));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
            string committed = LoopJson.Encode(store.Read()); store.failAfterWrite = false;
            session.Execute(command);
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(committed));
            Assert.That(session.Snapshot.commands.Count, Is.EqualTo(1));
        }
        [Test] public void TwoSessionsRejectStaleInputAndFileCommitHasOneWinner()
        {
            var first = new CompetitionSession(new CompetitionStore(FilePath), Definition());
            first.Start(new Profile(), 33, "cas");
            var second = new CompetitionSession(new CompetitionStore(FilePath), Definition());
            var a = first.Prepare("travel"); var b = second.Prepare("travel");
            int committed = 0; var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.Invoke(() => Run(first, a), () => Run(second, b));
            void Run(CompetitionSession session, CompetitionCommand command)
            {
                try { session.Execute(command); System.Threading.Interlocked.Increment(ref committed); }
                catch (Exception error) { errors.Add(error); }
            }
            Assert.That(committed, Is.EqualTo(1)); Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors.Single(), Is.InstanceOf<InvalidOperationException>());
            Assert.That(new CompetitionStore(FilePath).Read().commands.Count, Is.EqualTo(1));
        }
        [Test] public void BackupRestorePreservesDamagedBytesAndInvalidatesOldCommands()
        {
            var store = new CompetitionStore(FilePath); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 33, "restore"); var stale = session.Prepare("travel"); session.Execute(stale);
            var confirmed = store.InspectBackup();
            File.WriteAllText(FilePath, "damaged-user-checkpoint");
            Assert.Throws<IOException>(() => store.Read());
            var restored = store.RestoreBackup();
            Assert.That(LoopJson.Encode(restored.state), Is.EqualTo(LoopJson.Encode(confirmed.state)));
            Assert.That(restored.generation, Is.GreaterThan(confirmed.generation));
            Assert.That(Directory.GetFiles(directory, "*.damaged-*").Select(File.ReadAllText), Does.Contain("damaged-user-checkpoint"));
            session.Reload(); Assert.Throws<InvalidOperationException>(() => session.Execute(stale));
        }
        [Test] public void MissingPrimaryWithBackupIsNotSilentlyReinitialized()
        {
            var store = new CompetitionStore(FilePath); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 1, "missing"); session.Execute(session.Prepare("travel"));
            File.Delete(FilePath);
            Assert.Throws<IOException>(() => store.Read());
            Assert.That(store.InspectBackup().state.matchId, Is.EqualTo("missing"));
        }
        [Test] public void ForeignTargetAndInvalidRevisionCannotWriteCheckpoint()
        {
            var store = new CompetitionStore(FilePath); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 33, "target"); string before = File.ReadAllText(FilePath);
            var command = session.Prepare("travel"); command.targetId = "foreign-match";
            Assert.Throws<InvalidOperationException>(() => session.Execute(command));
            var candidate = session.Snapshot; candidate.revision += 2;
            Assert.Throws<InvalidOperationException>(() => store.Commit(candidate, candidate.revision - 2, candidate.generation));
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo(before));
        }
        [Test] public void NewMatchCannotReplaceActiveGame()
        {
            var store = new MemoryStore(); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 33, "active");
            Assert.Throws<InvalidOperationException>(() => session.Start(new Profile(), 1, "replacement"));
            Assert.That(session.Snapshot.state.matchId, Is.EqualTo("active"));
        }
        static CompetitionCommand Next(CompetitionSession session)
        {
            var state = session.Snapshot.state; var human = CompetitionRules.Human(state);
            switch (human.phase)
            {
                case CompetitionPhase.Map: return session.Prepare("travel");
                case CompetitionPhase.Combat: return session.Prepare("combat");
                case CompetitionPhase.LapReward: return session.Prepare("lap", human.completedLaps < 2 ? "balanced" : "aegis");
                case CompetitionPhase.Encounter: return session.Prepare("choose", CompetitionCommandRules.LegalChoices(state, human).First().id);
                default: return session.Prepare("advance");
            }
        }
        [Test] public void AiRoundAndHumanCommandCommitTogetherOrNotAtAll()
        {
            var store = new MemoryStore(); var session = new CompetitionSession(store, Definition());
            session.Start(new Profile(), 123, "atomic-ai");
            bool checkedBoundary = false;
            for (int count = 0; count < 80 && !session.Snapshot.state.finished; count++)
            {
                var before = session.Snapshot; var command = Next(session);
                var expected = LoopJson.Copy(before.state); CompetitionCommandRules.Apply(expected, command.kind, command.arg);
                if (expected.worldRound == before.state.worldRound) { session.Execute(command); continue; }
                string snapshot = LoopJson.Encode(before); store.fail = true;
                Assert.Throws<IOException>(() => session.Execute(command));
                Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(snapshot));
                Assert.That(LoopJson.Encode(new CompetitionSession(store, Definition()).Snapshot), Is.EqualTo(snapshot));
                store.fail = false; session.Execute(command); session.Execute(command);
                Assert.That(LoopJson.Encode(session.Snapshot.state), Is.EqualTo(LoopJson.Encode(expected)));
                Assert.That(session.Snapshot.commands.Count, Is.EqualTo(before.commands.Count + 1));
                checkedBoundary = true; break;
            }
            Assert.That(checkedBoundary, Is.True, "The test must cross a real human-to-AI round boundary.");
        }
        [Test] public void StartingNextMatchArchivesCompletedCheckpointWithoutTouchingLegacyFile()
        {
            Directory.CreateDirectory(directory); string legacy = Path.Combine(directory, "legacy.json");
            File.WriteAllText(legacy, "user-owned-v1-data");
            var session = new CompetitionSession(new CompetitionStore(FilePath), Definition());
            session.Start(new Profile(), 123, "completed"); session.Execute(session.Prepare("abandon"));
            for (int i = 0; i < 30 && !session.Snapshot.state.finished; i++) session.Execute(session.Prepare("advance"));
            Assert.That(session.Snapshot.state.finished, Is.True);
            string previous = File.ReadAllText(FilePath);
            session.Start(new Profile(), 456, "new-match");
            Assert.That(Directory.GetFiles(directory, "*.completed-*").Select(File.ReadAllText), Does.Contain(previous));
            Assert.That(File.ReadAllText(legacy), Is.EqualTo("user-owned-v1-data"));
            Assert.That(session.Snapshot.commands, Is.Empty); Assert.That(session.Snapshot.state.matchId, Is.EqualTo("new-match"));
        }
        [Test] public void ConfirmedRestartArchivesActiveRunAndClearsOnlyCompetitionProgress()
        {
            var store = new CompetitionStore(FilePath); var session = new CompetitionSession(store, Definition());
            var profile = new Profile { growth = 3, currency = 123 };
            session.Start(profile, 33, "active-old"); session.Execute(session.Prepare("travel"));
            string original = File.ReadAllText(FilePath), before = LoopJson.Encode(session.Snapshot);
            Assert.Throws<InvalidOperationException>(()=>session.Start(profile, 51, "no-confirm"));
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo(original));
            var staleCommand = session.Prepare("abandon");
            session.Restart(profile, 51, "confirmed-new");
            Assert.That(Directory.GetFiles(directory, "*.restarted-*").Select(File.ReadAllText), Does.Contain(original));
            Assert.That(LoopJson.Encode(store.InspectBackup()), Is.EqualTo(before));
            var after = session.Snapshot;
            Assert.That(after.state.matchId, Is.EqualTo("confirmed-new"));
            Assert.That(after.state.worldRound, Is.EqualTo(1)); Assert.That(after.commands, Is.Empty); Assert.That(after.receipts, Is.Empty);
            Assert.That(after.state.participants.All(p=>p.position==0 && p.completedLaps==0 && p.dice.Count==1 && p.hp==75), Is.True);
            Assert.That(profile.currency, Is.EqualTo(123)); Assert.That(profile.growth, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(()=>session.Execute(staleCommand));
            Assert.That(LoopJson.Encode(new CompetitionSession(store,Definition()).Snapshot), Is.EqualTo(LoopJson.Encode(after)));
        }
        [Test] public void FailedOrStaleRestartPreservesCurrentAdventure()
        {
            var memory = new MemoryStore(); var session = new CompetitionSession(memory, Definition());
            session.Start(new Profile(), 33, "before-failure"); string before = LoopJson.Encode(session.Snapshot);
            memory.fail=true;
            Assert.Throws<IOException>(()=>session.Restart(new Profile(), 51, "failed-restart"));
            Assert.That(LoopJson.Encode(memory.Read()), Is.EqualTo(before)); Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
            var store = new CompetitionStore(FilePath); var first = new CompetitionSession(store, Definition());
            first.Start(new Profile(),33,"concurrent"); var stale = new CompetitionSession(store,Definition());
            first.Execute(first.Prepare("travel")); string current=File.ReadAllText(FilePath);
            Assert.Throws<InvalidOperationException>(()=>stale.Restart(new Profile(),51,"stale-reset"));
            Assert.That(File.ReadAllText(FilePath),Is.EqualTo(current));
            Assert.That(Directory.GetFiles(directory,"*.restarted-*"),Is.Empty);
            using(var held=new FileStream(FilePath,FileMode.Open,FileAccess.Read,FileShare.None))
                Assert.Throws<IOException>(()=>first.Restart(new Profile(),51,"locked-reset"));
            Assert.That(File.ReadAllText(FilePath),Is.EqualTo(current));
        }
    }
}
