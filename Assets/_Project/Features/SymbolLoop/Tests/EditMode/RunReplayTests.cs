using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace DiceWarrior.SymbolLoop.Tests
{
    // Golden expectations are a checked-in artifact. Tests never write or regenerate them.
    public sealed class RunReplayTests
    {
        const string GoldenPath = "Assets/_Project/Features/SymbolLoop/Tests/EditMode/ReplayGoldenVectors.json";

        [Serializable] public sealed class GoldenFile
        {
            public int format = 1;
            public string rulesVersion = BackendProtocol.RulesVersion;
            public List<GoldenVector> vectors = new List<GoldenVector>();
        }
        [Serializable] public sealed class GoldenVector
        {
            public string name;
            public RunTicket ticket;
            public int resumeAfter, payout;
            public List<RunCommandEntry> commands = new List<RunCommandEntry>();
            public List<ReplayState> states = new List<ReplayState>();
        }
        [Serializable] public sealed class ReplayState
        {
            public uint seed, travelRng, combatRng;
            public int region, completedRegions, position, lastTravel, hp, maxHp, power, guard, gold, xp, level, turn;
            public Phase phase;
            public bool won;
            public Tile[] board;
            public List<DieSlot> dice;
            public string[] equipment, claimed;
            public Enemy enemy;
            public TurnLog lastTurn;
        }

        static GoldenFile ReadGolden()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), GoldenPath);
            Assert.That(File.Exists(path), Is.True, "Generate and inspect the golden file explicitly before running replay tests.");
            var file = LoopJson.Decode<GoldenFile>(File.ReadAllText(path));
            Assert.That(file.format, Is.EqualTo(1));
            Assert.That(file.rulesVersion, Is.EqualTo(BackendProtocol.RulesVersion));
            return file;
        }

        [TestCase("victory")]
        [TestCase("defeat")]
        [TestCase("abandon-after-region")]
        [TestCase("shop-equipment-resume")]
        public void FixedGoldenReplaysEveryStateAndResumesWithoutRandomDrift(string name)
        {
            var vector = ReadGolden().vectors.Single(v => v.name == name);
            string ticketBefore = LoopJson.Encode(vector.ticket), commandsBefore = LoopJson.Encode(vector.commands);
            var run = NewRun(vector.ticket);
            AssertState(vector.states[0], Capture(run), name + " initial");
            Assert.That(vector.states.Count, Is.EqualTo(vector.commands.Count + 1));
            for (int i = 0; i < vector.commands.Count; ++i)
            {
                var command = vector.commands[i];
                Assert.That(RunCommandRules.Target(run), Is.EqualTo(command.targetId), name + " target " + command.sequence);
                RunCommandRules.Apply(run, command.kind, command.arg);
                AssertState(vector.states[i + 1], Capture(run), name + " command " + command.sequence);
                // A process restart reconstructs only the persisted snapshot, not an in-memory RNG.
                if (i + 1 == vector.resumeAfter) run = LoopJson.Copy(run);
            }
            var verified = MockRunVerifier.Verify(vector.ticket, vector.commands);
            Assert.That(verified.accepted, Is.True, verified.code + ": " + verified.message);
            Assert.That(verified.payout, Is.EqualTo(vector.payout));
            AssertState(vector.states.Last(), Capture(verified.finalRun), name + " verifier final");
            Assert.That(LoopJson.Encode(vector.ticket), Is.EqualTo(ticketBefore));
            Assert.That(LoopJson.Encode(vector.commands), Is.EqualTo(commandsBefore));
            verified.finalRun.rules.hp = 1;
            verified.finalRun.dice[0].definitionId = "changed-output";
            Assert.That(LoopJson.Encode(vector.ticket), Is.EqualTo(ticketBefore), "Output must not alias authority inputs.");
        }

        [Test] public void GoldenCoverageIncludesTerminalOutcomesSlotsShoppingGrowthAndComboEffects()
        {
            var vectors = ReadGolden().vectors;
            Assert.That(vectors.Count, Is.InRange(3, 5));
            Assert.That(vectors.Select(v => v.ticket.seed).Distinct().Count(), Is.EqualTo(vectors.Count));
            Assert.That(vectors.Single(v => v.name == "victory").states.Last().won, Is.True);
            Assert.That(vectors.Single(v => v.name == "defeat").states.Last().hp, Is.Zero);
            var abandoned = vectors.Single(v => v.name == "abandon-after-region");
            Assert.That(abandoned.commands.Last().kind, Is.EqualTo("abandon"));
            Assert.That(abandoned.states.Last().completedRegions, Is.EqualTo(1));
            Assert.That(abandoned.payout, Is.EqualTo(10));
            var feature = vectors.Single(v => v.name == "shop-equipment-resume");
            Assert.That(feature.commands.Any(c => c.arg.StartsWith("die:")), Is.True);
            Assert.That(feature.commands.Any(c => c.arg.StartsWith("gear:")), Is.True);
            Assert.That(feature.commands.Where((c, i) => feature.states[i].phase == Phase.Shop && c.kind == "choose" && c.arg != "leave").Any(), Is.True);
            Assert.That(feature.states.Any(s => s.level > 1), Is.True);
            foreach (int slots in new[] { 1, 2, 3 })
                Assert.That(vectors.SelectMany(v => v.states).Any(s => s.dice.Count == slots), Is.True, "dice count " + slots);
            var turns = vectors.SelectMany(v => v.states).Where(s => s.lastTurn != null).Select(s => s.lastTurn).ToArray();
            Assert.That(turns.Any(t => t.faces.Length == 2 && t.skills.Count == 1), Is.True, "Two-symbol combo");
            Assert.That(turns.Any(t => t.faces.Length == 3 && t.skills.Count == 1), Is.True, "Three-symbol combo");
        }

        [Test] public void SequenceOrderDuplicateTargetAndTerminalTamperingAreRejectedAtTheirCommand()
        {
            var vector = ReadGolden().vectors.Single(v => v.name == "victory");
            CheckMutation(vector, c => c[0].sequence = 2, "REPLAY_SEQUENCE_INVALID", 1);
            CheckMutation(vector, c => c[1].clientCommandId = c[0].clientCommandId, "REPLAY_COMMAND_ID_DUPLICATE", 2);
            CheckMutation(vector, c => c[0].targetId = "1:11:Map:999", "REPLAY_TARGET_MISMATCH", 1);
            CheckMutation(vector, c => { var entry = c[0]; c[0] = c[1]; c[1] = entry; Renumber(c); }, "REPLAY_TARGET_MISMATCH", 1);
            // Removing the movement that produced the following target cannot be replayed.
            CheckMutation(vector, c => { c.RemoveAt(0); Renumber(c); }, "REPLAY_TARGET_MISMATCH", 1);
            CheckMutation(vector, c => c.RemoveAt(c.Count - 1), "REPLAY_INCOMPLETE", vector.commands.Count - 1);
            CheckMutation(vector, c => c.Add(new RunCommandEntry { sequence = c.Count + 1, kind = "travel", arg = "",
                targetId = "3:0:Result:0", clientCommandId = "after-result" }), "REPLAY_ALREADY_FINISHED", vector.commands.Count + 1);
        }

        [Test] public void InvalidPhaseGearSlotPaymentAndClientProvidedRollCannotCreateRewards()
        {
            var vectors = ReadGolden().vectors;
            var vector = vectors.Single(v => v.name == "victory");
            CheckMutation(vector, c => c[0].kind = "combat", "REPLAY_COMMAND_INVALID", 1);
            CheckMutation(vector, c => c[0].arg = "6", "REPLAY_COMMAND_INVALID", 1);
            int combat = vector.commands.FindIndex(c => c.kind == "combat");
            CheckMutation(vector, c => c[combat].arg = "Sword,Sword,Sword", "REPLAY_COMMAND_INVALID", combat + 1);
            var feature = vectors.Single(v => v.name == "shop-equipment-resume");
            int choice = feature.commands.FindIndex(c => c.arg.StartsWith("gear:") || c.arg.StartsWith("die:"));
            CheckMutation(feature, c => c[choice].arg = "gear:missing", "REPLAY_COMMAND_INVALID", choice + 1);
            CheckMutation(feature, c => c[choice].arg = "die:3:steel", "REPLAY_COMMAND_INVALID", choice + 1);
            CheckMutation(feature, c => c[choice].arg = "die:0:missing", "REPLAY_COMMAND_INVALID", choice + 1);
            // Stay in a shop until funds are exhausted, then a further purchase must fail.
            int shop = feature.commands.FindIndex(c => c.targetId.Contains(":Shop:"));
            var commands = LoopJson.Copy(feature.commands.Take(shop).ToList());
            var run = NewRun(feature.ticket);
            foreach (var command in commands) RunCommandRules.Apply(run, command.kind, command.arg);
            Assert.That(run.phase, Is.EqualTo(Phase.Shop));
            while (run.gold >= 20) Append(run, commands, "choose", "gear:mail");
            commands.Add(Entry(run, commands.Count + 1, "choose", "gear:mail"));
            AssertRejected(MockRunVerifier.Verify(feature.ticket, commands), "REPLAY_COMMAND_INVALID", commands.Count);
        }

        [Test] public void InvalidTicketUnsupportedRulesAndMalformedOrOversizedLogsFailClosed()
        {
            var ticket = Ticket(33, "boundary");
            var commands = new List<RunCommandEntry> { Entry(NewRun(ticket), 1, "abandon", "") };
            var unsupported = LoopJson.Copy(ticket); unsupported.rulesVersion = "future-rules";
            AssertRejected(MockRunVerifier.Verify(unsupported, commands), "REPLAY_RULES_UNSUPPORTED", 0);
            foreach (var invalid in new Action<RunTicket>[] { t => t.seed = 0, t => t.startingProfile = null,
                t => t.startingProfile.values.growth = 10001, t => t.startingProfile.values.startingWeapon = "mail",
                t => t.rules.dice[0] = null })
            {
                var copy = LoopJson.Copy(ticket); invalid(copy);
                AssertRejected(MockRunVerifier.Verify(copy, commands), "REPLAY_TICKET_INVALID", 0);
            }
            AssertRejected(MockRunVerifier.Verify(ticket, null), "REPLAY_LOG_EMPTY", 0);
            AssertRejected(MockRunVerifier.Verify(ticket, new List<RunCommandEntry>()), "REPLAY_LOG_EMPTY", 0);
            AssertRejected(MockRunVerifier.Verify(ticket, new RunCommandEntry[2049]), "REPLAY_LIMIT_EXCEEDED", 0);
            var oversized = Enumerable.Range(1, 200).Select(i => new RunCommandEntry { sequence = i, kind = "choose",
                arg = new string('가', 1024), targetId = "1:0:Map:0", clientCommandId = "size-" + i }).ToList();
            AssertRejected(MockRunVerifier.Verify(ticket, oversized), "REPLAY_LIMIT_EXCEEDED", 0);
            foreach (var change in new Action<RunCommandEntry>[] { c => c.clientCommandId = " ", c => c.clientCommandId = new string('x', 129),
                c => c.targetId = new string('x', 129), c => c.kind = "settle", c => c.arg = new string('x', 1025) })
            {
                var invalid = LoopJson.Copy(commands); change(invalid[0]);
                AssertRejected(MockRunVerifier.Verify(ticket, invalid), "REPLAY_COMMAND_INVALID", 1);
            }
            AssertRejected(MockRunVerifier.Verify(ticket, new RunCommandEntry[] { null }), "REPLAY_COMMAND_INVALID", 1);
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(() => MockRunVerifier.Verify(ticket, commands, cancellation.Token));
            }
        }

        [Test] public void FrozenTicketDeterminesPayoutAndOverflowIsRejected()
        {
            var vector = ReadGolden().vectors.Single(v => v.name == "victory");
            var frozen = LoopJson.Copy(vector.ticket);
            frozen.rules.regionCurrency = 7; frozen.rules.winCurrency = 11;
            var result = MockRunVerifier.Verify(frozen, vector.commands);
            Assert.That(result.accepted, Is.True, result.code);
            Assert.That(result.payout, Is.EqualTo(32));
            AssertState(vector.states.Last(), Capture(result.finalRun), "Frozen replay");
            frozen.rules.regionCurrency = int.MaxValue;
            AssertRejected(MockRunVerifier.Verify(frozen, vector.commands), "REPLAY_STATE_INVALID", vector.commands.Count);
        }

        static void CheckMutation(GoldenVector vector, Action<List<RunCommandEntry>> mutate, string code, int sequence)
        {
            var commands = LoopJson.Copy(vector.commands); mutate(commands);
            string ticketBefore = LoopJson.Encode(vector.ticket), logBefore = LoopJson.Encode(commands);
            AssertRejected(MockRunVerifier.Verify(vector.ticket, commands), code, sequence);
            Assert.That(LoopJson.Encode(vector.ticket), Is.EqualTo(ticketBefore));
            Assert.That(LoopJson.Encode(commands), Is.EqualTo(logBefore));
        }
        static void AssertRejected(RunVerificationResult result, string code, int sequence)
        {
            Assert.That(result.accepted, Is.False); Assert.That(result.code, Is.EqualTo(code), result.message);
            Assert.That(result.failedSequence, Is.EqualTo(sequence)); Assert.That(result.payout, Is.Zero);
            Assert.That(result.finalRun, Is.Null);
        }
        static void Renumber(List<RunCommandEntry> commands)
        { for (int i = 0; i < commands.Count; ++i) commands[i].sequence = i + 1; }

        static void AssertState(ReplayState expected, ReplayState actual, string context)
        {
            // Unity's bundled NUnit does not expose Assert.Multiple.
            {
                Assert.That(actual.seed, Is.EqualTo(expected.seed), context + " seed");
                Assert.That(actual.travelRng, Is.EqualTo(expected.travelRng), context + " travel RNG");
                Assert.That(actual.combatRng, Is.EqualTo(expected.combatRng), context + " combat RNG");
                Assert.That(new[] { actual.region, actual.completedRegions, actual.position, actual.lastTravel },
                    Is.EqualTo(new[] { expected.region, expected.completedRegions, expected.position, expected.lastTravel }), context + " board position");
                Assert.That(actual.phase, Is.EqualTo(expected.phase), context + " phase");
                Assert.That(actual.board, Is.EqualTo(expected.board), context + " board cells");
                Assert.That(new[] { actual.hp, actual.maxHp, actual.power, actual.guard, actual.gold, actual.xp, actual.level, actual.turn },
                    Is.EqualTo(new[] { expected.hp, expected.maxHp, expected.power, expected.guard, expected.gold, expected.xp, expected.level, expected.turn }), context + " player stats");
                Assert.That(actual.won, Is.EqualTo(expected.won), context + " won");
                Assert.That(actual.equipment, Is.EqualTo(expected.equipment), context + " equipment");
                Assert.That(actual.claimed, Is.EqualTo(expected.claimed), context + " claimed laps");
                Assert.That(actual.dice.Select(d => d.instanceId + "/" + d.definitionId),
                    Is.EqualTo(expected.dice.Select(d => d.instanceId + "/" + d.definitionId)), context + " dice");
                Assert.That(LoopJson.Encode(actual.enemy), Is.EqualTo(LoopJson.Encode(expected.enemy)), context + " enemy state");
                // The file stores every face, effect, shield transition and damage explicitly, without hashes.
                Assert.That(LoopJson.Encode(actual.lastTurn), Is.EqualTo(LoopJson.Encode(expected.lastTurn)), context + " turn effects");
            }
        }
        static ReplayState Capture(Run run) => new ReplayState {
            seed = run.seed, travelRng = run.travelRng, combatRng = run.combatRng, region = run.region,
            completedRegions = run.completedRegions, position = run.position, lastTravel = run.lastTravel, phase = run.phase,
            hp = run.hp, maxHp = run.maxHp, power = run.power, guard = run.guard, gold = run.gold, xp = run.xp,
            level = run.level, turn = run.turn, won = run.won, board = (Tile[])run.board.Clone(),
            dice = LoopJson.Copy(run.dice), equipment = (string[])run.equipment.Clone(), claimed = run.claimed.ToArray(),
            enemy = LoopJson.Copy(run.enemy), lastTurn = LoopJson.Copy(run.lastTurn)
        };
        static RunTicket Ticket(uint seed, string name) => new RunTicket {
            serverRunId = "golden-" + name, seed = seed, rules = new Rules(),
            startingProfile = new BackendProfile { values = new Profile { currency = 20 } }
        };
        static Run NewRun(RunTicket ticket) => LoopRules.Start(LoopJson.Copy(ticket.rules),
            LoopJson.Copy(ticket.startingProfile.values), ticket.seed, ticket.serverRunId);
        static RunCommandEntry Entry(Run run, int sequence, string kind, string arg) => new RunCommandEntry {
            sequence = sequence, kind = kind, arg = arg, targetId = RunCommandRules.Target(run),
            clientCommandId = run.id + ":command:" + sequence
        };
        static void Append(Run run, List<RunCommandEntry> commands, string kind, string arg)
        {
            var entry = Entry(run, commands.Count + 1, kind, arg);
            RunCommandRules.Apply(run, kind, arg); commands.Add(entry);
        }

        // Explicit authoring command, never called by a test. The caller must inspect and freeze its output.
        public static string GenerateGoldenVectors()
        {
            var file = new GoldenFile();
            file.vectors.Add(BuildVector("victory", 33, "safe"));
            file.vectors.Add(BuildVector("defeat", 51, "reckless"));
            file.vectors.Add(BuildVector("abandon-after-region", 19, "abandon"));
            file.vectors.Add(BuildVector("shop-equipment-resume", 1, "equipment"));
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), GoldenPath);
            if (File.Exists(path)) throw new InvalidOperationException("The frozen golden artifact already exists; do not regenerate it during tests.");
            File.WriteAllText(path, LoopJson.Encode(file));
            return string.Join("; ", file.vectors.Select(Summarize));
        }
        public static string InspectGoldenCandidate(uint seed, string policy)
        { return Summarize(BuildVector("candidate", seed, policy)); }
        static string Summarize(GoldenVector vector) => vector.name + ":seed=" + vector.ticket.seed + ",commands=" + vector.commands.Count +
            ",hp=" + vector.states.Last().hp + ",won=" + vector.states.Last().won + ",regions=" + vector.states.Last().completedRegions +
            ",payout=" + vector.payout + ",shopBuys=" + vector.commands.Where((c, i) => vector.states[i].phase == Phase.Shop && c.arg != "leave").Count() +
            ",die=" + vector.commands.Count(c => c.arg.StartsWith("die:")) + ",gear=" + vector.commands.Count(c => c.arg.StartsWith("gear:"));
        static GoldenVector BuildVector(string name, uint seed, string policy)
        {
            var vector = new GoldenVector { name = name, ticket = Ticket(seed, name) };
            var run = NewRun(vector.ticket); vector.states.Add(Capture(run));
            bool changedDie = false, boughtGear = false;
            for (int step = 0; run.phase != Phase.Result && step < 400; ++step)
            {
                string kind = "choose", arg = "";
                if (policy == "abandon" && run.completedRegions == 1) kind = "abandon";
                else switch (run.phase)
                {
                    case Phase.Map: kind = "travel"; break;
                    case Phase.Combat: kind = "combat"; break;
                    case Phase.LapReward: kind = "lap"; arg = "aegis"; break;
                    case Phase.Rest: arg = policy == "reckless" ? "train" : "heal"; break;
                    case Phase.Event: arg = policy == "reckless" && run.hp > 8 ? "risk" : "study"; break;
                    case Phase.Treasure:
                        if (policy == "reckless") arg = "leave";
                        else if (policy == "equipment" && !changedDie) { arg = "die:0:steel"; changedDie = true; }
                        else arg = "gear:mail";
                        break;
                    case Phase.Shop:
                        if (policy == "equipment" && !changedDie && run.gold >= 20) { arg = "die:0:steel"; changedDie = true; }
                        else if (policy == "equipment" && !boughtGear && run.gold >= 20) { arg = "gear:charm"; boughtGear = true; }
                        else arg = "leave";
                        break;
                    default: throw new InvalidOperationException("Unrecognized golden phase");
                }
                Append(run, vector.commands, kind, arg); vector.states.Add(Capture(run));
            }
            if (run.phase != Phase.Result) throw new InvalidOperationException("Golden policy did not terminate");
            vector.resumeAfter = Math.Max(1, vector.commands.Count / 2);
            vector.payout = checked(run.completedRegions * vector.ticket.rules.regionCurrency + (run.won ? vector.ticket.rules.winCurrency : 0));
            return vector;
        }
    }
}
