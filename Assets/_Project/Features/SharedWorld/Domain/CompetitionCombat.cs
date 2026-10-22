using System;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionCombat
    {
        public static TurnLog Resolve(Rules rules, ParticipantState participant, Symbol[] faces) => Resolve(rules, participant, faces, false);

        public static TurnLog Resolve(CompetitionDefinition definition, ParticipantState participant, Symbol[] faces)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (CompetitionV4Content.IsEnabled(definition)) return CompetitionElements.Resolve(definition, participant, faces);
            if (definition.rulesVersion != CompetitionContent.RulesVersion && definition.rulesVersion != CompetitionContent.LegacyVersion &&
                definition.rulesVersion != CompetitionContent.StatsVersion)
                throw new ArgumentException("Unsupported competition combat version");
            if (CompetitionStats.IsEnabled(definition))
            {
                var stats = CompetitionStats.Describe(definition, participant);
                return Resolve(definition.combat, participant, faces, true,
                    skill => CompetitionStats.DamageBonus(definition, participant, stats, skill),
                    skill => CompetitionStats.BlockBonus(definition, participant, stats));
            }
            return Resolve(definition.combat, participant, faces, definition.rulesVersion == CompetitionContent.RulesVersion);
        }

        static TurnLog Resolve(Rules rules, ParticipantState participant, Symbol[] faces, bool allowPartial,
            Func<Skill, int> damageBonus = null, Func<Skill, int> blockBonus = null)
        {
            var combat = new Run { rules = rules, hp = participant.hp, power = participant.power, guard = participant.guard,
                equipment = participant.equipment, enemy = participant.enemy, turn = participant.turn };
            var log = damageBonus == null ? SymbolCombat.Resolve(combat, faces, allowPartial) :
                SymbolCombat.Resolve(combat, faces, allowPartial, damageBonus, blockBonus);
            participant.hp = combat.hp;
            participant.turn = combat.turn;
            participant.lastTurn = log;
            return log;
        }
    }
}
