using NUnit.Framework;

namespace DiceWarrior.BalanceSim.Tests;

public sealed class ScenarioTests
{
    static string Content => Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-4.json");

    [Test]
    public void CliRejectsMissingAndUnknownArgumentsWithoutStartingMatches()
    {
        Assert.That(Program.Main(new[] { "--unknown", "true" }), Is.EqualTo(2));
        Assert.That(Program.Main(new[] { "--output" }), Is.EqualTo(2));
    }

    [Test]
    public void SeedsAreUniqueNonzeroStableAndPrefixCompatible()
    {
        var first = BalanceScenario.GenerateSeeds(BalanceScenario.DefaultMasterSeed, 1000);
        Assert.That(first.Distinct().Count(), Is.EqualTo(1000));
        Assert.That(first, Does.Not.Contain(0u));
        Assert.That(first.Take(3), Is.EqualTo(BalanceScenario.GenerateSeeds(BalanceScenario.DefaultMasterSeed, 3)));
        Assert.That(BalanceScenario.SeedHash(first), Is.EqualTo(BalanceScenario.SeedHash(BalanceScenario.GenerateSeeds(BalanceScenario.DefaultMasterSeed, 1000))));
        Assert.Throws<ArgumentException>(() => BalanceScenario.GenerateSeeds(0, 3));
        Assert.Throws<ArgumentException>(() => BalanceScenario.GenerateSeeds(1, 0));
    }

    [Test]
    public void CanonicalContentAndSupportedInputsAreRequired()
    {
        var s = new BalanceScenario();
        var d = s.LoadDefinition(Content);
        Assert.DoesNotThrow(() => s.Validate(d, new uint[] { 1, 2, 3 }));
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 0 }));
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1, 1 }));
        Assert.Throws<ArgumentException>(() => s.Validate(d, Array.Empty<uint>()));
        s.PolicyId = "best-future-rng";
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1 }));
        s.PolicyId = "neutral-v1";
        s.Heroes = new[] { "unknown" };
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1 }));
        s.Heroes = new[] { "wanderer", "scholar", "vanguard" };
        s.Profile.startingDie = "unknown";
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1 }));
        s.Profile.startingDie = "balanced";
        s.Profile.startingWeapon = "unknown";
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1 }));
        s.Profile.startingWeapon = "blade";
        d.maxRounds--;
        Assert.Throws<ArgumentException>(() => s.Validate(d, new uint[] { 1 }));
        Assert.Throws<FileNotFoundException>(() => s.LoadDefinition(Content + ".missing"));
        var altered = Path.Combine(TestContext.CurrentContext.WorkDirectory, "invalid-content-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(altered, "{malformed");
            Assert.Throws<ArgumentException>(() => s.LoadDefinition(altered));
            File.WriteAllText(altered, File.ReadAllText(Content) + " ");
            Assert.Throws<ArgumentException>(() => s.LoadDefinition(altered));
        }
        finally { File.Delete(altered); }
    }

    [Test]
    public void OutputRejectsReuseOutsideRootsAndTraversal()
    {
        var repo = Path.Combine(TestContext.CurrentContext.WorkDirectory, "output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repo, "artifacts", "balance-sim"));
        try
        {
            var target = Path.Combine(repo, "artifacts", "balance-sim", "new");
            Assert.That(BalanceScenario.PrepareOutput(repo, target), Is.EqualTo(target));
            Assert.Throws<IOException>(() => BalanceScenario.PrepareOutput(repo, target));
            Assert.Throws<ArgumentException>(() => BalanceScenario.PrepareOutput(repo, Path.Combine(repo, "outside")));
            Assert.Throws<ArgumentException>(() => BalanceScenario.PrepareOutput(repo, Path.Combine(repo, "artifacts", "balance-sim", "..", "escape")));
        }
        finally { Directory.Delete(repo, true); }
    }
}
