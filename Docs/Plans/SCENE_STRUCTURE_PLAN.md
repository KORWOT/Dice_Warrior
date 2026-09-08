# SCENE_STRUCTURE_PLAN

상태: IMPLEMENT / COMPLETE. 사용자 승인: 2026-09-08 "좋아 다음 씬구조 정리".
대상: D:/UnityProject/Dice_Warrior, main 433edddc872433ead6db52a872ea0fcac659d92e, Unity 6000.6.0f1. 기존 승인 결과가 untracked인 실제 checkout 사용. 커밋/브랜치/staging 없음. baseline-git-status.txt 및 baseline-hashes.json에 기존 상태 보존.

## 계약
- 실제 Title → Lobby ↔ InGame 세 씬. Title은 초기화 완료 후 Lobby 진입, Lobby는 기존 준비/새 런/이어하기/손상 저장 보관 UI, InGame은 기존 탐험·전투·보상·장비·결과 UI.
- GameApplication 프리팹 하나가 RunUIController, SceneFlowController 및 런/UI 수명을 소유한다. 명시적 SceneEntry만 부트스트랩한다. 전역 자동 Scene 생성/탐색을 확장하지 않는다. 기존 FateDicePrototype은 개발/회귀 fixture로 보존.
- 각 제작 씬에서 바로 Play 가능. InGame 직접 진입 시 유효 저장을 읽으며 저장 없음/손상은 쓰기 없이 Lobby 복귀. UI/Canvas/EventSystem은 하나. 씬 이동은 보상을 주지 않으며 새 런은 저장 성공 후 진입.
- 씬 로드 동안 입력 잠금과 중복 요청 방지, 실패 시 현재 UI에 오류 표시/잠금 복구. 개별 view는 scene/store를 참조하지 않는다.
- 명시적 Bootstrap(prefab, optionalStore)로 테스트 저장소를 초기화 전에 주입. 실제 사용자 저장을 테스트에 사용하지 않는다.
- Build Settings 첫 enabled 씬 Title, 다음 Lobby/InGame. SampleScene/legacy는 빌드에서 비활성화하되 자산/기존 configObjects 보존.

## 동결 API / 책임
Main RunUIController: public bool initializeOnAwake=true; Initialize(LocalRunStore store, IRunSceneNavigation navigation=null); EnterTitle(Action enterLobby); EnterLobby(); bool TryEnterInGame(); SyncInputLock(); ReportNavigationError(string). Busy는 actionBusy 또는 navigation.IsTransitioning. Legacy Awake/UseStore/commands 호환. managed new/continue/menu는 navigation.RequestInGame/RequestLobby를 호출. gameplay rules/store format 그대로.
C Flow/SceneNavigation.cs: enum GameSceneRole {Title,Lobby,InGame}; IRunSceneNavigation {bool IsTransitioning{get;} bool RequestLobby(); bool RequestInGame();}.
C Flow/GameApplication.cs: public RunUIController controller; public SceneFlowController sceneFlow; public static GameApplication Current{get;}; public static GameApplication Bootstrap(GameApplication prefab, LocalRunStore store=null). inactive authored prefab를 instantiate, sceneFlow.Initialize(controller), controller.Initialize(store/default,sceneFlow), DontDestroyOnLoad, activate. bootstrap boundary에서만 static 사용; duplicate/current 및 domain-reload-off 수명 주의. public void EnterScene(GameSceneRole role) routes to flow. OnDestroy clears owned Current.
C Flow/SceneEntry.cs: public GameSceneRole role; public GameApplication applicationPrefab; Start bootstraps then EnterScene(role). Production scenes only.
C Flow/SceneFlowController.cs: serialized public string titleScenePath,lobbyScenePath,inGameScenePath; Initialize(RunUIController); EnterScene(GameSceneRole); RequestLobby/RequestInGame; IsTransitioning. LoadSceneAsync Single + input lock, bounded error handling and direct InGame fallback. No app/global lookup in flow; controller is explicit. Role entry must be accepted before unlocking. Public CurrentRole nullable useful diagnostics. Validate target availability before loading. Single transition; no speculative queue/framework.
B Presentation/TitleUI.cs: TitleUIData : UIData {string title,subtitle,status; Action enterLobby; ButtonAppearance appearance;}; TitleUI : BaseUI<TitleUIData> public Text title,subtitle,status; public CommonButtonView enterButton. Button key enter-lobby. Bind display/own callback only, unbind cleanly.
B Editor/SceneStructureAuthoring.cs: public static string CreateAssets(). Editor-only guards stopped/clean, create missing TitleUI/GameApplication/Title/Lobby/InGame through Editor API, add TitleUI to existing root registry preserving seven screens. App prefab inactive; RunUIController.initializeOnAwake=false, copy existing prototype config/font/catalog/reusable/root references. Exact scene paths constants public. Add production scenes to EditorBuildSettings with Title first; preserve unrelated entries/configObjects. Do not overwrite existing authored layouts. All Unity execution Main only.

## Allowlist
New C#: Assets/_Project/Features/Run/Flow/{SceneNavigation,GameApplication,SceneEntry,SceneFlowController}.cs; Assets/_Project/Features/Run/Presentation/TitleUI.cs; Assets/_Project/Features/Run/Editor/SceneStructureAuthoring.cs; Assets/_Project/Features/Run/Tests/PlayMode/SceneStructureTests.cs.
Modified C#: Assets/_Project/Features/Run/Presentation/RunUIController.cs only.
Exact asset writes via Editor API: Assets/_Project/Scenes/Title.unity; Lobby.unity; InGame.unity (same folder); Assets/_Project/Features/Run/Prefabs/TitleUI.prefab; GameApplication.prefab (same folder); Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab (add registry only); ProjectSettings/EditorBuildSettings.asset (scene list only).
Unity-generated meta for new files/folders. Matching Docs/CodeMap/<source>.md for all new/modified scripts; direct relationship maps FateDiceScreen/UIManager/UIRoot/MenuUI/RunUIData/UiStructureAuthoring and Docs/CodeMap/INDEX.md. Docs/Plans/SCENE_STRUCTURE_PLAN.md, Docs/Reports/SCENE_STRUCTURE_REPORT.md. Cwd artifacts/scene-structure evidence/staging allowed. Existing UI tests may perform their already-authorized temporary prefab edit probes with exact restoration.
No changes to game rules/config/default save/schema/packages/other settings/legacy scene/existing seven views/layouts. No login/backend, loading animation system, addressables, new game content, APK/device test or broad folder migration.

## AC / verification / budget
1. Initial missing-scene/prefab/build contract RED, then compilation and asset reference checks.
2. Production PlayMode: Title→Lobby→new→InGame→Lobby→continue; each direct scene boot, valid/corrupt/missing saves, state/RNG/offer/dice/reroll/last-result persistence, duplicate/failed transition lock, one root/Canvas/EventSystem. Representative actual production run commands and result return.
3. Full existing EditMode122 + PlayMode37 regressions and new tests. Positive executed counts required; 0-case is NOT_RUN. Clean compiler/runtime errors. Live hierarchy and Title/Lobby/InGame screenshot checks.
4. CodeMap 1:1/meta/index and protected file baseline diff; report separate actual tests from unrun APK/device.
Sequence: freeze plan + initial RED; implement independently; Main integrate/import/author/test; A read-only initial flow/state review, Main asset/test/docs review in same bundle; max2 fix/retest bundles; report complete only all required AC pass.
Delegate budget: reuse existing C(local_save), B(dice_fate_rules), A(hand_oracle), max4 simultaneous inclMain, unique3 total, no recursion. One writer/file; Main owns controller/tests/all CodeMaps/INDEX/PLAN/REPORT and serialized Unity calls. Other agents stage owned files in cwd only. No per-agent additional review budget. Prior completed task budgets/finding states unchanged.
Progress: initial review0/1, fix/retest0/2. Prior UI COMPLETE remains historical.

## 최초 검토 / 수정 묶음1
- RED 2/2 예상 실패: Title 자산 없음, 기존 첫 빌드 SampleScene. red-play.json 보존.
- 컴파일 PASS. SCENE-01: prefab 작성용 untitled scene을 열어 둔 채 다음 untitled production scene을 additive 생성하여 Unity가 거부했다. 기존 scene은 finally로 clean 복원됨.
- 같은 생성기에서 prefab 임시 씬을 먼저 종료한 뒤 production 씬을 하나씩 만들도록 수정. authoring-fixed.json 실제 성공, 기존 assets 재작성 없음. 각 제작 씬은 Camera/AudioListener 하나를 작성한다.
- A 최초 코드검토: 추가 필수 코드 결함 없음. 사전 Bootstrap tests가 놓치는 cold SceneEntry.Start는 실제 direct Play로 보강한다.
- production 9/9 PASS; 기존 Edit122/122 PASS. full Play 및 cold 진입/자산/문서 검증 진행 중. 최초검토1/1, 수정1/2 사용.

## 완료 판정
필수 AC 통과. Edit122/122, Play46/46(신규9 포함), cold3씬/기본저장비생성/정상종료, 실제 pointer flow 및 portrait 캡처, 자산 참조/재실행 보존 확인. C#48↔CodeMap48와 meta/index/보호hash 검증 증거는 SCENE_STRUCTURE_REPORT 참조. SCENE-01 해결, 최초1/1·수정1/2·남은필수0. APK/device는 NOT_RUN이며 완료 범위에 포함하지 않는다. 최종 Title 씬 stopped/clean.
