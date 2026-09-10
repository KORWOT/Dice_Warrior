namespace FateDice
{
    // Captured from the displayed state, never refreshed inside a stale button callback.
    public sealed class RunCommandToken
    {
        public string RunId { get; }
        public int Sequence { get; }
        public RunCommandToken(string runId, int sequence)
        { RunId = runId; Sequence = sequence; }
    }
}
