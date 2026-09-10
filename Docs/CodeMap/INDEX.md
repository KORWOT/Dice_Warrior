# CodeMap 기능 색인

실제 로컬 플레이 루프, 저장/이어하기, 전용 Scene과 설정 SO의 직접 연결을 구현했다. Assets/_Project의 113개 프로젝트 소유 C#은 각각 같은 상대 경로의 요약 문서와 대응한다. 실행 증거와 조작·밸런싱 안내는 [REPORT](../Reports/FATE_DICE_PROTOTYPE_REPORT.md), 고정 단계와 Finding 이력은 [PLAN](../Plans/FATE_DICE_PROTOTYPE_PLAN.md)에 있다. 후속 작업의 실행 상태는 각 작업의 REPORT를 따른다.

| 기능 / 스크립트 | 한국어 역할과 직접 관계 | 요약 |
|---|---|---|
| RA-C / RunRulesCatalog.cs | 순수 규칙 정의/조회/검증 및 런 규칙 복사 | [요약](Assets/_Project/Features/Fate/Domain/RunRulesCatalog.cs.md) |
| RA-C / RulesCopy.cs | 정의의 모든 중첩 필드 명시 깊은 복제 | [요약](Assets/_Project/Features/Fate/Domain/RulesCopy.cs.md) |
| RA-C / RunStateData.cs | Core/Runtime 어댑터의 공통 런 상태 필드 | [요약](Assets/_Project/Features/Run/Domain/RunStateData.cs.md) |
| RA-C / CoreRunState.cs | 순수 Core 규칙/가변 런 상태 | [요약](Assets/_Project/Features/Run/Domain/CoreRunState.cs.md) |
| RA-C / RunStateCopy.cs | 상태/기록/노드/카드의 명시 깊은 복제 | [요약](Assets/_Project/Features/Run/Domain/RunStateCopy.cs.md) |
| RA-C / RunApplication.cs | 순수 상태 소유·직렬 명령·검증·체크포인트 확정 | [요약](Assets/_Project/Features/Run/Application/RunApplication.cs.md) |
| RA-C / CoreBoundaryTests.cs | 실제 Core 참조 차단/복제/후보 소유권 검사 | [요약](Assets/_Project/Features/Run/Tests/EditMode/CoreBoundaryTests.cs.md) |
| RA-C / CoreReplayTests.cs | 분리 전 273명령의 고정 전체 상태/복원 재연 | [요약](Assets/_Project/Features/Run/Tests/EditMode/CoreReplayTests.cs.md) |
| RA-B / IRunStore.cs | 로컬 체크포인트 계약, 파일/실패 주입 저장소 | [요약](Assets/_Project/Features/Save/Runtime/IRunStore.cs.md) |
| RA-B / RunCommandToken.cs | 표시 시점 runId/sequence로 오래된 명령 거절 | [요약](Assets/_Project/Features/Run/Application/RunCommandToken.cs.md) |
| RA-B / RunStateValidator.cs | 세션 후보와 저장 경계가 공유하는 순수 상태 검증 | [요약](Assets/_Project/Features/Run/Domain/RunStateValidator.cs.md) |
| RA-B Tests / RunBoundaryTests.cs | 깊은 상태 소유·실패 원자성·시간·재진입·ID/토큰 | [요약](Assets/_Project/Features/Run/Tests/EditMode/RunBoundaryTests.cs.md) |
| RA-B Tests / RunBoundaryFlowTests.cs | 실제 UI 저장 후 오류 복구·재표시 및 전투 연출의 정확 위치/중단 복원 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/RunBoundaryFlowTests.cs.md) |
| RA-A / ISeedSource.cs | 새 여정에만 시드 요청. Controller 소비, GameApplication 조립 | [요약](Assets/_Project/Features/Run/Runtime/ISeedSource.cs.md) |
| RA-A / FixedSeedSource.cs | nonzero 고정 재연/작업실 공급자 | [요약](Assets/_Project/Features/Run/Runtime/FixedSeedSource.cs.md) |
| RA-A / SystemSeedSource.cs | 일반 새 여정 OS 난수 시드, 게임 RNG 독립 | [요약](Assets/_Project/Features/Run/Runtime/SystemSeedSource.cs.md) |
| RA-A Tests / SeedAndPresentationTests.cs | 시드/시간/구형 저장/동일 명령열의 규칙 보존 | [요약](Assets/_Project/Features/Run/Tests/EditMode/SeedAndPresentationTests.cs.md) |
| RA-A Tests / SeedPresentationFlowTests.cs | 실제 UI 공급자 호출/재개/각 phase 시간과 0/재굴림 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/SeedPresentationFlowTests.cs.md) |
| Fate / FateDiceConfig.cs | SO의 모든 규칙·콘텐츠·표시 설정을 검증하고 새 런 스냅샷을 제공. Screen·RunSession·저장 검사가 사용 | [요약](Assets/_Project/Features/Fate/Configs/FateDiceConfig.cs.md) |
| Dice / DiceRules.cs | 단일 규칙 RNG, 가중 추첨, 6D6와 10패 판정. RunSession·카드 생성·저장 검사가 사용 | [요약](Assets/_Project/Features/Dice/Runtime/DiceRules.cs.md) |
| Fate / FateCardRules.cs | 탐험·행동 카드 3장의 독립 등급과 콘텐츠 확정. RunApplication이 호출, 전투 평가와 저장이 결과 사용 | [요약](Assets/_Project/Features/Fate/Runtime/FateCardRules.cs.md) |
| Run / RunState.cs | 런 전체 상태·설정 스냅샷·노드 ID·보상·최종 기록·저장 봉투 DTO. 모든 규칙과 저장이 공유 | [요약](Assets/_Project/Features/Run/Runtime/RunState.cs.md) |
| Run / RunSession.cs | 공개 API facade. Core RunApplication에 명령 위임, 표시·저장 DTO 조합 | [요약](Assets/_Project/Features/Run/Runtime/RunSession.cs.md) |
| Combat / CombatRules.cs | 의도·행동의 실제 피해/보호량과 생존 순서. Growth의 스탯/태그 보정을 사용하며 Screen과 평가식 공유 | [요약](Assets/_Project/Features/Combat/Runtime/CombatRules.cs.md) |
| Exploration / ExplorationRules.cs | 모드0의 기존 앞길 생성과 모드1의 전체 절차 지도 생성 위임·활성/기록 가지 정리·층 진행. RunSession이 진행 시 호출 | [요약](Assets/_Project/Features/Exploration/Runtime/ExplorationRules.cs.md) |
| Growth / GrowthRules.cs | XP/레벨·장비 교체·태그 조건·보상 자원. Combat과 Screen에 같은 파생 스탯 제공 | [요약](Assets/_Project/Features/Growth/Runtime/GrowthRules.cs.md) |
| Save / LocalRunStore.cs | 전체 스냅샷/상태 검증, 체크섬, 원자 저장, 손상 원본 보관. Screen의 Checkpoint로 연결 | [요약](Assets/_Project/Features/Save/Runtime/LocalRunStore.cs.md) |
| Run / FateDiceScreen.cs | 기존 Scene GUID/API 보존 facade. 상속 RunUIController가 typed 화면·세션·저장 소유 | [요약](Assets/_Project/Features/Run/Presentation/FateDiceScreen.cs.md) |
| Run / FateDiceWidgets.cs | 프리팹에 작성된 배치 아래 반복 버튼·카드·노드·가변 연결선만 소유. Manager에 입력잠금 위임 | [요약](Assets/_Project/Features/Run/Presentation/FateDiceWidgets.cs.md) |
| Run Editor / PrototypeAuthoring.cs | Editor API로 신규 SO/Scene 생성, 자산 검사, 실제 SO 편집→기존 저장 비교→복원 검증 | [요약](Assets/_Project/Features/Run/Editor/PrototypeAuthoring.cs.md) |
| Run Tests / RuleTests.cs | 독립 46,656 오라클·규칙 경계·확률·설정 오류 검사. DiceRules·FateCardRules·Config 검증 | [요약](Assets/_Project/Features/Run/Tests/EditMode/RuleTests.cs.md) |
| Run Tests / RunTests.cs | 실제 RunSession 명령으로 전투·5사건·성장·태그·보스·재굴림·교체 회귀 검사 | [요약](Assets/_Project/Features/Run/Tests/EditMode/RunTests.cs.md) |
| Run Tests / SaveTests.cs | 디스크 왕복·동일 RNG/카드·손상 보존·파일 교체 실패·보상 중복·스냅샷 검사 | [요약](Assets/_Project/Features/Run/Tests/EditMode/SaveTests.cs.md) |
| Run Tests / FateDiceGuiTests.cs | 실제 Scene의 포인터/레이캐스트 입력으로 완주·패배·이어하기·성장·2세로비·입력 잠금 검사 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/FateDiceGuiTests.cs.md) |
| Run / FateDiceVisualCatalog.cs | 안정 ID·공개 유형·등급·버튼 용도 표시 매핑 검증/조회. 게임 규칙과 외형을 분리 | [요약](Assets/_Project/Features/Run/Configs/FateDiceVisualCatalog.cs.md) |
| Shared UI / CommonButtonView.cs | 공통 원본의 로컬 참조, 선택/눌림/비활성, own listener 정리 | [요약](Assets/_Project/Shared/UI/Presentation/CommonButtonView.cs.md) |
| Exploration / ExplorationNodeView.cs | 공개 유형·개별 ID·입력·선택·취소 가능한 fade. 런 기록 수정 없음 | [요약](Assets/_Project/Features/Exploration/Presentation/ExplorationNodeView.cs.md) |
| Combat / ActionCardView.cs | 원본 스킬 그림과 현재 제시 ID/등급/실제 효과/태그를 구분 | [요약](Assets/_Project/Features/Combat/Presentation/ActionCardView.cs.md) |
| Fate / FateCardView.cs | 사건 ID를 받지 않는 공개 유형/등급/그림 표시와 개별 선택 | [요약](Assets/_Project/Features/Fate/Presentation/FateCardView.cs.md) |
| Run Editor / UiPrototypeAuthoring.cs | 실제 원본4개·catalog 생성/Scene 연결, 기존 이미지 기반 비율 probe | [요약](Assets/_Project/Features/Run/Editor/UiPrototypeAuthoring.cs.md) |
| Run Tests / UiVisualCatalogTests.cs | 매핑 오류·합법 fallback·Sprite 공유·공개 타입·상태 불변19개 | [요약](Assets/_Project/Features/Run/Tests/EditMode/UiVisualCatalogTests.cs.md) |
| Run Tests / ReusableViewTests.cs | View 재Bind·잔여 listener/그림/태그·중복 제시·fade 취소7개 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/ReusableViewTests.cs.md) |
| UI 구조 / UiStructureAuthoring.cs | Editor API로 없는 UIRoot.prefab 및 Menu/Exploration/Combat/Encounter/Reward/Equipment/ResultUI.prefab 전체 화면을 생성하고 기존 FateDicePrototype.unity의 uiRootPrefab을 연결한다. | [요약](Assets/_Project/Features/Run/Editor/UiStructureAuthoring.cs.md) |
| UI 구조 / CombatUI.cs | 전투 전용 header/HP/arena/dice/portrait 카드 가로열을 typed data에 연결한다. | [요약](Assets/_Project/Features/Run/Presentation/CombatUI.cs.md) |
| UI 구조 / EncounterUI.cs | 일반 사건·휴식 및 상점의 공개 artwork/설명/outcome과 선택 목록을 표시한다. RunScreenView<EncounterUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/EncounterUI.cs.md) |
| UI 구조 / EquipmentUI.cs | 발견한 장비·현재 장비 비교 또는 주사위 교체 안내와 선택을 표시한다. RunScreenView<EquipmentUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/EquipmentUI.cs.md) |
| UI 구조 / ExplorationUI.cs | 전체 공개 지도·접근 불가/완료 경로를 표시하며 가능한 노드 탭에서 즉시 이동한다. 굴림과 운명 선택은 별도 popup이다. RunScreenView<ExplorationUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/ExplorationUI.cs.md) |
| UI 구조 / MenuUI.cs | 준비 로비의 캐릭터·세팅·성장 안내 탭과 새 여정·이어하기를 바인딩한다. 시드는 작업실에서 편집한다. RunScreenView<MenuUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/MenuUI.cs.md) |
| UI 구조 / ResultUI.cs | 런 결과 요약/선택 등급 기록과 재시작 요청을 표시한다. RunScreenView<ResultUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/ResultUI.cs.md) |
| UI 구조 / RewardUI.cs | 공개 artwork, 보상 설명/진행 안내와 명시적 Claim 입력을 표시한다. RunScreenView<RewardUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/RewardUI.cs.md) |
| UI 구조 / RunScreenLayout.cs | 전체 화면 prefab 안의 common HUD Text(header/stats/situation/fate/notice/gear), diceRow/body/footer, ScrollRect의 serialized 직접 참조와 Validate를 제공한다. | [요약](Assets/_Project/Features/Run/Presentation/RunScreenLayout.cs.md) |
| UI 구조 / RunScreenView.cs | RunScreenView<TData>는 BaseUI<TData>와 IRunScreenView를 연결하는 공통 HUD 바인더다. IRunScreenView.Widgets가 현재 화면의 진단 경계를 제공한다. | [요약](Assets/_Project/Features/Run/Presentation/RunScreenView.cs.md) |
| UI 구조 / RunUIController.cs | 앱 루트 또는 legacy prototype에서 런/저장/화면 데이터를 소유하고 명시적 씬 이동을 요청한다. | [요약](Assets/_Project/Features/Run/Presentation/RunUIController.cs.md) |
| UI 구조 / RunUIData.cs | 화면별 표시 DTO와 공개 CampaignNodeUIData를 정의한다. nullable type으로 먼 유형을 숨기고 지도 좌표/연결/선택·완료 상태를 전달한다. ScriptableObject나 저장 모델이 아니다. | [요약](Assets/_Project/Features/Run/Presentation/RunUIData.cs.md) |
| UI 구조 / UIManagerTests.cs | 가벼운 concrete test UI/데이터와 authored-style root fixture로 shared UI lifecycle/cache/type/input/popup/failure 계약13개를 검증한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/UIManagerTests.cs.md) |
| UI 구조 / UiStructureTests.cs | 실제 UIRoot와 7개 전체 화면 prefab의 존재/스크립트/고정 ScrollRect/Text/InputField 구조, 실제 prefab 편집 전파와 캐시 복귀, 실제 EventSystem modal 입력 차단을 검증한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/UiStructureTests.cs.md) |
| UI 구조 / BaseUI.cs | 공통 UIData 및 BaseUI/BaseUI<TData>의 typed 바인딩·초기화·활성·닫기 수명. SharedUI는 uGUI만 참조하며 게임 타입을 모른다. | [요약](Assets/_Project/Shared/UI/Presentation/BaseUI.cs.md) |
| UI 구조 / UIManager.cs | 직접 등록한 BaseUI prefab 목록을 구체 타입으로 검증하고 타입마다 한 인스턴스를 캐시한다. active 기본 화면 하나와 명시적 popup stack을 소유한다. Resources/string-path load/게임 계산 없음. | [요약](Assets/_Project/Shared/UI/Presentation/UIManager.cs.md) |
| UI 구조 / UIRoot.cs | authored Canvas/safeArea/screenLayer/popupLayer/overlayLayer/cacheLayer/inputGroup/popupBlocker 참조를 관리한다. | [요약](Assets/_Project/Shared/UI/Presentation/UIRoot.cs.md) |
| 씬 구조 / SceneStructureAuthoring.cs | Editor API로 누락된 TitleUI/GameApplication prefab와 Title/Lobby/InGame 실제 씬을 생성하고 제작용 build scene list를 연결한다. | [요약](Assets/_Project/Features/Run/Editor/SceneStructureAuthoring.cs.md) |
| 씬 구조 / GameApplication.cs | 제작용 씬 전체에서 RunUIController/SceneFlowController/UIRoot/세션 수명을 소유하는 명시적 앱 루트. | [요약](Assets/_Project/Features/Run/Flow/GameApplication.cs.md) |
| 씬 구조 / SceneEntry.cs | Title/Lobby/InGame 씬마다 하나씩 작성되는 제작용 진입 컴포넌트. | [요약](Assets/_Project/Features/Run/Flow/SceneEntry.cs.md) |
| 씬 구조 / SceneFlowController.cs | 세 제작용 씬의 실제 비동기 Single 로드와 진입 수락, 입력 잠금을 소유한다. | [요약](Assets/_Project/Features/Run/Flow/SceneFlowController.cs.md) |
| 씬 구조 / SceneNavigation.cs | 제작용 씬 역할(Title/Lobby/InGame)과 RunUIController가 사용하는 최소 이동 인터페이스 IRunSceneNavigation을 정의한다. | [요약](Assets/_Project/Features/Run/Flow/SceneNavigation.cs.md) |
| 씬 구조 / TitleUI.cs | BaseUI<TitleUIData> 파생 타이틀 화면. 같은 파일의 TitleUIData는 title/subtitle/status, ButtonAppearance, enterLobby Action을 전달한다. | [요약](Assets/_Project/Features/Run/Presentation/TitleUI.cs.md) |
| 씬 구조 / SceneStructureTests.cs | 실제 Title/Lobby/InGame assets와 GameApplication prefab를 사용하는 제작용 씬 구조 통합 검증. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/SceneStructureTests.cs.md) |

직접 자산: `Assets/_Project/Scenes/FateDicePrototype.unity`의 `FateDiceScreen.config` → `Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset`; `uiFont` → `Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf`. 런타임 자산 검색으로 누락을 대체하지 않는다.

추가 직접 자산: Screen.visuals → Run/Configs/DefaultFateDiceVisuals.asset; Screen.uiPrefabs → Shared/UI/Prefabs/CommonButtonView.prefab 및 Exploration/Combat/Fate 각 Prefabs 원본. 세 기능 원본은 CommonButtonView의 구조 Variant이고 실제 화면이 Instantiate한다. Grade 색상은 기존 설정에만 있다.

컴파일 경계: FateDice.SharedUI는 uGUI만 참조하며 FateDice.Runtime이 이를 참조한다.  `FateDice.Runtime`(uGUI/InputSystem), `FateDice.Editor`(Editor 전용), `FateDice.EditMode.Tests`, `FateDice.PlayMode.Tests`. 실제 런타임 조작은 Screen→Session→규칙→Checkpoint 저장 후 화면 갱신 흐름이다. Android 기기 검증과 출시·계정 보안은 별도 후속 범위다.
UI 구조 개선: [PLAN](../Plans/UI_STRUCTURE_PLAN.md), [REPORT](../Reports/UI_STRUCTURE_REPORT.md). Scene의 상속 uiRootPrefab → Shared/UI/Prefabs/UIRoot.prefab → UIManager.prefabs의 Run/Prefabs 7개 전체 화면. 고정 레이아웃은 prefab에, 반복 View 원본은 기존 네 파일에 있다. RunUIController → typed UIData → UIManager → BaseUI/RunScreenView → Widgets/반복 View 흐름이며 게임·저장은 Controller/Session에 남는다. 이 단락은 UI_STRUCTURE 작업 결과이며, 후속 SCENE_STRUCTURE에서 아래 제작용 세 씬을 연결했다.

제작용 씬 구조: [PLAN](../Plans/SCENE_STRUCTURE_PLAN.md), [REPORT](../Reports/SCENE_STRUCTURE_REPORT.md).
`Title.unity → Lobby.unity ↔ InGame.unity`; 각 SceneEntry.applicationPrefab → `Run/Prefabs/GameApplication.prefab` → RunUIController/SceneFlowController → 기존 UIRoot. TitleUI.prefab가 8번째 전체 화면이며 Lobby는 기존 MenuUI, InGame은 기존 RunPhase별 화면을 사용한다. AppRoot에만 지속 수명을 두고 SceneEntry는 씬과 함께 종료한다. 빌드는 Title/Lobby/InGame이 첫 3개이며 Sample은 disabled, FateDicePrototype 자산은 개발·회귀 fixture로 남는다.


## 전투 화면 배치
[PLAN](../Plans/COMBAT_LAYOUT_PLAN.md), [REPORT](../Reports/COMBAT_LAYOUT_REPORT.md). CombatUI/ActionCardView를 전투 전용 배치로 변경하고 CombatDie variant를 직접 참조한다. 기본3장은 가로로 함께 보이고 설정4~5장은 가로 스크롤로 유지한다. 새 이미지/아이콘/폰트/게임 규칙은 추가하지 않았다.

| 기능 / 스크립트 | 한국어 역할과 직접 관계 | 요약 |
|---|---|---|
| 전투 UI / CombatStageGraphic.cs | 중앙 연출 공간에 익명 실루엣과 바닥을 그리는 입력 비차단 Graphic. | [요약](Assets/_Project/Features/Combat/Presentation/CombatStageGraphic.cs.md) |
| 전투 UI Editor / CombatLayoutAuthoring.cs | 승인된 프리팹3개를 Editor API로 배치하고 GUID/상속을 보존한다. | [요약](Assets/_Project/Features/Run/Editor/CombatLayoutAuthoring.cs.md) |
| 전투 UI Tests / CombatLayoutTests.cs | 실제 InGame 두 비율/HP/주사위/카드3·5/재굴림/입력·저장 oracle를 검사한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/CombatLayoutTests.cs.md) |

## 한글 UI 및 폰트
[PLAN](../Plans/KOREAN_UI_PLAN.md), [REPORT](../Reports/KOREAN_UI_REPORT.md), [Pretendard 출처·라이선스](../ThirdParty/PRETENDARD.md). 기본 플레이 흐름 전체를 표시 경계에서 한글화하고 상업용 배포 가능한 OFL 글꼴을 프로젝트에 포함한다. 기존 규칙·저장 데이터와 사용자 편집 콘텐츠는 보존한다.

| 기능 / 스크립트 | 한국어 역할과 직접 관계 | 요약 |
|---|---|---|
| 한글 표시 / KoreanText.cs | 기본 콘텐츠·enum·태그·기존 영어 저장 알림을 화면용 한국어로 변환한다. | [요약](Assets/_Project/Features/Run/Presentation/KoreanText.cs.md) |
| 한글 UI Editor / KoreanUiAuthoring.cs | 지정 프리팹 폰트·고정 문구·기본 글리프와 legacy 폰트 참조를 적용한다. | [요약](Assets/_Project/Features/Run/Editor/KoreanUiAuthoring.cs.md) |
| 한글 UI Tests / KoreanUiTests.cs | 제작 화면·한글 실제 글리프·줄 잘림·기존 저장 불변·사용자 문구를 검사한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/KoreanUiTests.cs.md) |

## 플레이 작업실
[PLAN](../Plans/PLAY_WORKBENCH_PLAN.md), [REPORT](../Reports/PLAY_WORKBENCH_REPORT.md). `Fate Dice > 플레이 작업실`에서 정지 중 실제 데이터 UI 미리보기, 원본 프리팹·설정·씬 찾기, 9개 시작 상황과 시드·시련·등급·화면 비율을 선택한다. 격리 Play는 기존 저장과 분리되며 중지 시 원래 씬으로 돌아온다.

| 스크립트 | 역할 | 요약 |
|---|---|---|
| PlayWorkbenchSession.cs | 실제 규칙 기반 시작 상태, 전용 저장 주입, 요청·씬 복원 수명 | [요약](Assets/_Project/Features/Run/Editor/PlayWorkbenchSession.cs.md) |
| UIWorkbenchPreview.cs | 8종 원본 화면을 실제 데이터로 표시하는 폐기 가능한 Stage | [요약](Assets/_Project/Features/Run/Editor/UIWorkbenchPreview.cs.md) |
| PlayWorkbenchWindow.cs | 한국어 테스트 옵션, 미리보기·원본 편집·격리 실행 도구 | [요약](Assets/_Project/Features/Run/Editor/PlayWorkbenchWindow.cs.md) |
| SceneEntryEditor.cs | 씬 역할·앱 연결과 작업실 진입 Inspector | [요약](Assets/_Project/Features/Run/Editor/SceneEntryEditor.cs.md) |
| PlayWorkbenchTests.cs | 상태 유효성·결정성·원본 보존·두 화면 비율 미리보기 6개 검사 | [요약](Assets/_Project/Features/Run/Tests/EditMode/PlayWorkbenchTests.cs.md) |
## 준비 로비·캠페인 지도·6주사위 창

[PLAN](../Plans/LOBBY_MAP_DICE_PLAN.md), [REPORT](../Reports/LOBBY_MAP_DICE_REPORT.md). Title → 준비 Lobby → InGame의 지도/전투 화면으로 이어진다. 현재 캐릭터는 기존 방랑자 1명이며 영구 성장 규칙은 추후 설계한다. 지도는 실제 ID별 분기/합류와 완료 경로를 표시하고, 노드 도착 후 버튼으로 6개의 주사위를 굴린다. `Fate Dice > 플레이 작업실`에서 준비 로비·캠페인 지도·전투 등 시작점과 6주사위 창 미리보기/원본 편집을 제공한다.

| 스크립트 | 역할 | 요약 |
|---|---|---|
| CampaignMapView.cs | 공개/완료 노드와 실제 연결선, 현재 위치·터치·도착 표시 | [요약](Assets/_Project/Features/Exploration/Presentation/CampaignMapView.cs.md) |
| DiceRollUI.cs | 명시적 굴리기 버튼과 여섯 주사위의 modal 표시 수명 | [요약](Assets/_Project/Features/Run/Presentation/DiceRollUI.cs.md) |
| DiceFaceView.cs | CanvasRenderer를 사용하는 주사위 면·눈금 그리기 | [요약](Assets/_Project/Features/Run/Presentation/DiceFaceView.cs.md) |
| LobbyPreparationAuthoring.cs | 기존 MenuUI 원본을 준비 로비 탭/고정 출전 버튼 구조로 작성 | [요약](Assets/_Project/Features/Run/Editor/LobbyPreparationAuthoring.cs.md) |
| CampaignMapAuthoring.cs | 기존 ExplorationUI와 node 원본에 편집 가능한 지도 구조 작성 | [요약](Assets/_Project/Features/Run/Editor/CampaignMapAuthoring.cs.md) |
| DiceRollAuthoring.cs | 여섯 주사위 popup 원본 작성·renderer 직렬화·UIRoot 등록 | [요약](Assets/_Project/Features/Run/Editor/DiceRollAuthoring.cs.md) |
| LobbyCampaignFlowTests.cs | 준비/실제 노드 터치/굴림/중복 차단/저장 재개/완료 경로 검사 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/LobbyCampaignFlowTests.cs.md) |

## 와일드 카드·조합·전투 피드백

[PLAN](../Plans/COMBAT_FEEDBACK_PLAN.md), [REPORT](../Reports/COMBAT_FEEDBACK_REPORT.md). 와일드 카드는 기존 시작 행동 카드 선택의 표시명이다. 조합 단계는 저장 설정의 priority 순서, 운명력은 확정 상태를 표시한다. 결과 유지 → 선택 카드 강조 → 실제 공격/수호와 적 반응 → 다음 화면을 표시하며 게임 규칙·RNG·저장 schema는 유지한다.

| 소스 | 역할 | 요약 |
|---|---|---|
| CombatFeedbackAuthoring.cs | 세 원본의 문구·결과 영역·피드백 참조를 버전 1로 작성 | [요약](Assets/_Project/Features/Run/Editor/CombatFeedbackAuthoring.cs.md) |
| CombatFeedbackTests.cs | 실제 포인터·저장 oracle·피해/수호·시간·두 세로 비율·중단 복원 | [요약](Assets/_Project/Features/Run/Tests/PlayMode/CombatFeedbackTests.cs.md) |

## RA-D 효과·획득·상점 가격

- [ActionEffects.cs](Assets/_Project/Features/Combat/Runtime/ActionEffects.cs.md): ActionEffectDefinition과 Damage/Block 명시 처리기, EffectResolver가 기존 태그·등급·반올림으로 계산해 합산한다. CombatRules.Evaluate/Resolve가 같은 평가를 사용한다. null/빈 effects는 구형 두 계수, 명시 배열이 권위이며 IsValid가 미지원/음수/비유한/전체0을 거절한다. 규칙 RNG/상태를 쓰지 않고 결과 ActionEffect만 반환한다.
- [ShopRules.cs](Assets/_Project/Features/Shop/Domain/ShopRules.cs.md): ShopOffer(productId,price) 및 ShopRules가 저장된 기본가격×등급배율을 AwayFromZero로 정수화해 입장 snapshot을 만든다. RunApplication이 입장/구매/퇴장 수명을 소유하고 UI는 Offer의 가격으로 표시·가능 여부를 판정한다. LocalRunStore/RunApplication의 RestoreLegacy는 유효한 구형 상점만 당시 기본가격으로 복원한다. 최신 SO를 참조하지 않고 RNG를 쓰지 않는다.
- [ContentExtensionTests.cs](Assets/_Project/Features/Run/Tests/EditMode/ContentExtensionTests.cs.md): RA-D 콘텐츠/효과/가격 계약 EditMode 검사. 독립 30피해·23수호, 명시효과 검증, 실제 보물획득→장비→Roll추첨→사용→디스크재개, 중복 소유/RNG, 등급별 정확가격/실패저장/구형JSON/복사 독립성을 검증한다. 격리 TEMP 저장만 사용하고 사용자 저장은 접근하지 않는다.
- [DiceDistributionTests.cs](Assets/_Project/Features/Run/Tests/EditMode/DiceDistributionTests.cs.md): 독립 수학 분포와 실제 200만 RNG 표본 검증
- [ContentExtensionFlowTests.cs](Assets/_Project/Features/Run/Tests/PlayMode/ContentExtensionFlowTests.cs.md): 실제 보상·장비·지도·굴림·카드·상점 버튼의 획득/피해/표시가격/결제/저장재개를 검증한다.


## 주사위 조합 연출

- [DiceFeedbackCatalog.cs](Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.cs.md) — 조합 ID별 색·Text Animator 태그·확대량을 편집하는 표시 전용 SO. DiceHandFeedbackStyle도 같은 파일에 정의한다.
- [DiceResultFeedback.cs](Assets/_Project/Features/Run/Presentation/DiceResultFeedback.cs.md) — 이미 확정된 조합 이름과 색을 TMP/FEEL/All In 1 UI ribbon으로 표시한다.
- [SelectionFeedback.cs](Assets/_Project/Features/Run/Presentation/SelectionFeedback.cs.md) — 선택한 카드 한 장의 scale/color를 FEEL로 강조한다.
- [DicePresentationAuthoring.cs](Assets/_Project/Features/Run/Editor/DicePresentationAuthoring.cs.md) — 정확한 주사위/행동/운명 카드 원본 3개에 조합 표시/한 줄/FEEL 선택 효과를 작성하는 Editor 전용 migration.
- [DicePresentationEffectsTests](Assets/_Project/Features/Run/Tests/PlayMode/DicePresentationEffectsTests.cs.md) — 한 행/자산/색/이름/priority/저장/취소·0초 실제 prefab 검증.
- [PlayWorkbenchTests.cs](Assets/_Project/Features/Run/Tests/EditMode/PlayWorkbenchTests.cs.md) — 비활성 Canvas에서 바인딩한 정지 Dice preview의 실제 메시/한 행/원본 보존 회귀.
- [PresentationProbe.cs](artifacts/dice-presentation-effects/PresentationProbe.cs.md) — Assets 밖의 실제 Unity 버튼 연출 캡처 도구. 게임 명령은 실행하지 않는다.

Runtime/Editor는 Unity.TextMeshPro, Febucci Runtime/TMP, MoreMountains.Tools를 참조한다. Core 경계는 유지한다. 결과: [PLAN](../Plans/DICE_PRESENTATION_EFFECTS_PLAN.md), [REPORT](../Reports/DICE_PRESENTATION_EFFECTS_REPORT.md).

## 주사위별 조합 오라

- [DiceComboHighlights.cs](Assets/_Project/Features/Run/Presentation/DiceComboHighlights.cs.md): 확정 hand를 구성하는 최소 주사위 index/묶음만 표시용으로 선택한다. 게임 판정·RNG 불변.
- [DiceAuraGraphic.cs](Assets/_Project/Features/Run/Presentation/DiceAuraGraphic.cs.md): Canvas 링·원형 문양·색 번짐·반짝임, 입력 비차단 및 정착 후 재구성 중지.
- [DiceAuraTests.cs](Assets/_Project/Features/Run/Tests/PlayMode/DiceAuraTests.cs.md): 독립 참여 index, 실제 오라 메시·opacity·범위·초기화, 사용자 색 migration 보존.
- [AuraPreviewCapture.cs](artifacts/dice-combo-aura/AuraPreviewCapture.cs.md): 실제 prefab의 세 조합/두 세로 비율 시각 fixture를 임시 PreviewScene에서 캡처한다.

DiceResultFeedback가 기존 catalog의 대표색/보조색과 저장 priority 강도를 전달하며 DicePresentationAuthoring.ApplyAuras가 DiceRollUI 원본에 작성한다. 관련 [PLAN](../Plans/DICE_COMBO_AURA_PLAN.md), [REPORT](../Reports/DICE_COMBO_AURA_REPORT.md).

## 등급별 연출과 시작·종료 동기화

[PLAN](../Plans/PRESENTATION_LIFECYCLE_PLAN.md), [REPORT](../Reports/PRESENTATION_LIFECYCLE_REPORT.md). 일반 링부터 최상위 다중 파동까지 네 타임라인을 설정하고 실제 완료 후 전투·화면을 진행한다. 기존 hold는 최소 읽기 시간, 0은 생략이다. 전체 시퀀스가 10초를 넘으면 오류 로그와 함께 정리하고 확정 상태를 복구한다.

- [PresentationPlayback.cs](Assets/_Project/Features/Run/Presentation/PresentationPlayback.cs.md): 중첩 IEnumerator 감독, 시작/종료 시각과 사건, Completed/Cancelled/Faulted/TimedOut 및 세대별 정리.
- [PresentationPlaybackTests.cs](Assets/_Project/Features/Run/Tests/PlayMode/PresentationPlaybackTests.cs.md): 시간 경계·중첩·예외·취소·재진입·이벤트 16검사.
- [PresentationFlowTests.cs](Assets/_Project/Features/Run/Tests/PlayMode/PresentationFlowTests.cs.md): 등급 구조/모션, 실제 FEEL 종료, paused10초 강제 종료·로그·저장, 재바인딩 소유권 9검사.
- [TierPreviewCapture.cs](artifacts/presentation-lifecycle/TierPreviewCapture.cs.md): 네 연출 단계·두 세로 비율의 실제 프리팹 정지 캡처.

RunUIController→PresentationPlayback→DiceRollUI/SelectionFeedback/CombatUI의 실제 완료 수명이다. DiceResultFeedback→DiceFeedbackCatalog의 편집 가능한 tier→DiceAuraGraphic 순서로 표시하며 게임 규칙·저장 schema에 연결하지 않는다. 노드 도착 기간은 CampaignMapView 원본에서 편집한다.

## 절차 캠페인 지도와 운명 선택 팝업

[PLAN](../Plans/PROCEDURAL_CAMPAIGN_PLAN.md), [REPORT](../Reports/PROCEDURAL_CAMPAIGN_REPORT.md). 신규 모드1은 전체 층별 분기·합류와 기본 일반10층+단일보스, 구형 모드0 저장은 원래 생성 규칙을 유지한다. 지도 전체의 위치·연결을 표시하고 기본 가까운2층의 유형만 공개한다. 운명카드는 지도 위 별도 팝업에서 강조 선택 후 확정한다. 최초 노드 미리보기/이동 버튼 계약은 아래 CAMPAIGN_FLOW_POLISH의 즉시 탭 이동으로 대체됐다. 실제 실행/화면 검증 상태는 REPORT를 따르며 아래 목록은 PASS 증거가 아니다.

| 소스 | 역할과 직접 관계 | 요약 |
|---|---|---|
| ProceduralMapGenerator.cs | 초기 시드에서 분리한 지도 난수로 가변 분기·합류·교차 없는 다음 층 연결과 단일보스를 생성한다. ExplorationRules가 모드1에서 사용한다. | [요약](Assets/_Project/Features/Exploration/Runtime/ProceduralMapGenerator.cs.md) |
| ProceduralMapTests.cs | 여러 시드·일반1~100층·도달성·분기/합류·난수 분리·규칙 스냅샷·구형 저장·실패 원자성을 검사한다. | [요약](Assets/_Project/Features/Run/Tests/EditMode/ProceduralMapTests.cs.md) |
| CampaignMapProjection.cs | 확정 활성/기록 그래프를 공개 DTO로 변환하고 먼 type을 제거한다. Controller와 정지 Workbench가 공유한다. | [요약](Assets/_Project/Features/Run/Presentation/CampaignMapProjection.cs.md) |
| CampaignPathGraphic.cs | 공개 지도 연결의 점선을 입력 비차단 Graphic으로 그린다. CampaignMapView가 위치·회전·색을 전달한다. | [요약](Assets/_Project/Features/Exploration/Presentation/CampaignPathGraphic.cs.md) |
| MapNodeGraphic.cs | 노드 유형·상태의 원형 표식과 현재 위치를 Canvas에 그린다. ExplorationNodeView가 사용한다. | [요약](Assets/_Project/Features/Exploration/Presentation/MapNodeGraphic.cs.md) |
| FateChoiceUI.cs | 공개 후보·여섯 주사위·강조 선택·단일 확정과 실제 선택 연출/퇴장·바인딩 수명을 소유하는 modal이다. | [요약](Assets/_Project/Features/Fate/Presentation/FateChoiceUI.cs.md) |
| FateChoiceUIData.cs | 공통 HUD/context와 공개 offers, confirm/close 요청을 담는 운명 popup 전용 DTO다. | [요약](Assets/_Project/Features/Fate/Presentation/FateChoiceUIData.cs.md) |
| FateChoiceAuthoring.cs | 승인된 FateCard 원본·새 FateChoiceUI popup·UIRoot 등록을 Editor API로 작성한다. | [요약](Assets/_Project/Features/Run/Editor/FateChoiceAuthoring.cs.md) |
| FateChoicePopupTests.cs | 실제 popup의 탭/확정·중복 입력·후보 교체·바인딩·카드/주사위 배치와 종료 수명을 검사한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/FateChoicePopupTests.cs.md) |
| ProceduralCampaignAuthoring.cs | 지도/운명 작성기를 순서대로 호출하고 기존 기본 설정의 새 지도 필드를 모드1로 전환한다. 기존 저장/이미 작성된 사용자 값은 보존한다. | [요약](Assets/_Project/Features/Run/Editor/ProceduralCampaignAuthoring.cs.md) |
| ProceduralCampaignFlowTests.cs | 실제 제작 화면의 공개 지도·이동/굴림/운명 확정·재개/재굴림·소유 binding/제한 시간 흐름을 검사한다. | [요약](Assets/_Project/Features/Run/Tests/PlayMode/ProceduralCampaignFlowTests.cs.md) |

직접 연결: [RunUIController](Assets/_Project/Features/Run/Presentation/RunUIController.cs.md) → CampaignMapProjection → [ExplorationUI](Assets/_Project/Features/Run/Presentation/ExplorationUI.cs.md) → [FateDiceWidgets](Assets/_Project/Features/Run/Presentation/FateDiceWidgets.cs.md) → [CampaignMapView](Assets/_Project/Features/Exploration/Presentation/CampaignMapView.cs.md). 노드·카드 게임 명령은 Controller/RunSession 경계에 남는다. FateChoiceUI의 확정 후 FEEL·퇴장 완료는 기존 PresentationPlayback의 실시간10초 감독과 binding별 정리를 사용하고, 선택 대기는 제한 시간에서 제외한다. [UIWorkbenchPreview](Assets/_Project/Features/Run/Editor/UIWorkbenchPreview.cs.md)는 같은 공개 변환과 실제 등록 원본을 폐기용 Stage에 표시한다.

## 캠페인 복귀·즉시 이동·전투 진입 (2026-09-11)

[PLAN](../Plans/CAMPAIGN_FLOW_POLISH_PLAN.md), [REPORT](../Reports/CAMPAIGN_FLOW_POLISH_REPORT.md). CampaignMapView는 화면 복귀의 최종 레이아웃 후 현재 노드·플레이어·다음 경로로 한 번 초점을 맞추고, ExplorationUI는 노드 탭을 단일 이동 요청으로 연결한다. Controller는 새 전투 화면→CombatUI.PlayEntry 실제 완료→DiceRollUI 순서를 기존 PresentationPlayback에 통합한다. 다음 턴은 진입 연출을 반복하지 않는다.

- [CampaignFlowAuthoring.cs](Assets/_Project/Features/Run/Editor/CampaignFlowAuthoring.cs.md): ExplorationUI/CombatUI 두 원본만 한 번 작성하며 편집값을 보존한다.
- [CampaignFlowPolishTests.cs](Assets/_Project/Features/Run/Tests/PlayMode/CampaignFlowPolishTests.cs.md): 실제 포인터 이동·복귀 초점·전투 진입·저장/취소/timeout 경계 검증. 구조 설명은 실행 PASS를 대신하지 않는다.
