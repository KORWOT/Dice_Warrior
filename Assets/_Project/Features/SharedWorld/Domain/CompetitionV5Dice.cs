using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionV5Dice
    {
        public static int RemainingRequiredCommands(CompetitionState state)
        {
            if (state == null || !CompetitionV5Content.IsEnabled(state.definition)) throw new ArgumentException("Expected v5 match budget");
            if (state.finished) return 0;
            var p = CompetitionRules.Human(state);
            int futureRounds = state.definition.maxRounds - state.worldRound;
            if (futureRounds < 0) throw new ArgumentException("Invalid world round");
            if (p.finished || p.eliminated) return futureRounds + 1;
            int turns = state.definition.maxCombatTurns;
            int current;
            switch (p.phase)
            {
                case CompetitionPhase.Map: current = 2 + turns * 3; break;
                case CompetitionPhase.Encounter: current = 1 + turns * 3; break;
                case CompetitionPhase.LapReward: current = 1 + (p.completedLaps == 2 ? turns * 3 : 0); break;
                case CompetitionPhase.Combat:
                    var s = p.v5?.selection ?? throw new ArgumentException("Missing v5 budget selection");
                    current = s.phase == V5SelectionPhase.AwaitRoll ? 3 * (turns - p.encounterTurns) :
                        s.phase == V5SelectionPhase.Selection ? 1 + s.rerollsLeft + 3 * (turns - p.encounterTurns - 1) :
                        throw new ArgumentException("Invalid v5 budget phase");
                    break;
                default: throw new ArgumentException("Invalid active phase for command reservation");
            }
            return checked(current + futureRounds * (2 + turns * 3));
        }

        public static string HoldUnavailableReason(CompetitionState state, ParticipantState p)
        {
            if (state == null || state.finished || !CompetitionV5Content.IsEnabled(state.definition) || p == null || p.finished || p.eliminated ||
                p.phase != CompetitionPhase.Combat || p.v5?.selection?.phase != V5SelectionPhase.Selection)
                return "먼저 주사위를 굴려 주세요.";
            if (p.v5.selection.holdChanges >= state.definition.v5.maxHoldChangesPerTurn)
                return "이번 턴의 보관 변경 12회를 사용했습니다. 리롤 또는 실행할 수 있습니다.";
            if (p.isHuman && (long)state.actionNumber + 1 + RemainingRequiredCommands(state) > BackendProtocol.MaxCommands)
                return "경기 진행과 리롤을 위한 기록 공간을 보존합니다. 리롤 또는 실행해 주세요.";
            return null;
        }
        public static bool CanHold(CompetitionState state, ParticipantState p) => HoldUnavailableReason(state, p) == null;
        internal static void Begin(ParticipantState p)
        {
            p.v5.selection = new V5SelectionState { phase = V5SelectionPhase.AwaitRoll, combatTurnSequence = p.encounterTurns + 1,
                rerollsLeft = 1, results = Array.Empty<V5FaceResult>() };
        }

        internal static bool Apply(CompetitionState state, ParticipantState p, string kind, string arg)
        {
            if (!CompetitionV5Content.IsEnabled(state.definition) || p.phase != CompetitionPhase.Combat || p.v5?.selection == null ||
                p.hp <= 0 || p.enemy == null || p.enemy.hp <= 0) throw new InvalidOperationException("진행 중인 v5 전투가 아닙니다.");
            CompetitionV5Content.ValidateParticipant(state, p);
            var selection = p.v5.selection;
            if (kind == "combat_hold" || kind == "combat_release")
            {
                string reason = HoldUnavailableReason(state, p);
                if (reason != null) throw new InvalidOperationException(reason);
                var result = selection.results.SingleOrDefault(r => r.dieInstanceId == arg);
                bool hold = kind == "combat_hold";
                if (result == null || result.held == hold) throw new InvalidOperationException("현재 주사위의 보관 상태와 입력이 맞지 않습니다.");
                result.held = hold; ++selection.holdChanges;
                return false;
            }
            if (!string.IsNullOrEmpty(arg)) throw new ArgumentException("This v5 command has no argument");
            if (kind == "combat_roll")
            {
                if (selection.phase != V5SelectionPhase.AwaitRoll) throw new InvalidOperationException("이미 굴린 턴입니다.");
                selection.results = p.dice.Select((die, i) => new V5FaceResult { dieInstanceId = die.instanceId, slotIndex = i }).ToArray();
                foreach (var result in selection.results) Draw(state.definition, p, result);
                selection.phase = V5SelectionPhase.Selection; selection.rollSequence = 1; selection.rerollsLeft = 1;
                p.message = "현재 눈을 보관하거나 리롤한 뒤 실행하세요.";
                return false;
            }
            if (selection.phase != V5SelectionPhase.Selection) throw new InvalidOperationException("먼저 주사위를 굴려 주세요.");
            if (kind == "combat_reroll")
            {
                if (selection.rerollsLeft <= 0 || !selection.results.Any(r => !r.held)) throw new InvalidOperationException("다시 굴릴 주사위 또는 리롤 횟수가 없습니다.");
                foreach (var result in selection.results.Where(r => !r.held)) Draw(state.definition, p, result);
                --selection.rerollsLeft; ++selection.rollSequence;
                p.message = "보관하지 않은 주사위만 다시 굴렸습니다.";
                return false;
            }
            if (kind != "combat_execute") throw new ArgumentException("Unknown v5 command");
            CompetitionV5Skills.Execute(state.definition, p);
            bool ended = CompetitionEncounterRules.FinishCombat(state, p);
            if (ended) p.v5.selection = null;
            else Begin(p);
            return ended;
        }

        static void Draw(CompetitionDefinition d, ParticipantState p, V5FaceResult result)
        {
            var faces = CompetitionV5Content.Faces(d, p.adventurerId, p.dice[result.slotIndex].definitionId);
            result.faceIndex = CompetitionRandom.Next(ref p.combatRng, 6);
            result.faceId = faces[result.faceIndex].id;
        }

        internal static string AiCommand(ParticipantState p, out string arg)
        {
            arg = "";
            var selection = p.v5?.selection ?? throw new ArgumentException("Missing v5 AI selection");
            if (selection.phase == V5SelectionPhase.AwaitRoll) return "combat_roll";
            if (selection.phase != V5SelectionPhase.Selection) throw new ArgumentException("Invalid v5 AI phase");
            if (selection.rollSequence == 1 && selection.rerollsLeft > 0 &&
                selection.results.Any(r => !r.held && r.faceId == CompetitionV5Content.BlankFaceId))
            {
                var keep = selection.results.FirstOrDefault(r => !r.held && r.faceId != CompetitionV5Content.BlankFaceId);
                if (keep != null && selection.holdChanges < 12) { arg = keep.dieInstanceId; return "combat_hold"; }
                return "combat_reroll";
            }
            return "combat_execute";
        }
    }
}
