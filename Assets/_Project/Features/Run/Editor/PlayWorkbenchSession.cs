using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FateDice.Editor
{
    public enum WorkbenchStartPoint { Title, Lobby, Map, ExplorationCards, Combat, Shop, Reward, Equipment, Result }

    [Serializable]
    public sealed class WorkbenchOptions
    {
        public GameApplication application;
        public uint seed = 33;
        public string trialId;
        public Grade cap = Grade.Legendary;
        public WorkbenchStartPoint startPoint = WorkbenchStartPoint.Combat;
    }

    // Editor-only launch state survives script reloads without changing the production bootstrap.
    [InitializeOnLoad]
    public static class PlayWorkbenchSession
    {
        const string Prefix = "FateDice.PlayWorkbench.";
        const string PendingKey = Prefix + "Pending";
        const string ActiveKey = Prefix + "Active";
        const string SetupKey = Prefix + "SceneSetup";
        const string DisabledKey = Prefix + "DisabledEntries";
        const string ErrorKey = Prefix + "LastError";
        const string StoreKey = Prefix + "LastStorePath";

        [Serializable]
        sealed class LaunchRequest
        {
            public string applicationPath, storePath, scenePath, trialId;
            public uint seed;
            public Grade cap;
        }

        [Serializable]
        sealed class SceneRecord
        {
            public string path;
            public bool isLoaded, isActive;
        }

        [Serializable]
        sealed class SceneRecords { public SceneRecord[] scenes; }

        [Serializable]
        sealed class DisabledEntry
        {
            public string entityId;
            public string scenePath;
        }

        [Serializable]
        sealed class DisabledEntries { public List<DisabledEntry> entries = new List<DisabledEntry>(); }

        public static bool IsPending => !string.IsNullOrEmpty(SessionState.GetString(PendingKey, ""));
        public static string LastError => SessionState.GetString(ErrorKey, "");
        public static string LastStorePath => SessionState.GetString(StoreKey, "");

        static PlayWorkbenchSession()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            EditorApplication.update -= RecoverCancelledRequest;
            EditorApplication.update += RecoverCancelledRequest;
        }

        // This path creates no Unity objects, files, callbacks, or changes to the source ScriptableObject.
        public static RunState Build(WorkbenchOptions options)
        {
            var config = Configuration(options, out var trial);
            NodeType? forcedType = null;
            switch (options.startPoint)
            {
                case WorkbenchStartPoint.Combat:
                case WorkbenchStartPoint.Result: forcedType = NodeType.Combat; break;
                case WorkbenchStartPoint.Shop: forcedType = NodeType.Shop; break;
                case WorkbenchStartPoint.Reward:
                case WorkbenchStartPoint.Equipment: forcedType = NodeType.Treasure; break;
            }
            if (forcedType.HasValue)
            {
                config.fate.nodeWeights = new float[5];
                config.fate.nodeWeights[(int)forcedType.Value] = 1;
            }
            if (options.startPoint == WorkbenchStartPoint.Result)
            {
                // Explicit defeat fixture: only the copied starting/enemy stats are changed.
                config.growth.startingMaxHp = 1;
                config.growth.startingPower = 0;
                config.growth.startingGuard = 0;
                var normalEnemies = new HashSet<string>(config.world.events
                    .Where(e => e.type == NodeType.Combat).Select(e => e.enemyId), StringComparer.Ordinal);
                foreach (var enemy in config.combat.enemies.Where(e => normalEnemies.Contains(e.id)))
                {
                    enemy.maxHp = Math.Max(enemy.maxHp, 1024);
                    enemy.power = Math.Max(enemy.power, 100000);
                }
            }

            var run = RunSession.New(config, options.seed, trial, options.cap);
            if (options.startPoint == WorkbenchStartPoint.Title || options.startPoint == WorkbenchStartPoint.Lobby ||
                options.startPoint == WorkbenchStartPoint.Map) return run.State;

            Accept(run.ChooseNode(run.State.availableNodeIds[0]), "첫 경로 선택");
            Accept(run.Roll(), "탐험 주사위 굴리기");
            if (options.startPoint == WorkbenchStartPoint.ExplorationCards) return run.State;
            Accept(run.ChooseFate(run.State.cards[0].id), "운명 카드 선택");

            switch (options.startPoint)
            {
                case WorkbenchStartPoint.Combat:
                    Accept(run.Roll(), "전투 주사위 굴리기");
                    RequirePhase(run, RunPhase.CombatCards);
                    break;
                case WorkbenchStartPoint.Shop:
                    RequirePhase(run, RunPhase.Shop);
                    break;
                case WorkbenchStartPoint.Reward:
                case WorkbenchStartPoint.Equipment:
                    Accept(run.ResolveEncounter(false), "보물 결과 확정");
                    if (options.startPoint == WorkbenchStartPoint.Equipment)
                    {
                        if (string.IsNullOrEmpty(run.State.pendingReward.equipmentId) &&
                            string.IsNullOrEmpty(run.State.pendingReward.dieId))
                            throw new InvalidOperationException("선택된 보물 보상에 장비나 교체 주사위가 없습니다. 보상 설정을 확인해 주세요.");
                        Accept(run.ClaimReward(), "장비 보상 수령");
                        RequirePhase(run, RunPhase.EquipmentChoice);
                    }
                    else RequirePhase(run, RunPhase.Reward);
                    break;
                case WorkbenchStartPoint.Result:
                    for (var turn = 0; turn < 128 && run.State.phase != RunPhase.Result; turn++)
                    {
                        RequirePhase(run, RunPhase.CombatRoll);
                        Accept(run.Roll(), "결과 예시 전투 굴리기");
                        var card = run.State.cards.OrderBy(c => CombatRules.Evaluate(run.State, c).block)
                            .ThenBy(c => CombatRules.Evaluate(run.State, c).damage).First();
                        Accept(run.ChooseAction(card.id), "결과 예시 행동 선택");
                    }
                    if (run.State.phase != RunPhase.Result || run.State.won)
                        throw new InvalidOperationException("현재 적 의도와 행동 설정으로 패배 예시를 만들 수 없습니다. 공격 의도와 수치를 확인해 주세요.");
                    break;
            }
            return run.State;
        }

        public static void Start(WorkbenchOptions options)
        {
            try
            {
                GuardEditor();
                RestoreSceneSetup();
                var state = Build(options);
                Configuration(options, out var trial);
                var applicationPath = AssetDatabase.GetAssetPath(options.application);
                if (string.IsNullOrEmpty(applicationPath) || !PrefabUtility.IsPartOfPrefabAsset(options.application) ||
                    options.application.gameObject.activeSelf)
                    throw new InvalidOperationException("비활성 GameApplication 원본 프리팹을 지정해 주세요.");
                var flow = options.application.sceneFlow;
                if (!flow) throw new InvalidOperationException("GameApplication에 씬 전환 연결이 없습니다.");
                var scenePath = options.startPoint == WorkbenchStartPoint.Title ? flow.titleScenePath :
                    options.startPoint == WorkbenchStartPoint.Lobby ? flow.lobbyScenePath : flow.inGameScenePath;
                foreach (var path in new[] { flow.titleScenePath, flow.lobbyScenePath, flow.inGameScenePath })
                    if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path) ||
                        !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path))
                        throw new InvalidOperationException("제작 씬이 없거나 빌드 목록에서 비활성 상태입니다: " + path);

                var setup = EditorSceneManager.GetSceneManagerSetup();
                if (setup.Length == 0 || setup.Any(s => string.IsNullOrEmpty(s.path)))
                    throw new InvalidOperationException("이름 없는 씬을 먼저 저장하거나 직접 닫아 주세요.");

                var storePath = Path.Combine(IsolatedDirectory(), Guid.NewGuid().ToString("N"), "run.json");
                var store = new LocalRunStore(storePath);
                store.Save(state);
                store.Load(); // Use the existing full state/envelope validator before changing scenes.
                SessionState.SetString(StoreKey, store.Path);
                var request = new LaunchRequest { applicationPath = applicationPath, storePath = store.Path,
                    scenePath = scenePath, seed = options.seed, trialId = trial, cap = options.cap };
                SessionState.SetString(SetupKey, JsonUtility.ToJson(new SceneRecords { scenes = setup.Select(s =>
                    new SceneRecord { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive }).ToArray() }));
                try
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
                        throw new InvalidOperationException("Game 창을 열 수 없습니다.");
                    PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Fate Dice Workbench");
                    SessionState.SetString(ErrorKey, "");
                    SessionState.SetString(PendingKey, JsonUtility.ToJson(request));
                    EditorApplication.isPlaying = true;
                }
                catch
                {
                    SessionState.EraseString(PendingKey);
                    if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreSceneSetup();
                    throw;
                }
            }
            catch (Exception error)
            {
                SessionState.SetString(ErrorKey, "격리 플레이를 시작하지 못했습니다: " + error.Message);
                throw;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BootstrapPending()
        {
            var pending = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(pending)) return;
            SessionState.EraseString(PendingKey); // Consume before any operation that can fail or reenter.
            SessionState.SetString(ActiveKey, pending);
            try
            {
                var request = JsonUtility.FromJson<LaunchRequest>(pending);
                if (request == null || string.IsNullOrEmpty(request.applicationPath) ||
                    SceneManager.GetActiveScene().path != request.scenePath)
                    throw new InvalidOperationException("격리 시작 요청 또는 시작 씬이 일치하지 않습니다.");
                ValidateIsolatedPath(request.storePath);
                var store = new LocalRunStore(request.storePath);
                store.Load();
                var prefab = AssetDatabase.LoadAssetAtPath<GameApplication>(request.applicationPath);
                var app = GameApplication.Bootstrap(prefab, store);
                app.controller.Seed = request.seed;
                // Existing menu selections update only UI settings; they do not save, roll or change scenes.
                var menu = app.controller.UI.ActiveScreen as MenuUI;
                if (!menu || !menu.settingsTab || !menu.characterTab)
                    throw new InvalidOperationException("준비 로비의 탭 연결을 확인해 주세요.");
                menu.settingsTab.onClick.Invoke();
                SelectMenuOption(app.controller, "trial-" + request.trialId);
                SelectMenuOption(app.controller, "cap-" + request.cap);
                menu.characterTab.onClick.Invoke();
            }
            catch (Exception error) { AbortPlay(error); }
        }

        static void SelectMenuOption(RunUIController controller, string key)
        {
            if (controller.Widgets == null || !controller.Widgets.Buttons.TryGetValue(key, out var button) || !button)
                throw new InvalidOperationException("초기 선택 버튼을 찾을 수 없습니다: " + key);
            button.onClick.Invoke();
        }

        static GameConfigData Configuration(WorkbenchOptions options, out string trial)
        {
            if (options == null) throw new ArgumentNullException(nameof(options), "작업실 시작 옵션이 필요합니다.");
            if (!options.application || !options.application.controller || !options.application.controller.config)
                throw new ArgumentException("GameApplication과 게임 설정을 지정해 주세요.", nameof(options));
            if (options.seed == 0) throw new ArgumentOutOfRangeException(nameof(options.seed), "시드는 1 이상이어야 합니다.");
            if (!Enum.IsDefined(typeof(Grade), options.cap))
                throw new ArgumentOutOfRangeException(nameof(options.cap), "등급 상한을 선택해 주세요.");
            if (!Enum.IsDefined(typeof(WorkbenchStartPoint), options.startPoint))
                throw new ArgumentOutOfRangeException(nameof(options.startPoint), "지원하는 시작 화면을 선택해 주세요.");
            var config = options.application.controller.config.Snapshot();
            trial = string.IsNullOrEmpty(options.trialId) ? config.combat.trialActionIds[0] : options.trialId;
            if (!config.combat.trialActionIds.Contains(trial))
                throw new ArgumentException("설정에 등록된 와일드 카드를 선택해 주세요.", nameof(options.trialId));
            return config;
        }

        static void Accept(bool accepted, string command)
        {
            if (!accepted) throw new InvalidOperationException("시작 상황을 만들 수 없습니다: " + command);
        }

        static void RequirePhase(RunSession run, RunPhase phase)
        {
            if (run.State.phase != phase)
                throw new InvalidOperationException("설정의 실제 명령 결과가 요청한 시작 상황과 다릅니다: " + phase);
        }

        static string IsolatedDirectory() => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "FateDiceWorkbench"));

        static void ValidateIsolatedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.GetFullPath(path).StartsWith(
                IsolatedDirectory() + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(path), "run.json", StringComparison.Ordinal))
                throw new InvalidOperationException("작업실 전용 저장 경로가 아닙니다.");
        }

        static void GuardEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsPending ||
                !string.IsNullOrEmpty(SessionState.GetString(ActiveKey, "")))
                throw new InvalidOperationException("현재 플레이 또는 시작 요청이 끝난 뒤 다시 시작해 주세요.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("컴파일과 에셋 갱신이 끝난 뒤 시작해 주세요.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null ||
                !StageUtility.GetCurrentStageHandle().Equals(StageUtility.GetMainStageHandle()))
                throw new InvalidOperationException("프리팹 편집과 미리보기를 닫고 제작 씬으로 돌아와 주세요.");
            GuardScenes();
        }

        static void GuardScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("열린 씬의 변경사항을 먼저 저장하거나 직접 되돌려 주세요: " + scene.name);
            }
        }

        static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && IsPending)
            {
                SessionState.SetString(ActiveKey, SessionState.GetString(PendingKey, ""));
                AbortPlay(new InvalidOperationException("씬 시작 전 격리 초기화가 실행되지 않아 플레이를 중단했습니다."));
                return;
            }
            if (change != PlayModeStateChange.EnteredEditMode ||
                (!IsPending &&
                 string.IsNullOrEmpty(SessionState.GetString(ActiveKey, "")))) return;
            SessionState.EraseString(PendingKey);
            SessionState.EraseString(ActiveKey);
            RestoreDisabledEntries();
            // Scene restoration is deferred until Unity has finished its own exit-to-edit setup.
            EditorApplication.delayCall -= RestoreAfterPlay;
            EditorApplication.delayCall += RestoreAfterPlay;
        }

        static void RecoverCancelledRequest()
        {
            // A cancelled/failed compile can return to Edit without the normal exit callback.
            // Never leave that request armed for a later ordinary press of the Play button.
            if (!IsPending || EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            SessionState.EraseString(PendingKey);
            SessionState.SetString(ErrorKey, "격리 플레이 시작이 취소되었습니다.");
            EditorApplication.delayCall -= RestoreAfterPlay;
            EditorApplication.delayCall += RestoreAfterPlay;
        }

        static void RestoreAfterPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { RestoreSceneSetup(); }
            catch (Exception error)
            {
                SessionState.SetString(ErrorKey, "원래 씬 구성을 복원하지 못했습니다. 씬 변경사항을 확인해 주세요: " + error.Message);
                Debug.LogWarning(LastError);
            }
        }

        static void RestoreSceneSetup()
        {
            var json = SessionState.GetString(SetupKey, "");
            if (string.IsNullOrEmpty(json)) return;
            GuardScenes(); // Never close a user's new dirty scene to force restoration.
            var record = JsonUtility.FromJson<SceneRecords>(json);
            if (record?.scenes == null || record.scenes.Length == 0 ||
                record.scenes.Any(s => s == null || string.IsNullOrEmpty(s.path) || !AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)))
                throw new InvalidOperationException("복원할 원래 씬 정보를 확인할 수 없습니다.");
            EditorSceneManager.RestoreSceneManagerSetup(record.scenes.Select(s =>
                new SceneSetup { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive }).ToArray());
            SessionState.EraseString(SetupKey);
        }

        static void AbortPlay(Exception error)
        {
            SessionState.SetString(ErrorKey, "격리 초기화에 실패하여 플레이를 중단했습니다: " + error.Message);
            SessionState.EraseString(PendingKey);
            // Stopping Play is asynchronous. Disable this Play session's entry points first so their
            // Start methods cannot fall back to the user's default store in the remaining frame.
            var disabled = new DisabledEntries();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var entry in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SceneEntry>(true)))
                {
                    if (!entry.enabled) continue;
                    disabled.entries.Add(new DisabledEntry { entityId = EntityId.ToULong(entry.GetEntityId()).ToString(), scenePath = scene.path });
                    entry.enabled = false;
                }
            }
            SessionState.SetString(DisabledKey, JsonUtility.ToJson(disabled));
            Debug.LogWarning(LastError);
            EditorApplication.isPlaying = false;
        }

        static void RestoreDisabledEntries()
        {
            var json = SessionState.GetString(DisabledKey, "");
            SessionState.EraseString(DisabledKey);
            if (string.IsNullOrEmpty(json)) return;
            var disabled = JsonUtility.FromJson<DisabledEntries>(json);
            if (disabled?.entries == null) return;
            foreach (var record in disabled.entries)
            {
                var entry = EditorUtility.EntityIdToObject(EntityId.FromULong(ulong.Parse(record.entityId))) as SceneEntry;
                if (entry && entry.gameObject.scene.path == record.scenePath) entry.enabled = true;
            }
        }
    }
}
