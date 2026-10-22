using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionCombat
    {
        public static TurnLog Resolve(Rules rules, ParticipantState participant, Symbol[] faces)
        {
            var combat = new Run { rules = rules, hp = participant.hp, power = participant.power, guard = participant.guard,
                equipment = participant.equipment, enemy = participant.enemy, turn = participant.turn };
            var log = SymbolCombat.Resolve(combat, faces);
            participant.hp = combat.hp;
            participant.turn = combat.turn;
            participant.lastTurn = log;
            return log;
        }
    }
}
