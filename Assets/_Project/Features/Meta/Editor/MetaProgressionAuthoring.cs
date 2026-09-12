using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FateDice.Editor
{
    public static class MetaProgressionAuthoring
    {
        public const string AppPath = "Assets/_Project/Features/Run/Prefabs/GameApplication.prefab";
        public const string MenuPath = "Assets/_Project/Features/Run/Prefabs/MenuUI.prefab";
        [MenuItem("Fate Dice/메타 진행 UI 연결")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("플레이·컴파일·프리팹 편집을 마친 뒤 적용해 주세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬 변경을 먼저 보존해 주세요.");
            foreach (var path in new[] { AppPath, MenuPath })
                if (EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<GameObject>(path)))
                    throw new InvalidOperationException("프리팹의 저장되지 않은 변경을 먼저 보존해 주세요: " + path);
            var config = AssetDatabase.LoadAssetAtPath<MetaProgressionConfig>(MetaProgressionConfig.DefaultAssetPath);
            if (!config)
            {
                config = ScriptableObject.CreateInstance<MetaProgressionConfig>();
                AssetDatabase.CreateAsset(config, MetaProgressionConfig.DefaultAssetPath);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            var root = PrefabUtility.LoadPrefabContents(MenuPath);
            try
            {
                var view = root.GetComponent<MenuUI>();
                if (view.metaLayoutVersion < 1)
                {
                    view.metaCharacters = Column("보유 캐릭터 선택", view.characterPanel);
                    view.metaLoadout = Column("보유 장비 및 주사위 설정", view.settingsPanel);
                    view.metaGrowth = Column("영구 성장 실행", view.growthPanel);
                    var legacy = view.growthPanel.Find("Permanent growth status");
                    view.legacyGrowthNotice = legacy ? legacy.GetComponent<Text>() : null;
                    var title = view.growthPanel.Find("Growth heading");
                    if (title) title.GetComponent<Text>().text = "모험가 성장";
                    view.metaLayoutVersion = 1;
                    if (!PrefabUtility.SaveAsPrefabAsset(root, MenuPath)) throw new InvalidOperationException("메타 로비 저장 실패");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            root = PrefabUtility.LoadPrefabContents(AppPath);
            try
            {
                var app = root.GetComponent<GameApplication>();
                if (app.metaConfig != config)
                {
                    app.metaConfig = config;
                    if (!PrefabUtility.SaveAsPrefabAsset(root, AppPath)) throw new InvalidOperationException("메타 설정 연결 저장 실패");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "메타 성장 SO와 로비 보유품·성장 컨테이너를 연결했습니다.";
        }
        static RectTransform Column(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var group = rect.GetComponent<VerticalLayoutGroup>(); group.spacing = 10;
            group.childControlHeight = group.childControlWidth = group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return rect;
        }
    }
}
