# UIWorkbenchPreview.cs

## RA-A 현재 계약 (2026-09-09)

OpenDice의 duration은 state.config.presentation.DiceTiming(combat).rollSeconds를 사용한다. runtime과 같은 탐험/전투 표시 설정 선택이며 숨은 .65 최소값 없음. 정지 미리보기는 rolling=false이고 RNG/저장/게임 명령 없음.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.


- COMBAT_FEEDBACK 변경: 로비 안내의 시련 카드를 와일드 카드로 수정했다. Hand(state)는 KoreanText.HandSummary를 사용하고 주사위 popup은 multiline=true의 두 줄 조합 단계/운명력을 표시한다. 전투 handLabel은 폭이 제한된 HUD용 HandStage를 사용하고 운명력은 기존 별도 표시를 유지한다. 이미 확정된 예시 상태만 읽고 새 추첨·저장을 추가하지 않는다. 실제 실행 증거는 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다.

- 역할: 정지한 Editor에서 실제 UIRoot와 선택한 화면 원본을 복제해 한 번 표시하는 폐기용 PreviewSceneStage다. 미리보기 편집은 저장하지 않으며 원본 Prefab Stage 편집과 분리한다.
- 입력/API: Open(WorkbenchOptions, int height=1280)은 열린 UIWorkbenchPreview를 반환한다. SourcePrefabPath는 선택한 등록 화면 원본 경로, PreviewRoot는 복제한 UIRoot GameObject의 읽기 전용 참조다. 높이는 양수, 논리 폭은 720이다.
- 열기: Play/전환/컴파일/import/기존 Prefab Stage를 거절한다. PlayWorkbenchSession.Build 결과와 앱의 uiRootPrefab/uiPrefabs/visuals 및 선택 화면 등록을 검증한다. 이전 UIWorkbenchPreview를 닫고 새 Stage에 진입한 뒤 클론을 구성하며 실패하면 해당 Stage/클론을 정리한다.
- 복제/입력: 비활성 임시 부모에서 UIRoot를 생성하고 활성화 전에 BaseInputModule/EventSystem을 제거한다. CanvasScaler/GraphicRaycaster는 클론에서 비활성화한다. WorldSpace Canvas 720×height와 전체 safeArea, 입력 잠금, Navigation.None을 적용한다. GameApplication/RunUIController는 원본 연결을 읽고 검사할 뿐 생성하거나 초기화하지 않는다. LocalRunStore를 만들지 않는다.
- 화면: Title/Lobby는 TitleUI/MenuUI, Map은 ExplorationUI, 탐험카드는 ExplorationUI 위 FateChoiceUI popup, Combat은 CombatUI, Shop은 EncounterUI, Reward/Equipment/Result는 해당 전용 화면을 사용한다. registry의 실제 원본 타입으로 UIManager.Show/ShowPopup을 호출한다.
- 표시 데이터: 실제 config/RunState에서 RunUIContext와 각 typed UIData를 만든다. CombatRules/GrowthRules의 HP·의도 수치·효과를 사용하고, 탐험 FateOfferUIData에는 공개 type/grade/offered ID만 전달한다. KoreanText로 기본 문구를 표시하며 장비/보상/족보도 실제 설정을 읽는다. UIChoiceData와 카드/주사위에는 게임 콜백을 연결하지 않는다.
- 레이아웃: Canvas와 scroll content를 명시적으로 재배치하고 전투 카드 너비는 실제 viewport/최대 3개 가시 열에 맞춘다. scroll은 시작 위치로 놓고 SceneView를 2D/720×height 경계로 프레이밍한다. 원본 sprite/font/씬/프리팹을 저장하거나 수정하지 않는다.
- 닫기/수명: StageUtility.GoToMainStage가 OnCloseStage를 실행한다. PreviewRoot 전체를 DestroyImmediate하고 base Stage 정리를 호출한다. 정지 모드에서는 runtime Destroy를 쓰는 UIManager.CloseAll/Widgets.Clear 경로를 직접 실행하지 않으며, 개별 child OnDestroy의 지역 listener 정리는 유지한다.
- 관계: PlayWorkbenchWindow는 preview 열기/닫기 진입점, PlayWorkbenchSession은 메모리 예시 생성, UIRoot/UIManager/8개 View/RunUIData/TitleUIData/FateDiceWidgets는 원본 표시 계층이다. KoreanText/FateDiceVisualCatalog와 CombatRules/GrowthRules만 표시 변환에 사용한다. 제작 런 컨트롤러의 게임 실행 책임을 가져오지 않는다.
- 이전 PLAY_WORKBENCH 작업 검수: 당시 EditMode 128/128 통과(작업실 검사 6개 포함). 실제 실행·직접 클릭 및 보존 경계는 Docs/Reports/PLAY_WORKBENCH_REPORT.md를 참조한다.

- 표시 보조: SceneView 격자는 미리보기 중 숨기고 종료 시 이전 값을 복원한다.

## 로비·캠페인·주사위 창 (LOBBY_MAP_DICE)
- Open은 기존 9개 시작점/8개 base 화면을 그대로 사용한다. OpenDice(options,height)는 별도 API로 실제 Build 복사본의 Map→ChooseNode 도착 또는 Combat 고정 결과를 배경 화면과 등록된 DiceRollUI popup에 표시한다. 롤 명령/저장/Play 요청을 생성하지 않고 모든 클릭 콜백을 비운다.
- Dice preview의 SourcePrefabPath는 UIManager registry의 DiceRollUI 실제 원본 경로다. PreviewRoot와 정리/소스보존 수명은 기존과 같다.
- Lobby characterName/characterDetails/growthDetails는 실제 config의 출발 능력치·기본 행동/주사위·레벨 성장 데이터를 표시하며 영구 성장은 추후 제공임을 설명한다. seed 필드는 MenuUI에서 숨긴다.
- Map은 하단 현재 위치로, 탐험 카드와 나머지 세로 콘텐츠는 상단으로 스크롤한다. popup preview의 six faces는 실제 고정값 또는 아직 굴리지 않은 0이며 cosmetic roll을 자동 실행하지 않는다.
- 구형 모드0 완료 경로: ExplorationUIData.completedNodes는 resolvedEventIds 순서대로 nodeHistory에서 찾은 완료 노드만 복사한다. 각 복사본의 id/type/childIds를 보존하며, 버린 가지 전체를 완료 경로로 전달하지 않는다. 신규 모드1은 아래 절차 지도 공개 projection을 사용한다.
- C01 레이아웃 보정: 정지 미리보기에는 CampaignMapView의 runtime LateUpdate가 실행되지 않으므로, Canvas/scroll의 최종 배치를 끝낸 뒤 모든 map.RefreshLayout을 명시 호출하고 Canvas를 다시 재배치한다. 임시 viewport 너비로 잡힌 노드 위치를 확정된 authored viewport 너비에 맞춘다. 프리팹이나 씬의 원본 저장은 하지 않는다.
- 직접 관계 추가: CampaignMapView/ExplorationUI, DiceRollUI/DiceRollUIData. 이번 변경 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.

## RA-C 직접 관계
기존 GameConfigData와 PresentationSettings 표시 API 유지. GameConfigData.DeepCopy는 규칙/표시를 각각 명시 복사하며 Core에 표시 타입이 들어가지 않는다.


## 주사위 조합 연출 (2026-09-10)

주사위 정지 미리보기에 실제 조합 label/hand/priority 순위를 전달한다. holdSeconds=0으로 vendor 재생 없이 한글 TMP 이름/조합 색을 확인한다. 원본 prefab의 DiceResultFeedback을 사용하고 저장/새 게임 명령은 없다.
검증 상태: DICE_PRESENTATION_EFFECTS_REPORT의 실제 결과를 따른다.

## 절차 지도·운명 팝업 미리보기 (2026-09-10)

- 메모리 예시 상태를 PlayWorkbenchSession.Build로 만든 뒤 runtime과 같은 CampaignMapProjection.Apply → ExplorationUI 경로를 사용한다. 신규 모드1의 전체 분기/합류·가까운 공개 유형·단일보스·접근 불가 기록을 표시하고 모드0 예시는 기존 짧은 지도 표시를 유지한다.
- ExplorationCards 예시는 지도 위 등록된 FateChoiceUI/FateChoiceUIData를 표시한다. offers에는 id/type/grade만 있고 confirm/close/주사위 콜백은 연결하지 않는다. SourcePrefabPath도 해당 FateChoiceUI 원본을 가리켜 작업실의 원본 편집과 표시 대상이 일치한다. OpenDice는 기존 별도 DiceRollUI 원본 경로를 유지한다.
- Canvas와 ScrollRect의 최종 배치 뒤 CampaignMapView.RefreshLayout 및 FateChoiceUI.RefreshLayout을 명시 호출한다. 정지 Editor에 runtime LateUpdate가 없더라도 실제 authored viewport 크기로 노드와 카드 너비를 다시 계산한다. 논리 폭720과 요청 높이의 두 세로 비율을 확인하는 표시 경로다.
- 소유권: Stage/클론/지역 listener만 폐기하고 실제 Scene/Prefab/설정/사용자 저장은 쓰지 않는다. 예시 구성은 격리된 규칙 실행을 사용할 수 있지만 preview의 View 입력으로 게임 명령을 실행하지 않으며 live Controller/LocalRunStore를 만들지 않는다.
- 직접 관계 추가: CampaignMapProjection, CampaignMapView, ExplorationUI, FateChoiceUI/FateChoiceUIData, UIManager popup registry. 새 Scene이나 runtime 관리자 탐색을 추가하지 않는다.
- 검수 주의: 실제 원본 등록, 원본 보존, 공개 데이터·카드 수·레이아웃 및 Stage 종료를 확인한다. 정지 구조 확인과 실행/모바일 조작 검증은 구분하며 실제 결과는 Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md를 따른다.
