# RunUIController.cs

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
- Map DTO는 공개 active graph와 resolvedEventIds 순서로 nodeHistory에서 확인한 완료 노드만 복사한다. View가 전체 런 기록을 조회하지 않으며 버린 가지는 완료 경로로 표시하지 않는다.
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
