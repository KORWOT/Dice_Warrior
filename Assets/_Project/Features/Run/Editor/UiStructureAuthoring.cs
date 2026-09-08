using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // Create missing full-screen assets. Existing authored layouts are never regenerated.
    public static class UiStructureAuthoring
    {
        public const string RootPath = "Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab";
        public const string ScreenFolder = "Assets/_Project/Features/Run/Prefabs/";
        [MenuItem("Fate Dice/Create UI structure assets (new only)")]
        public static string CreateAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Create UI assets outside Play Mode.");
            if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Save the current scene before authoring UI assets.");
            var config = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            if (!config) throw new InvalidOperationException("Existing Fate Dice config is required.");
            var style = config.Snapshot().presentation;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var prefabs = new BaseUI[]
            {
                CreateScreen<MenuUI>(font, style, view =>
                {
                    view.trialHeading = ContentText("TRIAL WILDCARD", view.layout.body, font, style, 40, "TRIAL WILDCARD");
                    view.trialChoices = Row("Wildcards", view.layout.body, 112);
                    view.capHeading = ContentText("Grade cap heading", view.layout.body, font, style, 42, "EXPLORATION GRADE CAP (combat is independent)");
                    view.capChoices = Row("Grade cap", view.layout.body, 112);
                    view.seedHeading = ContentText("Seed heading", view.layout.body, font, style, 40, "SEED (nonzero number, for repeatable runs)");
                    var seed = Rect("seed", view.layout.body); Height(seed, 64);
                    var background = seed.gameObject.AddComponent<Image>(); background.color = style.panel;
                    var label = Text("Value", seed, font, style.bodyFontSize, style.text); Fill(label.rectTransform);
                    label.rectTransform.offsetMin = new Vector2(16, 4); label.rectTransform.offsetMax = new Vector2(-16, -4);
                    view.seedInput = seed.gameObject.AddComponent<InputField>(); view.seedInput.textComponent = label;
                    view.seedInput.contentType = InputField.ContentType.IntegerNumber; view.seedInput.targetGraphic = background;
                    view.mainChoices = Column("Journey choices", view.layout.body);
                    view.error = ContentText("Save error", view.layout.body, font, style, 80);
                    view.lastResult = ContentText("Last result", view.layout.body, font, style, 100);
                }),
                CreateScreen<ExplorationUI>(font, style, view =>
                {
                    view.mapContainer = Column("Map", view.layout.body);
                    view.instructions = ContentText("Exploration instructions", view.layout.body, font, style, 100);
                    view.rollChoices = Column("Roll choices", view.layout.body);
                    view.fateChoices = Column("Fate choices", view.layout.body);
                }),
                CreateScreen<CombatUI>(font, style, view =>
                {
                    Artwork(view.layout.body, font, style, out view.artworkRoot, out view.artwork, out view.artworkFallback);
                    view.rollChoices = Column("Roll choices", view.layout.body);
                    view.actionChoices = Column("Action choices", view.layout.body);
                }),
                CreateScreen<EncounterUI>(font, style, view =>
                {
                    Artwork(view.layout.body, font, style, out view.artworkRoot, out view.artwork, out view.artworkFallback);
                    view.description = ContentText("Encounter description", view.layout.body, font, style, 110);
                    view.outcome = ContentText("Outcome", view.layout.body, font, style, 110);
                    view.choices = Column("Encounter and shop choices", view.layout.body);
                }),
                CreateScreen<RewardUI>(font, style, view =>
                {
                    Artwork(view.layout.body, font, style, out view.artworkRoot, out view.artwork, out view.artworkFallback);
                    view.description = ContentText("Reward description", view.layout.body, font, style, 140);
                    view.instructions = ContentText("Reward instructions", view.layout.body, font, style, 100);
                    view.choices = Column("Reward choices", view.layout.body);
                }),
                CreateScreen<EquipmentUI>(font, style, view =>
                {
                    Artwork(view.layout.body, font, style, out view.artworkRoot, out view.artwork, out view.artworkFallback);
                    view.details = ContentText("Found item", view.layout.body, font, style, 130);
                    view.current = ContentText("Current item", view.layout.body, font, style, 100);
                    view.choices = Column("Equipment choices", view.layout.body);
                }),
                CreateScreen<ResultUI>(font, style, view =>
                {
                    view.summary = ContentText("Journey result", view.layout.body, font, style, 100);
                    view.grades = ContentText("Chosen grades", view.layout.body, font, style, 110);
                    view.choices = Column("Result choices", view.layout.body);
                })
            };
            var root = AssetDatabase.LoadAssetAtPath<UIRoot>(RootPath);
            if (!root) root = CreateRoot(style, prefabs);
            var scene = EditorSceneManager.OpenScene(PrototypeAuthoring.ScenePath, OpenSceneMode.Single);
            var controller = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<FateDiceScreen>(true)).Single();
            controller.uiRootPrefab = root;
            EditorUtility.SetDirty(controller); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "UIRoot and seven complete screen prefabs connected to " + scene.path;
        }

        private static T CreateScreen<T>(Font font, PresentationSettings style, Action<T> author) where T : BaseUI
        {
            var path = ScreenFolder + typeof(T).Name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) return existing;
            var rect = Rect(typeof(T).Name, null); Fill(rect);
            try
            {
                var view = rect.gameObject.AddComponent<T>();
                view.group = rect.gameObject.AddComponent<CanvasGroup>();
                var layout = rect.gameObject.AddComponent<RunScreenLayout>();
                // All seven concrete screens inherit this serialized field.
                typeof(T).GetField("layout").SetValue(view, layout);
                var stack = rect.gameObject.AddComponent<VerticalLayoutGroup>();
                stack.padding = new RectOffset(20, 20, 16, 16); stack.spacing = 10;
                stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = true; stack.childForceExpandHeight = false;
                layout.header = FixedText("Header", rect, font, style.titleFontSize, style.text, 54);
                layout.stats = FixedText("Stats", rect, font, style.bodyFontSize, style.text, 100);
                layout.situation = FixedText("Situation", rect, font, style.bodyFontSize, style.text, 76);
                layout.diceRow = Row("Dice", rect, 104);
                layout.fate = FixedText("Fate", rect, font, style.bodyFontSize, style.text, 74);
                layout.notice = FixedText("Notice", rect, font, style.bodyFontSize - 3, style.accent, 72);
                var scrollRoot = Rect("Choices", rect);
                var flex = scrollRoot.gameObject.AddComponent<LayoutElement>(); flex.flexibleHeight = 1; flex.minHeight = 160;
                layout.scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); layout.scroll.horizontal = false; layout.scroll.vertical = true;
                var viewport = Rect("Viewport", scrollRoot); Fill(viewport); viewport.gameObject.AddComponent<Image>().color = style.background;
                viewport.gameObject.AddComponent<RectMask2D>();
                layout.body = Column("Content", viewport);
                layout.body.anchorMin = new Vector2(0, 1); layout.body.anchorMax = Vector2.one; layout.body.pivot = new Vector2(.5f, 1);
                layout.body.offsetMin = layout.body.offsetMax = Vector2.zero;
                layout.body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                layout.scroll.viewport = viewport; layout.scroll.content = layout.body; layout.scroll.movementType = ScrollRect.MovementType.Clamped;
                layout.footer = Row("Navigation", rect, 108);
                author(view);
                layout.gear = ContentText("Gear summary", layout.body, font, style, 145);
                Folder(path);
                return PrefabUtility.SaveAsPrefabAsset(rect.gameObject, path).GetComponent<T>();
            }
            finally { UnityEngine.Object.DestroyImmediate(rect.gameObject); }
        }

        private static UIRoot CreateRoot(PresentationSettings style, BaseUI[] prefabs)
        {
            var rect = Rect("UIRoot", null);
            try
            {
                var root = rect.gameObject.AddComponent<UIRoot>();
                root.canvas = rect.gameObject.AddComponent<Canvas>(); root.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = rect.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = style.referenceResolution; scaler.matchWidthOrHeight = .5f;
                rect.gameObject.AddComponent<GraphicRaycaster>();
                var background = rect.gameObject.AddComponent<Image>(); background.color = style.background;
                root.safeArea = Rect("Safe Area", rect); Fill(root.safeArea);
                root.inputGroup = root.safeArea.gameObject.AddComponent<CanvasGroup>();
                root.screenLayer = Rect("Screens", root.safeArea); Fill(root.screenLayer);
                root.popupLayer = Rect("Popups", root.safeArea); Fill(root.popupLayer);
                var blocker = Rect("Modal background blocker", root.popupLayer); Fill(blocker);
                root.popupBlocker = blocker.gameObject.AddComponent<Image>(); root.popupBlocker.color = new Color(0, 0, 0, .65f);
                root.popupBlocker.raycastTarget = true; blocker.gameObject.SetActive(false);
                root.overlayLayer = Rect("Overlay", root.safeArea); Fill(root.overlayLayer);
                root.cacheLayer = Rect("Cached screens", root.safeArea); Fill(root.cacheLayer); root.cacheLayer.gameObject.SetActive(false);
                var input = new GameObject("Input", typeof(EventSystem), typeof(InputSystemUIInputModule)); input.transform.SetParent(rect, false);
                input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                var manager = rect.gameObject.AddComponent<UIManager>(); manager.root = root; manager.prefabs = prefabs;
                Folder(RootPath);
                return PrefabUtility.SaveAsPrefabAsset(rect.gameObject, RootPath).GetComponent<UIRoot>();
            }
            finally { UnityEngine.Object.DestroyImmediate(rect.gameObject); }
        }
        private static void Artwork(Transform parent, Font font, PresentationSettings style, out RectTransform root, out Image image, out Text fallback)
        {
            root = Rect("Revealed event artwork", parent); Height(root, 100);
            var picture = Rect("Artwork", root); Fill(picture); picture.offsetMin = new Vector2(12, 4); picture.offsetMax = new Vector2(-12, -4);
            image = picture.gameObject.AddComponent<Image>(); image.raycastTarget = false; image.preserveAspect = true;
            fallback = Text("Artwork fallback", root, font, style.titleFontSize, style.accent); Fill(fallback.rectTransform); fallback.alignment = TextAnchor.MiddleCenter;
        }
        private static Text ContentText(string name, Transform parent, Font font, PresentationSettings style, float minHeight, string initial = "")
        {
            var text = Text(name, parent, font, style.bodyFontSize, style.text); text.text = initial;
            var element = text.gameObject.AddComponent<LayoutElement>(); element.minHeight = minHeight; element.flexibleWidth = 1;
            // Text supplies preferred height when it grows, so descriptions remain editable and scrollable.
            return text;
        }
        private static Text FixedText(string name, Transform parent, Font font, int size, Color color, float height)
        { var text = Text(name, parent, font, size, color); Height(text.rectTransform, height); return text; }
        private static Text Text(string name, Transform parent, Font font, int size, Color color)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.MiddleLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false; text.raycastTarget = false; return text;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); if (parent) rect.SetParent(parent, false); return rect; }
        private static RectTransform Row(string name, Transform parent, float height)
        {
            var row = Rect(name, parent); Height(row, height); var group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 8; group.childControlHeight = group.childControlWidth = group.childForceExpandWidth = group.childForceExpandHeight = true; return row;
        }
        private static RectTransform Column(string name, Transform parent)
        {
            var column = Rect(name, parent); var group = column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = 10; group.childControlHeight = group.childControlWidth = group.childForceExpandWidth = true; group.childForceExpandHeight = false; return column;
        }
        private static void Fill(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Height(RectTransform rect, float height)
        { var element = rect.gameObject.AddComponent<LayoutElement>(); element.minHeight = element.preferredHeight = height; element.flexibleHeight = 0; element.flexibleWidth = 1; }
        private static void Folder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
            for (var i = 1; i < parts.Length - 1; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }
    }
}
