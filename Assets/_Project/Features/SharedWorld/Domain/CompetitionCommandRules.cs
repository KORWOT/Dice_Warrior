using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionCommandRules
    {
        public static string Target(CompetitionState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var p = CompetitionRules.Human(state);
            return FormattableString.Invariant($"{state.matchId}|{p.id}|{state.worldRound}|{state.actionNumber}|{p.completedLaps}|{p.position}|{p.phase}|{p.offer?.id}|{p.turn}");
        }

        public static void Apply(CompetitionState state, string kind, string arg)
        {
            if (state == null || state.finished) throw new InvalidOperationException("이미 끝났거나 존재하지 않는 경기입니다.");
            arg = arg ?? "";
            var p = CompetitionRules.Human(state);
            bool roundEnded;
            if (kind == "advance")
            {
                Empty(arg);
                if (CompetitionRules.Active(p)) throw new InvalidOperationException("진행 중인 모험가의 차례를 완료하세요.");
                roundEnded = true;
            }
            else roundEnded = ApplyParticipant(state, p, kind, arg);
            if (roundEnded) CompleteRound(state);
            ++state.actionNumber;
            CompetitionRules.Rank(state);
            CompetitionRules.Validate(state);
        }

        public static EncounterChoice[] LegalChoices(CompetitionState state, ParticipantState participant) => CompetitionEncounterRules.LegalChoices(state, participant);

        internal static bool ApplyParticipant(CompetitionState state, ParticipantState p, string kind, string arg)
        {
            if (!CompetitionRules.Active(p)) throw new InvalidOperationException("완료·탈락한 모험가입니다.");
            switch (kind)
            {
                case "travel":
                    Empty(arg);
                    if (p.phase != CompetitionPhase.Map) throw new InvalidOperationException("보드 이동 차례가 아닙니다.");
                    p.lastTravel = 1 + CompetitionRandom.Next(ref p.travelRng, 6); p.lastTurn = null; p.offer = null; p.enemy = null; p.encounterTurns = 0;
                    if (CompetitionV4Content.IsEnabled(state.definition)) { p.v4.enemyId = null; p.v4.lastTurn = null; }
                    if (p.position + p.lastTravel >= state.definition.world.places.Length)
                    {
                        p.position = 0; p.phase = CompetitionPhase.LapReward; p.message = "한 바퀴 완주! 보상을 받으세요.";
                    }
                    else
                    {
                        p.position += p.lastTravel; p.offer = CompetitionEncounterRules.CreateOffer(state, p); p.phase = CompetitionPhase.Encounter;
                        p.message = state.definition.world.places[p.position].name + "에 도착했습니다.";
                    }
                    return false;
                case "choose": return CompetitionEncounterRules.Choose(state, p, arg);
                case "combat": Empty(arg); return CompetitionEncounterRules.Combat(state, p);
                case "lap": return ClaimLap(state, p, arg);
                case "abandon":
                    Empty(arg); CompetitionRules.Eliminate(p, "모험을 포기했습니다."); return true;
                default: throw new ArgumentException("Unknown competition command");
            }
        }

        static bool ClaimLap(CompetitionState state, ParticipantState p, string arg)
        {
            if (p.phase != CompetitionPhase.LapReward || p.completedLaps >= state.definition.lapsToFinish) throw new InvalidOperationException("No lap reward");
            var rules = state.definition.combat;
            var gear = rules.gear.SingleOrDefault(g => g.id == arg);
            if (p.completedLaps == 2 && gear == null) throw new ArgumentException("Unknown lap equipment");
            if (p.completedLaps < 2 && arg.Length > 0 && !rules.dice.Any(d => d.id == arg)) throw new ArgumentException("Unknown lap die");
            int lap = p.completedLaps + 1;
            string id = state.matchId + ":" + p.id + ":lap:" + lap;
            if (p.claimed.Contains(id)) throw new InvalidOperationException("Lap already claimed");
            p.claimed.Add(id); p.completedLaps = lap; p.gold += rules.lapGold;
            CompetitionRules.AddXp(state.definition, p, rules.lapXp); p.hp = Math.Min(p.maxHp, p.hp + rules.lapHeal);
            if (lap < state.definition.lapsToFinish)
            {
                p.dice.Add(new DieSlot { instanceId = id + ":die", definitionId = arg.Length == 0 ? rules.dice[lap].id : arg });
                p.phase = CompetitionPhase.Map; p.message = lap + "바퀴 완주 · 전투 주사위 " + p.dice.Count + "개";
                return true;
            }
            p.equipment[gear.slot] = gear.id;
            CompetitionAttributes.RefreshStats(state.definition, p);
            CompetitionEncounterRules.BeginCombat(p, "순환의 수호자", rules.bossHp, rules.bossAttack, true);
            if (CompetitionV4Content.IsEnabled(state.definition)) { p.v4.enemyId = "final"; p.v4.lastTurn = null; }
            return false;
        }

        static void CompleteRound(CompetitionState state)
        {
            state.lastRound.Clear();
            foreach (var p in state.participants.Skip(1))
                if (CompetitionRules.Active(p)) state.lastRound.Add(CompetitionAi.PlayTurn(state, p));
            if (state.participants.All(p => !CompetitionRules.Active(p)))
            { state.finished = true; state.endReason = "all-complete"; }
            else if (state.worldRound >= state.definition.maxRounds)
            { state.finished = true; state.endReason = "round-limit"; }
            else ++state.worldRound;
        }
        static void Empty(string arg) { if (arg.Length != 0) throw new ArgumentException("This command has no argument"); }
    }
}
