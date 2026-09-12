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
    public sealed class CampaignFlowPolishTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string SaveFailure = "Injected campaign movement save failure";
        GameApplication app;
        CountingStore store;
        string directory, userSave;
        byte[] userBytes;
        float oldTimeScale;
        int coldEntries;
        double entryBegan;
        EntryPose entryPose;
        RunUIController Controller => app.controller;
        RunState State => Controller.Session.ReadSnapshot();
        CombatUI Combat => Controller.UI.ActiveScreen as CombatUI;
        FateChoiceUI Fate => Controller.UI.Popups.LastOrDefault() as FateChoiceUI;

        sealed class CountingStore : IRunStore
        {
            public readonly LocalRunStore inner;
            public int saves, attempts;
            public bool fail;
            public CountingStore(string path) { inner = new LocalRunStore(path); }
            public bool Exists => inner.Exists;
            public RunState Load() => inner.Load();
            public string Archive() => inner.Archive();
            public void Save(RunState state)
            {
                attempts++;
                if (fail) throw new IOException(SaveFailure);
                inner.Save(state); saves++;
            }
        }
        [SetUp] public void SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null);
            oldTimeScale = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "FateDiceCampaignFlowPolish", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); store = new CountingStore(Path.Combine(directory, "run.json"));
            userSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            userBytes = File.Exists(userSave) ? File.ReadAllBytes(userSave) : null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = oldTimeScale; yield return CloseOwner();
            Assert.That(File.Exists(userSave), Is.EqualTo(userBytes != null));
            if (userBytes != null) CollectionAssert.AreEqual(userBytes, File.ReadAllBytes(userSave));
            var prefix = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceCampaignFlowPolish")) + Path.DirectorySeparatorChar;
            Assert.That(Path.GetFullPath(directory).StartsWith(prefix, StringComparison.OrdinalIgnoreCase), Is.True);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void AuthoredMapDoesNotRequireASeparateMoveButton()
        {
            var view = UnityEditor.AssetDatabase.LoadAssetAtPath<ExplorationUI>(
                "Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab");
            Assert.That(view.moveButton.gameObject.activeSelf, Is.False,
                "Available node taps must travel directly; the extra move control must be hidden in the authored view.");
        }

        [Test] public void CombatViewExposesActualEntryPlaybackAndBindingOwnership()
        {
            Assert.That(typeof(CombatUI).GetMethod("PlayEntry"), Is.Not.Null,
                "Combat entry needs an actual presentation completion before the dice window opens.");
            Assert.That(typeof(CombatUI).GetMethod("ResetEntry"), Is.Not.Null);
            Assert.That(typeof(CombatUI).GetProperty("EntryBindingVersion"), Is.Not.Null);
            Assert.That(typeof(CombatUI).GetProperty("IsEntryPlaying"), Is.Not.Null);
        }

        [UnityTest] public IEnumerator NodeTapCommitsOnceAndSaveFailureCanRetryWithoutUsingStaleCallbacks()
        {
            var run = NewRun(NodeType.Rest); store.Save(run.State); yield return Open(1280);
            var view = (ExplorationUI)Controller.UI.ActiveScreen;
            var oldData = Data<ExplorationUIData>(view); string id = State.availableNodeIds[0];
            string before = Stable(State); var bytes = Bytes();
            var marker = view.campaignMap.playerMarker.anchoredPosition;
            int saves = store.saves, attempts = store.attempts;
            store.fail = true; LogAssert.Expect(LogType.Warning, SaveFailure);
            Click(Command("node-" + id)); yield return Unlocked();
            Assert.That(store.attempts, Is.EqualTo(attempts + 1)); Assert.That(store.saves, Is.EqualTo(saves));
            Assert.That(Stable(State), Is.EqualTo(before)); CollectionAssert.AreEqual(bytes, Bytes());
            Assert.That(view.campaignMap.playerMarker.anchoredPosition, Is.EqualTo(marker));
            Assert.That(Controller.Widgets.Notice.text, Does.Contain("이동을 저장하지 못했습니다"));
            store.fail = false; Assert.That(run.ChooseNode(id), Is.True);
            var node = Command("node-" + id); Click(node);
            Assert.That(Controller.Busy, Is.True); Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(store.saves, Is.EqualTo(saves + 1)); AssertState(run.State);
            node.onClick.Invoke(); Assert.That(store.saves, Is.EqualTo(saves + 1));
            yield return Unlocked(); bytes = Bytes();
            oldData.chooseNode(id); yield return Unlocked();
            Assert.That(store.saves, Is.EqualTo(saves + 1), "The old token must remain invalid after Busy clears.");
            AssertState(run.State); CollectionAssert.AreEqual(bytes, Bytes());
            Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<DiceRollUI>());
        }

        [UnityTest] public IEnumerator AllFiveEventsReturnToCurrentNodeAndManualScrollSurvivesRefreshAtBothRatios()
        {
            foreach (int height in new[] { 1280, 1600 })
            foreach (NodeType type in new[] { NodeType.Combat, NodeType.Rest, NodeType.Treasure, NodeType.Event, NodeType.Shop })
            {
                var run = NewRun(type);
                for (int i = 0; i < 3; i++) FinishRulesEvent(run);
                Assert.That(run.State.eventsResolved, Is.EqualTo(3)); store.Save(run.State);
                var original = Bytes(); int saves = store.saves; yield return Open(height);
                var view = (ExplorationUI)Controller.UI.ActiveScreen;
                AssertFocus(view, State.resolvedEventIds.Last());
                Assert.That(store.saves, Is.EqualTo(saves)); CollectionAssert.AreEqual(original, Bytes());
                string id = State.availableNodeIds[0];
                var positions = view.campaignMap.Nodes.ToDictionary(p => p.Key, p => ((RectTransform)p.Value.transform).anchoredPosition);
                Assert.That(run.ChooseNode(id), Is.True); Click(Command("node-" + id)); yield return Unlocked(); AssertState(run.State);
                var scroll = view.campaignMap.GetComponentInParent<ScrollRect>();
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)view.campaignMap.Nodes[id].transform)), Is.False,
                    "The fixture must scroll away before returning to the map.");
                yield return CompleteVisibleEvent(run); AssertState(run.State);
                view = (ExplorationUI)Controller.UI.ActiveScreen; AssertFocus(view, id);
                foreach (var pair in view.campaignMap.Nodes)
                    Assert.That(((RectTransform)pair.Value.transform).anchoredPosition, Is.EqualTo(positions[pair.Key]));
                if (type == NodeType.Rest) yield return Capture("returned-map-" + height + ".png");
                scroll = view.campaignMap.GetComponentInParent<ScrollRect>();
                scroll.StopMovement(); scroll.verticalNormalizedPosition = .83f; yield return Settled();
                float manual = scroll.verticalNormalizedPosition; original = Bytes(); saves = store.saves;
                Assert.That(Controller.RefreshView(), Is.True); yield return Settled();
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(manual).Within(.001f), "Rebinding must retain manual scrolling.");
                AssertState(run.State); Assert.That(store.saves, Is.EqualTo(saves)); CollectionAssert.AreEqual(original, Bytes());
                yield return CloseOwner();
            }
        }

        [UnityTest] public IEnumerator FateCombatWaitsForSelectionAndEntryBeforeOpeningDiceAtBothRatios()
        {
            foreach (int height in new[] { 1280, 1600 })
            {
                var run = NewRun(NodeType.Combat, true);
                Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll(), Is.True);
                store.Save(run.State); yield return Open(height);
                var popup = Fate; var card = popup.Cards[0]; int saves = store.saves;
                Click(card.frame.button); Assert.That(store.saves, Is.EqualTo(saves));
                Assert.That(run.ChooseFate(card.OfferedId), Is.True);
                var confirm = popup.confirmButton.button; Click(confirm); confirm.onClick.Invoke();
                Assert.That(Controller.Busy && popup.IsOpen, Is.True, "Fate selection must finish before the battle appears.");
                AssertState(run.State); var bytes = Bytes();
                yield return Until(() => Combat && Combat.IsEntryPlaying, 5, "Combat entry never began.");
                var view = Combat; double began = Time.realtimeSinceStartupAsDouble;
                Assert.That(popup.IsOpen, Is.False); Assert.That(Controller.UI.Popups, Is.Empty);
                Assert.That(view.actionFeedback.text, Does.Contain("전투 시작"));
                yield return Capture("entry-" + height + ".png");
                while (view.IsEntryPlaying)
                {
                    Assert.That(Controller.Busy, Is.True); Assert.That(Controller.UI.Popups, Is.Empty);
                    Assert.That(Time.realtimeSinceStartupAsDouble - began, Is.LessThan(4)); yield return null;
                }
                yield return Unlocked();
                Assert.That(Time.realtimeSinceStartupAsDouble - began, Is.GreaterThanOrEqualTo(view.entrySeconds - .08f));
                Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<DiceRollUI>());
                Assert.That(store.saves, Is.EqualTo(saves + 1)); AssertState(run.State); CollectionAssert.AreEqual(bytes, Bytes());
                yield return CloseOwner();
            }
        }

        [UnityTest] public IEnumerator PausedCombatRollResumeCompletesEntryWithoutRepeatingItOnRerollOrNextTurn()
        {
            var run = Battle(false); var saved = run.State; saved.rerollUnlocked = true; saved.rerollCharges = 5;
            run = new RunSession(saved); store.Save(run.State); var original = Bytes(); int saves = store.saves;
            Time.timeScale = 0; yield return Open(1280, false);
            Assert.That(Controller.Busy && Combat.IsEntryPlaying, Is.True); Assert.That(Controller.UI.Popups, Is.Empty);
            Assert.That(Controller.RefreshView(), Is.False); yield return Unlocked();
            Assert.That(Time.realtimeSinceStartupAsDouble - entryBegan, Is.GreaterThanOrEqualTo(.65));
            Assert.That(coldEntries, Is.EqualTo(1)); entryPose.AssertRestored(Combat);
            Assert.That(store.saves, Is.EqualTo(saves)); AssertState(run.State); CollectionAssert.AreEqual(original, Bytes());
            Assert.That(run.Roll(), Is.True); Click(Dice().rollButton.button); yield return NoEntryUntilUnlocked(); AssertState(run.State);
            Assert.That(run.Reroll(2), Is.True); Click(Command("die-2")); yield return NoEntryUntilUnlocked(); AssertState(run.State);
            string id = run.State.cards.OrderByDescending(c => CombatRules.Evaluate(run.State, c).damage).First().id;
            Assert.That(run.ChooseAction(id), Is.True); Assert.That(run.State.phase, Is.EqualTo(RunPhase.CombatRoll));
            Click(Command("card-" + id)); yield return NoEntryUntilUnlocked();
            Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<DiceRollUI>()); Assert.That(coldEntries, Is.EqualTo(1));
            Assert.That(store.saves, Is.EqualTo(saves + 3)); AssertState(run.State);
        }

        [UnityTest] public IEnumerator CombatCardsResumeKeepsOffersWithoutAutomaticEntryOrRoll()
        {
            var run = Battle(true); store.Save(run.State); var bytes = Bytes(); int saves = store.saves;
            yield return Open(1600);
            Assert.That(Combat.IsEntryPlaying, Is.False); Assert.That(coldEntries, Is.Zero);
            Assert.That(Controller.UI.Popups, Is.Empty); AssertState(run.State);
            foreach (var card in run.State.cards) AssertHit(Command("card-" + card.id));
            Assert.That(Controller.RefreshView(), Is.True); yield return Settled();
            Assert.That(Combat.IsEntryPlaying, Is.False); Assert.That(coldEntries, Is.Zero);
            Assert.That(store.saves, Is.EqualTo(saves)); CollectionAssert.AreEqual(bytes, Bytes());
        }

        [UnityTest] public IEnumerator CancelAndDisableRestoreEntryPoseWithoutReplayingTheSave()
        {
            foreach (string ending in new[] { "cancel", "controller", "component", "gameObject" })
            {
                var run = Battle(false); store.Save(run.State); var bytes = Bytes(); int saves = store.saves;
                yield return Open(1280, false, 20); var view = Combat;
                Assert.That(view.IsEntryPlaying && Controller.Busy, Is.True); yield return null;
                if (ending == "cancel") Controller.Playback.Cancel();
                else if (ending == "controller") Controller.enabled = false;
                else if (ending == "component") view.enabled = false;
                else view.gameObject.SetActive(false);
                yield return Unlocked(); Assert.That(view.IsEntryPlaying, Is.False, ending); entryPose.AssertRestored(view);
                if (ending == "cancel") Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<DiceRollUI>());
                else Assert.That(Controller.UI.Popups, Is.Empty, "A disabled entry cannot receive a dice popup.");
                if (ending == "controller")
                {
                    Controller.enabled = true; yield return Unlocked();
                    Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<DiceRollUI>()); Assert.That(coldEntries, Is.EqualTo(1));
                }
                Assert.That(store.saves, Is.EqualTo(saves)); AssertState(run.State); CollectionAssert.AreEqual(bytes, Bytes());
                yield return CloseOwner();
            }
        }

        [UnityTest] public IEnumerator OldEntryCancelAndControllerDisablePreserveReboundCombatAndNewDicePopup()
        {
            foreach (bool disable in new[] { false, true })
            {
                var run = Battle(false); store.Save(run.State); var bytes = Bytes(); int saves = store.saves;
                yield return Open(1280, false, 20); var view = Combat; int oldBinding = view.EntryBindingVersion;
                var replacement = Data<CombatUIData>(view); replacement.enemyName = "새 전투 표시";
                Controller.UI.Show<CombatUI>(replacement); Assert.That(view.EntryBindingVersion, Is.GreaterThan(oldBinding));
                var pose = new EntryPose(view); var next = view.PlayEntry();
                try
                {
                    Assert.That(next.MoveNext(), Is.True);
                    var popup = Controller.UI.ShowPopup<DiceRollUI>(new DiceRollUIData { title = "새 주사위 소유자", duration = 0 });
                    int binding = view.EntryBindingVersion;
                    if (disable) Controller.enabled = false; else Controller.Playback.Cancel();
                    yield return Unlocked();
                    Assert.That(view.IsOpen && view.IsEntryPlaying, Is.True); Assert.That(view.EntryBindingVersion, Is.EqualTo(binding));
                    Assert.That(view.actionFeedback.text, Does.Contain("새 전투 표시"));
                    Assert.That(popup.IsOpen && Controller.UI.Popups.LastOrDefault() == popup, Is.True);
                    Assert.That(popup.title.text, Is.EqualTo("새 주사위 소유자"));
                    if (disable) { Controller.enabled = true; yield return null; Assert.That(popup.IsOpen, Is.True); }
                    Assert.That(store.saves, Is.EqualTo(saves)); AssertState(run.State); CollectionAssert.AreEqual(bytes, Bytes());
                }
                finally { (next as IDisposable)?.Dispose(); }
                pose.AssertRestored(view); yield return CloseOwner();
            }
        }

        [UnityTest] public IEnumerator EntryWatchdogTimesOutOnceAtTenRealSecondsAndRecoversUnrolledDice()
        {
            var run = Battle(false); store.Save(run.State); var bytes = Bytes(); int saves = store.saves;
            Time.timeScale = 0;
            LogAssert.Expect(LogType.Error, new Regex(@"Run presentation timeout:.*elapsed=10\.\d+s; limit=10\.0s"));
            yield return Open(1280, false, 20);
            int ends = 0; Controller.Playback.Ended += _ => ends++; double began = Controller.Playback.StartedAt;
            while (Time.realtimeSinceStartupAsDouble - began < 9.4) yield return null;
            Assert.That(Controller.Busy && Combat.IsEntryPlaying, Is.True); Assert.That(Controller.UI.Popups, Is.Empty);
            yield return Unlocked(2);
            Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(Controller.Playback.EndedAt - began, Is.InRange(10d, 10.8d));
            Assert.That(ends, Is.EqualTo(1)); Assert.That(coldEntries, Is.EqualTo(1));
            Assert.That(Combat.IsEntryPlaying, Is.False); entryPose.AssertRestored(Combat);
            var popup = Dice(); Assert.That(popup.IsRolling, Is.False);
            Assert.That(popup.dice.Select(d => d.Value), Is.All.EqualTo(0));
            Assert.That(store.saves, Is.EqualTo(saves)); AssertState(run.State); CollectionAssert.AreEqual(bytes, Bytes());
            yield return null; Assert.That(ends, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator RealMouseDragFromNodeScrollsWithoutTravellingOrSaving()
        {
            var run = NewRun(NodeType.Rest); store.Save(run.State); yield return Open(1280);
            var node = Command("node-" + State.availableNodeIds[0]); EnsureVisible(node);
            var scroll = node.GetComponentInParent<ScrollRect>(); var point = ScreenRect((RectTransform)node.transform).center;
            string before = Stable(State); var bytes = Bytes(); int saves = store.saves;
            using (var mouse = new TemporaryMouse())
            {
                var trace = new List<string>();
                mouse.Queue(point, false); yield return null; yield return null;
                trace.Add("hover " + mouse.Describe(scroll));
                float start = scroll.verticalNormalizedPosition;
                mouse.Queue(point, true); yield return null;
                trace.Add("down " + mouse.Describe(scroll));
                var moved = point - Vector2.up * Mathf.Max(100, EventSystem.current.pixelDragThreshold * 4);
                mouse.Queue(moved, true); yield return null; yield return null;
                trace.Add("move1 " + mouse.Describe(scroll));
                // ScrollRect captures its origin when the threshold is crossed; keep moving after BeginDrag.
                moved -= Vector2.up * 100;
                mouse.Queue(moved, true); yield return null; yield return null;
                trace.Add("move2 " + mouse.Describe(scroll));
                mouse.Queue(moved, false); yield return null; yield return null;
                trace.Add("up " + mouse.Describe(scroll));
                Assert.That(Mathf.Abs(scroll.verticalNormalizedPosition - start), Is.GreaterThan(.005f),
                    "The real input module must actually scroll. start=" + start + " end=" + scroll.verticalNormalizedPosition + "\n" + string.Join("\n", trace));
            }
            Assert.That(Controller.Busy, Is.False); Assert.That(State.phase, Is.EqualTo(RunPhase.Map));
            Assert.That(Stable(State), Is.EqualTo(before)); Assert.That(store.saves, Is.EqualTo(saves)); CollectionAssert.AreEqual(bytes, Bytes());
        }

        static RunSession NewRun(NodeType type, bool longBattle = false)
        {
            var config = Asset<GameApplication>(AppPath).controller.config.Snapshot();
            config.world.mapGenerationVersion = 1; config.world.mapColumns = 5; config.world.mapPathCount = 5;
            config.world.eventsToBoss = 10; config.world.previewDepth = 2;
            config.fate.nodeWeights = Enumerable.Range(0, 5).Select(i => i == (int)type ? 1f : 0f).ToArray();
            config.growth.startingPower = longBattle ? 10 : 100; config.growth.startingMaxHp = 1000;
            foreach (var enemy in config.combat.enemies) { enemy.maxHp = longBattle ? 10000 : 1; enemy.power = 0; enemy.guard = 0; }
            config.presentation.explorationDice = new RollPresentationSettings { rollSeconds = 0, resultHoldSeconds = 0 };
            config.presentation.combatDice = new RollPresentationSettings { rollSeconds = 0, resultHoldSeconds = 0 };
            config.presentation.actionSeconds = 0;
            return RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Common);
        }
        static RunSession Battle(bool cards)
        {
            var run = NewRun(NodeType.Combat, true);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]) && run.Roll() && run.ChooseFate(run.State.cards[0].id), Is.True);
            Assert.That(run.State.phase, Is.EqualTo(RunPhase.CombatRoll));
            if (cards) Assert.That(run.Roll(), Is.True);
            return run;
        }
        static void FinishRulesEvent(RunSession run)
        {
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            for (int step = 0; run.State.phase != RunPhase.Map && step < 40; step++) Assert.That(Advance(run), Is.True);
            Assert.That(run.State.phase, Is.EqualTo(RunPhase.Map));
        }
        static bool Advance(RunSession run)
        {
            var state = run.State;
            switch (state.phase)
            {
                case RunPhase.ExplorationRoll: case RunPhase.CombatRoll: return run.Roll();
                case RunPhase.ExplorationCards: return run.ChooseFate(state.cards[0].id);
                case RunPhase.CombatCards: return run.ChooseAction(state.cards.OrderByDescending(c => CombatRules.Evaluate(state, c).damage).First().id);
                case RunPhase.Encounter: return run.ResolveEncounter(false);
                case RunPhase.Shop: return run.LeaveShop();
                case RunPhase.Reward: return run.ClaimReward();
                case RunPhase.EquipmentChoice: return !string.IsNullOrEmpty(state.pendingEquipmentId) ? run.Equip(true) : run.ReplaceDie(0);
                default: throw new InvalidOperationException("Unfinished fixture phase " + state.phase);
            }
        }
        IEnumerator CompleteVisibleEvent(RunSession oracle)
        {
            for (int step = 0; State.phase != RunPhase.Map && step < 40; step++)
            {
                var phase = State.phase; Button button;
                if (phase == RunPhase.ExplorationRoll || phase == RunPhase.CombatRoll) button = Dice().rollButton.button;
                else if (phase == RunPhase.ExplorationCards) { Click(Fate.Cards[0].frame.button); button = Fate.confirmButton.button; }
                else if (phase == RunPhase.CombatCards)
                    button = Command("card-" + State.cards.OrderByDescending(c => CombatRules.Evaluate(State, c).damage).First().id);
                else button = Command(phase == RunPhase.Encounter ? "resolve" : phase == RunPhase.Shop ? "leave" : phase == RunPhase.Reward ? "claim" :
                    !string.IsNullOrEmpty(State.pendingEquipmentId) ? "equip-accept" : "die-0");
                Assert.That(Advance(oracle), Is.True); int saves = store.saves;
                Click(button); yield return Unlocked(); Assert.That(store.saves, Is.EqualTo(saves + 1)); AssertState(oracle.State);
            }
            Assert.That(State.phase, Is.EqualTo(RunPhase.Map));
        }
        IEnumerator Open(int height, bool wait = true, float? seconds = null)
        {
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice campaign flow polish");
            yield return null; coldEntries = 0; entryPose = null;
            app = GameApplication.Bootstrap(Asset<GameApplication>(AppPath), store, new FixedSeedSource(33));
            Controller.Playback.Started += playback =>
            {
                if (playback.Label != "전투 진입 / 저장 재개" || !Combat) return;
                coldEntries++;
                if (seconds.HasValue) Combat.entrySeconds = seconds.Value;
                Canvas.ForceUpdateCanvases(); entryPose = new EntryPose(Combat); entryBegan = Time.realtimeSinceStartupAsDouble;
            };
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/InGame.unity", LoadSceneMode.Single);
            if (wait) yield return Unlocked();
            else yield return Until(() => Combat && Combat.IsEntryPlaying, 5, "Saved combat entry did not begin.");
            if (wait) yield return Settled();
            Assert.That(app.sceneFlow.CurrentRole, Is.EqualTo(GameSceneRole.InGame));
        }
        IEnumerator CloseOwner()
        {
            if (app) Object.Destroy(app.gameObject);
            app = null; yield return null; Assert.That(GameApplication.Current, Is.Null);
        }
        IEnumerator Unlocked(float seconds = 12)
        {
            yield return Until(() => !Controller.Busy, seconds, "Input remained locked."); yield return Settled();
        }
        IEnumerator NoEntryUntilUnlocked()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            do
            {
                Assert.That(!Combat || !Combat.IsEntryPlaying, Is.True, "A turn or paid reroll repeated entry.");
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline)); yield return null;
            } while (Controller.Busy);
            yield return Settled();
        }
        static IEnumerator Until(Func<bool> condition, float seconds, string message)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + seconds;
            while (!condition()) { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), message); yield return null; }
        }
        static IEnumerator Settled()
        {
            yield return null; Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        }
        static IEnumerator Capture(string name)
        {
            yield return null; yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "artifacts", "campaign-flow-polish");
                Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
            }
            finally { Object.Destroy(texture); }
        }
        void AssertFocus(ExplorationUI view, string current)
        {
            var scroll = view.campaignMap.GetComponentInParent<ScrollRect>();
            Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)view.campaignMap.Nodes[current].transform)), Is.True,
                "Current node is outside the returned viewport: " + current);
            foreach (string id in State.availableNodeIds)
                Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect((RectTransform)view.campaignMap.Nodes[id].transform)), Is.True,
                    "Next available node is outside the returned viewport: " + id);
        }
        Button Command(string key) => Controller.Widgets.Buttons[key];
        DiceRollUI Dice()
        {
            Assert.That(Controller.UI.Popups.LastOrDefault(), Is.TypeOf<DiceRollUI>()); return (DiceRollUI)Controller.UI.Popups.Last();
        }
        void Click(Button button)
        {
            Assert.That(Controller.Busy, Is.False); var pointer = Pointer(button); var hit = AssertHit(button, pointer);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }
        PointerEventData Pointer(Button button)
        {
            EnsureVisible(button);
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = ScreenRect((RectTransform)button.transform).center };
        }
        GameObject AssertHit(Button button, PointerEventData pointer = null)
        {
            Assert.That(button && button.IsInteractable() && button.isActiveAndEnabled, Is.True, button ? button.name : "missing button");
            pointer = pointer ?? Pointer(button);
            var hits = new List<RaycastResult>(); Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var target = hits.Select(h => ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject)).FirstOrDefault(h => h != null);
            Assert.That(target, Is.SameAs(button.gameObject)); return target;
        }
        static void EnsureVisible(Button button)
        {
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)button.transform;
            foreach (var scroll in rect.GetComponentsInParent<ScrollRect>())
            {
                scroll.StopMovement();
                for (int step = 0; !Contains(ScreenRect(scroll.viewport), ScreenRect(rect)) && step <= 40; step++)
                {
                    if (scroll.vertical) scroll.verticalNormalizedPosition = 1 - step / 40f;
                    if (scroll.horizontal) scroll.horizontalNormalizedPosition = step / 40f;
                    Canvas.ForceUpdateCanvases();
                }
                Assert.That(Contains(ScreenRect(scroll.viewport), ScreenRect(rect)), Is.True, "Clipped control " + button.name);
            }
        }
        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]), max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMin >= outer.yMin - 1 && inner.yMax <= outer.yMax + 1;
        static T Asset<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        static T Data<T>(BaseUI view) where T : RunUIData => (T)typeof(BaseUI<T>).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        byte[] Bytes() => File.ReadAllBytes(store.inner.Path);
        void AssertState(RunState expected)
        {
            Assert.That(Stable(State), Is.EqualTo(Stable(expected))); Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)));
        }
        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state)); copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
        sealed class EntryPose
        {
            readonly Vector3 arenaScale, arenaPosition, textScale;
            readonly Quaternion arenaRotation;
            readonly Color stage, artwork, text, flash;
            public EntryPose(CombatUI view)
            {
                view.arena.ForceUpdateRectTransforms(); arenaPosition = view.arena.localPosition;
                arenaRotation = view.arena.localRotation; arenaScale = view.arena.localScale;
                textScale = view.actionFeedback.rectTransform.localScale; stage = view.stageGraphic.color;
                artwork = view.artwork.color; text = view.actionFeedback.color; flash = view.hitFlash.color;
            }
            public void AssertRestored(CombatUI view)
            {
                Assert.That(view.arena.localScale, Is.EqualTo(arenaScale));
                Assert.That(Vector3.Distance(view.arena.localPosition, arenaPosition), Is.LessThan(.001f));
                Assert.That(view.arena.localRotation, Is.EqualTo(arenaRotation));
                Assert.That(view.actionFeedback.rectTransform.localScale, Is.EqualTo(textScale));
                Assert.That(view.stageGraphic.color, Is.EqualTo(stage)); Assert.That(view.artwork.color, Is.EqualTo(artwork));
                Assert.That(view.actionFeedback.color, Is.EqualTo(text)); Assert.That(view.hitFlash.color, Is.EqualTo(flash));
                Assert.That(view.actionFeedback.text, Is.Empty);
            }
        }
        // Route synthetic input while unfocused using an owned, disposable settings clone.
        sealed class TemporaryMouse : IDisposable
        {
            readonly Type input, stateType;
            readonly object device, previous;
            readonly MethodInfo queue;
            readonly PropertyInfo settings;
            readonly Object originalSettings, temporarySettings;
            readonly HideFlags originalFlags;
            readonly string originalJson;
            bool disposed;
            public TemporaryMouse()
            {
                input = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem", true);
                stateType = Type.GetType("UnityEngine.InputSystem.LowLevel.MouseState, Unity.InputSystem", true);
                var mouse = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem", true);
                previous = mouse.GetProperty("current").GetValue(null);
                queue = input.GetMethods().Single(m => m.Name == "QueueStateEvent" && m.IsGenericMethodDefinition).MakeGenericMethod(stateType);
                settings = input.GetProperty("settings");
                originalSettings = (Object)settings.GetValue(null);
                originalFlags = originalSettings.hideFlags;
                originalJson = JsonUtility.ToJson(originalSettings);
                temporarySettings = Object.Instantiate(originalSettings);
                temporarySettings.hideFlags = HideFlags.HideAndDontSave;
                try
                {
                    SetMode("backgroundBehavior", "IgnoreFocus");
                    SetMode("editorInputBehaviorInPlayMode", "AllDeviceInputAlwaysGoesToGameView");
                    try
                    {
                        // InputManager destroys an outgoing HideAndDontSave settings object.
                        // Retain our borrowed original during the swap, then restore its exact flags.
                        if (originalFlags == HideFlags.HideAndDontSave) originalSettings.hideFlags = HideFlags.DontSave;
                        settings.SetValue(null, temporarySettings);
                    }
                    finally { originalSettings.hideFlags = originalFlags; }
                    device = input.GetMethod("AddDevice", new[] { typeof(string), typeof(string), typeof(string) })
                        .Invoke(null, new object[] { "Mouse", "CampaignFlowTestMouse", null });
                    input.GetMethod("EnableDevice").Invoke(null, new[] { device });
                    device.GetType().GetMethod("MakeCurrent").Invoke(device, null);
                }
                catch { Dispose(); throw; }
            }
            void SetMode(string name, string value)
            {
                var property = temporarySettings.GetType().GetProperty(name);
                property.SetValue(temporarySettings, Enum.Parse(property.PropertyType, value));
            }
            public void Queue(Vector2 point, bool pressed)
            {
                var state = Activator.CreateInstance(stateType);
                stateType.GetField("position").SetValue(state, point); stateType.GetField("buttons").SetValue(state, (ushort)(pressed ? 1 : 0));
                queue.Invoke(null, new[] { device, state, (object)(-1d) });
            }
            // Observe after the existing yields; never update input, dispatch events or modify the live pointer.
            public string Describe(ScrollRect scroll)
            {
                try
                {
                    var system = EventSystem.current; var module = system ? system.currentInputModule : null;
                    var position = Member(device, "position"); var button = Member(device, "leftButton");
                    var value = position.GetType().GetMethod("ReadValue", Type.EmptyTypes).Invoke(position, null);
                    var rows = new List<string>();
                    var pointers = Member(module, "m_PointerStates") as IEnumerable;
                    if (pointers != null) foreach (var pointer in pointers)
                    {
                        var data = Member(pointer, "eventData") as PointerEventData; var left = Member(pointer, "leftButton");
                        if (data == null) { rows.Add("missing eventData"); continue; }
                        rows.Add("device=" + Device(Member(data, "device")) + " control=" + Member(Member(data, "control"), "path") +
                            " pos=" + data.position + " delta=" + data.delta + " hit=" + Name(data.pointerCurrentRaycast.gameObject) +
                            " press=" + Name(data.pointerPress) + " drag=" + Name(data.pointerDrag) + " dragging=" + data.dragging +
                            " leftPressed=" + Member(left, "isPressed") + " leftPress=" + Name(Member(left, "m_PressObject") as Object) +
                            " leftDrag=" + Name(Member(left, "m_DragObject") as Object) + " leftDragging=" + Member(left, "m_Dragging"));
                    }
                    return "frame=" + Time.frameCount + " normalized=" + scroll.verticalNormalizedPosition +
                        " content=" + scroll.content.anchoredPosition + " contentRect=" + scroll.content.rect +
                        " viewport=" + ScreenRect(scroll.viewport) + " movement=" + scroll.movementType +
                        " focus=" + Application.isFocused + "/" + (system && system.isFocused) + " cursor=" + Cursor.lockState +
                        " module=" + Name(module) + " ignoresFocus=" + Member(module, "shouldIgnoreFocus") +
                        " virtual=" + Device(device) + " pos=" + value + " pressed=" + Member(button, "isPressed") +
                        " pointers=[" + string.Join("; ", rows) + "]";
                }
                catch (Exception error) { return "diagnostic unavailable: " + error.GetBaseException().Message; }
            }
            static object Member(object target, string name)
            {
                if (target == null) return null;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var property = target.GetType().GetProperty(name, flags);
                return property != null ? property.GetValue(target) : target.GetType().GetField(name, flags)?.GetValue(target);
            }
            static string Device(object value) => value == null ? "none" : Member(value, "deviceId") + ":" + Member(value, "name") +
                " enabled=" + Member(value, "enabled") + " native=" + Member(value, "native");
            static string Name(Object value) => value ? value.name : "none";
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    if (device != null) input.GetMethods().Single(m => m.Name == "RemoveDevice" && m.GetParameters().Length == 1)
                        .Invoke(null, new[] { device });
                }
                finally
                {
                    try { settings.SetValue(null, originalSettings); }
                    finally
                    {
                        try { if (previous != null) previous.GetType().GetMethod("MakeCurrent").Invoke(previous, null); }
                        finally { if (temporarySettings) Object.DestroyImmediate(temporarySettings); }
                    }
                    Assert.That(settings.GetValue(null), Is.SameAs(originalSettings));
                    Assert.That(originalSettings.hideFlags, Is.EqualTo(originalFlags));
                    Assert.That(JsonUtility.ToJson(originalSettings), Is.EqualTo(originalJson), "Original input settings must remain unchanged.");
                }
            }
        }
    }
}
