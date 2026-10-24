using System;
using System.IO;
using System.Linq;
using System.Text;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionV5StoreTests
    {
        string directory;
        string Checkpoint => Path.Combine(directory, "checkpoint.json");
        [SetUp] public void Setup() => directory = Path.Combine(Path.GetTempPath(), "DiceWarriorV5Tests", Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup()
        {
            string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DiceWarriorV5Tests")) + Path.DirectorySeparatorChar;
            string resolved = Path.GetFullPath(directory);
            if (!resolved.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Fixture cleanup escaped its private root.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }

        [Test] public void EverySelectionStageReloadsExactCommittedFacesHeldRngAndRemainingRerolls()
        {
            var session = NewCombatSession();
            string die = session.Snapshot.state.participants[0].dice[0].instanceId;
            string[] kinds = { "combat_roll", "combat_hold", "combat_release", "combat_reroll" };
            foreach (string kind in kinds)
            {
                session.Execute(session.Prepare(kind, kind == "combat_hold" || kind == "combat_release" ? die : ""));
                string before = LoopJson.Encode(session.Snapshot); byte[] raw = File.ReadAllBytes(Checkpoint);
                session = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV5Tests.Definition());
                Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before), kind);
                Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw), "Reload is read-only.");
            }
            Assert.That(session.Snapshot.state.participants[0].v5.selection.rerollsLeft, Is.Zero);
            session.Execute(session.Prepare("combat_execute"));
            Assert.That(session.Snapshot.state.participants[0].encounterTurns, Is.EqualTo(1));
        }

        [TestCase("combat_hold")] [TestCase("combat_reroll")] [TestCase("combat_execute")]
        public void SaveFailurePreservesPublishedAndRawStateThenSameIdRetriesExactlyOnce(string kind)
        {
            var fault = new FaultStore(new CompetitionStore(Checkpoint)); var session = NewCombatSession(fault);
            session.Execute(session.Prepare("combat_roll"));
            string arg = kind == "combat_hold" ? session.Snapshot.state.participants[0].dice[0].instanceId : "";
            var command = session.Prepare(kind, arg); var before = session.Snapshot;
            string published = LoopJson.Encode(before); byte[] raw = File.ReadAllBytes(Checkpoint), backup = File.ReadAllBytes(Checkpoint + ".backup");
            var expected = LoopJson.Copy(before.state); CompetitionCommandRules.Apply(expected, kind, arg);
            fault.failBefore = true;
            Assert.Throws<IOException>(() => session.Execute(command));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(published));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw));
            Assert.That(File.ReadAllBytes(Checkpoint + ".backup"), Is.EqualTo(backup));
            fault.failBefore = false; session.Execute(command);
            string committed = LoopJson.Encode(session.Snapshot); byte[] committedRaw = File.ReadAllBytes(Checkpoint);
            session.Execute(command);
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(committed));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(committedRaw));
            Assert.That(LoopJson.Encode(session.Snapshot.state), Is.EqualTo(LoopJson.Encode(expected)));
            Assert.That(session.Snapshot.commands.Count, Is.EqualTo(before.commands.Count + 1));
            Assert.That(session.Snapshot.receipts.Count(r => r.id == command.id), Is.EqualTo(1));
        }

        [TestCase("combat_roll")] [TestCase("combat_reroll")] [TestCase("combat_execute")]
        public void LostAcknowledgementRecoversDiskReceiptWithoutRerollOrSecondEnemyAction(string kind)
        {
            var fault = new FaultStore(new CompetitionStore(Checkpoint)); var session = NewCombatSession(fault);
            if (kind != "combat_roll") session.Execute(session.Prepare("combat_roll"));
            var before = session.Snapshot; var command = session.Prepare(kind);
            fault.failAfter = true;
            Assert.Throws<IOException>(() => session.Execute(command));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(LoopJson.Encode(before)));
            var persisted = new CompetitionStore(Checkpoint).Read(); byte[] raw = File.ReadAllBytes(Checkpoint);
            Assert.That(persisted.commands.Count, Is.EqualTo(before.commands.Count + 1));
            fault.failAfter = false; session.Execute(command); session.Execute(command);
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(LoopJson.Encode(persisted)));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw));
            var restarted = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV5Tests.Definition());
            restarted.Execute(command);
            Assert.That(LoopJson.Encode(restarted.Snapshot), Is.EqualTo(LoopJson.Encode(persisted)));
        }

        [Test] public void DuplicateIdDifferentBodyAndStaleTargetCannotChangeCommittedSelection()
        {
            var session = NewCombatSession(); var roll = session.Prepare("combat_roll");
            var stale = session.Prepare("combat_roll"); session.Execute(roll);
            string before = LoopJson.Encode(session.Snapshot); byte[] raw = File.ReadAllBytes(Checkpoint);
            var changed = LoopJson.Copy(roll); changed.kind = "combat_execute";
            Assert.Throws<InvalidOperationException>(() => session.Execute(changed));
            Assert.Throws<InvalidOperationException>(() => session.Execute(stale));
            var foreign = session.Prepare("combat_hold", "foreign-die");
            Assert.Catch(() => session.Execute(foreign));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw));
        }

        [Test] public void RecoveryRestoresConfirmedSelectionAndInvalidatesPreRecoveryInput()
        {
            var store = new CompetitionStore(Checkpoint); var session = NewCombatSession(store);
            session.Execute(session.Prepare("combat_roll")); var rolled = session.Snapshot;
            session.Execute(session.Prepare("combat_hold", rolled.state.participants[0].dice[0].instanceId));
            var stale = session.Prepare("combat_execute");
            File.WriteAllText(Checkpoint, "isolated-corrupt-v5-fixture");
            Assert.Throws<IOException>(() => session.Reload());
            session.RestoreBackup();
            Assert.That(LoopJson.Encode(session.Snapshot.state), Is.EqualTo(LoopJson.Encode(rolled.state)));
            Assert.That(session.Snapshot.generation, Is.EqualTo(rolled.generation + 1));
            Assert.That(Directory.GetFiles(directory, "*.damaged-*").Select(File.ReadAllText), Does.Contain("isolated-corrupt-v5-fixture"));
            Assert.Throws<InvalidOperationException>(() => session.Execute(stale));
            session.Execute(session.Prepare("combat_hold", rolled.state.participants[0].dice[0].instanceId));
            Assert.That(session.Snapshot.state.participants[0].v5.selection.results[0].held, Is.True);
        }

        [Test] public void NewApplicationResumesV4CombatAndExplicitRestartPreservesItsExactRawCheckpoint()
        {
            var store = new CompetitionStore(Checkpoint); var legacy = new CompetitionSession(store, CompetitionV4Tests.Definition());
            legacy.Start(new Profile(), 123, "legacy-v4"); ReachCombat(legacy);
            string old = LoopJson.Encode(legacy.Snapshot); byte[] original = File.ReadAllBytes(Checkpoint);
            var updated = new CompetitionSession(store, CompetitionV5Tests.Definition());
            Assert.That(LoopJson.Encode(updated.Snapshot), Is.EqualTo(old));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(original));
            Assert.That(updated.Snapshot.state.participants.All(p => p.v5 == null), Is.True);
            Assert.That(LoopJson.Encode(updated.Snapshot.state), Does.Not.Contain("\"v5\""));
            updated.Execute(updated.Prepare("combat"));
            Assert.That(updated.Snapshot.state.definition.rulesVersion, Is.EqualTo("shared-world-preview-4"));
            byte[] progressed = File.ReadAllBytes(Checkpoint); string progressedState = LoopJson.Encode(updated.Snapshot.state);
            Assert.Throws<InvalidOperationException>(() => updated.Start(new Profile(), 456, "unconfirmed"));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(progressed));
            updated.Restart(new Profile(), 456, "explicit-v5", "scholar");
            Assert.That(updated.Snapshot.state.definition.rulesVersion, Is.EqualTo("shared-world-preview-5"));
            Assert.That(Directory.GetFiles(directory, "*.restarted-*").Select(File.ReadAllBytes).Any(bytes => bytes.SequenceEqual(progressed)), Is.True);
            Assert.That(LoopJson.Encode(store.InspectBackup().state), Is.EqualTo(progressedState));
            Assert.That(updated.Snapshot.commands, Is.Empty); Assert.That(updated.Snapshot.receipts, Is.Empty);
            updated.RestoreBackup();
            Assert.That(LoopJson.Encode(updated.Snapshot.state), Is.EqualTo(progressedState));
        }

        [Test] public void FullStoredGameIncludingAiReplaysHumanCommandsAndStaysBelowFourMiB()
        {
            var definition = CompetitionV5Tests.Definition(); var profile = new Profile { growth = 2 };
            var session = new CompetitionSession(new CompetitionStore(Checkpoint), definition);
            session.Start(profile, 456, "stored-v5", "scholar");
            var replay = CompetitionRules.Start(definition, profile, 456, "stored-v5", "scholar");
            long maxBytes = new FileInfo(Checkpoint).Length; bool restoredSelection = false;
            for (int count = 0; count < 2048 && !session.Snapshot.state.finished; ++count)
            {
                var c = CompetitionV5Tests.NextCommand(session.Snapshot.state);
                var command = session.Prepare(c.kind, c.arg); session.Execute(command);
                CompetitionCommandRules.Apply(replay, c.kind, c.arg);
                Assert.That(LoopJson.Hash(LoopJson.Encode(session.Snapshot.state)), Is.EqualTo(LoopJson.Hash(LoopJson.Encode(replay))), "Full state including AI at " + count);
                maxBytes = Math.Max(maxBytes, new FileInfo(Checkpoint).Length);
                if (!restoredSelection && c.kind == "combat_roll")
                {
                    string before = LoopJson.Encode(session.Snapshot);
                    session = new CompetitionSession(new CompetitionStore(Checkpoint), definition);
                    replay = LoopJson.Decode<CompetitionState>(LoopJson.Encode(replay));
                    Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before)); restoredSelection = true;
                }
            }
            var final = session.Snapshot;
            Assert.That(restoredSelection, Is.True); Assert.That(final.state.finished, Is.True);
            Assert.That(final.state.participants.Skip(1).Any(p => p.turn > 0), Is.True);
            Assert.That(final.commands.Count, Is.LessThanOrEqualTo(2048));
            Assert.That(final.state.actionNumber, Is.EqualTo(final.commands.Count));
            Assert.That(maxBytes, Is.LessThan(4 * 1024 * 1024));
            TestContext.WriteLine("v5 real envelope peak=" + maxBytes + " bytes; commands=" + final.commands.Count + "; stateHash=" + LoopJson.Hash(LoopJson.Encode(final.state)) + "; commandHash=" + LoopJson.Hash(LoopJson.Encode(final.commands)));
        }

        [Test] public void LastThreeReservedCommandsFinishAt2048AndOptionalToggleCannotOverwriteFile()
        {
            var document = BoundaryDocument(2045); var p = document.state.participants[0];
            document.state.worldRound = 30; p.encounterTurns = 19; p.turn = 19; p.v5.selection.combatTurnSequence = 20;
            WriteBoundaryFixture(document);
            var session = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV5Tests.Definition());
            session.Execute(session.Prepare("combat_roll")); byte[] before = File.ReadAllBytes(Checkpoint);
            Assert.Throws<InvalidOperationException>(() => session.Execute(session.Prepare("combat_hold", p.dice[0].instanceId)));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(before));
            session.Execute(session.Prepare("combat_reroll")); session.Execute(session.Prepare("combat_execute"));
            Assert.That(session.Snapshot.commands.Count, Is.EqualTo(2048));
            Assert.That(session.Snapshot.state.actionNumber, Is.EqualTo(2048));
            Assert.That(session.Snapshot.state.finished, Is.True);
            Assert.That(new FileInfo(Checkpoint).Length, Is.LessThan(4 * 1024 * 1024));
            TestContext.WriteLine("2048-command format fixture actual envelope=" + new FileInfo(Checkpoint).Length + " bytes; historical placeholder commands do not claim replay authority.");
        }

        [Test] public void InflatedOrOmittedActionCountCannotBypassReservationDuringLoad()
        {
            var document = BoundaryDocument(12);
            foreach (int forged in new[] { 0, 11, 13 })
            {
                document.state.actionNumber = forged;
                Assert.Throws<InvalidOperationException>(() => CompetitionStore.ValidateDocument(document));
            }
        }

        [Test] public void Full2048CommandCheckpointRejectsFurtherActionWithoutWriting()
        {
            WriteBoundaryFixture(BoundaryDocument(2048));
            var session = new CompetitionSession(new CompetitionStore(Checkpoint), CompetitionV5Tests.Definition());
            string before = LoopJson.Encode(session.Snapshot); byte[] raw = File.ReadAllBytes(Checkpoint);
            Assert.Throws<InvalidOperationException>(() => session.Execute(session.Prepare("combat_roll")));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw));
        }

        [Test] public void OversizedCommitPreservesActualPrimaryBackupAndPublishedSelection()
        {
            var store = new CompetitionStore(Checkpoint); var session = NewCombatSession(store);
            session.Execute(session.Prepare("combat_roll"));
            string before = LoopJson.Encode(session.Snapshot); byte[] raw = File.ReadAllBytes(Checkpoint), backup = File.ReadAllBytes(Checkpoint + ".backup");
            var huge = session.Snapshot; ++huge.revision; huge.state.participants[0].message = new string('가', 1500000);
            Assert.That(Encoding.UTF8.GetByteCount(LoopJson.Encode(huge)), Is.GreaterThan(4 * 1024 * 1024));
            Assert.Throws<IOException>(() => store.Commit(huge, session.Snapshot.revision, session.Snapshot.generation));
            Assert.That(File.ReadAllBytes(Checkpoint), Is.EqualTo(raw));
            Assert.That(File.ReadAllBytes(Checkpoint + ".backup"), Is.EqualTo(backup));
            Assert.That(LoopJson.Encode(session.Snapshot), Is.EqualTo(before));
        }

        CompetitionSession NewCombatSession(ICompetitionStore store = null)
        {
            var session = new CompetitionSession(store ?? new CompetitionStore(Checkpoint), CompetitionV5Tests.Definition());
            session.Start(new Profile(), 123, "stored-combat"); ReachCombat(session); return session;
        }

        static void ReachCombat(CompetitionSession session)
        {
            for (int count = 0; count < 200 && !session.Snapshot.state.finished; ++count)
            {
                var state = session.Snapshot.state; var p = state.participants[0];
                if (p.phase == CompetitionPhase.Combat) return;
                var c = CompetitionV5Tests.NextCommand(state);
                if (p.phase == CompetitionPhase.Encounter)
                {
                    var combat = CompetitionCommandRules.LegalChoices(state, p).FirstOrDefault(choice => choice.kind == EncounterKind.Combat);
                    if (combat != null) c.arg = combat.id;
                }
                session.Execute(session.Prepare(c.kind, c.arg));
            }
            Assert.Fail("Fixture must reach a real legal combat before testing storage.");
        }

        static CompetitionDocument BoundaryDocument(int count)
        {
            var document = new CompetitionDocument { revision = count + 1, state = CompetitionV5Tests.Combat() };
            document.state.actionNumber = count;
            for (int i = 1; i <= count; ++i)
            {
                string id = "boundary-fixture-" + i;
                document.commands.Add(new CompetitionCommandEntry { sequence = i, kind = "travel", arg = "", targetId = "fixture-only", clientCommandId = id });
                document.receipts.Add(new Receipt { id = id, fingerprint = new string('0', 64), revision = i + 1 });
            }
            return document;
        }

        void WriteBoundaryFixture(CompetitionDocument document)
        {
            // Format/capacity fixture only. The placeholder historical entries do not claim semantic replay validity.
            CompetitionStore.ValidateDocument(document); string payload = LoopJson.Encode(document);
            string envelope = LoopJson.Encode(new { format = 1, payload, checksum = LoopJson.Hash(payload) });
            Directory.CreateDirectory(directory); File.WriteAllText(Checkpoint, envelope, new UTF8Encoding(false));
        }

        sealed class FaultStore : ICompetitionStore
        {
            readonly CompetitionStore inner;
            public bool failBefore, failAfter;
            public FaultStore(CompetitionStore inner) { this.inner = inner; }
            public CompetitionDocument Read() => inner.Read();
            public void Commit(CompetitionDocument candidate, long revision, int generation, bool replaceActive = false)
            {
                if (failBefore) throw new IOException("Isolated pre-commit fault");
                inner.Commit(candidate, revision, generation, replaceActive);
                if (failAfter) throw new IOException("Isolated acknowledgement loss after real file commit");
            }
            public CompetitionDocument InspectBackup() => inner.InspectBackup();
            public CompetitionDocument RestoreBackup() => inner.RestoreBackup();
        }
    }
}
