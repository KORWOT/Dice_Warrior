using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionEncounterRules
    {
        public static EncounterOffer CreateOffer(CompetitionState state, ParticipantState p)
        {
            var place = state.definition.world.places[p.position];
            int visit = ++p.visits[p.position];
            // One persisted event-stream draw per landing. Weighted selections use a local context stream.
            uint local = CompetitionRandom.Split(LoopRules.Next(ref p.eventRng), p.id + "/" + place.id,
                FormattableString.Invariant($"{p.completedLaps}/{visit}"));
            var pool = place.encounterIds.Select(id => state.definition.encounters.Single(e => e.id == id))
                .Where(e => e.minLaps <= p.completedLaps && e.maxLaps >= p.completedLaps).ToList();
            var selected = new List<EncounterDefinition>();
            int count = Math.Min(3, pool.Count);
            for (int i = 0; i < count; ++i)
            {
                int roll = CompetitionRandom.Next(ref local, pool.Sum(e => e.weight));
                int index = 0;
                while (roll >= pool[index].weight) { roll -= pool[index].weight; ++index; }
                selected.Add(pool[index]); pool.RemoveAt(index);
            }
            if (!selected.Any(e => Allowed(p, e)))
            {
                var fallback = pool.FirstOrDefault(e => Allowed(p, e));
                if (fallback == null) throw new InvalidOperationException("Encounter has no legal option");
                selected[selected.Count - 1] = fallback;
            }
            return new EncounterOffer { id = FormattableString.Invariant($"{state.matchId}:{p.id}:{place.id}:{p.completedLaps}:{visit}"),
                placeId = place.id, visit = visit, stage = p.completedLaps,
                choices = selected.Select(e => Freeze(state, p, e)).ToArray() };
        }

        static EncounterChoice Freeze(CompetitionState state, ParticipantState p, EncounterDefinition e)
        {
            bool available = Allowed(p, e);
            return new EncounterChoice { id = e.id, label = e.label, description = e.description, kind = e.kind, rewardId = e.rewardId,
                goldCost = e.goldCost, hpCost = e.hpCost, gold = e.gold, xp = e.xp, heal = e.heal, honor = e.honor,
                enemyHp = state.definition.combat.enemyHp + p.completedLaps * state.definition.combat.enemyHpPerRegion + e.enemyHpBonus,
                enemyAttack = state.definition.combat.enemyAttack + p.completedLaps + e.enemyAttackBonus,
                available = available, unavailableReason = available ? null : p.gold < e.goldCost ? "골드가 부족합니다." :
                    p.hp <= e.hpCost || p.hp < e.minHp ? "체력이 부족합니다." : "필요한 장비가 없습니다." };
        }
        static bool Allowed(ParticipantState p, EncounterDefinition e) => p.gold >= e.goldCost && p.hp > e.hpCost && p.hp >= e.minHp &&
            (e.requiredGearId == null || p.equipment.Contains(e.requiredGearId));

        public static EncounterChoice[] LegalChoices(CompetitionState state, ParticipantState p)
        {
            if (p.phase != CompetitionPhase.Encounter || p.offer == null) return Array.Empty<EncounterChoice>();
            return p.offer.choices.Where(c => c.available && Allowed(p, state.definition.encounters.Single(e => e.id == c.id))).ToArray();
        }

        internal static bool Choose(CompetitionState state, ParticipantState p, string id)
        {
            if (p.phase != CompetitionPhase.Encounter || p.offer == null || p.offer.selectedChoiceId != null) throw new InvalidOperationException("No open encounter");
            var choice = p.offer.choices.SingleOrDefault(c => c.id == id);
            if (choice == null) throw new ArgumentException("Unknown offered choice");
            if (!LegalChoices(state, p).Any(c => c.id == id)) throw new InvalidOperationException("Choice requirements not met");
            p.offer.selectedChoiceId = id;
            p.gold -= choice.goldCost; p.hp -= choice.hpCost;
            if (choice.kind == EncounterKind.Combat)
            {
                BeginCombat(p, choice.label, choice.enemyHp, choice.enemyAttack, false);
                if (CompetitionV4Content.UsesAttributes(state.definition)) { p.v4.enemyId = choice.id; p.v4.lastTurn = null; }
                return false;
            }
            if (choice.kind == EncounterKind.Gear)
            {
                var gear = state.definition.combat.gear.Single(g => g.id == choice.rewardId); p.equipment[gear.slot] = gear.id;
                CompetitionAttributes.RefreshStats(state.definition, p);
            }
            else if (choice.kind == EncounterKind.Die)
                p.dice[0] = new DieSlot { instanceId = p.offer.id + ":die", definitionId = choice.rewardId };
            p.hp = Math.Min(p.maxHp, p.hp + choice.heal);
            Reward(state, p, choice);
            p.phase = CompetitionPhase.Map;
            p.message = choice.label + " · 완료";
            return true;
        }

        internal static void BeginCombat(ParticipantState p, string name, int hp, int attack, bool boss)
        {
            p.phase = CompetitionPhase.Combat; p.encounterTurns = 0; p.lastTurn = null;
            p.enemy = new Enemy { name = name, hp = hp, maxHp = hp, attack = attack, boss = boss,
                intent = (Intent)CompetitionRandom.Next(ref p.combatRng, 3) };
            p.message = name + "에 도전합니다.";
            if (p.v5 != null) { p.v5.lastTurn = null; CompetitionV5Dice.Begin(p); }
        }

        internal static bool Combat(CompetitionState state, ParticipantState p)
        {
            if (p.phase != CompetitionPhase.Combat || p.hp <= 0 || p.enemy == null || p.enemy.hp <= 0) throw new InvalidOperationException("No active combat");
            var rules = state.definition.combat;
            var faces = p.dice.Select(d => rules.dice.Single(x => x.id == d.definitionId))
                .Select(d => d.faces[CompetitionRandom.Next(ref p.combatRng, 6)]).ToArray();
            CompetitionCombat.Resolve(state.definition, p, faces);
            return FinishCombat(state, p);
        }

        internal static bool FinishCombat(CompetitionState state, ParticipantState p)
        {
            var rules = state.definition.combat;
            ++p.encounterTurns;
            if (p.hp <= 0) { CompetitionRules.Eliminate(p, "전투에서 탈락했습니다."); return true; }
            if (p.enemy.hp <= 0)
            {
                if (p.enemy.boss)
                {
                    p.gold += rules.combatGold; CompetitionRules.AddXp(state.definition, p, rules.combatXp);
                    CompetitionRules.Honor(p, state.matchId + ":" + p.id + ":final", "최종 시련", state.definition.finalHonor);
                    p.trialSucceeded = true; p.finished = true; p.phase = CompetitionPhase.Complete; p.message = "최종 시련 성공!";
                }
                else
                {
                    Reward(state, p, p.offer.choices.Single(c => c.id == p.offer.selectedChoiceId));
                    p.phase = CompetitionPhase.Map; p.message = p.enemy.name + " · 승리";
                }
                return true;
            }
            if (p.encounterTurns >= state.definition.maxCombatTurns)
            { CompetitionRules.Eliminate(p, "전투 제한 턴에 도달해 탈락했습니다."); return true; }
            p.enemy.intent = (Intent)CompetitionRandom.Next(ref p.combatRng, 3);
            p.message = "기호 주사위로 다음 기술을 준비합니다.";
            return false;
        }

        static void Reward(CompetitionState state, ParticipantState p, EncounterChoice choice)
        {
            if (CompetitionV4Content.UsesAttributes(state.definition) && state.definition.v4.treasureEncounterIds.Contains(choice.id))
                p.gold = checked(p.gold + CompetitionAttributes.Describe(state.definition, p).attributes.luck / 5);
            p.gold += choice.gold; CompetitionRules.AddXp(state.definition, p, choice.xp);
            CompetitionRules.Honor(p, p.offer.id + ":honor", choice.label, choice.honor);
        }
    }
}
