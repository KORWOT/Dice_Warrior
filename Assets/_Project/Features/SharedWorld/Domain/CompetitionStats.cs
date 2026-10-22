using System;
using System.Linq;
using System.Collections.Generic;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public sealed class CompetitionStatBreakdown
    {
        public string name, passiveName, passiveDescription;
        public int hp, maxHp, baseHp, accountHp, levelHp, baseStrength, baseGuard, baseArcane, levelBonus, strength, guard, arcane;
    }

    public static class CompetitionStats
    {
        public static bool IsEnabled(CompetitionDefinition definition) => definition != null && definition.rulesVersion == CompetitionContent.StatsVersion;

        public static CompetitionStatBreakdown Preview(CompetitionDefinition definition, Profile profile, string adventurerId)
        {
            if (!IsEnabled(definition)) return null;
            CompetitionContent.Validate(definition);
            if (profile == null || profile.growth < 0 || profile.growth > 1000) throw new ArgumentException("Invalid starting profile");
            var adventurer = definition.adventurers.SingleOrDefault(a => a.id == adventurerId);
            if (adventurer == null) throw new ArgumentException("Unknown adventurer");
            var result = Breakdown(definition, adventurer, profile.growth * 5, 1);
            result.hp = result.maxHp;
            return result;
        }

        public static CompetitionStatBreakdown Describe(CompetitionDefinition definition, ParticipantState participant)
        {
            if (!IsEnabled(definition)) return null;
            if (participant == null || participant.growth == null || participant.level < 1) throw new ArgumentException("Missing adventurer growth");
            var adventurer = definition.adventurers.SingleOrDefault(a => a.id == participant.adventurerId);
            if (adventurer?.stats == null || definition.growth == null) throw new ArgumentException("Missing adventurer stats");
            var result = Breakdown(definition, adventurer, participant.growth.accountHp, participant.level);
            result.hp = participant.hp;
            return result;
        }

        static CompetitionStatBreakdown Breakdown(CompetitionDefinition definition, AdventurerDefinition adventurer, int accountHp, int level)
        {
            var stats = adventurer.stats;
            int levelBonus = checked((level - 1) * definition.growth.statPerLevel);
            int levelHp = checked((level - 1) * definition.growth.hpPerLevel);
            return new CompetitionStatBreakdown { name = adventurer.name, passiveName = stats.passiveName, passiveDescription = stats.passiveDescription,
                baseHp = stats.hp, accountHp = accountHp, levelHp = levelHp, maxHp = checked(stats.hp + accountHp + levelHp),
                baseStrength = stats.strength, baseGuard = stats.guard, baseArcane = stats.arcane, levelBonus = levelBonus,
                strength = checked(stats.strength + levelBonus), guard = checked(stats.guard + levelBonus), arcane = checked(stats.arcane + levelBonus) };
        }

        public static string ScalingText(CompetitionDefinition definition, string skillId)
        {
            if (!IsEnabled(definition)) return "";
            var skill = definition.combat.basics.Concat(definition.combat.combos).SingleOrDefault(s => s.id == skillId);
            var scaling = definition.growth.scaling.SingleOrDefault(s => s.skillId == skillId);
            if (skill == null || scaling == null) throw new ArgumentException("Unknown skill scaling");
            var effects = new List<string>();
            if (skill.damage > 0)
            {
                var terms = new List<string>();
                if (scaling.strength > 0) terms.Add(scaling.strength == 1 ? "무력" : "무력×" + scaling.strength);
                if (scaling.arcane > 0) terms.Add(scaling.arcane == 1 ? "비전" : "비전×" + scaling.arcane);
                string bonus = terms.Count == 0 ? "" : string.Join("+", terms);
                if (scaling.divisor != 1 && terms.Count > 0) bonus = "내림((" + bonus + ")/" + scaling.divisor + ")";
                effects.Add("피해 " + skill.damage + (bonus.Length == 0 ? "" : "+" + bonus));
            }
            if (skill.block > 0) effects.Add("보호 " + skill.block + "+수호");
            return string.Join(" · ", effects);
        }

        internal static int DamageBonus(CompetitionDefinition definition, ParticipantState participant, CompetitionStatBreakdown stats, Skill skill)
        {
            var scaling = definition.growth.scaling.Single(s => s.skillId == skill.id);
            var passive = definition.adventurers.Single(a => a.id == participant.adventurerId).stats;
            return checked((stats.strength * scaling.strength + stats.arcane * scaling.arcane) / scaling.divisor +
                (passive.damageTags.Contains(skill.tag) ? passive.damageBonus : 0));
        }

        internal static int BlockBonus(CompetitionDefinition definition, ParticipantState participant, CompetitionStatBreakdown stats) =>
            checked(stats.guard + definition.adventurers.Single(a => a.id == participant.adventurerId).stats.blockBonus);
    }
}
