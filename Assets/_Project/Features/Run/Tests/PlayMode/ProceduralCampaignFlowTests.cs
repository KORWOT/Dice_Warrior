using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class ProceduralCampaignFlowTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        GameApplication app;
        CountingStore store;
        string directory, userSave;
        byte[] userBytes;
        float oldTimeScale;
        RunUIController Controller => app.controller;
        FateChoiceUI Popup => Controller.UI.Popups.LastOrDefault() as FateChoiceUI;

        sealed class CountingStore : IRunStore
        {
            public readonly LocalRunStore inner;
            public int saves;
            public bool fail;
            public CountingStore(string path) { inner = new LocalRunStore(path); }
            public bool Exists => inner.Exists;
            public RunState Load() => inner.Load();
            public void Save(RunState state)
            {
                if (fail) throw new IOException("Injected save failure");
                inner.Save(state); saves++;
            }
            public string Archive() => inner.Archive();
        }

        [SetUp] public void SetUp()
        {
            oldTimeScale = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "FateDiceProceduralFlow", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new CountingStore(Path.Combine(directory, "run.json"));
            userSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            userBytes = File.Exists(userSave) ? File.ReadAllBytes(userSave) : null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = oldTimeScale;
            if (app) Object.Destroy(app.gameObject);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            Assert.That(File.Exists(userSave), Is.EqualTo(userBytes != null));
            if (userBytes != null) CollectionAssert.AreEqual(userBytes, File.ReadAllBytes(userSave));
            var prefix = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceProceduralFlow")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) Directory.Delete(directory, true);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AuthoredMapSupportsImmediateTravelAndPublicFloorSnapshots()
        {
            var type = typeof(ExplorationUI).Assembly.GetType("FateDice.CampaignNodeUIData");
            Assert.That(type, Is.Not.Null, "The map needs a public snapshot that masks distant node types.");
            Assert.That(type.GetField("type").FieldType, Is.EqualTo(typeof(NodeType?)));
            Assert.That(typeof(CampaignMapView).GetMethod("BindCampaign"), Is.Not.Null);
            var view = UnityEditor.AssetDatabase.LoadAssetAtPath<ExplorationUI>(
                "Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab");
            Assert.That(view, Is.Not.Null);
            Assert.That(!view.moveButton || !view.moveButton.gameObject.activeSelf, Is.True,
                "The authored map must not offer a second travel button after a node tap.");
        }

        [Test] public void ProjectionMasksDistantTypesAndNeverCarriesASecondPrivateGraph()
        {
            var run = NewRun();
            var before = Stable(run.State);
            var data = new ExplorationUIData { nodes = run.State.nodes.ToArray(), completedNodes = run.State.nodeHistory.ToArray() };
            CampaignMapProjection.Apply(data, run.State);
            Assert.That(data.nodes, Is.Empty);
            Assert.That(data.completedNodes, Is.Empty);
            Assert.That(data.campaignNodes.Length, Is.EqualTo(run.State.nodes.Count));
            Assert.That(data.campaignNodes.Where(n => n.floor <= 2).All(n => n.revealed && n.type.HasValue), Is.True);
            Assert.That(data.campaignNodes.Where(n => n.floor > 2 && n.floor <= 10).All(n => !n.revealed && n.type == null), Is.True);
            Assert.That(data.campaignNodes.Single(n => n.floor == 11).type, Is.EqualTo(NodeType.Boss));
            Assert.That(Stable(run.State), Is.EqualTo(before));
            var first = data.campaignNodes.ToDictionary(n => n.id, n => (n.floor, n.lane, string.Join(",", n.childIds)));
            run.ChooseNode(run.State.availableNodeIds[0]);
            CampaignMapProjection.Apply(data, run.State);
            foreach (var node in data.campaignNodes)
                Assert.That((node.floor, node.lane, string.Join(",", node.childIds)), Is.EqualTo(first[node.id]));
            Assert.That(data.campaignNodes.Any(n => n.unreachable && !n.completed), Is.True);
            Assert.That(data.campaignNodes.Where(n => n.floor == 3).All(n => n.type.HasValue), Is.True);
            Assert.That(data.campaignNodes.Where(n => n.floor == 4).All(n => n.type == null), Is.True);
        }

        [UnityTest] public IEnumerator MapTravelRollPopupConfirmAndRewardWorkAtBothPortraitRatios()
        {
            foreach (int height in new[] { 1280, 1600 })
            {
                var run = NewRun(); store.Save(run.State);
                yield return Open(height);
                var view = (ExplorationUI)Controller.UI.ActiveScreen;
                var positions = view.campaignMap.Nodes.ToDictionary(pair => pair.Key, pair => ((RectTransform)pair.Value.transform).anchoredPosition);
                string nodeId = run.State.availableNodeIds[0];
                int saves = store.saves;
                var nodeButton = Controller.Widgets.Buttons["node-" + nodeId];
                Assert.That(run.ChooseNode(nodeId), Is.True);
                Click(nodeButton);
                Assert.That(Controller.Busy, Is.True, "The first node tap must commit and begin arrival.");
                Assert.That(store.saves, Is.EqualTo(saves + 1));
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(run.State)));
                Assert.That(Stable(store.Load()), Is.EqualTo(Stable(run.State)));
                Assert.That(Controller.UI.Popups, Is.Empty, "Arrival must finish before opening the dice.");
                nodeButton.onClick.Invoke();
                Assert.That(store.saves, Is.EqualTo(saves + 1), "A repeated callback must not travel twice.");
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(run.State)));
                foreach (var pair in view.campaignMap.Nodes)
                    Assert.That(((RectTransform)pair.Value.transform).anchoredPosition, Is.EqualTo(positions[pair.Key]));
                yield return Unlocked();
                var dice = (DiceRollUI)Controller.UI.Popups.Last();
                Assert.That(run.Roll(), Is.True);
                Click(dice.rollButton.button); yield return Unlocked();
                Assert.That(Popup, Is.Not.Null);
                Assert.That(view.mapContainer.gameObject.activeInHierarchy, Is.True);
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(run.State)));
                var mapMenu = Controller.Widgets.Buttons["menu"];
                Assert.That(HitAt(mapMenu), Is.Not.SameAs(mapMenu.gameObject), "Modal background must block map input.");
                var choice = Popup.Cards[0];
                saves = store.saves;
                Click(choice.frame.button);
                Assert.That(Popup.SelectedId, Is.EqualTo(choice.OfferedId));
                Assert.That(store.saves, Is.EqualTo(saves), "Selecting a card must not commit it.");
                Assert.That(Controller.Session.Phase, Is.EqualTo(RunPhase.ExplorationCards));
                Assert.That(run.ChooseFate(choice.OfferedId), Is.True);
                var confirm = Popup.confirmButton.button;
                Click(confirm); confirm.onClick.Invoke();
                Assert.That(store.saves, Is.EqualTo(saves + 1), "Double confirmation must save once.");
                yield return Unlocked();
                Assert.That(Controller.UI.Popups, Is.Empty);
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(run.State)));
                Assert.That(run.ResolveEncounter(false), Is.True);
                Click(Controller.Widgets.Buttons["resolve"]); yield return Unlocked();
                Assert.That(run.ClaimReward(), Is.True);
                Click(Controller.Widgets.Buttons["claim"]); yield return Unlocked();
                Assert.That(Controller.Session.Phase, Is.EqualTo(RunPhase.Map));
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(run.State)));
                view = (ExplorationUI)Controller.UI.ActiveScreen;
                foreach (var pair in view.campaignMap.Nodes)
                    Assert.That(((RectTransform)pair.Value.transform).anchoredPosition, Is.EqualTo(positions[pair.Key]), "Travel changed saved map positions.");
                Object.Destroy(app.gameObject); yield return null; app = null;
            }
        }

        [UnityTest] public IEnumerator ClosingAndReopeningFateKeepsOffersAndRerollClearsTheSelection()
        {
            var run = NewRun(); run.ChooseNode(run.State.availableNodeIds[0]); run.Roll();
            var state = run.State; state.rerollUnlocked = true; state.rerollCharges = 3;
            store.Save(state); yield return Open(1600);
            var snapshot = Stable(Controller.Session.State);
            int saves = store.saves;
            Click(Popup.Cards[1].frame.button);
            Click(Popup.closeButton.button);
            Assert.That(Popup, Is.Null);
            Click(Controller.Widgets.Buttons["open-fate"]);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            Assert.That(Popup.SelectedId, Is.Null.Or.Empty);
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(snapshot));
            Assert.That(store.saves, Is.EqualTo(saves));
            Click(Popup.Cards[1].frame.button);
            // Invoke the real displayed die's pointer click, not the session method.
            var dieButton = Popup.rerollButtons[0].button;
            var oracle = new RunSession(Controller.Session.State); oracle.Reroll(0);
            Click(dieButton); yield return Unlocked();
            Assert.That(Popup.SelectedId, Is.Null.Or.Empty);
            Assert.That(store.saves, Is.EqualTo(saves + 1));
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(oracle.State)));
            Assert.That(Popup.Cards.Select(c => c.OfferedId), Is.EqualTo(oracle.State.cards.Select(c => c.id)));
        }

        [UnityTest] public IEnumerator FateSelectionWaitsWithoutTimeoutAndForcedExitRecoversOnce()
        {
            PrepareCards(); yield return Open(1280);
            Time.timeScale = 0;
            var popup = Popup;
            popup.exitSeconds = 60;
            int starts = 0, ends = 0;
            Controller.Playback.Started += _ => starts++;
            Controller.Playback.Ended += _ => ends++;
            // Selecting is idle; no playback or rule command begins before confirmation.
            Click(popup.Cards[0].frame.button);
            double idleStarted = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - idleStarted < 10.1) yield return null;
            Assert.That(popup.IsOpen && popup.SelectedId != null, Is.True, "Thinking time must not be a presentation timeout.");
            Assert.That(Controller.Playback.IsPlaying, Is.False);
            Assert.That(starts, Is.Zero);
            var oracle = new RunSession(Controller.Session.State); oracle.ChooseFate(popup.SelectedId);
            int saves = store.saves;
            LogAssert.Expect(LogType.Error, new Regex("Run presentation timeout:.*ExplorationCards.*limit=10\\.0s"));
            Click(popup.confirmButton.button);
            double began = Controller.Playback.StartedAt;
            while (Time.realtimeSinceStartupAsDouble - began < 9.4) yield return null;
            Assert.That(Controller.Busy, Is.True);
            yield return Unlocked(2);
            Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(Controller.Playback.EndedAt - began, Is.InRange(10d, 10.8d));
            Assert.That(starts, Is.EqualTo(1)); Assert.That(ends, Is.EqualTo(1));
            Assert.That(store.saves, Is.EqualTo(saves + 1));
            Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(oracle.State)));
            Assert.That(Controller.Widgets.Buttons["resolve"].IsInteractable(), Is.True);
        }

        [UnityTest] public IEnumerator FailedConfirmationRetainsTheCommittedOffersAndCanRetry()
        {
            PrepareCards(); yield return Open(1280);
            var before = Stable(Controller.Session.State);
            string cardId = Popup.Cards[0].OfferedId;
            store.fail = true;
            Click(Popup.Cards[0].frame.button); Click(Popup.confirmButton.button); yield return Unlocked();
            Assert.That(Controller.Session.Phase, Is.EqualTo(RunPhase.ExplorationCards));
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(before));
            Assert.That(Popup.Cards.Select(c => c.OfferedId), Does.Contain(cardId));
            store.fail = false;
            Click(Popup.Cards.Single(c => c.OfferedId == cardId).frame.button);
            Click(Popup.confirmButton.button); yield return Unlocked();
            Assert.That(Controller.Session.Phase, Is.EqualTo(RunPhase.Encounter));
        }

        [UnityTest] public IEnumerator CancellingAndDisablingFateExitPreserveTheSingleCommittedEvent()
        {
            foreach (bool disable in new[] { false, true })
            {
                PrepareCards(); yield return Open(1280);
                var popup = Popup; popup.exitSeconds = 60;
                Click(popup.Cards[0].frame.button);
                int saves = store.saves;
                Click(popup.confirmButton.button); yield return null;
                var committed = Stable(Controller.Session.State);
                if (disable) Controller.enabled = false; else Controller.Playback.Cancel();
                yield return null;
                Assert.That(Controller.Busy, Is.False);
                Assert.That(Controller.UI.Popups, Is.Empty);
                Assert.That(store.saves, Is.EqualTo(saves + 1));
                Assert.That(Stable(Controller.Session.State), Is.EqualTo(committed));
                if (disable) { Controller.enabled = true; yield return null; }
                Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<EncounterUI>());
                Object.Destroy(app.gameObject); yield return null; app = null;
            }
        }

        [UnityTest] public IEnumerator OldFateExitCannotCloseAReboundPopup()
        {
            PrepareCards(); yield return Open(1280);
            var popup = Popup; popup.exitSeconds = 60;
            var offers = Controller.Session.State.cards;
            Click(popup.Cards[0].frame.button); Click(popup.confirmButton.button); yield return null;
            var committed = Stable(Controller.Session.State);
            var context = new RunUIContext { manager = Controller.UI, prefabs = Controller.uiPrefabs,
                visuals = Controller.visuals, presentation = Controller.Session.State.config.presentation };
            Controller.UI.ShowPopup<FateChoiceUI>(new FateChoiceUIData { context = context, hud = new RunHUDData(),
                offers = offers.Select(c => new FateOfferUIData { id = c.id, type = c.type, grade = c.grade }).ToArray() });
            int binding = popup.BindingVersion;
            yield return Unlocked();
            Assert.That(popup.IsOpen && popup.BindingVersion == binding, Is.True);
            Assert.That(Popup, Is.SameAs(popup));
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(committed));
        }

        [Test] public void DiskResumeKeepsCurrentLocationAndTwoFloorReveal()
        {
            var run = NewRun();
            for (int i = 0; i < 3; i++)
            {
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
                Assert.That(run.Roll(), Is.True);
                Assert.That(run.ChooseFate(run.State.cards.First(card => card.type == NodeType.Rest).id), Is.True);
                Assert.That(run.ResolveEncounter(false), Is.True);
                Assert.That(run.ClaimReward(), Is.True);
            }
            var before = new ExplorationUIData(); CampaignMapProjection.Apply(before, run.State);
            store.Save(run.State);
            var after = new ExplorationUIData(); CampaignMapProjection.Apply(after, store.Load());
            Assert.That(after.currentNodeId, Is.EqualTo(run.State.resolvedEventIds.Last()));
            Assert.That(after.campaignNodes.Select(node => (node.id, node.type, node.revealed)),
                Is.EqualTo(before.campaignNodes.Select(node => (node.id, node.type, node.revealed))));
            Assert.That(after.campaignNodes.Where(node => node.floor <= 5).All(node => node.type.HasValue), Is.True);
            Assert.That(after.campaignNodes.Where(node => node.floor == 6).All(node => !node.type.HasValue), Is.True);
        }

        [UnityTest] public IEnumerator NewFateBindingSurvivesCancelDisableAndRerollCompletion()
        {
            foreach (string ending in new[] { "cancel", "disable", "reroll" })
            {
                PrepareCards();
                var state = store.Load(); state.rerollUnlocked = true; state.rerollCharges = 3;
                state.config.presentation.explorationDice.rollSeconds = .4f; store.Save(state);
                yield return Open(1280);
                var popup = Popup; popup.exitSeconds = 60;
                var context = new RunUIContext { manager = Controller.UI, prefabs = Controller.uiPrefabs,
                    visuals = Controller.visuals, presentation = Controller.Session.State.config.presentation };
                var replacement = new FateChoiceUIData { context = context, hud = new RunHUDData { fate = "새 팝업 소유자" },
                    offers = state.cards.Select(c => new FateOfferUIData { id = c.id, type = c.type, grade = c.grade }).ToArray() };
                if (ending == "reroll") Click(popup.rerollButtons[0].button);
                else { Click(popup.Cards[0].frame.button); Click(popup.confirmButton.button); }
                yield return null;
                // A different presenter reuses the pooled instance while the old command is finishing.
                Controller.UI.ShowPopup<FateChoiceUI>(replacement);
                int binding = popup.BindingVersion;
                if (ending == "cancel") Controller.Playback.Cancel();
                if (ending == "disable") Controller.enabled = false;
                yield return Unlocked();
                Assert.That(popup.IsOpen, Is.True, ending);
                Assert.That(popup.BindingVersion, Is.EqualTo(binding), ending);
                Assert.That(popup.status.text, Is.EqualTo("새 팝업 소유자"), ending);
                Object.Destroy(app.gameObject); yield return null; app = null;
            }
        }

        static T Asset<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        static RunSession NewRun()
        {
            var config = Asset<GameApplication>(AppPath).controller.config.Snapshot();
            config.world.mapGenerationVersion = 1; config.world.mapColumns = 5; config.world.mapPathCount = 5;
            config.world.eventsToBoss = 10; config.world.previewDepth = 2;
            config.fate.nodeWeights = new float[] { 0, 0, 0, 0, 1 };
            config.presentation.explorationDice.rollSeconds = 0; config.presentation.explorationDice.resultHoldSeconds = 0;
            return RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Common);
        }
        void PrepareCards()
        {
            var run = NewRun(); run.ChooseNode(run.State.availableNodeIds[0]); run.Roll(); store.Save(run.State);
        }
        IEnumerator Open(int height)
        {
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice procedural campaign");
            yield return null;
            app = GameApplication.Bootstrap(Asset<GameApplication>(AppPath), store, new FixedSeedSource(33));
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/InGame.unity", LoadSceneMode.Single);
            yield return Unlocked(); yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        }
        IEnumerator Unlocked(float seconds = 12)
        {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Controller.Busy) { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(until)); yield return null; }
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        }
        void Click(Button button)
        {
            Assert.That(button && button.IsInteractable() && button.isActiveAndEnabled, Is.True, button ? button.name : "missing button");
            foreach (var scroll in button.GetComponentsInParent<ScrollRect>())
            {
                scroll.StopMovement();
                for (int i = 0; i <= 40 && !Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)button.transform)); i++)
                {
                    if (scroll.vertical) scroll.verticalNormalizedPosition = 1 - i / 40f;
                    if (scroll.horizontal) scroll.horizontalNormalizedPosition = i / 40f;
                    Canvas.ForceUpdateCanvases();
                }
            }
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = ScreenRect((RectTransform)button.transform).center };
            var hit = Hit(pointer);
            Assert.That(hit, Is.SameAs(button.gameObject), "Actual pointer did not reach " + button.name);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }
        GameObject HitAt(Button button) => Hit(new PointerEventData(EventSystem.current)
        { position = ScreenRect((RectTransform)button.transform).center });
        GameObject Hit(PointerEventData pointer)
        {
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            return hits.Select(hit => ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(hit => hit != null);
        }
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMin >= outer.yMin - 1 && inner.yMax <= outer.yMax + 1;
        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state)); copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
    }
}
