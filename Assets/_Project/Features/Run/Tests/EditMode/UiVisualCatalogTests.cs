using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class UiVisualCatalogTests
    {
        FateDiceVisualCatalog catalog;
        GameConfigData config;

        [SetUp]
        public void SetUp()
        {
            config = FateDice.Editor.PrototypeAuthoring.CreateDefaults();
            catalog = ScriptableObject.CreateInstance<FateDiceVisualCatalog>();
            catalog.nodes = Enum.GetValues(typeof(NodeType)).Cast<NodeType>()
                .Select(type => new NodeVisualEntry { type=type, visual=Art("node-"+type) }).ToArray();
            catalog.fates = Enum.GetValues(typeof(NodeType)).Cast<NodeType>()
                .Select(type => new NodeVisualEntry { type=type, visual=Art("public-"+type) }).ToArray();
            catalog.grades = Enum.GetValues(typeof(Grade)).Cast<Grade>()
                .Select(grade => new GradeVisualEntry { grade=grade }).ToArray();
            catalog.buttons = Enum.GetValues(typeof(ButtonPurpose)).Cast<ButtonPurpose>()
                .Select(purpose => new ButtonAppearance { purpose=purpose, normal=Color.white, selected=Color.green,
                    pressed=Color.gray, disabled=Color.black }).ToArray();
            catalog.defaultAction = Art("action fallback");
            catalog.defaultEvent = Art("event fallback");
        }

        [TearDown]
        public void TearDown()
        {
            if (catalog != null) UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void EmptyOptionalImagesAndContentMappingsRemainReadableAndLeaveRulesUnchanged()
        {
            string before = JsonUtility.ToJson(config);
            Assert.DoesNotThrow(() => catalog.Validate(config));
            Assert.That(catalog.ResolveAction(config,config.combat.actions[0].id), Is.SameAs(catalog.defaultAction));
            Assert.That(catalog.ResolveEvent(config,config.world.events[0].id), Is.SameAs(catalog.defaultEvent));
            foreach (NodeType type in Enum.GetValues(typeof(NodeType)))
            {
                Assert.That(catalog.ResolveNode(type).fallbackGlyph, Is.EqualTo("node-"+type));
                Assert.That(catalog.ResolveFate(type).fallbackGlyph, Is.EqualTo("public-"+type));
            }
            foreach (Grade grade in Enum.GetValues(typeof(Grade)))
            {
                Assert.That(catalog.ResolveGrade(grade).grade, Is.EqualTo(grade));
                Assert.That(catalog.ResolveGrade(grade).border, Is.Null);
                Assert.That(catalog.ResolveGrade(grade).badge, Is.Null);
            }
            foreach (ButtonPurpose purpose in Enum.GetValues(typeof(ButtonPurpose)))
                Assert.That(catalog.ResolveButton(purpose).purpose, Is.EqualTo(purpose));
            Assert.That(JsonUtility.ToJson(config), Is.EqualTo(before));
        }

        [TestCase("action")]
        [TestCase("event")]
        public void UnknownContentNeverSilentlyUsesDefaultArtwork(string domain)
        {
            foreach (string id in new[]{"missing-visual-test-id", "", null})
            {
                var error = Assert.Throws<InvalidOperationException>(() => {
                    if (domain=="action") catalog.ResolveAction(config,id); else catalog.ResolveEvent(config,id);
                });
                StringAssert.Contains(domain, error.Message.ToLowerInvariant());
            }
        }

        [Test]
        public void OriginalStableIdsSelectArtworkRegardlessOfArrayOrderOrDisplayName()
        {
            var first = config.combat.actions[0];
            var second = config.combat.actions[1];
            first.label = second.label = "Same display name";
            var firstArt = Art("first");
            var secondArt = Art("second");
            catalog.actions = new[]{
                new ActionVisualEntry {contentId=second.id,visual=secondArt},
                new ActionVisualEntry {contentId=first.id,visual=firstArt}
            };
            Array.Reverse(config.combat.actions);
            catalog.Validate(config);
            Assert.That(catalog.ResolveAction(config,first.id), Is.SameAs(firstArt));
            Assert.That(catalog.ResolveAction(config,second.id), Is.SameAs(secondArt));
        }

        [TestCase("actions", "duplicate")]
        [TestCase("events", "duplicate")]
        [TestCase("actions", "unknown")]
        [TestCase("events", "unknown")]
        public void InvalidContentMappingReportsTheFieldPath(string domain, string fault)
        {
            string id = domain=="actions" ? config.combat.actions[0].id : config.world.events[0].id;
            string other = fault=="duplicate" ? id : "unknown-content-id";
            if (domain=="actions") catalog.actions = new[]{
                new ActionVisualEntry{contentId=id},new ActionVisualEntry{contentId=other}
            };
            else catalog.events = new[]{
                new EventVisualEntry{contentId=id},new EventVisualEntry{contentId=other}
            };
            var error = Assert.Throws<InvalidOperationException>(() => catalog.Validate(config));
            StringAssert.Contains(domain+"[1].contentId", error.Message);
        }

        [TestCase("nodes")]
        [TestCase("fates")]
        [TestCase("grades")]
        [TestCase("buttons")]
        public void RequiredEnumMappingsRejectNullDuplicateInvalidAndMissingRows(string domain)
        {
            foreach (string fault in new[]{"null","duplicate","invalid","missing"})
            {
                SetUpEnumFault(domain,fault);
                var error = Assert.Throws<InvalidOperationException>(() => catalog.Validate(config));
                StringAssert.Contains(domain, error.Message);
                RestoreEnumRows();
            }
        }

        [TestCase("actions")]
        [TestCase("events")]
        public void NullContentMappingRowsAreErrorsRatherThanOptionalPictures(string domain)
        {
            if (domain=="actions") catalog.actions = new ActionVisualEntry[]{null};
            else catalog.events = new EventVisualEntry[]{null};
            var error = Assert.Throws<InvalidOperationException>(() => catalog.Validate(config));
            StringAssert.Contains(domain+"[0]",error.Message);
        }

        [Test]
        public void NullVisualForValidContentFallsBackWhileAnImageFreeMappedGlyphIsKept()
        {
            string actionId = config.combat.actions[0].id, eventId = config.world.events[0].id;
            catalog.actions = new[]{new ActionVisualEntry{contentId=actionId,visual=null}};
            catalog.events = new[]{new EventVisualEntry{contentId=eventId,visual=null}};
            catalog.Validate(config);
            Assert.That(catalog.ResolveAction(config,actionId), Is.SameAs(catalog.defaultAction));
            Assert.That(catalog.ResolveEvent(config,eventId), Is.SameAs(catalog.defaultEvent));
            var mapped = Art("specific without sprite");
            catalog.actions[0].visual = mapped;
            Assert.That(catalog.ResolveAction(config,actionId), Is.SameAs(mapped));
            Assert.That(mapped.icon, Is.Null);
            Assert.That(mapped.artwork, Is.Null);
        }

        [Test]
        public void NodeFadeRejectsNegativeAndNonFiniteSeconds()
        {
            foreach (float seconds in new[]{-.1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                catalog.nodeFadeSeconds = seconds;
                var error = Assert.Throws<InvalidOperationException>(() => catalog.Validate(config));
                StringAssert.Contains("nodeFadeSeconds",error.Message);
            }
            catalog.nodeFadeSeconds = 0;
            Assert.DoesNotThrow(() => catalog.Validate(config));
        }

        [Test]
        public void OneSpriteCanBeSharedWithoutChangingContentOrRunState()
        {
            var texture = new Texture2D(2,2);
            var sprite = Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f));
            try
            {
                var art = Art("shared"); art.icon = sprite; art.artwork = sprite;
                catalog.actions = config.combat.actions.Take(2).Select(x => new ActionVisualEntry{contentId=x.id,visual=art}).ToArray();
                catalog.events = new[]{new EventVisualEntry{contentId=config.world.events[0].id,visual=art}};
                catalog.nodes[0].visual = art; catalog.fates[0].visual = art;
                catalog.grades[0].border = sprite; catalog.grades[0].badge = sprite; catalog.buttons[0].icon = sprite;
                var state = new RunState {config=config,rngState=731,eventsResolved=4,sequence=9};
                string before = JsonUtility.ToJson(state);
                catalog.Validate(config);
                foreach (var row in catalog.actions)
                {
                    Assert.That(catalog.ResolveAction(config,row.contentId).icon, Is.SameAs(sprite));
                    Assert.That(catalog.ResolveAction(config,row.contentId).artwork, Is.SameAs(sprite));
                }
                Assert.That(catalog.ResolveEvent(config,config.world.events[0].id).artwork, Is.SameAs(sprite));
                Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PublicFateLookupHasNoEventDependencyOrHiddenEventFallback()
        {
            var publicVisual = catalog.fates.Single(x=>x.type==NodeType.Combat).visual;
            catalog.defaultEvent = Art("SECRET EVENT");
            catalog.events = new[]{new EventVisualEntry{contentId="invalid-hidden-event-id",visual=Art("SECRET ENEMY")}};
            Assert.That(catalog.ResolveFate(NodeType.Combat), Is.SameAs(publicVisual));
            Assert.That(catalog.ResolveFate(NodeType.Combat).fallbackGlyph, Is.EqualTo("public-Combat"));
        }

        [Test]
        public void InvalidEnumLookupsAreExplicitErrors()
        {
            Assert.Throws<InvalidOperationException>(()=>catalog.ResolveNode((NodeType)99));
            Assert.Throws<InvalidOperationException>(()=>catalog.ResolveFate((NodeType)99));
            Assert.Throws<InvalidOperationException>(()=>catalog.ResolveGrade((Grade)99));
            Assert.Throws<InvalidOperationException>(()=>catalog.ResolveButton((ButtonPurpose)99));
        }

        static VisualArtwork Art(string glyph) => new VisualArtwork {fallbackGlyph=glyph,tint=Color.white};

        void RestoreEnumRows()
        {
            catalog.nodes = Enum.GetValues(typeof(NodeType)).Cast<NodeType>().Select(x=>new NodeVisualEntry{type=x}).ToArray();
            catalog.fates = Enum.GetValues(typeof(NodeType)).Cast<NodeType>().Select(x=>new NodeVisualEntry{type=x}).ToArray();
            catalog.grades = Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(x=>new GradeVisualEntry{grade=x}).ToArray();
            catalog.buttons = Enum.GetValues(typeof(ButtonPurpose)).Cast<ButtonPurpose>().Select(x=>new ButtonAppearance{purpose=x}).ToArray();
        }

        void SetUpEnumFault(string domain, string fault)
        {
            if (domain=="nodes" || domain=="fates")
            {
                var rows = domain=="nodes" ? catalog.nodes : catalog.fates;
                if (fault=="null") rows[0]=null;
                if (fault=="duplicate") rows[1].type=rows[0].type;
                if (fault=="invalid") rows[0].type=(NodeType)99;
                if (fault=="missing") rows=rows.Skip(1).ToArray();
                if (domain=="nodes") catalog.nodes=rows; else catalog.fates=rows;
            }
            else if (domain=="grades")
            {
                if (fault=="null") catalog.grades[0]=null;
                if (fault=="duplicate") catalog.grades[1].grade=catalog.grades[0].grade;
                if (fault=="invalid") catalog.grades[0].grade=(Grade)99;
                if (fault=="missing") catalog.grades=catalog.grades.Skip(1).ToArray();
            }
            else
            {
                if (fault=="null") catalog.buttons[0]=null;
                if (fault=="duplicate") catalog.buttons[1].purpose=catalog.buttons[0].purpose;
                if (fault=="invalid") catalog.buttons[0].purpose=(ButtonPurpose)99;
                if (fault=="missing") catalog.buttons=catalog.buttons.Skip(1).ToArray();
            }
        }
    }
}
