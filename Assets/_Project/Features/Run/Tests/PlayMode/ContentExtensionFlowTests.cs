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
    public sealed class ContentExtensionFlowTests
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
            directory=Path.Combine(Path.GetTempPath(),"FateDiceContentFlowTests",Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);disk=new LocalRunStore(Path.Combine(directory,"run.json"));store=new ProbeStore(disk);
            defaultSave=Path.Combine(Application.persistentDataPath,"FateDiceLocal","run.json");
            defaultExisted=File.Exists(defaultSave);defaultBytes=defaultExisted?File.ReadAllBytes(defaultSave):null;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720,1280,"Fate Dice content integration");
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
                string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FateDiceContentFlowTests"))+Path.DirectorySeparatorChar;
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

        void Prepare(NodeType type)
        {
            var config=Prefab().controller.config.Snapshot();config.world.mapGenerationVersion=0;
            foreach(var row in config.fate.exploration)row.weights=type==NodeType.Shop?new float[]{0,0,1,0,0}:new float[]{1,0,0,0,0};
            foreach(var row in config.fate.combat)row.weights=new float[]{0,1,0,0,0};
            foreach(var enemy in config.combat.enemies){enemy.maxHp=100000;enemy.power=0;}
            config.presentation.explorationDice=new RollPresentationSettings{rollSeconds=0,resultHoldSeconds=0};
            config.presentation.combatDice=new RollPresentationSettings{rollSeconds=0,resultHoldSeconds=0};config.presentation.actionSeconds=0;
            var initial=RunSession.New(config,33,"fireball",type==NodeType.Shop?Grade.Rare:Grade.Common).ReadSnapshot();
            foreach(var node in initial.nodes)node.type=NodeType.Combat;
            initial.nodes.Single(n=>n.id==initial.availableNodeIds[0]).type=type;initial.gold=1000;
            var run=new RunSession(initial);Assert.That(run.ChooseNode(initial.availableNodeIds[0]),Is.True);
            Assert.That(run.Roll(),Is.True);Assert.That(run.ChooseFate(run.State.cards[0].id),Is.True);
            if(type==NodeType.Treasure)Assert.That(run.ResolveEncounter(false),Is.True);
            disk.Save(run.State);
        }
        IEnumerator Open()
        {
            app=GameApplication.Bootstrap(Prefab(),store,new FixedSeedSource(33));
            yield return SceneManager.LoadSceneAsync(SceneFolder+"InGame.unity",LoadSceneMode.Single);
            yield return Until(()=>!Controller.Busy&&Controller.Session!=null&&app.sceneFlow.CurrentRole==GameSceneRole.InGame,"InGame did not settle.");
            yield return Settled();Assert.That(store.Saves,Is.Zero,"A checkpoint load must not rewrite it.");
        }
        IEnumerator Press(string key)
        {
            TestContext.WriteLine("PRESS "+key);
            var target=Button(key);EnsureVisible(target);var hits=new List<RaycastResult>();Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(Pointer(target),hits);
            var first=hits.Select(hit=>ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(value=>value!=null);
            Assert.That(first,Is.SameAs(target.gameObject),"Press "+key+" was blocked by "+(first?string.Join(" ",first.GetComponentsInChildren<Text>().Select(t=>t.text)):"nothing"));
            string before=key.StartsWith("fate-")?Stable(State):null;
            Click(Button(key));
            if(before!=null)
            {
                Assert.That(Stable(State),Is.EqualTo(before));
                yield return Press("confirm-fate");
                yield break;
            }
            yield return Until(()=>!Controller.Busy,"Command did not settle: "+key);yield return Settled();
        }
        IEnumerator Roll()
        {
            var popup=Controller.UI.Popups.LastOrDefault() as DiceRollUI;
            Assert.That(popup,Is.Not.Null);Click(popup.rollButton.button);
            yield return Until(()=>!Controller.Busy,"Dice roll did not settle.");yield return Settled();
        }
        [UnityTest] public IEnumerator ShopButtonsShowFixedPricesAndChargeExactlyTheDisplayedAmountAfterResume()
        {
            Prepare(NodeType.Shop);yield return Open();
            var prices=new[]{13,15,20,23};var ids=new[]{"potion","reroll","die","blade"};
            for(int i=0;i<ids.Length;i++)
            {
                var button=Button("buy-"+ids[i]);AssertRaycast(button);
                Assert.That(string.Join(" ",button.GetComponentsInChildren<Text>().Select(t=>t.text)),Does.Contain(prices[i]+" 골드"));
            }
            int gold=State.gold;uint rng=State.rngState;yield return Press("buy-potion");
            Assert.That(State.gold,Is.EqualTo(gold-13));Assert.That(State.rngState,Is.EqualTo(rng));
            Assert.That(Stable(disk.Load()),Is.EqualTo(Stable(State)));
            yield return Press("claim");Assert.That(State.phase,Is.EqualTo(RunPhase.Shop));
            Assert.That(Button("buy-potion").IsInteractable(),Is.False);
            Assert.That(State.shopOffers.Select(o=>o.price),Is.EqualTo(prices));
            Object.Destroy(app.gameObject);app=null;yield return null;store.Saves=0;
            yield return Open();Assert.That(State.gold,Is.EqualTo(gold-13));
            Assert.That(Button("buy-potion").IsInteractable(),Is.False);
            yield return Press("buy-reroll");Assert.That(State.gold,Is.EqualTo(gold-28));
            yield return Press("claim");Assert.That(State.rerollCharges,Is.EqualTo(3));
            yield return Press("leave");Assert.That(State.phase,Is.EqualTo(RunPhase.Map));Assert.That(State.shopOffers,Is.Empty);
        }
        [UnityTest] public IEnumerator RealRewardEquipRollAndActionButtonsUseTheAcquiredCardAndItsFallback()
        {
            Prepare(NodeType.Treasure);yield return Open();
            Assert.That(string.Join(" ",Controller.UI.ActiveScreen.GetComponentsInChildren<Text>().Select(t=>t.text)),Does.Contain("잔불 베기"));
            Assert.That(State.actionIds,Does.Not.Contain("ember_slash"));
            yield return Press("claim");Assert.That(State.actionIds.Count(x=>x=="ember_slash"),Is.EqualTo(1));
            yield return Press("equip-accept");Assert.That(State.equipmentIds[0],Is.EqualTo("ember_blade"));
            yield return Press("node-"+State.availableNodeIds[0]);yield return Roll();
            yield return Press("fate-"+State.cards[0].id);
            OfferedCard found=null;
            for(int turn=0;turn<32&&found==null;turn++)
            {
                yield return Roll();found=State.cards.FirstOrDefault(c=>c.contentId=="ember_slash");
                if(found==null)yield return Press("card-"+State.cards[0].id);
            }
            Assert.That(found,Is.Not.Null);var cardButton=Button("card-"+found.id);AssertRaycast(cardButton);
            Assert.That(string.Join(" ",cardButton.GetComponentsInChildren<Text>().Select(t=>t.text)),Does.Contain("잔불 베기"));
            var before=State;int expected=before.enemyHp-Math.Max(0,30-before.enemyShield);
            Assert.That(CombatRules.Evaluate(before,found).damage,Is.EqualTo(30));
            yield return Press("card-"+found.id);Assert.That(State.enemyHp,Is.EqualTo(expected));
            Assert.That(State.combatTurns,Is.EqualTo(before.combatTurns+1));Assert.That(Stable(disk.Load()),Is.EqualTo(Stable(State)));
            var committed=Stable(State);Assert.That(Controller.RefreshView(),Is.True);yield return Settled();Assert.That(Stable(State),Is.EqualTo(committed));
        }
        Button Button(string key)
        {
            if(Controller.UI.Popups.LastOrDefault() is FateChoiceUI fate)
            {
                if(key=="confirm-fate")return fate.confirmButton.button;
                if(key.StartsWith("fate-"))return fate.Cards.Single(card=>card.OfferedId==key.Substring(5)).frame.button;
            }
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
            EnsureVisible(button);
            var hits=new List<RaycastResult>();Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(Pointer(button),hits);
            var target=hits.Select(hit=>ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(value=>value!=null);
            Assert.That(target,Is.SameAs(button.gameObject),"Real authored raycast did not reach the requested button.");return target;
        }
        static Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        static bool Contains(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-1&&inner.xMax<=outer.xMax+1&&inner.yMin>=outer.yMin-1&&inner.yMax<=outer.yMax+1;
        static void EnsureVisible(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var scroll=button.GetComponentInParent<ScrollRect>();
            if(!scroll||!scroll.vertical)return;
            scroll.StopMovement();
            for(int i=0;i<=40&&!Contains(ScreenRect(scroll.viewport),ScreenRect(rect));i++)
            {scroll.verticalNormalizedPosition=1-i/40f;Canvas.ForceUpdateCanvases();}
            Assert.That(Contains(ScreenRect(scroll.viewport),ScreenRect(rect)),Is.True,"Button must be fully reachable inside the existing scroll viewport.");
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
