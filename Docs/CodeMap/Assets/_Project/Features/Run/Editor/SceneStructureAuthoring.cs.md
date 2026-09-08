# SceneStructureAuthoring.cs
- 역할: Editor API로 누락된 TitleUI/GameApplication prefab와 Title/Lobby/InGame 실제 씬을 생성하고 제작용 build scene list를 연결한다.
- 입력: 보존된 FateDicePrototype preview에서 config/font/visual catalog/반복 원본4개/UIRoot 직접 참조를 읽는다. 기존 asset은 덮어쓰지 않으며 UIRoot registry에 TitleUI만 추가한다.
- 앱: inactive 루트에 RunUIController.initializeOnAwake=false, SceneFlowController의 정확한 세 경로, GameApplication의 로컬 참조를 직렬화한다.
- 씬: 각 SceneEntry role/applicationPrefab, Main Camera(orthographic5, 위치0/0/-10, 테마 SolidColor), AudioListener 하나. 저장 후 작성용 씬을 닫고 이전 활성 씬을 복원한다.
- 빌드: Title/Lobby/InGame을 enabled 첫 3개로, 기존 Sample/Prototype은 disabled. 다른 entry/configObjects 보존.
- 안전/수명: stopped/clean scene/PrefabMode guard. prefab 임시 씬을 먼저 닫은 뒤 production 씬을 하나씩 만든다(SCENE-01). 편집은 Editor API이며 meta/GUID는 Unity가 관리한다.
- 관계: SceneEntry/GameApplication/Flow/TitleUI, 기존 UiStructureAuthoring의 root 경로와 PrototypeAuthoring.ScenePath. 같은 기존 Editor assembly, 추가 패키지 없음.
- 검수: actual create 및 재실행 hash 불변, scene/prefab missing scripts/직접 refs/camera, build list/configObjects 보존. SCENE_STRUCTURE_PLAN의 exact assets만 쓴다.

## 한글 UI 적용 (2026-09-08)
- GameApplication의 uiFont와 TitleUI의 초기 한국어 문구는 KoreanUiAuthoring.Apply가 적용한다. 기존 제작 Title/Lobby/InGame scene은 이번 작업에서 변경하지 않았다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.

## 플레이 작업실 (2026-09-08)
- PlayWorkbenchWindow가 기존 ApplicationPath/TitleScenePath/LobbyScenePath/InGameScenePath를 사용한다. 작업실은 제작 씬을 새로 만들지 않고 기존 씬을 열며, 격리 Play 종료 시 이전 구성을 복원한다.
- 사용·검증: Docs/Reports/PLAY_WORKBENCH_REPORT.md.

## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- 기존 TitleScenePath/LobbyScenePath/InGameScenePath와 ApplicationPath를 계속 사용한다. Lobby는 준비 화면, InGame은 캠페인 맵과 전투를 맡으며 이번 UI 개편을 위해 제작 씬이나 별도 전투 씬을 생성하지 않는다.
- SceneStructureAuthoring의 직접 책임은 최초 제작 씬·GameApplication·TitleUI와 Title registry 연결이다. 이번 MenuUI.prefab/ExplorationUI.prefab 편집은 각각 LobbyPreparationAuthoring/CampaignMapAuthoring, DiceRollUI.prefab 생성과 UIRoot의 popup 추가 등록은 DiceRollAuthoring이 맡는다. 이들은 이 파일의 CreateAssets를 변경하거나 호출해 UI를 다시 생성하지 않는다.
- 기존 GameApplication의 RunUIController는 같은 UIRoot prefab 참조를 통해 갱신된 화면과 popup registry를 사용한다. PlayWorkbenchWindow/PlayWorkbenchSession은 기존 제작 경로 상수를 이용해 준비·맵·주사위 시작점으로 연결하며 기존 세 씬 역할과 격리 저장 계약을 유지한다.
- 문서 범위: 실제 소스와 이번 작업의 staged authoring/컨트롤러 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.
