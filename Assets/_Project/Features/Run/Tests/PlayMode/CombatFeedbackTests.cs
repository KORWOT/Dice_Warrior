using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class CombatFeedbackTests
    {
        const string CombatPath = "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab";
        const string DicePath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string InGamePath = "Assets/_Project/Scenes/InGame.unity";
        GameApplication app;
        LocalRunStore store;
        string directory, defaultSave;
        byte[] defaultBytes;
        bool defaultExisted;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.State;
        CombatUI Combat => Controller.UI.ActiveScreen as CombatUI;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its application.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceCombatFeedbackTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            defaultSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            defaultExisted = File.Exists(defaultSave);
            defaultBytes = defaultExisted ? File.ReadAllBytes(defaultSave) : null;
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            yield return CloseOwner();
            try
            {
                Assert.That(File.Exists(defaultSave), Is.EqualTo(defaultExisted), "The isolated fixture changed the user's save existence.");
                if (defaultExisted) Assert.That(File.ReadAllBytes(defaultSave), Is.EqualTo(defaultBytes), "The isolated fixture changed the user's save bytes.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceCombatFeedbackTests")) + Path.DirectorySeparatorChar;
                if (!string.IsNullOrEmpty(directory) && Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test] public void AuthoredFeedbackCanShowActionDamageAndHoldDiceResults()
        {
#if UNITY_EDITOR
            var combat = UnityEditor.AssetDatabase.LoadAssetAtPath<CombatUI>(CombatPath);
            var dice = UnityEditor.AssetDatabase.LoadAssetAtPath<DiceRollUI>(DicePath);
            Assert.That(combat, Is.Not.Null);
            Assert.That(dice, Is.Not.Null);
            AssertPublicReference<Text>(combat, "actionFeedback");
            AssertPublicReference<Text>(combat, "damageFeedback");
            AssertPublicReference<Image>(combat, "hitFlash");
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.data.presentation.explorationDice, Is.Not.Null);
            Assert.That(config.data.presentation.combatDice, Is.Not.Null);
            foreach (bool inCombat in new[] { false, true })
                Assert.That(config.data.presentation.DiceTiming(inCombat).resultHoldSeconds, Is.GreaterThanOrEqualTo(.8f),
                    "The authored default keeps the six final faces and combination readable in both phases.");
#else
            Assert.Fail("Authored prefab integration tests run in the Editor.");
#endif
        }

        static void AssertPublicReference<T>(Component component, string name) where T : Component
        {
            var field = component.GetType().GetField(name);
            Assert.That(field, Is.Not.Null, "Missing authored feedback reference: " + name);
            Assert.That(field.FieldType, Is.EqualTo(typeof(T)), name);
            Assert.That(field.GetValue(component), Is.Not.Null, name + " must be connected on the actual prefab.");
        }

        [Test] public void HandSummaryUsesSavedPriorityOrdinalAndCustomLabelWithoutChangingTheRun()
        {
            var config = Prefab().controller.config.Snapshot();
            var state = RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Legendary).State;
            // Deliberately sparse and reversed priorities make enum position and priority/10 wrong.
            for (int i = 0; i < state.config.dice.hands.Length; i++)
                state.config.dice.hands[i].priority = 1000 - i * 23;
            var hand = state.config.dice.hands[2];
            hand.label = "나만의 조합 / Custom hand";
            state.dice = new[] { 1, 1, 1, 2, 3, 4 };
            state.hand = hand.kind;
            state.fatePower = 7; // Display current state, not the hand definition or grade enum.
            string before = JsonUtility.ToJson(state);
            string line = KoreanText.HandSummary(state);
            string multiline = KoreanText.HandSummary(state, true);
            foreach (string text in new[] { line, multiline })
            {
                Assert.That(text, Does.Contain("나만의 조합 / Custom hand"));
                Assert.That(text, Does.Contain("조합 단계 8/10"));
                Assert.That(text, Does.Match(@"운명력\s+7(?!\d)"));
            }
            Assert.That(line, Does.Not.Contain("\n"));
            Assert.That(multiline, Does.Contain("\n"));
            Assert.That(JsonUtility.ToJson(state), Is.EqualTo(before), "Formatting must not mutate the snapshot or consume RNG.");
        }

        [UnityTest] public IEnumerator Portrait1280ShowsSixFinalFacesAndCombinationBeforeClosing() => RollResult(1280);
        [UnityTest] public IEnumerator Portrait1600ShowsSixFinalFacesAndCombinationBeforeClosing() => RollResult(1600);

        IEnumerator RollResult(int height)
        {
            CombatSave(false);
            yield return OpenCombat(height);
            var popup = Controller.UI.Popups.Last() as DiceRollUI;
            Assert.That(popup, Is.Not.Null);
            var facePositions = popup.dice.Select(die => die.rectTransform.localPosition).ToArray();
            var resultScale = popup.result.transform.localScale;
            var oracle = new RunSession(Copy(State));
            Assert.That(oracle.Roll(), Is.True);
            int saves = 0;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
            var roll = popup.rollButton.button;
            Click(roll);
            Assert.That(Controller.Busy && popup.IsRolling, Is.True);
            AssertOracle(oracle.State);
            var bytes = File.ReadAllBytes(store.Path);
            roll.onClick.Invoke();
            yield return Until(() => !popup.IsRolling, "The dice did not reveal their fixed result.");
            Assert.That(popup.IsOpen && popup.gameObject.activeInHierarchy, Is.True);
            Assert.That(Controller.Busy, Is.True, "Input must stay locked while the result is readable.");
            CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(die => die.Value).ToArray());
            for (int i = 0; i < popup.dice.Length; i++)
                AssertRestoredPosition(popup.dice[i].rectTransform.localPosition, facePositions[i], "Visible final die " + i);
            var hand = oracle.State.config.dice.hands.Single(h => h.kind == oracle.State.hand);
            int ordinal = 1 + oracle.State.config.dice.hands.Count(h => h.priority < hand.priority);
            Assert.That(popup.result.text, Does.Contain(KoreanText.Content(hand.label)));
            Assert.That(popup.result.text, Does.Contain("조합 단계 " + ordinal + "/10"));
            Assert.That(popup.result.text, Does.Match(@"운명력\s+" + oracle.State.fatePower + @"(?!\d)"));
            float revealed = Time.realtimeSinceStartup;
            var feedback = popup.resultFeedback;
            var initialPhases = feedback.dieAuras.Select(aura => aura.Phase).ToArray();
            var initialBannerScale = feedback.transform.localScale;
            bool pulsed = false;
            while (popup.IsOpen && Time.realtimeSinceStartup - revealed < .7f)
            {
                pulsed |= feedback.transform.localScale != initialBannerScale ||
                    feedback.dieAuras.Where((aura, index) => aura.Visible && aura.Phase != initialPhases[index]).Any();
                Assert.That(Controller.Busy, Is.True);
                Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Result animation must not create another checkpoint.");
                yield return null;
            }
            Assert.That(Time.realtimeSinceStartup - revealed, Is.GreaterThanOrEqualTo(.65f), "The popup closed before the result could be read.");
            Assert.That(popup.IsOpen, Is.True);
            Assert.That(pulsed, Is.True, "The dedicated combo banner or participating auras must visibly animate.");
            AssertReadable(popup.result);
            yield return Unlocked();
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(Controller.UI.Popups, Is.Empty);
            AssertOracle(oracle.State);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
            Assert.That(popup.result.transform.localScale, Is.EqualTo(resultScale));
            for (int i = 0; i < popup.dice.Length; i++)
            {
                AssertRestoredPosition(popup.dice[i].rectTransform.localPosition, facePositions[i], "Cached final die " + i);
                Assert.That(popup.dice[i].rectTransform.localRotation, Is.EqualTo(Quaternion.identity));
            }
            AssertReadable(Combat.layout.fate);
            foreach (var offer in State.cards) AssertRaycast(Command("card-" + offer.id));
        }

        [UnityTest] public IEnumerator DisablingControllerDuringRollingClosesPopupAndKeepsCommittedCards() => InterruptRoll(false);
        [UnityTest] public IEnumerator DisablingControllerDuringResultHoldClosesPopupAndKeepsCommittedCards() => InterruptRoll(true);

        IEnumerator InterruptRoll(bool afterReveal)
        {
            CombatSave(false);
            yield return OpenCombat(1280);
            var popup = Controller.UI.Popups.Last() as DiceRollUI;
            Assert.That(popup, Is.Not.Null);
            var oracle = new RunSession(Copy(State));
            Assert.That(oracle.Roll(), Is.True);
            int saves = 0;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
            Click(popup.rollButton.button);
            Assert.That(Controller.Busy && popup.IsRolling, Is.True);
            AssertOracle(oracle.State);
            byte[] bytes = File.ReadAllBytes(store.Path);
            if (afterReveal)
            {
                yield return Until(() => !popup.IsRolling, "The roll never reached its result hold before interruption.");
                CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(die => die.Value).ToArray());
            }
            else
            {
                yield return null;
                Assert.That(popup.IsRolling, Is.True, "This variant must interrupt rolling before the result hold begins.");
            }
            Assert.That(popup.IsOpen && popup.gameObject.activeInHierarchy, Is.True);
            Assert.That(Controller.Busy, Is.True, "The interruption must occur while this presentation owns the input lock.");
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
            Controller.enabled = false;
            yield return null;
            Controller.enabled = true;
            yield return Settled();
            Assert.That(Controller.Busy, Is.False, "An interrupted presentation cannot retain its controller lock.");
            Assert.That(Controller.UI.Popups, Is.Empty, "The committed dice popup must not remain above the restored cards.");
            Assert.That(popup.IsOpen || popup.gameObject.activeInHierarchy || popup.IsRolling, Is.False);
            Assert.That(Combat, Is.Not.Null);
            Assert.That(State.phase, Is.EqualTo(RunPhase.CombatCards));
            CollectionAssert.AreEquivalent(oracle.State.cards.Select(card => card.id),
                Combat.actionChoices.GetComponentsInChildren<ActionCardView>().Select(card => card.OfferedId));
            foreach (var offer in oracle.State.cards)
            {
                var card = Command("card-" + offer.id);
                Assert.That(card.IsInteractable(), Is.True, "Re-enabled cards must be usable without another roll.");
                AssertRaycast(card);
            }
            Assert.That(saves, Is.EqualTo(1), "Disable/re-enable must not checkpoint or replay the already committed roll.");
            AssertOracle(oracle.State);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
        }

        enum Outcome { Attack, FullBlock, Defend, Kill, Defeat }
        [UnityTest] public IEnumerator SelectedCardCommitsOnceThenShowsActualDamageAndRetaliation() => SelectAction(Outcome.Attack);
        [UnityTest] public IEnumerator ShieldsReportBlockedDamageWithoutInventingHpLoss() => SelectAction(Outcome.FullBlock);
        [UnityTest] public IEnumerator EnemyDefenseReportsShieldGainWithoutRetaliationDamage() => SelectAction(Outcome.Defend);
        [UnityTest] public IEnumerator LethalHitReportsRemainingHpAndNeverDisplaysRetaliation() => SelectAction(Outcome.Kill);
        [UnityTest] public IEnumerator LethalRetaliationShowsActualHpLossBeforeResultScreen() => SelectAction(Outcome.Defeat);
        [UnityTest] public IEnumerator Portrait1600CombatFeedbackRemainsReadableAndButtonsStayFixed() => SelectAction(Outcome.Attack, 1600);

        IEnumerator SelectAction(Outcome outcome, int height = 1280)
        {
            CombatSave(true, outcome);
            yield return OpenCombat(height);
            var before = Copy(State);
            var view = Combat;
            var offers = State.cards.ToArray();
            var card = Command("card-" + offers[1].id);
            var sibling = Command("card-" + offers[0].id);
            var cardScale = card.transform.localScale;
            var siblingScale = sibling.transform.localScale;
            var origin = view.arena.anchoredPosition;
            var rotation = view.arena.localRotation;
            var menuPosition = ScreenRect((RectTransform)view.menuButton.transform).center;
            var flashColor = view.hitFlash.color;
            var oracle = new RunSession(Copy(State));
            Assert.That(oracle.ChooseAction(offers[1].id), Is.True);
            int expectedEnemy = outcome == Outcome.FullBlock ? 20 : outcome == Outcome.Kill ? 0 : 14;
            int expectedHp = outcome == Outcome.Attack ? 23 : outcome == Outcome.Defeat ? 0 : 30;
            Assert.That(oracle.State.enemyHp, Is.EqualTo(expectedEnemy));
            Assert.That(oracle.State.hp, Is.EqualTo(expectedHp));
            if (outcome == Outcome.Defend) Assert.That(oracle.State.enemyShield, Is.EqualTo(7));
            if (outcome == Outcome.Kill) Assert.That(oracle.State.rngState, Is.EqualTo(before.rngState));

            int saves = 0;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
            Click(card);
            Assert.That(Controller.Busy, Is.True);
            AssertOracle(oracle.State);
            Assert.That(saves, Is.EqualTo(1), "The action must be checkpointed before any visual delay.");
            byte[] bytes = File.ReadAllBytes(store.Path);
            Assert.That(Controller.UI.ActiveScreen, Is.SameAs(view));
            Assert.That(view.IsFeedbackPlaying, Is.False, "Card selection must precede the battle reaction.");
            AssertHealth(view, before.hp, before.enemyHp, 40, 30);
            CollectionAssert.AreEquivalent(offers.Select(offer => offer.id),
                view.actionChoices.GetComponentsInChildren<ActionCardView>().Select(action => action.OfferedId));
            card.onClick.Invoke();
            sibling.onClick.Invoke();
            view.menuButton.button.onClick.Invoke();
            AssertOracle(oracle.State);
            bool selectedPulsed = false;
            float deadline = Time.realtimeSinceStartup + 10;
            while (!view.IsFeedbackPlaying)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Selected card never entered combat feedback.");
                selectedPulsed |= card.transform.localScale != cardScale;
                Assert.That(sibling.transform.localScale, Is.EqualTo(siblingScale), "Only the selected card should pulse.");
                AssertHealth(view, before.hp, before.enemyHp, 40, 30);
                Assert.That(Controller.UI.ActiveScreen, Is.SameAs(view));
                yield return null;
            }
            Assert.That(selectedPulsed, Is.True);
            Assert.That(card.transform.localScale, Is.EqualTo(cardScale), "Card scale must restore before the attack phase.");
            Assert.That(view.actionFeedback.text, Does.Contain("시험 타격"));
            Assert.That(view.actionFeedback.text, Does.Contain(KoreanText.Grade(offers[1].grade)));
            string playerAction = view.actionFeedback.text;
            bool sawEnemyHit = false, sawRetaliation = false, sawShake = false, sawFlash = false;
            bool checkedActionText = false, checkedEnemyText = false;
            while (Controller.Busy)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Combat feedback never released its input lock.");
                Assert.That(Controller.UI.ActiveScreen, Is.SameAs(view), "Reward/result rendering must wait for the feedback to finish.");
                Assert.That(Vector2.Distance(ScreenRect((RectTransform)view.menuButton.transform).center, menuPosition), Is.LessThan(.1f),
                    "The arena may shake, but command buttons must remain fixed.");
                Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
                sawShake |= Vector2.Distance(view.arena.anchoredPosition, origin) > .1f;
                sawFlash |= view.hitFlash.gameObject.activeInHierarchy && view.hitFlash.color.a > .01f;
                Assert.That(view.hitFlash.raycastTarget, Is.False);
                if (view.IsFeedbackPlaying && !string.IsNullOrEmpty(view.damageFeedback.text))
                {
                    bool enemyReacting = view.actionFeedback.text != playerAction;
                    if (!enemyReacting)
                    {
                        sawEnemyHit = true;
                        Assert.That(view.damageFeedback.text, Does.Match(@"적 체력\s*-" + (before.enemyHp - expectedEnemy) + @"(?!\d)"),
                            "The player phase must report actual HP lost, including the remaining HP cap on a lethal hit.");
                        Assert.That(view.damageFeedback.text, Does.Match(@"공격\s+10(?!\d)"));
                        Assert.That(view.damageFeedback.text, Does.Match(@"수호\s*\+3(?!\d)"));
                        Assert.That(view.damageFeedback.text, Does.Match(@"막힘\s+" + (outcome == Outcome.FullBlock ? 10 : 4) + @"(?!\d)"));
                        AssertHealth(view, before.hp, expectedEnemy, 40, 30);
                        if (!checkedActionText)
                        {
                            Canvas.ForceUpdateCanvases();
                            AssertReadable(view.actionFeedback);
                            AssertReadable(view.damageFeedback);
                            checkedActionText = true;
                        }
                    }
                    else
                    {
                        sawRetaliation = true;
                        Assert.That(outcome, Is.Not.EqualTo(Outcome.Kill), "A defeated enemy cannot show any counteraction.");
                        if (outcome == Outcome.Defend)
                        {
                            Assert.That(view.damageFeedback.text, Does.Contain("수호"));
                            AssertNumber(view.damageFeedback.text, 7);
                            Assert.That(view.enemyShield.text, Does.Match(@"수호\s+7(?!\d)"));
                        }
                        else
                        {
                            Assert.That(view.damageFeedback.text, Does.Match(@"내 체력\s*-" + (before.hp - expectedHp) + @"(?!\d)"),
                                "Incoming damage must report actual HP lost, including the remaining HP cap on defeat.");
                            Assert.That(view.damageFeedback.text, Does.Match(@"받은 공격\s+12(?!\d)"));
                            Assert.That(view.damageFeedback.text, Does.Match(@"수호 흡수\s+" + (outcome == Outcome.FullBlock ? 12 : 5) + @"(?!\d)"));
                        }
                        AssertHealth(view, expectedHp, expectedEnemy, 40, 30);
                        if (!checkedEnemyText)
                        {
                            Canvas.ForceUpdateCanvases();
                            AssertReadable(view.actionFeedback);
                            AssertReadable(view.damageFeedback);
                            checkedEnemyText = true;
                        }
                    }
                }
                yield return null;
            }
            Assert.That(sawEnemyHit, Is.True, "The committed enemy HP loss needs an observable player-action phase.");
            Assert.That(sawRetaliation, Is.EqualTo(outcome != Outcome.Kill));
            Assert.That(sawShake, Is.True, "A visible arena hit reaction is required.");
            Assert.That(sawFlash, Is.True);
            Assert.That(view.arena.anchoredPosition, Is.EqualTo(origin));
            Assert.That(view.arena.localRotation, Is.EqualTo(rotation));
            Assert.That(view.hitFlash.color, Is.EqualTo(flashColor));
            Assert.That(view.IsFeedbackPlaying, Is.False);
            Assert.That(saves, Is.EqualTo(1));
            AssertOracle(oracle.State);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
            if (outcome == Outcome.Kill) Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<RewardUI>());
            else if (outcome == Outcome.Defeat) Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<ResultUI>());
            else
            {
                Assert.That(Controller.UI.ActiveScreen, Is.SameAs(view));
                AssertHealth(view, expectedHp, expectedEnemy, 40, 30);
            }
        }

        [UnityTest] public IEnumerator FailedCheckpointLeavesCardsAndHealthWithoutSuccessFeedback()
        {
            CombatSave(true);
            yield return OpenCombat(1280);
            var before = Copy(State);
            var bytes = File.ReadAllBytes(store.Path);
            var view = Combat;
            var card = Command("card-" + State.cards[0].id);
            var origin = view.arena.anchoredPosition;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = _ => throw new IOException("Combat feedback checkpoint fixture failure");
            LogAssert.Expect(LogType.Warning, "Combat feedback checkpoint fixture failure");
            Click(card);
            float deadline = Time.realtimeSinceStartup + 10;
            while (Controller.Busy)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "A failed checkpoint did not release input.");
                Assert.That(view.IsFeedbackPlaying, Is.False);
                Assert.That(view.arena.anchoredPosition, Is.EqualTo(origin));
                AssertHealth(view, before.hp, before.enemyHp, 40, 30);
                yield return null;
            }
            Controller.Session.Checkpoint = checkpoint;
            Assert.That(Combat, Is.SameAs(view));
            Assert.That(view.layout.notice.text, Does.Contain("저장"));
            AssertOracle(before);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
            Assert.That(string.IsNullOrEmpty(view.actionFeedback.text), Is.True);
            Assert.That(string.IsNullOrEmpty(view.damageFeedback.text), Is.True);
        }

        [UnityTest] public IEnumerator ClosingViewDuringFeedbackRestoresArenaAndReopenUsesCommittedSave()
        {
            CombatSave(true);
            yield return OpenCombat(1280);
            var view = Combat;
            var origin = view.arena.anchoredPosition;
            var rotation = view.arena.localRotation;
            var color = view.hitFlash.color;
            var oracle = new RunSession(Copy(State));
            var id = State.cards[0].id;
            Assert.That(oracle.ChooseAction(id), Is.True);
            Click(Command("card-" + id));
            yield return Until(() => view.IsFeedbackPlaying && Vector2.Distance(view.arena.anchoredPosition, origin) > .1f,
                "The arena never reached a visible hit reaction before interruption.");
            var bytes = File.ReadAllBytes(store.Path);
            Controller.UI.CloseAll();
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsFeedbackPlaying, Is.False);
            Assert.That(view.arena.anchoredPosition, Is.EqualTo(origin));
            Assert.That(view.arena.localRotation, Is.EqualTo(rotation));
            Assert.That(view.hitFlash.color, Is.EqualTo(color));
            Assert.That(string.IsNullOrEmpty(view.actionFeedback.text), Is.True);
            Assert.That(string.IsNullOrEmpty(view.damageFeedback.text), Is.True);
            yield return CloseOwner();
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes));
            yield return OpenCombat(1280);
            AssertOracle(oracle.State);
            AssertHealth(Combat, 23, 14, 40, 30);
            Assert.That(Combat.IsFeedbackPlaying, Is.False);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Reopening an interrupted presentation must not replay the action.");
        }

        RunSession CombatSave(bool rolled, Outcome outcome = Outcome.Attack)
        {
            var config = Prefab().controller.config.Snapshot();
            config.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            config.fate.gradeMultipliers = new float[] { 1, 1, 1, 1, 1 };
            config.growth.startingMaxHp = 40;
            config.growth.startingPower = 10;
            config.growth.startingGuard = 6;
            config.presentation.actionSeconds = .22f;
            config.presentation.explorationDice = new RollPresentationSettings();
            config.presentation.combatDice = new RollPresentationSettings();
            foreach (var action in config.combat.actions)
            {
                action.label = "시험 타격";
                action.damageCoefficient = 1;
                action.blockCoefficient = .5f;
            }
            foreach (var enemy in config.combat.enemies)
            {
                enemy.maxHp = 30;
                enemy.power = 12;
                enemy.guard = 7;
                foreach (var intent in enemy.intents)
                {
                    intent.kind = outcome == Outcome.Defend ? IntentKind.Defend : IntentKind.Attack;
                    intent.coefficient = 1;
                }
            }
            var run = RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Legendary);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll(), Is.True);
            Assert.That(run.ChooseFate(run.State.cards[0].id), Is.True);
            Assert.That(run.State.phase, Is.EqualTo(RunPhase.CombatRoll));
            var prepared = run.ReadSnapshot();
            prepared.hp = outcome == Outcome.Defeat ? 4 : 30;
            prepared.enemyHp = outcome == Outcome.Kill ? 3 : 20;
            prepared.shield = outcome == Outcome.FullBlock ? 20 : 2;
            prepared.enemyShield = outcome == Outcome.FullBlock ? 12 : 4;
            run = new RunSession(prepared);
            if (rolled) Assert.That(run.Roll(), Is.True);
            store.Save(run.State);
            return run;
        }

        static GameApplication Prefab()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>(AppPath);
            Assert.That(prefab, Is.Not.Null);
            return prefab;
#else
            throw new InvalidOperationException("Production prefab integration tests run in the Editor.");
#endif
        }

        IEnumerator OpenCombat(int height)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice combat feedback");
#endif
            yield return null;
            var bytes = File.ReadAllBytes(store.Path);
            app = GameApplication.Bootstrap(Prefab(), store);
            Assert.That(Controller.Store, Is.SameAs(store));
            yield return SceneManager.LoadSceneAsync(InGamePath, LoadSceneMode.Single);
            yield return Until(() => !Controller.Busy && app.sceneFlow.CurrentRole == GameSceneRole.InGame &&
                SceneManager.GetActiveScene().path == InGamePath, "Production InGame scene did not settle.");
            yield return Settled();
            Assert.That(Screen.width, Is.EqualTo(720));
            Assert.That(Screen.height, Is.EqualTo(height));
            Assert.That(Combat, Is.Not.Null);
            Assert.That(File.ReadAllBytes(store.Path), Is.EqualTo(bytes), "Opening the saved battle must preserve its checkpoint bytes.");
        }

        IEnumerator CloseOwner()
        {
            if (app) Object.Destroy(app.gameObject);
            app = null;
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
        }

        static IEnumerator Until(Func<bool> condition, string message)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), message);
                yield return null;
            }
        }

        IEnumerator Unlocked()
        {
            yield return Until(() => !Controller.Busy, "Presentation did not release input within ten seconds.");
            yield return Settled();
        }

        static IEnumerator Settled()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        Button Command(string key)
        {
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key, out var button), Is.True, "Missing visible command " + key);
            return button;
        }

        void Click(Button button)
        {
            Assert.That(Controller.Busy, Is.False);
            Assert.That(button.IsInteractable(), Is.True);
            GameObject hit = AssertRaycast(button);
            var pointer = Pointer(button);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        GameObject AssertRaycast(Button button)
        {
            var rect = ScreenRect((RectTransform)button.transform);
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(48));
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(48));
            Assert.That(Contains(ScreenRect(Controller.UI.Root.safeArea), rect), Is.True);
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(Pointer(button), hits);
            var hit = hits.Select(result => ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject)).FirstOrDefault(target => target != null);
            Assert.That(hit, Is.SameAs(button.gameObject), "The authored screen raycast did not reach the visible command.");
            return hit;
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

        static void AssertReadable(Text text)
        {
            Assert.That(text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text), Is.True, text.name);
            Assert.That(text.raycastTarget, Is.False, "Feedback text cannot intercept command input.");
            Assert.That(Contains(Screen.safeArea, ScreenRect(text.rectTransform)), Is.True, text.text);
            Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + .1f), "Feedback text is clipped: " + text.text);
            var mesh = text.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null, "Feedback has no rendered text mesh: " + text.text);
            Assert.That(mesh.vertexCount, Is.GreaterThan(0), "Feedback produced no visible glyphs: " + text.text);
        }

        static void AssertNumber(string text, int number) => Assert.That(Regex.IsMatch(text ?? "", @"(?<!\d)" + number + @"(?!\d)"), Is.True,
            "Actual feedback must contain the independently expected quantity " + number + ": " + text);

        static void AssertRestoredPosition(Vector3 actual, Vector3 expected, string phase)
        {
            float distance = Vector3.Distance(actual, expected);
            Assert.That(distance, Is.LessThanOrEqualTo(.001f), phase + " position was not restored: expected " +
                expected.ToString("G9") + ", actual " + actual.ToString("G9") + ", distance " + distance.ToString("G9"));
        }

        static void AssertHealth(CombatUI view, int hp, int enemyHp, int maximumHp, int enemyMaximumHp)
        {
            Assert.That(view.layout.stats.text.Replace(" ", ""), Does.Contain("체력" + hp + "/" + maximumHp));
            Assert.That(view.enemyHealth.text.Replace(" ", ""), Is.EqualTo(enemyHp + "/" + enemyMaximumHp));
            Assert.That(view.playerHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)hp / maximumHp).Within(.001));
            Assert.That(view.enemyHealthFill.rectTransform.anchorMax.x, Is.EqualTo((float)enemyHp / enemyMaximumHp).Within(.001));
        }

        static RunState Copy(RunState state) => JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
        static string Stable(RunState state)
        {
            var copy = Copy(state);
            copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }

        void AssertOracle(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected)), "UI presentation changed the committed RunSession result.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)), "The committed save differs from the command oracle.");
        }
    }
}

