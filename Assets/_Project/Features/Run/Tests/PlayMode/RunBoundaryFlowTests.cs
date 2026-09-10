using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class RunBoundaryFlowTests
    {
        const string AppPath="Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string SceneFolder="Assets/_Project/Scenes/";
        GameApplication app;
        LocalRunStore disk;
        ProbeStore store;
        FateDiceVisualCatalog visualClone;
        string directory,defaultSave;
        bool defaultExisted;
        byte[] defaultBytes;
        RunUIController Controller=>app.controller;
        RunState State=>Controller.Session.ReadSnapshot();
        sealed class ProbeStore:IRunStore
        {
            readonly LocalRunStore inner;
            public int Saves;
            public bool Fail;
            public Action AfterSave;
            public ProbeStore(LocalRunStore value){inner=value;}
            public bool Exists=>inner.Exists;
            public RunState Load()=>inner.Load();
            public string Archive()=>inner.Archive();
            public void Save(RunState state)
            {
                Saves++;
                if(Fail)throw new IOException("Controlled boundary UI checkpoint failure.");
                inner.Save(state);AfterSave?.Invoke();
            }
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current,Is.Null,"Previous fixture leaked its application.");
            directory=Path.Combine(Path.GetTempPath(),"FateDiceBoundaryFlowTests",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);disk=new LocalRunStore(Path.Combine(directory,"run.json"));store=new ProbeStore(disk);
            defaultSave=Path.Combine(Application.persistentDataPath,"FateDiceLocal","run.json");
            defaultExisted=File.Exists(defaultSave);defaultBytes=defaultExisted?File.ReadAllBytes(defaultSave):null;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720,1280,"Fate Dice state boundary");
#endif
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if(app)Object.Destroy(app.gameObject);app=null;
            if(visualClone)Object.Destroy(visualClone);visualClone=null;
            yield return null;
            try
            {
                Assert.That(GameApplication.Current,Is.Null);
                Assert.That(File.Exists(defaultSave),Is.EqualTo(defaultExisted));
                if(defaultExisted)CollectionAssert.AreEqual(defaultBytes,File.ReadAllBytes(defaultSave),"User save bytes changed.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceBoundaryFlowTests"))+Path.DirectorySeparatorChar;
                if(!string.IsNullOrEmpty(directory)&&Path.GetFullPath(directory).StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
        static GameApplication Prefab()
        {
#if UNITY_EDITOR
            var result=UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
            Assert.That(result,Is.Not.Null);return result;
#else
            throw new InvalidOperationException("Production integration tests require the Editor.");
#endif
        }
        void Prepare(string phase)
        {
            var config=Prefab().controller.config.Snapshot();
            config.fate.nodeWeights=new float[]{1,0,0,0,0};
            config.presentation.explorationDice=new RollPresentationSettings{rollSeconds=0,resultHoldSeconds=0};
            config.presentation.combatDice=new RollPresentationSettings{rollSeconds=0,resultHoldSeconds=0};
            config.presentation.actionSeconds=0;
            // Keep every action nonlethal for the post-commit render recovery fixture.
            foreach(var enemy in config.combat.enemies)enemy.maxHp=1000;
            var run=RunSession.New(config,33,config.combat.trialActionIds[0],Grade.Common);
            if(phase!="map")Assert.That(run.ChooseNode(run.State.availableNodeIds[0]),Is.True);
            if(phase=="action")
            {
                Assert.That(run.Roll(),Is.True);Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);Assert.That(run.Roll(),Is.True);
            }
            disk.Save(run.State);
        }
        IEnumerator Open()
        {
            app=GameApplication.Bootstrap(Prefab(),store,new FixedSeedSource(33));
            Assert.That(Controller.Store,Is.SameAs(store));
            yield return SceneManager.LoadSceneAsync(SceneFolder+"InGame.unity",LoadSceneMode.Single);
            yield return Until(()=>!Controller.Busy&&Controller.Session!=null&&app.sceneFlow.CurrentRole==GameSceneRole.InGame,"InGame did not settle.");
            yield return Settled();Assert.That(store.Saves,Is.Zero,"Loading a checkpoint must not rewrite it.");
        }
        [UnityTest] public IEnumerator SavedActionSurvivesPresentationFailureAndRefreshDoesNotExecuteItAgain()
        {
            Prepare("action");yield return Open();
            visualClone=Object.Instantiate(Controller.visuals);Controller.visuals=visualClone;
            var originalFallback=visualClone.defaultEvent;
            var before=State;var restingArena=((CombatUI)Controller.UI.ActiveScreen).arena.anchoredPosition;var card=before.cards[0];var oracle=new RunSession(before);
            Assert.That(oracle.ChooseAction(card.id),Is.True);Assert.That(oracle.State.phase,Is.EqualTo(RunPhase.CombatRoll));
            store.AfterSave=()=>{store.AfterSave=null;visualClone.defaultEvent=null;};
            LogAssert.Expect(LogType.Warning,"Run presentation: FateDiceVisualCatalog.defaultEvent: A fallback appearance object is required.");
            Click(Button("card-"+card.id));
            yield return Until(()=>!Controller.Busy,"A presentation exception left input locked.");
            Assert.That(store.Saves,Is.EqualTo(1));Assert.That(Stable(State),Is.EqualTo(Stable(oracle.State)));
            Assert.That(Stable(disk.Load()),Is.EqualTo(Stable(oracle.State)));
            var savedBytes=File.ReadAllBytes(disk.Path);var committed=Stable(State);
            visualClone.defaultEvent=originalFallback;
            Assert.That(Controller.RefreshView(),Is.True);Assert.That(Controller.RefreshView(),Is.True);
            yield return Settled();Assert.That(Controller.Busy,Is.False);
            Assert.That(store.Saves,Is.EqualTo(1));Assert.That(Stable(State),Is.EqualTo(committed));
            CollectionAssert.AreEqual(savedBytes,File.ReadAllBytes(disk.Path));
            Assert.That(Controller.UI.ActiveScreen,Is.TypeOf<CombatUI>());
            Assert.That(Controller.UI.Popups.LastOrDefault(),Is.TypeOf<DiceRollUI>());
            AssertRaycast(((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button);
            var combatView=(CombatUI)Controller.UI.ActiveScreen;var arena=combatView.arena;
            Assert.That(arena.anchoredPosition,Is.EqualTo(restingArena));Assert.That(combatView.IsFeedbackPlaying,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FeedbackRestoresFractionalLocalPositionWithoutCoordinateRoundTrip()
        {
            Prepare("action");yield return Open();
            var view=(CombatUI)Controller.UI.ActiveScreen;var liveArena=view.arena;
            var parent=new GameObject("Fractional anchor fixture",typeof(RectTransform));
            var child=new GameObject("Feedback arena fixture",typeof(RectTransform));
            try
            {
                var parentRect=(RectTransform)parent.transform;parentRect.sizeDelta=new Vector2(720,1000);
                var arena=(RectTransform)child.transform;arena.SetParent(parentRect,false);arena.anchorMin=arena.anchorMax=new Vector2(0,1);
                // Resolve Unity's initial anchor layout before observing the playback baseline.
                arena.anchoredPosition=new Vector2(360.125f,-440.875f);
                arena.ForceUpdateRectTransforms();yield return Settled();
                var local=arena.localPosition;var anchored=arena.anchoredPosition;view.arena=arena;
                Assert.That(local,Is.EqualTo(new Vector3(.125f,59.125f,0)),"The settled fixture must retain real fractional coordinates.");
                view.actionFeedbackSeconds=.1f;view.feedbackHoldSeconds=0;
                yield return view.PlayFeedback(new CombatFeedbackData{actionLabel="정밀 복원",grade=Grade.Common,
                    attack=30,enemyHpLost=30,enemyHpAfter=70,enemyMaxHp=100,playerMaxHp=90,playerHpAfter=90});
                Assert.That(arena.localPosition,Is.EqualTo(local),"Original local position must be exact, without anchor conversion loss.");
                Assert.That(arena.anchoredPosition,Is.EqualTo(anchored));Assert.That(view.IsFeedbackPlaying,Is.False);
            }
            finally{view.arena=liveArena;Object.Destroy(child);Object.Destroy(parent);}
        }
        [UnityTest] public IEnumerator InterruptedFeedbackRestoresCapturedSubpixelPositionBeforeNextLayout()
        {
            Prepare("action");yield return Open();
            var view=(CombatUI)Controller.UI.ActiveScreen;var liveArena=view.arena;
            var parent=new GameObject("Interrupted anchor fixture",typeof(RectTransform));
            var child=new GameObject("Interrupted feedback arena",typeof(RectTransform));
            IEnumerator playback=null,impact=null;
            try
            {
                var parentRect=(RectTransform)parent.transform;parentRect.sizeDelta=new Vector2(720,1000);
                var arena=(RectTransform)child.transform;arena.SetParent(parentRect,false);arena.anchorMin=arena.anchorMax=new Vector2(0,1);
                arena.ForceUpdateRectTransforms();
                arena.localPosition=new Vector3(-.000014f,59.000015f,0);
                var local=arena.localPosition;var anchored=new Vector2(360,-441);view.arena=arena;
                playback=view.PlayFeedback(new CombatFeedbackData{actionLabel="중단 복원",grade=Grade.Common,
                    attack=30,enemyHpLost=30,enemyHpAfter=70,enemyMaxHp=100,playerMaxHp=90,playerHpAfter=90});
                // Drive one real impact step and cancel in the same frame. No Unity layout pass
                // may change this independent preimage before the production method captures it.
                Assert.That(playback.MoveNext(),Is.True);impact=playback.Current as IEnumerator;
                Assert.That(impact,Is.Not.Null);Assert.That(impact.MoveNext(),Is.True);
                Assert.That(view.IsFeedbackPlaying,Is.True);
                Assert.That(arena.localPosition,Is.Not.EqualTo(local),"The real impact must have displaced the arena.");
                view.ResetFeedback();
                Assert.That(arena.localPosition,Is.EqualTo(local),"Cancellation must restore the exact captured subpixel position.");
                Assert.That(arena.anchoredPosition,Is.EqualTo(anchored));Assert.That(view.IsFeedbackPlaying,Is.False);
            }
            finally
            {
                (impact as IDisposable)?.Dispose();(playback as IDisposable)?.Dispose();
                view.arena=liveArena;Object.Destroy(child);Object.Destroy(parent);
            }
        }
        [UnityTest] public IEnumerator DelayedCallbackFromAnOldMapCannotChangeTheNewerDisplayedCap()
        {
            Prepare("map");yield return Open();
            var view=(ExplorationUI)Controller.UI.ActiveScreen;
            var property=typeof(BaseUI<ExplorationUIData>).GetProperty("Data",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(property,Is.Not.Null);var oldData=(ExplorationUIData)property.GetValue(view);
            var delayed=oldData.cap.clicked;var before=State;
            Click(Button("cap-cycle"));yield return Until(()=>!Controller.Busy,"Cap change did not release input.");
            yield return Settled();Assert.That(State.explorationCap,Is.EqualTo(Grade.Uncommon));
            Assert.That(State.sequence,Is.EqualTo(before.sequence+1));Assert.That(store.Saves,Is.EqualTo(1));
            var committed=Stable(State);var bytes=File.ReadAllBytes(disk.Path);
            // Replay the saved UI delegate as a delayed dispatch, after Busy is already false and Map still matches.
            delayed();yield return Until(()=>!Controller.Busy,"Rejected delayed callback left input locked.");
            Assert.That(Stable(State),Is.EqualTo(committed));Assert.That(store.Saves,Is.EqualTo(1));
            CollectionAssert.AreEqual(bytes,File.ReadAllBytes(disk.Path));
            // Editing public display DTO data itself also cannot change session rules or path state.
            oldData.nodes[0].childIds.Clear();oldData.context.presentation.explorationDice.rollSeconds=99;
            Assert.That(Stable(State),Is.EqualTo(committed));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FailedRollSaveKeepsTheExactDiceBoundaryAndRealButtonRetryWorks()
        {
            Prepare("roll");yield return Open();
            var before=State;var oracle=new RunSession(before);Assert.That(oracle.Roll(),Is.True);
            var originalBytes=File.ReadAllBytes(disk.Path);store.Fail=true;
            LogAssert.Expect(LogType.Warning,"Controlled boundary UI checkpoint failure.");
            Click(((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button);
            yield return Until(()=>!Controller.Busy,"Failed save left roll input locked.");yield return Settled();
            Assert.That(Stable(State),Is.EqualTo(Stable(before)));Assert.That(store.Saves,Is.EqualTo(1));
            CollectionAssert.AreEqual(originalBytes,File.ReadAllBytes(disk.Path));
            store.Fail=false;Assert.That(Controller.RefreshView(),Is.True);yield return Settled();
            Assert.That(store.Saves,Is.EqualTo(1));
            Click(((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button);
            yield return Until(()=>!Controller.Busy,"Successful retry left input locked.");yield return Settled();
            Assert.That(store.Saves,Is.EqualTo(2));Assert.That(Stable(State),Is.EqualTo(Stable(oracle.State)));
            Assert.That(Stable(disk.Load()),Is.EqualTo(Stable(oracle.State)));
            foreach(var card in State.cards)AssertRaycast(Button("fate-"+card.id));
            LogAssert.NoUnexpectedReceived();
        }
        Button Button(string key)
        {
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key,out var value),Is.True,"Missing command: "+key);return value;
        }
        void Click(Button button)
        {
            Assert.That(Controller.Busy,Is.False);var hit=AssertRaycast(button);var pointer=Pointer(button);
            ExecuteEvents.Execute(hit,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit,pointer,ExecuteEvents.pointerClickHandler);
        }
        GameObject AssertRaycast(Button button)
        {
            Assert.That(button,Is.Not.Null);Assert.That(button.gameObject.activeInHierarchy&&button.IsInteractable(),Is.True);
            var hits=new List<RaycastResult>();Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(Pointer(button),hits);
            var target=hits.Select(hit=>ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(value=>value!=null);
            Assert.That(target,Is.SameAs(button.gameObject),"Real authored raycast did not reach the requested button.");return target;
        }
        static PointerEventData Pointer(Button button)
        {
            var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
            return new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                position=(RectTransformUtility.WorldToScreenPoint(null,corners[0])+RectTransformUtility.WorldToScreenPoint(null,corners[2]))*.5f};
        }
        static IEnumerator Until(Func<bool> condition,string reason)
        {
            float deadline=Time.realtimeSinceStartup+10;
            while(!condition()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),reason);yield return null;}
        }
        static IEnumerator Settled(){yield return null;Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();}
        static string Stable(RunState state)
        {
            var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));copy.playedSeconds=0;
            if(copy.lastResult!=null)copy.lastResult.playedSeconds=0;return JsonUtility.ToJson(copy);
        }
    }
}
