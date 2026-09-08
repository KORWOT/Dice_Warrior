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
    public sealed class CombatLayoutTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string CombatPath = "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab";
        const string SceneFolder = "Assets/_Project/Scenes/";
        GameApplication app;
        LocalRunStore store;
        string directory;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.State;
        CombatUI Combat => Controller.UI.ActiveScreen as CombatUI;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its persistent app.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceCombatLayoutTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        static GameApplication AppPrefab()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
            Assert.That(prefab, Is.Not.Null);
            return prefab;
#else
            throw new InvalidOperationException("Production prefab integration tests run in the Editor.");
#endif
        }

        RunSession CombatSave(int offers = 3, bool rolled = false, bool paidReroll = false)
        {
            // A snapshot and real commands establish all node/event/intent/pending-reward IDs.
            var config = AppPrefab().controller.config.Snapshot();
            config.world.offeredCards = offers;
            var run = RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Legendary);
            var node = run.State.nodes.First(n => run.State.availableNodeIds.Contains(n.id) && n.type == NodeType.Combat);
            Assert.That(run.ChooseNode(node.id) && run.Roll(), Is.True);
            Assert.That(run.ChooseFate(run.State.cards.First(c => c.type == NodeType.Combat).id), Is.True);
            Assert.That(run.State.phase, Is.EqualTo(RunPhase.CombatRoll));
            // Valid saved fixture values make fractional HP/shield and a paid reroll observable.
            run.State.hp = Math.Max(1, GrowthRules.Stats(run.State).maxHp - 7);
            run.State.enemyHp = Math.Max(1, run.State.config.Enemy(run.State.activeEnemyId).maxHp - 5);
            run.State.shield = 4;
            run.State.enemyShield = 2;
            if (paidReroll)
            {
                run.State.rerollUnlocked = true;
                run.State.rerollCharges = run.State.config.growth.rerollCost * 3;
            }
            if (rolled) Assert.That(run.Roll(), Is.True);
            store.Save(run.State); // Full save validation must accept the isolated fixture.
            return run;
        }

        IEnumerator OpenCombat(int height)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice combat layout");
#endif
            yield return null;
            var bytes = File.ReadAllBytes(store.Path);
            app = GameApplication.Bootstrap(AppPrefab(), store);
            Assert.That(Controller.Store, Is.SameAs(store), "Inject the isolated store before controller initialization.");
            yield return SceneManager.LoadSceneAsync(SceneFolder + "InGame.unity", LoadSceneMode.Single);
            yield return WaitScene(GameSceneRole.InGame);
            yield return LayoutSettled();
            Assert.That(Screen.width, Is.EqualTo(720));
            Assert.That(Screen.height, Is.EqualTo(height));
            Assert.That(Combat, Is.Not.Null);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Opening a battle must not rewrite its save.");
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        IEnumerator WaitScene(GameSceneRole role)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (Controller.Busy || app.sceneFlow.CurrentRole != role ||
                   SceneManager.GetActiveScene().path != SceneFolder + role + ".unity")
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Scene navigation did not settle.");
                yield return null;
            }
            yield return null;
        }

        IEnumerator Unlocked()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 8;
            while (Controller.Busy)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Combat input lock did not recover.");
                yield return null;
            }
            yield return LayoutSettled();
        }

        static IEnumerator LayoutSettled()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [Test] public void CombatHasDedicatedStageAndHorizontalActionCards()
        {
#if UNITY_EDITOR
            var view = UnityEditor.AssetDatabase.LoadAssetAtPath<CombatUI>(CombatPath);
            Assert.That(view.transform.Find("Battle Header"), Is.Not.Null, "Combat needs a compact header separate from the arena.");
            Assert.That(view.transform.Find("Arena"), Is.Not.Null, "Combat needs a large central stage.");
            Assert.That(view.actionChoices.GetComponent<GridLayoutGroup>(), Is.Not.Null, "Portrait action cards belong in one horizontal row.");
            Assert.That(view.actionScroll.content, Is.SameAs(view.actionChoices));
            Assert.That(view.actionScroll.horizontal, Is.True);
            Assert.That(view.actionScroll.vertical, Is.False);
            Assert.That(view.stageGraphic.raycastTarget, Is.False);
            Assert.That(view.stageGraphic.GetComponent<CanvasRenderer>(), Is.Not.Null,
                "The authored stage must have its rendering component before activation.");
#endif
        }

        [UnityTest] public IEnumerator Portrait1280RollCardsAndMenuUseTheVisibleBattleLayout() => Portrait(1280);
        [UnityTest] public IEnumerator Portrait1600RollCardsAndMenuUseTheVisibleBattleLayout() => Portrait(1600);

        IEnumerator Portrait(int height)
        {
            var run = CombatSave();
            string original = Stable(run.State);
            yield return OpenCombat(height);
            Assert.That(Stable(State), Is.EqualTo(original));
            AssertCombatData();
            AssertArenaAndDice();
            AssertTouch(Button("roll"));
            AssertTouch(Button("menu"));
            yield return LayoutSettled();
            Assert.That(Stable(State), Is.EqualTo(original), "Layout updates cannot draw RNG or grant rewards.");

            var oracle = new RunSession(store.Load());
            Assert.That(oracle.Roll(), Is.True);
            Click("roll");
            yield return Unlocked();
            AssertOracle(oracle.State);
            Assert.That(State.phase, Is.EqualTo(RunPhase.CombatCards));
            AssertCombatData();
            AssertArenaAndDice();
            AssertThreeCardsVisible();
            foreach (var card in State.cards) AssertRaycast(Button("card-" + card.id));
            string cards = Stable(State);
            Click("menu");
            yield return WaitScene(GameSceneRole.Lobby);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            Assert.That(Stable(State), Is.EqualTo(cards), "Returning to Lobby cannot consume an offer or claim its pending reward.");
            Assert.That(Stable(store.Load()), Is.EqualTo(cards));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator PaidDieAndOfferedCardClicksMatchTheRealCommandAndStoredState()
        {
            CombatSave(3, true, true);
            yield return OpenCombat(1280);
            AssertThreeCardsVisible();
            int[] dice = (int[])State.dice.Clone();
            int charges = State.rerollCharges;
            var oracle = new RunSession(store.Load());
            Assert.That(oracle.Reroll(2), Is.True);
            Click("die-2");
            yield return Unlocked();
            AssertOracle(oracle.State);
            Assert.That(State.rerollCharges, Is.EqualTo(charges - State.config.growth.rerollCost));
            for (int i = 0; i < dice.Length; i++) if (i != 2) Assert.That(State.dice[i], Is.EqualTo(dice[i]));
            AssertCombatData();
            AssertThreeCardsVisible();
            string offeredId = State.cards[1].id;
            Assert.That(oracle.ChooseAction(offeredId), Is.True);
            Click("card-" + offeredId);
            yield return Unlocked();
            AssertOracle(oracle.State);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator FiveGeneratedOffersRemainReachableAndTheLastCardUsesItsOfferedId()
        {
            CombatSave(5, true);
            yield return OpenCombat(1280);
            Assert.That(State.cards.Count, Is.EqualTo(5));
            Assert.That(State.cards.GroupBy(c => c.contentId).Any(g => g.Count() > 1), Is.True,
                "Five generated offers include repeated owned actions with distinct offer IDs.");
            var offers = State.cards.ToArray();
            var views = Combat.actionChoices.GetComponentsInChildren<ActionCardView>();
            CollectionAssert.AreEquivalent(offers.Select(c => c.id), views.Select(v => v.OfferedId));
            Assert.That(views.Select(v => v.OfferedId).Distinct().Count(), Is.EqualTo(5));
            Assert.That(Combat.actionScroll.content.rect.width, Is.GreaterThan(Combat.actionScroll.viewport.rect.width + 1));
            string before = Stable(State);
            foreach (var offer in offers)
            {
                var button = Button("card-" + offer.id);
                yield return RevealHorizontally(button);
                var view = button.GetComponentInParent<ActionCardView>();
                Assert.That(view.OriginalId, Is.EqualTo(offer.contentId));
                AssertCardEffect(view, offer);
                AssertRaycast(button);
            }
            Assert.That(Stable(State), Is.EqualTo(before), "Scrolling and inspecting offers must be read-only.");
            Assert.That(Stable(store.Load()), Is.EqualTo(before));
            var oracle = new RunSession(store.Load());
            string last = offers[4].id;
            Assert.That(oracle.ChooseAction(last), Is.True);
            Click("card-" + last);
            yield return Unlocked();
            AssertOracle(oracle.State);
            LogAssert.NoUnexpectedReceived();
        }

        IEnumerator RevealHorizontally(Button button)
        {
            var scroll = Combat.actionScroll;
            scroll.StopMovement();
            for (int step = 0; step <= 30; step++)
            {
                scroll.horizontalNormalizedPosition = step / 30f;
                Canvas.ForceUpdateCanvases();
                if (Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)button.transform)))
                {
                    yield return null;
                    yield break;
                }
            }
            Assert.Fail("The configured action card cannot be fully exposed by horizontal scrolling.");
        }

        void AssertCombatData()
        {
            var enemy = State.config.Enemy(State.activeEnemyId);
            var intent = enemy.intents.Single(i => i.id == State.activeIntentId);
            var stats = GrowthRules.Stats(State);
            Assert.That(Combat.enemyName.text, Is.EqualTo(KoreanText.Content(enemy.label)));
            Assert.That(Combat.enemyHealth.text.Replace(" ", ""), Is.EqualTo(State.enemyHp + "/" + enemy.maxHp));
            Assert.That(Combat.layout.stats.text, Does.Contain("체력 " + State.hp + "/" + stats.maxHp));
            Assert.That(Combat.playerShield.text, Does.Contain(State.shield.ToString()));
            Assert.That(Combat.enemyShield.text, Does.Contain(State.enemyShield.ToString()));
            Assert.That(Combat.layout.situation.text, Does.Contain(KoreanText.Content(intent.label)));
            Assert.That(Combat.intentValue.text, Is.EqualTo(CombatRules.IntentAmount(State).ToString()));
            Assert.That(Combat.turn.text, Is.EqualTo("누적 턴 " + (State.combatTurns + 1)));
            Assert.That(Combat.enemyHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)State.enemyHp / enemy.maxHp).Within(.001));
            Assert.That(Combat.playerHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)State.hp / stats.maxHp).Within(.001));
            if (State.phase == RunPhase.CombatCards)
            {
                var hand = State.config.dice.hands.Single(h => h.kind == State.hand);
                Assert.That(Combat.layout.fate.text, Does.Contain(KoreanText.Content(hand.label)));
                Assert.That(Combat.fatePower.text, Does.Contain(State.fatePower.ToString()));
                if (State.rerollUnlocked) Assert.That(Combat.rerolls.text, Does.Contain(State.rerollCharges.ToString()));
                else Assert.That(Combat.rerolls.gameObject.activeInHierarchy, Is.False, "An unearned resource need not be displayed.");
            }
        }

        void AssertArenaAndDice()
        {
            Rect safe = ScreenRect(Controller.UI.Root.safeArea);
            Rect arena = ScreenRect(Combat.arena);
            Assert.That(Contains(safe, arena), Is.True);
            Assert.That(arena.height, Is.GreaterThanOrEqualTo(Screen.height * .18f), "The central stage must retain useful portrait space.");
            Assert.That(arena.width, Is.GreaterThanOrEqualTo(safe.width * .8f));
            Assert.That(Combat.stageGraphic.raycastTarget, Is.False);
            var dice = Enumerable.Range(0, 6).Select(i => Button("die-" + i)).ToArray();
            var rects = dice.Select(b => ScreenRect((RectTransform)b.transform)).ToArray();
            foreach (var die in dice) AssertTouch(die);
            for (int i = 1; i < rects.Length; i++)
                Assert.That(rects[i - 1].xMax, Is.LessThanOrEqualTo(rects[i].xMin + 1), "Six dice must not overlap.");
            Assert.That(rects.Max(r => r.yMax), Is.LessThanOrEqualTo(arena.yMin + 2), "Dice belong below the arena.");
        }

        void AssertThreeCardsVisible()
        {
            Assert.That(State.cards.Count, Is.EqualTo(3));
            Rect viewport = ScreenRect(Combat.actionScroll.viewport);
            var rects = new List<Rect>();
            foreach (var offer in State.cards)
            {
                Button button = Button("card-" + offer.id);
                Rect rect = ScreenRect((RectTransform)button.transform);
                AssertTouch(button);
                Assert.That(Contains(viewport, rect), Is.True, "All three cards must be visible before any scrolling.");
                Assert.That(rect.height, Is.GreaterThan(rect.width), "Action cards must retain their portrait presentation.");
                AssertCardEffect(button.GetComponentInParent<ActionCardView>(), offer);
                rects.Add(rect);
            }
            rects = rects.OrderBy(r => r.xMin).ToList();
            for (int i = 1; i < rects.Count; i++)
            {
                Assert.That(rects[i - 1].xMax, Is.LessThanOrEqualTo(rects[i].xMin + 1));
                Assert.That(rects[i].yMin, Is.EqualTo(rects[0].yMin).Within(2), "Cards must share one row.");
            }
            Rect dice = ScreenRect(Combat.layout.diceRow);
            Assert.That(rects.Max(r => r.yMax), Is.LessThanOrEqualTo(dice.yMin + 2), "Actions must not cover the dice row.");
        }

        void AssertCardEffect(ActionCardView view, OfferedCard card)
        {
            Assert.That(view, Is.Not.Null);
            Assert.That(view.OfferedId, Is.EqualTo(card.id));
            Assert.That(view.OriginalId, Is.EqualTo(card.contentId));
            var effect = CombatRules.Evaluate(State, card);
            Assert.That(view.effectLabel.text, Is.EqualTo("피해 " + effect.damage + " / 수호 " + effect.block));
        }

        Button Button(string key)
        {
            if (key == "roll" && Controller.UI.Popups.LastOrDefault() is DiceRollUI popup) return popup.rollButton.button;
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key, out var button), Is.True, "Missing command key: " + key);
            return button;
        }

        void AssertTouch(Button button)
        {
            Rect rect = ScreenRect((RectTransform)button.transform);
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(48));
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(48));
            Assert.That(Contains(ScreenRect(Controller.UI.Root.safeArea), rect), Is.True, "Touch target is outside the safe area.");
        }

        GameObject AssertRaycast(Button button)
        {
            AssertTouch(button);
            var pointer = Pointer(button);
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var hit = hits.Select(h => ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject)).FirstOrDefault(h => h != null);
            Assert.That(hit, Is.SameAs(button.gameObject), "Visible raycast did not reach the requested command.");
            return hit;
        }

        void Click(string key)
        {
            Button button = Button(key);
            Assert.That(Controller.Busy, Is.False);
            Assert.That(button.IsInteractable(), Is.True, key);
            GameObject hit = AssertRaycast(button);
            var pointer = Pointer(button);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        static PointerEventData Pointer(Button button) => new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = ScreenRect((RectTransform)button.transform).center
        };

        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 2 && inner.xMax <= outer.xMax + 2 &&
            inner.yMin >= outer.yMin - 2 && inner.yMax <= outer.yMax + 2;

        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }

        void AssertOracle(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected)), "The visible command changed more than its RunSession oracle.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)), "The persisted command boundary differs from the run.");
        }
    }
}
