using System;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionContent
    {
        public const string RulesVersion = "shared-world-preview-1";
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
            if (d == null || d.rulesVersion != RulesVersion || d.aiVersion != AiVersion || d.scoreVersion != ScoreVersion ||
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
            if (d.adventurers == null || d.adventurers.Length < 4 || d.adventurers.Any(a => a == null || string.IsNullOrWhiteSpace(a.id) || string.IsNullOrWhiteSpace(a.name)) ||
                d.adventurers.Select(a => a.id).Distinct().Count() != d.adventurers.Length || !d.adventurers.Any(a => a.id == "wanderer"))
                throw new ArgumentException("Invalid adventurers");
            if (d.policies == null || d.policies.Length != 3 || d.policies.Any(p => p == null || string.IsNullOrWhiteSpace(p.id) ||
                string.IsNullOrWhiteSpace(p.sponsorName) || p.lowHpPercent < 0 || p.lowHpPercent > 100 ||
                new[] { p.combat, p.heal, p.study, p.risk, p.gear, p.die, p.leave }.Any(v => v < 0 || v > 1000)) ||
                d.policies.Select(p => p.id).Distinct().Count() != 3)
                throw new ArgumentException("Invalid AI policies");
        }
    }
}
