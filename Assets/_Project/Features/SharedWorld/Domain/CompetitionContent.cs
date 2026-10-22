using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionContent
    {
        public const string RulesVersion = "shared-world-preview-2";
        public const string LegacyVersion = "shared-world-preview-1";
        public const string StatsVersion = "shared-world-preview-3";
        public const string AiVersion = "shared-world-ai-1";
        public const string ScoreVersion = "shared-world-score-1";

        public static CompetitionDefinition Load(string json)
        {
            var definition = LoopJson.Decode<CompetitionDefinition>(json);
            Validate(definition);
            return definition;
        }

        public static void Validate(CompetitionDefinition d)
        {
            if (d == null || (d.rulesVersion != RulesVersion && d.rulesVersion != LegacyVersion && d.rulesVersion != StatsVersion && !CompetitionV4Content.IsEnabled(d)) || d.aiVersion != AiVersion || d.scoreVersion != ScoreVersion ||
                d.maxRounds < 1 || d.maxRounds > 30 || d.maxCombatTurns < 1 || d.maxCombatTurns > 64 || d.lapsToFinish != 3 || d.finalHonor < 0)
                throw new ArgumentException("Unsupported competition definition");
            LoopRules.ValidateRules(d.combat);
            if (d.world == null || string.IsNullOrWhiteSpace(d.world.id) || string.IsNullOrWhiteSpace(d.world.version) ||
                d.world.places == null || d.world.places.Length != 12 || d.world.places.Any(p => p == null ||
                    string.IsNullOrWhiteSpace(p.id) || string.IsNullOrWhiteSpace(p.name) || p.encounterIds == null) ||
                d.world.places.Select(p => p.id).Distinct().Count() != 12 || d.world.places[0].encounterIds.Length != 0)
                throw new ArgumentException("Invalid fixed world");
            if (d.encounters == null || d.encounters.Length == 0 || d.encounters.Any(e => e == null ||
                string.IsNullOrWhiteSpace(e.id) || string.IsNullOrWhiteSpace(e.label) || !Enum.IsDefined(typeof(EncounterKind), e.kind) ||
                e.minLaps < 0 || e.maxLaps < e.minLaps || e.maxLaps > 3 || e.weight < 1 || e.weight > 1000 ||
                e.minHp < 0 || e.goldCost < 0 || e.hpCost < 0 || e.gold < 0 || e.xp < 0 || e.heal < 0 || e.honor < 0 ||
                e.enemyHpBonus < 0 || e.enemyAttackBonus < 0 ||
                (e.kind == EncounterKind.Gear && !d.combat.gear.Any(g => g.id == e.rewardId)) ||
                (e.kind == EncounterKind.Die && !d.combat.dice.Any(x => x.id == e.rewardId)) ||
                (e.requiredGearId != null && !d.combat.gear.Any(g => g.id == e.requiredGearId))) ||
                d.encounters.Select(e => e.id).Distinct().Count() != d.encounters.Length)
                throw new ArgumentException("Invalid encounter definitions");
            foreach (var place in d.world.places.Skip(1))
            {
                if (place.encounterIds.Distinct().Count() != place.encounterIds.Length ||
                    place.encounterIds.Any(id => !d.encounters.Any(e => e.id == id))) throw new ArgumentException("Invalid place encounter reference");
                for (int stage = 0; stage < 3; ++stage)
                {
                    var available = d.encounters.Where(e => place.encounterIds.Contains(e.id) && e.minLaps <= stage && e.maxLaps >= stage).ToArray();
                    if (available.Length < 2 || !available.Any(e => e.goldCost == 0 && e.hpCost == 0 && e.minHp == 0 && e.requiredGearId == null))
                        throw new ArgumentException("Every place and stage needs choices including a free legal option");
                }
            }
            if (d.adventurers == null || d.adventurers.Length < (CompetitionStats.IsEnabled(d) || CompetitionV4Content.IsEnabled(d) ? 3 : 4) || d.adventurers.Any(a => a == null || string.IsNullOrWhiteSpace(a.id) || string.IsNullOrWhiteSpace(a.name)) ||
                d.adventurers.Select(a => a.id).Distinct().Count() != d.adventurers.Length || !d.adventurers.Any(a => a.id == "wanderer"))
                throw new ArgumentException("Invalid adventurers");
            if (d.policies == null || d.policies.Length != 3 || d.policies.Any(p => p == null || string.IsNullOrWhiteSpace(p.id) ||
                string.IsNullOrWhiteSpace(p.sponsorName) || p.lowHpPercent < 0 || p.lowHpPercent > 100 ||
                new[] { p.combat, p.heal, p.study, p.risk, p.gear, p.die, p.leave }.Any(v => v < 0 || v > 1000)) ||
                d.policies.Select(p => p.id).Distinct().Count() != 3)
                throw new ArgumentException("Invalid AI policies");
            ValidateGrowth(d);
            CompetitionV4Content.Validate(d);
        }

        static void ValidateGrowth(CompetitionDefinition d)
        {
            if (!CompetitionStats.IsEnabled(d))
            {
                if (d.growth != null || d.adventurers.Any(a => a.stats != null)) throw new ArgumentException("Legacy rules cannot contain RGB growth");
                return;
            }
            if (d.adventurers.Length != 3 || !new[] { "wanderer", "scholar", "vanguard" }.All(id => d.adventurers.Any(a => a.id == id)) ||
                !new[] { "war", "wisdom", "luck" }.All(id => d.policies.Any(p => p.id == id))) throw new ArgumentException("Invalid v3 roster");
            foreach (var adventurer in d.adventurers)
            {
                var s = adventurer.stats;
                if (s == null || s.hp < 1 || s.hp > 10000 || new[] { s.strength, s.guard, s.arcane, s.damageBonus, s.blockBonus }.Any(v => v < 0 || v > 1000) ||
                    string.IsNullOrWhiteSpace(s.passiveName) || string.IsNullOrWhiteSpace(s.passiveDescription) || s.damageTags == null ||
                    s.damageTags.Distinct().Count() != s.damageTags.Length || s.damageTags.Any(t => !new[] { "steel", "arcane", "fire", "guard" }.Contains(t)))
                    throw new ArgumentException("Invalid adventurer stats");
            }
            var skills = d.combat.basics.Concat(d.combat.combos).ToArray();
            var g = d.growth;
            if (g == null || g.hpPerLevel != 6 || g.statPerLevel != 1 || g.scaling == null || g.scaling.Length != skills.Length ||
                g.scaling.Any(s => s == null || !skills.Any(skill => skill.id == s.skillId) || s.strength < 0 || s.strength > 100 ||
                    s.arcane < 0 || s.arcane > 100 || s.divisor < 1 || s.divisor > 100) ||
                g.scaling.Select(s => s.skillId).Distinct().Count() != skills.Length) throw new ArgumentException("Invalid RGB growth scaling");
        }
    }
}
