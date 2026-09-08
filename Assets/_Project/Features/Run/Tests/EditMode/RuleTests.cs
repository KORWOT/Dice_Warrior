using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class RuleTests
    {
        static readonly HandKind[] PriorityOrder =
        {
            HandKind.Pair, HandKind.TwoPairs, HandKind.Triple, HandKind.Straight,
            HandKind.FullHouse, HandKind.ThreePairs, HandKind.FourKind,
            HandKind.FullStraight, HandKind.FiveKind, HandKind.SixKind
        };

        [Test]
        public void All46656OrderedRollsMatchIndependentHandOracleAndPriorityCounts()
        {
            int[] expectedSupport = {45936,25950,17136,4320,7950,1800,2436,720,186,6};
            int[] expectedBest = {7200,16200,7200,3600,7500,1800,2250,720,180,6};
            var supports = new int[10];
            var bests = new int[10];
            var definitions = Hands();
            for (int code = 0; code < 46656; code++)
            {
                int remaining = code;
                var dice = new int[6];
                for (int d = 0; d < dice.Length; d++) { dice[d] = remaining % 6 + 1; remaining /= 6; }
                int expectedWinner = -1;
                for (int h = 0; h < PriorityOrder.Length; h++)
                {
                    bool expected = IndependentOracle(PriorityOrder[h], dice);
                    bool actual = DiceRules.Supports(PriorityOrder[h], dice);
                    Assert.That(actual, Is.EqualTo(expected), "hand=" + PriorityOrder[h] + " roll=" + code);
                    if (actual) supports[h]++;
                    if (expected) expectedWinner = h;
                }
                var winner = DiceRules.BestHand(dice, definitions);
                Assert.That(winner.kind, Is.EqualTo(PriorityOrder[expectedWinner]), "best roll=" + code);
                bests[Array.IndexOf(PriorityOrder, winner.kind)]++;
            }
            CollectionAssert.AreEqual(expectedSupport, supports, "Individual supports overlap.");
            CollectionAssert.AreEqual(expectedBest, bests, "Exactly one adopted best hand per roll.");
            Assert.That(bests.Sum(), Is.EqualTo(46656));
        }

        // Independent oracle uses a sorted multiplicity partition and ordered distinct string;
        // fixed combinatorial totals above additionally prevent a shared per-roll mistake.
        static bool IndependentOracle(HandKind kind, int[] dice)
        {
            string shape = string.Join("", dice.GroupBy(x => x).Select(x => x.Count()).OrderByDescending(x => x));
            string distinct = string.Join("", dice.Distinct().OrderBy(x => x));
            switch (kind)
            {
                case HandKind.Pair: return shape != "111111";
                case HandKind.TwoPairs: return new[]{"42","33","321","222","2211"}.Contains(shape);
                case HandKind.Triple: return new[]{"6","51","42","411","33","321","3111"}.Contains(shape);
                case HandKind.ThreePairs: return shape == "222";
                case HandKind.FullHouse: return new[]{"42","33","321"}.Contains(shape);
                case HandKind.FourKind: return new[]{"6","51","42","411"}.Contains(shape);
                case HandKind.Straight: return distinct.Contains("12345") || distinct.Contains("23456");
                case HandKind.FullStraight: return distinct == "123456";
                case HandKind.FiveKind: return shape == "6" || shape == "51";
                case HandKind.SixKind: return shape == "6";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        [TestCase("111222", HandKind.FullHouse)]
        [TestCase("111122", HandKind.FourKind)]
        [TestCase("111111", HandKind.SixKind)]
        [TestCase("112233", HandKind.ThreePairs)]
        [TestCase("123455", HandKind.Straight)]
        [TestCase("123466", HandKind.Pair)]
        [TestCase("123456", HandKind.FullStraight)]
        public void GddBoundaryRollsChooseExpectedHand(string digits, HandKind expected)
        {
            var dice = digits.Select(c => c - '0').ToArray();
            Assert.That(DiceRules.BestHand(dice, Hands()).kind, Is.EqualTo(expected));
            if (digits == "111111") Assert.That(DiceRules.Supports(HandKind.TwoPairs, dice), Is.False);
        }

        [Test]
        public void HighestPriorityWinsIndependentlyOfFatePowerAndDefinitionOrder()
        {
            var definitions = Hands();
            definitions.Single(x => x.kind == HandKind.Pair).priority = 100;
            definitions.Single(x => x.kind == HandKind.Pair).fatePower = 1;
            definitions.Single(x => x.kind == HandKind.SixKind).fatePower = 999;
            Array.Reverse(definitions);
            var best = DiceRules.BestHand(new[]{2,2,2,2,2,2}, definitions);
            Assert.That(best.kind, Is.EqualTo(HandKind.Pair));
            Assert.That(best.fatePower, Is.EqualTo(1));
        }

        [Test]
        public void InvalidRollsAndAmbiguousDefinitionsFailExplicitly()
        {
            Assert.Throws<ArgumentException>(() => DiceRules.Supports(HandKind.Pair, new[]{1,1}));
            Assert.Throws<ArgumentException>(() => DiceRules.Supports(HandKind.Pair, new[]{1,1,2,3,4,7}));
            var definitions = Hands();
            definitions[1].priority = definitions[0].priority;
            Assert.Throws<ArgumentException>(() => DiceRules.BestHand(new[]{1,1,2,2,3,4}, definitions));
        }

        [Test]
        public void RngHasKnownXorshift32SequenceAndRejectsZero()
        {
            uint state = 1;
            uint[] actual = Enumerable.Range(0,5).Select(_ => DiceRules.Next(ref state)).ToArray();
            CollectionAssert.AreEqual(new uint[]{270369,67634689,2647435461,307599695,2398689233}, actual);
            uint invalid = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => DiceRules.Next(ref invalid));
            Assert.That(invalid, Is.Zero);
        }

        [Test]
        public void WeightedChoiceConsumesExactlyOneSampleIncludingCertainOutcomes()
        {
            uint actual = 12345, expected = actual;
            int index = DiceRules.WeightedIndex(new float[]{0,0,1,0}, ref actual);
            DiceRules.Next(ref expected);
            Assert.That(index, Is.EqualTo(2));
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void InvalidWeightsFailBeforeConsumingRandomness()
        {
            foreach (var weights in new[]{new float[]{0,0}, new float[]{-1,2}, new[]{float.NaN,1f}, new[]{float.PositiveInfinity,1f}, Array.Empty<float>()})
            {
                uint state = 12;
                Assert.Throws<ArgumentException>(() => DiceRules.WeightedIndex(weights, ref state));
                Assert.That(state, Is.EqualTo(12));
            }
        }

        [Test]
        public void WeightedChoiceDistributionApproximatesConfiguredRatios()
        {
            uint state = 1957;
            var counts = new int[3];
            const int samples = 30000;
            for (int i = 0; i < samples; i++) counts[DiceRules.WeightedIndex(new float[]{1,2,7}, ref state)]++;
            Assert.That(counts[0] / (double)samples, Is.EqualTo(0.1).Within(0.015));
            Assert.That(counts[1] / (double)samples, Is.EqualTo(0.2).Within(0.015));
            Assert.That(counts[2] / (double)samples, Is.EqualTo(0.7).Within(0.015));
        }

        [Test]
        public void SixDiceUseEachOwnedDieFacesAndConsumeSixSamples()
        {
            var config = Fixture();
            config.dice.dice = new[]{
                new DieDefinition{id="base",values=new[]{1,2,3,4,5,6},weights=new float[]{1,1,1,1,1,1}},
                new DieDefinition{id="fixed",values=new[]{6,6,6,6,6,6},weights=new float[]{1,0,0,0,0,0}}
            };
            uint state = 77, replay = state, expected = state;
            var ids = new[]{"base","fixed","base","fixed","base","fixed"};
            int[] first = DiceRules.Roll(config, ids, ref state);
            int[] second = DiceRules.Roll(config, ids, ref replay);
            CollectionAssert.AreEqual(first, second);
            Assert.That(first.Length, Is.EqualTo(6));
            Assert.That(first[1], Is.EqualTo(6));
            Assert.That(first[3], Is.EqualTo(6));
            Assert.That(first[5], Is.EqualTo(6));
            for (int i = 0; i < 6; i++) DiceRules.Next(ref expected);
            Assert.That(state, Is.EqualTo(expected));
            Assert.That(state, Is.EqualTo(replay));
        }

        [Test]
        public void GradeUsesInclusivePowerRowsSeparateTablesAndCombatIgnoresCap()
        {
            var config = Fixture();
            config.fate.exploration = new[]{
                new GradeRow{minimumPower=1,weights=new float[]{1,0,0,0,0}},
                new GradeRow{minimumPower=5,weights=new float[]{0,0,1,0,0}}
            };
            config.fate.combat = new[]{new GradeRow{minimumPower=1,weights=new float[]{0,0,0,0,1}}};
            uint state = 25;
            Assert.That(FateCardRules.DrawGrade(config,4,false,Grade.Legendary,ref state), Is.EqualTo(Grade.Common));
            Assert.That(FateCardRules.DrawGrade(config,5,false,Grade.Legendary,ref state), Is.EqualTo(Grade.Rare));
            Assert.That(FateCardRules.DrawGrade(config,-100,false,Grade.Legendary,ref state), Is.EqualTo(Grade.Common));
            Assert.That(FateCardRules.DrawGrade(config,100,false,Grade.Legendary,ref state), Is.EqualTo(Grade.Rare));
            Assert.That(FateCardRules.DrawGrade(config,1,true,Grade.Common,ref state), Is.EqualTo(Grade.Legendary));
        }

        [Test]
        public void ExplorationCapAggregatesOverflowIntoCapInsteadOfDroppingIt()
        {
            var config = Fixture();
            config.fate.exploration[0].weights = new float[]{1,1,1,1,1};
            uint state = 9121;
            var counts = new int[5];
            const int samples = 30000;
            for (int i = 0; i < samples; i++) counts[(int)FateCardRules.DrawGrade(config,1,false,Grade.Rare,ref state)]++;
            Assert.That(counts[0] / (double)samples, Is.EqualTo(0.2).Within(0.015));
            Assert.That(counts[1] / (double)samples, Is.EqualTo(0.2).Within(0.015));
            Assert.That(counts[2] / (double)samples, Is.EqualTo(0.6).Within(0.015));
            Assert.That(counts[3] + counts[4], Is.Zero);
            CollectionAssert.AreEqual(new float[]{1,1,1,1,1}, config.fate.exploration[0].weights);
        }

        [Test]
        public void ExplorationGuaranteesSelectedTypeAndPreselectsMatchingHiddenContent()
        {
            var state = State();
            state.selectedNode.type = NodeType.Rest;
            state.config.fate.nodeWeights = new float[]{0,1,0,0,0};
            state.config.fate.exploration[0].weights = new float[]{0,0,0,0,1};
            var cards = FateCardRules.GenerateExploration(state);
            Assert.That(cards.Count, Is.EqualTo(3));
            Assert.That(cards[0].type, Is.EqualTo(NodeType.Rest));
            Assert.That(cards[1].type, Is.EqualTo(NodeType.Event));
            Assert.That(cards[2].type, Is.EqualTo(NodeType.Event));
            foreach (var card in cards)
            {
                Assert.That(card.grade, Is.EqualTo(Grade.Legendary));
                var content = state.config.Event(card.contentId);
                Assert.That(content.type, Is.EqualTo(card.type));
                Assert.That(content.grade, Is.EqualTo(card.grade));
            }
            Assert.That(cards.Select(x => x.id).Distinct().Count(), Is.EqualTo(cards.Count));
        }

        [Test]
        public void CardGradesAreIndependentAcrossSlotsAndNotGuaranteedByHighPower()
        {
            var state = State();
            state.fatePower = 10;
            state.config.fate.exploration[0].weights = new float[]{1,0,0,0,1};
            int mixed = 0, common = 0;
            var slotLegendary = new int[3];
            const int samples = 3000;
            for (int i = 0; i < samples; i++)
            {
                var cards = FateCardRules.GenerateExploration(state);
                if (cards.Select(x => x.grade).Distinct().Count() > 1) mixed++;
                if (cards.All(x => x.grade == Grade.Common)) common++;
                for (int s = 0; s < cards.Count; s++) if (cards[s].grade == Grade.Legendary) slotLegendary[s]++;
            }
            Assert.That(mixed / (double)samples, Is.EqualTo(0.75).Within(0.04));
            Assert.That(common, Is.GreaterThan(0));
            foreach (int count in slotLegendary) Assert.That(count / (double)samples, Is.EqualTo(0.5).Within(0.04));
        }

        [Test]
        public void SelectedTypeBiasChangesOnlyNonGuaranteedSlots()
        {
            var state = State();
            state.selectedNode.type = NodeType.Treasure;
            state.config.fate.nodeWeights = new float[]{1,1,1,1,1};
            state.config.fate.selectedTypeBias = 4;
            const int samples = 4000;
            int selected = 0;
            for (int i = 0; i < samples; i++)
            {
                var cards = FateCardRules.GenerateExploration(state);
                Assert.That(cards[0].type, Is.EqualTo(NodeType.Treasure));
                selected += cards.Skip(1).Count(x => x.type == NodeType.Treasure);
            }
            Assert.That(selected / (double)(samples * 2), Is.EqualTo(0.5).Within(0.035));
        }

        [Test]
        public void EmptyExplorationPoolFailsWithoutInventingAnotherTypeOrGrade()
        {
            var state = State();
            state.config.world.events = state.config.world.events.Where(x => x.type != NodeType.Rest).ToArray();
            state.selectedNode.type = NodeType.Rest;
            Assert.Throws<InvalidOperationException>(() => FateCardRules.GenerateExploration(state));
        }

        [Test]
        public void ActionExactGradePoolWinsOtherwiseAllOwnedAreUniformWithoutBias()
        {
            var state = State();
            state.config.fate.combat[0].weights = new float[]{0,0,1,0,0};
            foreach (var card in FateCardRules.GenerateActions(state))
            {
                Assert.That(card.contentId, Is.EqualTo("rare"));
                Assert.That(card.grade, Is.EqualTo(Grade.Rare));
            }
            state.config.fate.combat[0].weights = new float[]{0,0,0,0,1};
            string configBefore = JsonUtility.ToJson(state.config);
            var counts = state.actionIds.ToDictionary(x => x, _ => 0);
            const int samples = 5000;
            bool allDefend = false;
            for (int i = 0; i < samples; i++)
            {
                var cards = FateCardRules.GenerateActions(state);
                foreach (var card in cards) { Assert.That(card.grade, Is.EqualTo(Grade.Legendary)); counts[card.contentId]++; }
                if (cards.All(x => x.contentId == "defend")) allDefend = true;
            }
            foreach (int count in counts.Values) Assert.That(count / (double)(samples * 3), Is.EqualTo(0.2).Within(0.025));
            Assert.That(allDefend, Is.True, "No guaranteed attack, and duplicate action cards are legal.");
            Assert.That(JsonUtility.ToJson(state.config), Is.EqualTo(configBefore), "No mutation of original card numbers or grades.");
        }

        [Test]
        public void BothGeneratorsChangeOnlyRngAndReplaySameCardsFromSnapshot()
        {
            foreach (bool combat in new[]{false,true})
            {
                var state = State();
                string before = JsonUtility.ToJson(state);
                var replay = JsonUtility.FromJson<RunState>(before);
                uint oldRng = state.rngState;
                var first = combat ? FateCardRules.GenerateActions(state) : FateCardRules.GenerateExploration(state);
                var second = combat ? FateCardRules.GenerateActions(replay) : FateCardRules.GenerateExploration(replay);
                Assert.That(state.rngState, Is.EqualTo(replay.rngState));
                Assert.That(state.rngState, Is.Not.EqualTo(oldRng));
                CollectionAssert.AreEqual(first.Select(CardKey).ToArray(), second.Select(CardKey).ToArray());
                state.rngState = oldRng;
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before), "Generator must return cards without assigning state.cards, sequence or other state.");
            }
        }

        [Test]
        public void MissingConfigReportsErrorsWithoutThrowing()
        {
            string[] errors = null;
            Assert.DoesNotThrow(() => errors = new GameConfigData().Validate());
            Assert.That(errors, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void AuthoredDefaultsPassValidation()
        {
            var config = FateDice.Editor.PrototypeAuthoring.CreateDefaults();
            CollectionAssert.IsEmpty(config.Validate());
        }

        [TestCase("negative")]
        [TestCase("nan")]
        [TestCase("zero")]
        [TestCase("duplicate")]
        [TestCase("missing-action")]
        [TestCase("card-count")]
        public void InvalidAuthoringDataReportsErrors(string fault)
        {
            var config = FateDice.Editor.PrototypeAuthoring.CreateDefaults();
            switch (fault)
            {
                case "negative": config.fate.exploration[0].weights[0] = -1; break;
                case "nan": config.fate.combat[0].weights[0] = float.NaN; break;
                case "zero": config.fate.exploration[0].weights = new float[5]; break;
                case "duplicate": config.combat.actions[1].id = config.combat.actions[0].id; break;
                case "missing-action": config.combat.startingActionIds[0] = "missing-test-id"; break;
                case "card-count": config.world.offeredCards = 6; break;
            }
            string[] errors = null;
            Assert.DoesNotThrow(() => errors = config.Validate());
            Assert.That(errors, Is.Not.Null.And.Not.Empty, fault);
        }

        [Test]
        public void FiniteHugeNodeWeightsPreserveRelativeChoicesSourceAndRngContract()
        {
            foreach (float bias in new[]{3f,float.MaxValue})
            {
                var state = State();
                state.config = FateDice.Editor.PrototypeAuthoring.CreateDefaults();
                state.selectedNode.type = NodeType.Treasure;
                state.config.fate.nodeWeights = Enumerable.Repeat(float.MaxValue,5).ToArray();
                state.config.fate.selectedTypeBias = bias;
                CollectionAssert.IsEmpty(state.config.Validate(), "Finite relative weights are valid authoring data.");
                string before = JsonUtility.ToJson(state);
                var scaled = JsonUtility.FromJson<RunState>(before);
                scaled.config.fate.nodeWeights = new float[]{1,1,1,1,1};
                uint initialRng = state.rngState, expectedRng = initialRng;
                List<OfferedCard> actual = null;
                Assert.DoesNotThrow(() => actual = FateCardRules.GenerateExploration(state));
                var expected = FateCardRules.GenerateExploration(scaled);
                CollectionAssert.AreEqual(expected.Select(CardKey).ToArray(), actual.Select(CardKey).ToArray());
                Assert.That(actual[0].type, Is.EqualTo(NodeType.Treasure));
                for (int draw = 0; draw < 8; draw++) DiceRules.Next(ref expectedRng);
                Assert.That(state.rngState, Is.EqualTo(expectedRng), "Three offers require 2 + 3 + 3 samples.");
                Assert.That(state.rngState, Is.EqualTo(scaled.rngState));
                state.rngState = initialRng;
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before), "Neither source weights nor other run fields may change.");
                state.config.fate.nodeWeights[0] = float.PositiveInfinity;
                Assert.Throws<ArgumentException>(() => FateCardRules.GenerateExploration(state));
                Assert.That(state.rngState, Is.EqualTo(initialRng), "Invalid authoring data must not consume the run stream.");
            }
        }
        static string CardKey(OfferedCard card) => card.id + "/" + card.type + "/" + card.grade + "/" + card.contentId;

        static HandDefinition[] Hands() => PriorityOrder.Select((kind,index) =>
            new HandDefinition{kind=kind,label=kind.ToString(),priority=index,fatePower=index+1}).ToArray();

        static GameConfigData Fixture()
        {
            var events = new List<EventDefinition>();
            foreach (NodeType type in new[]{NodeType.Combat,NodeType.Event,NodeType.Treasure,NodeType.Shop,NodeType.Rest})
                foreach (Grade grade in Enum.GetValues(typeof(Grade)))
                    events.Add(new EventDefinition{id=type+"-"+grade,type=type,grade=grade,reward=new RewardDefinition()});
            return new GameConfigData
            {
                version="rule-fixture",
                dice=new DiceSettings{
                    minimumFatePower=1,maximumFatePower=10,hands=Hands(),basicDieId="base",replacementDieId="base",
                    dice=new[]{new DieDefinition{id="base",values=new[]{1,2,3,4,5,6},weights=new float[]{1,1,1,1,1,1}}}
                },
                fate=new FateSettings{
                    exploration=new[]{new GradeRow{minimumPower=1,weights=new float[]{1,1,1,1,1}}},
                    combat=new[]{new GradeRow{minimumPower=1,weights=new float[]{1,1,1,1,1}}},
                    nodeWeights=new float[]{1,1,1,1,1},selectedTypeBias=2,
                    gradeMultipliers=new float[]{1,1.2f,1.5f,2,3},capRewardMultipliers=new float[]{1,1,1,1,1}
                },
                combat=new CombatSettings{actions=new[]{
                    new ActionDefinition{id="attack",grade=Grade.Common,damageCoefficient=1},
                    new ActionDefinition{id="defend",grade=Grade.Common,blockCoefficient=1},
                    new ActionDefinition{id="heavy",grade=Grade.Common,damageCoefficient=2},
                    new ActionDefinition{id="rare",grade=Grade.Rare,damageCoefficient=2},
                    new ActionDefinition{id="trial",grade=Grade.Uncommon,damageCoefficient=1}
                }},
                world=new WorldSettings{offeredCards=3,events=events.ToArray()}
            };
        }

        static RunState State() => new RunState
        {
            runId="rule-test",sequence=7,rngState=65237,initialSeed=65237,
            config=Fixture(),selectedNode=new NodeState{id="node",type=NodeType.Combat},
            explorationCap=Grade.Legendary,fatePower=3,actionIds=new List<string>{"attack","defend","heavy","rare","trial"},
            dieIds=new[]{"base","base","base","base","base","base"}
        };
    }
}
