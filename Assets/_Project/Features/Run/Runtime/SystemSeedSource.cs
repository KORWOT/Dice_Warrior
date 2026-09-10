using System;
using System.Security.Cryptography;

namespace FateDice
{
    // Operating-system entropy chooses the starting seed; gameplay uses only DiceRules afterward.
    public sealed class SystemSeedSource : ISeedSource
    {
        public uint NextSeed()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                var bytes = new byte[sizeof(uint)];
                uint seed;
                do
                {
                    random.GetBytes(bytes);
                    seed = BitConverter.ToUInt32(bytes, 0);
                } while (seed == 0);
                return seed;
            }
        }
    }
}
