# UiStructureAuthoring.cs
- 역할: Editor API로 없는 UIRoot.prefab 및 Menu/Exploration/Combat/Encounter/Reward/Equipment/ResultUI.prefab 전체 화면을 생성하고 기존 FateDicePrototype.unity의 uiRootPrefab을 연결한다.
- 입력: 기존 FateDiceConfig presentation, 내장 LegacyRuntime font. 기존 네 반복 원본이나 visual catalog는 재생성하지 않으며 기존 전체 화면 asset도 덮어쓰지 않는다.
- 자산 구조: 단일 Canvas/Scaler/GraphicRaycaster, SafeArea 입력그룹, Screens/Popups/Overlay/inactive cache, modal blocker, InputSystem EventSystem; manager.prefabs에는 7개 원본 직접 참조. 각 화면은 고정 HUD/scroll/feature text/image/input/container를 직렬화한다.
- 안전/수명: Play중 또는 dirty scene이면 authoring을 거절한다. SaveAsPrefabAsset/Scene API가 GUID와 참조를 관리한다. 임시 작성 객체는 finally DestroyImmediate한다. 수동 YAML 작성 없음.
- 관계: Main 메뉴의 새 UI structure authoring command, PrototypeAuthoring.ScenePath/기존 config, RunScreenLayout/7화면/UIRoot/UIManager. Editor asmdef만 Unity.InputSystem을 직접 참조한다.
- 검수: persistent whole screen refs/missing scripts, Scene config/GUID 유지, prefab 수정→실제 화면 전파 및 신규-only 재실행 보존. UI_STRUCTURE_PLAN의 정확한 허용 asset 범위를 따른다.
- Scene 구조 이후: SceneStructureAuthoring이 기존 UIRoot registry에 TitleUI를 추가하여 실제 registry는 8개다. 이 생성기의 기존 7화면 작성 계약은 그대로이며 기존 root를 재생성하지 않는다. 제작용 Scene/App 생성은 별도 생성기 책임이다.

## 전투 배치 변경 (2026-09-08)
- 후속 CombatLayoutAuthoring.Apply가 승인된 기존 CombatUI만 전투 전용 배치로 이전한다. 본 생성기는 기존 자산을 덮어쓰지 않으며 새 전투 배치의 작성 책임은 후속 도구에 있다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 기존 화면 생성 후 KoreanUiAuthoring.Apply가 지정 화면의 Text 폰트/고정 메뉴 문구를 보정한다. 이전 저작 도구로 새 화면을 다시 생성할 경우 같은 폰트 적용 단계가 필요하다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
