using UnityEditor;
using UnityEngine;

namespace FateDice.Editor
{
    [CustomEditor(typeof(SceneEntry))]
    public sealed class SceneEntryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var role = serializedObject.FindProperty("role");
            var app = serializedObject.FindProperty("applicationPrefab");
            role.enumValueIndex = EditorGUILayout.Popup("씬 역할", role.enumValueIndex, new[] { "타이틀", "준비 로비", "캠페인 인게임" });
            EditorGUILayout.PropertyField(app, new GUIContent("애플리케이션 원본"));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("UI는 플레이할 때 생성됩니다. 정지 중에는 작업실의 상황 미리보기로 확인하고, UI 원본 프리팹에서 배치를 편집하세요.", MessageType.Info);
            var entry = (SceneEntry)target;
            var point = entry.role == GameSceneRole.Title ? WorkbenchStartPoint.Title :
                entry.role == GameSceneRole.Lobby ? WorkbenchStartPoint.Lobby : WorkbenchStartPoint.Map;
            using (new EditorGUI.DisabledScope(!entry.applicationPrefab))
            {
                if (GUILayout.Button("이 씬의 플레이 작업실 열기", GUILayout.Height(32)))
                    PlayWorkbenchWindow.OpenFor(entry.applicationPrefab, point);
                if (!EditorApplication.isPlayingOrWillChangePlaymode && GUILayout.Button("기본 UI 원본 편집"))
                {
                    try { PlayWorkbenchWindow.OpenSource(entry.applicationPrefab, point); }
                    catch (System.Exception error) { Debug.LogWarning(error.Message, entry); }
                }
            }
        }
    }
}
