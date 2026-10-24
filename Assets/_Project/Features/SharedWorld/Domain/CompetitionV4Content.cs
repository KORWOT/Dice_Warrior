using System;
using System.Collections.Generic;
using System.Linq;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.SharedWorld
{
    public static class CompetitionV4Content
    {
        static readonly AttributeStat[] AttributeList = (AttributeStat[])Enum.GetValues(typeof(AttributeStat));
        static string StatTarget(AttributeStat stat) => stat.ToString().ToLowerInvariant();
        public const string Version = "shared-world-preview-4";
        public static bool IsEnabled(CompetitionDefinition d) => d?.rulesVersion == Version;
        public static bool UsesAttributes(CompetitionDefinition d) => IsEnabled(d) || CompetitionV5Content.IsEnabled(d);
        public static string ElementTag(DamageType type) => "element." + type.ToString().ToLowerInvariant();

        public static void Validate(CompetitionDefinition d)
        {
            if (!UsesAttributes(d)) { if (d.v4 != null) throw new ArgumentException("Legacy content contains v4 fields"); return; }
            var v = d.v4;
            if (v == null || v.baseHp != 30 || v.hpPerConstitution != 6 || v.attributePerLevel != 1 || v.weaknessBasisPoints != 2000 ||
                v.resistanceBasisPoints != 1500 || d.growth != null || d.adventurers.Any(a => a.stats != null)) throw new ArgumentException("Invalid v4 contract");
            CompetitionTags.Validate(v.tags, v.conflicts);
            var empty = CompetitionTags.Build(v.tags, v.conflicts, Array.Empty<TagGrant>(), TagStage.FinalResult);
            foreach (var type in (DamageType[])Enum.GetValues(typeof(DamageType)))
                RequireTags(v, new[] { ElementTag(type) }, TagOwnerKind.DamageComponent);
            RequireTags(v, new[] { "place.world" }, TagOwnerKind.Place);
            Exact(v.adventurers?.Select(a => a?.id), d.adventurers.Select(a => a.id), "adventurers");
            if (v.adventurers.Length != 3 || !new[] { "wanderer", "scholar", "vanguard" }.All(id => v.adventurers.Any(a => a.id == id)) ||
                !new[] { "war", "wisdom", "luck" }.All(id => d.policies.Any(p => p.id == id))) throw new ArgumentException("Invalid v4 roster");
            foreach (var a in v.adventurers)
            {
                if (a.attributes == null || AttributeList.Any(s => a.attributes.Get(s) < 1 || a.attributes.Get(s) > 100) ||
                    string.IsNullOrWhiteSpace(a.passiveName) || a.passiveName.Length > 100 || string.IsNullOrWhiteSpace(a.passiveDescription) ||
                    a.passiveDescription.Length > 500) throw new ArgumentException("Invalid v4 adventurer");
                RequireTags(v, a.identityTags, TagOwnerKind.Source); ValidateBindings(a.bindings, empty);
            }
            var skills = d.combat.basics.Concat(d.combat.combos).ToArray();
            Exact(v.skills?.Select(s => s?.skillId), skills.Select(s => s.id), "skills");
            foreach (var skill in v.skills)
            {
                var old = skills.Single(s => s.id == skill.skillId);
                if (!Enum.IsDefined(typeof(DamageType), skill.damageType)) throw new ArgumentException("Unknown damage type");
                Coefficients(skill.damageScaling, old.damage > 0); Coefficients(skill.blockScaling, old.block > 0);
                RequireTags(v, skill.identityTags, TagOwnerKind.Skill); RequireTags(v, skill.grantedActionTags, TagOwnerKind.Action);
            }
            Exact(v.gear?.Select(g => g?.gearId), d.combat.gear.Select(g => g.id), "gear");
            foreach (var gear in v.gear)
            {
                RequireTags(v, gear.identityTags, TagOwnerKind.Equipment); RequireTags(v, gear.grantedActionTags, TagOwnerKind.Action);
                ValidateBindings(gear.bindings, empty);
                if (gear.conversion.HasValue && (!Enum.IsDefined(typeof(DamageType), gear.conversion.Value) ||
                    d.combat.gear.Single(g => g.id == gear.gearId).slot != 0)) throw new ArgumentException("Only a weapon can convert one primary damage component");
            }
            Exact(v.enemies?.Select(e => e?.encounterId), d.encounters.Where(e => e.kind == EncounterKind.Combat).Select(e => e.id).Concat(new[] { "final" }), "enemies");
            foreach (var enemy in v.enemies)
            {
                RequireTags(v, enemy.identityTags, TagOwnerKind.Target);
                if (!Enum.IsDefined(typeof(DamageType), enemy.attackType) ||
                    (enemy.weakness.HasValue && !Enum.IsDefined(typeof(DamageType), enemy.weakness.Value)) ||
                    (enemy.resistance.HasValue && !Enum.IsDefined(typeof(DamageType), enemy.resistance.Value)) ||
                    (enemy.weakness.HasValue && enemy.weakness == enemy.resistance)) throw new ArgumentException("Invalid enemy affinity");
            }
            if (v.treasureEncounterIds == null || v.treasureEncounterIds.Length > 16 || v.treasureEncounterIds.Distinct().Count() != v.treasureEncounterIds.Length ||
                v.treasureEncounterIds.Any(id => !d.encounters.Any(e => e.id == id && e.gold > 0 && e.kind != EncounterKind.Combat)))
                throw new ArgumentException("Invalid treasure rewards");
            // Finite content bounds cover every possible source combination before it can be saved.
            if (d.encounters.Length > 64 || d.combat.gear.Length > 32 || skills.Length > 32 ||
                v.adventurers.Any(a => a.bindings.Length > 16) || v.gear.Any(g => g.bindings.Length > 16)) throw new ArgumentException("v4 content cap exceeded");
        }

        static void Exact(IEnumerable<string> actual, IEnumerable<string> expected, string context)
        {
            if (actual == null) throw new ArgumentException("Missing " + context);
            var a = actual.ToArray(); var e = expected.ToArray();
            if (a.Any(string.IsNullOrWhiteSpace) || a.Length != e.Length || a.Distinct().Count() != a.Length || !a.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(e.OrderBy(x => x, StringComparer.Ordinal)))
                throw new ArgumentException("Incomplete v4 " + context);
        }
        static void Coefficients(AttributeValues values, bool active)
        {
            if (values == null || AttributeList.Any(s => values.Get(s) < 0 || values.Get(s) > 10000) ||
                AttributeList.Sum(s => values.Get(s)) != (active ? 10000 : 0)) throw new ArgumentException("Invalid stat coefficients");
        }
        static void RequireTags(CompetitionV4Definition v, string[] ids, TagOwnerKind owner)
        {
            if (ids == null || ids.Length > 16 || ids.Distinct().Count() != ids.Length || ids.Any(id => !v.tags.Any(t => t.id == id && t.allowedOwners.Contains(owner))))
                throw new ArgumentException("Invalid identity or granted tag owner");
        }
        static void ValidateBindings(V4Binding[] bindings, TagSnapshot empty)
        {
            var targets = AttributeList.Select(StatTarget).Concat(new[] { "damage", "block", "resistance" }).ToArray();
            if (bindings == null || bindings.Length > 16 || bindings.Any(b => b == null || string.IsNullOrWhiteSpace(b.id) || b.id.Length > 100 ||
                string.IsNullOrWhiteSpace(b.stackGroup) || b.stackGroup.Length > 100 || !targets.Contains(b.target) || b.anyOf == null ||
                b.anyOf.Length < 1 || b.anyOf.Length > 4) || bindings.Select(b => b.id).Distinct().Count() != bindings.Length)
                throw new ArgumentException("Invalid binding definitions");
            foreach (var b in bindings)
            {
                CompetitionModifiers.Normalize(new[] { new V4Modifier { id = b.id, sourceKind = "validation", sourceId = "validation", target = b.target,
                    stackGroup = b.stackGroup, amount = b.amount, operation = b.operation, stack = b.stack } });
                foreach (var q in b.anyOf)
                {
                    empty.Matches(q);
                    if (q.all.Length + q.any.Length + q.none.Length == 0) throw new ArgumentException("Binding needs an explicit condition");
                    if (AttributeList.Any(s => TargetIs(s, b.target)) && q.ownerKind != TagOwnerKind.Source && q.ownerKind != TagOwnerKind.Equipment)
                        throw new ArgumentException("Attributes cannot depend on an action or target snapshot");
                }
                if (b.target == "resistance" && (b.operation != ModifierOperation.Flat || b.amount < 0 || b.amount > 8500))
                    throw new ArgumentException("Invalid resistance contribution");
            }
        }
        static bool TargetIs(AttributeStat stat, string target) => StatTarget(stat) == target;

        internal static void ValidateEffect(CompetitionDefinition d, V5SkillDefinition skill)
        {
            var effect = skill.effect;
            if (effect == null || effect.skillId != skill.id || !Enum.IsDefined(typeof(DamageType), effect.damageType))
                throw new ArgumentException("Invalid v5 skill effect");
            Coefficients(effect.damageScaling, skill.damage > 0); Coefficients(effect.blockScaling, skill.block > 0);
            RequireTags(d.v4, effect.identityTags, TagOwnerKind.Skill); RequireTags(d.v4, effect.grantedActionTags, TagOwnerKind.Action);
        }

        public static TagSnapshot Tags(CompetitionDefinition d, ParticipantState p, string skillId = null, DamageType? type = null, V4EnemyDefinition enemy = null,
            V4SkillDefinition skillDefinition = null)
        {
            var grants = new List<TagGrant>(); var v = d.v4;
            Add(v.adventurers.Single(a => a.id == p.adventurerId).identityTags, TagOwnerKind.Source, "self", "adventurer", p.adventurerId);
            for (int i = 0; i < p.equipment.Length; ++i)
                if (p.equipment[i] != null)
                {
                    var g = v.gear.Single(x => x.gearId == p.equipment[i]);
                    Add(g.identityTags, TagOwnerKind.Equipment, "slot:" + i, "gear", "slot:" + i + "/" + g.gearId);
                    if (skillId != null) Add(g.grantedActionTags, TagOwnerKind.Action, "action", "gear", "slot:" + i + "/" + g.gearId);
                }
            if (skillId != null)
            {
                var s = skillDefinition ?? v.skills.Single(x => x.skillId == skillId);
                Add(s.identityTags, TagOwnerKind.Skill, "skill", "skill", skillId);
                Add(s.grantedActionTags, TagOwnerKind.Action, "action", "skill", skillId);
            }
            Add(new[] { "place.world" }, TagOwnerKind.Place, "place", "place", d.world.places[p.position].id);
            if (enemy != null) Add(enemy.identityTags, TagOwnerKind.Target, "target", "enemy", enemy.encounterId);
            if (type.HasValue) Add(new[] { ElementTag(type.Value) }, TagOwnerKind.DamageComponent, "damage", "damage", "primary", TagStage.PostConversion);
            return CompetitionTags.Build(v.tags, v.conflicts, grants, TagStage.PostConversion);

            void Add(string[] ids, TagOwnerKind owner, string ownerId, string kind, string source, TagStage stage = TagStage.BaseComposition)
            {
                for (int i = 0; i < ids.Length; ++i) grants.Add(new TagGrant { tagId = ids[i], ownerKind = owner, ownerId = ownerId,
                    sourceKind = kind, sourceId = source, ordinal = i, stage = stage });
            }
        }

    }
}

