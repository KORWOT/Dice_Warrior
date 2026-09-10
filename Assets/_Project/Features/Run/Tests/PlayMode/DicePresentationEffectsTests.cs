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
using Object = UnityEngine.Object;

namespace FateDice.Tests
{
    // New presentation types are observed through reflection so the initial missing-feature
    // test compiles before their implementation or any new test assembly references exist.
    public sealed class DicePresentationEffectsTests
    {
        const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        const string PopupPath = "Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab";
        const string InGamePath = "Assets/_Project/Scenes/InGame.unity";
        GameApplication app;
        LocalRunStore store;
        string directory, defaultSave;
        bool defaultExisted;
        byte[] defaultBytes;
        RunUIController Controller => app.controller;

        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null, "A previous fixture leaked its application.");
            directory = Path.Combine(Path.GetTempPath(), "FateDicePresentationEffectsTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            store = new LocalRunStore(Path.Combine(directory, "run.json"));
            defaultSave = Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json");
            defaultExisted = File.Exists(defaultSave);
            defaultBytes = defaultExisted ? File.ReadAllBytes(defaultSave) : null;
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
                Assert.That(File.Exists(defaultSave), Is.EqualTo(defaultExisted), "The test changed the user's save existence.");
                if (defaultExisted) CollectionAssert.AreEqual(defaultBytes, File.ReadAllBytes(defaultSave));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDicePresentationEffectsTests")) + Path.DirectorySeparatorChar;
                if (!string.IsNullOrEmpty(directory) && Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test] public void AuthoredPopupConnectsComboTextAnimationFeedbackAndShader()
        {
            var popup = Asset<DiceRollUI>(PopupPath);
            var feedback = Feedback(popup);
            var label = Read(feedback, "comboName") as Graphic;
            Assert.That(label, Is.Not.Null, "The authored combo name needs a TMP Graphic.");
            Assert.That(IsType(label.GetType(), "TMP_Text"), Is.True);
            Assert.That(Read(label, "font"), Is.Not.Null, "The combo name requires an authored Korean font.");
            Assert.That(label.raycastTarget, Is.False);
            var animator = Read(feedback, "textAnimator") as Component;
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.GetType().Name, Is.EqualTo("TextAnimator_TMP"));
            Assert.That(animator.gameObject, Is.SameAs(label.gameObject));
            var reveal = Read(feedback, "reveal") as Component;
            Assert.That(reveal, Is.Not.Null);
            Assert.That(reveal.GetType().Name, Is.EqualTo("MMF_Player"));
            var ribbon = Read(feedback, "ribbon") as Image;
            Assert.That(ribbon, Is.Not.Null);
            Assert.That(ribbon.raycastTarget, Is.False);
            Assert.That(ribbon.material, Is.Not.Null);
            Assert.That(ribbon.material.shader.name.Replace(" ", ""), Does.Contain("AllIn1"));
            var catalog = Read(feedback, "catalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (HandKind hand in Enum.GetValues(typeof(HandKind)))
            {
                var style = Invoke(catalog, "Resolve", hand);
                Assert.That(Read(style, "hand"), Is.EqualTo(hand), "A hand resolved to another hand's style.");
                Assert.That(((Color)Read(style, "color")).a, Is.GreaterThan(0));
            }
            Assert.That(popup.dice.Length, Is.EqualTo(6));
            Assert.That(popup.dice.Distinct().Count(), Is.EqualTo(6));
        }

        [UnityTest] public IEnumerator Portrait1280KeepsAllSixDiceInOneRowThroughBothRollPhases() => BothPhases(1280);
        [UnityTest] public IEnumerator Portrait1600KeepsAllSixDiceInOneRowThroughBothRollPhases() => BothPhases(1600);

        IEnumerator BothPhases(int height)
        {
            foreach (bool combat in new[] { false, true })
            {
                Prepare(combat, .12f, .6f);
                yield return Open(height);
                var popup = Popup();
                AssertOneRow(popup);
                var oracle = new RunSession(Controller.Session.State);
                Assert.That(oracle.Roll(), Is.True);
                int saves = 0;
                var checkpoint = Controller.Session.Checkpoint;
                Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
                var roll = popup.rollButton.button;
                float began = Time.unscaledTime;
                Click(roll);
                AssertOracle(oracle.State);
                Assert.That(saves, Is.EqualTo(1), "The command must commit before its first effect frame.");
                var bytes = File.ReadAllBytes(store.Path);
                roll.onClick.Invoke();
                yield return Until(() => !popup.IsRolling, "The committed faces were not revealed.");
                float revealed = Time.unscaledTime;
                Assert.That(revealed - began, Is.GreaterThanOrEqualTo(.06f));
                Assert.That(popup.IsOpen && Controller.Busy, Is.True);
                CollectionAssert.AreEqual(oracle.State.dice, popup.dice.Select(die => die.Value));
                AssertOneRow(popup);
                AssertCombo(popup, oracle.State);
                Assert.That(popup.result.text, Is.EqualTo(KoreanText.HandSummary(oracle.State, true)));
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(popup.IsOpen && Controller.Busy, Is.True, "Effects must retain the configured result hold.");
                AssertOracle(oracle.State);
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
                yield return Until(() => !Controller.Busy, "Effects retained the input lock after the configured hold.");
                float strength = oracle.State.config.dice.hands.Count(x => x.priority < oracle.State.config.dice.hands.Single(h => h.kind == oracle.State.hand).priority) /
                    (float)(oracle.State.config.dice.hands.Length - 1);
                float expectedDuration = popup.resultFeedback.catalog.ResolveTimeline(strength).Duration(.6f);
                Assert.That(Time.unscaledTime - revealed, Is.InRange(expectedDuration - .06f, expectedDuration + .35f));
                Assert.That(Controller.Playback.State, Is.EqualTo(PresentationPlaybackState.Completed));
                Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(false));
                Assert.That(saves, Is.EqualTo(1));
                AssertOracle(oracle.State);
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
                if (combat) Assert.That(Controller.UI.Popups, Is.Empty);
                else Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<FateChoiceUI>());
                Object.Destroy(app.gameObject); app = null;
                yield return null;
                Assert.That(GameApplication.Current, Is.Null);
            }
        }

        [UnityTest] public IEnumerator SavedSparsePriorityAndCustomNameDriveTheDisplayedComboWithoutChangingRules()
        {
            Prepare(false, .05f, .7f, true);
            yield return Open(1280);
            var oracle = new RunSession(Controller.Session.State);
            Assert.That(oracle.Roll(), Is.True);
            var popup = Popup();
            Click(popup.rollButton.button);
            var bytes = File.ReadAllBytes(store.Path);
            yield return Until(() => !popup.IsRolling, "The custom hand was not revealed.");
            var expected = oracle.State;
            int index = Array.FindIndex(expected.config.dice.hands, hand => hand.kind == expected.hand);
            var data = typeof(BaseUI<DiceRollUIData>).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
            Assert.That(Read(data, "comboName"), Is.EqualTo("검증 조합 " + index));
            Assert.That(Read(data, "hand"), Is.EqualTo(expected.hand));
            // Priorities are 1000, 977, 954, ...: index 0 is strongest, independent of enum values.
            Assert.That((float)Read(data, "comboStrength"), Is.EqualTo(1f - index / (float)(expected.config.dice.hands.Length - 1)).Within(.00001f));
            Assert.That((float)Read(data, "holdSeconds"), Is.EqualTo(.7f));
            AssertCombo(popup, expected);
            AssertOracle(expected);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
        }

        [UnityTest] public IEnumerator ClosingDuringRollingRestoresTheCapturedDiePoseExactly() => CloseEffect(false);
        [UnityTest] public IEnumerator ClosingDuringRevealRestoresPoseColorAndStopsEffects() => CloseEffect(true);

        IEnumerator CloseEffect(bool afterReveal)
        {
            Prepare(false, .2f, .8f);
            yield return Open(1280);
            var popup = Popup();
            Controller.UI.CloseTopPopup();
            var first = popup.dice[0].rectTransform;
            first.localPosition += new Vector3(.125f, -.125f, 0);
            first.localRotation = Quaternion.Euler(0, 0, 9);
            first.localScale = new Vector3(1.04f, .98f, 1);
            var pose = new Pose(popup);
            Controller.UI.ShowPopup<DiceRollUI>(Presentation(true));
            if (afterReveal)
            {
                popup.CompleteRoll();
                yield return null;
                Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(true), "The test must interrupt a live result effect.");
            }
            else
            {
                yield return null;
                Assert.That(popup.IsRolling, Is.True);
                Assert.That(first.localRotation, Is.Not.EqualTo(pose.rotations[0]), "The test must interrupt a displaced die.");
            }
            Controller.UI.CloseTopPopup();
            pose.AssertRestored();
            Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(false));
            yield return new WaitForSecondsRealtime(.12f);
            pose.AssertRestored();
            Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(false), "A stopped vendor player resumed after close.");
        }

        [UnityTest] public IEnumerator RebindingDuringRevealClearsThePreviousNameAndRestoresItsPose()
        {
            Prepare(false, .2f, .8f);
            yield return Open(1280);
            var popup = Popup();
            var pose = new Pose(popup);
            Controller.UI.ShowPopup<DiceRollUI>(Presentation(true));
            popup.CompleteRoll();
            yield return null;
            Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(true));
            var waiting = new DiceRollUIData { title = "다음 굴림", detail = "", result = "", duration = 0, rolling = false };
            Write(waiting, "comboName", ""); Write(waiting, "holdSeconds", 0f);
            Controller.UI.ShowPopup<DiceRollUI>(waiting);
            pose.AssertRestored();
            Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(false));
            yield return new WaitForSecondsRealtime(.12f);
            pose.AssertRestored();
            Assert.That(ParsedName(popup), Is.Empty, "A previous text animation wrote its result into the next binding.");
        }

        [UnityTest] public IEnumerator ZeroRollAndHoldLeaveNoVendorPlaybackOrExtraInputDelay()
        {
            Prepare(false, 0, 0);
            yield return Open(1280);
            var popup = Popup();
            var oracle = new RunSession(Controller.Session.State);
            Assert.That(oracle.Roll(), Is.True);
            int saves = 0;
            var checkpoint = Controller.Session.Checkpoint;
            Controller.Session.Checkpoint = next => { saves++; checkpoint(next); };
            Click(popup.rollButton.button);
            var bytes = File.ReadAllBytes(store.Path);
            yield return null; yield return null;
            Assert.That(Controller.Busy, Is.False);
            Assert.That(Controller.UI.Popups.Single(), Is.TypeOf<FateChoiceUI>());
            Assert.That(Read(Feedback(popup), "IsPlaying"), Is.EqualTo(false));
            Assert.That(saves, Is.EqualTo(1));
            AssertOracle(oracle.State);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(store.Path));
        }

        [UnityTest] public IEnumerator LongResultHoldSettlesTheEntranceWhileKeepingNameAndColor()
        {
            Prepare(false, .1f, 2f);
            yield return Open(1280);
            var popup = Popup();
            Click(popup.rollButton.button);
            yield return Until(() => !popup.IsRolling, "The committed faces were not revealed.");
            var expected = Controller.Session.State;
            var feedback = Feedback(popup);
            Assert.That(Read(feedback, "IsPlaying"), Is.EqualTo(true));
            yield return Until(() => popup.resultFeedback.Phase == DiceResultFeedback.ResultPhase.Reading,
                "The authored motion did not enter its reading phase.");
            Assert.That(popup.IsOpen && Controller.Busy, Is.True);
            Assert.That(Read(feedback, "IsPlaying"), Is.EqualTo(false), "The entrance must settle before a long result hold ends.");
            var scale = feedback.transform.localScale;
            var label = Read(feedback, "comboName");
            var mesh = (Mesh)Read(label, "mesh");
            var vertices = mesh.vertices;
            AssertCombo(popup, expected);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(popup.IsOpen && Controller.Busy, Is.True);
            Assert.That(feedback.transform.localScale, Is.EqualTo(scale));
            CollectionAssert.AreEqual(vertices, ((Mesh)Read(label, "mesh")).vertices, "Glyph motion continued during the settled hold.");
            AssertCombo(popup, expected);
        }

        void Prepare(bool combat, float roll, float hold, bool custom = false)
        {
            var config = Asset<GameApplication>(AppPath).controller.config.Snapshot();
            config.fate.nodeWeights = new float[] { 1, 0, 0, 0, 0 };
            config.presentation.explorationDice = new RollPresentationSettings { rollSeconds = roll, resultHoldSeconds = hold };
            config.presentation.combatDice = new RollPresentationSettings { rollSeconds = roll, resultHoldSeconds = hold };
            if (custom)
                for (int i = 0; i < config.dice.hands.Length; i++)
                { config.dice.hands[i].priority = 1000 - i * 23; config.dice.hands[i].label = "검증 조합 " + i; }
            var run = RunSession.New(config, 33, config.combat.trialActionIds[0], Grade.Legendary);
            Assert.That(run.ChooseNode(run.State.availableNodeIds[0]), Is.True);
            if (combat)
            {
                Assert.That(run.Roll(), Is.True);
                Assert.That(run.ChooseFate(run.State.cards.First(card => card.type == NodeType.Combat).id), Is.True);
            }
            store.Save(run.State);
        }

        IEnumerator Open(int height)
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, (uint)height, "Fate Dice presentation effects");
#endif
            app = GameApplication.Bootstrap(Asset<GameApplication>(AppPath), store, new FixedSeedSource(33));
            yield return SceneManager.LoadSceneAsync(InGamePath, LoadSceneMode.Single);
            yield return Until(() => !Controller.Busy && app.sceneFlow.CurrentRole.ToString() == "InGame", "The production scene did not settle.");
            yield return null; Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(Screen.width, Is.EqualTo(720)); Assert.That(Screen.height, Is.EqualTo(height));
        }

        static T Asset<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            var value = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(value, Is.Not.Null, "Missing production asset: " + path);
            return value;
#else
            throw new InvalidOperationException("These production prefab tests require the Editor.");
#endif
        }

        DiceRollUI Popup()
        {
            Assert.That(Controller.UI.Popups.LastOrDefault(), Is.TypeOf<DiceRollUI>());
            return (DiceRollUI)Controller.UI.Popups.Last();
        }

        static Component Feedback(DiceRollUI popup)
        {
            var value = Read(popup, "resultFeedback") as Component;
            Assert.That(value, Is.Not.Null, "DiceRollUI needs its authored resultFeedback.");
            return value;
        }

        static object Read(object target, string name)
        {
            Assert.That(target, Is.Not.Null, "Cannot inspect missing owner for " + name);
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(target);
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, type.Name + " is missing public " + name);
            return property.GetValue(target);
        }

        static void Write(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, target.GetType().Name + " is missing public " + name);
            field.SetValue(target, value);
        }

        static object Invoke(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, args.Select(arg => arg.GetType()).ToArray());
            Assert.That(method, Is.Not.Null, target.GetType().Name + " is missing " + name);
            return method.Invoke(target, args);
        }

        static bool IsType(Type type, string name) => type != null && (type.Name == name || IsType(type.BaseType, name));
        static string ParsedName(DiceRollUI popup) => (string)Invoke(Read(Feedback(popup), "comboName"), "GetParsedText");

        static DiceRollUIData Presentation(bool rolling)
        {
            var data = new DiceRollUIData { title = "복원 검사", detail = "", result = "복원 조합", values = new[] { 1, 1, 2, 3, 4, 5 }, rolling = rolling, duration = .5f };
            Write(data, "comboName", "복원 조합"); Write(data, "hand", (HandKind)Enum.GetValues(typeof(HandKind)).GetValue(0));
            Write(data, "comboStrength", .5f); Write(data, "holdSeconds", .8f);
            return data;
        }

        static void AssertCombo(DiceRollUI popup, RunState expected)
        {
            var feedback = Feedback(popup);
            var label = (Graphic)Read(feedback, "comboName");
            Invoke(label, "ForceMeshUpdate", false, false);
            Assert.That(ParsedName(popup), Is.EqualTo(KoreanText.Content(expected.config.dice.hands.Single(hand => hand.kind == expected.hand).label)));
            var style = Invoke(Read(feedback, "catalog"), "Resolve", expected.hand);
            Assert.That(label.color, Is.EqualTo((Color)Read(style, "color")), "The visible combo name does not use its hand's authored color.");
            Assert.That((float)Read(label, "preferredHeight"), Is.LessThanOrEqualTo(label.rectTransform.rect.height + 2));
            Assert.That(Contains(Screen.safeArea, ScreenRect(label.rectTransform)), Is.True);
            var mesh = (Mesh)Read(label, "mesh");
            Assert.That(mesh.vertexCount, Is.GreaterThan(0), "The Korean combo name has no rendered glyphs.");
        }

        static void AssertOneRow(DiceRollUI popup)
        {
            var rectangles = popup.dice.Select(die => ScreenRect(die.rectTransform)).ToArray();
            var row = ScreenRect((RectTransform)popup.dice[0].transform.parent);
            for (int i = 0; i < 6; i++)
            {
                Assert.That(rectangles[i].width, Is.GreaterThanOrEqualTo(48));
                Assert.That(rectangles[i].height, Is.GreaterThanOrEqualTo(48));
                Assert.That(rectangles[i].center.y, Is.EqualTo(rectangles[0].center.y).Within(.5f), "All six dice must occupy one horizontal row.");
                Assert.That(Contains(Screen.safeArea, rectangles[i]) && Contains(row, rectangles[i]), Is.True, "A die extends beyond its authored row or safe area.");
                if (i > 0) Assert.That(rectangles[i].xMin, Is.GreaterThanOrEqualTo(rectangles[i - 1].xMax - .5f), "Neighboring dice overlap or are out of order.");
            }
        }

        static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var points = corners.Select(point => RectTransformUtility.WorldToScreenPoint(null, point)).ToArray();
            return Rect.MinMaxRect(points.Min(point => point.x), points.Min(point => point.y), points.Max(point => point.x), points.Max(point => point.y));
        }
        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.xMax <= outer.xMax + 1 && inner.yMin >= outer.yMin - 1 && inner.yMax <= outer.yMax + 1;

        void Click(Button button)
        {
            Assert.That(Controller.Busy, Is.False);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = ScreenRect((RectTransform)button.transform).center };
            var hits = new List<RaycastResult>();
            Controller.UI.Root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            var hit = hits.Select(item => ExecuteEvents.GetEventHandler<IPointerClickHandler>(item.gameObject)).FirstOrDefault(item => item != null);
            Assert.That(hit, Is.SameAs(button.gameObject), "The actual pointer cannot reach the roll command.");
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        static IEnumerator Until(Func<bool> condition, string reason)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!condition()) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), reason); yield return null; }
        }
        static string Stable(RunState state)
        {
            var copy = JsonUtility.FromJson<RunState>(JsonUtility.ToJson(state));
            copy.playedSeconds = 0; if (copy.lastResult != null) copy.lastResult.playedSeconds = 0;
            return JsonUtility.ToJson(copy);
        }
        void AssertOracle(RunState expected)
        {
            Assert.That(Stable(Controller.Session.State), Is.EqualTo(Stable(expected)), "Effects changed the committed rules or RNG.");
            Assert.That(Stable(store.Load()), Is.EqualTo(Stable(expected)), "The disk save differs from the single command oracle.");
        }

        sealed class Pose
        {
            readonly Transform[] transforms;
            readonly Vector3[] positions, scales;
            public readonly Quaternion[] rotations;
            readonly Graphic[] graphics;
            readonly Color[] colors;
            public Pose(DiceRollUI popup)
            {
                var feedback = Feedback(popup);
                graphics = popup.dice.Cast<Graphic>().Concat(new[] { (Graphic)Read(feedback, "comboName"), (Graphic)Read(feedback, "ribbon") }).ToArray();
                transforms = popup.dice.Select(die => die.transform).Concat(new[] { feedback.transform, ((Component)Read(feedback, "comboName")).transform, ((Component)Read(feedback, "ribbon")).transform }).Distinct().ToArray();
                positions = transforms.Select(item => item.localPosition).ToArray();
                rotations = transforms.Select(item => item.localRotation).ToArray();
                scales = transforms.Select(item => item.localScale).ToArray();
                colors = graphics.Select(item => item.color).ToArray();
            }
            public void AssertRestored()
            {
                for (int i = 0; i < transforms.Length; i++)
                {
                    Assert.That(transforms[i].localPosition, Is.EqualTo(positions[i]), transforms[i].name + " local position was not restored exactly.");
                    Assert.That(transforms[i].localRotation, Is.EqualTo(rotations[i]), transforms[i].name + " rotation was not restored exactly.");
                    Assert.That(transforms[i].localScale, Is.EqualTo(scales[i]), transforms[i].name + " scale was not restored exactly.");
                }
                for (int i = 0; i < graphics.Length; i++) Assert.That(graphics[i].color, Is.EqualTo(colors[i]), graphics[i].name + " color was not restored.");
            }
        }
    }
}
