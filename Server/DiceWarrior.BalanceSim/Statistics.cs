namespace DiceWarrior.BalanceSim;

public static class Statistics
{
    public static SimulationSummary BuildSummary(BatchResult batch, int bootstrapIterations = 2000, uint bootstrapSeed = 0xB0C70001)
    {
        Validate(batch);
        if (bootstrapIterations < 1 || bootstrapIterations > 100000 || bootstrapSeed == 0)
            throw new ArgumentOutOfRangeException(nameof(bootstrapIterations));
        var valid = batch.Matches.Where(x => x.Status == "PASS").OrderBy(x => x.Seed).ThenBy(x => x.Hero, StringComparer.Ordinal).ToArray();
        var summary = new SimulationSummary
        {
            Status = batch.Status == "PASS" && batch.Matches.Count == batch.RequestedMatches && valid.Length == batch.RequestedMatches ? "PASS" : "BLOCKED",
            Manifest = batch.Manifest, RequestedMatches = batch.RequestedMatches, ExecutedMatches = batch.Matches.Count,
            SuccessfulMatches = valid.Length, FailedMatches = batch.Matches.Count - valid.Length,
            NotRunMatches = batch.RequestedMatches - batch.Matches.Count, CompletionRate = Rate(valid.Length, batch.Matches.Count),
            BootstrapIterations = bootstrapIterations, BootstrapSeed = bootstrapSeed,
            BootstrapMethod = "complete seed blocks; mean(left-right); xorshift32-13-17-5 with rejection sampling; stable FNV-derived metric seeds; percentile nearest-rank 2.5%/97.5%",
            Performance = batch.Performance,
            InterpretationNotes = new[]
            {
                "Results describe the fixed neutral-v1 automated human policy, not human player skill or universal adventurer strength.",
                "AI results are fixed slot/adventurer/policy combinations, stratified by the human adventurer. Adventurer and AI policy effects are confounded.",
                "Participant outcome denominators include only valid completed matches. Failures and unattempted matches are disclosed separately, never converted to losses or zero measurements.",
                "Wilson intervals apply to binomial trial/elimination/rank1 proportions. Fractional first-place scores are not Bernoulli outcomes and do not use Wilson intervals.",
                "The four participants in a match and repeated human scenarios sharing a seed are dependent. Paired comparisons resample complete seed blocks, not participant rows.",
                "Paired intervals are unadjusted exploratory 95% intervals across multiple metrics and three hero pairs; post-hoc selection and multiplicity prohibit automatic balance pass/fail conclusions.",
                "Current absolute HP participates in ranking. HP/maxHP is reported separately. XP is remaining XP, not cumulative XP.",
                "Descriptive percentiles use nearest rank. Missing measurements are null/NOT_MEASURED; game failures and NOT_RUN matches are separate categories.",
                "Technical PASS means the requested experiment completed. It does not establish acceptable game balance; no tuning or sensitivity experiment was performed."
            }
        };
        var roster = valid.SelectMany(x => x.Participants.Where(p => !p.IsHuman)).GroupBy(p => p.Slot, StringComparer.Ordinal)
            .Select(g => g.First()).OrderBy(p => p.Slot, StringComparer.Ordinal).ToArray();
        foreach (var hero in batch.Manifest.Heroes)
        {
            var attempted = batch.Matches.Where(x => x.Hero == hero).ToArray();
            var matches = valid.Where(x => x.Hero == hero).ToArray();
            int failed = attempted.Length - matches.Length, notRun = batch.Manifest.Seeds.Length - attempted.Length;
            summary.Humans.Add(Participants(hero, "human", hero, null, matches.Select(x => x.Participants.Single(p => p.IsHuman)).ToArray(), failed, notRun));
            foreach (var ai in roster)
                summary.AiGroups.Add(Participants(hero, ai.Slot, ai.Hero, ai.AiPolicy,
                    matches.Select(x => x.Participants.Single(p => p.Slot == ai.Slot)).ToArray(), failed, notRun));
            summary.MatchGroups.Add(new MatchSummary
            {
                Hero = hero, SampleCount = matches.Length, FailedMatches = failed, NotRunMatches = notRun,
                Distributions = new()
                {
                    ["worldRound"] = Describe(matches.Select(x => (double)x.WorldRound)),
                    ["humanCommands"] = Describe(matches.Select(x => (double)x.HumanCommands)),
                    ["elapsedMs"] = Describe(matches.Select(x => x.ElapsedMs))
                },
                Frequencies = new()
                {
                    ["endReason"] = Frequency(matches.Select(x => x.EndReason), new[] { "all-complete", "round-limit" }),
                    ["trialWinnerCount"] = Frequency(matches.Select(x => Invariant(x.TrialWinnerCount)), Enumerable.Range(0, 5).Select(Invariant)),
                    ["rank1Count"] = Frequency(matches.Select(x => Invariant(x.Participants.Count(p => p.Rank == 1))), Enumerable.Range(1, 4).Select(Invariant)),
                    ["rankPattern"] = Frequency(matches.Select(x => string.Join("/", x.Participants.Select(p => p.Rank).Order()))),
                    ["hasRankTie"] = Frequency(matches.Select(x => x.Participants.Select(p => p.Rank).Distinct().Count() < 4 ? "yes" : "no"), new[] { "no", "yes" }),
                    ["humanHpTiebreakExposure"] = Frequency(matches.Select(x =>
                    {
                        var human = x.Participants.Single(p => p.IsHuman);
                        return x.Participants.Any(p => !p.IsHuman && p.TrialSucceeded == human.TrialSucceeded && p.Laps == human.Laps &&
                            p.Honor == human.Honor && p.Hp != human.Hp) ? "yes" : "no";
                    }), new[] { "no", "yes" })
                }
            });
            summary.HumanDetails.Add(Observations(hero, matches, failed, notRun));
        }
        AddPaired(summary, valid, bootstrapIterations, bootstrapSeed);
        foreach (var metric in new[] { "ai-full-skill-history", "ai-full-event-choice-history", "ai-lifetime-max-combat-turns" })
            summary.Unmeasured.Add(new() { Metric = metric, MissingCount = valid.Length * 3,
                Reason = "Each valid match has three AI participants; public last-round/last-combat state does not reconstruct lifetime events." });
        int missingHuman = summary.HumanDetails.Sum(x => x.MissingMatches);
        if (missingHuman > 0)
            summary.Unmeasured.Add(new() { Metric = "human-command-observations", MissingCount = missingHuman,
                Reason = "Observation disabled or absent for these valid matches. Their final outcomes remain measured." });
        return summary;
    }

    public static ConfidenceInterval Wilson95(int successes, int denominator)
    {
        if (denominator < 0 || successes < 0 || successes > denominator) throw new ArgumentOutOfRangeException(nameof(successes));
        if (denominator == 0) return null;
        const double z = 1.959963984540054;
        double p = successes / (double)denominator, z2 = z * z, d = 1 + z2 / denominator;
        double center = (p + z2 / (2 * denominator)) / d;
        double half = z * Math.Sqrt(p * (1 - p) / denominator + z2 / (4 * denominator * (double)denominator)) / d;
        return new() { Lower = Math.Max(0, center - half), Upper = Math.Min(1, center + half) };
    }

    public static DistributionSummary Describe(IEnumerable<double> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var values = samples.ToArray();
        if (values.Any(x => !double.IsFinite(x))) throw new ArgumentException("Non-finite observation.", nameof(samples));
        Array.Sort(values);
        if (values.Length == 0) return new();
        return new()
        {
            SampleCount = values.Length, Mean = values.Average(), Min = values[0], Max = values[^1],
            P05 = Percentile(values, .05), P25 = Percentile(values, .25), P50 = Percentile(values, .5),
            P75 = Percentile(values, .75), P95 = Percentile(values, .95)
        };
    }

    static void Validate(BatchResult batch)
    {
        if (batch?.Manifest?.Seeds == null || batch.Manifest.Heroes == null || batch.Matches == null ||
            batch.Manifest.Seeds.Length == 0 || batch.Manifest.Heroes.Length < 2 ||
            batch.Manifest.Seeds.Any(x => x == 0) || batch.Manifest.Seeds.Distinct().Count() != batch.Manifest.Seeds.Length ||
            batch.Manifest.Heroes.Any(string.IsNullOrWhiteSpace) || batch.Manifest.Heroes.Distinct(StringComparer.Ordinal).Count() != batch.Manifest.Heroes.Length ||
            batch.RequestedMatches != batch.Manifest.Seeds.Length * batch.Manifest.Heroes.Length || batch.Matches.Count > batch.RequestedMatches)
            throw new ArgumentException("Invalid batch sample frame.", nameof(batch));
        var identities = new HashSet<(uint, string)>();
        string roster = null;
        foreach (var match in batch.Matches)
        {
            if (match == null || !batch.Manifest.Seeds.Contains(match.Seed) || !batch.Manifest.Heroes.Contains(match.Hero, StringComparer.Ordinal) ||
                !identities.Add((match.Seed, match.Hero)) || (match.Status != "PASS" && match.Status != "BLOCKED"))
                throw new ArgumentException("Duplicate, unknown, or invalid match result.", nameof(batch));
            if (match.Status != "PASS") continue;
            if (match.Participants == null || match.Participants.Count != 4 || match.Participants.Any(p => p == null) ||
                match.Participants.Count(p => p.IsHuman) != 1 || match.Participants.Single(p => p.IsHuman).Hero != match.Hero ||
                match.Participants.Select(p => p.Slot).Distinct(StringComparer.Ordinal).Count() != 4 ||
                match.TrialWinnerCount != match.Participants.Count(p => p.TrialSucceeded) || match.WorldRound < 1 ||
                match.HumanCommands < 0 || !double.IsFinite(match.ElapsedMs) || match.ElapsedMs < 0 || string.IsNullOrWhiteSpace(match.EndReason))
                throw new ArgumentException("Incomplete valid-match record.", nameof(batch));
            int firstCount = match.Participants.Count(p => p.Rank == 1);
            if (firstCount == 0) throw new ArgumentException("No first-place participant.", nameof(batch));
            foreach (var p in match.Participants)
            {
                double share = p.Rank == 1 ? 1d / firstCount : 0;
                if (string.IsNullOrWhiteSpace(p.Slot) || string.IsNullOrWhiteSpace(p.Hero) || (!p.IsHuman && string.IsNullOrWhiteSpace(p.AiPolicy)) ||
                    p.Rank < 1 || p.Rank > 4 || p.MaxHp <= 0 || p.Hp < 0 || p.Hp > p.MaxHp || p.Laps < 0 || p.Laps > 3 ||
                    !double.IsFinite(p.FirstPlaceShare) || Math.Abs(p.FirstPlaceShare - share) > 1e-12 ||
                    !double.IsFinite(p.HpRatio) || Math.Abs(p.HpRatio - p.Hp / (double)p.MaxHp) > 1e-12)
                    throw new ArgumentException("Inconsistent participant result.", nameof(batch));
            }
            var currentRoster = string.Join(";", match.Participants.Where(p => !p.IsHuman).OrderBy(p => p.Slot, StringComparer.Ordinal)
                .Select(p => p.Slot + "/" + p.Hero + "/" + p.AiPolicy));
            if (roster != null && roster != currentRoster) throw new ArgumentException("Mixed AI roster.", nameof(batch));
            roster = currentRoster;
        }
    }

    static ParticipantSummary Participants(string scenarioHero, string slot, string hero, string policy, ParticipantResult[] rows, int failed, int notRun)
    {
        var result = new ParticipantSummary
        {
            ScenarioHero = scenarioHero, Slot = slot, Hero = hero, AiPolicy = policy,
            SampleCount = rows.Length, FailedMatches = failed, NotRunMatches = notRun,
            Rates = new()
            {
                ["trialSucceeded"] = Rate(rows.Count(p => p.TrialSucceeded), rows.Length),
                ["eliminated"] = Rate(rows.Count(p => p.Eliminated), rows.Length),
                ["rank1"] = Rate(rows.Count(p => p.Rank == 1), rows.Length)
            },
            Frequencies = new()
            {
                ["rank"] = Frequency(rows.Select(p => Invariant(p.Rank)), Enumerable.Range(1, 4).Select(Invariant)),
                ["laps"] = Frequency(rows.Select(p => Invariant(p.Laps)), Enumerable.Range(0, 4).Select(Invariant)),
                ["eliminationReason"] = Frequency(rows.Select(p => p.Eliminated ? p.EliminationReason ?? "unspecified" : "not-eliminated")),
                ["finalTrialStatus"] = Frequency(rows.Select(p => p.TrialSucceeded ? "succeeded" : p.Laps != 3 ? "not-reached" :
                    p.Eliminated ? "eliminated-at-final-trial" : "unfinished-at-final-trial"),
                    new[] { "succeeded", "eliminated-at-final-trial", "unfinished-at-final-trial", "not-reached" })
            }
        };
        foreach (var (name, value) in ParticipantMetrics()) result.Distributions[name] = Describe(rows.Select(value));
        result.Distributions["maxEncounterTurns"] = Describe(rows.Where(p => p.MaxEncounterTurns.HasValue).Select(p => (double)p.MaxEncounterTurns.Value));
        result.Distributions["maxEncounterTurns"].MissingCount = rows.Count(p => !p.MaxEncounterTurns.HasValue);
        return result;
    }

    static (string Name, Func<ParticipantResult, double> Value)[] ParticipantMetrics() => new (string, Func<ParticipantResult, double>)[]
    {
        ("fractional1st", p => p.FirstPlaceShare), ("rank", p => p.Rank), ("laps", p => p.Laps),
        ("hp", p => p.Hp), ("maxHp", p => p.MaxHp), ("hpRatio", p => p.HpRatio),
        ("level", p => p.Level), ("xp", p => p.Xp), ("honor", p => p.Honor), ("gold", p => p.Gold)
    };

    static void AddPaired(SimulationSummary summary, MatchResult[] valid, int iterations, uint masterSeed)
    {
        var heroes = summary.Manifest.Heroes;
        var blocks = valid.GroupBy(x => x.Seed).Where(g => g.Count() == heroes.Length)
            .OrderBy(g => g.Key).Select(g => g.ToDictionary(x => x.Hero, StringComparer.Ordinal)).ToArray();
        var metrics = ParticipantMetrics().Select(m => (m.Name, Value: (Func<MatchResult, double>)(x => m.Value(x.Participants.Single(p => p.IsHuman))))).ToList();
        metrics.AddRange(new (string, Func<MatchResult, double>)[]
        {
            ("trialSucceeded", x => x.Participants.Single(p => p.IsHuman).TrialSucceeded ? 1 : 0),
            ("eliminated", x => x.Participants.Single(p => p.IsHuman).Eliminated ? 1 : 0),
            ("rank1", x => x.Participants.Single(p => p.IsHuman).Rank == 1 ? 1 : 0),
            ("worldRound", x => x.WorldRound), ("humanCommands", x => x.HumanCommands)
        });
        for (int left = 0; left < heroes.Length; left++)
            for (int right = left + 1; right < heroes.Length; right++)
                foreach (var metric in metrics)
                {
                    var differences = blocks.Select(b => metric.Value(b[heroes[left]]) - metric.Value(b[heroes[right]])).ToArray();
                    summary.Paired.Add(new()
                    {
                        LeftHero = heroes[left], RightHero = heroes[right], Metric = metric.Name, BlockCount = blocks.Length,
                        ExcludedBlocks = summary.Manifest.Seeds.Length - blocks.Length,
                        MeanDifference = differences.Length == 0 ? null : differences.Average(),
                        Ci95 = Bootstrap(differences, iterations, DerivedSeed(masterSeed, heroes[left] + "|" + heroes[right] + "|" + metric.Name))
                    });
                }
    }

    static ConfidenceInterval Bootstrap(double[] values, int iterations, uint seed)
    {
        if (values.Length == 0) return null;
        if (values.All(x => x == values[0])) return new() { Lower = values[0], Upper = values[0] };
        var means = new double[iterations];
        uint state = seed, bound = (uint)values.Length, threshold = unchecked(0u - bound) % bound;
        for (int i = 0; i < iterations; i++)
        {
            double sum = 0;
            for (int j = 0; j < values.Length; j++)
            {
                uint random;
                do { state ^= state << 13; state ^= state >> 17; state ^= state << 5; random = state; } while (random < threshold);
                sum += values[random % bound];
            }
            means[i] = sum / values.Length;
        }
        Array.Sort(means);
        return new() { Lower = Percentile(means, .025), Upper = Percentile(means, .975) };
    }

    static uint DerivedSeed(uint seed, string key)
    {
        uint hash = 2166136261u ^ seed;
        foreach (char c in key) { hash ^= c; hash = unchecked(hash * 16777619u); }
        return hash == 0 ? 1u : hash;
    }

    static HumanObservationSummary Observations(string hero, MatchResult[] matches, int failed, int notRun)
    {
        var measured = matches.Where(x => x.Observations?.Measured == true).Select(x => x.Observations).ToArray();
        var result = new HumanObservationSummary
        {
            Hero = hero, Status = measured.Length == 0 ? "NOT_MEASURED" : measured.Length == matches.Length ? "MEASURED" : "PARTIAL",
            MeasuredMatches = measured.Length, MissingMatches = matches.Length - measured.Length, FailedMatches = failed, NotRunMatches = notRun
        };
        if (measured.Length == 0) return result;
        result.Totals = SumCounters(measured,
            ("combatEntries", x => x.CombatEntries), ("combatTurns", x => x.CombatTurns),
            ("eventGoldCost", x => x.EventGoldCost), ("eventHpCost", x => x.EventHpCost),
            ("eventGoldReward", x => x.EventGoldReward), ("eventXpReward", x => x.EventXpReward),
            ("eventHonorReward", x => x.EventHonorReward), ("eventHealOffered", x => x.EventHealOffered),
            ("combatHpDamageTaken", x => x.CombatHpDamageTaken), ("combatDamageBlocked", x => x.CombatDamageBlocked));
        result.Counts = new()
        {
            ["commandsByKind"] = SumDictionary(measured.Select(x => x.CommandsByKind)),
            ["offeredByKind"] = SumDictionary(measured.Select(x => x.OfferedByKind)),
            ["legalOfferedByKind"] = SumDictionary(measured.Select(x => x.LegalOfferedByKind)),
            ["selectedByKind"] = SumDictionary(measured.Select(x => x.SelectedByKind)),
            ["selectedById"] = SumDictionary(measured.Select(x => x.SelectedById)),
            ["lapRewardsById"] = SumDictionary(measured.Select(x => x.LapRewardsById)),
            ["equipmentGainedById"] = SumDictionary(measured.Select(x => x.EquipmentGainedById)),
            ["diceGainedById"] = SumDictionary(measured.Select(x => x.DiceGainedById))
        };
        foreach (var group in measured.SelectMany(x => x.Skills).GroupBy(x => x.Key, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var rows = group.Select(x => x.Value).ToArray();
            result.Skills[group.Key] = new()
            {
                Counters = SumCounters(rows, ("applied", x => x.Applied), ("skippedEnemyDefeated", x => x.SkippedEnemyDefeated),
                    ("hpDamage", x => x.HpDamage), ("shieldAbsorbed", x => x.ShieldAbsorbed), ("blockGenerated", x => x.BlockGenerated)),
                HpDamage = Describe(rows.SelectMany(x => x.AppliedHpDamageSamples).Select(x => (double)x))
            };
            if (result.MissingMatches > 0) result.Skills[group.Key].HpDamage.MissingCount = null;
        }
        foreach (var group in measured.SelectMany(x => x.DamageTypes).GroupBy(x => x.Key, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
            result.DamageTypes[group.Key] = SumCounters(group.Select(x => x.Value), ("applied", x => x.Applied),
                ("hpDamage", x => x.HpDamage), ("shieldAbsorbed", x => x.ShieldAbsorbed));
        foreach (var group in measured.SelectMany(x => x.Enemies).GroupBy(x => x.Key, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
            result.Enemies[group.Key] = SumCounters(group.Select(x => x.Value), ("entries", x => x.Entries), ("turns", x => x.Turns),
                ("defeated", x => x.Defeated), ("hpDamage", x => x.HpDamage), ("shieldAbsorbed", x => x.ShieldAbsorbed),
                ("retaliationHpDamage", x => x.RetaliationHpDamage), ("retaliationBlocked", x => x.RetaliationBlocked));
        return result;
    }

    static Dictionary<string, long> SumCounters<T>(IEnumerable<T> source, params (string Name, Func<T, int> Value)[] metrics)
    {
        var rows = source.ToArray();
        return metrics.ToDictionary(x => x.Name, x => rows.Sum(row => (long)x.Value(row)), StringComparer.Ordinal);
    }

    static Dictionary<string, long> SumDictionary(IEnumerable<Dictionary<string, int>> source) => source.SelectMany(x => x)
        .GroupBy(x => x.Key, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal)
        .ToDictionary(x => x.Key, x => x.Sum(v => (long)v.Value), StringComparer.Ordinal);

    static Dictionary<string, int> Frequency(IEnumerable<string> source, IEnumerable<string> knownValues = null)
    {
        var counts = source.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        foreach (var value in knownValues ?? Array.Empty<string>()) counts.TryAdd(value, 0);
        return counts.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    static RateSummary Rate(int count, int total) => new() { Count = count, Denominator = total, Estimate = total == 0 ? null : count / (double)total, Ci95 = Wilson95(count, total) };
    static double Percentile(double[] sorted, double p) => sorted[Math.Clamp((int)Math.Ceiling(p * sorted.Length) - 1, 0, sorted.Length - 1)];
    static string Invariant(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class ConfidenceInterval
{
    public double Lower { get; set; }
    public double Upper { get; set; }
}

public sealed class RateSummary
{
    public int Count { get; set; }
    public int Denominator { get; set; }
    public double? Estimate { get; set; }
    public ConfidenceInterval Ci95 { get; set; }
}

public sealed class DistributionSummary
{
    public int SampleCount { get; set; }
    public int? MissingCount { get; set; } = 0;
    public double? Mean { get; set; }
    public double? Min { get; set; }
    public double? P05 { get; set; }
    public double? P25 { get; set; }
    public double? P50 { get; set; }
    public double? P75 { get; set; }
    public double? P95 { get; set; }
    public double? Max { get; set; }
}

public sealed class ParticipantSummary
{
    public string ScenarioHero { get; set; }
    public string Slot { get; set; }
    public string Hero { get; set; }
    public string AiPolicy { get; set; }
    public int SampleCount { get; set; }
    public int FailedMatches { get; set; }
    public int NotRunMatches { get; set; }
    public Dictionary<string, RateSummary> Rates { get; set; } = new();
    public Dictionary<string, DistributionSummary> Distributions { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> Frequencies { get; set; } = new();
}

public sealed class MatchSummary
{
    public string Hero { get; set; }
    public int SampleCount { get; set; }
    public int FailedMatches { get; set; }
    public int NotRunMatches { get; set; }
    public Dictionary<string, DistributionSummary> Distributions { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> Frequencies { get; set; } = new();
}

public sealed class PairedDifference
{
    public string LeftHero { get; set; }
    public string RightHero { get; set; }
    public string Metric { get; set; }
    public int BlockCount { get; set; }
    public int ExcludedBlocks { get; set; }
    public double? MeanDifference { get; set; }
    public ConfidenceInterval Ci95 { get; set; }
}

public sealed class MeasurementGap
{
    public string Metric { get; set; }
    public string Status { get; set; } = "NOT_MEASURED";
    public int MissingCount { get; set; }
    public double? Value { get; set; }
    public string Reason { get; set; }
}

public sealed class SkillSummary
{
    public Dictionary<string, long> Counters { get; set; } = new();
    public DistributionSummary HpDamage { get; set; }
}

public sealed class HumanObservationSummary
{
    public string Hero { get; set; }
    public string Status { get; set; }
    public int MeasuredMatches { get; set; }
    public int MissingMatches { get; set; }
    public int FailedMatches { get; set; }
    public int NotRunMatches { get; set; }
    public Dictionary<string, long> Totals { get; set; } = new();
    public Dictionary<string, Dictionary<string, long>> Counts { get; set; } = new();
    public Dictionary<string, SkillSummary> Skills { get; set; } = new();
    public Dictionary<string, Dictionary<string, long>> DamageTypes { get; set; } = new();
    public Dictionary<string, Dictionary<string, long>> Enemies { get; set; } = new();
}

public sealed class SimulationSummary
{
    public string Status { get; set; }
    public ScenarioManifest Manifest { get; set; }
    public int RequestedMatches { get; set; }
    public int ExecutedMatches { get; set; }
    public int SuccessfulMatches { get; set; }
    public int FailedMatches { get; set; }
    public int NotRunMatches { get; set; }
    public RateSummary CompletionRate { get; set; }
    public int BootstrapIterations { get; set; }
    public uint BootstrapSeed { get; set; }
    public string BootstrapMethod { get; set; }
    public List<ParticipantSummary> Humans { get; set; } = new();
    public List<ParticipantSummary> AiGroups { get; set; } = new();
    public List<MatchSummary> MatchGroups { get; set; } = new();
    public List<HumanObservationSummary> HumanDetails { get; set; } = new();
    public List<PairedDifference> Paired { get; set; } = new();
    public List<MeasurementGap> Unmeasured { get; set; } = new();
    public PerformanceMetrics Performance { get; set; }
    public string[] InterpretationNotes { get; set; }
}
