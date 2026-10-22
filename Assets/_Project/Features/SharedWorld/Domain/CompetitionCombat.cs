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
            if (definition.rulesVersion != CompetitionContent.RulesVersion && definition.rulesVersion != CompetitionContent.LegacyVersion)
                throw new ArgumentException("Unsupported competition combat version");
            return Resolve(definition.combat, participant, faces, definition.rulesVersion == CompetitionContent.RulesVersion);
        }

        static TurnLog Resolve(Rules rules, ParticipantState participant, Symbol[] faces, bool allowPartial)
        {
            var combat = new Run { rules = rules, hp = participant.hp, power = participant.power, guard = participant.guard,
                equipment = participant.equipment, enemy = participant.enemy, turn = participant.turn };
            var log = SymbolCombat.Resolve(combat, faces, allowPartial);
            participant.hp = combat.hp;
            participant.turn = combat.turn;
            participant.lastTurn = log;
            return log;
        }
    }
}
