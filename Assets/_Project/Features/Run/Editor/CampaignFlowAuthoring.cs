using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FateDice.Editor
{
    public static class CampaignFlowAuthoring
    {
        const string CombatPath = "Assets/_Project/Features/Run/Prefabs/CombatUI.prefab";
        [MenuItem("Fate Dice/즉시 이동과 전투 진입 연출 적용")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("플레이·컴파일·미리보기를 종료한 뒤 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 저장한 뒤 적용하세요.");
            string map = CampaignMapAuthoring.ApplyDirectTravel();
            var asset = AssetDatabase.LoadAssetAtPath<CombatUI>(CombatPath);
            if (!asset) throw new InvalidOperationException("기존 전투 원본이 필요합니다.");
            if (asset.entryVersion >= 1) return map + "\n기존 전투 진입 연출 편집값을 유지했습니다.";
            var root = PrefabUtility.LoadPrefabContents(CombatPath);
            try
            {
                var view = root.GetComponent<CombatUI>();
                if (!view.arena || !view.stageGraphic || !view.actionFeedback || !view.hitFlash)
                    throw new InvalidOperationException("전투 무대와 피드백 원본 참조가 필요합니다.");
                view.entrySeconds = .7f; view.entryIntensity = .06f; view.entryVersion = 1;
                PrefabUtility.SaveAsPrefabAsset(root, CombatPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return map + "\n전투 진입 연출을 적용했습니다. 시간과 강도는 CombatUI 원본에서 편집합니다.";
        }
    }
}
