using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiceWarrior.SharedWorld.Tests
{
    public sealed class CompetitionTagTests
    {
        static TagDefinition[] Definitions() => new[] {
            new TagDefinition { id = "family", name = "계열", allowedOwners = new[] { TagOwnerKind.Skill, TagOwnerKind.Action } },
            new TagDefinition { id = "family.steel", name = "강철", parentId = "family", allowedOwners = new[] { TagOwnerKind.Skill, TagOwnerKind.Action } },
            new TagDefinition { id = "element.fire", name = "화염", allowedOwners = new[] { TagOwnerKind.Source, TagOwnerKind.Action, TagOwnerKind.Target } },
            new TagDefinition { id = "element.ice", name = "냉기", allowedOwners = new[] { TagOwnerKind.Source, TagOwnerKind.Action, TagOwnerKind.Target } },
            new TagDefinition { id = "place.world", name = "세계", allowedOwners = new[] { TagOwnerKind.Place } }
        };

        static TagGrant Grant(string tag, string source, int ordinal = 0, int priority = 0,
            TagOwnerKind owner = TagOwnerKind.Action, string ownerId = "action", TagStage stage = TagStage.BaseComposition) =>
            new TagGrant { tagId = tag, ownerKind = owner, ownerId = ownerId, sourceKind = "equipment", sourceId = source,
                ordinal = ordinal, priority = priority, stage = stage };

        static TagConflictGroup[] Group(TagConflictPolicy policy) => new[] {
            new TagConflictGroup { id = "primary-element", tagIds = new[] { "element.fire", "element.ice" }, policy = policy }
        };

        [Test] public void IdentityAndContextTagsNeverPropagateToAnotherOwner()
        {
            var snapshot = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), new[] {
                Grant("family.steel", "skill:slash", owner: TagOwnerKind.Skill, ownerId: "skill"),
                Grant("element.fire", "adventurer:scholar", owner: TagOwnerKind.Source, ownerId: "self"),
                Grant("element.ice", "enemy:ice", owner: TagOwnerKind.Target, ownerId: "target"),
                Grant("place.world", "world:first", owner: TagOwnerKind.Place, ownerId: "place")
            }, TagStage.BaseComposition);
            Assert.That(snapshot.Has(TagOwnerKind.Skill, "skill", "family.steel"), Is.True);
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "family.steel"), Is.False);
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "element.fire"), Is.False);
            Assert.That(snapshot.Has(TagOwnerKind.Source, "self", "element.ice"), Is.False);
            Assert.That(snapshot.Has(TagOwnerKind.Source, "other", "element.fire"), Is.False);
        }

        [Test] public void AllAnyNoneUseExplicitChildrenAndEmptyAnyDoesNotReject()
        {
            var snapshot = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), new[] {
                Grant("family.steel", "skill:slash"), Grant("element.fire", "wand")
            }, TagStage.BaseComposition);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action",
                all = new[] { "family.steel" }, any = new[] { "element.ice", "element.fire" }, none = new[] { "place.world" } }), Is.True);
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "family"), Is.False);
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "family", true), Is.True);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", all = new[] { "family" } }), Is.False);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", all = new[] { "family" }, includeChildren = true }), Is.True);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", none = new[] { "family" }, includeChildren = true }), Is.False);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", all = new[] { "element.fire", "element.ice" } }), Is.False);
            Assert.That(snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", any = new[] { "element.ice" } }), Is.False);
        }

        [Test] public void DuplicateMembershipKeepsEverySourceAndRemovalOnlyRemovesThatSource()
        {
            var grants = new[] { Grant("element.fire", "wand", 0), Grant("element.fire", "wand", 1), Grant("element.fire", "charm") };
            var snapshot = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), grants, TagStage.BaseComposition);
            Assert.That(snapshot.Raw.Count, Is.EqualTo(3)); Assert.That(snapshot.Effective.Count, Is.EqualTo(1));
            var removed = snapshot.WithoutSource("equipment", "wand");
            Assert.That(removed.Has(TagOwnerKind.Action, "action", "element.fire"), Is.True);
            Assert.That(removed.Raw.Select(g => g.sourceId), Is.EqualTo(new[] { "charm" }));
            Assert.That(snapshot.Raw.Count, Is.EqualTo(3), "Source removal must return a separate snapshot.");
            Assert.That(removed.WithoutSource("equipment", "charm").Has(TagOwnerKind.Action, "action", "element.fire"), Is.False);
        }

        [Test] public void ConflictIsLocalToOwnerAndRejectPolicyNeverDiscardsRawEvidence()
        {
            var grants = new[] { Grant("element.fire", "wand"), Grant("element.ice", "charm", owner: TagOwnerKind.Target, ownerId: "target") };
            var snapshot = CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Reject), grants, TagStage.BaseComposition);
            Assert.That(snapshot.Effective.Count, Is.EqualTo(2));
            grants[1].ownerKind = TagOwnerKind.Action; grants[1].ownerId = "action";
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Reject), grants, TagStage.BaseComposition));
            Assert.That(grants[0].tagId, Is.EqualTo("element.fire")); Assert.That(grants[1].tagId, Is.EqualTo("element.ice"));
        }

        [Test] public void PrioritySelectsOneTagPreservesProvenanceAndRestoresSuppressedTag()
        {
            var grants = new[] { Grant("element.fire", "wand", priority: 20), Grant("element.fire", "ring", priority: 20), Grant("element.ice", "charm", priority: 10) };
            var snapshot = CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Priority), grants, TagStage.PostConversion);
            Assert.That(snapshot.Raw.Count, Is.EqualTo(3));
            Assert.That(snapshot.Effective.Select(g => g.tagId), Is.EqualTo(new[] { "element.fire" }));
            Assert.That(snapshot.WithoutSource("equipment", "wand").Has(TagOwnerKind.Action, "action", "element.fire"), Is.True);
            var restored = snapshot.WithoutSource("equipment", "wand").WithoutSource("equipment", "ring");
            Assert.That(restored.Has(TagOwnerKind.Action, "action", "element.ice"), Is.True);
            grants[2].priority = 20;
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Priority), grants, TagStage.PostConversion));
        }

        [Test] public void StageVisibilityIsFrozenAndFutureGrantDoesNotRetroactivelyChangeSnapshot()
        {
            var grants = new List<TagGrant> { Grant("element.fire", "base", priority: 1),
                Grant("element.ice", "conversion", priority: 2, stage: TagStage.PostConversion) };
            var early = CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Priority), grants, TagStage.BaseComposition);
            Assert.That(early.Raw.Count, Is.EqualTo(2));
            Assert.That(early.Has(TagOwnerKind.Action, "action", "element.fire"), Is.True);
            Assert.That(early.Has(TagOwnerKind.Action, "action", "element.ice"), Is.False);
            var late = CompetitionTags.Build(Definitions(), Group(TagConflictPolicy.Priority), grants, TagStage.PostConversion);
            Assert.That(late.Has(TagOwnerKind.Action, "action", "element.ice"), Is.True);
            Assert.That(late.Has(TagOwnerKind.Action, "action", "element.fire"), Is.False);
            grants.Clear();
            Assert.That(early.Raw.Count, Is.EqualTo(2));
            Assert.That(early.Has(TagOwnerKind.Action, "action", "element.fire"), Is.True);
        }

        [Test] public void InputAndReturnedObjectsCannotMutateTheFrozenSnapshot()
        {
            var definitions = Definitions(); var groups = Group(TagConflictPolicy.Priority);
            var grants = new[] { Grant("family.steel", "skill:slash"), Grant("element.fire", "wand", priority: 2) };
            var snapshot = CompetitionTags.Build(definitions, groups, grants, TagStage.PostConversion);
            definitions[1].parentId = null; definitions[1].allowedOwners[0] = TagOwnerKind.Place;
            groups[0].tagIds[0] = "family.steel"; grants[0].tagId = "element.ice";
            snapshot.Raw[0].tagId = "element.ice"; snapshot.Effective[0].ownerId = "other";
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "family", true), Is.True);
            Assert.That(snapshot.Has(TagOwnerKind.Action, "action", "family.steel"), Is.True);
            Assert.That(snapshot.Raw.Select(g => g.tagId), Does.Contain("family.steel"));
            Assert.Throws<NotSupportedException>(() => ((IList<TagGrant>)snapshot.Raw).Add(Grant("element.ice", "injected")));
        }

        [Test] public void OrdinalOrderingIsStableAcrossInputEnumerationOrder()
        {
            var grants = new[] { Grant("element.fire", "a2", 2), Grant("element.fire", "a10", 1), Grant("family.steel", "z", 0) };
            var first = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), grants, TagStage.BaseComposition);
            var reverse = CompetitionTags.Build(Definitions().Reverse().ToArray(), Array.Empty<TagConflictGroup>(), grants.Reverse(), TagStage.BaseComposition);
            Assert.That(first.Raw.Select(g => g.sourceId), Is.EqualTo(new[] { "a10", "a2", "z" }));
            Assert.That(reverse.Raw.Select(g => g.sourceId), Is.EqualTo(first.Raw.Select(g => g.sourceId)));
            Assert.That(reverse.Effective.Select(g => g.tagId), Is.EqualTo(first.Effective.Select(g => g.tagId)));
        }

        [Test] public void InvalidCatalogParentsGroupsAndOwnersAreRejected()
        {
            var definitions = Definitions(); definitions[0].parentId = "family.steel";
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()), "cycle");
            definitions = Definitions(); definitions[1].parentId = "missing";
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()), "unknown parent");
            definitions = Definitions(); definitions[1].id = definitions[0].id;
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()), "duplicate ID");
            definitions = Definitions(); definitions[0].allowedOwners = new[] { (TagOwnerKind)99 };
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()), "unknown owner");
            var groups = Group(TagConflictPolicy.Reject); groups[0].tagIds[0] = "missing";
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(Definitions(), groups), "unknown group tag");
            groups = Group(TagConflictPolicy.Reject).Concat(new[] { new TagConflictGroup { id = "overlap", tagIds = new[] { "element.fire", "family.steel" } } }).ToArray();
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(Definitions(), groups), "overlapping conflict groups");
        }

        [Test] public void InvalidGrantsAndQueriesCannotSilentlyMatch()
        {
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(),
                new[] { Grant("missing", "wand") }, TagStage.BaseComposition));
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(),
                new[] { Grant("place.world", "world") }, TagStage.BaseComposition), "disallowed grant owner");
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(),
                new[] { Grant("element.fire", "wand"), Grant("element.ice", "wand") }, TagStage.BaseComposition), "duplicate provenance key");
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(),
                new[] { Grant("element.fire", "wand", ordinal: -1) }, TagStage.BaseComposition));
            var snapshot = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), Array.Empty<TagGrant>(), TagStage.BaseComposition);
            Assert.Throws<ArgumentException>(() => snapshot.Has(TagOwnerKind.Action, "action", "missing"));
            Assert.Throws<ArgumentException>(() => snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", none = new[] { "missing" } }));
            Assert.Throws<ArgumentException>(() => snapshot.Matches(new TagQuery { ownerKind = (TagOwnerKind)99, ownerId = "action" }));
        }

        [Test] public void FiniteBoundsRejectOversizedCatalogIdsQueriesAndStreamingGrants()
        {
            var definitions = Enumerable.Range(0, 129).Select(i => new TagDefinition { id = "tag." + i, name = "tag", allowedOwners = new[] { TagOwnerKind.Action } }).ToArray();
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()));
            definitions = Definitions(); definitions[0].id = new string('a', 129);
            Assert.Throws<ArgumentException>(() => CompetitionTags.Validate(definitions, Array.Empty<TagConflictGroup>()));
            var snapshot = CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(), Array.Empty<TagGrant>(), TagStage.BaseComposition);
            Assert.Throws<ArgumentException>(() => snapshot.Matches(new TagQuery { ownerKind = TagOwnerKind.Action, ownerId = "action", all = Enumerable.Repeat("element.fire", 33).ToArray() }));
            int enumerated = 0;
            Assert.Throws<ArgumentException>(() => CompetitionTags.Build(Definitions(), Array.Empty<TagConflictGroup>(),
                ManyGrants(() => ++enumerated), TagStage.BaseComposition));
            Assert.That(enumerated, Is.EqualTo(513), "An over-limit enumerable must stop before consuming the unbounded tail.");
        }

        static IEnumerable<TagGrant> ManyGrants(Action next)
        {
            for (int i = 0; i < 600; ++i) { next(); yield return Grant("element.fire", "wand", i); }
        }
    }
}
