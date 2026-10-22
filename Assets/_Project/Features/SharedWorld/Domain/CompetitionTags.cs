using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DiceWarrior.SharedWorld
{
    public enum TagOwnerKind { Source, Action, Skill, Target, Equipment, Status, Place, Result, DamageComponent }
    public enum TagStage { BaseComposition, PreResolve, PostConversion, FinalResult }
    public enum TagConflictPolicy { Reject, Priority }

    [Serializable] public sealed class TagDefinition
    {
        public string id, name, parentId;
        public TagOwnerKind[] allowedOwners = Array.Empty<TagOwnerKind>();
    }
    [Serializable] public sealed class TagConflictGroup
    {
        public string id;
        public string[] tagIds = Array.Empty<string>();
        public TagConflictPolicy policy;
    }
    [Serializable] public sealed class TagGrant
    {
        public string tagId, ownerId, sourceKind, sourceId;
        public TagOwnerKind ownerKind = (TagOwnerKind)(-1);
        public int ordinal, priority;
        public TagStage stage;
    }
    [Serializable] public sealed class TagQuery
    {
        public TagOwnerKind ownerKind = (TagOwnerKind)(-1);
        public string ownerId;
        public string[] all = Array.Empty<string>(), any = Array.Empty<string>(), none = Array.Empty<string>();
        public bool includeChildren;
    }
    public sealed class TagSnapshot
    {
        readonly TagDefinition[] definitions;
        readonly TagConflictGroup[] groups;
        readonly Dictionary<string, TagDefinition> catalog;
        readonly TagGrant[] raw, effective;
        readonly TagStage stage;

        internal TagSnapshot(TagDefinition[] definitions, TagConflictGroup[] groups, TagGrant[] grants, TagStage stage)
        {
            this.definitions = definitions.Select(d => new TagDefinition { id = d.id, name = d.name, parentId = d.parentId,
                allowedOwners = (TagOwnerKind[])d.allowedOwners.Clone() }).ToArray();
            this.groups = groups.Select(g => new TagConflictGroup { id = g.id, tagIds = (string[])g.tagIds.Clone(), policy = g.policy }).ToArray();
            catalog = this.definitions.ToDictionary(d => d.id, StringComparer.Ordinal);
            this.stage = stage;
            raw = Sort(grants.Select(Copy));
            effective = Resolve();
        }

        // Read-only containers alone do not protect their mutable DTO elements.
        public IReadOnlyList<TagGrant> Raw => Array.AsReadOnly(raw.Select(Copy).ToArray());
        public IReadOnlyList<TagGrant> Effective => Array.AsReadOnly(effective.Select(Copy).ToArray());

        public bool Has(TagOwnerKind ownerKind, string ownerId, string tagId, bool includeChildren = false)
        {
            ValidateOwner(ownerKind, ownerId);
            ValidateTag(tagId);
            return HasCore(ownerKind, ownerId, tagId, includeChildren);
        }

        public bool Matches(TagQuery query)
        {
            if (query == null) throw new ArgumentException("Missing tag query");
            ValidateOwner(query.ownerKind, query.ownerId);
            if (query.all == null || query.any == null || query.none == null ||
                (long)query.all.Length + query.any.Length + query.none.Length > CompetitionTags.MaxQueryTerms)
                throw new ArgumentException("Invalid tag query size");
            // Validate all terms before short-circuit evaluation so unknown NONE/ANY terms cannot hide.
            foreach (var id in query.all.Concat(query.any).Concat(query.none)) ValidateTag(id);
            return query.all.All(id => HasCore(query.ownerKind, query.ownerId, id, query.includeChildren)) &&
                (query.any.Length == 0 || query.any.Any(id => HasCore(query.ownerKind, query.ownerId, id, query.includeChildren))) &&
                !query.none.Any(id => HasCore(query.ownerKind, query.ownerId, id, query.includeChildren));
        }

        public TagSnapshot WithoutSource(string sourceKind, string sourceId)
        {
            CompetitionTags.ValidateId(sourceKind); CompetitionTags.ValidateId(sourceId);
            return CompetitionTags.Build(definitions, groups,
                raw.Where(g => g.sourceKind != sourceKind || g.sourceId != sourceId), stage);
        }

        bool HasCore(TagOwnerKind ownerKind, string ownerId, string tagId, bool includeChildren)
        {
            foreach (var grant in effective)
            {
                if (grant.ownerKind != ownerKind || grant.ownerId != ownerId) continue;
                string candidate = grant.tagId;
                while (candidate != null)
                {
                    if (candidate == tagId) return true;
                    if (!includeChildren) break;
                    candidate = catalog[candidate].parentId;
                }
            }
            return false;
        }

        void ValidateTag(string id)
        {
            CompetitionTags.ValidateId(id);
            if (!catalog.ContainsKey(id)) throw new ArgumentException("Unknown tag: " + id);
        }

        static void ValidateOwner(TagOwnerKind kind, string id)
        {
            if (!Enum.IsDefined(typeof(TagOwnerKind), kind)) throw new ArgumentException("Unknown tag owner");
            CompetitionTags.ValidateId(id);
        }

        TagGrant[] Resolve()
        {
            var visible = raw.Where(g => (int)g.stage <= (int)stage).ToList();
            foreach (var group in groups.OrderBy(g => g.id, StringComparer.Ordinal))
            {
                foreach (var owner in visible.Where(g => group.tagIds.Contains(g.tagId)).GroupBy(OwnerKey).ToArray())
                {
                    var tags = owner.Select(g => g.tagId).Distinct(StringComparer.Ordinal).ToArray();
                    if (tags.Length < 2) continue;
                    if (group.policy == TagConflictPolicy.Reject) throw new ArgumentException("Conflicting tags in group: " + group.id);
                    int maximum = owner.Max(g => g.priority);
                    var winners = owner.Where(g => g.priority == maximum).Select(g => g.tagId).Distinct(StringComparer.Ordinal).ToArray();
                    if (winners.Length != 1) throw new ArgumentException("Ambiguous tag priority in group: " + group.id);
                    string winner = winners[0];
                    visible.RemoveAll(g => OwnerKey(g) == owner.Key && group.tagIds.Contains(g.tagId) && g.tagId != winner);
                }
            }
            return Sort(visible.GroupBy(g => OwnerKey(g) + "|" + g.tagId, StringComparer.Ordinal)
                .Select(g => g.OrderByDescending(x => x.priority).First()));
        }

        static string OwnerKey(TagGrant g) => ((int)g.ownerKind).ToString(CultureInfo.InvariantCulture) + "|" + g.ownerId;
        static TagGrant[] Sort(IEnumerable<TagGrant> grants) => grants.OrderBy(g => g.ownerKind)
            .ThenBy(g => g.ownerId, StringComparer.Ordinal).ThenBy(g => g.tagId, StringComparer.Ordinal)
            .ThenBy(g => g.sourceKind, StringComparer.Ordinal).ThenBy(g => g.sourceId, StringComparer.Ordinal)
            .ThenBy(g => g.ordinal).ThenBy(g => g.stage).ThenBy(g => g.priority).ToArray();

        internal static TagGrant Copy(TagGrant g) => new TagGrant { tagId = g.tagId, ownerKind = g.ownerKind, ownerId = g.ownerId,
            sourceKind = g.sourceKind, sourceId = g.sourceId, ordinal = g.ordinal, priority = g.priority, stage = g.stage };
    }
    public static class CompetitionTags
    {
        public const int MaxDefinitions = 128, MaxGroups = 32, MaxGrants = 512, MaxQueryTerms = 32, MaxIdLength = 128;

        public static void Validate(TagDefinition[] definitions, TagConflictGroup[] groups)
        {
            if (definitions == null || definitions.Length > MaxDefinitions || groups == null || groups.Length > MaxGroups)
                throw new ArgumentException("Invalid tag catalog size");
            var catalog = new Dictionary<string, TagDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Missing tag definition");
                ValidateId(definition.id);
                if (string.IsNullOrWhiteSpace(definition.name) || definition.name.Length > 128 || definition.name.Any(char.IsControl) ||
                    definition.allowedOwners == null || definition.allowedOwners.Length == 0 || definition.allowedOwners.Length > 9 ||
                    definition.allowedOwners.Any(o => !Enum.IsDefined(typeof(TagOwnerKind), o)) ||
                    definition.allowedOwners.Distinct().Count() != definition.allowedOwners.Length || catalog.ContainsKey(definition.id))
                    throw new ArgumentException("Invalid tag definition: " + definition.id);
                if (definition.parentId != null) ValidateId(definition.parentId);
                catalog.Add(definition.id, definition);
            }
            foreach (var definition in definitions)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal) { definition.id };
                string parentId = definition.parentId;
                while (parentId != null)
                {
                    if (!catalog.TryGetValue(parentId, out var parent) || !visited.Add(parentId)) throw new ArgumentException("Invalid tag parent forest");
                    parentId = parent.parentId;
                }
            }
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            var groupedTags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in groups)
            {
                if (group == null) throw new ArgumentException("Missing tag conflict group");
                ValidateId(group.id);
                if (!groupIds.Add(group.id) || !Enum.IsDefined(typeof(TagConflictPolicy), group.policy) ||
                    group.tagIds == null || group.tagIds.Length < 2 || group.tagIds.Length > MaxDefinitions)
                    throw new ArgumentException("Invalid tag conflict group");
                foreach (string id in group.tagIds)
                {
                    ValidateId(id);
                    if (!catalog.ContainsKey(id) || !groupedTags.Add(id)) throw new ArgumentException("Unknown or overlapping conflict tag");
                }
            }
        }

        public static TagSnapshot Build(TagDefinition[] definitions, TagConflictGroup[] groups, IEnumerable<TagGrant> grants, TagStage stage)
        {
            Validate(definitions, groups);
            if (grants == null || !Enum.IsDefined(typeof(TagStage), stage)) throw new ArgumentException("Invalid tag snapshot input");
            var catalog = definitions.ToDictionary(d => d.id, StringComparer.Ordinal);
            var copy = new List<TagGrant>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var grant in grants)
            {
                if (copy.Count >= MaxGrants) throw new ArgumentException("Too many tag grants");
                if (grant == null) throw new ArgumentException("Missing tag grant");
                ValidateId(grant.tagId); ValidateId(grant.ownerId); ValidateId(grant.sourceKind); ValidateId(grant.sourceId);
                if (!catalog.TryGetValue(grant.tagId, out var definition) || !definition.allowedOwners.Contains(grant.ownerKind) ||
                    !Enum.IsDefined(typeof(TagStage), grant.stage) || grant.ordinal < 0)
                    throw new ArgumentException("Invalid tag grant or owner");
                string key = ((int)grant.ownerKind).ToString(CultureInfo.InvariantCulture) + "|" + grant.ownerId + "|" + grant.sourceKind + "|" +
                    grant.sourceId + "|" + grant.ordinal.ToString(CultureInfo.InvariantCulture);
                if (!keys.Add(key)) throw new ArgumentException("Duplicate tag grant provenance");
                copy.Add(TagSnapshot.Copy(grant));
            }
            return new TagSnapshot(definitions, groups, copy.ToArray(), stage);
        }

        internal static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength || id.Any(c =>
                !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-' && c != ':' && c != '/'))
                throw new ArgumentException("Invalid bounded tag identifier");
        }
    }
}
