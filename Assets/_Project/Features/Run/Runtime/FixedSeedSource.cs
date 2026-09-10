using System;

namespace FateDice
{
    public sealed class FixedSeedSource : ISeedSource
    {
        readonly uint seed;
        public uint Value => seed;

        public FixedSeedSource(uint seed)
        {
            if (seed == 0) throw new ArgumentOutOfRangeException(nameof(seed), "Seed must be nonzero.");
            this.seed = seed;
        }

        public uint NextSeed() => seed;
    }
}
