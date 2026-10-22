using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionModifierTests
    {
        static V4Modifier Modifier(string id, int amount, ModifierOperation operation = ModifierOperation.Flat,
            ModifierStack stack = ModifierStack.Stackable, string source = "slot:0/blade", string group = "damage.flat") =>
            new V4Modifier { id = id, amount = amount, operation = operation, stack = stack, sourceKind = "gear", sourceId = source,
                target = "damage", stackGroup = group };

        [TestCase(DamageType.Fire, 138)]
        [TestCase(DamageType.Ice, 98)]
        public void ElementBonusAndAffinityApplyOnceBeforeFinalRounding(DamageType type, int expected)
        {
            var definition = new CompetitionDefinition { v4 = new CompetitionV4Definition() };
            var enemy = new V4EnemyDefinition { weakness = DamageType.Fire, resistance = DamageType.Ice };
            int affinity = CompetitionElements.Affinity(definition, enemy, type);
            int result = CompetitionModifiers.EvaluateScaled(100, new AttributeValues(), new AttributeValues(),
                new[] { Modifier("element-bonus", 1500, ModifierOperation.PercentAdd) }, affinity);
            Assert.That(result, Is.EqualTo(expected), "100 × 1.15 × 1.20 = 138; 100 × 1.15 × 0.85 = 97.75 → 98.");
        }

        [Test] public void FractionalStatTermsAreCombinedWithoutPerTermRounding()
        {
            var values = new AttributeValues { strength = 1, dexterity = 1 };
            var coefficients = new AttributeValues { strength = 5000, dexterity = 5000 };
            Assert.That(CompetitionModifiers.EvaluateScaled(0, values, coefficients, Array.Empty<V4Modifier>()), Is.EqualTo(1),
                "The two half points sum to one; rounding each half first would produce two.");
            values.dexterity = 2;
            Assert.That(CompetitionModifiers.EvaluateScaled(10, values, coefficients,
                new[] { Modifier("bonus", 1500, ModifierOperation.PercentAdd) }, 8500), Is.EqualTo(11),
                "11.5 × 1.15 × 0.85 = 11.24125; rounding 11.5 before modifiers would incorrectly produce 12.");
        }

        [Test] public void EveryAttributeContributesAndHalfPointsRoundAwayFromZero()
        {
            var values = new AttributeValues { strength = 1, dexterity = 2, constitution = 4, intelligence = 8, will = 16, luck = 32 };
            var coefficients = new AttributeValues { strength = 10000, dexterity = 10000, constitution = 10000,
                intelligence = 10000, will = 10000, luck = 10000 };
            Assert.That(CompetitionModifiers.EvaluateScaled(1, values, coefficients, Array.Empty<V4Modifier>()), Is.EqualTo(64));
            Assert.That(CompetitionModifiers.EvaluateScaled(10, new AttributeValues { will = 1 },
                new AttributeValues { will = 5000 }, Array.Empty<V4Modifier>()), Is.EqualTo(11));
        }

        [Test] public void ExactDuplicateIdentityCountsOnceAndConflictingDuplicateIsRejected()
        {
            var first = Modifier("steel-bonus", 3); var duplicate = Modifier("steel-bonus", 3);
            Assert.That(CompetitionModifiers.Normalize(new[] { first, duplicate }).Length, Is.EqualTo(1));
            Assert.That(CompetitionModifiers.Evaluate(10, new[] { first, duplicate }), Is.EqualTo(13));
            duplicate.amount = 4;
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { first, duplicate }));
            duplicate.amount = 3; duplicate.stackGroup = "other";
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { first, duplicate }));
        }

        [Test] public void UniqueBySourceStacksDifferentSourcesButRejectsTwoEffectsForOneSource()
        {
            var first = Modifier("first", 3, stack: ModifierStack.UniqueBySource, source: "slot:0/blade");
            var second = Modifier("second", 5, stack: ModifierStack.UniqueBySource, source: "slot:2/charm");
            Assert.That(CompetitionModifiers.Evaluate(10, new[] { first, second }), Is.EqualTo(18));
            second.sourceId = "slot:0/blade";
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(10, new[] { first, second }));
        }

        [Test] public void HighestWinsUsesOrdinalTieBreakAndKeepsOneContribution()
        {
            var z = Modifier("z-effect", 7, stack: ModifierStack.HighestWins, source: "z");
            var a = Modifier("a-effect", 7, stack: ModifierStack.HighestWins, source: "a");
            var low = Modifier("low-effect", 4, stack: ModifierStack.HighestWins, source: "0");
            var result = CompetitionModifiers.Normalize(new[] { z, low, a });
            Assert.That(result.Length, Is.EqualTo(1)); Assert.That(result[0].sourceId, Is.EqualTo("a"));
            Assert.That(CompetitionModifiers.Evaluate(10, new[] { a, z, low }), Is.EqualTo(17));
            Assert.That(CompetitionModifiers.Normalize(new[] { a, z, low })[0].sourceId, Is.EqualTo("a"));
        }

        [Test] public void PercentAddSumsWhileDistinctFinalMultiplyGroupsMultiply()
        {
            var tenPercent = Modifier("ten", 1000, ModifierOperation.PercentAdd, source: "a", group: "element.percent");
            var twentyPercent = Modifier("twenty", 2000, ModifierOperation.PercentAdd, source: "b", group: "element.percent");
            Assert.That(CompetitionModifiers.Evaluate(100, new[] { tenPercent, twentyPercent }), Is.EqualTo(130));
            twentyPercent.stackGroup = "another.percent";
            Assert.That(CompetitionModifiers.Evaluate(100, new[] { tenPercent, twentyPercent }), Is.EqualTo(130));
            var first = Modifier("first", 11000, ModifierOperation.FinalMultiply, ModifierStack.HighestWins, "a", "first.factor");
            var second = Modifier("second", 12000, ModifierOperation.FinalMultiply, ModifierStack.HighestWins, "b", "second.factor");
            Assert.That(CompetitionModifiers.Evaluate(100, new[] { first, second }), Is.EqualTo(132));
            second.stackGroup = "first.factor";
            Assert.That(CompetitionModifiers.Evaluate(100, new[] { first, second }), Is.EqualTo(120));
        }

        [Test] public void FlatThenPercentThenFinalFactorRoundsOnlyTheFinalValue()
        {
            var modifiers = new[] { Modifier("flat", 10),
                Modifier("percent", 1000, ModifierOperation.PercentAdd, group: "percent"),
                Modifier("factor", 15000, ModifierOperation.FinalMultiply, ModifierStack.HighestWins, group: "factor") };
            Assert.That(CompetitionModifiers.Evaluate(100, modifiers), Is.EqualTo(182), "(100+10) × 1.1 × 1.5 = 181.5 → 182.");
            Assert.That(CompetitionModifiers.Evaluate(100, modifiers.Reverse()), Is.EqualTo(182));
        }

        [Test] public void ConflictingGroupPoliciesAndMixedNumericTargetsAreRejected()
        {
            var first = Modifier("first", 5);
            var second = Modifier("second", 1000, ModifierOperation.PercentAdd, source: "second");
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { first, second }));
            second.operation = ModifierOperation.Flat; second.stack = ModifierStack.HighestWins;
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { first, second }));
            second.stack = ModifierStack.Stackable; second.target = "block";
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(10, new[] { first, second }));
        }

        [Test] public void NumericCapsRejectOutOfRangeInputsAndResultsWithoutIntegerWraparound()
        {
            Assert.That(CompetitionModifiers.Evaluate(100000000, Array.Empty<V4Modifier>()), Is.EqualTo(100000000));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(100000001, Array.Empty<V4Modifier>()));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(-1, Array.Empty<V4Modifier>()));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(100000000, new[] { Modifier("one-too-many", 1) }));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.EvaluateScaled(0, new AttributeValues { strength = 100000001 },
                new AttributeValues { strength = 10000 }, Array.Empty<V4Modifier>()));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.EvaluateScaled(0, new AttributeValues { strength = 1 },
                new AttributeValues { strength = 10001 }, Array.Empty<V4Modifier>()));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.EvaluateScaled(1, new AttributeValues(), new AttributeValues(), Array.Empty<V4Modifier>(), 20001));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { Modifier("too-large", 20001) }));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { Modifier("negative-factor", -1,
                ModifierOperation.FinalMultiply, ModifierStack.HighestWins) }));
            var percentages = Enumerable.Range(0, 5).Select(i => Modifier("percent-" + i, 20000, ModifierOperation.PercentAdd));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(1, percentages));
            var factors = Enumerable.Range(0, 64).Select(i => Modifier("factor-" + i, 20000, ModifierOperation.FinalMultiply,
                ModifierStack.HighestWins, group: "factor-" + i));
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Evaluate(1, factors), "2^64 must not wrap to a valid int or silently clamp.");
        }

        [Test] public void OversizedModifierStreamStopsAtTheFirstDisallowedEntry()
        {
            int consumed = 0;
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(ManyModifiers(() => ++consumed)));
            Assert.That(consumed, Is.EqualTo(65));
        }

        [Test] public void SuccessfulEvaluationAndRejectedInputNeverMutateCallerOwnedValues()
        {
            var values = new AttributeValues { strength = 7, dexterity = 6 };
            var coefficients = new AttributeValues { strength = 7000, dexterity = 3000 };
            var modifiers = new[] { Modifier("bonus", 2) };
            string before = LoopJson.Encode(values) + LoopJson.Encode(coefficients) + LoopJson.Encode(modifiers);
            Assert.That(CompetitionModifiers.EvaluateScaled(2, values, coefficients, modifiers), Is.EqualTo(11));
            var normalized = CompetitionModifiers.Normalize(modifiers); normalized[0].amount = 999; normalized[0].sourceId = "changed";
            Assert.That(LoopJson.Encode(values) + LoopJson.Encode(coefficients) + LoopJson.Encode(modifiers), Is.EqualTo(before));
            var bad = new[] { Modifier("same", 2), Modifier("same", 3) };
            string rejectedBefore = LoopJson.Encode(bad);
            Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(bad));
            Assert.That(LoopJson.Encode(bad), Is.EqualTo(rejectedBefore));
        }

        [TestCase("id")]
        [TestCase("sourceKind")]
        [TestCase("sourceId")]
        [TestCase("target")]
        [TestCase("stackGroup")]
        public void IdentityFieldsRejectDelimiterCollisionsAndOversizedStrings(string field)
        {
            foreach (var invalid in new[] { "unsafe|identity", new string('x', 129) })
            {
                var modifier = Modifier("valid", 1);
                switch (field)
                {
                    case "id": modifier.id = invalid; break;
                    case "sourceKind": modifier.sourceKind = invalid; break;
                    case "sourceId": modifier.sourceId = invalid; break;
                    case "target": modifier.target = invalid; break;
                    case "stackGroup": modifier.stackGroup = invalid; break;
                }
                Assert.Throws<ArgumentException>(() => CompetitionModifiers.Normalize(new[] { modifier }), field + ": invalid identity must not reach joined grouping keys");
            }
        }

        static IEnumerable<V4Modifier> ManyModifiers(Action next)
        {
            for (int i = 0; i < 100; ++i) { next(); yield return Modifier("effect-" + i, 1); }
        }
    }
}
