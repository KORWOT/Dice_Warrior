using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FateDice.Tests
{
    public sealed class ReusableViewTests
    {
        readonly List<Object> owned=new List<Object>();
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var item in owned)if(item)Object.Destroy(item);
            owned.Clear();yield return null;
        }
        GameObject Root(string name,Transform parent=null)
        {
            var go=new GameObject(name,typeof(RectTransform));
            if(parent)go.transform.SetParent(parent,false);else owned.Add(go);
            return go;
        }
        Image Image(string name,Transform parent)=>Root(name,parent).AddComponent<Image>();
        Text Text(string name,Transform parent)=>Root(name,parent).AddComponent<Text>();
        CommonButtonView Frame(Transform parent=null)
        {
            var go=Root("CommonButtonFixture",parent);
            var frame=go.AddComponent<CommonButtonView>();
            frame.background=go.AddComponent<Image>();frame.button=go.AddComponent<Button>();
            frame.button.targetGraphic=frame.background;frame.group=go.AddComponent<CanvasGroup>();
            frame.border=Image("Border",go.transform);frame.icon=Image("Icon",go.transform);
            frame.label=Text("Label",go.transform);frame.iconFallback=Text("IconFallback",go.transform);
            return frame;
        }
        ExplorationNodeView Node()
        {
            var go=Root("NodeFixture");var view=go.AddComponent<ExplorationNodeView>();view.frame=Frame(go.transform);return view;
        }
        ActionCardView Action()
        {
            var go=Root("ActionFixture");var view=go.AddComponent<ActionCardView>();view.frame=Frame(go.transform);
            view.artwork=Image("Artwork",view.frame.transform);view.gradeBadge=Image("GradeBadge",view.frame.transform);
            view.artworkFallback=Text("ArtworkFallback",view.frame.transform);view.gradeLabel=Text("Grade",view.frame.transform);
            view.effectLabel=Text("Effect",view.frame.transform);view.tagsLabel=Text("Tags",view.frame.transform);
            return view;
        }
        FateCardView Fate()
        {
            var go=Root("FateFixture");var view=go.AddComponent<FateCardView>();view.frame=Frame(go.transform);
            view.artwork=Image("Artwork",view.frame.transform);view.gradeBadge=Image("GradeBadge",view.frame.transform);
            view.artworkFallback=Text("ArtworkFallback",view.frame.transform);return view;
        }
        Sprite Sprite()
        {
            var texture=new Texture2D(4,2);owned.Add(texture);
            var sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,4,2),new Vector2(.5f,.5f));owned.Add(sprite);return sprite;
        }
        static ButtonAppearance Style()=>new ButtonAppearance{purpose=ButtonPurpose.Primary,normal=Color.gray,selected=Color.cyan,pressed=Color.blue,disabled=Color.black};
        static VisualArtwork Visual(Sprite sprite=null,string fallback="X")=>new VisualArtwork{icon=sprite,artwork=sprite,tint=Color.white,fallbackGlyph=fallback};
        static GradeVisualEntry GradeVisual(Grade grade,Sprite sprite=null)=>new GradeVisualEntry{grade=grade,border=sprite,badge=sprite};

        [Test] public void CommonRebindReplacesOnlyOwnedCallbackAndRestoresPresentation()
        {
            var frame=Frame();var sprite=Sprite();var style=Style();
            int external=0,old=0,current=0;string selected=null;
            frame.button.onClick.AddListener(()=>external++);
            frame.Bind("old","Old label",sprite,"old glyph",style,true,true,_=>old++);
            frame.SetBorder(sprite,Color.red);frame.group.alpha=.2f;frame.group.interactable=false;frame.group.blocksRaycasts=false;
            frame.Bind("new","New label",null,"",style,true,false,id=>{current++;selected=id;});
            Assert.That(frame.BoundId,Is.EqualTo("new"));Assert.That(frame.label.text,Is.EqualTo("New label"));
            Assert.That(frame.icon.sprite,Is.Null);Assert.That(frame.icon.gameObject.activeSelf,Is.False);
            Assert.That(frame.iconFallback.text,Is.Empty);Assert.That(frame.border.sprite,Is.Null);
            Assert.That(frame.group.alpha,Is.EqualTo(1));Assert.That(frame.group.interactable,Is.True);Assert.That(frame.group.blocksRaycasts,Is.True);
            Assert.That(frame.button.colors.normalColor,Is.EqualTo(style.normal));Assert.That(frame.button.colors.pressedColor,Is.EqualTo(style.pressed));
            Assert.That(frame.button.colors.disabledColor,Is.EqualTo(style.disabled));
            frame.button.onClick.Invoke();
            Assert.That(old,Is.Zero);Assert.That(current,Is.EqualTo(1));Assert.That(external,Is.EqualTo(1));Assert.That(selected,Is.EqualTo("new"));
            frame.Unbind();frame.button.onClick.Invoke();
            Assert.That(frame.BoundId,Is.Null.Or.Empty);Assert.That(frame.label.text,Is.Empty);
            Assert.That(frame.button.interactable,Is.False);Assert.That(frame.group.interactable,Is.False);
            Assert.That(current,Is.EqualTo(1));Assert.That(external,Is.EqualTo(2),"Unbind must preserve listeners owned by another component.");
        }
        [Test] public void CommonOptionalIconFallbackAndDisabledInputAreExplicit()
        {
            var frame=Frame();var style=Style();var sprite=Sprite();style.icon=sprite;int clicked=0;
            frame.Bind("styled","Styled",null,"fallback",style,true,true,_=>clicked++);
            Assert.That(frame.icon.sprite,Is.SameAs(sprite));Assert.That(frame.icon.preserveAspect,Is.True);
            Assert.That(frame.button.colors.normalColor,Is.EqualTo(style.selected));
            style.icon=null;frame.Bind("disabled","Unavailable",null,"?",style,false,false,_=>clicked++);
            Assert.That(frame.icon.sprite,Is.Null);Assert.That(frame.iconFallback.text,Is.EqualTo("?"));
            Assert.That(frame.iconFallback.gameObject.activeSelf,Is.True);Assert.That(frame.button.interactable,Is.False);
            frame.button.onClick.Invoke();Assert.That(clicked,Is.Zero);
            frame.Unbind();Assert.That(frame.iconFallback.text,Is.Empty);Assert.That(frame.icon.sprite,Is.Null);
        }
        [Test] public void ActionRebindClearsSpritesTagsAndUsesCurrentOfferedIdentity()
        {
            var view=Action();var sprite=Sprite();int old=0,current=0;string selected=null;
            view.Bind("offer-old","fireball","Fireball",Grade.Rare,"28 damage","fire magic",Visual(sprite),GradeVisual(Grade.Rare,sprite),Color.magenta,Style(),_=>old++);
            view.frame.group.alpha=.2f;
            view.Bind("offer-new","strike","Strike",Grade.Common,"13 damage","",Visual(null,"S"),GradeVisual(Grade.Common),Color.gray,Style(),id=>{current++;selected=id;});
            Assert.That(view.OfferedId,Is.EqualTo("offer-new"));Assert.That(view.OriginalId,Is.EqualTo("strike"));
            Assert.That(view.frame.label.text,Is.EqualTo("Strike"));Assert.That(view.effectLabel.text,Is.EqualTo("13 damage"));Assert.That(view.tagsLabel.text,Is.Empty);
            Assert.That(view.gradeLabel.text,Is.EqualTo("일반"));Assert.That(view.gradeLabel.color,Is.EqualTo(Color.gray));
            Assert.That(view.artwork.sprite,Is.Null);Assert.That(view.gradeBadge.sprite,Is.Null);
            Assert.That(view.artworkFallback.text,Is.EqualTo("S"));Assert.That(view.artworkFallback.gameObject.activeSelf,Is.True);
            Assert.That(view.artwork.preserveAspect,Is.True);Assert.That(view.frame.group.alpha,Is.EqualTo(1));
            view.frame.button.onClick.Invoke();Assert.That(old,Is.Zero);Assert.That(current,Is.EqualTo(1));Assert.That(selected,Is.EqualTo("offer-new"));
            view.Unbind();
            Assert.That(view.OfferedId,Is.Null.Or.Empty);Assert.That(view.OriginalId,Is.Null.Or.Empty);
            Assert.That(view.gradeLabel.text,Is.Empty);Assert.That(view.effectLabel.text,Is.Empty);Assert.That(view.tagsLabel.text,Is.Empty);
            Assert.That(view.artworkFallback.text,Is.Empty);Assert.That(view.artwork.sprite,Is.Null);Assert.That(view.gradeBadge.sprite,Is.Null);
        }
        [Test] public void DuplicateOriginalActionsKeepIndependentGradeEffectAndSelection()
        {
            var first=Action();var second=Action();var sprite=Sprite();var selections=new List<string>();
            first.Bind("offer-a","strike","Strike",Grade.Common,"13 damage","physical",Visual(sprite),GradeVisual(Grade.Common),Color.gray,Style(),selections.Add);
            second.Bind("offer-b","strike","Strike",Grade.Legendary,"32 damage","physical",Visual(sprite),GradeVisual(Grade.Legendary),Color.yellow,Style(),selections.Add);
            Assert.That(first.artwork.sprite,Is.SameAs(second.artwork.sprite));
            Assert.That(first.effectLabel.text,Is.EqualTo("13 damage"));Assert.That(second.effectLabel.text,Is.EqualTo("32 damage"));
            Assert.That(first.gradeLabel.text,Is.EqualTo("일반"));Assert.That(second.gradeLabel.text,Is.EqualTo("전설"));
            second.frame.button.onClick.Invoke();first.frame.button.onClick.Invoke();
            CollectionAssert.AreEqual(new[]{"offer-b","offer-a"},selections);
        }
        [Test] public void FateAcceptsOnlyPublicPresentationAndRebindClearsOldArtwork()
        {
            var view=Fate();var sprite=Sprite();var originalName=view.gameObject.name;var selections=new List<string>();
            view.Bind("fate-old",NodeType.Rest,Grade.Rare,Visual(sprite),GradeVisual(Grade.Rare,sprite),Color.magenta,Style(),_=>Assert.Fail("Old fate callback survived."));
            view.Bind("fate-new",NodeType.Combat,Grade.Common,Visual(null,"C"),GradeVisual(Grade.Common),Color.gray,Style(),selections.Add);
            Assert.That(view.OfferedId,Is.EqualTo("fate-new"));Assert.That(view.frame.label.text,Is.EqualTo("전투  /  일반"));
            Assert.That(view.artwork.sprite,Is.Null);Assert.That(view.gradeBadge.sprite,Is.Null);Assert.That(view.artworkFallback.text,Is.EqualTo("C"));
            Assert.That(view.gameObject.name,Is.EqualTo(originalName));
            Assert.That(typeof(FateCardView).GetMethod("Bind").GetParameters().Any(x=>x.ParameterType==typeof(EventDefinition)),Is.False);
            view.frame.button.onClick.Invoke();CollectionAssert.AreEqual(new[]{"fate-new"},selections);
            view.Unbind();Assert.That(view.OfferedId,Is.Null.Or.Empty);Assert.That(view.artworkFallback.text,Is.Empty);
            Assert.That(view.frame.label.text,Is.Empty);Assert.That(view.artwork.sprite,Is.Null);
        }
        [UnityTest] public IEnumerator NodeFadeBlocksInputImmediatelyAndOldCompletionCannotHideReboundNode()
        {
            var view=Node();int old=0,current=0;
            view.Bind("node-old",NodeType.Shop,"Shop",Visual(),Style(),true,true,_=>old++);
            view.FadeOut(.05f);
            Assert.That(view.IsFading,Is.True);Assert.That(view.frame.group.interactable,Is.False);Assert.That(view.frame.group.blocksRaycasts,Is.False);
            view.frame.button.onClick.Invoke();Assert.That(old,Is.Zero);
            yield return null;
            view.Bind("node-new",NodeType.Rest,"Rest",Visual(null,"R"),Style(),true,false,_=>current++);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(view.gameObject.activeSelf,Is.True);Assert.That(view.IsFading,Is.False);
            Assert.That(view.NodeId,Is.EqualTo("node-new"));Assert.That(view.Type,Is.EqualTo(NodeType.Rest));
            Assert.That(view.frame.group.alpha,Is.EqualTo(1));Assert.That(view.frame.group.interactable,Is.True);
            view.frame.button.onClick.Invoke();Assert.That(current,Is.EqualTo(1));Assert.That(old,Is.Zero);
        }
        [UnityTest] public IEnumerator CompletedNodeFadeKeepsIdentityUntilUnbindAndReuseRestoresInput()
        {
            var view=Node();view.Bind("retired-node",NodeType.Event,"Event",Visual(),Style(),true,false,_=>{});
            view.FadeOut(.02f);var end=Time.realtimeSinceStartup+.3f;
            while(view.IsFading&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(view.IsFading,Is.False);Assert.That(view.gameObject.activeSelf,Is.False);
            Assert.That(view.NodeId,Is.EqualTo("retired-node"));Assert.That(view.Type,Is.EqualTo(NodeType.Event));
            view.Unbind();Assert.That(view.NodeId,Is.Null.Or.Empty);
            view.Bind("reused-node",NodeType.Treasure,"Treasure",Visual(),Style(),false,false,_=>Assert.Fail("Unselectable preview fired."));
            Assert.That(view.gameObject.activeSelf,Is.True);Assert.That(view.frame.group.alpha,Is.EqualTo(1));
            Assert.That(view.frame.button.interactable,Is.False);view.frame.button.onClick.Invoke();
        }
    }
}
