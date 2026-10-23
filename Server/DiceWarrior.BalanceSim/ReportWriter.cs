using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DiceWarrior.BalanceSim;

public static class ReportWriter
{
    static readonly string[] FileNames = { "summary.json", "summary.md", "matches.csv", "participants.csv", "failures.jsonl" };
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static SimulationSummary Write(string outputDirectory, BatchResult batch)
    {
        if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException(outputDirectory);
        foreach (var name in FileNames)
            if (File.Exists(Path.Combine(outputDirectory, name)) || Directory.Exists(Path.Combine(outputDirectory, name)))
                throw new IOException("Refusing to overwrite report evidence: " + name);
        var summary = Statistics.BuildSummary(batch);
        WriteNew(Path.Combine(outputDirectory, "summary.json"), JsonSerializer.Serialize(summary, JsonOptions) + "\n");
        WriteNew(Path.Combine(outputDirectory, "summary.md"), Markdown(summary));
        var ordered = batch.Matches.OrderBy(x => x.Seed).ThenBy(x => x.Hero, StringComparer.Ordinal).ToArray();
        var matches = new StringBuilder();
        Csv(matches, "scenarioId", "seed", "hero", "status", "endReason", "worldRound", "humanCommands", "trialWinnerCount", "rank1Count", "elapsedMs", "stateHash", "rngHash", "commandHash", "failureReason");
        foreach (var m in ordered)
            Csv(matches, batch.Manifest.ScenarioId, m.Seed, m.Hero, m.Status, m.Status == "PASS" ? m.EndReason : null,
                m.Status == "PASS" ? m.WorldRound : null, m.HumanCommands, m.Status == "PASS" ? m.TrialWinnerCount : null,
                m.Status == "PASS" ? m.Participants.Count(p => p.Rank == 1) : null, m.ElapsedMs, m.StateHash, m.RngHash, m.CommandHash, m.Failure?.Reason);
        WriteNew(Path.Combine(outputDirectory, "matches.csv"), matches.ToString());
        var participants = new StringBuilder();
        Csv(participants, "scenarioId", "seed", "scenarioHero", "slot", "hero", "aiPolicy", "isHuman", "trialSucceeded", "eliminated", "eliminationReason", "finished", "rank", "fractional1st", "laps", "hp", "maxHp", "hpRatio", "level", "xp", "honor", "gold", "maxEncounterTurns", "maxEncounterTurnsStatus");
        foreach (var m in ordered.Where(x => x.Status == "PASS"))
            foreach (var p in m.Participants.OrderBy(x => x.Slot, StringComparer.Ordinal))
                Csv(participants, batch.Manifest.ScenarioId, m.Seed, m.Hero, p.Slot, p.Hero, p.AiPolicy, p.IsHuman, p.TrialSucceeded,
                    p.Eliminated, p.EliminationReason, p.Finished, p.Rank, p.FirstPlaceShare, p.Laps, p.Hp, p.MaxHp, p.HpRatio,
                    p.Level, p.Xp, p.Honor, p.Gold, p.MaxEncounterTurns, p.MaxEncounterTurns.HasValue ? "MEASURED" : "NOT_MEASURED");
        WriteNew(Path.Combine(outputDirectory, "participants.csv"), participants.ToString());
        var failures = new StringBuilder();
        foreach (var m in ordered.Where(x => x.Status != "PASS"))
            failures.Append(JsonSerializer.Serialize(new
            {
                SchemaVersion = 1, batch.Manifest.ScenarioId, batch.Manifest.PolicyId, m.Seed, m.Hero, m.Status,
                m.HumanCommands, m.Failure, batch.Manifest.Runtime, batch.Manifest.Os
            })).Append('\n');
        WriteNew(Path.Combine(outputDirectory, "failures.jsonl"), failures.ToString());
        return summary;
    }

    static string Markdown(SimulationSummary s)
    {
        var text = new StringBuilder();
        text.AppendLine("# B0/C7 balance simulation summary").AppendLine();
        text.AppendLine($"Status: **{s.Status}**. Requested {s.RequestedMatches}; executed {s.ExecutedMatches}; valid completed {s.SuccessfulMatches}; failed {s.FailedMatches}; NOT_RUN {s.NotRunMatches}.").AppendLine();
        text.AppendLine($"Scenario `{s.Manifest.ScenarioId}`; policy `{s.Manifest.PolicyId}`; rules `{s.Manifest.RulesVersion}`; Git `{s.Manifest.GitSha}`.");
        text.AppendLine($"Content SHA256 `{s.Manifest.ContentSha256}`; seed-set SHA256 `{s.Manifest.SeedSetSha256}`; source SHA256 `{s.Manifest.RunnerSourceSha256}`.");
        text.AppendLine($"Growth {s.Manifest.Growth}; die `{s.Manifest.StartingDie}`; weapon `{s.Manifest.StartingWeapon}`; master seed {s.Manifest.MasterSeed}.").AppendLine();
        text.AppendLine($"Completion per attempted match: {Percent(s.CompletionRate.Estimate)}; Wilson 95% {Interval(s.CompletionRate.Ci95, true)}; n={s.CompletionRate.Denominator}, failed={s.FailedMatches}, NOT_RUN={s.NotRunMatches}, missing=0.").AppendLine();
        text.AppendLine("## Interpretation contract").AppendLine();
        foreach (var note in s.InterpretationNotes) text.Append("- ").AppendLine(note);
        text.AppendLine().AppendLine("## Human final outcomes").AppendLine();
        foreach (var group in s.Humans) ParticipantTables(text, group);
        text.AppendLine("## AI fixed combinations, stratified by human scenario").AppendLine();
        text.AppendLine("AI adventurer and policy cannot be separated by this experiment. No pooled independent-participant win rate is estimated.").AppendLine();
        foreach (var group in s.AiGroups) ParticipantTables(text, group);
        text.AppendLine("## Match distributions").AppendLine();
        foreach (var group in s.MatchGroups)
        {
            text.AppendLine($"### Human {group.Hero}").AppendLine();
            DistributionTable(text, group.Distributions, group.FailedMatches, group.NotRunMatches);
            FrequencyTable(text, group.Frequencies, group.SampleCount, group.FailedMatches, group.NotRunMatches);
        }
        text.AppendLine("## Paired seed-block comparisons").AppendLine();
        text.AppendLine($"{s.BootstrapMethod}; replicates={s.BootstrapIterations}; master bootstrap seed={s.BootstrapSeed}. Positive values mean left minus right. All three hero matches must be valid for a block to enter any comparison. Proportion differences below are fractions, so 0.01 means one percentage point.").AppendLine();
        text.AppendLine("| Left | Right | Metric | Complete seed blocks | Excluded blocks | Mean difference | Bootstrap 95% | Batch failures | Batch NOT_RUN |");
        text.AppendLine("|---|---|---|---:|---:|---:|---|---:|---:|");
        foreach (var p in s.Paired)
            text.AppendLine($"| {p.LeftHero} | {p.RightHero} | {p.Metric} | {p.BlockCount} | {p.ExcludedBlocks} | {Number(p.MeanDifference)} | {Interval(p.Ci95)} | {s.FailedMatches} | {s.NotRunMatches} |");
        text.AppendLine().AppendLine("## Human observations").AppendLine();
        text.AppendLine("Actual enemy HP damage, enemy shield absorption, generated player block, and enemy-defeated skips are separate. EventHealOffered is the selected option's nominal healing amount, not inferred effective healing. Counts represent observed actions/effects and may exceed the match sample size.").AppendLine();
        foreach (var observations in s.HumanDetails) ObservationTables(text, observations);
        text.AppendLine("## Unmeasured metrics").AppendLine();
        text.AppendLine("| Metric | Status | Missing participant/match observations | Value | Reason |");
        text.AppendLine("|---|---|---:|---|---|");
        foreach (var missing in s.Unmeasured)
            text.AppendLine($"| {missing.Metric} | {missing.Status} | {missing.MissingCount} | null | {Escape(missing.Reason)} |");
        text.AppendLine().AppendLine($"Missing counts above refer only to valid completed matches (n={s.SuccessfulMatches}); failed={s.FailedMatches}, NOT_RUN={s.NotRunMatches} are separate.").AppendLine();
        text.AppendLine("## Performance").AppendLine();
        var pmetrics = s.Performance;
        text.AppendLine($"Measured batch attempts n={s.ExecutedMatches}; failed={s.FailedMatches}; NOT_RUN={s.NotRunMatches}. {pmetrics?.Sampling}").AppendLine();
        if (pmetrics != null)
        {
            text.AppendLine($"Elapsed {Number(pmetrics.ElapsedSeconds)} s; throughput {Number(pmetrics.MatchesPerSecond)} matches/s; per-match p50 {Number(pmetrics.MatchP50Ms)} ms, p95 {Number(pmetrics.MatchP95Ms)} ms.");
            text.AppendLine($"Peak RSS {pmetrics.PeakWorkingSetBytes} bytes; maximum observed managed heap {pmetrics.MaxObservedManagedHeapBytes} bytes; allocated {pmetrics.AllocatedBytes} bytes; GC collections [{string.Join(",", pmetrics.GcCollections ?? Array.Empty<int>())}].");
            text.AppendLine($"Raw bytes before verification/reports: {pmetrics.RawOutputBytesBeforeReports} bytes. The runner records the bytes measured before performance.json separately; final total directory bytes are recorded by the external run audit.");
        }
        text.AppendLine().AppendLine("Sensitivity experiments, independent holdout, live gameplay/UI, Unity execution, and authority/server settlement: NOT_RUN.");
        return text.ToString();
    }

    static void ParticipantTables(StringBuilder text, ParticipantSummary group)
    {
        text.AppendLine($"### {group.ScenarioHero} / {group.Slot} / {group.Hero}" + (group.AiPolicy == null ? "" : " / " + group.AiPolicy)).AppendLine();
        text.AppendLine("| Metric | Count | Denominator | Rate | Wilson 95% | Missing | Failed | NOT_RUN |");
        text.AppendLine("|---|---:|---:|---:|---|---:|---:|---:|");
        foreach (var rate in group.Rates)
            text.AppendLine($"| {rate.Key} | {rate.Value.Count} | {rate.Value.Denominator} | {Percent(rate.Value.Estimate)} | {Interval(rate.Value.Ci95, true)} | 0 | {group.FailedMatches} | {group.NotRunMatches} |");
        text.AppendLine();
        DistributionTable(text, group.Distributions, group.FailedMatches, group.NotRunMatches);
        FrequencyTable(text, group.Frequencies, group.SampleCount, group.FailedMatches, group.NotRunMatches);
    }

    static void DistributionTable(StringBuilder text, Dictionary<string, DistributionSummary> metrics, int failed, int notRun)
    {
        text.AppendLine("| Metric | n | Missing | Mean | Min | p05 | p25 | p50 | p75 | p95 | Max | Failed | NOT_RUN |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var metric in metrics)
        {
            var d = metric.Value;
            text.AppendLine($"| {metric.Key} | {d.SampleCount} | {d.MissingCount?.ToString(CultureInfo.InvariantCulture) ?? "NOT_MEASURED"} | {Number(d.Mean)} | {Number(d.Min)} | {Number(d.P05)} | {Number(d.P25)} | {Number(d.P50)} | {Number(d.P75)} | {Number(d.P95)} | {Number(d.Max)} | {failed} | {notRun} |");
        }
        text.AppendLine();
    }

    static void FrequencyTable(StringBuilder text, Dictionary<string, Dictionary<string, int>> frequencies, int n, int failed, int notRun)
    {
        text.AppendLine("| Metric | Value: count | n | Missing | Failed | NOT_RUN |");
        text.AppendLine("|---|---|---:|---:|---:|---:|");
        foreach (var frequency in frequencies)
            text.AppendLine($"| {frequency.Key} | {Escape(string.Join("; ", frequency.Value.Select(x => x.Key + ": " + x.Value)))} | {n} | 0 | {failed} | {notRun} |");
        text.AppendLine();
    }

    static void ObservationTables(StringBuilder text, HumanObservationSummary observation)
    {
        text.AppendLine($"### Human {observation.Hero}: {observation.Status}").AppendLine();
        text.AppendLine($"Measured matches={observation.MeasuredMatches}; missing={observation.MissingMatches}; failed={observation.FailedMatches}; NOT_RUN={observation.NotRunMatches}.").AppendLine();
        if (observation.MeasuredMatches == 0) { text.AppendLine("Detailed counters and damage contributions: NOT_MEASURED (null).").AppendLine(); return; }
        text.AppendLine("| Scope / ID | Counters | Measured matches | Missing matches | Failed | NOT_RUN |");
        text.AppendLine("|---|---|---:|---:|---:|---:|");
        ObservationRow(text, observation, "totals", observation.Totals);
        foreach (var group in observation.Counts) ObservationRow(text, observation, group.Key, group.Value);
        foreach (var group in observation.Skills) ObservationRow(text, observation, "skill/" + group.Key, group.Value.Counters);
        foreach (var group in observation.DamageTypes) ObservationRow(text, observation, "damageType/" + group.Key, group.Value);
        foreach (var group in observation.Enemies) ObservationRow(text, observation, "enemy/" + group.Key, group.Value);
        text.AppendLine().AppendLine("Skill damage distribution sample size is applied effect count, with skips excluded:").AppendLine();
        DistributionTable(text, observation.Skills.ToDictionary(x => x.Key, x => x.Value.HpDamage), observation.FailedMatches, observation.NotRunMatches);
    }

    static void ObservationRow(StringBuilder text, HumanObservationSummary observation, string scope, Dictionary<string, long> values) =>
        text.AppendLine($"| {Escape(scope)} | {Escape(string.Join("; ", values.Select(x => x.Key + ": " + x.Value)))} | {observation.MeasuredMatches} | {observation.MissingMatches} | {observation.FailedMatches} | {observation.NotRunMatches} |");

    static string Number(double? value) => value?.ToString("0.######", CultureInfo.InvariantCulture) ?? "NOT_MEASURED";
    static string Percent(double? value) => value.HasValue ? (value.Value * 100).ToString("0.###", CultureInfo.InvariantCulture) + "%" : "NOT_MEASURED";
    static string Interval(ConfidenceInterval value, bool percent = false) => value == null ? "NOT_MEASURED" :
        "[" + (percent ? Percent(value.Lower) : Number(value.Lower)) + ", " + (percent ? Percent(value.Upper) : Number(value.Upper)) + "]";
    static string Escape(string text) => (text ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    static void Csv(StringBuilder target, params object[] cells)
    {
        target.AppendJoin(',', cells.Select(value =>
        {
            string cell = value switch
            {
                null => "", bool b => b ? "true" : "false", double d => d.ToString("G17", CultureInfo.InvariantCulture),
                IFormattable number => number.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString()
            };
            return cell.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 ? cell : "\"" + cell.Replace("\"", "\"\"") + "\"";
        })).Append('\n');
    }

    static void WriteNew(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
