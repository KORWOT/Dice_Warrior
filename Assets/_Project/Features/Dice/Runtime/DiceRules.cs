using System;
using System.Collections.Generic;

namespace FateDice
{
    /// <summary>Pure rules with one caller-owned xorshift32 stream. No UnityEngine.Random or global state.</summary>
    public static class DiceRules
    {
        public static uint Next(ref uint state)
        {
            if (state == 0) throw new ArgumentOutOfRangeException(nameof(state), "The run seed must be nonzero.");
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }

        public static int WeightedIndex(float[] weights, ref uint state)
        {
            double total = CheckWeights(weights);
            // Even a one-item or certain outcome consumes one sample. Animation never calls this.
            double target = Next(ref state) / 4294967296.0 * total;
            double cumulative = 0;
            int lastPositive = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0) lastPositive = i;
                cumulative += weights[i];
                if (target < cumulative) return i;
            }
            // Only floating-point accumulation at the upper boundary can reach this branch.
            return lastPositive;
        }

        static double CheckWeights(float[] weights)
        {
            if (weights == null || weights.Length == 0) throw new ArgumentException("A weighted pool cannot be empty.", nameof(weights));
            double total = 0;
            foreach (float weight in weights)
            {
                if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0)
                    throw new ArgumentException("Weights must be finite and nonnegative.", nameof(weights));
                total += weight;
            }
            if (total <= 0 || double.IsInfinity(total)) throw new ArgumentException("Weight sum must be finite and positive.", nameof(weights));
            return total;
        }

        public static int[] Roll(GameConfigData config, string[] dieIds, ref uint state)
        {
            if (config == null || config.dice == null) throw new ArgumentException("Missing dice configuration.", nameof(config));
            if (dieIds == null || dieIds.Length != 6) throw new ArgumentException("A roll requires six owned dice.", nameof(dieIds));
            var definitions = new DieDefinition[6];
            for (int i = 0; i < definitions.Length; i++)
            {
                var die = config.Die(dieIds[i]);
                if (die.values == null || die.values.Length != 6 || die.weights == null || die.weights.Length != 6)
                    throw new ArgumentException("Die " + die.id + " requires six values and weights.", nameof(config));
                foreach (int value in die.values)
                    if (value < 1 || value > 6) throw new ArgumentException("Die face values must be 1..6.", nameof(config));
                CheckWeights(die.weights);
                definitions[i] = die;
            }
            // Validate all dice first, then commit the RNG after all six samples.
            uint next = state;
            var result = new int[6];
            for (int i = 0; i < result.Length; i++)
                result[i] = definitions[i].values[WeightedIndex(definitions[i].weights, ref next)];
            state = next;
            return result;
        }

        public static bool Supports(HandKind kind, int[] dice) => SupportsCounts(kind, Count(dice));

        public static HandDefinition BestHand(int[] dice, HandDefinition[] definitions)
        {
            var counts = Count(dice);
            if (definitions == null || definitions.Length != 10)
                throw new ArgumentException("Exactly ten hand definitions are required.", nameof(definitions));
            var kinds = new bool[10];
            var priorities = new HashSet<int>();
            HandDefinition best = null;
            foreach (var definition in definitions)
            {
                if (definition == null || (int)definition.kind < 0 || (int)definition.kind >= kinds.Length ||
                    kinds[(int)definition.kind] || definition.priority < 0 || !priorities.Add(definition.priority))
                    throw new ArgumentException("Hand kinds and nonnegative priorities must be complete and unique.", nameof(definitions));
                kinds[(int)definition.kind] = true;
                if (SupportsCounts(definition.kind, counts) && (best == null || definition.priority > best.priority))
                    best = definition;
            }
            if (best == null) throw new InvalidOperationException("A valid 6D6 roll must have a supported hand.");
            return best;
        }

        static int[] Count(int[] dice)
        {
            if (dice == null || dice.Length != 6) throw new ArgumentException("Hand evaluation requires six dice.", nameof(dice));
            var counts = new int[7];
            foreach (int value in dice)
            {
                if (value < 1 || value > 6) throw new ArgumentException("Die results must be 1..6.", nameof(dice));
                counts[value]++;
            }
            return counts;
        }

        static bool SupportsCounts(HandKind kind, int[] counts)
        {
            int max = 0, pairs = 0, exactPairs = 0, distinct = 0;
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] > max) max = counts[value];
                if (counts[value] >= 2) pairs++;
                if (counts[value] == 2) exactPairs++;
                if (counts[value] > 0) distinct++;
            }
            switch (kind)
            {
                case HandKind.Pair: return max >= 2;
                case HandKind.TwoPairs: return pairs >= 2;
                case HandKind.Triple: return max >= 3;
                case HandKind.ThreePairs: return exactPairs == 3;
                case HandKind.FullHouse: return max >= 3 && pairs >= 2;
                case HandKind.FourKind: return max >= 4;
                case HandKind.Straight:
                    return counts[2] > 0 && counts[3] > 0 && counts[4] > 0 && counts[5] > 0 &&
                           (counts[1] > 0 || counts[6] > 0);
                case HandKind.FullStraight: return distinct == 6;
                case HandKind.FiveKind: return max >= 5;
                case HandKind.SixKind: return max == 6;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
