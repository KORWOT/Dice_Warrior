namespace FateDice
{
    // Called only when starting a journey. Continuing uses the checkpoint's RNG state.
    public interface ISeedSource
    {
        uint NextSeed();
    }
}
