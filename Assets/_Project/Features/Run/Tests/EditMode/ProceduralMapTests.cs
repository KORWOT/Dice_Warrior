using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FateDice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    // Reflection keeps the new public data contract executable before its production fields exist.
    public sealed class ProceduralMapTests
    {
        static FieldInfo Field(Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, type.Name + "." + name + " must preserve the versioned map contract.");
            return field;
        }
        static int Read(object value, string name) => (int)Field(value.GetType(), name).GetValue(value);
        static void Set(object value, string name, int number) => Field(value.GetType(), name).SetValue(value, number);
        static int Floor(NodeState node) => Read(node, "floor");
        static int Lane(NodeState node) => Read(node, "lane");

        static GameConfigData Config(int floors = 10, int columns = 5, int paths = 5)
        {
            var data = PrototypeAuthoring.CreateDefaults();
            data.world.eventsToBoss = floors;
            Set(data.world, "mapGenerationVersion", 1);
            Set(data.world, "mapColumns", columns);
            Set(data.world, "mapPathCount", paths);
            return data;
        }
        static RunSession New(uint seed = 41, GameConfigData config = null) =>
            RunSession.New(config ?? Config(), seed, "fireball", Grade.Common);

        static string Shape(RunStateData state)
        {
            var all = state.nodes.Concat(state.nodeHistory).ToDictionary(n => n.id);
            return string.Join("|", all.Values.OrderBy(Floor).ThenBy(Lane).Select(n =>
                Floor(n) + ":" + Lane(n) + ":" + n.type + ">" +
                string.Join(",", n.childIds.Select(id => all[id]).OrderBy(Lane).Select(c => Floor(c) + ":" + Lane(c)))));
        }
        static void AssertGraph(RunState state, int floors, int columns)
        {
            var all = state.nodes.Concat(state.nodeHistory).ToDictionary(n => n.id);
            Assert.That(all.Values.Count(n => n.type == NodeType.Boss), Is.EqualTo(1));
            var boss = all.Values.Single(n => n.type == NodeType.Boss);
            Assert.That(Floor(boss), Is.EqualTo(floors + 1));
            Assert.That(boss.childIds, Is.Empty);
            Assert.That(all.Values.Select(Floor).Distinct().OrderBy(f => f), Is.EqualTo(Enumerable.Range(1, floors + 1)));
            Assert.That(all.Values.Select(n => Floor(n) + ":" + Lane(n)).Distinct().Count(), Is.EqualTo(all.Count));
            foreach (var node in all.Values)
            {
                Assert.That(Lane(node), Is.InRange(0, columns - 1));
                Assert.That(node.childIds.Distinct().Count(), Is.EqualTo(node.childIds.Count));
                if (node.type != NodeType.Boss) Assert.That(node.childIds.Count, Is.InRange(1, 3));
                foreach (var childId in node.childIds)
                {
                    Assert.That(all.ContainsKey(childId), Is.True);
                    var child = all[childId];
                    Assert.That(Floor(child), Is.EqualTo(Floor(node) + 1));
                    if (child.type != NodeType.Boss) Assert.That(Math.Abs(Lane(child) - Lane(node)), Is.LessThanOrEqualTo(1));
                }
                var seen = new HashSet<string>();
                var pending = new Stack<string>(); pending.Push(node.id);
                while (pending.Count > 0)
                {
                    var id = pending.Pop();
                    if (seen.Add(id)) foreach (var child in all[id].childIds) pending.Push(child);
                }
                Assert.That(seen, Does.Contain(boss.id), "Every generated node must lead to the sole boss.");
            }
            foreach (var layer in all.Values.GroupBy(Floor))
                foreach (var left in layer)
                    foreach (var right in layer.Where(n => Lane(n) > Lane(left)))
                        foreach (var leftChild in left.childIds)
                            foreach (var rightChild in right.childIds)
                                Assert.That(Lane(all[leftChild]), Is.LessThanOrEqualTo(Lane(all[rightChild])), "Map edges must not cross between distinct lanes.");
            RunStateValidator.Validate(state);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(10)] [TestCase(100)]
        public void EverySupportedFloorBoundaryHasCompleteConnectedMapAndOneBoss(int floors)
        {
            for (uint seed = 1; seed <= 8; seed++) AssertGraph(New(seed, Config(floors)).State, floors, 5);
        }

        [Test]
        public void SeedsCreateVariableRootsBranchesAndMergesWithoutCrossings()
        {
            var rootCounts = new HashSet<int>(); var childCounts = new HashSet<int>();
            var shapes = new HashSet<string>(); var mergedBeforeBoss = false;
            for (uint seed = 1; seed <= 64; seed++)
            {
                var state = New(seed).State; AssertGraph(state, 10, 5);
                rootCounts.Add(state.availableNodeIds.Count); shapes.Add(Shape(state));
                foreach (var node in state.nodes.Where(n => n.type != NodeType.Boss)) childCounts.Add(node.childIds.Count);
                mergedBeforeBoss |= state.nodes.Where(n => n.type != NodeType.Boss)
                    .Any(n => state.nodes.Count(parent => parent.childIds.Contains(n.id)) > 1);
            }
            Assert.That(rootCounts.Count, Is.GreaterThan(1), "The start must not always recreate three fixed choices.");
            Assert.That(childCounts, Does.Contain(1)); Assert.That(childCounts.Any(c => c > 1), Is.True);
            Assert.That(mergedBeforeBoss, Is.True); Assert.That(shapes.Count, Is.GreaterThan(32));
        }

        [Test]
        public void SameSeedAndSavedRulesReproduceGraphWithoutConsumingGameplayRandom()
        {
            var source = Config(); var first = New(7523, source); var saved = first.State;
            Assert.That(saved.rngState, Is.EqualTo(7523u));
            Set(source.world, "mapColumns", 2); Set(source.world, "mapPathCount", 2);
            Assert.That(Read(saved.config.world, "mapColumns"), Is.EqualTo(5));
            Assert.That(Shape(New(7523, saved.config).State), Is.EqualTo(Shape(saved)));
            var expectedRng = 7523u;
            var expectedDice = DiceRules.Roll(saved.config, saved.dieIds, ref expectedRng);
            Assert.That(first.ChooseNode(saved.availableNodeIds[0]), Is.True);
            Assert.That(first.State.rngState, Is.EqualTo(7523u));
            Assert.That(first.Roll(), Is.True); Assert.That(first.State.dice, Is.EqualTo(expectedDice));
        }

        [Test]
        public void MapWidthChangesDoNotShiftTheFirstGameplayRoll()
        {
            var first = New(829, Config(10, 2, 2)); var second = New(829, Config(10, 7, 12));
            first.ChooseNode(first.State.availableNodeIds[0]); second.ChooseNode(second.State.availableNodeIds[0]);
            first.Roll(); second.Roll(); Assert.That(first.State.dice, Is.EqualTo(second.State.dice));
        }

        [Test]
        public void TenNormalResolutionsReachOneExistingBossWithoutRegeneratingTheMap()
        {
            var config = Config(); config.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 };
            var run = New(91, config); var original = run.State; var shape = Shape(original);
            var bossId = original.nodes.Single(n => n.type == NodeType.Boss).id; var nextId = original.nextNodeId;
            for (int floor = 1; floor <= 10; floor++)
            {
                Assert.That(run.State.availableNodeIds.All(id => Floor(run.State.nodes.Single(n => n.id == id)) == floor), Is.True);
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
                Assert.That(Floor(run.State.selectedNode), Is.EqualTo(floor));
                Assert.That(run.Roll(), Is.True); Assert.That(run.ChooseFate(run.State.cards[0].id), Is.True);
                Assert.That(run.ResolveEncounter(false), Is.True); Assert.That(run.ClaimReward(), Is.True);
                Assert.That(run.State.eventsResolved, Is.EqualTo(floor));
                Assert.That(run.State.nextNodeId, Is.EqualTo(nextId)); Assert.That(Shape(run.State), Is.EqualTo(shape));
            }
            Assert.That(run.State.availableNodeIds, Is.EqualTo(new[] { bossId }));
            Assert.That(run.ChooseNode(bossId), Is.True); Assert.That(run.State.phase, Is.EqualTo(RunPhase.CombatRoll));
            Assert.That(run.State.eventsResolved, Is.EqualTo(10));
        }

        [Test]
        public void IllegalPathAndFailedCheckpointPreserveEntireGraphAndRng()
        {
            var run = New(); var before = JsonUtility.ToJson(run.State); var writes = 0;
            var distant = run.State.nodes.First(n => Floor(n) == 3).id;
            run.Checkpoint = _ => { writes++; throw new IOException("Checkpoint rejected by test."); };
            Assert.That(run.ChooseNode(distant), Is.False); Assert.That(writes, Is.Zero);
            Assert.Throws<IOException>(() => run.ChooseNode(run.State.availableNodeIds[0]));
            Assert.That(writes, Is.EqualTo(1)); Assert.That(JsonUtility.ToJson(run.State), Is.EqualTo(before));
        }

        [Test]
        public void DeepCopyPreservesCoordinatesRulesAndIndependentChildren()
        {
            var original = New().State; var copy = original.DeepCopy(); var core = original.ToCore();
            Assert.That(Shape(copy), Is.EqualTo(Shape(original))); Assert.That(Shape(core), Is.EqualTo(Shape(original)));
            Set(copy.nodes[0], "lane", 99); copy.nodes[0].childIds.Clear(); Set(copy.config.world, "mapColumns", 7);
            Assert.That(Lane(original.nodes[0]), Is.LessThan(5)); Assert.That(original.nodes[0].childIds, Is.Not.Empty);
            Assert.That(Read(original.config.world, "mapColumns"), Is.EqualTo(5));
        }

        [TestCase("mapGenerationVersion", 2)] [TestCase("mapGenerationVersion", -1)]
        [TestCase("mapColumns", 1)] [TestCase("mapColumns", 8)]
        [TestCase("mapPathCount", 1)] [TestCase("mapPathCount", 13)]
        public void UnsupportedGenerationSettingsAreRejectedBeforeCreatingRun(string field, int value)
        {
            var config = Config(); Set(config.world, field, value);
            Assert.That(config.Validate(), Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => New(41, config));
        }

        static RunState SmallMap()
        {
            var state = New(41, Config(2, 2, 2)).State;
            Func<string, int, int, string[], NodeState> node = (id, floor, lane, children) =>
            {
                var result = new NodeState { id = id, type = floor == 3 ? NodeType.Boss : NodeType.Rest, childIds = children.ToList() };
                Set(result, "floor", floor); Set(result, "lane", lane); return result;
            };
            state.nodes = new List<NodeState> { node("A",1,0,new[]{"C"}), node("B",1,1,new[]{"D"}),
                node("C",2,0,new[]{"Z"}), node("D",2,1,new[]{"Z"}), node("Z",3,0,Array.Empty<string>()) };
            state.availableNodeIds = new List<string> { "A", "B" }; state.nodeHistory.Clear(); state.nextNodeId = 6;
            RunStateValidator.Validate(state); return state;
        }

        [TestCase("crossing")] [TestCase("floor")] [TestCase("lane")] [TestCase("boss")]
        [TestCase("dead-end")] [TestCase("progress")] [TestCase("selected-copy")]
        public void InvalidStoredTopologyAndProgressCannotEnterASession(string defect)
        {
            var state = SmallMap();
            if (defect == "crossing") { state.nodes[0].childIds[0] = "D"; state.nodes[1].childIds[0] = "C"; }
            if (defect == "floor") Set(state.nodes[2], "floor", 1);
            if (defect == "lane") Set(state.nodes[2], "lane", 2);
            if (defect == "boss") state.nodes[2].type = NodeType.Boss;
            if (defect == "dead-end") state.nodes[2].childIds.Clear();
            if (defect == "progress") { state.eventsResolved = 1; state.resolvedEventIds.Add("archived-normal"); }
            if (defect == "selected-copy")
            {
                var run = new RunSession(state); Assert.That(run.ChooseNode("A"), Is.True); state = run.State;
                Set(state.selectedNode, "lane", 1);
            }
            Assert.Throws<RunStateValidationException>(() => RunStateValidator.Validate(state));
        }

        [Test]
        public void PruningRetainsSharedFutureAndArchivesOtherBranchesWithStableCoordinates()
        {
            var state = SmallMap(); state.nodes[0].childIds.Add("D");
            var run = new RunSession(state); var before = Shape(state);
            Assert.That(run.ChooseNode("A"), Is.True);
            Assert.That(run.State.nodes.Select(n => n.id), Is.EquivalentTo(new[] { "A", "C", "D", "Z" }));
            Assert.That(run.State.nodeHistory.Select(n => n.id), Is.EquivalentTo(new[] { "B" }));
            Assert.That(Shape(run.State), Is.EqualTo(before));
        }

        [Test]
        public void CheckpointsPreserveGeneratedMapAndFixedOffersAcrossDiskResume()
        {
            WithStore(store =>
            {
                var run = New(); store.Save(run.State); var shape = Shape(run.State);
                run = new RunSession(store.Load()) { Checkpoint = store.Save };
                run.ChooseNode(run.State.availableNodeIds[0]); run = new RunSession(store.Load()) { Checkpoint = store.Save };
                run.Roll(); var expected = JsonUtility.ToJson(run.State); var bytes = File.ReadAllBytes(store.Path);
                var loaded = store.Load(); Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(expected));
                Assert.That(Shape(loaded), Is.EqualTo(shape)); Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
                var resumed = new RunSession(loaded); var continuous = new RunSession(run.State);
                var card = loaded.cards[0].id; resumed.ChooseFate(card); continuous.ChooseFate(card);
                Assert.That(JsonUtility.ToJson(resumed.State), Is.EqualTo(JsonUtility.ToJson(continuous.State)));
            });
        }

        [Test]
        public void SchemaOneWithoutMapFieldsLoadsAndContinuesOriginalLegacyGenerator()
        {
            WithStore(store =>
            {
                var config = PrototypeAuthoring.CreateDefaults();
                Assert.That(Read(config.world, "mapGenerationVersion"), Is.Zero);
                config.world.eventsToBoss = 2; config.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 };
                var run = New(33, config); var state = run.State;
                var payload = Regex.Replace(JsonUtility.ToJson(state), "\\\"(?:mapGenerationVersion|mapColumns|mapPathCount|floor|lane)\\\":-?\\d+,?", "");
                // All current optional map fields precede another serialized field; protect trailing-list punctuation too.
                payload = payload.Replace(",}", "}");
                string checksum;
                using (var sha = SHA256.Create()) checksum = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
                File.WriteAllText(store.Path, JsonUtility.ToJson(new LocalSaveEnvelope { schema = 1, payload = payload, checksum = checksum }));
                var bytes = File.ReadAllBytes(store.Path); var loaded = store.Load();
                Assert.That(Read(loaded.config.world, "mapGenerationVersion"), Is.Zero);
                Assert.That(loaded.rngState, Is.EqualTo(state.rngState)); Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
                var resumed = new RunSession(loaded) { Checkpoint = store.Save };
                resumed.ChooseNode(resumed.State.availableNodeIds[0]); resumed.Roll(); resumed.ChooseFate(resumed.State.cards[0].id);
                resumed.ResolveEncounter(false); resumed.ClaimReward();
                Assert.That(resumed.State.availableNodeIds.Count, Is.EqualTo(config.world.branchCount));
                Assert.That(Read(resumed.State.config.world, "mapGenerationVersion"), Is.Zero);
                Assert.That(resumed.State.nextNodeId, Is.GreaterThan(state.nextNodeId));
            });
        }

        static void WithStore(Action<LocalRunStore> body)
        {
            var directory = Path.Combine(Path.GetTempPath(), "FateDiceProceduralMapTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { body(new LocalRunStore(Path.Combine(directory, "run.json"))); }
            finally { Directory.Delete(directory, true); }
        }
    }
}
