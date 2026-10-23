using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;
using NUnit.Framework;

namespace DiceWarrior.BalanceSim.Tests;

public sealed class MatchRunnerTests
{
    static string ContentPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "shared-world-preview-4.json");
    static CompetitionDefinition Definition() => CompetitionContent.Load(File.ReadAllText(ContentPath));
    static CompetitionState NewState() => CompetitionRules.Start(Definition(), new Profile(), 123u, "policy-fixture");
    static (BalanceScenario Scenario, CompetitionDefinition Definition) SetupScenario()
    {
        var scenario = new BalanceScenario();
        return (scenario, scenario.LoadDefinition(ContentPath));
    }

    [Test] public void NeutralPolicyKeepsFrozenOfferOrderForTiesAndConsumesNoState()
    {
        var state = NewState();
        CompetitionCommandRules.Apply(state, "travel", "");
        var p = CompetitionRules.Human(state);
        Assert.That(p.phase, Is.EqualTo(CompetitionPhase.Encounter));
        foreach (var c in p.offer.choices) { c.xp = 1; c.heal = 0; c.honor = 0; }
        var expected = CompetitionCommandRules.LegalChoices(state, p)[0];
        string before = LoopJson.Encode(state);
        var command = HumanDecisionPolicies.Next(state, "neutral-v1");
        Assert.That(command.kind, Is.EqualTo("choose"));
        Assert.That(command.arg, Is.EqualTo(expected.id));
        Assert.That(command.targetId, Is.EqualTo(CompetitionCommandRules.Target(state)));
        Assert.That(command.sequence, Is.EqualTo(state.actionNumber + 1));
        Assert.That(LoopJson.Encode(state), Is.EqualTo(before));
    }

    [Test] public void HealPriorityAppliesOnlyBelowHalfHpAndUnknownPoliciesAreRejected()
    {
        var state = NewState(); var p = CompetitionRules.Human(state);
        var definitions = state.definition.encounters.Where(e => e.hpCost == 0 && e.goldCost == 0 && e.requiredGearId == null && e.minHp == 0).Take(2).ToArray();
        p.phase = CompetitionPhase.Encounter;
        p.offer = new EncounterOffer { id = "policy-offer", choices = new[] {
            new EncounterChoice { id = definitions[0].id, kind = EncounterKind.Study, available = true, xp = 10 },
            new EncounterChoice { id = definitions[1].id, kind = EncounterKind.Heal, available = true, heal = 1 } } };
        p.hp = p.maxHp / 2;
        Assert.That(HumanDecisionPolicies.Next(state, "neutral-v1").arg, Is.EqualTo(definitions[0].id));
        p.hp--;
        Assert.That(HumanDecisionPolicies.Next(state, "neutral-v1").arg, Is.EqualTo(definitions[1].id));
        Assert.Throws<ArgumentException>(() => HumanDecisionPolicies.Next(state, "unknown"));
        state.finished = true;
        Assert.Throws<InvalidOperationException>(() => HumanDecisionPolicies.Next(state, "neutral-v1"));
    }

    [TestCase("wanderer")]
    [TestCase("scholar")]
    [TestCase("vanguard")]
    public void SameInputReplayRestoreAndObservationModePreserveCompleteState(string hero)
    {
        var setup = SetupScenario();
        var first = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, hero);
        var repeated = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, hero);
        var restored = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, hero, new RunOptions { RestoreAfterCommands = new[] { 1, 5, 12 } });
        var unobserved = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, hero, new RunOptions { Observe = false });
        var replay = MatchRunner.Replay(setup.Definition, setup.Scenario, 123u, hero, first.Commands);
        Assert.That(first.Result.Status, Is.EqualTo("PASS"), first.Result.Failure?.Reason);
        Assert.That(first.FinalState.finished, Is.True);
        foreach (var candidate in new[] { repeated, restored, unobserved, replay })
        {
            Assert.That(candidate.Result.Status, Is.EqualTo("PASS"), candidate.Result.Failure?.Reason);
            Assert.That(candidate.Result.StateHash, Is.EqualTo(first.Result.StateHash));
            Assert.That(candidate.Result.RngHash, Is.EqualTo(first.Result.RngHash));
            Assert.That(candidate.Result.CommandHash, Is.EqualTo(first.Result.CommandHash));
        }
        Assert.That(LoopJson.Encode(first.Result.Observations), Is.EqualTo(LoopJson.Encode(repeated.Result.Observations)));
        Assert.That(LoopJson.Encode(first.Result.Participants), Is.EqualTo(LoopJson.Encode(restored.Result.Participants)));
        Assert.That(LoopJson.Encode(first.Result.Participants), Is.EqualTo(LoopJson.Encode(unobserved.Result.Participants)));
        Assert.That(first.Result.Participants.Skip(1).Select(p => p.AiPolicy + ":" + p.Hero), Is.EqualTo(new[] { "war:wanderer", "wisdom:scholar", "luck:vanguard" }));
        Assert.That(first.Result.Participants.Skip(1).All(p => !p.MaxEncounterTurns.HasValue), Is.True);
        Assert.That(first.Result.Observations.CommandsByKind.Values.Sum(), Is.EqualTo(first.Result.HumanCommands));
        Assert.That(unobserved.Result.Observations.Measured, Is.False);
        Assert.That(first.Result.HumanCommands, Is.LessThanOrEqualTo(BackendProtocol.MaxCommands));
    }

    [Test] public void CommandLimitReportsBlockedWithPartialReplayInsteadOfSuccess()
    {
        var setup = SetupScenario(); setup.Scenario.MaxCommands = 1;
        var result = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, "wanderer");
        Assert.That(result.Result.Status, Is.EqualTo("BLOCKED"));
        Assert.That(result.Result.Failure.Reason, Is.EqualTo("command-limit"));
        Assert.That(result.Result.Failure.CommandNumber, Is.EqualTo(2));
        Assert.That(result.Commands.Count, Is.EqualTo(1));
        Assert.That(result.FinalState.finished, Is.False);
        Assert.That(result.Result.Participants, Is.Empty);
    }

    [Test] public void ReplayRejectsChangedTargetsTruncationAndCommandsAfterFinish()
    {
        var setup = SetupScenario();
        var complete = MatchRunner.Run(setup.Definition, setup.Scenario, 123u, "wanderer");
        var changed = LoopJson.Copy(complete.Commands); changed[0].targetId += "changed";
        Assert.That(MatchRunner.Replay(setup.Definition, setup.Scenario, 123u, "wanderer", changed).Result.Status, Is.EqualTo("BLOCKED"));
        Assert.That(MatchRunner.Replay(setup.Definition, setup.Scenario, 123u, "wanderer", complete.Commands.Take(1).ToArray()).Result.Failure.Reason, Is.EqualTo("replay-incomplete"));
        var extra = LoopJson.Copy(complete.Commands); extra.Add(LoopJson.Copy(extra[^1]));
        Assert.That(MatchRunner.Replay(setup.Definition, setup.Scenario, 123u, "wanderer", extra).Result.Failure.Reason, Is.EqualTo("replay-after-finish"));
    }

    [Test] public void CollectorSeparatesShieldAbsorptionFromOverkillAndSkippedSkills()
    {
        var state = NewState(); var p = CompetitionRules.Human(state); var collector = new OutcomeCollector();
        var command = new CompetitionCommandEntry { kind = "combat", arg = "" };
        p.phase = CompetitionPhase.Combat; p.v4.enemyId = "fixture-enemy"; p.enemy = new Enemy { hp = 5, maxHp = 5, shield = 3 };
        collector.Before(state, command);
        p.encounterTurns = 1; p.enemy.hp = 0;
        p.lastTurn = new TurnLog { retaliationDamage = 2, playerBlock = 4, skills = new() {
            new SkillLog { skillId = "hit", enemyShieldBefore = 3, enemyShieldAfter = 0 },
            new SkillLog { skillId = "skip", status = "enemy-defeated" } } };
        p.v4.lastTurn = new V4CombatLog { retaliationBeforeBlock = 6, skills = new() {
            new V4SkillResult { skillId = "hit", damageType = DamageType.Fire, damageBeforeShield = 20, damageApplied = 5, block = 4 },
            new V4SkillResult { skillId = "skip", status = "enemy-defeated" } } };
        collector.After(state, command);
        Assert.That(collector.Observations.Skills["hit"].HpDamage, Is.EqualTo(5));
        Assert.That(collector.Observations.Skills["hit"].ShieldAbsorbed, Is.EqualTo(3));
        Assert.That(collector.Observations.Skills["skip"].SkippedEnemyDefeated, Is.EqualTo(1));
        Assert.That(collector.Observations.Skills["skip"].Applied, Is.Zero);
        Assert.That(collector.Observations.CombatHpDamageTaken, Is.EqualTo(2));
        Assert.That(collector.Observations.CombatDamageBlocked, Is.EqualTo(4));
        Assert.That(collector.Observations.Enemies["fixture-enemy"].Defeated, Is.EqualTo(1));
    }

    [Test] public void ParticipantCollectorSplitsTiedFirstPlaceAndKeepsAiHistoryMissing()
    {
        var state = NewState();
        foreach (var p in state.participants) { p.rank = 1; p.hp = 30; }
        var rows = OutcomeCollector.Participants(state, new HumanObservations { MaxEncounterTurns = 8 });
        Assert.That(rows.Sum(p => p.FirstPlaceShare), Is.EqualTo(1).Within(1e-12));
        Assert.That(rows.All(p => p.FirstPlaceShare == 0.25), Is.True);
        Assert.That(rows[0].MaxEncounterTurns, Is.EqualTo(8));
        Assert.That(rows.Skip(1).All(p => p.MaxEncounterTurns == null), Is.True);
        Assert.That(rows[0].HpRatio, Is.EqualTo(30d / state.participants[0].maxHp));
        Assert.Throws<InvalidOperationException>(() => OutcomeCollector.ValidateEnded(state));
    }
}
