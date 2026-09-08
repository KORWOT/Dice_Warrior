# PlayWorkbenchWindow.cs

- COMBAT_FEEDBACK 변경: 옵션 Popup의 사용자 표시를 와일드 카드로 정정한다. DrawTrials/WorkbenchOptions.trialId와 기존 설정 목록·명령·저장·미리보기 경로는 유지한다. 실행 증거는 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다.

- 역할: Fate Dice/플레이 작업실 메뉴의 EditorWindow다. 시작 상황·시드·시련·등급 상한을 정하고 정지 미리보기, 원본 편집, 격리 Play를 연결한다. 게임 화면이나 게임 규칙을 구현하지 않는다.
- API/입력: Open()은 창을 열고, OpenFor(application, point)는 SceneEntry Inspector에서 앱/시작점을 전달한다. serialized WorkbenchOptions와 previewHeight(1280/1600)를 사용한다. SourcePrefabPath(app, point), OpenSource(app, point), EnsureCleanStage()는 원본 탐색과 Stage 전환을 제공한다.
- 기본값/표시: SceneStructureAuthoring.ApplicationPath의 앱을 찾고 설정 Snapshot의 시련 목록을 읽는다. 한국어 시작점/시련/등급과 720×1280 또는 720×1600 비율을 표시한다. 시드 0은 시작 버튼을 비활성화하고 안내한다. 예시용 확률·스탯은 복사본에서만 바뀐다는 설명을 노출한다.
- 작업 흐름: 상황 미리보기는 UIWorkbenchPreview.Open, 격리 플레이 시작은 PlayWorkbenchSession.Start를 호출한다. 시작 후 선택한 미리보기 높이로 Game View 해상도를 맞춘다. 실행 중 현재 SceneFlow 역할/RunPhase를 읽고 중지 버튼을 제공한다. 정지한 preview Stage에는 닫기 버튼을 제공한다.
- 원본 탐색: 앱/config/visual catalog/UIRoot를 직접 연결로 찾아 선택·Ping하거나 Prefab Stage로 연다. UI 화면 원본은 UIRoot의 UIManager.prefabs에서 concrete type으로 찾는다. Map/ExplorationCards는 ExplorationUI, Shop은 EncounterUI를 공유한다. 원본 Prefab Stage의 화면을 선택하고 2D SceneView로 프레이밍한다.
- 제작 씬: SceneStructureAuthoring의 Title/Lobby/InGame 경로를 EditorSceneManager로 연다. 변경된 씬을 먼저 저장하도록 거절하고 열린 씬의 SceneEntry를 선택한다. Window가 제작 asset을 자동 저장하거나 생성하지 않는다.
- 수명/실패: OnEnable/OnDisable이 Editor Play 상태 listener를 소유한다. EnsureCleanStage는 dirty Prefab Stage를 거절하고 깨끗한 Prefab/preview Stage를 닫는다. Run wrapper가 예외 메시지를 창 notice로 표시하며 Session.LastError/LastStorePath도 보여준다. 게임 입력이나 실제 포인터 검증을 대체하지 않는다.
- 관계: SceneEntryEditor → OpenFor/OpenSource, PlayWorkbenchSession → 옵션·격리 Play, UIWorkbenchPreview → 정지 표시, SceneStructureAuthoring → 원본 경로 상수, GameApplication/RunUIController/UIManager → 원본 참조·현재 상태 조회, KoreanText → 옵션 표시를 담당한다.
- 이전 PLAY_WORKBENCH 작업 검수: 당시 EditMode 128/128 통과(작업실 검사 6개 포함). 실제 실행·직접 클릭 및 보존 경계는 Docs/Reports/PLAY_WORKBENCH_REPORT.md를 참조한다.

- 현재 상태는 OnInspectorUpdate로 Play 중 갱신한다. 선택한 화면 비율은 미리보기와 Game View 모두 적용한다.

## 로비·캠페인·주사위 창 (LOBBY_MAP_DICE)
- 시작점 명칭을 준비 로비/캠페인 지도/운명 카드/전투 행동으로 표시한다. Window 상단에 실제 로드된 제작 씬과 선택한 미리보기 시작점을 별도로 표시한다. 제작 씬 열기와 미리보기 열기는 서로 다른 작업이며 씬 role을 바꾸지 않는다.
- 6주사위 창 미리보기는 EnsureCleanStage→UIWorkbenchPreview.OpenDice, 주사위 창 원본 편집은 실제 UIManager.prefabs의 DiceRollUI 원본을 찾아 Prefab Stage로 연다.
- 기존 9개 enum/옵션, seed 입력, 격리 Play·dirty guard·등록 탐색·OnInspectorUpdate는 유지한다. 이번 변경 실행 증거는 메인 LOBBY_MAP_DICE 검증으로 갱신해야 한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
