using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.BalanceSim;

public sealed class HumanObservations
{
    public bool Measured { get; set; } = true;
    public Dictionary<string, int> CommandsByKind { get; set; } = new();
    public Dictionary<string, int> OfferedByKind { get; set; } = new();
    public Dictionary<string, int> LegalOfferedByKind { get; set; } = new();
    public Dictionary<string, int> SelectedByKind { get; set; } = new();
    public Dictionary<string, int> SelectedById { get; set; } = new();
    public Dictionary<string, int> LapRewardsById { get; set; } = new();
    public Dictionary<string, int> EquipmentGainedById { get; set; } = new();
    public Dictionary<string, int> DiceGainedById { get; set; } = new();
    public int CombatEntries { get; set; }
    public int CombatTurns { get; set; }
    public int MaxEncounterTurns { get; set; }
    public int EventGoldCost { get; set; }
    public int EventHpCost { get; set; }
    public int EventGoldReward { get; set; }
    public int EventXpReward { get; set; }
    public int EventHonorReward { get; set; }
    public int EventHealOffered { get; set; }
    public int CombatHpDamageTaken { get; set; }
    public int CombatDamageBlocked { get; set; }
    public Dictionary<string, SkillObservation> Skills { get; set; } = new();
    public Dictionary<string, DamageObservation> DamageTypes { get; set; } = new();
    public Dictionary<string, EnemyObservation> Enemies { get; set; } = new();
    public string[] NotMeasured { get; set; } = { "ai-full-skill-history", "ai-full-event-choice-history", "ai-lifetime-max-combat-turns" };
}

public sealed class SkillObservation
{
    public int Applied { get; set; }
    public int SkippedEnemyDefeated { get; set; }
    public int HpDamage { get; set; }
    public int ShieldAbsorbed { get; set; }
    public int BlockGenerated { get; set; }
    public List<int> AppliedHpDamageSamples { get; set; } = new();
}

public sealed class DamageObservation
{
    public int Applied { get; set; }
    public int HpDamage { get; set; }
    public int ShieldAbsorbed { get; set; }
}

public sealed class EnemyObservation
{
    public int Entries { get; set; }
    public int Turns { get; set; }
    public int Defeated { get; set; }
    public int HpDamage { get; set; }
    public int ShieldAbsorbed { get; set; }
    public int RetaliationHpDamage { get; set; }
    public int RetaliationBlocked { get; set; }
}

public sealed class OutcomeCollector
{
    public HumanObservations Observations { get; } = new();
    EncounterChoice selected;
    CompetitionPhase previousPhase;
    string enemyId;
    string[] equipment;
    string[] dice;
    int goldBefore;

    public void Before(CompetitionState state, CompetitionCommandEntry command)
    {
        var human = CompetitionRules.Human(state);
        previousPhase = human.phase;
        enemyId = human.v4?.enemyId;
        equipment = (string[])human.equipment.Clone();
        dice = human.dice.Select(d => d.definitionId).ToArray();
        goldBefore = human.gold;
        selected = command.kind == "choose" ? human.offer?.choices.Single(c => c.id == command.arg) : null;
        if (command.kind == "combat" && human.offer != null && human.offer.selectedChoiceId != null)
            selected = human.offer.choices.Single(c => c.id == human.offer.selectedChoiceId);
        Observations.MaxEncounterTurns = Math.Max(Observations.MaxEncounterTurns, human.encounterTurns);
    }

    public void After(CompetitionState state, CompetitionCommandEntry command)
    {
        var human = CompetitionRules.Human(state);
        Increment(Observations.CommandsByKind, command.kind);
        Observations.MaxEncounterTurns = Math.Max(Observations.MaxEncounterTurns, human.encounterTurns);
        if (command.kind == "travel" && human.phase == CompetitionPhase.Encounter)
        {
            foreach (var choice in human.offer.choices) Increment(Observations.OfferedByKind, choice.kind.ToString());
            foreach (var choice in CompetitionCommandRules.LegalChoices(state, human)) Increment(Observations.LegalOfferedByKind, choice.kind.ToString());
        }
        if (command.kind == "choose" && selected != null)
        {
            Increment(Observations.SelectedByKind, selected.kind.ToString());
            Increment(Observations.SelectedById, selected.id);
            Observations.EventGoldCost += selected.goldCost;
            Observations.EventHpCost += selected.hpCost;
            if (selected.kind != EncounterKind.Combat) AddReward(selected, human.gold - goldBefore + selected.goldCost);
        }
        if (command.kind == "lap") Increment(Observations.LapRewardsById, command.arg);
        if (previousPhase != CompetitionPhase.Combat && human.phase == CompetitionPhase.Combat)
        {
            Observations.CombatEntries++;
            Enemy(human.v4.enemyId).Entries++;
        }
        for (int slot = 0; slot < human.equipment.Length; ++slot)
            if (human.equipment[slot] != null && human.equipment[slot] != equipment[slot]) Increment(Observations.EquipmentGainedById, human.equipment[slot]);
        for (int index = 0; index < human.dice.Count; ++index)
            if (index >= dice.Length || human.dice[index].definitionId != dice[index]) Increment(Observations.DiceGainedById, human.dice[index].definitionId);
        if (command.kind == "combat")
        {
            ObserveCombat(human);
            if (selected != null && human.enemy != null && !human.enemy.boss && human.enemy.hp == 0)
                AddReward(selected, human.gold - goldBefore);
        }
    }

    void AddReward(EncounterChoice choice, int actualGold)
    {
        Observations.EventGoldReward += actualGold;
        Observations.EventXpReward += choice.xp;
        Observations.EventHonorReward += choice.honor;
        Observations.EventHealOffered += choice.kind == EncounterKind.Combat ? 0 : choice.heal;
    }

    void ObserveCombat(ParticipantState human)
    {
        var typed = human.v4?.lastTurn ?? throw new InvalidOperationException("Missing human typed combat log");
        var legacy = human.lastTurn ?? throw new InvalidOperationException("Missing human combat log");
        if (typed.skills.Count != legacy.skills.Count) throw new InvalidOperationException("Human combat logs disagree");
        var enemy = Enemy(enemyId ?? human.v4.enemyId);
        Observations.CombatTurns++; enemy.Turns++;
        int retaliationBlocked = Math.Min(legacy.playerBlock, typed.retaliationBeforeBlock);
        Observations.CombatHpDamageTaken += legacy.retaliationDamage;
        Observations.CombatDamageBlocked += retaliationBlocked;
        enemy.RetaliationHpDamage += legacy.retaliationDamage;
        enemy.RetaliationBlocked += retaliationBlocked;
        if (human.enemy?.hp == 0) enemy.Defeated++;
        for (int i = 0; i < typed.skills.Count; ++i)
        {
            var result = typed.skills[i]; var step = legacy.skills[i];
            if (result.skillId != step.skillId) throw new InvalidOperationException("Human combat skill ordering disagrees");
            if (!Observations.Skills.TryGetValue(result.skillId, out var skill))
                Observations.Skills.Add(result.skillId, skill = new SkillObservation());
            if (result.status == "enemy-defeated") { skill.SkippedEnemyDefeated++; continue; }
            if (result.status != "applied") throw new InvalidOperationException("Unknown human skill status");
            int absorbed = step.enemyShieldBefore - step.enemyShieldAfter;
            if (absorbed < 0 || result.damageApplied < 0) throw new InvalidOperationException("Negative human damage observation");
            skill.Applied++; skill.HpDamage += result.damageApplied; skill.ShieldAbsorbed += absorbed;
            skill.BlockGenerated += result.block; skill.AppliedHpDamageSamples.Add(result.damageApplied);
            string type = result.damageType.ToString();
            if (!Observations.DamageTypes.TryGetValue(type, out var damage))
                Observations.DamageTypes.Add(type, damage = new DamageObservation());
            damage.Applied++; damage.HpDamage += result.damageApplied; damage.ShieldAbsorbed += absorbed;
            enemy.HpDamage += result.damageApplied; enemy.ShieldAbsorbed += absorbed;
        }
    }

    EnemyObservation Enemy(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Missing frozen human enemy identity");
        if (!Observations.Enemies.TryGetValue(id, out var enemy)) Observations.Enemies.Add(id, enemy = new EnemyObservation());
        return enemy;
    }

    static void Increment(Dictionary<string, int> counts, string key)
        => counts[key] = counts.TryGetValue(key, out int count) ? checked(count + 1) : 1;

    public static List<ParticipantResult> Participants(CompetitionState state, HumanObservations observations)
    {
        int winners = state.participants.Count(p => p.rank == 1);
        if (winners == 0) throw new InvalidOperationException("No first-place participant");
        return state.participants.Select(p => new ParticipantResult {
            Slot = p.id, Hero = p.adventurerId, AiPolicy = p.aiPolicyId, IsHuman = p.isHuman,
            TrialSucceeded = p.trialSucceeded, Eliminated = p.eliminated, Finished = p.finished,
            EliminationReason = !p.eliminated ? null : p.hp == 0 ? "combat-hp-depleted" :
                p.encounterTurns >= state.definition.maxCombatTurns ? "combat-turn-limit" : "other: " + p.message,
            Rank = p.rank, FirstPlaceShare = p.rank == 1 ? 1d / winners : 0,
            Laps = p.completedLaps, Hp = p.hp, MaxHp = p.maxHp, HpRatio = (double)p.hp / p.maxHp,
            Level = p.level, Xp = p.xp, Honor = p.honor, Gold = p.gold,
            MaxEncounterTurns = p.isHuman ? observations.MaxEncounterTurns : null
        }).ToList();
    }

    public static void ValidateEnded(CompetitionState state)
    {
        CompetitionRules.Validate(state);
        if (!state.finished) throw new InvalidOperationException("Match did not finish");
        bool allComplete = state.participants.All(p => p.finished || p.eliminated);
        if ((state.endReason != "all-complete" && state.endReason != "round-limit") ||
            (state.endReason == "all-complete" && !allComplete) ||
            (state.endReason == "round-limit" && (allComplete || state.worldRound != state.definition.maxRounds)))
            throw new InvalidOperationException("Inconsistent match end reason");
        var ranked = CompetitionRules.Rankings(state);
        int rank = 1;
        for (int i = 0; i < ranked.Length; ++i)
        {
            if (i > 0 && (ranked[i].trialSucceeded != ranked[i - 1].trialSucceeded || ranked[i].completedLaps != ranked[i - 1].completedLaps ||
                ranked[i].honor != ranked[i - 1].honor || ranked[i].hp != ranked[i - 1].hp)) rank = i + 1;
            if (ranked[i].rank != rank) throw new InvalidOperationException("Inconsistent final rank");
        }
    }
}
