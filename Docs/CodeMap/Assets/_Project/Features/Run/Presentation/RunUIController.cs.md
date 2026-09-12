# RunUIController.cs

## 메타 진행 현재 계약 (2026-09-11)

partial 클래스의 RunUIController.Meta에 영구 진행 UI 조정을 분리한다. Initialize의 마지막 선택 인자로 IMetaProgressionService를 받으며 같은 RunStore만 허용한다. Busy는 meta 명령도 포함하고 OnDisable은 metaViewVersion을 증가시켜 늦은 응답의 화면 접근을 차단한다. 메타 주입 시 RenderMenu/StartNewJourney/Result 정산을 해당 partial로 위임한다. 미주입 테스트/작업실/legacy facade의 기존 런 흐름은 유지한다. ReadSavedPreview는 Exists 자체의 손상 오류도 UI로 보고한다. META_PROGRESSION_REPORT 참조.

## 캠페인 이동·복귀와 전투 진입 (2026-09-11)

- 현재 입력 계약은 아래 절차 지도 작업의 노드 미리보기/이동 버튼을 대체한다. ExplorationUI의 이동 가능한 노드 탭→TravelToNode→ChooseNode 저장1회→실제 도착 연출→탐험 주사위 창이다. 지도 초점은 CampaignMapView가 공개 currentId와 활성화/최종 레이아웃을 사용해 복귀 때 한 번 맞춘다. Controller가 지도 위치나 런 상태를 새로 계산하지 않는다.
- Show의 openDice와 Render의 deferCombatDice는 전투 화면을 먼저 보이게 하는 내부 표시 옵션이다. Fate 선택에서 새 CombatRoll로 바뀌면 기존 선택/퇴장 종료→Render(true)→PlayCombatEntry→주사위 창을 한 PresentationPlayback 안에서 진행한다. 보스 노드 직접 이동은 PresentNodeArrival에서 도착→같은 진입 순서를 기다린다.
- Cold CombatRoll 재개는 화면 교체 시 EnterCombatScreen이 입력 잠금과 독립 PresentationPlayback을 소유한다. 이미 보이는 CombatUI의 다음 턴/RefreshView와 CombatCards 재개에는 진입 연출을 반복하지 않는다. 진입 연출은 추가 Roll/저장/RNG를 실행하지 않는다.
- PlayCombatEntry는 CombatUI의 실제 PlayEntry IEnumerator 완료를 기다리고 view 활성/IsOpen/EntryBindingVersion/현재 화면/runId/sequence/CombatRoll을 확인한 뒤 기존 ShowDiceWindow(false)를 호출한다. 고정 WaitForSeconds를 Controller에 추가하지 않는다.
- presentingEntry와 shownCombat의 바인딩 소유를 구분한다. 일반 RefreshView는 진입 재생 중 재바인딩하지 않는다. disable/cancel/timeout/fault는 소유 연출만 원복하며 새 Combat binding의 화면·주사위 창을 닫지 않는다. 화면 자체를 disable한 경우에도 숨겨진 화면 위로 새 주사위 창을 열지 않는다.
- 기존10초 실시간 watchdog은 선택/도착+진입 전체를 감독하고 timeout 로그와 확정 상태 복구를 유지한다. 상위 Coroutine finally는 소유 entry/Busy를 해제한다. 직접 관계: RunUIController→CombatUI.PlayEntry/ResetEntry/EntryBindingVersion, 기존 PresentationPlayback·UIManager·DiceRollUI. 실제 결과는 CAMPAIGN_FLOW_POLISH_REPORT를 따른다.

## 절차 지도·운명 팝업 현재 계약 (2026-09-10)

- 역할: 기존 RunSession/IRunStore의 확정 상태를 typed UI에 연결하는 런 표현 조정자다. 신규 지도 생성은 Core, 공개 범위 계산은 CampaignMapProjection, 지도 탭 미리보기는 ExplorationUI, 운명 카드의 선택 상태는 FateChoiceUI가 담당한다.
- Render의 Map/ExplorationRoll/ExplorationCards는 ExplorationUI를 유지한다. CampaignMapProjection.Apply가 활성 nodes와 nodeHistory의 전체 그래프·실제 HUD를 공개 DTO로 바꾸며 모드0은 기존 짧은 지도 경로를 유지한다. inline fates는 빈 배열이다. 기본 신규 설정은 일반10층과 단일보스이며 렌더가 지도/RNG를 생성하지 않는다.
- 이동은 TravelToNode의 Busy 잠금 → token을 캡처한 ChooseNode 1회 및 저장 → 실제 노드 도착 연출 → 새 상태 표시 순서다. 노드 탭만으로 이동하지 않으며 지도 하단 이동 버튼이 이 콜백을 실행한다. 노드 도착 후 별도 주사위 창의 굴리기 버튼을 사용한다.
- ExplorationCards는 공개 id/type/grade만 담은 FateChoiceUIData를 ShowPopup한다. 팝업 내부 탭은 강조 선택만 하고 confirm에서 현재 popup 객체·BindingVersion·최상단·Busy를 확인한 뒤 token을 캡처한 ChooseFate를 1회 호출한다. 닫기/뒤로는 popup만 닫으며 지도 ‘운명 선택 열기’가 기존 후보를 다시 표시한다. 닫기·재개가 명령/저장/RNG를 변경하지 않는다.
- 유료 재굴림은 기존 RunHUDData.dieClicked → Reroll(index,token) 명령과 소모 규칙을 사용한다. 확정된 여섯 결과를 DiceRollUI에서 한 줄로 표시하고 완료 후 새 후보로 FateChoiceUI를 재바인딩해 선택을 해제한다. 새 추첨은 게임 명령에만 있다.
- Perform은 확정 전/후 snapshot과 현재 FateChoiceUI/BindingVersion을 캡처한다. 명령 성공 뒤 FateChoiceUI.PlaySelection의 실제 선택 FEEL 종료와 퇴장을 기다리고 소유한 최상단 popup을 닫은 후 사건을 표시한다. 전투는 기존 CombatUI/SelectionFeedback 경로를 유지한다. 카드 선택 대기는 PresentationPlayback의 제한 시간에 들어가지 않는다.
- 수명: 연출 중인(presentingFate) popup과 화면에 연결한(shownFate) popup의 binding을 추적한다. 정상 완료/재굴림 완료/OnDisable/timeout·fault·cancel 정리에서 새 binding의 Fate popup을 닫거나 다시 Render하지 않는다. OnDisable은 Cancel이 iterator 필드를 지우기 전에 소유 참조를 캡처한다. OnEnable도 새 소유자가 바인딩한 열린 Fate popup을 덮어쓰지 않는다.
- PresentSafely는 전체 실행 연출을 PresentationPlayback의 실시간 10초 제한으로 감독한다. timeout은 label/elapsed/limit 오류 로그와 안내를 남기고 소유 효과·입력 잠금을 정리하며 확정 snapshot을 복구한다. fault는 기존 warning, cancel은 강제 성공 처리 없이 종료한다. 복구가 ChooseFate/보상/RNG/저장을 다시 실행하지 않는다.
- 직접 관계 근거: Render → CampaignMapProjection/ExplorationUI/FateChoiceUIData; confirm → Command/Perform → Session.ChooseFate; PresentChange → FateChoiceUI.PlaySelection; OnDisable/PresentSafely → BindingVersion 검사 및 UIManager.CloseTopPopup. RunSession·SceneFlow·GameApplication의 기존 책임은 유지한다.
- 검수 주의: JsonUtility의 빈 selectedNode는 Projection이 처리한다. 선택 전 구체 사건 데이터가 없는지, stale token/binding·중복 확인·재굴림·닫기·10초 종료가 확정 상태를 보존하는지 확인한다. 현재 실행 상태는 Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md를 따른다. 아래 작업별 기록은 새 검증 PASS를 대신하지 않는다.

## RA-B 현재 계약 (2026-09-09)

IRunStore를 주입하고 Session.ReadSnapshot에서 기존 typed 표시 DTO를 만든다. UI의 playedSeconds/lastResult 직접 수정과 직접 초기/자동 Save를 제거했다. 시간은 RecordElapsed, 메뉴/pause/quit 저장은 SaveCheckpoint, 새 런은 New(...store,previousResult)가 담당한다. 화면 snapshot의 RunCommandToken을 노드/굴림/재굴림/카드/보상/상점/장비/상한 callback에 캡처한다. Update의 phase 조회는 복제 없는 Session.Phase이다. 명령 실패와 저장 성공 후 표현 실패를 분리한다. PresentSafely는 중첩 IEnumerator를 구동해 연출·Render 예외를 포착하고 finally로 해제하며 동일 확정 상태를 한 번 다시 표시한다. RefreshView는 bool 결과와 함께 명령/저장/RNG 없이 다시 표시할 수 있다. 표현 실패 warning은 Run presentation prefix. 초기 자산 검증·기존 UIManager/프리팹 연결은 유지한다.

실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다. 기존 기록은 이번 실행 증거를 대신하지 않는다.


## RA-A 현재 계약 (2026-09-09)

Initialize(store, navigation, seedSource)의 마지막 선택 인자로 공급자를 주입한다. 기본 SystemSeedSource, Seed setter는 FixedSeedSource 선택/0 거절, getter는 공급자를 호출하지 않는다. StartNewJourney만 NextSeed를 한 번 사용하고 저장 성공 뒤 표시 seed/session을 확정한다. Render/Continue/TryEnterInGame은 기존 저장 RNG/확정 카드를 사용한다. 탐험·전투/재굴림의 DiceTiming을 읽어 굴림→CompleteRoll→결과 유지→카드 표시를 순서대로 진행하고 0은 Wait를 생략한다. 동작/시간만 변경하며 게임 명령·저장 결과는 재계산하지 않는다. 초기화·새 런·UI 수명은 기존 계약을 유지한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 역할: config/로컬 저장/RunSession과 typed UIData 생성 및 게임 명령을 소유한다. 제작 씬에서는 GameApplication의 persistent controller, 기존 FateDiceScreen에서는 독립 prototype controller다.
- 초기화: initializeOnAwake=true는 기존 Awake 경로, 앱 prefab은 false로 명시적 Initialize(store,navigation). config/font/catalog/반복 원본/UIRoot를 검증하고 자신의 자식 UIManager를 사용한다. 중복 Initialize 거절.
- 입력/출력: Seed, Session/Store/PreviewConfig/Widgets/Busy 기존 API. UseStore는 격리 저장 주입 및 메뉴 재바인딩. EnterTitle(Action)/EnterLobby/TryEnterInGame은 scene flow의 역할 진입 훅.
- 이동/시간: 관리형 new/continue/menu는 IRunSceneNavigation을 요청. new는 Store.Save 성공 후 Session 확정, continue는 실제 디스크 재로드. menu 복귀는 시간 저장 성공 후 요청한다. Title/Lobby 및 씬 이동 중 시간 누적 없음; pause/quit은 InGame만 시간 저장.
- 게임 명령: actionBusy와 scene navigation.IsTransitioning을 합쳐 Busy로 중복 입력 차단, SyncInputLock은 UIManager에 전달. 기존 RunSession checkpoint와 realtime 지연 유지. 닫기/scene entry가 보상/장비/진행을 적용하지 않는다.
- 데이터: CombatRules/GrowthRules 실제 표시 계산, public node snapshot, offered/original ID 구분 및 선택 전 private event art 비공개 유지. View는 RunState/store/SceneManager를 받지 않는다.
- 실패: ReportNavigationError는 현재 화면 notice/Title status에 표시. 직접 InGame은 Session이 없을 때만 저장 로드, 없음/손상 false로 Flow의 Lobby fallback 유도. SaveElapsedTime 실패 시 메뉴 이동 중단.
- 수명/관계: GameApplication 또는 legacy FateDiceScreen이 소유. SceneFlowController ↔ 명시적 역할/이동 계약, UIManager/8개 화면/RunUIData/TitleUIData/LocalRunStore/RunSession 사용. OnDestroy는 소유 UI만 닫는다.
- 검수: 기존 122Edit/37Play와 새로운 실제 씬 이동·저장 동일성·실패/중복·cold entry. 현재 결과는 SCENE_STRUCTURE_REPORT.

## 전투 배치 변경 (2026-09-08)
- CombatRoll/CombatCards에서 실제 HP/정규화 비율/보호막/IntentAmount/족보/운명력/재굴림을 typed field로 전달한다. EVENT=eventsResolved+1, RUN TURN=런 누적 combatTurns+1. 없는 상태효과/자원을 만들지 않으며 기존 offered ID/효과/명령/저장을 유지한다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 기본 UI 문구/콘텐츠 이름/등급·족보·장비 설명·진행 알림은 KoreanText를 거쳐 한국어로 표시한다. 사용자 편집 문구는 보존하고 raw tag 배열은 DTO에 그대로 전달한다. PlayerError는 원래 예외를 Warning 진단으로 남기고 저장/접근/일반 실패를 한국어로 안내한다. 내부 명령 키·원본/제시 ID·게임 규칙·저장 schema는 변경하지 않았다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.


## 준비 로비·캠페인 지도·주사위 창 (2026-09-08)
- Menu 데이터는 기존 방랑자 1명의 출발 능력치/기본 행동/6주사위와 현재 런 성장 안내를 생성한다. 시드 입력은 플레이 작업실에 두고 로비에서는 숨긴다. 영구 성장 규칙·신규 캐릭터·저장 schema는 변경하지 않는다.
- 구형 모드0의 Map DTO는 공개 active graph와 resolvedEventIds 순서로 nodeHistory에서 확인한 완료 노드만 복사한다. 신규 모드1은 맨 위 절차 지도 계약의 전체 공개 projection을 사용한다. View가 저장 기록을 직접 조회하지 않으며 버린 가지는 완료 경로로 표시하지 않는다.
- TravelToNode는 Busy→ChooseNode 저장 성공→Widgets.AnimateNodeArrival→Render 순서다. 도착이 끝나야 DiceRollUI가 열린다. 실패하면 이동/굴림을 확정하지 않는다.
- ExplorationRoll/CombatRoll 렌더는 UIManager.ShowPopup<DiceRollUI>를 호출한다. 배경의 open-dice 요청은 창 재개만, popup의 roll 요청은 기존 Session.Roll 명령을 실행한다. 실제 결과를 저장한 후 표시 애니메이션을 기다려 popup을 닫고 카드를 렌더한다.
- 중복 입력은 actionBusy/UIManager 입력 잠금으로 막는다. 닫기·재개는 주사위를 생성하지 않으며 Cards 저장 재개는 다시 굴리지 않는다. 유료 재굴림은 기존 Reroll 명령/소모량을 유지한다.
- DiceRollUI는 전용 modal이며 기존 화면 8개와 함께 UIRoot에 등록된다. 수명/저장/씬 전환 소유권은 기존 Controller/SceneFlow/UIManager 계약 그대로다.
- 현재 작업의 검증과 범위: Docs/Reports/LOBBY_MAP_DICE_REPORT.md.



## 조합·카드·전투 피드백 (2026-09-08)

- 카드 선택 명령을 먼저 저장한 뒤 해당 카드만 강조하고, 확정된 before/after 표시 스냅샷으로 플레이어 행동·생존 적 반응을 순서대로 재생한 후 Render한다. 실제 HP 감소는 전후 차이, 공격량/획득 수호는 공유 Evaluate, 반격 예고는 before에서 읽는다. 조합은 KoreanText.HandSummary/HandStage, 명칭은 와일드 카드. 굴림 후 원본 resultHoldSeconds 동안 결과를 유지한다. 중복 입력 잠금·실패 시 성공 연출 생략·중단 시 transform/잠금/잔여 popup 정리를 적용한다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.

## RA-C 직접 관계
RunSession 공개 API를 유지하는 facade를 사용한다. 내부 명령은 Core RunApplication이 소유하고 Controller는 Runtime RunState 표시 복사/저장 어댑터만 사용한다. 코어 분리 때문에 UI 카드 ID 분기나 새 렌더 경로를 추가하지 않았다.

- RA-D: ShopRules.Offer로 표시 가격·구매 활성 여부를 일치시키고 RewardText에 일반 addActionId의 저장된 한국어 이름을 표시한다. 새 카드 전용 UI/세션 분기가 없다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


## 주사위 조합 연출 (2026-09-10)

ShowDiceWindow가 기존 저장 스냅샷에서 조합 label/HandKind/priority 순위 강도/해당 phase 결과 유지 시간을 DiceRollUIData에 전달한다. 추가 판정·난수·Checkpoint 없음. 기존 PresentChange의 굴림→유지→카드 선택→전투 반응 순서를 유지한다.
검증 상태: DICE_PRESENTATION_EFFECTS_REPORT의 실제 결과를 따른다.


## 등급별 연출 수명 (2026-09-10)

Playback(PresentationPlayback)이 시퀀스 시작/종료 시각·상태·이벤트를 소유한다. PresentChange는 실제 popup/선택 FEEL/전투 완료를 기다리며 .22/.18/추가 action 대기를 제거했다. PresentSafely는 TimedOut/Faulted/Cancelled에 소유 효과·popup 정리와 확정 snapshot 복구를 하고 비활성 중 Render를 생략한다. timeout은 label/elapsed/limit 오류 로그, Faulted는 기존 warning이다. OnDisable은 Cancel 뒤 잔여 UI/잠금을 정리한다. 새 Dice binding이면 옛 Close/Render를 생략한다. 추가 명령·저장·RNG 없음.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
