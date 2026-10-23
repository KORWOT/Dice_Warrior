using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;
using System.Security.Cryptography;
using System.Text;

namespace DiceWarrior.BalanceSim;

public sealed class BalanceScenario
{
    public const string ContentSha256 = "06DF1643383740F8D60551952CC36FAA1D5A0E130FDA7FB3A85D5632E11E26BA";
    public const string RulesVersion = "shared-world-preview-4";
    public const uint DefaultMasterSeed = 0xB0C70001u;
    public string ScenarioId { get; set; } = "v4-baseline-neutral-v1";
    public string PolicyId { get; set; } = "neutral-v1";
    public Profile Profile { get; set; } = new() { growth = 0, startingDie = "balanced", startingWeapon = "blade" };
    public string[] Heroes { get; set; } = { "wanderer", "scholar", "vanguard" };
    public int MaxCommands { get; set; } = BackendProtocol.MaxCommands;
    public double MatchTimeoutSeconds { get; set; } = 30;
    public string DefinitionHash { get; private set; }

    public CompetitionDefinition LoadDefinition(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != ContentSha256)
            throw new ArgumentException("Content hash differs from frozen v4 baseline.");
        var d = CompetitionContent.Load(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
        if (d.rulesVersion != RulesVersion) throw new ArgumentException("Expected v4 content.");
        DefinitionHash = LoopJson.Hash(LoopJson.Encode(d));
        return d;
    }

    public void Validate(CompetitionDefinition definition, IReadOnlyList<uint> seeds)
    {
        if (definition == null || DefinitionHash == null || definition.rulesVersion != RulesVersion ||
            LoopJson.Hash(LoopJson.Encode(definition)) != DefinitionHash)
            throw new ArgumentException("Load the frozen definition first; mutated content is forbidden.");
        if (seeds == null || seeds.Count == 0 || seeds.Count > 10000 || seeds.Any(s => s == 0) || seeds.Distinct().Count() != seeds.Count)
            throw new ArgumentException("Expected 1..10000 unique nonzero uint seeds.");
        if (ScenarioId != "v4-baseline-neutral-v1" || PolicyId != "neutral-v1" || Profile == null ||
            Profile.growth != 0 || Profile.currency != 0 || Profile.startingDie != "balanced" || Profile.startingWeapon != "blade" ||
            Heroes == null || !Heroes.SequenceEqual(new[] { "wanderer", "scholar", "vanguard" }) ||
            MaxCommands < 1 || MaxCommands > BackendProtocol.MaxCommands ||
            !double.IsFinite(MatchTimeoutSeconds) || MatchTimeoutSeconds <= 0 || MatchTimeoutSeconds > 300)
            throw new ArgumentException("Unsupported baseline policy, profile, heroes or execution bounds.");
    }

    public static uint[] GenerateSeeds(uint masterSeed, int count)
    {
        if (masterSeed == 0 || count < 1 || count > 10000) throw new ArgumentException("Invalid master seed/count.");
        var seeds = new uint[count];
        uint state = masterSeed;
        for (int i = 0; i < count; i++)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            seeds[i] = state;
        }
        if (seeds.Any(x => x == 0) || seeds.Distinct().Count() != count) throw new InvalidOperationException("Invalid generated seed set.");
        return seeds;
    }

    public static string SeedHash(IEnumerable<uint> seeds) => LoopJson.Hash(string.Join("\n", seeds.Select(s => s.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "\n");

    public static string PrepareOutput(string repositoryRoot, string outputPath)
    {
        var root = Path.GetFullPath(Path.Combine(repositoryRoot, "artifacts", "balance-sim"));
        var output = Path.GetFullPath(outputPath);
        if (!output.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Output must be a new directory beneath artifacts/balance-sim.");
        for (var check = new DirectoryInfo(output); check != null; check = check.Parent)
            if (check.Exists && (check.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Output path may not cross a symbolic link or junction.");
        if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Output already exists; use a new run directory.");
        Directory.CreateDirectory(output);
        return output;
    }
}
