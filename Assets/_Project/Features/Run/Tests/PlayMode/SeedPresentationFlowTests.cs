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
    public sealed class SeedPresentationFlowTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string SceneFolder = "Assets/_Project/Scenes/";
        GameApplication app;
        LocalRunStore store;
        string directory, defaultSave;
        bool defaultExisted;
        byte[] defaultBytes;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.State;

        sealed class CountingSource : ISeedSource
        {
            readonly Queue<uint> seeds;
            public int Calls { get; private set; }
            public CountingSource(params uint[] values) { seeds = new Queue<uint>(values); }
            public uint NextSeed()
            {
                Calls++;
                if (seeds.Count == 0) throw new InvalidOperationException("Unexpected seed consumption outside a new journey.");
                return seeds.Dequeue();
            }
        }

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its persistent application.");
            directory = Path.Combine(Path.GetTempPath(), "FateDiceSeedPresentationFlowTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            defaultSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            defaultExisted = File.Exists(defaultSave);
            defaultBytes = defaultExisted ? File.ReadAllBytes(defaultSave) : null;
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Fate Dice seed and presentation");
#endif
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            app = null;
            yield return null;
            try
            {
                Assert.That(GameApplication.Current, Is.Null);
                Assert.That(File.Exists(defaultSave), Is.EqualTo(defaultExisted), "An isolated test changed the user's save existence.");
                if (defaultExisted) CollectionAssert.AreEqual(defaultBytes, File.ReadAllBytes(defaultSave), "An isolated test changed the user's save bytes.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceSeedPresentationFlowTests")) + Path.DirectorySeparatorChar;
                if (!string.IsNullOrEmpty(directory) && Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [UnityTest] public IEnumerator EachNewJourneyRequestsExactlyOneSeedEvenWhenTheSourceRepeatsAValue()
        {
            var source = new CountingSource(211, 211);
            yield return Open("Lobby", source);
            for (int i = 0; i < 5; i++) { _ = Controller.Seed; Controller.EnterLobby(); }
            yield return Settled();
            Assert.That(source.Calls, Is.Zero, "Initialization, getter and menu rendering cannot consume a future seed.");
            Assert.That(store.Exists, Is.False);
            Click(Command("new"));
            Assert.That(source.Calls, Is.EqualTo(1));
            Assert.That(State.initialSeed, Is.EqualTo(211u));
            Assert.That(store.Load().initialSeed, Is.EqualTo(211u));
            var first = Copy(State);
            yield return SceneReady("InGame");
            Click(Command("menu")); yield return SceneReady("Lobby");
            Click(Command("new"));
            Assert.That(source.Calls, Is.EqualTo(2));
            Assert.That(State.initialSeed, Is.EqualTo(211u));
            Assert.That(State.runId, Is.Not.EqualTo(first.runId), "A new journey still needs its own identity when seeds match.");
            AssertFirstRollRules(first, State);
            yield return SceneReady("InGame");
            Assert.That(source.Calls, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator FixedSeedSetterSelectsAFixedSourceForEveryFollowingNewJourney()
        {
            var source = new CountingSource();
            yield return Open("Lobby", source);
            Controller.Seed = 77;
            Assert.Throws<ArgumentOutOfRangeException>(() => Controller.Seed = 0);
            Assert.That(Controller.Seed, Is.EqualTo(77u));
            Click(Command("new")); yield return SceneReady("InGame");
            Assert.That(State.initialSeed, Is.EqualTo(77u));
            var first = Copy(State);
            Click(Command("menu")); yield return SceneReady("Lobby");
            Click(Command("new")); yield return SceneReady("InGame");
            Assert.That(State.initialSeed, Is.EqualTo(77u));
            AssertFirstRollRules(first, State);
            Assert.That(source.Calls, Is.Zero, "The replaced source must not be consumed by fixed launches or rendering.");
        }

        [UnityTest] public IEnumerator ContinuePreservesExplorationOffersAndConsumesNoSeed() => ContinueWithoutSeed(false);
        [UnityTest] public IEnumerator ContinuePreservesCombatOffersAndConsumesNoSeed() => ContinueWithoutSeed(true);
        IEnumerator ContinueWithoutSeed(bool combat)
        {
            SaveRollBoundary(combat, true, false);
            var original = store.Load();
            var bytes = File.ReadAllBytes(store.Path);
            var source = new CountingSource();
            yield return Open("Lobby", source);
            for (int i = 0; i < 4; i++) { _ = Controller.Seed; Controller.EnterLobby(); }
            yield return Settled();
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            Click(Command("continue")); yield return SceneReady("InGame");
            Assert.That(source.Calls, Is.Zero);
            Assert.That(State.initialSeed, Is.EqualTo(original.initialSeed));
            Assert.That(State.rngState, Is.EqualTo(original.rngState));
            Assert.That(Stable(State), Is.EqualTo(Stable(original)));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            Assert.That(Controller.UI.Popups, Is.Empty, "A restored card phase must not roll or open a new dice modal.");
            AssertCardsVisible(combat, original);
        }

        [UnityTest] public IEnumerator ZeroFromSourcePreservesTheExistingJourneyAndCheckpoint()
        {
            var source = new CountingSource(211, 0);
            yield return Open("Lobby", source);
            Click(Command("new")); yield return SceneReady("InGame");
            Click(Command("menu")); yield return SceneReady("Lobby");
            var originalSession = Controller.Session;
            var originalState = JsonUtility.ToJson(State);
            var bytes = File.ReadAllBytes(store.Path);
            LogAssert.Expect(LogType.Warning, new Regex("(?i)seed"));
            Click(Command("new")); yield return null;
            Assert.That(source.Calls, Is.EqualTo(2));
            Assert.That(Controller.Session, Is.SameAs(originalSession));
            Assert.That(JsonUtility.ToJson(State), Is.EqualTo(originalState));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            Assert.That(Controller.Busy, Is.False);
            Assert.That(Controller.Widgets.Notice.text, Does.Contain("시작하지 못했습니다"));
        }

        [UnityTest] public IEnumerator ZeroFromSourceCannotCreateAnInitialSaveOrSession()
        {
            var source = new CountingSource(0);
            yield return Open("Lobby", source);
            LogAssert.Expect(LogType.Warning, new Regex("(?i)seed"));
            Click(Command("new")); yield return null;
            Assert.That(source.Calls, Is.EqualTo(1));
            Assert.That(Controller.Session, Is.Null);
            Assert.That(store.Exists, Is.False);
            Assert.That(Controller.UI.ActiveScreen, Is.TypeOf<MenuUI>());
            Assert.That(Controller.Busy, Is.False);
        }

        [UnityTest] public IEnumerator ExplorationRollUsesItsOwnRollAndResultHoldDurations() => TimedRoll(false, false);
        [UnityTest] public IEnumerator CombatRollUsesItsOwnRollAndResultHoldDurations() => TimedRoll(true, false);
        [UnityTest] public IEnumerator ExplorationRerollUsesExplorationTimingAndCommitsOnce() => TimedRoll(false, true);
        [UnityTest] public IEnumerator CombatRerollUsesCombatTimingAndCommitsOnce() => TimedRoll(true, true);

        IEnumerator TimedRoll(bool combat, bool reroll)
        {
            SaveRollBoundary(combat, reroll, false);
            var source = new CountingSource();
            yield return Open("InGame", source);
            var before = Copy(State);
            var oracle = new RunSession(Copy(State));
            Assert.That(reroll ? oracle.Reroll(2) : oracle.Roll(), Is.True);
            var input = reroll ? Command("die-2") : Popup().rollButton.button;
            int checkpoints = 0;
            var save = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { checkpoints++; save(next); };
            float began = Time.unscaledTime;
            Click(input);
            var popup = Popup();
            Assert.That(Controller.Busy && popup.IsRolling, Is.True);
            AssertOracle(oracle.State);
            Assert.That(checkpoints, Is.EqualTo(1), "The command must be persisted before any presentation wait.");
            byte[] bytes = File.ReadAllBytes(store.Path);
            input.onClick.Invoke();
            Assert.That(checkpoints, Is.EqualTo(1), "Repeated input while rolling cannot consume another command.");
            float roll = combat ? .8f : .2f;
            float hold = combat ? .2f : .8f;
            yield return Until(() => !popup.IsRolling, "The dice did not reveal their committed faces.");
            float revealed = Time.unscaledTime;
            Assert.That(revealed - began, Is.GreaterThanOrEqualTo(roll - .06f));
            Assert.That(revealed - began, Is.LessThan(roll + .25f), "The other phase or a legacy minimum overrode this phase's roll duration.");
            Assert.That(popup.IsOpen && popup.gameObject.activeInHierarchy, Is.True);
            Assert.That(Controller.Busy, Is.True, "Final faces must remain visible during the configured hold.");
            CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(d => d.Value));
            Assert.That(popup.result.text, Is.EqualTo(KoreanText.HandSummary(oracle.State, true)));
            yield return new WaitForSecondsRealtime(hold * .45f);
            Assert.That(popup.IsOpen && Controller.Busy, Is.True, "The result hold ended early.");
            AssertOracle(oracle.State);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            yield return Until(() => !Controller.Busy, "The result hold did not release input.");
            float finished = Time.unscaledTime;
            Assert.That(finished - revealed, Is.GreaterThanOrEqualTo(hold - .08f));
            Assert.That(finished - revealed, Is.LessThan(hold + .25f), "The other phase or prefab legacy hold overrode the saved hold duration.");
            Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(checkpoints, Is.EqualTo(1));
            Assert.That(source.Calls, Is.Zero);
            AssertOracle(oracle.State);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            yield return Settled();
            AssertCardsVisible(combat, oracle.State);
            if (reroll)
            {
                Assert.That(State.rerollCharges, Is.EqualTo(before.rerollCharges - before.config.growth.rerollCost));
                for (int i = 0; i < 6; i++) if (i != 2) Assert.That(State.dice[i], Is.EqualTo(before.dice[i]));
                Assert.That(State.cards.Select(c => c.id).Intersect(before.cards.Select(c => c.id)), Is.Empty);
            }
        }

        [UnityTest] public IEnumerator ZeroExplorationRollAndHoldHaveNoHiddenMinimum() => ZeroRoll(false, false);
        [UnityTest] public IEnumerator ZeroCombatRollAndHoldHaveNoHiddenMinimum() => ZeroRoll(true, false);
        [UnityTest] public IEnumerator ZeroExplorationRerollAndHoldHaveNoHiddenMinimum() => ZeroRoll(false, true);
        [UnityTest] public IEnumerator ZeroCombatRerollAndHoldHaveNoHiddenMinimum() => ZeroRoll(true, true);
        IEnumerator ZeroRoll(bool combat, bool reroll)
        {
            SaveRollBoundary(combat, reroll, true);
            var source = new CountingSource();
            yield return Open("InGame", source);
            var oracle = new RunSession(Copy(State));
            Assert.That(reroll ? oracle.Reroll(2) : oracle.Roll(), Is.True);
            int checkpoints = 0;
            var save = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { checkpoints++; save(next); };
            float began = Time.unscaledTime;
            Click(reroll ? Command("die-2") : Popup().rollButton.button);
            AssertOracle(oracle.State);
            var bytes = File.ReadAllBytes(store.Path);
            // Allow display scheduling to cross two frames, but no nonzero configured wait.
            yield return null; yield return null;
            Assert.That(Controller.Busy, Is.False, "Zero timing left an input delay or hidden .65-second floor.");
            Assert.That(Time.unscaledTime - began, Is.LessThan(.5f), "Zero timing must finish before the old minimum.");
            Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(checkpoints, Is.EqualTo(1));
            Assert.That(source.Calls, Is.Zero);
            AssertOracle(oracle.State);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            yield return Settled();
            AssertCardsVisible(combat, oracle.State);
        }

        [UnityTest] public IEnumerator ZeroExplorationRollStillHoldsItsVisibleResult() => OneZeroTiming(false, true);
        [UnityTest] public IEnumerator ZeroCombatRollStillHoldsItsVisibleResult() => OneZeroTiming(true, true);
        [UnityTest] public IEnumerator ZeroExplorationHoldClosesImmediatelyAfterItsRoll() => OneZeroTiming(false, false);
        [UnityTest] public IEnumerator ZeroCombatHoldClosesImmediatelyAfterItsRoll() => OneZeroTiming(true, false);
        IEnumerator OneZeroTiming(bool combat, bool zeroRoll)
        {
            SaveRollBoundary(combat, false, false);
            var saved = store.Load();
            var timing = combat ? saved.config.presentation.combatDice : saved.config.presentation.explorationDice;
            timing.rollSeconds = zeroRoll ? 0 : .3f;
            timing.resultHoldSeconds = zeroRoll ? .3f : 0;
            store.Save(saved);
            var source = new CountingSource();
            yield return Open("InGame", source);
            var oracle = new RunSession(Copy(State));
            Assert.That(oracle.Roll(), Is.True);
            float began = Time.unscaledTime;
            Click(Popup().rollButton.button);
            AssertOracle(oracle.State);
            var popup = Popup();
            Assert.That(Controller.Busy, Is.True);
            if (zeroRoll)
            {
                Assert.That(popup.IsRolling, Is.False, "A zero roll must reveal the fixed faces immediately, even when hold is nonzero.");
                CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(d => d.Value));
                Assert.That(popup.result.text, Is.EqualTo(KoreanText.HandSummary(oracle.State, true)));
            }
            else Assert.That(popup.IsRolling, Is.True, "A zero hold must not skip the requested roll animation.");
            var bytes = File.ReadAllBytes(store.Path);
            yield return Until(() => !Controller.Busy, "The phase with one zero duration did not finish.");
            Assert.That(Time.unscaledTime - began, Is.GreaterThanOrEqualTo(.28f));
            Assert.That(Time.unscaledTime - began, Is.LessThan(.55f), "The zero component gained a hidden roll floor or legacy hold.");
            Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(source.Calls, Is.Zero);
            AssertOracle(oracle.State);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            yield return Settled();
            AssertCardsVisible(combat, oracle.State);
        }

        void SaveRollBoundary(bool combat, bool cards, bool zero)
        {
            var data = Prefab().controller.config.Snapshot();
            data.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            data.presentation.explorationDice = new RollPresentationSettings { rollSeconds = zero ? 0 : .2f, resultHoldSeconds = zero ? 0 : .8f };
            data.presentation.combatDice = new RollPresentationSettings { rollSeconds = zero ? 0 : .8f, resultHoldSeconds = zero ? 0 : .2f };
            data.presentation.rollSeconds = 1.6f; // An explicit new group must win over serialized legacy values.
            var run = RunSession.New(data, 33, data.combat.trialActionIds[0], Grade.Legendary);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            if (combat)
            {
                Assert.That(run.Roll(), Is.True);
                Assert.That(run.ChooseFate(run.State.cards.First(c => c.type == NodeType.Combat).id), Is.True);
            }
            Assert.That(run.State.phase, Is.EqualTo(combat ? RunPhase.CombatRoll : RunPhase.ExplorationRoll));
            if (cards) Assert.That(run.Roll(), Is.True);
            var prepared = run.State;
            prepared.rerollUnlocked = true;
            prepared.rerollCharges = data.growth.rerollCost * 2;
            store.Save(prepared);
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
        IEnumerator Open(string scene, ISeedSource source)
        {
            app = GameApplication.Bootstrap(Prefab(), store, source);
            Assert.That(Controller.Store, Is.SameAs(store));
            yield return SceneManager.LoadSceneAsync(SceneFolder + scene + ".unity", LoadSceneMode.Single);
            yield return SceneReady(scene);
        }
        IEnumerator SceneReady(string scene)
        {
            yield return Until(() => !Controller.Busy && SceneManager.GetActiveScene().path == SceneFolder + scene + ".unity" &&
                app.sceneFlow.CurrentRole.ToString() == scene, "Production scene did not settle: " + scene);
            yield return Settled();
        }
        static IEnumerator Until(Func<bool> condition, string reason)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), reason);
                yield return null;
            }
        }
        static IEnumerator Settled()
        {
            yield return null; Canvas.ForceUpdateCanvases();
            yield return null; Canvas.ForceUpdateCanvases();
        }
        DiceRollUI Popup()
        {
            Assert.That(Controller.UI.Popups.LastOrDefault(), Is.TypeOf<DiceRollUI>());
            return (DiceRollUI)Controller.UI.Popups.Last();
        }
        Button Command(string key)
        {
            Assert.That(Controller.Widgets.Buttons.TryGetValue(key, out var button), Is.True, "Missing visible command: " + key);
            return button;
        }
        void Click(Button button)
        {
            Assert.That(Controller.Busy, Is.False);
            var hit = AssertRaycast(button);
            var pointer = Pointer(button);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }
        GameObject AssertRaycast(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(Pointer(button), hits);
            var hit = hits.Select(result => ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject)).FirstOrDefault(target => target != null);
            Assert.That(hit, Is.SameAs(button.gameObject), "The authored UI raycast did not reach this visible command.");
            return hit;
        }
        static PointerEventData Pointer(Button button)
        {
            var rect = (RectTransform)button.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = (RectTransformUtility.WorldToScreenPoint(null, corners[0]) + RectTransformUtility.WorldToScreenPoint(null, corners[2])) * .5f
            };
        }
        void AssertCardsVisible(bool combat, RunState expected)
        {
            Assert.That(State.phase, Is.EqualTo(combat ? RunPhase.CombatCards : RunPhase.ExplorationCards));
            foreach (var card in expected.cards) AssertRaycast(Command((combat ? "card-" : "fate-") + card.id));
        }
        static RunState Copy(RunState state) => JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
        static void AssertFirstRollRules(RunState first, RunState second)
        {
            Assert.That(second.initialSeed, Is.EqualTo(first.initialSeed));
            Assert.That(second.rngState, Is.EqualTo(first.rngState));
            CollectionAssert.AreEqual(first.nodes.Select(n => n.type), second.nodes.Select(n => n.type));
            var a = new RunSession(Copy(first));
            var b = new RunSession(Copy(second));
            // Separate new journeys own different IDs; each uses its own first available node.
            Assert.That(a.ChooseNode(a.State.availableNodeIds[0]), Is.True);
            Assert.That(b.ChooseNode(b.State.availableNodeIds[0]), Is.True);
            Assert.That(a.Roll() && b.Roll(), Is.True);
            CollectionAssert.AreEqual(a.State.dice, b.State.dice);
            Assert.That(b.State.hand, Is.EqualTo(a.State.hand));
            Assert.That(b.State.fatePower, Is.EqualTo(a.State.fatePower));
            Assert.That(b.State.rngState, Is.EqualTo(a.State.rngState));
            CollectionAssert.AreEqual(a.State.cards.Select(c => c.type), b.State.cards.Select(c => c.type));
            CollectionAssert.AreEqual(a.State.cards.Select(c => c.grade), b.State.cards.Select(c => c.grade));
            CollectionAssert.AreEqual(a.State.cards.Select(c => c.contentId), b.State.cards.Select(c => c.contentId));
        }
        static string Stable(RunState state)
        {
            var copy = Copy(state);
            copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
        void AssertOracle(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected)), "Presentation changed the committed state or rule RNG.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)), "The disk checkpoint differs from the command oracle.");
        }
    }
}
