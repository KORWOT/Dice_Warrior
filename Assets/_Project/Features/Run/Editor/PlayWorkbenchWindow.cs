using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FateDice.Editor
{
    public sealed class PlayWorkbenchWindow : EditorWindow
    {
        public const string MenuPath = "Fate Dice/플레이 작업실";
        static readonly string[] StartLabels = { "타이틀", "준비 로비", "캠페인 지도", "운명 카드", "전투 행동", "상점", "보상", "장비 선택", "여정 결과" };
        [SerializeField] WorkbenchOptions options = new WorkbenchOptions();
        [SerializeField] int previewHeight = 1280;
        Vector2 scroll;
        string notice;

        [MenuItem(MenuPath, false, 0)]
        public static PlayWorkbenchWindow Open()
        {
            var window = GetWindow<PlayWorkbenchWindow>();
            window.titleContent = new GUIContent("플레이 작업실");
            window.minSize = new Vector2(370, 540);
            window.Show();
            return window;
        }

        public static void OpenFor(GameApplication application, WorkbenchStartPoint point)
        {
            var window = Open();
            if (application) window.options.application = application;
            window.options.startPoint = point;
            window.EnsureDefaults();
            window.Repaint();
        }

        void OnEnable()
        {
            EnsureDefaults();
            EditorApplication.playModeStateChanged += OnPlayState;
        }
        void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayState;
        void OnPlayState(PlayModeStateChange change) => Repaint();
        void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

        void EnsureDefaults()
        {
            if (options == null) options = new WorkbenchOptions();
            if (!options.application)
                options.application = AssetDatabase.LoadAssetAtPath<GameApplication>(SceneStructureAuthoring.ApplicationPath);
            var config = options.application ? options.application.controller?.config : null;
            try
            {
                if (config && string.IsNullOrEmpty(options.trialId))
                    options.trialId = config.Snapshot().combat.trialActionIds.FirstOrDefault();
            }
            catch (Exception error) { notice = "설정을 확인해 주세요: " + error.Message; }
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("씬 · UI · 플레이", EditorStyles.boldLabel);
            var production = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .FirstOrDefault(scene => scene.path == SceneStructureAuthoring.TitleScenePath ||
                    scene.path == SceneStructureAuthoring.LobbyScenePath || scene.path == SceneStructureAuthoring.InGameScenePath);
            EditorGUILayout.LabelField("제작 씬: " + (production.IsValid() ? production.name : "별도 작업 씬") +
                "  /  미리보기: " + StartLabels[Mathf.Clamp((int)options.startPoint, 0, StartLabels.Length - 1)], EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.HelpBox("미리보기로 배치를 확인하고, UI 원본에서 편집한 뒤 같은 상황을 바로 실행하세요.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var app = (GameApplication)EditorGUILayout.ObjectField("애플리케이션", options.application, typeof(GameApplication), false);
                if (app != options.application) { options.application = app; options.trialId = null; EnsureDefaults(); }
                options.startPoint = (WorkbenchStartPoint)EditorGUILayout.Popup("시작 지점", (int)options.startPoint, StartLabels);
                long seed = EditorGUILayout.LongField("시드", options.seed);
                options.seed = (uint)Math.Max(0, Math.Min(uint.MaxValue, seed));
                options.cap = (Grade)EditorGUILayout.Popup("탐험 등급 상한", (int)options.cap,
                    Enum.GetValues(typeof(Grade)).Cast<Grade>().Select(KoreanText.Grade).ToArray());
                DrawTrials();
                previewHeight = EditorGUILayout.Popup("화면 비율", previewHeight == 1600 ? 1 : 0,
                    new[] { "720 × 1280", "720 × 1600" }) == 0 ? 1280 : 1600;
                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("상황 미리보기", GUILayout.Height(32))) Run(() =>
                    {
                        EnsureCleanStage();
                        UIWorkbenchPreview.Open(options, previewHeight);
                        notice = "미리보기는 저장되지 않습니다. 배치 변경은 ‘UI 원본 편집’에서 적용하세요.";
                    });
                    if (GUILayout.Button("UI 원본 편집", GUILayout.Height(32))) Run(() => OpenSource(options.application, options.startPoint));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("6주사위 창 미리보기")) Run(() =>
                    {
                        EnsureCleanStage();
                        UIWorkbenchPreview.OpenDice(options, previewHeight);
                        notice = "도착 후 주사위 창입니다. 전투 행동 시작점에서는 실제 고정 결과를 표시합니다.";
                    });
                    if (GUILayout.Button("주사위 창 원본 편집")) Run(() =>
                    {
                        var original = options.application.controller.uiRootPrefab.GetComponent<UIManager>()
                            .prefabs.OfType<DiceRollUI>().Single();
                        OpenPrefab(original.gameObject);
                    });
                }
                using (new EditorGUI.DisabledScope(!options.application || options.seed == 0))
                    if (GUILayout.Button("격리 플레이 시작", GUILayout.Height(38))) Run(() =>
                    {
                        EnsureCleanStage();
                        PlayWorkbenchSession.Start(options);
                        PlayModeWindow.SetCustomRenderingResolution(720, (uint)previewHeight, "작업실");
                        notice = "테스트 전용 저장 슬롯으로 실행합니다. 중지하면 원래 씬으로 돌아옵니다.";
                    });
                if (options.seed == 0) EditorGUILayout.HelpBox("시드는 1 이상으로 입력하세요.", MessageType.Warning);
                EditorGUILayout.HelpBox("전투·상점·보상·장비는 테스트 복사본의 사건 확률을 고정합니다. 결과 화면은 테스트용 능력치로 생성합니다. 실제 설정 원본과 기존 저장은 유지됩니다.", MessageType.None);
            }

            if (EditorApplication.isPlaying)
            {
                var current = GameApplication.Current;
                EditorGUILayout.LabelField("현재 화면", current ? current.sceneFlow.CurrentRole + " / " +
                    (current.controller.Session?.State.phase.ToString() ?? "메뉴") : "진입 중");
                if (GUILayout.Button("플레이 중지", GUILayout.Height(34))) EditorApplication.isPlaying = false;
            }
            else if (StageUtility.GetCurrentStage() is UIWorkbenchPreview)
            {
                if (GUILayout.Button("미리보기 닫기")) Run(() => StageUtility.GoToMainStage());
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("원본 편집", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || !options.application))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("게임 규칙 / 수치")) Run(() => Select(options.application.controller.config));
                    if (GUILayout.Button("그림 / 색상")) Run(() => Select(options.application.controller.visuals));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("앱 / 씬 연결")) Run(() => OpenPrefab(options.application.gameObject));
                    if (GUILayout.Button("화면 등록 / 레이어")) Run(() => OpenPrefab(options.application.controller.uiRootPrefab.gameObject));
                }
            }
            EditorGUILayout.HelpBox("게임 규칙의 변경은 새 여정과 새 미리보기에 반영됩니다. 기존 저장은 시작 당시의 설정을 유지합니다.", MessageType.None);
            EditorGUILayout.LabelField("제작 씬 열기", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("타이틀")) Run(() => OpenScene(SceneStructureAuthoring.TitleScenePath));
                if (GUILayout.Button("준비 로비")) Run(() => OpenScene(SceneStructureAuthoring.LobbyScenePath));
                if (GUILayout.Button("캠페인 인게임")) Run(() => OpenScene(SceneStructureAuthoring.InGameScenePath));
            }
            if (!string.IsNullOrEmpty(notice)) EditorGUILayout.HelpBox(notice, MessageType.Info);
            if (!string.IsNullOrEmpty(PlayWorkbenchSession.LastError))
                EditorGUILayout.HelpBox(PlayWorkbenchSession.LastError, MessageType.Error);
            if (!string.IsNullOrEmpty(PlayWorkbenchSession.LastStorePath))
                EditorGUILayout.LabelField("최근 테스트 저장", PlayWorkbenchSession.LastStorePath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        void DrawTrials()
        {
            if (!options.application || !options.application.controller || !options.application.controller.config) return;
            try
            {
                var config = options.application.controller.config.Snapshot();
                var ids = config.combat.trialActionIds;
                int current = Array.IndexOf(ids, options.trialId);
                int chosen = EditorGUILayout.Popup("와일드 카드", Math.Max(0, current),
                    ids.Select(id => KoreanText.Content(config.Action(id).label)).ToArray());
                if (ids.Length > 0) options.trialId = ids[chosen];
            }
            catch (Exception error) { EditorGUILayout.HelpBox("설정을 확인해 주세요: " + error.Message, MessageType.Error); }
        }

        public static string SourcePrefabPath(GameApplication app, WorkbenchStartPoint point)
        {
            if (!app || !app.controller || !app.controller.uiRootPrefab) throw new InvalidOperationException("앱의 UI 원본 연결이 필요합니다.");
            Type type;
            switch (point)
            {
                case WorkbenchStartPoint.Title: type = typeof(TitleUI); break;
                case WorkbenchStartPoint.Lobby: type = typeof(MenuUI); break;
                case WorkbenchStartPoint.Map: type = typeof(ExplorationUI); break;
                case WorkbenchStartPoint.ExplorationCards: type = typeof(FateChoiceUI); break;
                case WorkbenchStartPoint.Combat: type = typeof(CombatUI); break;
                case WorkbenchStartPoint.Shop: type = typeof(EncounterUI); break;
                case WorkbenchStartPoint.Reward: type = typeof(RewardUI); break;
                case WorkbenchStartPoint.Equipment: type = typeof(EquipmentUI); break;
                case WorkbenchStartPoint.Result: type = typeof(ResultUI); break;
                default: throw new ArgumentOutOfRangeException(nameof(point));
            }
            var view = app.controller.uiRootPrefab.GetComponent<UIManager>().prefabs.Single(p => p && p.GetType() == type);
            return AssetDatabase.GetAssetPath(view);
        }

        public static void OpenSource(GameApplication app, WorkbenchStartPoint point)
        {
            EnsureCleanStage();
            var stage = PrefabStageUtility.OpenPrefab(SourcePrefabPath(app, point));
            Selection.activeGameObject = stage.prefabContentsRoot;
            if (SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        public static void EnsureCleanStage()
        {
            var prefab = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefab && prefab.scene.isDirty) throw new InvalidOperationException("편집 중인 프리팹을 먼저 저장해 주세요.");
            if (prefab || StageUtility.GetCurrentStage() is UIWorkbenchPreview) StageUtility.GoToMainStage();
        }

        static void OpenScene(string path)
        {
            EnsureCleanStage();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("편집 중인 씬을 먼저 저장해 주세요.");
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var entry = UnityEngine.Object.FindAnyObjectByType<SceneEntry>();
            if (entry) Selection.activeGameObject = entry.gameObject;
        }
        static void OpenPrefab(GameObject original)
        {
            EnsureCleanStage();
            PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(original));
        }
        static void Select(UnityEngine.Object asset) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
        void Run(Action action)
        {
            try { action(); }
            catch (Exception error) { notice = error.Message; }
            Repaint();
        }
    }
}
