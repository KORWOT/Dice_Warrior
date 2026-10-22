using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionRandom
    {
        // FNV-1a over each UTF-16 code unit's low then high byte. Arithmetic is explicitly uint wraparound.
        public static uint Split(uint seed, string participantId, string stream)
        {
            uint value = seed ^ 2166136261u;
            unchecked
            {
                foreach (char c in participantId + "/" + stream)
                {
                    value = (value ^ (byte)c) * 16777619u;
                    value = (value ^ (byte)(c >> 8)) * 16777619u;
                }
            }
            return value == 0 ? 0x9e3779b9u : value;
        }
        public static int Next(ref uint state, int count) => (int)(LoopRules.Next(ref state) % (uint)count);
    }
}
