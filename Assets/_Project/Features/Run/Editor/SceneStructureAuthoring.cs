using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    // Creates missing production assets; existing layouts and configuration are preserved.
    public static class SceneStructureAuthoring
    {
        public const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
        public const string LobbyScenePath = "Assets/_Project/Scenes/Lobby.unity";
        public const string InGameScenePath = "Assets/_Project/Scenes/InGame.unity";
        public const string ApplicationPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        public const string TitleUIPath = "Assets/_Project/Features/Run/Prefabs/TitleUI.prefab";

        [MenuItem("Fate Dice/Create scene structure assets (new only)")]
        public static string CreateAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create scene structure assets outside Play Mode.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save all open scenes before creating scene structure assets.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Mode before creating scene structure assets.");

            var source = ReadPrototypeReferences();
            var previous = SceneManager.GetActiveScene();
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(staging);
            GameApplication app;
            try
            {
                var title = CreateTitle(source);
                RegisterTitle(source.root, title);
                app = CreateApplication(source);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (staging.IsValid() && staging.isLoaded) EditorSceneManager.CloseScene(staging, true);
            }
            // Unity cannot add another untitled scene while the asset-authoring scene is open.
            CreateScene(TitleScenePath, GameSceneRole.Title, app, source.style.background);
            CreateScene(LobbyScenePath, GameSceneRole.Lobby, app, source.style.background);
            CreateScene(InGameScenePath, GameSceneRole.InGame, app, source.style.background);
            SetBuildScenes();
            return "Production scene structure ready: Title -> Lobby <-> InGame. Existing assets and layouts retained.";
        }

        private sealed class PrototypeReferences
        {
            public FateDiceConfig config;
            public Font font;
            public FateDiceVisualCatalog visuals;
            public UiPrefabReferences prefabs;
            public UIRoot root;
            public PresentationSettings style;
        }

        private static PrototypeReferences ReadPrototypeReferences()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeAuthoring.ScenePath))
                throw new InvalidOperationException("The existing FateDicePrototype scene is required.");
            var preview = EditorSceneManager.OpenPreviewScene(PrototypeAuthoring.ScenePath);
            try
            {
                var controllers = preview.GetRootGameObjects()
                    .SelectMany(x => x.GetComponentsInChildren<RunUIController>(true)).ToArray();
                if (controllers.Length != 1)
                    throw new InvalidOperationException("FateDicePrototype must contain exactly one RunUIController.");
                var source = controllers[0];
                if (!source.config || !source.uiFont || !source.visuals || !source.uiRootPrefab || source.uiPrefabs == null)
                    throw new InvalidOperationException("FateDicePrototype requires its authored config, font, visuals, reusable prefabs and UI root.");
                source.uiPrefabs.Validate();
                if (AssetDatabase.GetAssetPath(source.uiRootPrefab) != UiStructureAuthoring.RootPath)
                    throw new InvalidOperationException("FateDicePrototype must reference the approved UIRoot prefab path.");
                var manager = source.uiRootPrefab.GetComponent<UIManager>();
                if (!manager || manager.prefabs == null || manager.prefabs.Any(x => !x))
                    throw new InvalidOperationException("The existing UIRoot requires UIManager and a complete screen registry.");
                return new PrototypeReferences
                {
                    config = source.config, font = source.uiFont, visuals = source.visuals,
                    root = source.uiRootPrefab, style = source.config.Snapshot().presentation,
                    prefabs = new UiPrefabReferences
                    {
                        commonButton = source.uiPrefabs.commonButton,
                        explorationNode = source.uiPrefabs.explorationNode,
                        actionCard = source.uiPrefabs.actionCard,
                        fateCard = source.uiPrefabs.fateCard
                    }
                };
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static TitleUI CreateTitle(PrototypeReferences source)
        {
            var existing = ExistingPrefab<TitleUI>(TitleUIPath);
            if (existing) return existing;
            var rect = Rect("TitleUI", null);
            Fill(rect);
            try
            {
                var view = rect.gameObject.AddComponent<TitleUI>();
                view.group = rect.gameObject.AddComponent<CanvasGroup>();
                var stack = rect.gameObject.AddComponent<VerticalLayoutGroup>();
                stack.padding = new RectOffset(40, 40, 64, 64);
                stack.spacing = 24;
                stack.childAlignment = TextAnchor.MiddleCenter;
                stack.childControlWidth = stack.childControlHeight = true;
                stack.childForceExpandWidth = true;
                stack.childForceExpandHeight = false;
                Spacer("Top space", rect);
                view.title = Text("Title", rect, source.font, source.style.titleFontSize,
                    source.style.text, 110, "FATE DICE");
                view.title.fontStyle = FontStyle.Bold;
                view.subtitle = Text("Subtitle", rect, source.font, source.style.bodyFontSize,
                    source.style.text, 120, "Choose your path. Shape your fate.");
                view.status = Text("Status", rect, source.font, source.style.bodyFontSize,
                    source.style.accent, 90, "Ready");
                var buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(source.prefabs.commonButton.gameObject, rect);
                buttonObject.name = "Enter Lobby";
                view.enterButton = buttonObject.GetComponent<CommonButtonView>();
                var buttonLayout = buttonObject.GetComponent<LayoutElement>();
                if (!buttonLayout) buttonLayout = buttonObject.AddComponent<LayoutElement>();
                buttonLayout.minHeight = Mathf.Max(102, buttonLayout.minHeight);
                buttonLayout.preferredHeight = Mathf.Max(buttonLayout.minHeight, buttonLayout.preferredHeight);
                buttonLayout.flexibleHeight = 0;
                if (view.enterButton.label) view.enterButton.label.text = "ENTER LOBBY";
                Spacer("Bottom space", rect);
                EnsureFolder(TitleUIPath);
                var saved = PrefabUtility.SaveAsPrefabAsset(rect.gameObject, TitleUIPath);
                if (!saved) throw new InvalidOperationException("Could not save " + TitleUIPath);
                return saved.GetComponent<TitleUI>();
            }
            finally { UnityEngine.Object.DestroyImmediate(rect.gameObject); }
        }

        private static void RegisterTitle(UIRoot root, TitleUI title)
        {
            var current = root.GetComponent<UIManager>().prefabs;
            var matches = current.OfType<TitleUI>().ToArray();
            if (matches.Length > 1 || (matches.Length == 1 && matches[0] != title))
                throw new InvalidOperationException("UIRoot already has a conflicting TitleUI registration.");
            if (matches.Length == 1) return;
            var contents = PrefabUtility.LoadPrefabContents(UiStructureAuthoring.RootPath);
            try
            {
                var manager = contents.GetComponent<UIManager>();
                manager.prefabs = manager.prefabs.Concat(new BaseUI[] { title }).ToArray();
                if (!PrefabUtility.SaveAsPrefabAsset(contents, UiStructureAuthoring.RootPath))
                    throw new InvalidOperationException("Could not save the TitleUI registry entry.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static GameApplication CreateApplication(PrototypeReferences source)
        {
            var existing = ExistingPrefab<GameApplication>(ApplicationPath);
            if (existing) return existing;
            var root = new GameObject("GameApplication");
            root.SetActive(false);
            try
            {
                var controller = root.AddComponent<RunUIController>();
                controller.initializeOnAwake = false;
                controller.config = source.config;
                controller.uiFont = source.font;
                controller.visuals = source.visuals;
                controller.uiPrefabs = source.prefabs;
                controller.uiRootPrefab = source.root;
                var flow = root.AddComponent<SceneFlowController>();
                flow.titleScenePath = TitleScenePath;
                flow.lobbyScenePath = LobbyScenePath;
                flow.inGameScenePath = InGameScenePath;
                var app = root.AddComponent<GameApplication>();
                app.controller = controller;
                app.sceneFlow = flow;
                EnsureFolder(ApplicationPath);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, ApplicationPath);
                if (!saved) throw new InvalidOperationException("Could not save " + ApplicationPath);
                return saved.GetComponent<GameApplication>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateScene(string path, GameSceneRole role, GameApplication app, Color background)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path))
                throw new InvalidOperationException("An incompatible asset already exists at " + path);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var entry = new GameObject("Scene Entry").AddComponent<SceneEntry>();
                entry.role = role;
                entry.applicationPrefab = app;
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                EnsureFolder(path);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("Could not save " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void SetBuildScenes()
        {
            var production = new[] { TitleScenePath, LobbyScenePath, InGameScenePath };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in production) scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (production.Contains(existing.path)) continue;
                var legacy = existing.path == PrototypeAuthoring.ScenePath ||
                    string.Equals(System.IO.Path.GetFileName(existing.path), "SampleScene.unity", StringComparison.OrdinalIgnoreCase);
                scenes.Add(new EditorBuildSettingsScene(existing.path, legacy ? false : existing.enabled));
            }
            // Assign only the scene list. EditorBuildSettings configObjects are not replaced.
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T ExistingPrefab<T>(string path) where T : Component
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!asset) return null;
            var gameObject = asset as GameObject;
            var component = gameObject ? gameObject.GetComponent<T>() : null;
            if (!component) throw new InvalidOperationException(path + " exists without " + typeof(T).Name + ".");
            return component;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent) rect.SetParent(parent, false);
            return rect;
        }

        private static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Spacer(string name, Transform parent)
        {
            var element = Rect(name, parent).gameObject.AddComponent<LayoutElement>();
            element.minHeight = 0; element.flexibleHeight = 1;
        }

        private static Text Text(string name, Transform parent, Font font, int size, Color color, float minimum, string initial)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color; text.text = initial;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false; text.raycastTarget = false;
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = minimum; layout.flexibleHeight = 0;
            return text;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length - 1; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
