using System;
using System.Linq;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionAi
    {
        public static string Choose(CompetitionState state, ParticipantState p)
        {
            var policy = state.definition.policies.Single(a => a.id == p.aiPolicyId);
            var legal = CompetitionEncounterRules.LegalChoices(state, p);
            if (legal.Length == 0) throw new InvalidOperationException("AI has no legal offered choice");
            int best = legal.Max(c => Score(policy, p, c));
            var tied = legal.Where(c => Score(policy, p, c) == best).ToArray();
            return tied.Length == 1 ? tied[0].id : tied[CompetitionRandom.Next(ref p.aiRng, tied.Length)].id;
        }

        static int Score(AiPolicyDefinition policy, ParticipantState p, EncounterChoice c)
        {
            int score;
            switch (c.kind)
            {
                case EncounterKind.Combat: score = policy.combat; break;
                case EncounterKind.Heal: score = policy.heal; break;
                case EncounterKind.Study: score = policy.study; break;
                case EncounterKind.Risk: score = policy.risk; break;
                case EncounterKind.Gear: score = policy.gear; break;
                case EncounterKind.Die: score = policy.die; break;
                default: score = policy.leave; break;
            }
            if (p.hp * 100 < p.maxHp * policy.lowHpPercent && c.heal > 0) score += 2000;
            if (c.kind == EncounterKind.Heal && p.hp == p.maxHp) score = 0;
            if (c.kind == EncounterKind.Gear && p.equipment.Contains(c.rewardId)) score = 0;
            if (c.kind == EncounterKind.Die && p.dice[0].definitionId == c.rewardId) score = 0;
            return score;
        }

        internal static ParticipantSummary PlayTurn(CompetitionState state, ParticipantState p)
        {
            var summary = new ParticipantSummary { participantId = p.id, name = p.name, fromPosition = p.position, hpBefore = p.hp };
            int honor = p.honor;
            CompetitionCommandRules.ApplyParticipant(state, p, "travel", "");
            bool ended = false;
            // A legal board action is travel + one choice/lap + at most maxCombatTurns rolls.
            for (int actions = 0; actions < state.definition.maxCombatTurns + 3 && !ended; ++actions)
            {
                switch (p.phase)
                {
                    case CompetitionPhase.Encounter: ended = CompetitionCommandRules.ApplyParticipant(state, p, "choose", Choose(state, p)); break;
                    case CompetitionPhase.Combat: ended = CompetitionCommandRules.ApplyParticipant(state, p, "combat", ""); break;
                    case CompetitionPhase.LapReward: ended = CompetitionCommandRules.ApplyParticipant(state, p, "lap", p.completedLaps < 2 ? state.startingProfile.startingDie : "aegis"); break;
                    default: ended = true; break;
                }
            }
            if (!ended) throw new InvalidOperationException("AI action bound exceeded");
            summary.toPosition = p.position; summary.travel = p.lastTravel; summary.hpAfter = p.hp; summary.honorGained = p.honor - honor;
            summary.completedLaps = p.completedLaps; summary.eliminated = p.eliminated; summary.finished = p.finished; summary.message = p.message;
            return summary;
        }
    }
}
