using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FateDice.Editor
{
    public static class ProceduralCampaignAuthoring
    {
        [MenuItem("Fate Dice/절차 지도와 운명 선택 창 적용")]
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("플레이를 멈추고 컴파일·가져오기가 끝난 뒤 적용하세요.");
            if (StageUtility.GetCurrentStage() != StageUtility.GetMainStage())
                throw new InvalidOperationException("미리보기 또는 프리팹 편집을 닫고 적용하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("열린 씬을 저장한 뒤 적용하세요.");
            var config = AssetDatabase.LoadAssetAtPath<FateDiceConfig>(FateDiceConfig.DefaultAssetPath);
            if (!config || config.data?.world == null) throw new InvalidOperationException("기본 설정 원본이 필요합니다.");
            var candidate = config.Snapshot();
            if (candidate.world.mapGenerationVersion == 0)
            {
                candidate.world.mapGenerationVersion = 1;
                candidate.world.mapColumns = 5;
                candidate.world.mapPathCount = 5;
                candidate.world.previewDepth = 2;
            }
            var errors = candidate.Validate();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("; ", errors));
            string map = CampaignMapAuthoring.ApplyProcedural();
            string popup = FateChoiceAuthoring.Apply();
            bool configured = config.data.world.mapGenerationVersion == 0;
            if (configured)
            {
                // Only the new map fields and agreed public depth change; gameplay definitions stay intact.
                config.data.world.mapGenerationVersion = candidate.world.mapGenerationVersion;
                config.data.world.mapColumns = candidate.world.mapColumns;
                config.data.world.mapPathCount = candidate.world.mapPathCount;
                config.data.world.previewDepth = candidate.world.previewDepth;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            return map + "\n" + popup + "\n" + (configured
                ? "새 여정에 절차 지도 버전 1을 적용했습니다. 기존 저장은 유지합니다."
                : "이미 적용된 지도 설정과 사용자 편집값을 유지했습니다.");
        }
    }
}
