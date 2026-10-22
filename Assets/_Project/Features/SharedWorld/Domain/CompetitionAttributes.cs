using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionAttributes
    {
        public static readonly AttributeStat[] All = (AttributeStat[])Enum.GetValues(typeof(AttributeStat));
        public static string Target(AttributeStat stat) => stat.ToString().ToLowerInvariant();
        public static string Label(AttributeStat stat) => new[] { "힘", "민첩", "체질", "지능", "정신", "행운" }[(int)stat];

        public static V4StatBreakdown Preview(CompetitionDefinition d, Profile profile, string id)
        {
            if (!CompetitionV4Content.IsEnabled(d)) return null;
            if (profile == null || profile.growth < 0 || profile.growth > 1000) throw new ArgumentException("Invalid preview profile");
            var p = new ParticipantState { id = "preview", adventurerId = id, v4 = new V4ParticipantState { accountHp = profile.growth * 5 } };
            p.equipment[0] = profile.startingWeapon;
            var result = Describe(d, p); result.hp = result.maxHp; return result;
        }

        public static V4StatBreakdown Describe(CompetitionDefinition d, ParticipantState p)
        {
            if (!CompetitionV4Content.IsEnabled(d)) return null;
            if (p?.v4 == null || p.level < 1 || p.level > 1000000) throw new ArgumentException("Missing v4 participant");
            var a = d.v4.adventurers.Single(x => x.id == p.adventurerId);
            var modifiers = Bindings(d, p, CompetitionV4Content.Tags(d, p));
            var result = new V4StatBreakdown { name = d.adventurers.Single(x => x.id == p.adventurerId).name,
                passiveName = a.passiveName, passiveDescription = a.passiveDescription, baseStats = LoopJson.Copy(a.attributes),
                attributes = new AttributeValues(), level = p.level, levelBonus = checked((p.level - 1) * d.v4.attributePerLevel),
                baseHp = d.v4.baseHp, accountHp = p.v4.accountHp, hp = p.hp, modifiers = modifiers };
            foreach (var stat in All)
                result.attributes.Set(stat, CompetitionModifiers.Evaluate(checked(a.attributes.Get(stat) + result.levelBonus), modifiers.Where(m => m.target == Target(stat))));
            result.constitutionHp = checked(result.attributes.constitution * d.v4.hpPerConstitution);
            result.maxHp = checked(result.baseHp + result.constitutionHp + result.accountHp);
            return result;
        }

        // Binding conditions only read the frozen snapshot. They never append tags or mutate stats.
        public static V4Modifier[] Bindings(CompetitionDefinition d, ParticipantState p, TagSnapshot tags)
        {
            var result = new List<V4Modifier>();
            Add(d.v4.adventurers.Single(a => a.id == p.adventurerId).bindings, "adventurer", p.adventurerId);
            for (int i = 0; i < p.equipment.Length; ++i)
                if (p.equipment[i] != null) Add(d.v4.gear.Single(g => g.gearId == p.equipment[i]).bindings, "gear", "slot:" + i + "/" + p.equipment[i]);
            return CompetitionModifiers.Normalize(result);

            void Add(V4Binding[] rules, string kind, string source)
            {
                foreach (var rule in rules.OrderBy(r => r.id, StringComparer.Ordinal))
                    if (rule.anyOf.Any(tags.Matches))
                        result.Add(new V4Modifier { id = rule.id, sourceKind = kind, sourceId = source, target = rule.target,
                            operation = rule.operation, stack = rule.stack, stackGroup = rule.stackGroup, amount = rule.amount });
            }
        }

        public static void RefreshStats(CompetitionDefinition d, ParticipantState p)
        {
            if (!CompetitionV4Content.IsEnabled(d)) return;
            int previousMax = p.maxHp;
            var stats = CompetitionAttributes.Describe(d, p);
            p.maxHp = stats.maxHp; p.hp = Math.Min(p.maxHp, checked(p.hp + Math.Max(0, p.maxHp - previousMax)));
            p.power = stats.attributes.strength; p.guard = stats.attributes.will;
        }

        public static void ValidateParticipant(CompetitionState state, ParticipantState p)
        {
            if (!CompetitionV4Content.IsEnabled(state.definition)) { if (p.v4 != null) throw new ArgumentException("Legacy state contains v4 fields"); return; }
            if (p.v4 == null || p.growth != null || state.startingProfile.growth > 1000 || p.v4.accountHp != state.startingProfile.growth * 5 ||
                p.level > 1000000 || p.xp >= p.level * 8 || (!p.isHuman && p.adventurerId != (p.aiPolicyId == "war" ? "wanderer" : p.aiPolicyId == "wisdom" ? "scholar" : "vanguard")))
                throw new ArgumentException("Invalid v4 participant growth");
            var stats = CompetitionAttributes.Describe(state.definition, p);
            if (p.maxHp != stats.maxHp || p.power != stats.attributes.strength || p.guard != stats.attributes.will)
                throw new ArgumentException("Inconsistent v4 derived stats");
            if (p.enemy == null ? p.v4.enemyId != null : p.v4.enemyId != (p.enemy.boss ? "final" : p.offer?.selectedChoiceId) ||
                !state.definition.v4.enemies.Any(e => e.encounterId == p.v4.enemyId)) throw new ArgumentException("Missing frozen enemy profile");
            var log = p.v4.lastTurn;
            if ((log == null) != (p.lastTurn == null)) throw new ArgumentException("Missing typed result");
            if (log != null && (log.skills == null || p.lastTurn.skills == null || log.skills.Count != p.lastTurn.skills.Count || log.skills.Count > 3 ||
                !Enum.IsDefined(typeof(DamageType), log.retaliationType) || log.retaliationBeforeBlock < 0 || log.skills.Where((s, i) => s == null ||
                    s.skillId != p.lastTurn.skills[i].skillId || s.status != p.lastTurn.skills[i].status || s.damageApplied != p.lastTurn.skills[i].damage ||
                    s.block != p.lastTurn.skills[i].block || s.damageBeforeShield < s.damageApplied || s.damageApplied < 0 || s.block < 0 ||
                    !Enum.IsDefined(typeof(DamageType), s.damageType) || !Enum.IsDefined(typeof(DamageType), s.originalType) ||
                    !new[] { 8500, 10000, 12000 }.Contains(s.affinityBasisPoints) || s.tags == null || s.tags.Length > 512 || s.modifiers == null || s.modifiers.Length > 64).Any()))
                throw new ArgumentException("Invalid typed result");
        }
    }
}

