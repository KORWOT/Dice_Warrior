using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FateDice.Tests
{
    public sealed class UiStructureTests
    {
        private const string ScreenFolder = "Assets/_Project/Features/Run/Prefabs/";
        private const string RootPath = "Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab";
        private static readonly string[] Names = { "MenuUI", "ExplorationUI", "CombatUI", "EncounterUI", "RewardUI", "EquipmentUI", "ResultUI" };

        [UnityTest] public IEnumerator ActualRootModalBlocksPointerCommandsAndKeepsTheGlobalLock()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Fate Dice 9:16");
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/FateDicePrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null; yield return null;
            var screen = Object.FindFirstObjectByType<FateDiceScreen>();
            var directory = Path.Combine(Path.GetTempPath(), "FateDiceStructureTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                screen.UseStore(new LocalRunStore(Path.Combine(directory, "run.json")));
                yield return null; Canvas.ForceUpdateCanvases();
                var scroll = screen.Widgets.Body.GetComponentInParent<ScrollRect>();
                scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                var start = screen.Widgets.Buttons["new"];
                var position = RectTransformUtility.WorldToScreenPoint(null, start.transform.TransformPoint(((RectTransform)start.transform).rect.center));
                Assert.That(TopHit(position).GetComponentInParent<Button>(), Is.SameAs(start));
                screen.UI.ShowPopup<ResultUI>(new ResultUIData
                {
                    context = new RunUIContext { manager = screen.UI, prefabs = screen.uiPrefabs, visuals = screen.visuals, presentation = screen.PreviewConfig.presentation },
                    hud = new RunHUDData { header = "TEST MODAL" }, summary = "This modal owns input.", grades = "",
                    restart = new UIChoiceData { key = "dismiss-probe", text = "Modal action", clicked = () => { } }
                });
                yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(start.IsInteractable(), Is.False);
                Assert.That(TopHit(position).GetComponentInParent<Button>(), Is.Not.SameAs(start));
                ClickTop(position);
                Assert.That(screen.Session, Is.Null); Assert.That(screen.Store.Exists, Is.False);
                screen.UI.SetInputLocked(true);
                Assert.That(screen.UI.CloseTopPopup(), Is.True);
                Assert.That(start.IsInteractable(), Is.False, "Closing modal cannot unlock an in-progress command.");
                ClickTop(position); Assert.That(screen.Session, Is.Null);
                screen.UI.SetInputLocked(false);
                Assert.That(start.IsInteractable(), Is.True);
                Assert.That(TopHit(position).GetComponentInParent<Button>(), Is.SameAs(start));
                ClickTop(position); yield return null;
                Assert.That(screen.Session.State.phase, Is.EqualTo(RunPhase.Map));
                Assert.That(screen.UI.Popups, Is.Empty);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
            LogAssert.NoUnexpectedReceived();
        }

        private static GameObject TopHit(Vector2 position)
        {
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.That(hits, Is.Not.Empty, "Actual EventSystem must provide a target.");
            return hits[0].gameObject;
        }
        private static void ClickTop(Vector2 position)
        {
            var target = TopHit(position);
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        [Test] public void SevenWholeScreensAndRootAreAuthoredAssets()
        {
#if UNITY_EDITOR
            var root = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RootPath);
            Assert.That(root, Is.Not.Null, "The Canvas/layers must be an authored prefab.");
            Assert.That(root.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<GraphicRaycaster>(true).Length, Is.EqualTo(1));
            var common = UnityEditor.AssetDatabase.LoadAssetAtPath<CommonButtonView>("Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab");
            var minimumButtonHeight = common.GetComponent<LayoutElement>().minHeight;
            foreach (var name in Names)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ScreenFolder + name + ".prefab");
                Assert.That(prefab, Is.Not.Null, name + " must have a complete editable prefab.");
                Assert.That(prefab.GetComponent(name), Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<ScrollRect>(true).Length, Is.EqualTo(1));
                Assert.That(prefab.GetComponentsInChildren<Text>(true).Length, Is.GreaterThanOrEqualTo(6));
                Assert.That(prefab.GetComponentsInChildren<Canvas>(true), Is.Empty, "One shared canvas owns screen ordering.");
                var layout = prefab.GetComponentInChildren<RunScreenLayout>(true);
                Assert.That(layout, Is.Not.Null);
                RectTransform[] choices = prefab.GetComponent(name) switch
                {
                    MenuUI view => new[] { view.trialChoices, view.capChoices, view.mainChoices },
                    ExplorationUI view => new[] { view.rollChoices, view.fateChoices },
                    CombatUI view => new[] { view.rollChoices, view.actionChoices },
                    EncounterUI view => new[] { view.choices },
                    RewardUI view => new[] { view.choices },
                    EquipmentUI view => new[] { view.choices },
                    ResultUI view => new[] { view.choices },
                    _ => throw new System.InvalidOperationException("Missing authored choice contract for " + name)
                };
                foreach (var container in choices.Concat(new[] { layout.diceRow, layout.footer }).Distinct())
                {
                    Assert.That(container, Is.Not.Null, name + " must reference its authored choice containers.");
                    if (!container.GetComponent<HorizontalLayoutGroup>()) continue;
                    var element = container.GetComponent<LayoutElement>();
                    Assert.That(element, Is.Not.Null, name + "/" + container.name);
                    Assert.That(element.minHeight, Is.GreaterThanOrEqualTo(minimumButtonHeight),
                        name + "/" + container.name + " must contain the common prefab's minimum touch height.");
                }
                Assert.That(prefab.GetComponentsInChildren<Transform>(true).Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)), Is.Zero);
            }
            var menu = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ScreenFolder + "MenuUI.prefab");
            Assert.That(menu.GetComponentInChildren<InputField>(true), Is.Not.Null, "Seed input is authored, not constructed by runtime code.");
#endif
        }

        [UnityTest] public IEnumerator EditedWholeScreenLayoutAppearsInActualSceneAndCachedReentry()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ScreenFolder + "MenuUI.prefab");
            Assert.That(prefab, Is.Not.Null);
            var savedBytes = File.ReadAllBytes(ScreenFolder + "MenuUI.prefab");
            // Use the prefab API and restore in finally, never hand-edit serialized YAML.
            var contents = UnityEditor.PrefabUtility.LoadPrefabContents(ScreenFolder + "MenuUI.prefab");
            var layout = contents.GetComponent<MenuUI>().layout.GetComponent<VerticalLayoutGroup>();
            Assert.That(layout, Is.Not.Null);
            var originalPadding = new RectOffset(layout.padding.left, layout.padding.right, layout.padding.top, layout.padding.bottom);
            var heading = contents.GetComponent<MenuUI>().trialHeading;
            var originalText = heading.text;
            var originalStyle = heading.fontStyle;
            UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
            string directory = null;
            try
            {
                contents = UnityEditor.PrefabUtility.LoadPrefabContents(ScreenFolder + "MenuUI.prefab");
                layout = contents.GetComponent<MenuUI>().layout.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(44, 28, originalPadding.top, originalPadding.bottom);
                heading = contents.GetComponent<MenuUI>().trialHeading;
                heading.text = "AUTHORED TRIAL CHOICE"; heading.fontStyle = FontStyle.Italic;
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, ScreenFolder + "MenuUI.prefab");
                UnityEditor.PrefabUtility.UnloadPrefabContents(contents); contents = null;
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Fate Dice 9:16");
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/FateDicePrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null; yield return null;
                var screen = Object.FindFirstObjectByType<FateDiceScreen>();
                directory = Path.Combine(Path.GetTempPath(), "FateDiceStructureTests", System.Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                screen.UseStore(new LocalRunStore(Path.Combine(directory, "run.json")));
                yield return null;
                var live = (MenuUI)screen.UI.ActiveScreen;
                Assert.That(live.layout.GetComponent<VerticalLayoutGroup>().padding.left, Is.EqualTo(44));
                live.settingsTab.onClick.Invoke();
                yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(live.settingsPanel.gameObject.activeInHierarchy, Is.True);
                var liveHeading = live.trialHeading;
                Assert.That(liveHeading.gameObject.activeInHierarchy, Is.True, "Open Settings before checking the visible authored heading.");
                Assert.That(liveHeading.text, Is.EqualTo("AUTHORED TRIAL CHOICE"));
                Assert.That(liveHeading.fontStyle, Is.EqualTo(FontStyle.Italic));
                var menuInstance = live.gameObject;
                screen.Widgets.Buttons["new"].onClick.Invoke();
                yield return null;
                Assert.That(screen.Session.State.phase, Is.EqualTo(RunPhase.Map));
                var before = JsonUtility.ToJson(screen.Session.State);
                screen.Widgets.Buttons["menu"].onClick.Invoke();
                yield return null;
                var reopened = (MenuUI)screen.UI.ActiveScreen;
                Assert.That(reopened.gameObject, Is.SameAs(menuInstance), "Screen is cached, not recreated.");
                Assert.That(reopened.layout.GetComponent<VerticalLayoutGroup>().padding.left, Is.EqualTo(44));
                reopened.settingsTab.onClick.Invoke();
                yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(reopened.trialHeading.gameObject.activeInHierarchy, Is.True);
                Assert.That(reopened.trialHeading.text, Is.EqualTo("AUTHORED TRIAL CHOICE"));
                Assert.That(reopened.trialHeading.fontStyle, Is.EqualTo(FontStyle.Italic));
                Assert.That(screen.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(screen.GetComponentsInChildren<EventSystem>(true).Length, Is.EqualTo(1));
                var stored = screen.Store.Load();
                var previous = JsonUtility.FromJson<RunState>(before);
                stored.playedSeconds = previous.playedSeconds = 0;
                Assert.That(JsonUtility.ToJson(stored), Is.EqualTo(JsonUtility.ToJson(previous)), "Closing the view may not grant rewards or change the run.");
            }
            finally
            {
                if (contents) UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
                contents = UnityEditor.PrefabUtility.LoadPrefabContents(ScreenFolder + "MenuUI.prefab");
                contents.GetComponent<MenuUI>().layout.GetComponent<VerticalLayoutGroup>().padding = originalPadding;
                heading = contents.GetComponent<MenuUI>().trialHeading;
                heading.text = originalText; heading.fontStyle = originalStyle;
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, ScreenFolder + "MenuUI.prefab");
                UnityEditor.PrefabUtility.UnloadPrefabContents(contents);
                Assert.That(File.ReadAllBytes(ScreenFolder + "MenuUI.prefab"), Is.EqualTo(savedBytes), "Prefab edit probe restores the exact original asset.");
                if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
            }
#else
            yield return null;
#endif
        }
    }
}
