# PlayWorkbenchSession.cs

## RA-A 현재 계약 (2026-09-09)

BootstrapPending이 FixedSeedSource(request.seed)를 GameApplication에 명시 주입한다. 기존 seed 0 오류, Build의 직접 RunSession.New 고정 재연, 격리 저장/BeforeSceneLoad/원래 씬 복원은 유지한다. 일반 게임의 SystemSeedSource 선택과 분리한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.


- COMBAT_FEEDBACK 변경: 등록되지 않은 출전 카드의 오류 메시지를 와일드 카드로 정정한다. trialId 필드·허용 목록·검증 조건·격리 저장 및 수명 계약은 변경하지 않는다. 실행 증거는 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다.

- 역할: Editor 전용 시작 옵션과 실제 RunSession 명령으로 만든 시작 상황, 한 번만 소비하는 격리 Play 요청을 소유한다. 제작 GameApplication/SceneEntry의 기본 초기화 코드는 수정하지 않는다.
- 입력/API: WorkbenchStartPoint는 Title/Lobby/Map/ExplorationCards/Combat/Shop/Reward/Equipment/Result다. WorkbenchOptions는 application/seed/trialId/cap/startPoint를 담는다. Build(options)는 RunState, Start(options)는 격리 Play 시작 요청이며 IsPending/LastError/LastStorePath는 읽기 전용이다.
- 상태 생성: 설정 SO의 Snapshot을 복사하고 New → ChooseNode/Roll/ChooseFate → 필요한 ResolveEncounter/ClaimReward/ChooseAction을 실행한다. Title/Lobby/Map은 Map, 나머지는 ExplorationCards/CombatCards/Shop/Reward/EquipmentChoice/Result 상태로 반환한다. Build는 저장·SessionState·Unity 객체를 만들지 않는다.
- 예시 설정: Combat/Result는 전투, Shop은 상점, Reward/Equipment는 보물 확률을 복사본에서 고정한다. Result는 시작 HP 1/Power 0/Guard 0, 일반 적 HP 최소 1024/Power 최소 100000인 복사본으로 실제 패배를 생성한다. 최대 128회 전투에서 패배를 만들지 못하거나 장비 보상 없는 설정이면 설명 가능한 예외로 거절한다.
- 검증: null 옵션, 앱/controller/config 누락, seed 0, 범위 밖 cap/startPoint, 등록되지 않은 trial을 거절한다. 빈 trial은 설정의 첫 시련을 선택한다. Start는 Play/중복 요청/컴파일·import/Prefab 또는 preview stage/dirty·이름 없는 씬을 거절하고, 비활성 원본 앱과 enabled 제작 씬 세 개를 확인한다.
- 저장/요청: Library/FateDiceWorkbench/<고유 ID>/run.json에 LocalRunStore.Save/Load로 완전 상태를 검증한다. 원래 SceneManagerSetup과 시작 요청은 SessionState에 기록한다. 시작 화면은 Title/Lobby 또는 InGame이며 Game View를 720×1280으로 준비한다.
- 초기화 순서: BeforeSceneLoad에서 pending을 먼저 지우고 active 요청으로 옮긴 뒤 전용 경로/현재 씬/저장을 검증한다. GameApplication.Bootstrap(prefab, store)에 저장소를 명시적으로 주입하고 Seed 및 기존 메뉴의 trial/cap 선택만 맞춘다. pending 없는 일반 Play는 즉시 반환한다.
- 실패/수명: 초기화 실패는 해당 Play의 SceneEntry를 먼저 비활성화하고 Play를 중단한다. 비동기 중지 사이 기본 저장소로 진입하지 않게 하며, EditMode 복귀 때 비활성화한 entry를 복원한다. 취소된 요청도 지워 다음 일반 Play에 남지 않게 한다. 원래 씬 복원은 Unity 종료 처리 뒤 지연 호출하고, 새 dirty 씬이 있으면 강제로 닫지 않는다. reload 옵션은 변경하지 않는다.
- 관계: PlayWorkbenchWindow가 Start, UIWorkbenchPreview와 PlayWorkbenchTests가 Build를 호출한다. FateDiceConfig/RunSession/CombatRules는 상태 생성, LocalRunStore는 격리 checkpoint 검증, GameApplication/RunUIController/SceneEntry는 명시적 bootstrap·초기 메뉴·실패 차단, EditorSceneManager/SessionState는 Editor 수명을 담당한다.
- 이전 PLAY_WORKBENCH 작업 검수: 당시 EditMode 128/128 통과(작업실 검사 6개 포함). 실제 실행·직접 클릭 및 보존 경계는 Docs/Reports/PLAY_WORKBENCH_REPORT.md를 참조한다.

- 복원 소유권: 이번 Pending/Active 요청이 있는 Play만 종료 시 자동 복원한다. 과거 Setup 기록만 남은 일반 Play에는 개입하지 않으며, 비상 중단도 Pending을 Active로 보존한다.

## 준비 로비 초기 선택 (LOBBY_MAP_DICE)
- BootstrapPending에서 주입한 앱의 공개 MenuUI.settingsTab을 열고 기존 trial/cap 버튼을 호출한 뒤 characterTab으로 돌아간다. 숨은 설정 패널의 CommonButton 입력 guard를 우회하거나 컨트롤러 private 필드를 reflection하지 않는다.
- 옵션 enum/Build 결과/고유 저장 경로/BeforeSceneLoad 순서/Pending·Active 복원 소유권/실패 중단·dirty guards는 변경하지 않는다. 기존 일반 Play 비개입 계약을 유지한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
