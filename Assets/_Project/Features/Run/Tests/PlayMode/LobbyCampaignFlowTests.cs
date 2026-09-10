using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class LobbyCampaignFlowTests
    {
        const string PrefabFolder = "Assets/_Project/Features/Run/Prefabs/";
        const string SceneFolder = "Assets/_Project/Scenes/";
        GameApplication app;
        LocalRunStore store;
        string directory;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.State;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its application.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceLobbyCampaignTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            yield return CloseOwner();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void PreparationLobbyAndSixDicePopupHaveAuthoredContracts()
        {
#if UNITY_EDITOR
            var popup = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "DiceRollUI.prefab");
            Assert.That(popup, Is.Not.Null, "Node arrival requires an authored six-dice popup prefab.");
            var view = popup.GetComponents<MonoBehaviour>().SingleOrDefault(component =>
                component && component.GetType().FullName == "FateDice.DiceRollUI");
            Assert.That(view, Is.Not.Null, "The popup must use the production DiceRollUI view.");
            var diceField = view.GetType().GetField("dice");
            Assert.That(diceField, Is.Not.Null, "DiceRollUI requires six explicitly referenced DiceFaceView children.");
            var dice = diceField.GetValue(view) as Array;
            Assert.That(dice, Is.Not.Null);
            Assert.That(dice.Length, Is.EqualTo(6));
            var faces = dice.Cast<Component>().ToArray();
            Assert.That(faces.All(face => face && face.transform.IsChildOf(popup.transform)), Is.True,
                "All six dice must be children of this popup, not external assets.");
            Assert.That(faces.Distinct().Count(), Is.EqualTo(6), "Six references must identify six separate dice.");
            Assert.That(faces.All(face => face.GetComponent<CanvasRenderer>()), Is.True,
                "Every custom die Graphic requires its own CanvasRenderer to display its face and pips.");
            foreach (string field in new[] { "title", "detail", "result", "rollButton" })
                AssertAuthoredReference(view, field);

            var lobby = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuUI>(PrefabFolder + "MenuUI.prefab");
            Assert.That(lobby, Is.Not.Null);
            foreach (string field in new[] { "characterPanel", "settingsPanel", "growthPanel",
                "characterName", "characterDetails", "growthDetails", "characterTab", "settingsTab", "growthTab" })
                AssertAuthoredReference(lobby, field);
            Assert.That(lobby.layoutVersion, Is.EqualTo(1));
            var exploration = UnityEditor.AssetDatabase.LoadAssetAtPath<ExplorationUI>(PrefabFolder + "ExplorationUI.prefab");
            Assert.That(exploration, Is.Not.Null);
            Assert.That(exploration.layoutVersion, Is.EqualTo(2));
            Assert.That(exploration.campaignMap, Is.Not.Null);
            Assert.That(exploration.campaignMap.transform, Is.SameAs(exploration.mapContainer));
            foreach (string field in new[] { "edgeLayer", "nodeLayer", "playerMarker", "currentLocation" })
                AssertAuthoredReference(exploration.campaignMap, field);
            Assert.That(typeof(FateDiceWidgets).GetMethod("AnimateNodeArrival", new[] { typeof(string) }),
                Is.Not.Null, "The campaign map must expose arrival animation without adding a game command.");
#else
            Assert.Ignore("Authored prefab contracts run in the Unity Editor.");
#endif
        }

        [UnityTest] public IEnumerator PreparationTabsConfigureNewRunAndContinuePreservesExistingRun()
        {
            yield return OpenScene("Lobby", 1280);
            var menu = (MenuUI)Controller.UI.ActiveScreen;
            AssertPanels(menu, 0);
            Assert.That(menu.seedInput.gameObject.activeInHierarchy, Is.False);
            Assert.That(menu.GetComponentsInChildren<CampaignMapView>().Length, Is.Zero, "Lobby is preparation, not the campaign map.");
            Assert.That(menu.characterName.text, Is.EqualTo(KoreanText.Content(Prefab().controller.config.Snapshot().growth.characterName)));
            Click(menu.settingsTab); yield return null;
            AssertPanels(menu, 1);
            var rules = Prefab().controller.config.Snapshot();
            string trial = rules.combat.trialActionIds.Last();
            Click(Command("trial-" + trial)); yield return null;
            AssertPanels(menu, 1);
            Click(Command("cap-Common")); yield return null;
            AssertPanels(menu, 1);
            Click(menu.growthTab); yield return null;
            AssertPanels(menu, 2);
            Assert.That(menu.growthDetails.text, Does.Contain("영구 성장"));
            Assert.That(menu.growthDetails.text, Does.Contain("추후"));
            Click(menu.characterTab); yield return null;
            AssertPanels(menu, 0);
            Assert.That(store.Exists, Is.False, "Preparation choices must not create a run or spend RNG.");
            Controller.Seed = 88;
            Click(Command("new")); yield return WaitScene("InGame");
            Assert.That(State.phase, Is.EqualTo(RunPhase.Map));
            Assert.That(State.initialSeed, Is.EqualTo(88u));
            Assert.That(State.explorationCap, Is.EqualTo(Grade.Common));
            CollectionAssert.AreEquivalent(rules.combat.startingActionIds.Concat(new[] { trial }).Distinct(), State.actionIds);
            var checkpoint = Copy(State);
            Click(Command("menu")); yield return WaitScene("Lobby");
            menu = (MenuUI)Controller.UI.ActiveScreen;
            AssertPanels(menu, 0);
            Click(menu.settingsTab); yield return null;
            Click(Command("cap-Legendary")); yield return null;
            AssertState(checkpoint);
            Click(Command("continue")); yield return WaitScene("InGame");
            AssertState(checkpoint);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ArrivalAndSixDiceCommitBeforeAnimationAtBothPortraitRatios()
        {
            foreach (int height in new[] { 1280, 1600 })
            {
                var run = NewRun(NodeType.Combat);
                store.Save(run.State);
                yield return OpenScene("InGame", height);
                var exploration = (ExplorationUI)Controller.UI.ActiveScreen;
                var map = exploration.campaignMap;
                AssertGraph(map, State);
                foreach (var id in State.availableNodeIds) EnsureVisible(Command("node-" + id));
                var oracle = new RunSession(Copy(State));
                string selected = State.availableNodeIds[0];
                var nodeButton = Command("node-" + selected);
                var start = map.playerMarker.anchoredPosition;
                Assert.That(oracle.ChooseNode(selected), Is.True);
                Click(nodeButton);
                Assert.That(Controller.Busy, Is.True);
                Assert.That(Controller.UI.Popups, Is.Empty, "Arrival must finish before the roll window opens.");
                AssertState(oracle.State);
                nodeButton.onClick.Invoke();
                AssertState(oracle.State);
                yield return Unlocked();
                Assert.That(map.playerMarker.anchoredPosition, Is.Not.EqualTo(start), "The marker must travel to the chosen node.");
                var popup = RollPopup();
                Assert.That(popup.dice, Has.Length.EqualTo(6));
                Assert.That(popup.dice.Select(d => d.Value), Is.All.EqualTo(0), "Unrolled faces cannot invent a saved result.");
                Assert.That(popup.IsRolling, Is.False);
                foreach (var die in popup.dice) AssertSafe((RectTransform)die.transform, 48);
                var underlying = Command("menu");
                var pointer = Pointer(ScreenRect((RectTransform)underlying.transform).center);
                var hit = Hit(pointer);
                Assert.That(hit, Is.Not.SameAs(underlying.gameObject), "The modal must intercept the underlying menu.");
                if (hit) Dispatch(hit, pointer);
                underlying.onClick.Invoke();
                Assert.That(app.sceneFlow.CurrentRole, Is.EqualTo(GameSceneRole.InGame));
                AssertState(oracle.State);

                // Dismissing and reopening the existing popup API must preserve the pending roll.
                Assert.That(Controller.UI.CloseTopPopup(), Is.True);
                yield return null;
                Click(Command("open-dice")); yield return null;
                AssertState(oracle.State);
                popup = RollPopup();
                var rollButton = popup.rollButton.button;
                Assert.That(oracle.Roll(), Is.True);
                Click(rollButton);
                Assert.That(Controller.Busy && popup.IsRolling, Is.True);
                Assert.That(State.phase, Is.EqualTo(RunPhase.ExplorationCards));
                AssertState(oracle.State);
                rollButton.onClick.Invoke();
                AssertState(oracle.State);
                float deadline = Time.realtimeSinceStartup + 8;
                while (popup.IsRolling)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                    AssertState(oracle.State);
                    yield return null;
                }
                CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(d => d.Value).ToArray());
                yield return Unlocked();
                AssertCardsPopup(false);
                AssertState(oracle.State);
                CollectionAssert.AreEquivalent(State.cards.Select(c => c.id),
                    ((FateChoiceUI)Controller.UI.Popups.Last()).Cards.Select(view => view.OfferedId));
                foreach (var card in State.cards) EnsureVisible(Command("fate-" + card.id));
                yield return CloseOwner();
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator ReopeningSavedRollAndCardsNeverDrawsExtraResults()
        {
            foreach (var phase in new[] { RunPhase.ExplorationRoll, RunPhase.ExplorationCards, RunPhase.CombatRoll, RunPhase.CombatCards })
            {
                var run = NewRun(NodeType.Combat);
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
                if (phase != RunPhase.ExplorationRoll) Assert.That(run.Roll(), Is.True);
                if (phase == RunPhase.CombatRoll || phase == RunPhase.CombatCards)
                {
                    Assert.That(run.ChooseFate(run.State.cards.Single(c => c.id == run.State.cards[0].id).id), Is.True);
                    if (phase == RunPhase.CombatCards) Assert.That(run.Roll(), Is.True);
                }
                Assert.That(run.State.phase, Is.EqualTo(phase));
                store.Save(run.State);
                var originalBytes = File.ReadAllBytes(store.Path);
                yield return OpenScene("InGame", 1280);
                AssertState(run.State);
                Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(originalBytes));
                bool pending = phase == RunPhase.ExplorationRoll || phase == RunPhase.CombatRoll;
                Assert.That(Controller.UI.Popups.Count, Is.EqualTo(pending || phase == RunPhase.ExplorationCards ? 1 : 0));
                if (!pending) AssertCardsPopup(phase == RunPhase.CombatCards);
                if (pending)
                {
                    var popup = RollPopup();
                    Assert.That(popup.IsRolling, Is.False);
                    Assert.That(run.Roll(), Is.True);
                    Click(popup.rollButton.button);
                    AssertState(run.State);
                    yield return Unlocked();
                    AssertCardsPopup(run.State.phase == RunPhase.CombatCards);
                    AssertState(run.State);
                }
                yield return CloseOwner();
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PaidRerollRemainsOneDieOneCostOutsideTheFreeRollPopup()
        {
            var run = NewRun(NodeType.Event);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll() && run.ChooseFate(run.State.cards[0].id), Is.True);
            Assert.That(run.ResolveEncounter(false) && run.ClaimReward(), Is.True);
            Assert.That(run.State.rerollUnlocked, Is.True, "The fixture earns the real event reward.");
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll(), Is.True);
            store.Save(run.State);
            yield return OpenScene("InGame", 1600);
            var before = (int[])State.dice.Clone();
            int charges = State.rerollCharges;
            Assert.That(run.Reroll(2), Is.True);
            Click(Command("die-2"));
            yield return Unlocked();
            AssertState(run.State);
            Assert.That(State.rerollCharges, Is.EqualTo(charges - State.config.growth.rerollCost));
            for (int i = 0; i < 6; i++) if (i != 2) Assert.That(State.dice[i], Is.EqualTo(before[i]));
            AssertCardsPopup(false); // Paid reroll returns to the fate choices after its one-die presentation.
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator TwoResolvedNodesRemainOnTheCampaignPathAcrossSaveReopen()
        {
            var run = NewRun(NodeType.Treasure);
            var chosen = new List<string>();
            for (int step = 0; step < 2; step++)
            {
                chosen.Add(run.State.availableNodeIds[0]);
                Assert.That(run.ChooseNode(chosen[step]) && run.Roll(), Is.True);
                Assert.That(run.ChooseFate(run.State.cards[0].id), Is.True);
                Assert.That(run.ResolveEncounter(false) && run.ClaimReward(), Is.True);
                // Complete the actual reward decisions before counting the event as resolved.
                while (run.State.phase == RunPhase.EquipmentChoice)
                {
                    bool accepted = !string.IsNullOrEmpty(run.State.pendingEquipmentId)
                        ? run.Equip(true) : run.ReplaceDie(0);
                    Assert.That(accepted, Is.True);
                }
                Assert.That(run.State.phase, Is.EqualTo(RunPhase.Map));
            }
            Assert.That(run.State.eventsResolved, Is.EqualTo(2));
            CollectionAssert.AreEqual(chosen, run.State.resolvedEventIds);
            var discarded = run.State.nodeHistory.Where(node => !chosen.Contains(node.id)).Select(node => node.id).ToArray();
            Assert.That(discarded.Length, Is.GreaterThan(0), "The fixture must distinguish completed nodes from abandoned branches.");
            var visibleIds = run.State.nodes.Select(node => node.id).Concat(chosen).ToArray();
            store.Save(run.State);
            var bytes = File.ReadAllBytes(store.Path);
            Dictionary<string, Vector2> originalPositions = null;

            for (int reopen = 0; reopen < 2; reopen++)
            {
                yield return OpenScene("InGame", 1280);
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertState(run.State);
                Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Opening the completed path must not rewrite the saved run.");
                var map = ((ExplorationUI)Controller.UI.ActiveScreen).campaignMap;
                var views = map.nodeLayer.GetComponentsInChildren<ExplorationNodeView>(true);
                Assert.That(views.Select(view => view.NodeId).Distinct().Count(), Is.EqualTo(views.Length));
                CollectionAssert.AreEquivalent(visibleIds, views.Select(view => view.NodeId));
                Assert.That(views.All(view => view.gameObject.activeInHierarchy && !view.IsFading), Is.True);
                Assert.That(views.Any(view => discarded.Contains(view.NodeId)), Is.False, "Abandoned history is not the completed route.");
                var positions = views.ToDictionary(view => view.NodeId, view => ((RectTransform)view.transform).anchoredPosition);
                foreach (string id in chosen)
                {
                    var view = views.Single(node => node.NodeId == id);
                    Assert.That(view.transform.parent, Is.SameAs(map.nodeLayer), "Completed nodes belong to the same graph geometry as current choices.");
                    Assert.That(view.frame.button.IsInteractable(), Is.False, "A completed node cannot be selected again.");
                    view.frame.button.onClick.Invoke();
                }
                AssertState(run.State);
                Assert.That(positions[chosen[1]].y, Is.GreaterThan(positions[chosen[0]].y), "Completed nodes retain their actual travel order from bottom to top.");
                var completedEdge = map.edgeLayer.Find("Path " + chosen[0] + " -> " + chosen[1]);
                Assert.That(completedEdge && completedEdge.gameObject.activeInHierarchy, Is.True, "The completed nodes must remain connected by their actual edge.");
                foreach (var id in run.State.availableNodeIds)
                {
                    Assert.That(positions[id].y, Is.GreaterThan(positions[chosen[1]].y));
                    Assert.That(views.Single(view => view.NodeId == id).frame.button.IsInteractable(), Is.True);
                    var outgoing = map.edgeLayer.Find("Path " + chosen[1] + " -> " + id);
                    Assert.That(outgoing && outgoing.gameObject.activeInHierarchy, Is.True, "The completed route must connect to the actual current choices.");
                }
                if (originalPositions == null) originalPositions = positions;
                else
                {
                    CollectionAssert.AreEquivalent(originalPositions.Keys, positions.Keys);
                    foreach (var id in positions.Keys)
                        Assert.That(Vector2.Distance(positions[id], originalPositions[id]), Is.LessThan(.01f), "Reopening changed path geometry: " + id);
                }
                yield return CloseOwner();
            }
            LogAssert.NoUnexpectedReceived();
        }

        GameApplication Prefab()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(PrefabFolder + "GameApplication.prefab");
#else
            throw new InvalidOperationException("These integration fixtures run in the Editor.");
#endif
        }

        RunSession NewRun(NodeType type)
        {
            var config = Prefab().controller.config.Snapshot();
            config.world.mapGenerationVersion = 0; // Preserve this suite's legacy path and reward oracles.
            config.fate.nodeWeights = new float[config.fate.nodeWeights.Length];
            config.fate.nodeWeights[(int)type] = 1;
            return RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Common);
        }

        IEnumerator OpenScene(string name, int height)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice lobby campaign");
#endif
            yield return null;
            app = GameApplication.Bootstrap(Prefab(), store);
            Assert.That(Controller.Store, Is.SameAs(store));
            yield return SceneManager.LoadSceneAsync(SceneFolder + name + ".unity", LoadSceneMode.Single);
            yield return WaitScene(name);
            Assert.That(Screen.width, Is.EqualTo(720));
            Assert.That(Screen.height, Is.EqualTo(height));
        }

        IEnumerator WaitScene(string name)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (SceneManager.GetActiveScene().path != SceneFolder + name + ".unity" || Controller.Busy || app.sceneFlow.CurrentRole.ToString() != name)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), name);
                yield return null;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(Object.FindObjectsByType<GameApplication>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        IEnumerator Unlocked()
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (Controller.Busy) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        IEnumerator CloseOwner()
        {
            if (app) Object.Destroy(app.gameObject);
            yield return null;
            app = null;
            Assert.That(GameApplication.Current, Is.Null);
        }

        DiceRollUI RollPopup()
        {
            Assert.That(Controller.UI.Popups, Has.Count.EqualTo(1));
            Assert.That(Controller.UI.Popups[0], Is.TypeOf<DiceRollUI>());
            var popup = (DiceRollUI)Controller.UI.Popups[0];
            Assert.That(popup.IsOpen && popup.gameObject.activeInHierarchy, Is.True);
            return popup;
        }

        Button Command(string key)
        {
            if (Controller.UI.Popups.LastOrDefault() is FateChoiceUI fate)
            {
                if (key.StartsWith("fate-")) return fate.Cards.Single(card => card.OfferedId == key.Substring(5)).frame.button;
                if (key.StartsWith("die-")) return fate.rerollButtons[int.Parse(key.Substring(4))].button;
            }
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key, out var button), Is.True, key);
            return button;
        }

        void AssertCardsPopup(bool combat)
        {
            if (combat) Assert.That(Controller.UI.Popups, Is.Empty);
            else
            {
                Assert.That(Controller.UI.Popups, Has.Count.EqualTo(1));
                Assert.That(Controller.UI.Popups[0], Is.TypeOf<FateChoiceUI>());
            }
        }

        void Click(Button button)
        {
            Assert.That(Controller.Busy, Is.False);
            Assert.That(button.IsInteractable() && button.isActiveAndEnabled, Is.True, button.name);
            var pointer = Pointer(EnsureVisible(button));
            var target = Hit(pointer);
            Assert.That(target, Is.SameAs(button.gameObject), "The actual raycast must reach the visible control.");
            Dispatch(target, pointer);
        }

        Vector2 EnsureVisible(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            foreach (var scroll in rect.GetComponentsInParent<ScrollRect>())
            {
                scroll.StopMovement();
                for (int step = 0; !Contains(ScreenRect(scroll.viewport), ScreenRect(rect)) && step <= 40; step++)
                {
                    if (scroll.vertical) scroll.verticalNormalizedPosition = 1 - step / 40f;
                    if (scroll.horizontal) scroll.horizontalNormalizedPosition = step / 40f;
                    Canvas.ForceUpdateCanvases();
                }
                Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect(rect)), Is.True, "Clipped command: " + button.name);
            }
            AssertSafe(rect, 48);
            return ScreenRect(rect).center;
        }

        void AssertSafe(RectTransform rect, float minSize)
        {
            var bounds = ScreenRect(rect);
            Assert.That(bounds.width, Is.GreaterThanOrEqualTo(minSize));
            Assert.That(bounds.height, Is.GreaterThanOrEqualTo(minSize));
            Assert.That(Contains(ScreenRect(Controller.UI.Root.safeArea), bounds), Is.True, rect.name);
        }

        GameObject Hit(PointerEventData pointer)
        {
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            return hits.Select(hit => ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(target => target);
        }

        static void Dispatch(GameObject target, PointerEventData pointer)
        {
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        static PointerEventData Pointer(Vector2 position) => new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = position };
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]), max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMin >= outer.yMin - 1 && inner.yMax <= outer.yMax + 1;
        static RunState Copy(RunState state) => JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
        static string Stable(RunState state) { var copy = Copy(state); copy.playedSeconds = 0; return JsonUtility.ToJson(copy); }
        void AssertState(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected)), "Presentation must preserve rules state, RNG, offered IDs and pending rewards.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)), "The command must checkpoint before animation.");
        }
        static void AssertPanels(MenuUI menu, int selected)
        {
            var panels = new[] { menu.characterPanel, menu.settingsPanel, menu.growthPanel };
            for (int i = 0; i < panels.Length; i++) Assert.That(panels[i].gameObject.activeSelf, Is.EqualTo(i == selected));
        }
        static void AssertGraph(CampaignMapView map, RunState state)
        {
            var views = map.nodeLayer.GetComponentsInChildren<ExplorationNodeView>();
            Assert.That(views.Select(view => view.NodeId).Distinct().Count(), Is.EqualTo(views.Length));
            CollectionAssert.AreEquivalent(state.nodes.Select(node => node.id), views.Select(view => view.NodeId));
            Assert.That(map.edgeLayer.GetComponentsInChildren<Image>().Length,
                Is.EqualTo(state.nodes.Sum(node => node.childIds.Distinct().Count()) + state.availableNodeIds.Count));
            foreach (var node in state.nodes)
                foreach (var child in node.childIds)
                    Assert.That(((RectTransform)views.Single(view => view.NodeId == child).transform).anchoredPosition.y,
                        Is.GreaterThan(((RectTransform)views.Single(view => view.NodeId == node.id).transform).anchoredPosition.y), "The actual child must be above its parent.");
        }

        static void AssertAuthoredReference(Component owner, string name)
        {
            var field = owner.GetType().GetField(name);
            Assert.That(field, Is.Not.Null, owner.GetType().Name + "." + name + " is missing.");
            Assert.That(field.GetValue(owner) as UnityEngine.Object, Is.Not.Null,
                owner.GetType().Name + "." + name + " must reference authored UI.");
        }
    }
}
