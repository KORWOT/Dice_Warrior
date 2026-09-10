using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FateDice.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class SeedAndPresentationTests
    {
        string directory, defaultSave;
        bool defaultExisted;
        byte[] defaultBytes;
        LocalRunStore store;

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "FateDiceSeedAndPresentationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            defaultSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            defaultExisted = File.Exists(defaultSave);
            defaultBytes = defaultExisted ? File.ReadAllBytes(defaultSave) : null;
        }

        [TearDown] public void TearDown()
        {
            try
            {
                Assert.That(File.Exists(defaultSave), Is.EqualTo(defaultExisted), "An isolated test changed the user's save existence.");
                if (defaultExisted) CollectionAssert.AreEqual(defaultBytes, File.ReadAllBytes(defaultSave));
            }
            finally
            {
                var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceSeedAndPresentationTests")) + Path.DirectorySeparatorChar;
                if (!string.IsNullOrEmpty(directory) && Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [TestCase(1u)] [TestCase(77u)] [TestCase(uint.MaxValue)]
        public void FixedSourceReusesTheExplicitSeedWithoutConsumingUnityRandom(uint seed)
        {
            ISeedSource source = new FixedSeedSource(seed);
            var before = UnityEngine.Random.state;
            for (int i = 0; i < 5; i++) Assert.That(source.NextSeed(), Is.EqualTo(seed));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(before), "Seed selection must be independent of cosmetic Unity Random.");
        }

        [Test] public void FixedSourceRejectsZero()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedSeedSource(0));
        }

        [Test] public void SystemSourceReturnsNonzeroSeedsWithoutConsumingUnityRandom()
        {
            ISeedSource source = new SystemSeedSource();
            var before = UnityEngine.Random.state;
            for (int i = 0; i < 32; i++) Assert.That(source.NextSeed(), Is.Not.Zero);
            Assert.That(UnityEngine.Random.state, Is.EqualTo(before));
            // Consecutive random seeds may legitimately match; uniqueness is deliberately not asserted.
        }

        [Test] public void FixedSeedCompatibilitySetterRejectsZeroWithoutLosingPreviousChoice()
        {
            var owner = new GameObject("RA-A isolated seed setter");
            owner.SetActive(false);
            try
            {
                var controller = owner.AddComponent<RunUIController>();
                controller.Seed = 77;
                Assert.Throws<ArgumentOutOfRangeException>(() => controller.Seed = 0);
                Assert.That(controller.Seed, Is.EqualTo(77u));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test] public void NewPresentationDefaultsAreReadableAndPhaseGroupsAreIndependent()
        {
            var style = new PresentationSettings();
            Assert.That(style.explorationDice, Is.Not.Null);
            Assert.That(style.combatDice, Is.Not.Null);
            Assert.That(style.explorationDice, Is.Not.SameAs(style.combatDice));
            AssertTiming(style.DiceTiming(false), .65f, .9f);
            AssertTiming(style.DiceTiming(true), .65f, .9f);
            style.explorationDice.rollSeconds = .12f;
            style.explorationDice.resultHoldSeconds = .34f;
            AssertTiming(style.DiceTiming(false), .12f, .34f);
            AssertTiming(style.DiceTiming(true), .65f, .9f);
        }

        [Test] public void ExplicitZeroTimingsAreValidAndAreNotRaisedToLegacyMinimums()
        {
            var data = PrototypeAuthoring.CreateDefaults();
            data.presentation.rollSeconds = 1.75f;
            data.presentation.explorationDice = Timing(0, 0);
            data.presentation.combatDice = Timing(0, 0);
            Assert.That(data.Validate(), Is.Empty);
            AssertTiming(data.presentation.DiceTiming(false), 0, 0);
            AssertTiming(data.presentation.DiceTiming(true), 0, 0);
            var run = RunSession.New(data, 33, data.combat.trialActionIds[0], Grade.Legendary);
            store.Save(run.State);
            AssertTiming(store.Load().config.presentation.DiceTiming(false), 0, 0);
            AssertTiming(store.Load().config.presentation.DiceTiming(true), 0, 0);
        }

        static IEnumerable<TestCaseData> InvalidTimings()
        {
            foreach (bool combat in new[] { false, true })
            foreach (bool hold in new[] { false, true })
            foreach (float value in new[] { -.01f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                yield return new TestCaseData(combat, hold, value);
        }

        [TestCaseSource(nameof(InvalidTimings))]
        public void InvalidPhaseTimingIsRejectedBeforeANewRunCanStart(bool combat, bool hold, float value)
        {
            var data = PrototypeAuthoring.CreateDefaults();
            data.presentation.explorationDice = Timing(.65f, .9f);
            data.presentation.combatDice = Timing(.65f, .9f);
            var group = combat ? data.presentation.combatDice : data.presentation.explorationDice;
            if (hold) group.resultHoldSeconds = value; else group.rollSeconds = value;
            Assert.That(data.Validate(), Is.Not.Empty, "Invalid phase timing must be a configuration error.");
            Assert.Throws<InvalidOperationException>(() => RunSession.New(data, 33, data.combat.trialActionIds[0], Grade.Legendary));
            Assert.That(store.Exists, Is.False);
        }

        [TestCase(.2f, .65f)] [TestCase(1.25f, 1.25f)]
        public void MissingLegacyGroupsRestoreEffectiveTimingsFromTheSavedSnapshot(float legacyRoll, float effectiveRoll)
        {
            var data = PrototypeAuthoring.CreateDefaults();
            data.presentation.rollSeconds = legacyRoll;
            var run = RunSession.New(data, 33, data.combat.trialActionIds[0], Grade.Legendary);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            Assert.That(run.Roll(), Is.True);
            string payload = JsonUtility.ToJson(run.State);
            payload = Regex.Replace(payload, @",?""explorationDice"":\{[^{}]*\}", "");
            payload = Regex.Replace(payload, @",?""combatDice"":\{[^{}]*\}", "");
            Assert.That(payload, Does.Not.Contain("\"explorationDice\""), "The legacy fixture must omit the field itself.");
            Assert.That(payload, Does.Not.Contain("\"combatDice\""), "The legacy fixture must omit the field itself.");
            File.WriteAllText(store.Path, Envelope(payload), new UTF8Encoding(false));
            byte[] original = File.ReadAllBytes(store.Path);
            var loaded = store.Load();
            AssertTiming(loaded.config.presentation.DiceTiming(false), effectiveRoll, .9f);
            AssertTiming(loaded.config.presentation.DiceTiming(true), effectiveRoll, .9f);
            Assert.That(loaded.initialSeed, Is.EqualTo(run.State.initialSeed));
            Assert.That(loaded.rngState, Is.EqualTo(run.State.rngState));
            AssertSameRules(run.State, loaded);
            CollectionAssert.AreEqual(run.State.cards.Select(c => c.id), loaded.cards.Select(c => c.id));
            CollectionAssert.AreEqual(run.State.dice, loaded.dice);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(store.Path), "Load must not rewrite a legacy checkpoint.");
            store.Save(loaded);
            AssertTiming(store.Load().config.presentation.DiceTiming(false), effectiveRoll, .9f);
            AssertTiming(store.Load().config.presentation.DiceTiming(true), effectiveRoll, .9f);
        }

        [TestCase(false)] [TestCase(true)]
        public void OnlyTheMissingPhaseUsesLegacyFallback(bool missingCombat)
        {
            var data = PrototypeAuthoring.CreateDefaults();
            data.presentation.rollSeconds = 1.25f;
            data.presentation.explorationDice = missingCombat ? Timing(0, .23f) : null;
            data.presentation.combatDice = missingCombat ? null : Timing(.17f, 0);
            Assert.That(data.Validate(), Is.Empty);
            AssertTiming(data.presentation.DiceTiming(missingCombat), 1.25f, .9f);
            AssertTiming(data.presentation.DiceTiming(!missingCombat), missingCombat ? 0 : .17f, missingCombat ? .23f : 0);
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingTimingSurvivesCopySnapshotAndNewRunWithoutAliasing(bool missingCombat)
        {
            var source = PrototypeAuthoring.CreateDefaults();
            source.presentation.rollSeconds = 1.25f;
            source.presentation.explorationDice = missingCombat ? Timing(0, .23f) : null;
            source.presentation.combatDice = missingCombat ? null : Timing(.17f, 0);
            var definition = ScriptableObject.CreateInstance<FateDiceConfig>();
            definition.data = source;
            try
            {
                var copies = new[] { source.DeepCopy(), definition.Snapshot(),
                    RunSession.New(source, 33, source.combat.trialActionIds[0], Grade.Legendary).State.config };
                foreach (var copy in copies)
                {
                    AssertTiming(copy.presentation.DiceTiming(missingCombat), 1.25f, .9f);
                    AssertTiming(copy.presentation.DiceTiming(!missingCombat), missingCombat ? 0 : .17f, missingCombat ? .23f : 0);
                    copy.presentation.DiceTiming(missingCombat).rollSeconds = .4f;
                    copy.presentation.DiceTiming(!missingCombat).rollSeconds = .5f;
                }
                Assert.That(missingCombat ? source.presentation.combatDice : source.presentation.explorationDice, Is.Null);
                AssertTiming(source.presentation.DiceTiming(missingCombat), 1.25f, .9f);
                AssertTiming(source.presentation.DiceTiming(!missingCombat), missingCombat ? 0 : .17f, missingCombat ? .23f : 0);
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test] public void NewGroupsRoundTripAndSourceEditsCannotReplaceTheSavedPresentation()
        {
            var source = PrototypeAuthoring.CreateDefaults();
            source.presentation.explorationDice = Timing(.13f, .27f);
            source.presentation.combatDice = Timing(.41f, .53f);
            var run = RunSession.New(source, 33, source.combat.trialActionIds[0], Grade.Legendary);
            store.Save(run.State);
            var bytes = File.ReadAllBytes(store.Path);
            source.presentation.explorationDice.rollSeconds = 1.1f;
            source.presentation.combatDice.resultHoldSeconds = 1.3f;
            for (int i = 0; i < 3; i++)
            {
                var loaded = store.Load();
                AssertTiming(loaded.config.presentation.DiceTiming(false), .13f, .27f);
                AssertTiming(loaded.config.presentation.DiceTiming(true), .41f, .53f);
                Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(JsonUtility.ToJson(run.State)));
            }
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            var fresh = RunSession.New(source, 33, source.combat.trialActionIds[0], Grade.Legendary);
            AssertTiming(fresh.State.config.presentation.DiceTiming(false), 1.1f, .27f);
            AssertTiming(fresh.State.config.presentation.DiceTiming(true), .41f, 1.3f);
        }

        [Test] public void TimingChangesPreserveTheCompleteSeededCommandSequenceAndSavedRules()
        {
            var firstConfig = PrototypeAuthoring.CreateDefaults();
            firstConfig.world.eventsToBoss = 1;
            firstConfig.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            firstConfig.growth.startingMaxHp = 1000;
            firstConfig.growth.startingPower = 1000;
            firstConfig.presentation.explorationDice = Timing(0, 0);
            firstConfig.presentation.combatDice = Timing(0, 0);
            foreach (var encounter in firstConfig.world.events)
                encounter.reward.rerollCharges = firstConfig.growth.rerollCost * 2;
            var secondConfig = firstConfig.DeepCopy();
            secondConfig.presentation.explorationDice = Timing(.17f, 1.1f);
            secondConfig.presentation.combatDice = Timing(1.3f, .29f);
            var firstState = RunSession.New(firstConfig, 88, firstConfig.combat.trialActionIds[0], Grade.Legendary).State;
            // Preserve the exact origin and every generated ID. Only timing differs in the replay.
            var secondState = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(firstState));
            secondState.config.presentation = secondConfig.presentation;
            // Prepare detached inputs; actual Reroll still enforces and spends its configured cost.
            firstState.rerollUnlocked = secondState.rerollUnlocked = true;
            firstState.rerollCharges = secondState.rerollCharges = firstConfig.growth.rerollCost * 2;
            var first = new RunSession(firstState);
            var second = new RunSession(secondState);
            var secondStore = new LocalRunStore(Path.Combine(directory, "slow.json"));
            first.Checkpoint = store.Save;
            second.Checkpoint = secondStore.Save;
            store.Save(first.State); secondStore.Save(second.State);
            AssertSameRules(first.State, second.State);
            var boundaries = new HashSet<RunPhase> { first.State.phase };

            void Step(Func<RunSession, bool> command)
            {
                Assert.That(command(first), Is.True);
                Assert.That(command(second), Is.True, "The second run must accept the identical command and offer ID.");
                AssertSameRules(first.State, second.State);
                Assert.That(JsonUtility.ToJson(store.Load()), Is.EqualTo(JsonUtility.ToJson(first.State)));
                Assert.That(JsonUtility.ToJson(secondStore.Load()), Is.EqualTo(JsonUtility.ToJson(second.State)));
                boundaries.Add(first.State.phase);
                first = new RunSession(store.Load()) { Checkpoint = store.Save };
                second = new RunSession(secondStore.Load()) { Checkpoint = secondStore.Save };
            }

            var nodeId = first.State.availableNodeIds[0]; Step(r => r.ChooseNode(nodeId));
            Step(r => r.Roll()); Step(r => r.Reroll(2));
            string fateId = first.State.cards.First(c => c.type == NodeType.Combat).id;
            Step(r => r.ChooseFate(fateId));
            bool checkedCombatReroll = false;
            for (int guard = 0; guard < 120 && first.State.phase != RunPhase.Result; guard++)
            {
                switch (first.State.phase)
                {
                    case RunPhase.Map:
                        string bossId = first.State.availableNodeIds[0]; Step(r => r.ChooseNode(bossId)); break;
                    case RunPhase.CombatRoll:
                        Step(r => r.Roll());
                        if (!checkedCombatReroll) { Step(r => r.Reroll(4)); checkedCombatReroll = true; }
                        break;
                    case RunPhase.CombatCards:
                        string actionId = first.State.cards.OrderByDescending(c => CombatRules.Evaluate(first.State, c).damage).First().id;
                        Step(r => r.ChooseAction(actionId)); break;
                    case RunPhase.Reward: Step(r => r.ClaimReward()); break;
                    case RunPhase.EquipmentChoice:
                        if (!string.IsNullOrEmpty(first.State.pendingEquipmentId)) Step(r => r.Equip(false));
                        else Step(r => r.ReplaceDie(-1));
                        break;
                    default: Assert.Fail("Unexpected phase in the recorded combat journey: " + first.State.phase); break;
                }
            }
            Assert.That(checkedCombatReroll, Is.True);
            Assert.That(first.State.phase, Is.EqualTo(RunPhase.Result));
            Assert.That(first.State.won, Is.True);
            Assert.That(first.State.eventsResolved, Is.EqualTo(1));
            Assert.That(boundaries, Does.Contain(RunPhase.ExplorationCards));
            Assert.That(boundaries, Does.Contain(RunPhase.CombatCards));
            Assert.That(boundaries, Does.Contain(RunPhase.Reward));
            AssertSameRules(first.State, second.State);
        }

        static RollPresentationSettings Timing(float roll, float hold) => new RollPresentationSettings { rollSeconds = roll, resultHoldSeconds = hold };
        static void AssertTiming(RollPresentationSettings timing, float roll, float hold)
        {
            Assert.That(timing, Is.Not.Null);
            Assert.That(timing.rollSeconds, Is.EqualTo(roll));
            Assert.That(timing.resultHoldSeconds, Is.EqualTo(hold));
        }
        static string RulesJson(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            copy.config.presentation = null; // Every ID, including the run identity, remains exact in this replay.
            return JsonUtility.ToJson(copy);
        }
        static void AssertSameRules(RunState first, RunState second) =>
            Assert.That(RulesJson(second), Is.EqualTo(RulesJson(first)), "Timing changed a rule, ID, offer, RNG state, reward, or progress field.");
        static string Envelope(string payload)
        {
            string checksum;
            using (var sha = SHA256.Create()) checksum = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
            return JsonUtility.ToJson(new LocalSaveEnvelope { schema = 1, payload = payload, checksum = checksum });
        }
    }
}
