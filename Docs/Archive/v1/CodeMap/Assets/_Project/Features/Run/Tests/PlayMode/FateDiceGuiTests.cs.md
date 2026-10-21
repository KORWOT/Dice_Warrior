# FateDiceGuiTests.cs

## RA-B 상태 경계 fixture 전환 (2026-09-09)

세션의 State는 독립 표시 복사다. 준비 상태를 지역 RunState DTO에 구성한 뒤 새 RunSession 또는 격리 저장소에 전달한다. 규칙 기대값과 기존 버튼/저장/재연 assertion은 유지한다. 잘못된 저장 검사는 동일한 수정 DTO를 LocalRunStore.Save에 전달한다. 실제 실행 증거는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


## RA-A 현재 계약 (2026-09-09)

LoadProductScene에서 Seed=33을 명시 주입한다. 일반 새 여정이 시스템 시드로 바뀐 뒤에도 기존 완주·보상·승리 재연의 독립 기대값을 보존한다. 기존 모든 버튼/레이캐스트/저장 assertion은 유지한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 책임: 실제제품Scene에서실제uGUI포인터/레이캐스트로플레이·저장·화면비를검증하는PlayMode8건.
- LoadProductScene은720x1280설정/전용Scene로드/명시루트에서테스트대상확인/Guid별Temp저장주입. TearDown은소유Screen파괴후자체임시저장폴더정리. 제품기본저장을쓰지않는다.
- Press는Busy/개별비활성거부→EnsureVisible본문스크롤→SafeArea/터치높이검사→GraphicRaycaster로클릭대상확인→pointerDown/up/click. 화면밖버튼을직접성공시키는검증이아니다.
- 시나리오: 기본seed33새런→10사건→Boss승리→결과→재시작/최근결과보존;획득die버튼중복거부;Scene재로드같은카드/RNG/유료재굴림잔량;손상안내·덮어쓰기차단·명시.bak보존;숨긴 로비 seed/설정한 출발 시드;5유형실효과/훈련16XP/장비·die교체;패배;720x1280/720x1600SafeArea/스크롤/텍스트높이.
- 수용fixture구분: 정상완주는원래SO그대로. 5유형과패배는실제SO스냅샷에서유형가중치/시작HP등을고정한격리저장시나리오이며기본밸런스주장이아님.
- 직접사용: Screen/Widgets/RunSession/GrowthRules/CombatRules/LocalRunStore,JsonUtility,UI/EventSystem,EditorSceneManager(EDITOR조건부),NUnit/UnityTestTools.
- 증거: m5-gui-acceptance-check.json8/8 PASS. Editor포인터자동화이며물리터치/Android실기기검증이아니다. 초기GUI미구현RED→현재동작PASS를구분해REPORT에기록.


## 추가 UI 수용 계약
- GUI 8→14개. 실제 Scene의 persistent catalog/4 prefab references, 원본 인스턴스, 매핑 asset 저장/화면 재진입/복원, 공통 prefab Bold 편집/화면 전파/복원을 확인한다.
- 이미지 전후 동일 런 복제로 같은 실제 ChooseNode/Roll/ChooseFate/ChooseAction을 실행하고 playedSeconds만 제외한 전체 상태를 비교한다. 원본 ID/제시 ID/실제 효과와 종횡비 영역도 확인한다.
- 공개 운명 그림에 실제 사건 이미지/이름이 없으며 선택 후 공개 영역에는 사건 Sprite가 표시되는지 확인한다.
- 실제 프리팹으로 strike Common/Epic 중복과 guard Rare를 표시하고 Epic 개별 ID를 선택한다. null Sprite fallback도 읽고 실제 클릭한다.
- 합류 fixture의 B/E 즉시 입력 차단·완료 숨김·ID 보존과 단일 C 표시 유지, 저장/메뉴 복귀 후 C/D 활성 선택을 확인한다.
- 기존 실제 raycast/scroll 입력·완주/패배/성장/디스크 이어하기/두 세로비 회귀를 유지한다. Editor 테스트이며 Android 실기기 검증을 뜻하지 않는다.

## 전투 배치 변경 (2026-09-08)
- 공개 그림 probe의 예전 자식 경로를 CombatUI.artwork 직접 참조로 바꾸고, action artwork/effect 비겹침 방향을 portrait 위/아래로 변경했다. pointer/원본 스타일 전파/ID/효과/상태 oracle는 완화하지 않았다. 새 제작용 InGame 두 비율은 CombatLayoutTests가 검사한다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 직접 표시 문구 기대를 한국어로 변경했다. 기존 포인터·규칙·저장·완주 oracle를 유지하며 손상 저장의 새 Warning은 명시적으로 기대한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.


## 준비 로비·캠페인·주사위 팝업 어댑터 (LOBBY_MAP_DICE)
- FindButton은 roll만 top DiceRollUI.rollButton에서 찾고 나머지 key는 기존 Widgets.Buttons를 사용한다. Press는 기존 Raycaster/포인터/스크롤/터치 검증을 유지한다. 메뉴 복귀 fixture는 popup을 먼저 명시적으로 닫고 실제 메뉴를 클릭한다.
- 공유 C는 1개이며 전체 NodeId unique를 요구한다. 도착 중 즉시 차단은 CanvasGroup을 반영하는 IsInteractable로 확인하고 공유 노드는 활성/비fade/비클릭을 요구한다. B/E 숨김·history·child IDs·RNG 검사는 유지한다.
- 일반 로비의 seed InputField는 숨김을 검사한다. 기존 public Seed를 41로 지정한 뒤 실제 새 여정 버튼을 누르고 initialSeed=41을 확인한다. 숨긴 필드를 플레이어 입력으로 가장하지 않는다.
- 완주/패배/보상/재굴림/저장·자산 원본 전파/두 화면 비율 검증은 유지한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

일반 완주·보상·수제 합류 경로의 고정 seed oracle를 유지하도록 테스트 소유 FateDiceConfig clone에 mapGenerationVersion=0을 명시하고 teardown에서 제거한다. 노드와 운명 카드 첫 포인터 탭 후 전체 상태/저장이 그대로인지 확인한 뒤 move 또는 FateChoiceUI.confirmButton을 실제 포인터로 누른다. 카드와 유료 주사위는 열린 FateChoiceUI의 실제 참조를 조회한다. 두 비율의 탐험 검사는 공개 HP/골드/수호 필드와 실제 중첩 scroll viewport를 사용한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.


## PROCEDURAL_CAMPAIGN 보완 묶음 2 직접 회귀

EarnedReroll은 Widgets의 옛 die-2 키 대신 실제 FateChoiceUI 버튼 조회를 사용한다. 첫 입력 후 DiceRollUI로 바뀌어도 같은 이전 버튼 callback을 다시 호출해 중복 비용/명령 방어 oracle를 유지한다. 비공개 event artwork 금지 검사는 유지하면서 공개 유형의 visible typeSymbol 또는 기존의 비어 있지 않은 artworkFallback glyph 중 하나를 요구한다.

근거: artifacts/procedural-campaign/play-initial.json의 실제 실패. 이번 담당은 Unity를 실행하지 않았으며 수정 후 검증은 Main의 마지막 통합 실행 대기다. 기존 EventSystem 경고를 기대 로그로 등록하거나 전역 객체를 삭제하지 않는다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

실제 노드 포인터를 한 번만 보내며 더 이상 별도 move 입력을 만들지 않는다. fate 카드의 강조 선택 뒤 상태/디스크 불변과 confirm 포인터는 유지한다. WaitUnlocked는 CanvasGroup 상태뿐 아니라 screen.Busy=false까지 기다려 실제 전투 진입 연출이 끝난 뒤 다음 roll을 누른다. 기존 14개 검사의 저장·RNG·단일 재굴림·중복 callback·원본/좌표/가시성 oracle를 유지한다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
