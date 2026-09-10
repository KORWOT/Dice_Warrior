using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class DiceDistributionTests
    {
        // Declared before any production RNG sample. Do not fit these bounds to observed counts.
        const int SamplesPerSeed = 62500;
        const int SamplesPerConfiguration = 500000;
        const int OfferedSlots = 3;
        const int FamilyComparisons = 340; // 4 * (10 hands + 9 powers + 36 faces + 30 slot grades).
        const double FamilyAlpha = 0.0001;
        static readonly uint[] Seeds = { 1u, 33u, 88u, 1957u, 2654435769u, 305419896u, 3735928559u, 2463534242u };
        static readonly string[] GradeNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        JObject fixture;
        GameConfigData config;

        [SetUp]
        public void LoadFixedExpectationsAndCheckActualAuthoredInputs()
        {
            string path = Path.Combine(Application.dataPath,
                "_Project/Features/Run/Tests/EditMode/Fixtures/DiceDistributionExpected.json");
            fixture = JObject.Parse(File.ReadAllText(path));
            Assert.That((int)fixture["schema"], Is.EqualTo(1));
            var sampling = fixture["sampling"];
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 6 }, sampling["replacementCounts"].ToObject<int[]>());
            CollectionAssert.AreEqual(Seeds, sampling["seeds"].ToObject<uint[]>());
            Assert.That((int)sampling["samplesPerSeed"], Is.EqualTo(SamplesPerSeed));
            Assert.That((int)sampling["samplesPerConfiguration"], Is.EqualTo(SamplesPerConfiguration));
            Assert.That((int)sampling["totalRollSamples"], Is.EqualTo(2000000));
            Assert.That((int)sampling["offeredSlots"], Is.EqualTo(OfferedSlots));
            Assert.That((int)sampling["familyComparisons"], Is.EqualTo(FamilyComparisons));
            Assert.That((double)sampling["familyAlpha"], Is.EqualTo(FamilyAlpha));
            CollectionAssert.AreEqual(GradeNames, Enum.GetNames(typeof(Grade)));
            var asset = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            Assert.That(asset, Is.Not.Null, "The distribution must sample the real authored config.");
            config = asset.Snapshot();
            AssertAuthoredInputs(fixture["inputs"]);
        }

        [Test]
        public void FrozenOracleRetainsKnownCountsAndOneEmberMeanDecrease()
        {
            var baseline = Case(0);
            CollectionAssert.AreEqual(new long[] { 7200, 16200, 7200, 3600, 7500, 1800, 2250, 720, 180, 6 },
                baseline["handMasses"].ToObject<long[]>());
            Assert.That((long)baseline["integerDenominator"], Is.EqualTo(46656));
            foreach (var expected in fixture["cases"])
            {
                long denominator = (long)expected["integerDenominator"];
                long[] hands = expected["handMasses"].ToObject<long[]>();
                long[] powers = expected["fatePowerMasses"].ToObject<long[]>();
                Assert.That(hands.Length, Is.EqualTo(10));
                Assert.That(powers.Length, Is.EqualTo(9));
                Assert.That(hands.Sum(), Is.EqualTo(denominator));
                Assert.That(powers.Sum(), Is.EqualTo(denominator));
                Assert.That(hands.All(x => x > 0) && powers.All(x => x > 0), Is.True);
                Assert.That(Probabilities(expected["explorationGradeProbabilities"]).Sum(), Is.EqualTo(1).Within(1e-14));
                Assert.That(Probabilities(expected["combatGradeProbabilities"]).Sum(), Is.EqualTo(1).Within(1e-14));
                double mean = powers.Select((mass, index) => (index + 1) * (double)mass / denominator).Sum();
                Assert.That(mean, Is.EqualTo(Ratio(expected["meanFatePower"])).Within(1e-14));
            }
            double delta = Ratio(Case(1)["meanFatePower"]) - Ratio(baseline["meanFatePower"]);
            Assert.That(delta, Is.EqualTo(-5.0 / 324).Within(1e-14));
            Assert.That(Ratio(Case(1)["meanFaceSum"]), Is.GreaterThan(Ratio(baseline["meanFaceSum"])));
            double sixKindProbability = 1.0 / 7776;
            Assert.That(CountTolerance(sixKindProbability), Is.LessThan(SamplesPerConfiguration * sixKindProbability),
                "The predeclared sample must reject zero observed base SixKind outcomes.");
            TestContext.WriteLine("FIXED THEORY: one Ember changes E[fate power] by -5/324, although E[face sum] rises. " +
                "Common shop: 12 gold / 3 charges; reroll costs 1 charge; Ember costs 16 gold. " +
                "Rare+ is a per-slot marginal, not an optimal-strategy or win-rate claim.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(6)]
        public void ActualGameRngMatchesFrozenIndependentDistribution(int replacementCount)
        {
            var expected = Case(replacementCount);
            string[] dieIds = expected["dieIds"].ToObject<string[]>();
            Assert.That(dieIds.Length, Is.EqualTo(6));
            for (int slot = 0; slot < dieIds.Length; slot++)
                Assert.That(dieIds[slot], Is.EqualTo(slot < replacementCount ? "ember" : "plain"));

            var hands = new int[10]; // Indexed by actual HandKind enum; fixture declares its independent mapping.
            var powers = new int[9];
            var faces = new int[6, 6];
            var exploration = new int[OfferedSlots, 5];
            var combat = new int[OfferedSlots, 5];
            var streams = new JArray();
            long powerSum = 0, faceSum = 0;
            int samples = 0;
            string startedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var elapsed = Stopwatch.StartNew();
            // Fixed finite work. No NUnit, JSON, independent classification or per-roll oracle work in the hot loop.
            foreach (uint seed in Seeds)
            {
                uint rng = seed;
                for (int sample = 0; sample < SamplesPerSeed; sample++)
                {
                    int[] rolled = DiceRules.Roll(config, dieIds, ref rng);
                    for (int slot = 0; slot < 6; slot++)
                    {
                        faces[slot, rolled[slot] - 1]++;
                        faceSum += rolled[slot];
                    }
                    HandDefinition adopted = DiceRules.BestHand(rolled, config.dice.hands);
                    int power = adopted.fatePower; // Same zero-bonus value assigned by RunApplication.RefreshOffers.
                    hands[(int)adopted.kind]++;
                    powers[power - 1]++;
                    powerSum += power;
                    // Each slot keeps its own N, rather than pooling correlated slots as 3N independent trials.
                    for (int slot = 0; slot < OfferedSlots; slot++)
                        exploration[slot, (int)FateCardRules.DrawGrade(config, power, false, Grade.Legendary, ref rng)]++;
                    for (int slot = 0; slot < OfferedSlots; slot++)
                        combat[slot, (int)FateCardRules.DrawGrade(config, power, true, Grade.Common, ref rng)]++;
                    samples++;
                }
                streams.Add(new JObject { ["seed"] = seed, ["samples"] = SamplesPerSeed, ["finalRngState"] = rng });
            }
            elapsed.Stop();

            var comparisons = new JArray();
            var failures = new List<string>();
            long denominator = (long)expected["integerDenominator"];
            for (int h = 0; h < 10; h++)
            {
                var definition = fixture["inputs"]["hands"][h];
                Compare(comparisons, failures, "hand/" + (string)definition["kind"],
                    hands[(int)definition["enumValue"]], (double)expected["handMasses"][h] / denominator);
            }
            for (int power = 1; power <= 9; power++)
                Compare(comparisons, failures, "fatePower/" + power, powers[power - 1],
                    (double)expected["fatePowerMasses"][power - 1] / denominator);
            for (int slot = 0; slot < 6; slot++)
            {
                var die = fixture["inputs"]["dice"].Single(x => (string)x["id"] == dieIds[slot]);
                for (int face = 1; face <= 6; face++)
                    Compare(comparisons, failures, "face/slot" + slot + "/" + face, faces[slot, face - 1],
                        Ratio(die["faceProbabilities"][face - 1]));
            }
            for (int slot = 0; slot < OfferedSlots; slot++)
                for (int grade = 0; grade < 5; grade++)
                {
                    Compare(comparisons, failures, "exploration/slot" + slot + "/" + GradeNames[grade],
                        exploration[slot, grade], Ratio(expected["explorationGradeProbabilities"][grade]));
                    Compare(comparisons, failures, "combat/slot" + slot + "/" + GradeNames[grade],
                        combat[slot, grade], Ratio(expected["combatGradeProbabilities"][grade]));
                }

            var resource = new JObject
            {
                ["commonRerollBundleGold"] = 12, ["chargesPerBundle"] = 3, ["chargesPerReroll"] = 1,
                ["commonEmberGold"] = 16, ["goldPerChargeShadowPrice"] = 4,
                ["sixteenGoldShadowEquivalentRerolls"] = 4,
                ["expectedMeanFatePower"] = Ratio(expected["meanFatePower"]),
                ["actualMeanFatePower"] = powerSum / (double)samples,
                ["meanFatePowerError"] = powerSum / (double)samples - Ratio(expected["meanFatePower"]),
                ["expectedMeanFaceSum"] = Ratio(expected["meanFaceSum"]),
                ["actualMeanFaceSum"] = faceSum / (double)samples,
                ["explorationRareOrAboveExpectedPerSlot"] = Ratio(expected["explorationRareOrAbove"]),
                ["combatRareOrAboveExpectedPerSlot"] = Ratio(expected["combatRareOrAbove"]),
                ["explorationRareOrAboveActualPerSlot"] = RareOrAbove(exploration, samples),
                ["combatRareOrAboveActualPerSlot"] = RareOrAbove(combat, samples),
                ["optionalOneRerollTheoryNotSampled"] = expected["optionalOneRerollTheory"].DeepClone(),
                ["replacementEfficiencyTheory"] = expected["replacementEfficiencyTheory"].DeepClone(),
                ["limits"] = fixture["resourceLimits"].DeepClone()
            };
            var evidence = new JObject
            {
                ["schema"] = 1, ["test"] = nameof(ActualGameRngMatchesFrozenIndependentDistribution),
                ["replacementCount"] = replacementCount, ["startedUtc"] = startedUtc,
                ["completedUtc"] = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ["elapsedSeconds"] = elapsed.Elapsed.TotalSeconds, ["samples"] = samples,
                ["samplingContract"] = fixture["sampling"].DeepClone(),
                ["provenance"] = fixture["provenance"].DeepClone(), ["streams"] = streams,
                ["comparisons"] = comparisons, ["failedComparisonCount"] = failures.Count,
                ["resourceSummary"] = resource
            };
            // Only test-owned temp evidence; no LocalRunStore, persistentDataPath, asset or user-save writes.
            string outputDirectory = Path.Combine(Path.GetTempPath(), "FateDiceDistribution");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "actual-" + replacementCount + ".json");
            File.WriteAllText(outputPath, evidence.ToString(Formatting.Indented), new UTF8Encoding(false));
            TestContext.WriteLine("DISTRIBUTION EVIDENCE " + outputPath);
            TestContext.WriteLine(resource.ToString(Formatting.None));
            Assert.That(samples, Is.EqualTo(SamplesPerConfiguration));
            Assert.That(hands.Sum(), Is.EqualTo(samples));
            Assert.That(powers.Sum(), Is.EqualTo(samples));
            for (int slot = 0; slot < 6; slot++) Assert.That(RowTotal(faces, slot), Is.EqualTo(samples));
            for (int slot = 0; slot < OfferedSlots; slot++)
            {
                Assert.That(RowTotal(exploration, slot), Is.EqualTo(samples));
                Assert.That(RowTotal(combat, slot), Is.EqualTo(samples));
            }
            Assert.That(comparisons.Count * 4, Is.EqualTo(FamilyComparisons));
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        void AssertAuthoredInputs(JToken inputs)
        {
            Assert.That(config.dice.basicDieId, Is.EqualTo((string)inputs["basicDieId"]));
            Assert.That(config.dice.replacementDieId, Is.EqualTo((string)inputs["replacementDieId"]));
            Assert.That(config.dice.minimumFatePower, Is.EqualTo((int)inputs["minimumFatePower"]));
            Assert.That(config.dice.maximumFatePower, Is.EqualTo((int)inputs["maximumFatePower"]));
            Assert.That(config.dice.dice.Length, Is.EqualTo(inputs["dice"].Count()));
            foreach (var expected in inputs["dice"])
            {
                var actual = config.Die((string)expected["id"]);
                CollectionAssert.AreEqual(expected["values"].ToObject<int[]>(), actual.values, actual.id + " values");
                CollectionAssert.AreEqual(expected["weights"].ToObject<float[]>(), actual.weights, actual.id + " weights");
            }
            Assert.That(config.dice.hands.Length, Is.EqualTo(10));
            foreach (var expected in inputs["hands"])
            {
                var kind = (HandKind)Enum.Parse(typeof(HandKind), (string)expected["kind"]);
                Assert.That((int)kind, Is.EqualTo((int)expected["enumValue"]));
                var actual = config.dice.hands.Single(x => x.kind == kind);
                Assert.That(actual.priority, Is.EqualTo((int)expected["priority"]), kind + " priority");
                Assert.That(actual.fatePower, Is.EqualTo((int)expected["fatePower"]), kind + " fate power");
            }
            AssertGradeRows(config.fate.exploration, inputs["gradeTables"]["exploration"]);
            AssertGradeRows(config.fate.combat, inputs["gradeTables"]["combat"]);
            CollectionAssert.AreEqual(inputs["gradeMultipliers"].ToObject<float[]>(), config.fate.gradeMultipliers);
            Assert.That(config.world.offeredCards, Is.EqualTo((int)inputs["offeredSlots"]));
            Assert.That(config.world.shopPriceMultipliers, Is.Not.Null);
            Assert.That(config.world.shopPriceMultipliers.Length, Is.EqualTo(5));
            Assert.That(config.world.shopPriceMultipliers[(int)Grade.Common],
                Is.EqualTo((float)inputs["commonShopPriceMultiplier"]));
            var economy = inputs["economy"];
            Assert.That(config.growth.startingGold, Is.EqualTo((int)economy["starting_gold"]));
            Assert.That(config.growth.rerollCost, Is.EqualTo((int)economy["reroll_cost_charges"]));
            foreach (var expected in economy["shop"])
            {
                var actual = config.world.shop.Single(x => x.id == (string)expected["id"]);
                Assert.That(actual.price, Is.EqualTo((int)expected["price_gold"]), actual.id + " base price");
                Assert.That(actual.reward.rerollCharges, Is.EqualTo((int)expected["reroll_charges"]));
                Assert.That(actual.reward.dieId ?? "", Is.EqualTo((string)expected["die_id"]));
            }
            foreach (var expected in economy["events"])
            {
                var actual = config.world.events.Single(x => x.id == (string)expected["id"]);
                Assert.That((int)actual.type, Is.EqualTo((int)expected["node_type"]));
                Assert.That((int)actual.grade, Is.EqualTo((int)expected["grade"]));
                Assert.That(actual.reward.health, Is.EqualTo((int)expected["health"]));
                Assert.That(actual.reward.gold, Is.EqualTo((int)expected["gold"]));
                Assert.That(actual.reward.xp, Is.EqualTo((int)expected["xp"]));
                Assert.That(actual.reward.rerollCharges, Is.EqualTo((int)expected["reroll_charges"]));
                Assert.That(actual.reward.dieId ?? "", Is.EqualTo((string)expected["die_id"]));
            }
            Assert.That(config.world.restTraining.xp, Is.EqualTo((int)economy["rest_training"]["xp_before_cap_bonus"]));
            Assert.That(config.world.restTraining.rerollCharges, Is.EqualTo((int)economy["rest_training"]["reroll_charges"]));
        }

        static void AssertGradeRows(GradeRow[] actual, JToken expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Count()));
            for (int row = 0; row < actual.Length; row++)
            {
                Assert.That(actual[row].minimumPower, Is.EqualTo((int)expected[row]["minimum_power"]));
                CollectionAssert.AreEqual(expected[row]["weights"].ToObject<float[]>(), actual[row].weights);
            }
        }

        JToken Case(int replacementCount) => fixture["cases"].Single(x => (int)x["replacementCount"] == replacementCount);

        static double Ratio(JToken value)
        {
            string[] parts = ((string)value).Split('/');
            long numerator = long.Parse(parts[0], CultureInfo.InvariantCulture);
            long denominator = parts.Length == 1 ? 1 : long.Parse(parts[1], CultureInfo.InvariantCulture);
            if (parts.Length > 2 || denominator <= 0) throw new FormatException("Invalid fixed rational: " + value);
            return numerator / (double)denominator;
        }

        static double[] Probabilities(JToken values) => values.Select(Ratio).ToArray();

        static int CountTolerance(double probability)
        {
            if (probability < 0 || probability > 1 || double.IsNaN(probability))
                throw new ArgumentOutOfRangeException(nameof(probability));
            if (probability == 0 || probability == 1) return 0;
            // Invert 2 exp[-t^2/(2(Np(1-p)+t/3))] <= alpha/M, then round count deviation up.
            double log = Math.Log(2.0 * FamilyComparisons / FamilyAlpha);
            return (int)Math.Ceiling(log / 3 + Math.Sqrt(
                2 * SamplesPerConfiguration * probability * (1 - probability) * log + log * log / 9));
        }

        static void Compare(JArray evidence, List<string> failures, string metric, int actual, double probability)
        {
            double expected = SamplesPerConfiguration * probability;
            int tolerance = CountTolerance(probability);
            double error = actual - expected;
            bool withinBound = Math.Abs(error) <= tolerance;
            evidence.Add(new JObject
            {
                ["metric"] = metric, ["samples"] = SamplesPerConfiguration, ["actualCount"] = actual,
                ["expectedProbability"] = probability, ["expectedCount"] = expected,
                ["signedCountError"] = error, ["absoluteCountError"] = Math.Abs(error),
                ["probabilityError"] = error / SamplesPerConfiguration,
                ["allowedCountError"] = tolerance, ["withinBound"] = withinBound
            });
            string line = string.Format(CultureInfo.InvariantCulture,
                "{0}: actual={1}, expected={2:F6}, countError={3:F6}, allowed={4}, N={5}",
                metric, actual, expected, error, tolerance, SamplesPerConfiguration);
            TestContext.WriteLine(line);
            if (!withinBound) failures.Add(line);
        }

        static int RowTotal(int[,] values, int row)
        {
            int total = 0;
            for (int column = 0; column < values.GetLength(1); column++) total += values[row, column];
            return total;
        }

        static JArray RareOrAbove(int[,] values, int samples)
        {
            var result = new JArray();
            for (int slot = 0; slot < OfferedSlots; slot++)
                result.Add((values[slot, 2] + values[slot, 3] + values[slot, 4]) / (double)samples);
            return result;
        }
    }
}
