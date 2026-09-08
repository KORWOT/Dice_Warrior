using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Tests
{
    public sealed class FateDiceGuiTests
    {
        private FateDiceScreen screen;
        private string saveDirectory;
        [UnitySetUp] public IEnumerator LoadProductScene()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720,1280,"Fate Dice 9:16");
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/FateDicePrototype.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;yield return null;
            screen=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<FateDiceScreen>()).Single();
            Assert.That(screen.enabled,Is.True);Assert.That(screen.Widgets,Is.Not.Null);
            saveDirectory=Path.Combine(Path.GetTempPath(),"FateDiceGuiTests",System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(saveDirectory);screen.UseStore(new LocalRunStore(Path.Combine(saveDirectory,"run.json")));
        }
        [UnityTest] public IEnumerator RealButtonsCompleteSectionAndRestart()
        {
            Assert.That(screen.Widgets.Buttons.ContainsKey("new"),Is.True);
            Press("new");yield return WaitUnlocked();
            Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Map));
            var steps=0;
            while(Session().State.phase!=RunPhase.Result)
            {
                Assert.That(steps++,Is.LessThan(400),"Journey did not terminate.");
                var state=Session().State;string key;
                switch(state.phase)
                {
                    case RunPhase.Map:
                        var node=state.nodes.Where(x=>state.availableNodeIds.Contains(x.id))
                            .OrderBy(x=>Preference(x.type)).First();
                        key="node-"+node.id;break;
                    case RunPhase.ExplorationRoll:case RunPhase.CombatRoll:key="roll";break;
                    case RunPhase.ExplorationCards:
                        foreach(var card in state.cards)
                        {
                            var label=screen.Widgets.Buttons["fate-"+card.id].GetComponentInChildren<Text>().text;
                            Assert.That(label,Is.EqualTo(KoreanText.Node(card.type)+"  /  "+KoreanText.Grade(card.grade)),"Exploration must reveal type and grade only.");
                        }
                        key="fate-"+state.cards.OrderBy(x=>Preference(x.type)).First().id;break;
                    case RunPhase.CombatCards:
                        Assert.That(screen.Widgets.Situation.text,Does.Contain("다음 행동:"));
                        key="card-"+state.cards.OrderByDescending(x=>CombatRules.Evaluate(state,x).damage).First().id;break;
                    case RunPhase.Encounter:key="resolve";break;
                    case RunPhase.Shop:key="leave";break;
                    case RunPhase.Reward:key="claim";break;
                    case RunPhase.EquipmentChoice:key=string.IsNullOrEmpty(state.pendingEquipmentId)?"die-0":"equip-accept";break;
                    default:Assert.Fail("Unhandled phase "+state.phase);yield break;
                }
                Assert.That(FindButton(key),Is.Not.Null,"Required product input "+key);
                var before=state.combatTurns;var combat=state.phase==RunPhase.CombatCards;
                Press(key);Press(key);yield return WaitUnlocked();
                if(combat)Assert.That(Session().State.combatTurns,Is.EqualTo(before+1),"Duplicate action applied twice.");
            }
            Assert.That(Session().State.won,Is.True,Session().State.message);
            Assert.That(Session().State.eventsResolved,Is.EqualTo(Session().State.config.world.eventsToBoss));
            Assert.That(screen.Widgets.Situation.text,Is.EqualTo("여정 완료"));
            Press("restart");yield return WaitUnlocked();
            Press("new");yield return WaitUnlocked();
            Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Map));
            Assert.That(Session().State.eventsResolved,Is.Zero);
            Assert.That(screen.Store.Load().lastResult.won,Is.True,"Starting again retains the last completed result.");
            LogAssert.NoUnexpectedReceived();
        }
        private static int Preference(NodeType type)
        {
            switch(type){case NodeType.Rest:return 0;case NodeType.Treasure:return 1;case NodeType.Event:return 2;case NodeType.Shop:return 3;default:return 4;}
        }

        [UnityTest] public IEnumerator EarnedRerollUsesTheRealDieButtonAndRestoresInput()
        {
            Press("new");yield return WaitUnlocked();
            var state=Session().State;
            var eventNode=state.nodes.Single(x=>state.availableNodeIds.Contains(x.id)&&x.type==NodeType.Event);
            Press("node-"+eventNode.id);yield return WaitUnlocked();
            Press("roll");yield return WaitUnlocked();
            Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();
            Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Encounter));
            Press("resolve");yield return WaitUnlocked();Press("claim");yield return WaitUnlocked();
            Assert.That(Session().State.rerollUnlocked,Is.True);
            Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();
            Press("roll");yield return WaitUnlocked();
            var before=(int[])Session().State.dice.Clone();var charges=Session().State.rerollCharges;var progress=Session().State.eventsResolved;
            Assert.That(screen.Widgets.Buttons["die-2"].interactable,Is.True,"Earned reroll must be connected to the displayed dice.");
            Press("die-2");Press("die-2");yield return WaitUnlocked();
            Assert.That(Session().State.rerollCharges,Is.EqualTo(charges-Session().State.config.growth.rerollCost));
            for(var i=0;i<6;i++)if(i!=2)Assert.That(Session().State.dice[i],Is.EqualTo(before[i]));
            Assert.That(Session().State.eventsResolved,Is.EqualTo(progress));
            Assert.That(screen.Widgets.Notice.text,Does.Contain("다시 굴"));
            Assert.That(screen.Busy,Is.False);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator SavedRunContinuesExactCardsAndPaidRerollAfterSceneReload()
        {
            Press("new");yield return WaitUnlocked();
            var node=Session().State.nodes.Single(x=>Session().State.availableNodeIds.Contains(x.id)&&x.type==NodeType.Event);
            Press("node-"+node.id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
            var saved=screen.Store.Load();Assert.That(saved.phase,Is.EqualTo(RunPhase.ExplorationCards));
            yield return ReloadSavedScreen();Press("continue");yield return WaitUnlocked();
            Assert.That(Session().State.rngState,Is.EqualTo(saved.rngState));
            CollectionAssert.AreEqual(saved.cards.Select(x=>x.type+"/"+x.grade+"/"+x.contentId),Session().State.cards.Select(x=>x.type+"/"+x.grade+"/"+x.contentId));
            CollectionAssert.AreEqual(saved.cards.Select(x=>x.id),Session().State.cards.Select(x=>x.id));
            CollectionAssert.AreEqual(saved.dice,Session().State.dice);
            Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();
            Press("resolve");yield return WaitUnlocked();Press("claim");yield return WaitUnlocked();
            Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
            Press("die-3");yield return WaitUnlocked();saved=screen.Store.Load();
            yield return ReloadSavedScreen();Press("continue");yield return WaitUnlocked();
            Assert.That(Session().State.rerollCharges,Is.EqualTo(saved.rerollCharges));
            Assert.That(Session().State.rngState,Is.EqualTo(saved.rngState));
            CollectionAssert.AreEqual(saved.dice,Session().State.dice);
            CollectionAssert.AreEqual(saved.cards.Select(x=>x.id),Session().State.cards.Select(x=>x.id));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DamagedSaveIsExplainedAndPreservedBeforeExplicitArchive()
        {
            var path=Path.Combine(saveDirectory,"run.json");File.WriteAllText(path,"broken-save-evidence");
            LogAssert.Expect(LogType.Warning,Assert.Throws<InvalidDataException>(()=>screen.Store.Load()).Message);
            screen.UseStore(new LocalRunStore(path));yield return null;
            Assert.That(screen.Widgets.Buttons["new"].interactable,Is.False);
            Assert.That(screen.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("저장")&&x.text.Contains("읽")),Is.True);
            Press("new");yield return null;Assert.That(File.ReadAllText(path),Is.EqualTo("broken-save-evidence"));
            Press("archive");yield return WaitUnlocked();Assert.That(File.Exists(path),Is.False);
            var backup=Directory.GetFiles(saveDirectory,"*.bak").Single();Assert.That(File.ReadAllText(backup),Is.EqualTo("broken-save-evidence"));
            Press("new");yield return WaitUnlocked();Assert.That(screen.Store.Exists,Is.True);
            Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Map));LogAssert.NoUnexpectedReceived();
        }
        private IEnumerator ReloadSavedScreen()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/FateDicePrototype.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;yield return null;
            screen=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<FateDiceScreen>()).Single();
            screen.UseStore(new LocalRunStore(Path.Combine(saveDirectory,"run.json")));yield return null;
        }
        [UnityTearDown] public IEnumerator RemoveIsolatedTestSave()
        {
            if(screen)Object.Destroy(screen.gameObject);yield return null;
            if(!string.IsNullOrEmpty(saveDirectory)&&Directory.Exists(saveDirectory))Directory.Delete(saveDirectory,true);
        }

        [UnityTest] public IEnumerator FiveSeededEncountersApplyRealRewardsEquipmentAndGrowth()
        {
            foreach(var type in new[]{NodeType.Combat,NodeType.Event,NodeType.Treasure,NodeType.Shop,NodeType.Rest})
            {
                var data=screen.config.Snapshot();data.fate.nodeWeights=new float[5];data.fate.nodeWeights[(int)type]=1;
                if(type==NodeType.Treasure)foreach(var item in data.world.events.Where(x=>x.type==NodeType.Treasure))item.reward.dieId="ember";
                screen.Store.Save(RunSession.New(data,88,"fireball",Grade.Common).State);screen.UseStore(screen.Store);yield return null;
                Press("continue");yield return WaitUnlocked();
                Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
                Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();
                if(type==NodeType.Combat)
                {
                    for(var turn=0;turn<80&&Session().State.phase==RunPhase.CombatRoll;turn++)
                    {
                        Press("roll");yield return WaitUnlocked();
                        var state=Session().State;var best=state.cards.OrderByDescending(x=>CombatRules.Evaluate(state,x).damage).First();
                        Press("card-"+best.id);yield return WaitUnlocked();
                    }
                }
                else if(type==NodeType.Shop){Press("buy-reroll");yield return WaitUnlocked();}
                else
                {
                    if(type==NodeType.Rest)Assert.That(screen.Widgets.Buttons["train"].GetComponentInChildren<Text>().text,Does.Contain("16 경험치"));
                    Press(type==NodeType.Rest?"train":"resolve");yield return WaitUnlocked();
                }
                Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Reward));Press("claim");yield return WaitUnlocked();
                if(type==NodeType.Shop){Assert.That(Session().State.rerollCharges,Is.EqualTo(3));Assert.That(Session().State.gold,Is.Zero);Press("leave");yield return WaitUnlocked();}
                if(type==NodeType.Treasure)
                {
                    Press("equip-accept");yield return WaitUnlocked();Press("die-4");yield return WaitUnlocked();
                    Assert.That(Session().State.equipmentIds[0],Is.EqualTo("ember_blade"));
                    Assert.That(Session().State.dieIds[4],Is.EqualTo("ember"));
                }
                Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Map));Assert.That(Session().State.eventsResolved,Is.EqualTo(1));
                if(type==NodeType.Rest)Assert.That(Session().State.level,Is.EqualTo(2));
                if(type==NodeType.Event){Assert.That(Session().State.hp,Is.LessThan(90));Assert.That(Session().State.rerollUnlocked,Is.True);}
                AssertDisplayedStats();
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator SeededDefeatUsesVisibleActionsAndPersistsResult()
        {
            var data=screen.config.Snapshot();data.growth.startingMaxHp=1;data.fate.nodeWeights=new float[]{1,0,0,0,0};
            foreach(var enemy in data.combat.enemies)
            {
                enemy.maxHp=1000;foreach(var intent in enemy.intents)intent.weight=intent.kind==IntentKind.Attack?1:0;
            }
            screen.Store.Save(RunSession.New(data,33,"fireball",Grade.Common).State);screen.UseStore(screen.Store);yield return null;
            Press("continue");yield return WaitUnlocked();Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();
            Press("roll");yield return WaitUnlocked();Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();
            for(var i=0;i<30&&Session().State.phase!=RunPhase.Result;i++)
            {
                Press("roll");yield return WaitUnlocked();var state=Session().State;
                Press("card-"+state.cards.OrderByDescending(x=>CombatRules.Evaluate(state,x).damage).First().id);yield return WaitUnlocked();
            }
            Assert.That(Session().State.phase,Is.EqualTo(RunPhase.Result));Assert.That(Session().State.won,Is.False);
            Assert.That(Session().State.hp,Is.Zero);Assert.That(Session().State.eventsResolved,Is.Zero);
            Assert.That(screen.Store.Load().lastResult.won,Is.False);
            Assert.That(screen.Widgets.Situation.text,Is.EqualTo("여정 종료"));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TwoPortraitRatiosKeepSafeAreaAndPrimaryControlsReachable()
        {
            foreach(var height in new[]{1280,1600})
            {
#if UNITY_EDITOR
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720,(uint)height,"Fate Dice portrait acceptance");
#endif
                yield return null;yield return null;Canvas.ForceUpdateCanvases();
                if(Session()!=null)Press("menu");
                yield return WaitUnlocked();
                EnsureVisible(screen.Widgets.Buttons["new"]);Press("new");yield return WaitUnlocked();
                Assert.That(Screen.width,Is.EqualTo(720));Assert.That(Screen.height,Is.EqualTo(height));
                var safe=Screen.safeArea;var root=ScreenRect(screen.Widgets.SafeRoot);
                Assert.That(root.xMin,Is.EqualTo(safe.xMin).Within(2));Assert.That(root.yMin,Is.EqualTo(safe.yMin).Within(2));
                Assert.That(root.xMax,Is.EqualTo(safe.xMax).Within(2));Assert.That(root.yMax,Is.EqualTo(safe.yMax).Within(2));
                foreach(var id in Session().State.availableNodeIds)EnsureVisible(screen.Widgets.Buttons["node-"+id]);
                EnsureVisible(screen.Widgets.Buttons["menu"]);EnsureVisible(screen.Widgets.Buttons["cap-cycle"]);
                foreach(var label in new[]{screen.Widgets.Header,screen.Widgets.Stats,screen.Widgets.Situation,screen.Widgets.Fate})
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1),"Truncated primary text: "+label.text);
                Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
                foreach(var card in Session().State.cards)EnsureVisible(screen.Widgets.Buttons["fate-"+card.id]);
                AssertDisplayedStats();
            }
            LogAssert.NoUnexpectedReceived();
        }
        private void AssertDisplayedStats()
        {
            var state=Session().State;var stats=GrowthRules.Stats(state);
            Assert.That(screen.Widgets.Stats.text,Does.Contain("체력 "+state.hp+"/"+stats.maxHp));
            Assert.That(screen.Widgets.Stats.text,Does.Contain("위력 "+stats.power));
            Assert.That(screen.Widgets.Stats.text,Does.Contain("방어력 "+stats.guard));
        }
        private static Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var min=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            var max=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private static bool ContainsRect(Rect outer,Rect inner)=>inner.xMin>=outer.xMin-1&&inner.xMax<=outer.xMax+1&&inner.yMin>=outer.yMin-1&&inner.yMax<=outer.yMax+1;
        private Vector2 EnsureVisible(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
            if(rect.IsChildOf(screen.Widgets.Body))
            {
                var scroll=screen.Widgets.Body.GetComponentInParent<ScrollRect>();scroll.StopMovement();
                for(var i=0;i<=40&&!ContainsRect(ScreenRect(scroll.viewport),ScreenRect(rect));i++)
                {scroll.verticalNormalizedPosition=1-i/40f;Canvas.ForceUpdateCanvases();}
                Assert.That(ContainsRect(ScreenRect(scroll.viewport),ScreenRect(rect)),Is.True,"Clipped or unreachable button "+button.name);
            }
            Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(48),"Small touch height "+button.name);
            Assert.That(ContainsRect(ScreenRect(screen.Widgets.SafeRoot),ScreenRect(rect)),Is.True,"Button outside safe area "+button.name);
            return ScreenRect(rect).center;
        }


        [UnityTest] public IEnumerator ActualScreensUseReusableNodeFateActionAndCommonViews()
        {
            Assert.That(screen.Widgets.Buttons["new"].GetComponent<CommonButtonView>(),Is.Not.Null,"The menu must instantiate the common button original.");
            Press("new");yield return WaitUnlocked();
            var first=Session().State.availableNodeIds[0];
            foreach(var id in Session().State.availableNodeIds)
                Assert.That(screen.Widgets.Buttons["node-"+id].GetComponent<ExplorationNodeView>(),Is.Not.Null);
            Press("node-"+first);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
            foreach(var card in Session().State.cards)
                Assert.That(screen.Widgets.Buttons["fate-"+card.id].GetComponent<FateCardView>(),Is.Not.Null);
            var guaranteed=Session().State.cards[0];Assert.That(guaranteed.type,Is.EqualTo(NodeType.Combat));
            Press("fate-"+guaranteed.id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
            foreach(var card in Session().State.cards)
            {
                var view=screen.Widgets.Buttons["card-"+card.id].GetComponent<ActionCardView>();
                Assert.That(view,Is.Not.Null);Assert.That(view.OfferedId,Is.EqualTo(card.id));Assert.That(view.OriginalId,Is.EqualTo(card.contentId));
                Assert.That(view.gradeLabel.text,Does.Contain(KoreanText.Grade(card.grade)));
                var effect=CombatRules.Evaluate(Session().State,card);
                Assert.That(view.effectLabel.text,Does.Contain("피해 "+effect.damage));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActualSceneOwnsPersistentVisualCatalogAndFourPrefabReferences()
        {
            var visualField=typeof(FateDiceScreen).GetField("visuals");var prefabField=typeof(FateDiceScreen).GetField("uiPrefabs");
            Assert.That(visualField,Is.Not.Null,"The screen needs an Inspector-authored visual catalog.");
            Assert.That(prefabField,Is.Not.Null,"The screen needs explicit prefab references.");
            var catalog=(FateDiceVisualCatalog)visualField.GetValue(screen);Assert.That(catalog,Is.Not.Null);
            var refs=prefabField.GetValue(screen);Assert.That(refs,Is.Not.Null);
#if UNITY_EDITOR
            Assert.That(UnityEditor.EditorUtility.IsPersistent(catalog),Is.True);
            foreach(var name in new[]{"commonButton","explorationNode","actionCard","fateCard"})
            {
                var field=refs.GetType().GetField(name);Assert.That(field,Is.Not.Null);
                var asset=(UnityEngine.Object)field.GetValue(refs);Assert.That(asset,Is.Not.Null,name);
                Assert.That(UnityEditor.EditorUtility.IsPersistent(asset),Is.True,name+" must be an authored asset.");
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(asset),Does.EndWith(".prefab"));
            }
#endif
            yield return null;LogAssert.NoUnexpectedReceived();
        }


#if UNITY_EDITOR
        [UnityTest] public IEnumerator PersistentImageMappingChangesAppearanceButNotThePlayedCommands()
        {
            const string probePath="Assets/_Project/Shared/UI/Art/UiMappingProbe.asset";
            var pictures=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(probePath).OfType<Sprite>().ToArray();
            Assert.That(pictures.Length,Is.EqualTo(2));
            var wide=pictures.Single(x=>x.rect.width>x.rect.height);var tall=pictures.Single(x=>x.rect.height>x.rect.width);
            var catalog=screen.visuals;var original=UnityEditor.EditorJsonUtility.ToJson(catalog);
            try
            {
                Press("new");yield return WaitUnlocked();
                var combat=Session().State.nodes.First(x=>Session().State.availableNodeIds.Contains(x.id)&&x.type==NodeType.Combat);
                var before=StateWithoutTime(Session().State);
                Assert.That(screen.Widgets.Buttons["node-"+combat.id].GetComponent<ExplorationNodeView>().frame.icon.sprite,Is.Null);
                foreach(var row in catalog.actions){row.visual.artwork=wide;row.visual.tint=Color.white;}
                foreach(var row in catalog.events){row.visual.artwork=tall;row.visual.tint=Color.white;}
                catalog.nodes.Single(x=>x.type==NodeType.Combat).visual.icon=wide;
                UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);
                Press("menu");yield return WaitUnlocked();Press("continue");yield return WaitUnlocked();
                Assert.That(StateWithoutTime(Session().State),Is.EqualTo(before),"Artwork must not alter progress or RNG.");
                var nodeView=screen.Widgets.Buttons["node-"+combat.id].GetComponent<ExplorationNodeView>();
                Assert.That(nodeView.frame.icon.sprite,Is.EqualTo(wide));Assert.That(nodeView.frame.icon.preserveAspect,Is.True);
                var oracle=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(Session().State)));
                Assert.That(oracle.ChooseNode(combat.id)&&oracle.Roll(),Is.True);
                Press("node-"+combat.id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
                Assert.That(StateWithoutTime(Session().State),Is.EqualTo(StateWithoutTime(oracle.State)));
                foreach(var card in Session().State.cards)
                {
                    var view=screen.Widgets.Buttons["fate-"+card.id].GetComponent<FateCardView>();
                    Assert.That(view.artwork.sprite,Is.Null,"Secret event art leaked before selection.");
                    Assert.That(view.artworkFallback.gameObject.activeSelf,Is.True);
                    Assert.That(view.frame.label.text,Is.EqualTo(KoreanText.Node(card.type)+"  /  "+KoreanText.Grade(card.grade)));
                    Assert.That(view.GetComponentsInChildren<Image>(true).Any(x=>x.sprite==tall),Is.False);
                    var secret=Session().State.config.Event(card.contentId).label;
                    Assert.That(view.GetComponentsInChildren<Text>(true).Any(x=>x.text.Contains(secret)||x.text.Contains(KoreanText.Content(secret))),Is.False);
                }
                var fate=Session().State.cards[0].id;
                Assert.That(oracle.ChooseFate(fate)&&oracle.Roll(),Is.True);
                Press("fate-"+fate);yield return WaitUnlocked();
                Assert.That(((CombatUI)screen.UI.ActiveScreen).artwork.sprite,Is.EqualTo(tall));
                Press("roll");yield return WaitUnlocked();
                Assert.That(StateWithoutTime(Session().State),Is.EqualTo(StateWithoutTime(oracle.State)));
                foreach(var card in Session().State.cards)
                {
                    var view=screen.Widgets.Buttons["card-"+card.id].GetComponent<ActionCardView>();
                    Assert.That(view.artwork.sprite,Is.EqualTo(wide));Assert.That(view.artwork.preserveAspect,Is.True);
                    Assert.That(view.OriginalId,Is.EqualTo(card.contentId));Assert.That(view.OfferedId,Is.EqualTo(card.id));
                    var effect=CombatRules.Evaluate(oracle.State,card);
                    Assert.That(view.effectLabel.text,Is.EqualTo("피해 "+effect.damage+" / 수호 "+effect.block));
                    Canvas.ForceUpdateCanvases();
                    Assert.That(ScreenRect(view.artwork.rectTransform).yMin,Is.GreaterThan(ScreenRect(view.effectLabel.rectTransform).yMax),"Portrait card art belongs above its effect text.");
                }
                var selected=Session().State.cards[1].id;Assert.That(oracle.ChooseAction(selected),Is.True);
                Press("card-"+selected);yield return WaitUnlocked();
                Assert.That(StateWithoutTime(Session().State),Is.EqualTo(StateWithoutTime(oracle.State)));
            }
            finally
            {
                UnityEditor.EditorJsonUtility.FromJsonOverwrite(original,catalog);
                UnityEditor.EditorUtility.SetDirty(catalog);UnityEditor.AssetDatabase.SaveAssetIfDirty(catalog);
                Assert.That(UnityEditor.EditorJsonUtility.ToJson(catalog),Is.EqualTo(original));
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CommonPrefabStyleEditPropagatesToMenuNodesFatesAndActions()
        {
            const string path="Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab";
            var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
            var original=root.GetComponent<CommonButtonView>().label.fontStyle;
            UnityEditor.PrefabUtility.UnloadPrefabContents(root);
            try
            {
                root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
                root.GetComponent<CommonButtonView>().label.fontStyle=FontStyle.Bold;
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);UnityEditor.PrefabUtility.UnloadPrefabContents(root);root=null;
                screen.UseStore(screen.Store);yield return null;
                Assert.That(screen.Widgets.Buttons["new"].GetComponent<CommonButtonView>().label.fontStyle,Is.EqualTo(FontStyle.Bold));
                Press("new");yield return WaitUnlocked();
                var id=Session().State.availableNodeIds[0];
                Assert.That(screen.Widgets.Buttons["node-"+id].GetComponent<CommonButtonView>().label.fontStyle,Is.EqualTo(FontStyle.Bold));
                Press("node-"+id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
                foreach(var card in Session().State.cards)
                    Assert.That(screen.Widgets.Buttons["fate-"+card.id].GetComponent<CommonButtonView>().label.fontStyle,Is.EqualTo(FontStyle.Bold));
                Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
                foreach(var card in Session().State.cards)
                    Assert.That(screen.Widgets.Buttons["card-"+card.id].GetComponent<CommonButtonView>().label.fontStyle,Is.EqualTo(FontStyle.Bold));
            }
            finally
            {
                if(root)UnityEditor.PrefabUtility.UnloadPrefabContents(root);
                root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
                root.GetComponent<CommonButtonView>().label.fontStyle=original;
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);UnityEditor.PrefabUtility.UnloadPrefabContents(root);
                Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<CommonButtonView>(path).label.fontStyle,Is.EqualTo(original));
            }
            LogAssert.NoUnexpectedReceived();
        }
#endif
        [UnityTest] public IEnumerator ActualActionOriginalSupportsDifferentSkillsAndDuplicateOfferedIds()
        {
            Press("new");yield return WaitUnlocked();Press("node-"+Session().State.availableNodeIds[0]);yield return WaitUnlocked();
            Press("roll");yield return WaitUnlocked();Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();Press("roll");yield return WaitUnlocked();
            var cards=Session().State.cards;
            cards[0].contentId="strike";cards[0].grade=Grade.Common;
            cards[1].contentId="strike";cards[1].grade=Grade.Epic;
            cards[2].contentId="guard";cards[2].grade=Grade.Rare;
            Press("menu");yield return WaitUnlocked();Press("continue");yield return WaitUnlocked();
            cards=Session().State.cards;
            var views=cards.Select(card=>screen.Widgets.Buttons["card-"+card.id].GetComponent<ActionCardView>()).ToArray();
            Assert.That(views.Select(x=>x.OriginalId),Is.EqualTo(new[]{"strike","strike","guard"}));
            Assert.That(views.Select(x=>x.OfferedId).Distinct().Count(),Is.EqualTo(3));
            Assert.That(views[0].effectLabel.text,Is.Not.EqualTo(views[1].effectLabel.text));
            foreach(var view in views){Assert.That(view.artwork.sprite,Is.Null);Assert.That(view.artworkFallback.gameObject.activeSelf,Is.True);}
            var oracle=new RunSession(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(Session().State)));
            oracle.ChooseAction(cards[1].id);Press("card-"+cards[1].id);yield return WaitUnlocked();
            Assert.That(StateWithoutTime(Session().State),Is.EqualTo(StateWithoutTime(oracle.State)));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ActualMergedPathFadesOnlyUnreachableNodesAndRestoresSavedHistory()
        {
            var data=screen.config.Snapshot();data.world.branchCount=2;data.world.previewDepth=1;data.fate.nodeWeights=new float[]{0,0,0,0,1};
            var run=RunSession.New(data,33,"fireball",Grade.Common);
            run.State.nodes=new System.Collections.Generic.List<NodeState>{
                new NodeState{id="A",type=NodeType.Rest,childIds=new System.Collections.Generic.List<string>{"C","D"}},
                new NodeState{id="B",type=NodeType.Combat,childIds=new System.Collections.Generic.List<string>{"C","E"}},
                new NodeState{id="C",type=NodeType.Shop},new NodeState{id="D",type=NodeType.Treasure},new NodeState{id="E",type=NodeType.Event}};
            run.State.availableNodeIds=new System.Collections.Generic.List<string>{"A","B"};
            screen.Store.Save(run.State);screen.UseStore(screen.Store);yield return null;yield return null;
            Press("continue");yield return WaitUnlocked();
            var views=screen.Widgets.Body.GetComponentsInChildren<ExplorationNodeView>();
            var a=views.Single(x=>x.NodeId=="A");var b=views.Single(x=>x.NodeId=="B");var e=views.Single(x=>x.NodeId=="E");
            var shared=views.Where(x=>x.NodeId=="C").ToArray();Assert.That(shared.Length,Is.EqualTo(1));
            Assert.That(views.Select(x=>x.NodeId).Distinct().Count(),Is.EqualTo(views.Length));
            Assert.That(a.Type,Is.Not.EqualTo(b.Type));Assert.That(a.frame.button.IsInteractable()&&b.frame.button.IsInteractable(),Is.True);
            var rng=Session().State.rngState;var next=Session().State.nextNodeId;
            Press("node-A");
            Assert.That(b.frame.button.IsInteractable()||e.frame.button.IsInteractable(),Is.False);
            Assert.That(shared.All(x=>x.gameObject.activeSelf&&x.frame.group.alpha>0&&!x.IsFading&&!x.frame.button.IsInteractable()),Is.True);
            yield return WaitUnlocked();yield return new WaitForSecondsRealtime(screen.visuals.nodeFadeSeconds+.1f);
            Assert.That(!b.gameObject.activeSelf&&!e.gameObject.activeSelf,Is.True);
            Assert.That(b.NodeId,Is.EqualTo("B"));Assert.That(shared.All(x=>x.gameObject.activeSelf),Is.True);
            Assert.That(Session().State.rngState,Is.EqualTo(rng));Assert.That(Session().State.nextNodeId,Is.EqualTo(next));
            CollectionAssert.AreEquivalent(new[]{"B","E"},screen.Store.Load().nodeHistory.Select(x=>x.id));
            Press("menu");yield return WaitUnlocked();Press("continue");yield return WaitUnlocked();
            Assert.That(screen.Widgets.Body.GetComponentsInChildren<ExplorationNodeView>().Any(x=>x.NodeId=="C"),Is.True);
            Press("roll");yield return WaitUnlocked();Press("fate-"+Session().State.cards[0].id);yield return WaitUnlocked();
            Press("resolve");yield return WaitUnlocked();Press("claim");yield return WaitUnlocked();
            CollectionAssert.AreEquivalent(new[]{"C","D"},Session().State.availableNodeIds);
            var stored=screen.Store.Load();CollectionAssert.AreEquivalent(new[]{"A","B","E"},stored.nodeHistory.Select(x=>x.id));
            CollectionAssert.AreEqual(new[]{"C","E"},stored.nodeHistory.Single(x=>x.id=="B").childIds);
            Press("menu");yield return WaitUnlocked();Press("continue");yield return WaitUnlocked();
            Assert.That(screen.Widgets.Buttons["node-C"].IsInteractable()&&screen.Widgets.Buttons["node-D"].IsInteractable(),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        private static string StateWithoutTime(RunState state)
        {
            var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));copy.playedSeconds=0;
            return JsonUtility.ToJson(copy);
        }

        [UnityTest] public IEnumerator LobbyHidesSeedAndUsesTheConfiguredLaunchSeed()
        {
            var menu=(MenuUI)screen.UI.ActiveScreen;
            Assert.That(menu.seedInput.gameObject.activeInHierarchy,Is.False);
            // Workbench configures the existing launch property; it is no longer a normal lobby input.
            screen.Seed=41;
            Press("new");yield return WaitUnlocked();
            Assert.That(Session().State.initialSeed,Is.EqualTo(41u));
            LogAssert.NoUnexpectedReceived();
        }
        private RunSession Session()
        {
            var property=typeof(FateDiceScreen).GetProperty("Session");
            Assert.That(property,Is.Not.Null,"Screen must expose its owned RunSession for objective state inspection.");
            return (RunSession)property.GetValue(screen);
        }
        private Button FindButton(string key)
        {
            if(key=="roll"&&screen.UI.Popups.LastOrDefault() is DiceRollUI popup)return popup.rollButton.button;
            return screen.Widgets.Buttons.TryGetValue(key,out var button)?button:null;
        }
        private void Press(string key)
        {
            if(screen.Busy)return;
            // Navigation tests dismiss the roll modal first; the new modal test checks underneath input blocking.
            if(key=="menu"&&screen.UI.Popups.LastOrDefault() is DiceRollUI)screen.UI.CloseTopPopup();
            var button=FindButton(key);
            if(!button||!button.IsInteractable())return;
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=EnsureVisible(button)};
            var hits=new System.Collections.Generic.List<RaycastResult>();
            screen.GetComponentInChildren<GraphicRaycaster>().Raycast(data,hits);
            var target=hits.Select(x=>ExecuteEvents.GetEventHandler<IPointerClickHandler>(x.gameObject)).FirstOrDefault(x=>x!=null);
            Assert.That(target,Is.EqualTo(button.gameObject),"Visible pointer raycast did not reach "+key);
            ExecuteEvents.Execute(target,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target,data,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target,data,ExecuteEvents.pointerClickHandler);
        }

        private IEnumerator WaitUnlocked()
        {
            yield return null;
            var end=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<end)
            {
                var group=screen.Widgets.SafeRoot.GetComponent<CanvasGroup>();
                if(group==null||group.interactable){yield return null;yield break;}
                yield return null;
            }
            Assert.Fail("Product input lock did not recover.");
        }
    }
}
