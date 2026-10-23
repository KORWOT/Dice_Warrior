using DiceWarrior.SharedWorld;
using Newtonsoft.Json;

namespace DiceWarrior.BalanceSim;

public sealed class ScenarioManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string GitSha { get; set; }
    public string RulesVersion { get; set; }
    public string ContentSha256 { get; set; }
    public string ScenarioId { get; set; }
    public string PolicyId { get; set; }
    public uint MasterSeed { get; set; }
    public uint[] Seeds { get; set; }
    public string SeedSetSha256 { get; set; }
    public string[] Heroes { get; set; }
    public string StartingDie { get; set; }
    public string StartingWeapon { get; set; }
    public int Growth { get; set; }
    public string RunnerSourceSha256 { get; set; }
    public string Runtime { get; set; }
    public string Os { get; set; }
    public int ProcessorCount { get; set; }
    public int MaxCommands { get; set; }
    public double MatchTimeoutSeconds { get; set; }
    public string CreatedUtc { get; set; }
    public string SeedGenerator { get; set; } = "xorshift32-13-17-5-v1; prefix stages";
}

public sealed class BatchResult
{
    public ScenarioManifest Manifest { get; set; }
    public int RequestedMatches { get; set; }
    public string Status { get; set; }
    public List<MatchResult> Matches { get; set; } = new();
    public PerformanceMetrics Performance { get; set; } = new();
}

public sealed class PerformanceMetrics
{
    public double ElapsedSeconds { get; set; }
    public double MatchesPerSecond { get; set; }
    public double MatchP50Ms { get; set; }
    public double MatchP95Ms { get; set; }
    public long PeakWorkingSetBytes { get; set; }
    public long MaxObservedManagedHeapBytes { get; set; }
    public long AllocatedBytes { get; set; }
    public int[] GcCollections { get; set; }
    public long RawOutputBytesBeforeReports { get; set; }
    public string Sampling { get; set; } = "working set process peak; managed heap after each match; replay excluded from batch timing";
}

public sealed class MatchResult
{
    public uint Seed { get; set; }
    public string Hero { get; set; }
    public string Status { get; set; }
    public string EndReason { get; set; }
    public int WorldRound { get; set; }
    public int HumanCommands { get; set; }
    public int TrialWinnerCount { get; set; }
    public string StateHash { get; set; }
    public string RngHash { get; set; }
    public string CommandHash { get; set; }
    public double ElapsedMs { get; set; }
    public List<ParticipantResult> Participants { get; set; } = new();
    public HumanObservations Observations { get; set; }
    public MatchFailure Failure { get; set; }
}

public sealed class ParticipantResult
{
    public string Slot { get; set; }
    public string Hero { get; set; }
    public string AiPolicy { get; set; }
    public bool IsHuman { get; set; }
    public bool TrialSucceeded { get; set; }
    public bool Eliminated { get; set; }
    public bool Finished { get; set; }
    public string EliminationReason { get; set; }
    public int Rank { get; set; }
    public double FirstPlaceShare { get; set; }
    public int Laps { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public double HpRatio { get; set; }
    public int Level { get; set; }
    public int Xp { get; set; }
    public int Honor { get; set; }
    public int Gold { get; set; }
    public int? MaxEncounterTurns { get; set; }
}

public sealed class MatchFailure
{
    public string Reason { get; set; }
    public string ExceptionType { get; set; }
    public int CommandNumber { get; set; }
    public string Phase { get; set; }
    public string Target { get; set; }
}

public sealed class RunOptions
{
    public bool Observe { get; set; } = true;
    public int[] RestoreAfterCommands { get; set; } = Array.Empty<int>();
}

// Kept only while verifying or writing a failed seed's minimal replay, never in normal output.
public sealed class MatchExecution
{
    public MatchResult Result { get; set; }
    public CompetitionState FinalState { get; set; }
    public List<CompetitionCommandEntry> Commands { get; set; } = new();
}
