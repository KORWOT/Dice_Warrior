using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FateDice.Tests
{
    public class ManagerTestData:UIData
    {
        public string key;
        public Action clicked;
        public bool failBind, failClose;
    }
    public sealed class ManagerDerivedData:ManagerTestData { }
    public sealed class ManagerWrongData:UIData { }
    public abstract class ManagerProbeUI:BaseUI<ManagerTestData>
    {
        public Button focusButton;
        public int initialized, bindings, closed, badEnable, delayed;
        public bool lastBindWasActive, unbindHadData;
        public string boundKey;
        public ManagerTestData CurrentData=>Data;
        UnityAction listener;
        bool failClose;
        protected override void OnInitialize(){initialized++;}
        protected override void OnBind(ManagerTestData data)
        {
            bindings++;lastBindWasActive=gameObject.activeInHierarchy;
            if(listener!=null)focusButton.onClick.RemoveListener(listener);
            boundKey=data.key;failClose=data.failClose;listener=()=>data.clicked?.Invoke();
            focusButton.onClick.AddListener(listener);
            if(data.failBind)throw new InvalidOperationException("Controlled view bind failure.");
        }
        protected override void OnUnbind()
        {
            closed++;unbindHadData=Data!=null;
            if(listener!=null)focusButton.onClick.RemoveListener(listener);
            listener=null;boundKey=null;
            if(failClose)throw new InvalidOperationException("Controlled view close failure.");
        }
        void OnEnable(){if(Data==null||string.IsNullOrEmpty(boundKey)||!IsOpen)badEnable++;}
        public void BeginDelayed(){StartCoroutine(Delayed());}
        IEnumerator Delayed(){yield return new WaitForSecondsRealtime(.04f);delayed++;focusButton.onClick.Invoke();}
    }
    public sealed class ManagerScreenA:ManagerProbeUI { }
    public sealed class ManagerScreenB:ManagerProbeUI { }
    public sealed class ManagerPopupA:ManagerProbeUI { }
    public sealed class ManagerPopupB:ManagerProbeUI { }
    public sealed class ManagerUnregisteredUI:ManagerProbeUI { }

    public sealed class UIManagerTests
    {
        readonly List<GameObject> owned=new List<GameObject>();
        UIManager manager;
        UIRoot root;
        EventSystem events;
        GameObject priorSelection;
        ManagerScreenA screenPrefab;
        [SetUp] public void Setup()
        {
            events=EventSystem.current;
            if(!events)events=MakeObject("UIManagerTestEventSystem").AddComponent<EventSystem>();
            priorSelection=events.currentSelectedGameObject;events.SetSelectedGameObject(null);
            var go=MakeObject("UIManagerRootFixture");
            root=go.AddComponent<UIRoot>();root.canvas=go.AddComponent<Canvas>();root.canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            root.inputGroup=go.AddComponent<CanvasGroup>();
            root.safeArea=Layer("SafeArea",go.transform);
            root.screenLayer=Layer("Screens",root.safeArea);root.popupLayer=Layer("Popups",root.safeArea);
            root.overlayLayer=Layer("Overlay",root.safeArea);root.cacheLayer=Layer("Cache",root.safeArea);
            root.popupBlocker=Layer("Blocker",root.popupLayer).gameObject.AddComponent<Image>();
            root.popupBlocker.raycastTarget=true;
            manager=go.AddComponent<UIManager>();manager.root=root;
            screenPrefab=Prefab<ManagerScreenA>();
            manager.prefabs=new BaseUI[]{screenPrefab,Prefab<ManagerScreenB>(),Prefab<ManagerPopupA>(),Prefab<ManagerPopupB>()};
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(events&&events.currentSelectedGameObject&&BelongsToOwned(events.currentSelectedGameObject))events.SetSelectedGameObject(null);
            foreach(var go in owned)if(go)UnityEngine.Object.Destroy(go);
            owned.Clear();yield return null;
            if(events&&priorSelection&&priorSelection.activeInHierarchy)events.SetSelectedGameObject(priorSelection);
        }
        bool BelongsToOwned(GameObject target)
        {
            foreach(var go in owned)if(go&&target.transform.IsChildOf(go.transform))return true;
            return false;
        }
        GameObject MakeObject(string name)
        {
            var go=new GameObject(name,typeof(RectTransform));owned.Add(go);return go;
        }
        static RectTransform Layer(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        T Prefab<T>() where T:ManagerProbeUI
        {
            var go=MakeObject(typeof(T).Name+"Fixture");go.SetActive(false);
            var group=go.AddComponent<CanvasGroup>();var view=go.AddComponent<T>();view.group=group;
            var button=Layer("Focus",go.transform).gameObject;button.AddComponent<Image>();view.focusButton=button.AddComponent<Button>();
            return view;
        }
        static ManagerTestData Data(string key="ready",Action clicked=null)=>new ManagerTestData{key=key,clicked=clicked};

        [Test] public void CacheUsesOneInstancePerConcreteTypeAndBindsBeforeFirstEnable()
        {
            var data=Data("first");var first=manager.Show<ManagerScreenA>(data);
            Assert.That(manager.ActiveScreen,Is.SameAs(first));Assert.That(manager.CachedCount,Is.EqualTo(1));
            Assert.That(first.Manager,Is.SameAs(manager));Assert.That(first.CurrentData,Is.SameAs(data));Assert.That(first.IsOpen,Is.True);
            Assert.That(first.initialized,Is.EqualTo(1));Assert.That(first.bindings,Is.EqualTo(1));Assert.That(first.badEnable,Is.Zero);
            Assert.That(first.lastBindWasActive,Is.False,"Initial binding must finish before OnEnable.");
            Assert.That(first.transform.parent,Is.SameAs(root.screenLayer));
            var again=manager.Show<ManagerScreenA>(new ManagerDerivedData{key="second"});
            Assert.That(again,Is.SameAs(first));Assert.That(first.closed,Is.Zero);Assert.That(first.bindings,Is.EqualTo(2));
            Assert.That(first.lastBindWasActive,Is.True,"An open screen rebind must not disable its node/fade hierarchy.");
            Assert.That(first.boundKey,Is.EqualTo("second"));Assert.That(first.initialized,Is.EqualTo(1));
            Assert.That(manager.TryGetCached<ManagerScreenA>(out var cached),Is.True);Assert.That(cached,Is.SameAs(first));
            Assert.That(manager.TryGetCached<ManagerPopupA>(out _),Is.False);Assert.That(manager.CachedCount,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator CloseRunsCleanupOnceClearsDataAndCancelsCoroutinesWithoutExecutingCallbacks()
        {
            int old=0,current=0;
            var first=manager.Show<ManagerScreenA>(Data("old",()=>old++));
            first.BeginDelayed();
            var second=manager.Show<ManagerScreenB>(Data("other"));
            Assert.That(first.closed,Is.EqualTo(1));Assert.That(first.unbindHadData,Is.True);
            Assert.That(first.IsOpen,Is.False);Assert.That(first.CurrentData,Is.Null);Assert.That(first.gameObject.activeSelf,Is.False);
            Assert.That(first.transform.parent,Is.SameAs(root.cacheLayer));
            first.focusButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.07f);
            Assert.That(first.delayed,Is.Zero);Assert.That(old,Is.Zero,"Closing UI cannot execute a game command.");
            var reopened=manager.Show<ManagerScreenA>(Data("new",()=>current++));Assert.That(reopened,Is.SameAs(first));
            Assert.That(first.initialized,Is.EqualTo(1));first.focusButton.onClick.Invoke();Assert.That(current,Is.EqualTo(1));Assert.That(old,Is.Zero);
            manager.CloseAll();manager.CloseAll();
            Assert.That(first.closed,Is.EqualTo(2));Assert.That(second.closed,Is.EqualTo(1));
            Assert.That(manager.ActiveScreen,Is.Null);Assert.That(manager.Popups,Is.Empty);Assert.That(manager.CachedCount,Is.EqualTo(2));
        }
        [Test] public void InvalidDataOrUnregisteredTypeCannotCloseOrRebindTheCurrentScreen()
        {
            var first=manager.Show<ManagerScreenA>(Data());
            Assert.Throws<ArgumentException>(()=>manager.Show<ManagerScreenB>(new ManagerWrongData()));
            Assert.Throws<ArgumentNullException>(()=>manager.Show<ManagerScreenB>(null));
            Assert.Throws<InvalidOperationException>(()=>manager.Show<ManagerUnregisteredUI>(Data()));
            Assert.That(manager.ActiveScreen,Is.SameAs(first));Assert.That(first.closed,Is.Zero);
            Assert.That(manager.CachedCount,Is.EqualTo(1));
        }
        [TestCase(false)] [TestCase(true)]
        public void NullOrDuplicateRegistrationsAreRejectedBeforeInstantiation(bool duplicate)
        {
            manager.prefabs=duplicate?new BaseUI[]{screenPrefab,screenPrefab}:new BaseUI[]{screenPrefab,null};
            Assert.Throws<InvalidOperationException>(()=>manager.Show<ManagerScreenA>(Data()));
            Assert.That(manager.CachedCount,Is.Zero);Assert.That(manager.ActiveScreen,Is.Null);Assert.That(manager.Popups,Is.Empty);
        }
        [UnityTest] public IEnumerator PopupStackBlocksUnderlyingViewsAndRestoresUsableFocusInOrder()
        {
            var screen=manager.Show<ManagerScreenA>(Data("screen"));events.SetSelectedGameObject(screen.focusButton.gameObject);
            var first=manager.ShowPopup<ManagerPopupA>(Data("first"));
            Assert.That(root.popupBlocker.gameObject.activeSelf,Is.True);Assert.That(screen.group.interactable,Is.False);
            Assert.That(first.group.interactable,Is.True);Assert.That(first.group.blocksRaycasts,Is.True);
            events.SetSelectedGameObject(first.focusButton.gameObject);
            var second=manager.ShowPopup<ManagerPopupB>(Data("second"));yield return null;
            Assert.That(manager.Popups.Count,Is.EqualTo(2));Assert.That(manager.Popups[1],Is.SameAs(second));
            Assert.That(first.group.interactable,Is.False);Assert.That(first.group.blocksRaycasts,Is.False);
            Assert.That(second.group.interactable,Is.True);
            Assert.That(root.popupBlocker.transform.GetSiblingIndex()+1,Is.EqualTo(second.transform.GetSiblingIndex()));
            Assert.That(first.transform.GetSiblingIndex(),Is.LessThan(root.popupBlocker.transform.GetSiblingIndex()));
            events.SetSelectedGameObject(second.focusButton.gameObject);
            Assert.That(manager.CloseTopPopup(),Is.True);
            Assert.That(second.closed,Is.EqualTo(1));Assert.That(first.group.interactable,Is.True);
            Assert.That(events.currentSelectedGameObject,Is.SameAs(first.focusButton.gameObject));
            Assert.That(manager.CloseTopPopup(),Is.True);
            Assert.That(root.popupBlocker.gameObject.activeSelf,Is.False);Assert.That(screen.group.interactable,Is.True);
            Assert.That(events.currentSelectedGameObject,Is.SameAs(screen.focusButton.gameObject));
            Assert.That(manager.CloseTopPopup(),Is.False);
        }
        [Test] public void PopupRebindMovesExistingInstanceToTopAndRoleConflictIsRejected()
        {
            var screen=manager.Show<ManagerScreenA>(Data());
            var a=manager.ShowPopup<ManagerPopupA>(Data("a"));var b=manager.ShowPopup<ManagerPopupB>(Data("b"));
            var rebound=manager.ShowPopup<ManagerPopupA>(Data("a2"));
            Assert.That(rebound,Is.SameAs(a));Assert.That(a.closed,Is.Zero);Assert.That(a.initialized,Is.EqualTo(1));Assert.That(a.bindings,Is.EqualTo(2));
            Assert.That(manager.Popups[0],Is.SameAs(b));Assert.That(manager.Popups[1],Is.SameAs(a));Assert.That(manager.CachedCount,Is.EqualTo(3));
            Assert.That(root.popupBlocker.transform.GetSiblingIndex()+1,Is.EqualTo(a.transform.GetSiblingIndex()));
            Assert.Throws<InvalidOperationException>(()=>manager.ShowPopup<ManagerScreenA>(Data()));
            Assert.Throws<InvalidOperationException>(()=>manager.Show<ManagerPopupA>(Data()));
            Assert.That(manager.ActiveScreen,Is.SameAs(screen));Assert.That(manager.Popups.Count,Is.EqualTo(2));
        }
        [Test] public void GlobalLockSurvivesPopupOpeningClosingAndScreenSwitching()
        {
            var screen=manager.Show<ManagerScreenA>(Data());manager.SetInputLocked(true);
            var popup=manager.ShowPopup<ManagerPopupA>(Data());
            Assert.That(root.inputGroup.interactable,Is.False);Assert.That(root.inputGroup.blocksRaycasts,Is.False);
            Assert.That(screen.group.interactable,Is.False);Assert.That(popup.group.interactable,Is.False);
            manager.CloseTopPopup();Assert.That(screen.group.interactable,Is.False);
            var changed=manager.Show<ManagerScreenB>(Data());Assert.That(changed.group.interactable,Is.False);
            manager.SetInputLocked(false);
            Assert.That(root.inputGroup.interactable,Is.True);Assert.That(root.inputGroup.blocksRaycasts,Is.True);Assert.That(changed.group.interactable,Is.True);
        }
        [Test] public void FailedNewScreenBindingLeavesExistingScreenPopupAndFocusUntouched()
        {
            var screen=manager.Show<ManagerScreenA>(Data());var popup=manager.ShowPopup<ManagerPopupA>(Data());
            events.SetSelectedGameObject(popup.focusButton.gameObject);
            Assert.Throws<InvalidOperationException>(()=>manager.Show<ManagerScreenB>(new ManagerTestData{key="broken",failBind=true}));
            Assert.That(manager.ActiveScreen,Is.SameAs(screen));Assert.That(manager.Popups.Count,Is.EqualTo(1));Assert.That(manager.Popups[0],Is.SameAs(popup));
            Assert.That(screen.closed,Is.Zero);Assert.That(popup.closed,Is.Zero);Assert.That(events.currentSelectedGameObject,Is.SameAs(popup.focusButton.gameObject));
            Assert.That(manager.TryGetCached<ManagerScreenB>(out var broken),Is.True);
            Assert.That(broken.IsOpen,Is.False);Assert.That(broken.gameObject.activeSelf,Is.False);Assert.That(broken.CurrentData,Is.Null);Assert.That(broken.closed,Is.EqualTo(1));
        }
        [Test] public void FailedCurrentRebindClosesPartialViewWithoutLeakingDataOrCallback()
        {
            int clicked=0;var screen=manager.Show<ManagerScreenA>(Data());
            Assert.Throws<InvalidOperationException>(()=>manager.Show<ManagerScreenA>(new ManagerTestData{key="partial",clicked=()=>clicked++,failBind=true}));
            Assert.That(manager.ActiveScreen,Is.Null);Assert.That(screen.IsOpen,Is.False);Assert.That(screen.CurrentData,Is.Null);
            Assert.That(screen.closed,Is.EqualTo(1));screen.focusButton.onClick.Invoke();Assert.That(clicked,Is.Zero);
            Assert.That(screen.transform.parent,Is.SameAs(root.cacheLayer));Assert.That(manager.CachedCount,Is.EqualTo(1));
        }
        [Test] public void ChangingScreenClosesAllPopupsAndClearsStaleSelection()
        {
            var a=manager.Show<ManagerScreenA>(Data());var popup=manager.ShowPopup<ManagerPopupA>(Data());
            events.SetSelectedGameObject(popup.focusButton.gameObject);
            var b=manager.Show<ManagerScreenB>(Data());
            Assert.That(a.closed,Is.EqualTo(1));Assert.That(popup.closed,Is.EqualTo(1));Assert.That(manager.Popups,Is.Empty);
            Assert.That(root.popupBlocker.gameObject.activeSelf,Is.False);Assert.That(manager.ActiveScreen,Is.SameAs(b));
            Assert.That(events.currentSelectedGameObject,Is.Not.SameAs(popup.focusButton.gameObject));
            Assert.That(events.currentSelectedGameObject,Is.Not.SameAs(a.focusButton.gameObject));
        }
        [Test] public void ThrowingUnbindStillClearsEveryClosedViewAndManagerReferences()
        {
            var a=manager.Show<ManagerScreenA>(new ManagerTestData{key="bad-close",failClose=true});
            var popup=manager.ShowPopup<ManagerPopupA>(Data());
            Assert.Catch<Exception>(()=>manager.CloseAll());
            Assert.That(manager.ActiveScreen,Is.Null);Assert.That(manager.Popups,Is.Empty);
            Assert.That(a.IsOpen,Is.False);Assert.That(a.CurrentData,Is.Null);Assert.That(a.gameObject.activeSelf,Is.False);Assert.That(a.closed,Is.EqualTo(1));
            Assert.That(popup.IsOpen,Is.False);Assert.That(popup.closed,Is.EqualTo(1));
            Assert.That(root.popupBlocker.gameObject.activeSelf,Is.False);
            Assert.DoesNotThrow(()=>manager.CloseAll());
        }
        [Test] public void RootAppliesSafeAreaKeepsCacheInactiveAndPreservesAuthoredViewAnchors()
        {
            var authored=(RectTransform)screenPrefab.transform;
            authored.anchorMin=new Vector2(.1f,.2f);authored.anchorMax=new Vector2(.9f,.8f);
            authored.offsetMin=new Vector2(7,8);authored.offsetMax=new Vector2(-9,-10);
            root.ApplySafeArea();var screen=manager.Show<ManagerScreenA>(Data());Canvas.ForceUpdateCanvases();
            Assert.That(root.cacheLayer.gameObject.activeSelf,Is.False);
            Assert.That(root.safeArea.anchorMin.x,Is.EqualTo(Screen.safeArea.xMin/Screen.width).Within(.001));
            Assert.That(root.safeArea.anchorMax.y,Is.EqualTo(Screen.safeArea.yMax/Screen.height).Within(.001));
            var rect=(RectTransform)screen.transform;
            Assert.That(rect.anchorMin,Is.EqualTo(authored.anchorMin));Assert.That(rect.anchorMax,Is.EqualTo(authored.anchorMax));
            Assert.That(rect.offsetMin.x,Is.EqualTo(authored.offsetMin.x).Within(.01));Assert.That(rect.offsetMax.y,Is.EqualTo(authored.offsetMax.y).Within(.01));
            Assert.That(root.overlayLayer.GetComponentsInChildren<Graphic>(true),Is.Empty,"The overlay has no automatic blocking graphic.");
        }
    }
}
