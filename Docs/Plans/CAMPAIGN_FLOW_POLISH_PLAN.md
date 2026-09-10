# 캠페인 복귀·즉시 이동·전투 진입 PLAN

상태: IMPLEMENT. 사용자 2026-09-10 요청의 세 동작을 구현한다. 기존 PROCEDURAL_CAMPAIGN 완료 작업의 재개방이나 잔여 수정 횟수 초기화가 아니라, 새로 승인된 입력/진입 계약의 변경이다. 기존 미커밋 작업을 기준으로 보존한다.

## 계약·AC
- 전투/일반 사건/상점/보상·장비 처리를 마쳐 Map으로 복귀하거나 Map 저장을 재개하면 현재 완료 노드와 다음 이동 가능 경로가 보이도록 스크롤한다. 레이아웃이 확정된 후 초점을 적용하고 매 프레임 강제 이동하지 않는다. 이후 사용자의 수동 스크롤은 유지한다.
- 이동 가능한 노드 탭은 ChooseNode 한 번→저장→기존 도착 연출→탐험 주사위 창 순서다. 별도 이동 버튼은 원본과 런타임에서 표시하지 않는다. 빠른 중복/오래된 callback/불가 노드/스크롤 drag 종료 입력으로 중복 이동하지 않는다. 저장 실패는 현 위치·RNG·저장을 보존하고 재시도 가능하다.
- 새 전투 진입(보스 포함)은 전투 화면 표시→적 등장/전투 시작 텍스트·짧은 확대/페이드 연출 완료→굴리기 창 순서다. 실제 IEnumerator 완료를 기다리며 Controller에 고정 대기 시간을 추가하지 않는다. CombatUI 원본에서 연출 시간/강도를 편집한다.
- 전투의 다음 턴/재굴림은 진입 연출을 반복하지 않는다. CombatRoll 저장에서 전투 화면으로 재진입하면 등장 연출 후 기존 미굴림 창을 연다. CombatCards 저장은 기존 확정 카드를 유지하고 재굴림/진입 처리를 자동 실행하지 않는다.
- 연출 중 입력 잠금, disable/rebind/cancel 복원, 기존 PresentationPlayback의 실시간10초 watchdog·로그·확정 상태 복구를 유지한다. 실제 타임아웃은 재생을 강제 종료하고 주사위 준비 상태를 보여주며 게임 명령을 다시 실행하지 않는다.
- 필수 실제 검증: 지도 복귀 초점·수동 스크롤, 단일 탭 이동·중복·저장 실패, 전투 진입 중 창 없음/실제 종료 후 창, 다음 턴 반복 없음, resume/disable/시간제한·원본 포즈 복원, 두 세로 비율. 직접 영향받는 기존 GUI/lifecycle 검사를 실제 새 입력 경로로 갱신한다.

## 범위·보호
- Main: RunUIController.cs, 신규 Run/Editor/CampaignFlowAuthoring.cs, 신규 Run/Tests/PlayMode/CampaignFlowPolishTests.cs, PLAN/REPORT/INDEX와 직접 계약 문단·해당 CodeMap, 실제 Unity 적용·검증·통합.
- campaign_view: ExplorationUI.cs, CampaignMapView.cs, CampaignMapAuthoring.cs, 필요 최소 CampaignMapProjection.cs(읽기 후 필요하면 Main과 파일 소유권 확인), 자신의 CodeMap. UI 탭 계약/초점과 원본 이동 버튼 제거용 ApplyDirectTravel 진입점. Core/Controller/Combat/Unity/다른 테스트 쓰기 금지.
- combat_entry: CombatUI.cs와 자신의 CodeMap. 공개 PlayEntry():IEnumerator, ResetEntry(), IsEntryPlaying, EntryBindingVersion(바인딩마다 증가)을 제공한다. 기존 arena/actionFeedback/hitFlash와 전용 직렬화 시간/강도 사용, 자체 게임 명령/전역 의존성 없음. 다른 파일/Unity 쓰기 금지.
- flow_test_updates: 직접 영향받는 기존 Run/Tests/PlayMode 및 EditMode 검사와 해당 CodeMap. 새 단일 탭/전투 진입 완료 경로에 적응하며 의미 있는 저장/RNG/입력 검사는 유지한다. 신규 CampaignFlowPolishTests 및 production 파일 수정 금지.
- 구현 중 소유권 인계: 기존6검사 수정이 끝난 뒤 Main이 RED skeleton인 CampaignFlowPolishTests.cs와 해당 CodeMap을 flow_test_updates에 단독 인계하여 실제 흐름 검사를 확장한다. Main은 이 파일을 동시 수정하지 않으며 production 통합·검증을 계속한다.
- 최대 동시4(Main+보조3), 고유 보조3, 재귀0. Main만 Unity 직렬 실행. 최초 통합 검토1묶음·보완 최대2묶음. 기존 Title EventSystem 중복6실패는 별도 기존 결함으로 보존하고 검사를 무시하지 않는다.
- 정확한 자산 쓰기: Editor API로 Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab의 이동 버튼 비표시/레이아웃, Assets/_Project/Features/Run/Prefabs/CombatUI.prefab의 진입 연출 설정만. 다른 Scene/Prefab/설정/패키지/공급사/기본 저장/GUID는 보호한다. 신규 C# meta는 Unity 생성.
- 기본 HEAD d8df4999174ad63bb73e0727ff0149cb55624db2; 실제 최신 Git/status/hash/Editor 상태는 artifacts/campaign-flow-polish/baseline.json에 기록한다. 기존 staged 포함 금지, stage/commit/branch 변경 없음.
- Docs/AI 보조 규칙 파일은 현재 checkout에 없음을 확인했다. 사용자가 제공한 AGENTS와 기존 PLAN/CodeMap을 적용하며 이 누락 때문에 승인된 개선을 중단하지 않는다.

## 실행
- [x] 현재 변경/Editor/보호 hash 기록, 관련 원본·요약 확인, 필수 새 동작 RED 실행.
- [x] 지도/전투 View와 Controller 연결 및 지정 원본 적용, CodeMap 동기화.
- [ ] 신규·직접 회귀 검증, 두 비율 화면/실제 포인터 입력, 최초 통합 결과와 보완 횟수 기록.
- [x] 자산/설정/저장 보존·재적용 멱등·현재REPORT, Editor 상태 복원.

최초 통합 검토의 CFP-R01/R02를 보완1에서 수정했다. 확대 실행11개 중9PASS/2FAIL의 CFP-R03(초점 좌표 변환)과 CFP-T01(실제 drag 입력 재현)을 보완2에서 수정했다. 누적 보완2/2이며 최종 전체 PlayMode 결과는 REPORT에 기록한다. 이후 일반 개선점으로 작업을 확장하지 않는다.

전체Play196개 중189PASS/7FAIL, 신규11/11PASS. 남은 신규 CFP-T02는 기존 LobbyCampaignFlowTests.cs:72의 원본 버전기대값2→3 갱신 누락이다. 추가1회 보완과 해당 검사만 재실행하도록 사용자 승인 요청 중이며, 답변 전에는 편집하지 않는다. 기존Title EventSystem6실패는 baseline과 같고 신규결함으로 재분류하지 않는다.
