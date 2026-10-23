using DiceWarrior.SharedWorld;

namespace DiceWarrior.BalanceSim;

public static class HumanDecisionPolicies
{
    public static CompetitionCommandEntry Next(CompetitionState state, string policyId)
    {
        if (policyId != "neutral-v1") throw new ArgumentException("Unsupported human policy", nameof(policyId));
        if (state == null || state.finished) throw new InvalidOperationException("Cannot choose for a missing or finished match");
        var human = CompetitionRules.Human(state);
        string kind, arg = "";
        if (human.finished || human.eliminated) kind = "advance";
        else switch (human.phase)
        {
            case CompetitionPhase.Map: kind = "travel"; break;
            case CompetitionPhase.Combat: kind = "combat"; break;
            case CompetitionPhase.LapReward:
                kind = "lap";
                arg = human.completedLaps < 2 ? state.startingProfile.startingDie : "aegis";
                break;
            case CompetitionPhase.Encounter:
                var choices = CompetitionCommandRules.LegalChoices(state, human);
                if (choices.Length == 0) throw new InvalidOperationException("Encounter has no legal choice");
                // Enumerable.OrderByDescending is stable: ties retain the frozen offer order.
                var selected = choices.OrderByDescending(c => c.kind == EncounterKind.Heal && (long)human.hp * 2 < human.maxHp
                    ? 100L : (long)c.xp + c.heal + c.honor).First();
                kind = "choose"; arg = selected.id;
                break;
            default: throw new InvalidOperationException("Unsupported active human phase: " + human.phase);
        }
        int sequence = checked(state.actionNumber + 1);
        return new CompetitionCommandEntry { sequence = sequence, clientCommandId = "human-command-" + sequence,
            kind = kind, arg = arg, targetId = CompetitionCommandRules.Target(state) };
    }
}
