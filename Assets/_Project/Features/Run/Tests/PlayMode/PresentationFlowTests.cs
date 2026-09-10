using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    public sealed class PresentationFlowTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        GameApplication app;
        GameObject holder;
        DiceFeedbackCatalog catalogClone;
        string directory, userSave;
        byte[] userBytes;
        float oldTimeScale;
        LocalRunStore store;
        RunUIController Controller => app.controller;

        [SetUp] public void SetUp()
        {
            oldTimeScale = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "FateDiceLifecycleTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            userSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            userBytes = File.Exists(userSave) ? File.ReadAllBytes(userSave) : null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = oldTimeScale;
            if (app) Object.Destroy(app.gameObject);
            if (holder) Object.Destroy(holder);
            if (catalogClone) Object.Destroy(catalogClone);
            yield return null;
            Assert.That(GameApplication.Current, Is.Null);
            Assert.That(File.Exists(userSave), Is.EqualTo(userBytes != null));
            if (userBytes != null) CollectionAssert.AreEqual(userBytes, File.ReadAllBytes(userSave));
            string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceLifecycleTests")) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase)) Directory.Delete(directory, true);
            LogAssert.NoUnexpectedReceived();
        }

        [Test] public void CatalogProvidesDistinctEditableTierTimelines()
        {
            var field = typeof(DiceFeedbackCatalog).GetField("tiers");
            Assert.That(field, Is.Not.Null, "Result feedback needs editable per-tier timelines, not one shared entrance duration.");
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DiceFeedbackCatalog>(
                "Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset");
            var tiers = ((Array)field.GetValue(catalog)).Cast<object>().ToArray();
            Assert.That(tiers.Length, Is.EqualTo(4));
            float previous = 0;
            foreach (var tier in tiers)
            {
                float total = new[] { "entranceSeconds", "flourishSeconds", "readSeconds", "exitSeconds" }
                    .Sum(name => (float)tier.GetType().GetField(name).GetValue(tier));
                Assert.That(total, Is.GreaterThan(previous));
                previous = total;
            }
            Assert.That(previous, Is.LessThan(8), "Default legendary motion must fit the 10 second safety limit including rolling.");
            Assert.That(typeof(DiceAuraGraphic).GetProperty("Complexity"), Is.Not.Null);
        }

        [UnityTest] public IEnumerator ComplexityChangesGeometryAndMotionWithoutLosingAlphaOrBounds()
        {
            holder = new GameObject("Tier geometry", typeof(RectTransform), typeof(Canvas));
            holder.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = new GameObject("Aura", typeof(RectTransform), typeof(DiceAuraGraphic));
            go.transform.SetParent(holder.transform, false);
            var aura = go.GetComponent<DiceAuraGraphic>();
            aura.rectTransform.sizeDelta = new Vector2(620, 500);
            aura.shape = DiceAuraGraphic.AuraShape.Crest;
            int previous = -1;
            foreach (DiceEffectComplexity tier in Enum.GetValues(typeof(DiceEffectComplexity)))
            {
                aura.SetVisual(Color.red, Color.yellow, .5f, .5f, tier, 1);
                yield return null; Canvas.ForceUpdateCanvases();
                var mesh = aura.canvasRenderer.GetMesh();
                int count = mesh ? mesh.vertexCount : 0;
                Assert.That(count, Is.GreaterThan(previous), "Higher tiers need more than the same geometry recolored.");
                if (tier == DiceEffectComplexity.Simple) Assert.That(count, Is.Zero);
                previous = count;
                var middle = mesh ? mesh.vertices : Array.Empty<Vector3>();
                aura.SetVisual(Color.red, Color.yellow, .5f, .8f, tier, 0);
                yield return null; Canvas.ForceUpdateCanvases();
                mesh = aura.canvasRenderer.GetMesh();
                if (mesh && count > 0)
                {
                    Assert.That(mesh.colors32.All(color => color.a == 0), Is.True);
                    Assert.That(mesh.vertices.SequenceEqual(middle), Is.False, "Motion phase must change the light geometry.");
                    foreach (var point in mesh.vertices)
                        Assert.That(point.x >= -310 && point.x <= 310 && point.y >= -250 && point.y <= 250, Is.True);
                }
            }
        }

        [UnityTest] public IEnumerator ResultWaitsForActualFeelEndThenReadsAndFades()
        {
            holder = new GameObject("Timeline fixture", typeof(RectTransform), typeof(Canvas));
            holder.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var popup = Object.Instantiate(Asset<DiceRollUI>("Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab"), holder.transform, false);
            popup.gameObject.SetActive(true);
            yield return null;
            var feedback = popup.resultFeedback;
            catalogClone = Object.Instantiate(feedback.catalog);
            feedback.catalog = catalogClone;
            var timeline = catalogClone.ResolveTimeline(1);
            timeline.entranceSeconds = .04f; timeline.flourishSeconds = .12f; timeline.readSeconds = .12f; timeline.exitSeconds = .12f;
            var reveal = Read(feedback, "reveal");
            var scale = ((IEnumerable)Read(reveal, "FeedbacksList")).Cast<object>().Single(item => item.GetType().Name == "MMF_Scale");
            scale.GetType().GetField("AnimateScaleDuration").SetValue(scale, 4f); // Actual .5s playback, longer than .04s entrance.
            feedback.Show(new DiceRollUIData { comboName = "식스 카드", hand = HandKind.SixKind,
                comboStrength = 1, values = new[] { 6, 6, 6, 6, 6, 6 }, holdSeconds = .12f });
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(feedback.IsPresenting, Is.True);
            Assert.That(feedback.Phase, Is.EqualTo(DiceResultFeedback.ResultPhase.Entrance));
            Assert.That(Read(reveal, "IsPlaying"), Is.EqualTo(true), "Longer authored FEEL was cut off by the old timer.");
            yield return Until(() => feedback.Phase == DiceResultFeedback.ResultPhase.Reading);
            Assert.That(feedback.IsPresenting, Is.True);
            Assert.That(Read(reveal, "IsPlaying"), Is.EqualTo(false));
            Assert.That(feedback.dieAuras.All(aura => aura.Visible && aura.Opacity == 1), Is.True);
            yield return Until(() => feedback.Phase == DiceResultFeedback.ResultPhase.Exit);
            Assert.That(feedback.IsPresenting, Is.True);
            yield return Until(() => !feedback.IsPresenting);
            Assert.That(feedback.Phase, Is.EqualTo(DiceResultFeedback.ResultPhase.Completed));
            Assert.That(feedback.EndedAt - feedback.StartedAt, Is.GreaterThan(.8));
            Assert.That(((Graphic)Read(feedback, "comboName")).color.a, Is.Zero);
        }

        [UnityTest] public IEnumerator DiceWatchdogForcesEndAtTenRealSecondsWhilePaused() => Watchdog(false);
        [UnityTest] public IEnumerator CombatWatchdogForcesEndAtTenRealSecondsWhilePaused() => Watchdog(true);

        IEnumerator Watchdog(bool combat)
        {
            Prepare(combat, combat ? 0 : 60);
            yield return Open();
            var before = Controller.Session.State;
            var oracle = new RunSession(before);
            var combatView = Controller.UI.ActiveScreen as CombatUI;
            Button button;
            if (combat)
            {
                combatView.actionFeedbackSeconds = 60;
                string id = before.cards[0].id;
                Assert.That(oracle.ChooseAction(id), Is.True);
                button = Controller.Widgets.Buttons["card-" + id];
            }
            else
            {
                Assert.That(oracle.Roll(), Is.True);
                button = ((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button;
            }
            int starts = 0, ends = 0, saves = 0;
            Controller.Playback.Started += _ => starts++;
            Controller.Playback.Ended += _ => ends++;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
            Time.timeScale = 0;
            LogAssert.Expect(LogType.Error, new Regex(@"Run presentation timeout: .*elapsed=10\.\d+s; limit=10\.0s"));
            Click(button);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            var bytes = File.ReadAllBytes(store.Path);
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(9.4f);
            Assert.That(Controller.Busy && Controller.Playback.IsPlaying, Is.True, "Watchdog fired before 10 real seconds.");
            yield return Until(() => !Controller.Busy, 2);
            Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(Controller.Playback.EndedAt - Controller.Playback.StartedAt, Is.InRange(10, 10.7));
            Assert.That(ends, Is.EqualTo(1)); Assert.That(saves, Is.EqualTo(1));
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(oracle.State)));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
            if (combat) Assert.That(combatView.IsFeedbackPlaying, Is.False);
            else Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<FateChoiceUI>());
            yield return null; Assert.That(ends, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator DisablingControllerCancelsOnceAndDoesNotReplayCommittedRoll()
        {
            Prepare(false, 60); yield return Open();
            int ends = 0; Controller.Playback.Ended += _ => ends++;
            Click(((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button);
            var bytes = File.ReadAllBytes(store.Path);
            string committed = Stable(Controller.Session.State);
            yield return null;
            Controller.enabled = false;
            Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
            Assert.That(ends, Is.EqualTo(1)); Assert.That(Controller.Busy, Is.False);
            Assert.That(Controller.UI.Popups, Is.Empty);
            Controller.enabled = true;
            yield return null;
            Assert.That(ends, Is.EqualTo(1)); Assert.That(Controller.Busy, Is.False);
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(committed));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
        }

        [UnityTest] public IEnumerator PublicCancelClosesOwnedPopupAndRestoresCommittedPhase()
        {
            Prepare(false, 60); yield return Open();
            Click(((DiceRollUI)Controller.UI.Popups.Last()).rollButton.button);
            var bytes = File.ReadAllBytes(store.Path);
            string committed = Stable(Controller.Session.State);
            Controller.Playback.Cancel();
            yield return Until(() => !Controller.Busy);
            Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
            Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<FateChoiceUI>());
            CollectionAssert.AreEquivalent(Controller.Session.State.cards.Select(card => card.id),
                ((FateChoiceUI)Controller.UI.Popups.Single()).Cards.Select(card => card.OfferedId));
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(committed));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
        }

        [UnityTest] public IEnumerator OlderRollCannotCloseTheSamePopupAfterRebinding()
        {
            Prepare(false, 60); yield return Open();
            var popup = (DiceRollUI)Controller.UI.Popups.Last();
            Click(popup.rollButton.button);
            var bytes = File.ReadAllBytes(store.Path);
            Controller.UI.ShowPopup<DiceRollUI>(new DiceRollUIData { title = "새 바인딩 소유자", duration = 0 });
            yield return Until(() => !Controller.Busy);
            Assert.That(popup.IsOpen, Is.True, "An old iterator closed a newer binding on the pooled popup.");
            Assert.That(popup.title.text, Is.EqualTo("새 바인딩 소유자"));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
        }

        [UnityTest] public IEnumerator OlderCombatIteratorCannotOverwriteReboundHudOrNewPlayback()
        {
            Prepare(true, 0); yield return Open();
            var view = (CombatUI)Controller.UI.ActiveScreen;
            var data = new CombatFeedbackData { actionLabel = "이전 공격", grade = Grade.Common, attack = 12,
                enemyHpLost = 12, enemyHpAfter = 988, enemyMaxHp = 1000, playerHpAfter = 1, playerMaxHp = 90,
                retaliates = true, enemyActionLabel = "이전 반격", incoming = 8, playerHpLost = 8 };
            var old = view.PlayFeedback(data);
            IEnumerator impact = null, next = null;
            try
            {
                Assert.That(old.MoveNext(), Is.True);
                impact = (IEnumerator)old.Current; Assert.That(impact.MoveNext(), Is.True);
                Controller.RefreshView();
                string reboundHealth = view.layout.stats.text;
                Assert.That(impact.MoveNext(), Is.False);
                Assert.That(old.MoveNext(), Is.False, "The superseded attack continued into its old retaliation.");
                Assert.That(view.layout.stats.text, Is.EqualTo(reboundHealth));
                next = view.PlayFeedback(data); Assert.That(next.MoveNext(), Is.True);
                ((IDisposable)old).Dispose();
                Assert.That(view.IsFeedbackPlaying, Is.True, "Disposal of the old playback cancelled the new one.");
            }
            finally { (impact as IDisposable)?.Dispose(); (old as IDisposable)?.Dispose(); (next as IDisposable)?.Dispose(); }
        }

        void Prepare(bool combat, float roll)
        {
            var config = Asset<GameApplication>(AppPath).controller.config.Snapshot();
            config.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            foreach (var enemy in config.combat.enemies) enemy.maxHp = 1000;
            config.presentation.explorationDice.rollSeconds = roll;
            var run = RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Common);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            if (combat)
            {
                Assert.That(run.Roll(), Is.True);
                Assert.That(run.ChooseFate(run.State.cards[0].id), Is.True);
                Assert.That(run.Roll(), Is.True);
            }
            store.Save(run.State);
        }

        IEnumerator Open()
        {
            app = GameApplication.Bootstrap(Asset<GameApplication>(AppPath), store, new FixedSeedSource(33));
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/InGame.unity", LoadSceneMode.Single);
            yield return Until(() => !Controller.Busy && app.sceneFlow.CurrentRole == GameSceneRole.InGame);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        }

        void Click(Button button)
        {
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var point = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = point };
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var hit = hits.Select(item => ExecuteEvents.GetEventHandler<IPointerClickHandler>(item.gameObject)).FirstOrDefault(item => item != null);
            Assert.That(hit, Is.SameAs(button.gameObject));
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }
        static T Asset<T>(string path) where T : Object => UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        static object Read(object target, string name) => target.GetType().GetField(name)?.GetValue(target) ?? target.GetType().GetProperty(name)?.GetValue(target);
        static IEnumerator Until(Func<bool> condition, float seconds = 5)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + seconds;
            while (!condition()) { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline)); yield return null; }
        }
        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0;
            if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
    }
}
