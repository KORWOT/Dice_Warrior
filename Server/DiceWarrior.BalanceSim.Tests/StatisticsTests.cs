using System.Globalization;
using System.Text.Json;
using NUnit.Framework;

namespace DiceWarrior.BalanceSim.Tests;

public sealed class StatisticsTests
{
    [TestCase(0, 10, 0, .2775327999)]
    [TestCase(5, 10, .2365930905, .7634069095)]
    [TestCase(10, 10, .7224672001, 1)]
    public void WilsonIntervalsRespectSmallSampleBoundaries(int successes, int n, double low, double high)
    {
        var interval = Statistics.Wilson95(successes, n);
        Assert.That(interval.Lower, Is.EqualTo(low).Within(.00000001));
        Assert.That(interval.Upper, Is.EqualTo(high).Within(.00000001));
    }

    [Test]
    public void EmptySamplesAreMissingAndImpossibleBinomialCountsAreRejected()
    {
        Assert.That(Statistics.Wilson95(0, 0), Is.Null);
        Assert.Throws<ArgumentOutOfRangeException>(() => Statistics.Wilson95(2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Statistics.Wilson95(-1, 2));
        var empty = Statistics.Describe(Array.Empty<double>());
        Assert.That(empty.SampleCount, Is.Zero);
        Assert.That(empty.Mean, Is.Null);
        Assert.That(empty.P95, Is.Null);
    }

    [Test]
    public void DistributionUsesNearestRankAndRejectsNonFiniteValues()
    {
        var summary = Statistics.Describe(Enumerable.Range(1, 100).Select(i => (double)i));
        Assert.That(summary.Mean, Is.EqualTo(50.5));
        Assert.That(summary.P05, Is.EqualTo(5));
        Assert.That(summary.P25, Is.EqualTo(25));
        Assert.That(summary.P50, Is.EqualTo(50));
        Assert.That(summary.P75, Is.EqualTo(75));
        Assert.That(summary.P95, Is.EqualTo(95));
        Assert.Throws<ArgumentException>(() => Statistics.Describe(new[] { double.NaN }));
    }

    [Test]
    public void TiedFirstPlaceIsDistinctFromTrialSuccessAndFractionalShare()
    {
        var batch = Batch(1);
        var match = batch.Matches[0];
        match.Participants[1].Rank = 1;
        match.Participants[0].FirstPlaceShare = .5;
        match.Participants[1].FirstPlaceShare = .5;
        var summary = Statistics.BuildSummary(batch, 100);
        var human = summary.Humans.Single(x => x.Hero == "wanderer");
        Assert.That(human.Rates["rank1"].Count, Is.EqualTo(1));
        Assert.That(human.Rates["trialSucceeded"].Count, Is.Zero);
        Assert.That(human.Distributions["fractional1st"].Mean, Is.EqualTo(.5));
        Assert.That(summary.AiGroups.Count, Is.EqualTo(9));
        Assert.That(summary.AiGroups.All(x => x.SampleCount == 1), Is.True);
    }

    [Test]
    public void FailureAndNotRunNeverBecomeParticipantLossesOrMissingZeros()
    {
        var batch = Batch(2);
        batch.Status = "BLOCKED";
        batch.Matches[4] = new MatchResult { Seed = 2, Hero = "scholar", Status = "BLOCKED", Failure = new() { Reason = "limit" } };
        batch.Matches.RemoveAt(5);
        var summary = Statistics.BuildSummary(batch, 100);
        Assert.That(summary.ExecutedMatches, Is.EqualTo(5));
        Assert.That(summary.SuccessfulMatches, Is.EqualTo(4));
        Assert.That(summary.FailedMatches, Is.EqualTo(1));
        Assert.That(summary.NotRunMatches, Is.EqualTo(1));
        Assert.That(summary.CompletionRate.Denominator, Is.EqualTo(5));
        Assert.That(summary.Humans.Single(x => x.Hero == "scholar").Rates["rank1"].Denominator, Is.EqualTo(1));
        Assert.That(summary.Humans.Single(x => x.Hero == "scholar").FailedMatches, Is.EqualTo(1));
        Assert.That(summary.Unmeasured.All(x => x.Status == "NOT_MEASURED" && x.Value == null), Is.True);
        Assert.That(summary.Unmeasured.Any(x => x.Metric == "ai-full-skill-history"), Is.True);
    }

    [Test]
    public void PairedDifferencesUseOnlyCompleteSeedBlocksAndKeepDirection()
    {
        var batch = Batch(2);
        foreach (var match in batch.Matches)
        {
            var human = match.Participants[0];
            human.Hp = match.Hero == "wanderer" ? 20 : 10;
            human.HpRatio = human.Hp / 40d;
        }
        batch.Matches.RemoveAt(5);
        batch.Status = "BLOCKED";
        var summary = Statistics.BuildSummary(batch, 100);
        var pair = summary.Paired.Single(x => x.LeftHero == "wanderer" && x.RightHero == "scholar" && x.Metric == "hp");
        Assert.That(pair.BlockCount, Is.EqualTo(1));
        Assert.That(pair.ExcludedBlocks, Is.EqualTo(1));
        Assert.That(pair.MeanDifference, Is.EqualTo(10));
        Assert.That(pair.Ci95.Lower, Is.EqualTo(10));
        Assert.That(pair.Ci95.Upper, Is.EqualTo(10));
    }

    [Test]
    public void BootstrapIsDeterministicUnderInputOrderAndDoesNotMutateRawResults()
    {
        var batch = Batch(5);
        foreach (var match in batch.Matches)
        {
            match.Participants[0].Hp = match.Hero == "wanderer" ? 5 + (int)match.Seed * 3 : 10;
            match.Participants[0].HpRatio = match.Participants[0].Hp / 40d;
        }
        var original = JsonSerializer.Serialize(batch.Matches);
        var first = Statistics.BuildSummary(batch, 200);
        Assert.That(JsonSerializer.Serialize(batch.Matches), Is.EqualTo(original));
        batch.Matches.Reverse();
        var second = Statistics.BuildSummary(batch, 200);
        Assert.That(JsonSerializer.Serialize(first.Paired), Is.EqualTo(JsonSerializer.Serialize(second.Paired)));
        var pair = first.Paired.Single(x => x.LeftHero == "wanderer" && x.RightHero == "scholar" && x.Metric == "hp");
        Assert.That(pair.MeanDifference, Is.EqualTo(4));
        Assert.That(pair.Ci95.Lower, Is.LessThan(pair.Ci95.Upper));
    }

    [Test]
    public void InvalidRawResultsCannotSilentlyDistortSummary()
    {
        var duplicate = Batch(1);
        duplicate.Matches[2] = duplicate.Matches[1];
        Assert.Throws<ArgumentException>(() => Statistics.BuildSummary(duplicate, 100));
        var invalidShare = Batch(1);
        invalidShare.Matches[0].Participants[0].FirstPlaceShare = .25;
        Assert.Throws<ArgumentException>(() => Statistics.BuildSummary(invalidShare, 100));
        var invalidHp = Batch(1);
        invalidHp.Matches[0].Participants[0].HpRatio = .99;
        Assert.Throws<ArgumentException>(() => Statistics.BuildSummary(invalidHp, 100));
    }

    [Test]
    public void HumanSkillContributionSeparatesActualDamageAbsorptionSkippedAndUnmeasuredMatches()
    {
        var batch = Batch(2);
        batch.Matches[0].Observations = new HumanObservations
        {
            Skills = new() { ["slash"] = new() { Applied = 2, SkippedEnemyDefeated = 1, HpDamage = 8, ShieldAbsorbed = 5, BlockGenerated = 3, AppliedHpDamageSamples = new() { 2, 6 } } },
            DamageTypes = new() { ["physical"] = new() { Applied = 2, HpDamage = 8, ShieldAbsorbed = 5 } },
            Enemies = new() { ["guardian"] = new() { Entries = 1, Turns = 2, Defeated = 1, HpDamage = 8, ShieldAbsorbed = 5 } },
            SelectedByKind = new() { ["combat"] = 1 }, CombatTurns = 2
        };
        batch.Matches[3].Observations = new HumanObservations { Measured = false };
        var summary = Statistics.BuildSummary(batch, 100);
        var details = summary.HumanDetails.Single(x => x.Hero == "wanderer");
        Assert.That(details.MeasuredMatches, Is.EqualTo(1));
        Assert.That(details.MissingMatches, Is.EqualTo(1));
        Assert.That(details.Skills["slash"].Counters["hpDamage"], Is.EqualTo(8));
        Assert.That(details.Skills["slash"].Counters["shieldAbsorbed"], Is.EqualTo(5));
        Assert.That(details.Skills["slash"].Counters["skippedEnemyDefeated"], Is.EqualTo(1));
        Assert.That(details.Skills["slash"].HpDamage.Mean, Is.EqualTo(4));
        Assert.That(details.DamageTypes["physical"]["hpDamage"], Is.EqualTo(8));
        Assert.That(details.Enemies["guardian"]["defeated"], Is.EqualTo(1));
        Assert.That(summary.HumanDetails.Single(x => x.Hero == "scholar").Status, Is.EqualTo("NOT_MEASURED"));
    }

    [Test]
    public void HpTiebreakExposureAndFinalTrialFailureAreExplicitFrequencies()
    {
        var batch = Batch(1);
        var match = batch.Matches[0];
        var human = match.Participants[0];
        human.Laps = 3;
        human.Eliminated = true;
        human.Hp = 0;
        human.HpRatio = 0;
        match.Participants[1].Laps = 3;
        match.Participants[1].Rank = 1;
        human.FirstPlaceShare = .5;
        match.Participants[1].FirstPlaceShare = .5;
        var summary = Statistics.BuildSummary(batch, 100);
        var group = summary.MatchGroups.Single(x => x.Hero == "wanderer");
        Assert.That(group.Frequencies["humanHpTiebreakExposure"]["yes"], Is.EqualTo(1));
        Assert.That(group.Frequencies["hasRankTie"]["yes"], Is.EqualTo(1));
        Assert.That(summary.Humans.Single(x => x.Hero == "wanderer").Frequencies["finalTrialStatus"]["eliminated-at-final-trial"], Is.EqualTo(1));
        Assert.That(summary.AiGroups.Single(x => x.ScenarioHero == "wanderer" && x.Slot == "ai-1").Frequencies["finalTrialStatus"]["unfinished-at-final-trial"], Is.EqualTo(1));
    }

    [Test]
    public void BatchVerificationFailureCannotBeOverwrittenByCompletedMatchCounts()
    {
        var batch = Batch(1);
        batch.Status = "BLOCKED";
        var summary = Statistics.BuildSummary(batch, 100);
        Assert.That(summary.SuccessfulMatches, Is.EqualTo(3));
        Assert.That(summary.FailedMatches, Is.Zero);
        Assert.That(summary.Status, Is.EqualTo("BLOCKED"));
    }

    [Test]
    public void ReportRoundTripPreservesMissingValuesCultureAndExistingEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(), "DiceWarriorBalanceReport-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            File.WriteAllText(Path.Combine(root, "manifest.json"), "preserved");
            var batch = Batch(1);
            batch.Manifest.ScenarioId = "scenario,quoted\"id";
            var summary = ReportWriter.Write(root, batch);
            Assert.That(summary.SuccessfulMatches, Is.EqualTo(3));
            Assert.That(File.ReadAllText(Path.Combine(root, "manifest.json")), Is.EqualTo("preserved"));
            Assert.That(File.ReadAllLines(Path.Combine(root, "participants.csv")).Length, Is.EqualTo(13));
            var csv = File.ReadAllText(Path.Combine(root, "participants.csv"));
            Assert.That(csv, Does.Contain("\"scenario,quoted\"\"id\""));
            Assert.That(csv, Does.Contain("0.5"));
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "summary.json")));
            Assert.That(json.RootElement.GetProperty("Unmeasured")[0].GetProperty("Value").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(File.ReadAllText(Path.Combine(root, "summary.md")), Does.Contain("NOT_MEASURED"));
            Assert.Throws<IOException>(() => ReportWriter.Write(root, batch));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            foreach (var file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }
    }

    static BatchResult Batch(int seeds)
    {
        var heroes = new[] { "wanderer", "scholar", "vanguard" };
        var batch = new BatchResult
        {
            Manifest = new() { ScenarioId = "synthetic", PolicyId = "neutral-v1", Heroes = heroes, Seeds = Enumerable.Range(1, seeds).Select(i => (uint)i).ToArray() },
            RequestedMatches = seeds * 3, Status = "PASS"
        };
        foreach (var seed in batch.Manifest.Seeds)
            foreach (var hero in heroes)
            {
                var match = new MatchResult { Seed = seed, Hero = hero, Status = "PASS", EndReason = "round-limit", WorldRound = 30, HumanCommands = 60 };
                for (var slot = 0; slot < 4; slot++)
                    match.Participants.Add(new ParticipantResult
                    {
                        Slot = slot == 0 ? "human" : "ai-" + slot, Hero = slot == 0 ? hero : heroes[slot - 1],
                        AiPolicy = slot == 0 ? null : new[] { "war", "wisdom", "luck" }[slot - 1], IsHuman = slot == 0,
                        Rank = slot + 1, FirstPlaceShare = slot == 0 ? 1 : 0, Hp = 20, MaxHp = 40, HpRatio = .5, Level = 1,
                        MaxEncounterTurns = slot == 0 ? 3 : null
                    });
                batch.Matches.Add(match);
            }
        return batch;
    }
}
