using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionRules
    {
        public static CompetitionState Start(CompetitionDefinition definition, Profile profile, uint seed, string matchId, string adventurerId = "wanderer")
        {
            CompetitionContent.Validate(definition);
            if (profile == null || profile.growth < 0 || profile.growth > 1000 || seed == 0 || string.IsNullOrWhiteSpace(matchId) ||
                !definition.adventurers.Any(a => a.id == adventurerId) || !definition.combat.dice.Any(d => d.id == profile.startingDie) ||
                !definition.combat.gear.Any(g => g.id == profile.startingWeapon && g.slot == 0)) throw new ArgumentException("Invalid match start");
            var state = new CompetitionState { matchId = matchId, seed = seed, definition = LoopJson.Copy(definition),
                startingProfile = LoopJson.Copy(profile), participants = new ParticipantState[4] };
            for (int i = 0; i < 4; ++i)
            {
                string id = i == 0 ? "human" : "ai-" + i;
                var adventurer = i == 0 ? definition.adventurers.Single(a => a.id == adventurerId) : definition.adventurers[i];
                var p = new ParticipantState { id = id, adventurerId = adventurer.id, name = adventurer.name, isHuman = i == 0,
                    aiPolicyId = i == 0 ? null : definition.policies[i - 1].id, sponsorId = i == 0 ? "플레이어의 후원자" : definition.policies[i - 1].sponsorName,
                    hp = definition.combat.hp + profile.growth * 5, maxHp = definition.combat.hp + profile.growth * 5,
                    power = definition.combat.power, guard = definition.combat.guard, visits = new int[12], rank = 1,
                    travelRng = CompetitionRandom.Split(seed, id, "travel"), combatRng = CompetitionRandom.Split(seed, id, "combat"),
                    eventRng = CompetitionRandom.Split(seed, id, "event"), aiRng = CompetitionRandom.Split(seed, id, "ai"), message = "탐험을 시작합니다." };
                p.dice.Add(new DieSlot { instanceId = matchId + ":" + id + ":starter", definitionId = profile.startingDie });
                p.equipment[0] = profile.startingWeapon;
                state.participants[i] = p;
            }
            Validate(state);
            return state;
        }

        public static void Validate(CompetitionState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.matchId) || state.seed == 0 || state.startingProfile == null ||
                state.startingProfile.growth < 0 || state.worldRound < 1 || state.actionNumber < 0 || state.lastRound == null)
                throw new ArgumentException("Invalid match");
            CompetitionContent.Validate(state.definition);
            if (state.worldRound > state.definition.maxRounds || state.participants == null || state.participants.Length != 4 ||
                state.participants.Any(p => p == null) || !state.participants[0].isHuman || state.participants.Skip(1).Any(p => p.isHuman) ||
                state.participants.Select(p => p.id).Distinct().Count() != 4 || state.lastRound.Count > 3 ||
                (state.finished && string.IsNullOrWhiteSpace(state.endReason))) throw new ArgumentException("Invalid match lifecycle");
            var rules = state.definition.combat;
            foreach (var p in state.participants)
            {
                if (string.IsNullOrWhiteSpace(p.id) || !state.definition.adventurers.Any(a => a.id == p.adventurerId) ||
                    (!p.isHuman && !state.definition.policies.Any(a => a.id == p.aiPolicyId)) || p.position < 0 || p.position >= 12 ||
                    p.completedLaps < 0 || p.completedLaps > 3 || p.hp < 0 || p.hp > p.maxHp || p.maxHp <= 0 || p.power < 0 || p.guard < 0 ||
                    p.gold < 0 || p.xp < 0 || p.level < 1 || p.turn < 0 || p.encounterTurns < 0 || p.encounterTurns > state.definition.maxCombatTurns ||
                    p.honor < 0 || p.rank < 1 || p.rank > 4 || !Enum.IsDefined(typeof(CompetitionPhase), p.phase) ||
                    p.visits == null || p.visits.Length != 12 || p.visits.Any(n => n < 0) ||
                    p.dice == null || p.dice.Count != Math.Min(p.completedLaps + 1, 3) || p.dice.Any(d => d == null ||
                        string.IsNullOrWhiteSpace(d.instanceId) || !rules.dice.Any(x => x.id == d.definitionId)) ||
                    p.dice.Select(d => d.instanceId).Distinct().Count() != p.dice.Count || p.equipment == null || p.equipment.Length != 3 ||
                    p.equipment.Where((id, slot) => id != null && !rules.gear.Any(g => g.id == id && g.slot == slot)).Any() ||
                    p.claimed == null || p.claimed.Distinct().Count() != p.claimed.Count || p.claimed.Count != p.completedLaps ||
                    p.scores == null || p.scores.Any(s => s == null || string.IsNullOrWhiteSpace(s.id) || s.amount < 0) ||
                    p.scores.Select(s => s.id).Distinct().Count() != p.scores.Count || p.scores.Sum(s => s.amount) != p.honor ||
                    ((p.finished || p.eliminated) != (p.phase == CompetitionPhase.Complete)) || (p.finished && p.eliminated) ||
                    (p.hp == 0 && !p.eliminated) || (p.trialSucceeded && (!p.finished || p.completedLaps != 3 || p.hp <= 0)))
                    throw new ArgumentException("Invalid participant");
                if (p.phase == CompetitionPhase.Combat && (p.enemy == null || p.enemy.hp <= 0 || p.hp <= 0)) throw new ArgumentException("Invalid active combat");
                if (p.enemy != null && (p.enemy.hp < 0 || p.enemy.maxHp <= 0 || p.enemy.hp > p.enemy.maxHp || p.enemy.attack <= 0 || p.enemy.shield < 0 ||
                    !Enum.IsDefined(typeof(Intent), p.enemy.intent))) throw new ArgumentException("Invalid enemy");
                if (p.phase == CompetitionPhase.Encounter && (p.offer == null || p.offer.choices == null || p.offer.choices.Length < 2 || p.offer.choices.Length > 3 ||
                    p.offer.placeId != state.definition.world.places[p.position].id || p.offer.selectedChoiceId != null)) throw new ArgumentException("Invalid encounter offer");
                if (p.offer != null && (string.IsNullOrWhiteSpace(p.offer.id) || p.offer.stage < 0 || p.offer.stage > 3 || p.offer.choices == null ||
                    p.offer.choices.Any(c => c == null || !state.definition.encounters.Any(e => e.id == c.id)) ||
                    p.offer.choices.Select(c => c.id).Distinct().Count() != p.offer.choices.Length)) throw new ArgumentException("Invalid offer choices");
                if (!p.claimed.SequenceEqual(Enumerable.Range(1, p.completedLaps).Select(lap => state.matchId + ":" + p.id + ":lap:" + lap)))
                    throw new ArgumentException("Invalid lap claims");
            }
        }
        public static ParticipantState Human(CompetitionState state) => state.participants[0];
        public static ParticipantState[] Rankings(CompetitionState state) => state.participants.OrderByDescending(p => p.trialSucceeded)
            .ThenByDescending(p => p.completedLaps).ThenByDescending(p => p.honor).ThenByDescending(p => p.hp).ThenBy(p => p.id, StringComparer.Ordinal).ToArray();

        internal static void Rank(CompetitionState state)
        {
            var ranked = Rankings(state);
            for (int i = 0; i < ranked.Length; ++i)
                ranked[i].rank = i > 0 && ranked[i].trialSucceeded == ranked[i - 1].trialSucceeded && ranked[i].completedLaps == ranked[i - 1].completedLaps &&
                    ranked[i].honor == ranked[i - 1].honor && ranked[i].hp == ranked[i - 1].hp ? ranked[i - 1].rank : i + 1;
        }

        internal static void AddXp(ParticipantState p, int amount)
        {
            p.xp += amount;
            while (p.xp >= p.level * 8)
            {
                p.xp -= p.level * 8; ++p.level; p.maxHp += 6; p.hp = Math.Min(p.maxHp, p.hp + 6); ++p.power; ++p.guard;
            }
        }
        internal static void Honor(ParticipantState p, string id, string reason, int amount)
        {
            if (p.scores.Any(s => s.id == id)) throw new InvalidOperationException("Honor already awarded");
            p.scores.Add(new ScoreRecord { id = id, reason = reason, amount = amount }); p.honor += amount;
        }
        internal static bool Active(ParticipantState p) => !p.finished && !p.eliminated;
        internal static void Eliminate(ParticipantState p, string reason)
        {
            p.eliminated = true; p.phase = CompetitionPhase.Complete; p.message = reason;
        }
    }
}
