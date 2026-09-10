using System;

namespace FateDice
{
    // Selects the minimum visible contributors to an already committed hand.
    // It does not choose a hand, reorder dice, or consume either source of RNG.
    public static class DiceComboHighlights
    {
        public static int[] Groups(HandKind hand, int[] values)
        {
            if (values == null || values.Length != 6)
                throw new ArgumentException("Dice highlights require exactly six results.", nameof(values));
            var counts = new int[7];
            foreach (int value in values)
            {
                if (value < 1 || value > 6)
                    throw new ArgumentException("Dice highlight results must be 1..6.", nameof(values));
                counts[value]++;
            }

            var groups = new[] { -1, -1, -1, -1, -1, -1 };
            switch (hand)
            {
                case HandKind.Pair: MarkRepeated(values, counts, groups, 2); break;
                case HandKind.Triple: MarkRepeated(values, counts, groups, 3); break;
                case HandKind.FourKind: MarkRepeated(values, counts, groups, 4); break;
                case HandKind.FiveKind: MarkRepeated(values, counts, groups, 5); break;
                case HandKind.SixKind: MarkRepeated(values, counts, groups, 6); break;
                case HandKind.TwoPairs: MarkPairs(values, counts, groups, 2); break;
                case HandKind.ThreePairs: MarkPairs(values, counts, groups, 3); break;
                case HandKind.FullHouse:
                    for (int triple = 1; triple <= 6; triple++)
                    {
                        if (counts[triple] < 3) continue;
                        for (int pair = 1; pair <= 6; pair++)
                        {
                            if (pair == triple || counts[pair] < 2) continue;
                            Take(values, groups, triple, 3, 0);
                            Take(values, groups, pair, 2, 1);
                            return groups;
                        }
                    }
                    break;
                case HandKind.Straight:
                    int start = HasRun(counts, 1, 5) ? 1 : HasRun(counts, 2, 6) ? 2 : 0;
                    if (start > 0)
                        for (int value = start; value < start + 5; value++) Take(values, groups, value, 1, 0);
                    break;
                case HandKind.FullStraight:
                    if (HasRun(counts, 1, 6))
                        for (int i = 0; i < groups.Length; i++) groups[i] = 0;
                    break;
            }
            return groups;
        }

        static void MarkRepeated(int[] values, int[] counts, int[] groups, int required)
        {
            for (int value = 1; value <= 6; value++)
            {
                if (counts[value] < required) continue;
                Take(values, groups, value, required, 0);
                return;
            }
        }

        static void MarkPairs(int[] values, int[] counts, int[] groups, int required)
        {
            int candidates = 0;
            for (int value = 1; value <= 6; value++) if (counts[value] >= 2) candidates++;
            if (candidates < required) return;
            int group = 0;
            for (int value = 1; value <= 6 && group < required; value++)
                if (counts[value] >= 2) Take(values, groups, value, 2, group++);
        }

        static bool HasRun(int[] counts, int first, int last)
        {
            for (int value = first; value <= last; value++) if (counts[value] == 0) return false;
            return true;
        }

        static void Take(int[] values, int[] groups, int face, int required, int group)
        {
            for (int i = 0; i < values.Length && required > 0; i++)
            {
                if (values[i] != face) continue;
                groups[i] = group;
                required--;
            }
        }
    }
}
