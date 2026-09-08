# CodeMap 기능 색인

실제 로컬 플레이 루프, 저장/이어하기, 전용 Scene과 설정 SO의 직접 연결을 구현했다. 아래 68개 프로젝트 소유 C#은 각각 같은 상대 경로의 요약 문서와 대응한다. 실행 증거와 조작·밸런싱 안내는 [REPORT](../Reports/FATE_DICE_PROTOTYPE_REPORT.md), 고정 단계와 Finding 이력은 [PLAN](../Plans/FATE_DICE_PROTOTYPE_PLAN.md)에 있다.

| 기능 / 스크립트 | 한국어 역할과 직접 관계 | 요약 |
|---|---|---|
| Fate / FateDiceConfig.cs | SO의 모든 규칙·콘텐츠·표시 설정을 검증하고 새 런 스냅샷을 제공. Screen·RunSession·저장 검사가 사용 | [요약](Assets/_Project/Features/Fate/Configs/FateDiceConfig.cs.md) |
| Dice / DiceRules.cs | 단일 규칙 RNG, 가중 추첨, 6D6와 10패 판정. RunSession·카드 생성·저장 검사가 사용 | [요약](Assets/_Project/Features/Dice/Runtime/DiceRules.cs.md) |
| Fate / FateCardRules.cs | 탐험·행동 카드 3장의 독립 등급과 콘텐츠 확정. RunSession이 호출, 전투 평가와 저장이 결과 사용 | [요약](Assets/_Project/Features/Fate/Runtime/FateCardRules.cs.md) |
| Run / RunState.cs | 런 전체 상태·설정 스냅샷·노드 ID·보상·최종 기록·저장 봉투 DTO. 모든 규칙과 저장이 공유 | [요약](Assets/_Project/Features/Run/Runtime/RunState.cs.md) |
| Run / RunSession.cs | 명령별 상태 복제→규칙 적용→저장→확정. Screen의 행동을 Combat·Exploration·Growth에 전달 | [요약](Assets/_Project/Features/Run/Runtime/RunSession.cs.md) |
| Combat / CombatRules.cs | 의도·행동의 실제 피해/보호량과 생존 순서. Growth의 스탯/태그 보정을 사용하며 Screen과 평가식 공유 | [요약](Assets/_Project/Features/Combat/Runtime/CombatRules.cs.md) |
| Exploration / ExplorationRules.cs | 연결된 분기 노드와 2단계 앞길 생성·정리, 임계값 다음 보스 경로. RunSession이 진행시 호출 | [요약](Assets/_Project/Features/Exploration/Runtime/ExplorationRules.cs.md) |
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
| UI 구조 / ExplorationUI.cs | 현재/미래/완료 경로의 세로 Map과 운명 카드를 캐시 화면에서 표시한다. 굴림은 별도 popup이다. RunScreenView<ExplorationUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/ExplorationUI.cs.md) |
| UI 구조 / MenuUI.cs | 준비 로비의 캐릭터·세팅·성장 안내 탭과 새 여정·이어하기를 바인딩한다. 시드는 작업실에서 편집한다. RunScreenView<MenuUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/MenuUI.cs.md) |
| UI 구조 / ResultUI.cs | 런 결과 요약/선택 등급 기록과 재시작 요청을 표시한다. RunScreenView<ResultUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/ResultUI.cs.md) |
| UI 구조 / RewardUI.cs | 공개 artwork, 보상 설명/진행 안내와 명시적 Claim 입력을 표시한다. RunScreenView<RewardUIData> 파생 화면이다. | [요약](Assets/_Project/Features/Run/Presentation/RewardUI.cs.md) |
| UI 구조 / RunScreenLayout.cs | 전체 화면 prefab 안의 common HUD Text(header/stats/situation/fate/notice/gear), diceRow/body/footer, ScrollRect의 serialized 직접 참조와 Validate를 제공한다. | [요약](Assets/_Project/Features/Run/Presentation/RunScreenLayout.cs.md) |
| UI 구조 / RunScreenView.cs | RunScreenView<TData>는 BaseUI<TData>와 IRunScreenView를 연결하는 공통 HUD 바인더다. IRunScreenView.Widgets가 현재 화면의 진단 경계를 제공한다. | [요약](Assets/_Project/Features/Run/Presentation/RunScreenView.cs.md) |
| UI 구조 / RunUIController.cs | 앱 루트 또는 legacy prototype에서 런/저장/화면 데이터를 소유하고 명시적 씬 이동을 요청한다. | [요약](Assets/_Project/Features/Run/Presentation/RunUIController.cs.md) |
| UI 구조 / RunUIData.cs | 화면별 런타임 표시 DTO. UIData→RunUIData(context,hud)→Menu/Exploration/Combat/Encounter/Reward/Equipment/ResultUIData를 정의한다. ScriptableObject나 저장 모델이 아니다. | [요약](Assets/_Project/Features/Run/Presentation/RunUIData.cs.md) |
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