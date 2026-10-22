using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionModifiers
    {
        static readonly AttributeStat[] CompetitionAttributesList = (AttributeStat[])Enum.GetValues(typeof(AttributeStat));
        public static V4Modifier[] Normalize(IEnumerable<V4Modifier> input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var entries = input.Take(65).ToArray();
            if (entries.Length > 64 || entries.Any(m => m == null || string.IsNullOrWhiteSpace(m.id) || string.IsNullOrWhiteSpace(m.sourceKind) ||
                string.IsNullOrWhiteSpace(m.sourceId) || string.IsNullOrWhiteSpace(m.target) || string.IsNullOrWhiteSpace(m.stackGroup) ||
                !Enum.IsDefined(typeof(ModifierOperation), m.operation) || !Enum.IsDefined(typeof(ModifierStack), m.stack) ||
                m.amount < -10000 || m.amount > 20000 || (m.operation == ModifierOperation.FinalMultiply &&
                (m.amount < 0 || m.stack != ModifierStack.HighestWins)))) throw new ArgumentException("Invalid or excessive modifiers");
            foreach (var m in entries)
            {
                CompetitionTags.ValidateId(m.id); CompetitionTags.ValidateId(m.sourceKind); CompetitionTags.ValidateId(m.sourceId);
                CompetitionTags.ValidateId(m.target); CompetitionTags.ValidateId(m.stackGroup);
            }
            var distinct = new List<V4Modifier>();
            foreach (var group in entries.GroupBy(m => m.sourceKind + "|" + m.sourceId + "|" + m.id + "|" + m.target))
            {
                var first = group.First();
                if (group.Any(m => m.amount != first.amount || m.operation != first.operation || m.stackGroup != first.stackGroup || m.stack != first.stack))
                    throw new ArgumentException("Conflicting modifier identity");
                distinct.Add(LoopJson.Copy(first));
            }
            foreach (var group in distinct.GroupBy(m => m.target + "|" + m.stackGroup))
                if (group.Select(m => m.operation).Distinct().Count() != 1 || group.Select(m => m.stack).Distinct().Count() != 1)
                    throw new ArgumentException("Conflicting stack group policy");
            var selected = new List<V4Modifier>();
            foreach (var group in distinct.GroupBy(m => m.target + "|" + m.stackGroup))
            {
                if (group.First().stack == ModifierStack.HighestWins)
                    selected.Add(group.OrderByDescending(m => m.amount).ThenBy(m => m.sourceKind, StringComparer.Ordinal)
                        .ThenBy(m => m.sourceId, StringComparer.Ordinal).ThenBy(m => m.id, StringComparer.Ordinal).First());
                else if (group.First().stack == ModifierStack.UniqueBySource)
                    foreach (var source in group.GroupBy(m => m.sourceKind + "|" + m.sourceId))
                    {
                        if (source.Count() != 1) throw new ArgumentException("Ambiguous unique source effect");
                        selected.Add(source.Single());
                    }
                else selected.AddRange(group);
            }
            return selected.OrderBy(m => m.target, StringComparer.Ordinal).ThenBy(m => m.operation)
                .ThenBy(m => m.stackGroup, StringComparer.Ordinal).ThenBy(m => m.id, StringComparer.Ordinal)
                .ThenBy(m => m.sourceKind, StringComparer.Ordinal).ThenBy(m => m.sourceId, StringComparer.Ordinal).ToArray();
        }

        public static int Evaluate(int basis, IEnumerable<V4Modifier> modifiers) => EvaluateScaled(basis, new AttributeValues(), new AttributeValues(), modifiers);

        public static int EvaluateScaled(int basis, AttributeValues values, AttributeValues coefficients, IEnumerable<V4Modifier> modifiers, int affinityBasisPoints = 10000)
        {
            if (basis < 0 || basis > 100000000 || values == null || coefficients == null || affinityBasisPoints < 0 || affinityBasisPoints > 20000 ||
                CompetitionAttributesList.Any(s => values.Get(s) < 0 || values.Get(s) > 100000000 || coefficients.Get(s) < 0 || coefficients.Get(s) > 10000))
                throw new ArgumentException("Invalid bounded numeric input");
            var active = Normalize(modifiers);
            if (active.Select(m => m.target).Distinct().Count() > 1) throw new ArgumentException("One numeric target per evaluation");
            long scaled = checked((long)basis * 10000);
            foreach (var stat in CompetitionAttributesList) scaled = checked(scaled + (long)values.Get(stat) * coefficients.Get(stat));
            foreach (var flat in active.Where(m => m.operation == ModifierOperation.Flat)) scaled = checked(scaled + (long)flat.amount * 10000);
            long percent = 10000L + active.Where(m => m.operation == ModifierOperation.PercentAdd).Sum(m => (long)m.amount);
            if (percent < 0 || percent > 100000) throw new ArgumentException("Percent modifier cap exceeded");
            // Exact bounded integer rational arithmetic; no per-term or intermediate rounding.
            BigInteger numerator = (BigInteger)scaled * percent * affinityBasisPoints;
            BigInteger denominator = 10000L * 10000 * 10000;
            foreach (var factor in active.Where(m => m.operation == ModifierOperation.FinalMultiply))
            { numerator *= factor.amount; denominator *= 10000; }
            if (numerator <= 0) return 0;
            var rounded = BigInteger.DivRem(numerator, denominator, out var remainder);
            if (remainder * 2 >= denominator) ++rounded;
            if (rounded > 100000000) throw new ArgumentException("Numeric result cap exceeded");
            return (int)rounded;
        }
    }
}

