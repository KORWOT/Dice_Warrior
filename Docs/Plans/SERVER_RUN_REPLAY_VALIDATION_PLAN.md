# SERVER_RUN_REPLAY_VALIDATION (P2) PLAN

유형 IMPLEMENT · 2026-10-21 · 완료. 증거: [REPORT](../Reports/SERVER_RUN_REPLAY_VALIDATION_REPORT.md).
승인: 사용자 “다음 P2 명령 로그 재현 검증기 구현…문서도 함께 갱신”.
명세: [원본 로드맵 P2](../Dice_Warrior_Next_Work_MockBackend_Firebase_Roadmap_v0.1.md), [완료 P1 PLAN](SERVER_BACKEND_FOUNDATION_PLAN.md).

## 목표·결정

권한 저장소의 시작 티켓만으로 독립 Run을 만들고 확정 명령 로그를 순차 재실행한다. 결과 단계까지 합법적으로 재현된 런만 서버 산식 `completedRegions*10 + (won?20:0)`으로 지급한다. 지급과 영수증/프로필은 기존 Mock 원자 저장 안에서 함께 확정한다.
사용자 결정: P2는 **새 제출부터 적용**한다. P1 PendingVerification 기록·응답은 그대로 보존하며 자동 재검증/지급하지 않는다. 기존 미제출 활성 런의 P1 형식 로그는 P2에서 검증할 수 있다.
처음 20 Mock 재화 정책은 유지한다. Firebase·실제 계정 자산으로 승격하지 않는다.

## 기준·보호

- HEAD/main `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`, 기존 staged 없음. P1의 미커밋 코드/문서는 이 작업의 승인된 연속 기준이며 reset/stash/재작성하지 않는다.
- 기존 RP 두 설정, Pretendard-Effects SDF, ProjectSettings/Packages 두 설정, 이번 시작 때 이미 수정된 ProjectSettings/ProjectSettings.asset, 사용자 원본 로드맵, Scene/Prefab/Packages/Archive, 완료 P1 PLAN/REPORT/증거를 보호한다.
- 기준 해시/상태: `Temp/server-run-replay/before.json`. 실제 사용자 저장/백업에는 접근하지 않는다. 새 검사도 임시 고유 경로만 사용한다.
- Orca·Git 쓰기·패키지·Scene/Prefab 편집 없음. 기존 Unity Editor 하나만 직렬 사용. 기존 연출/아트/3~4눈 설계/경제 수치 변경 없음.

## 계약

1. protocolVersion=1, rulesVersion=`symbol-loop-mvp-1` 유지. 콘텐츠·RNG/보드/전투 산식은 바뀌지 않는다. P2는 검증/지급 정책 추가다. 지원하지 않는 버전은 최신 규칙으로 대체하지 않는다.
2. Domain/RunCommandRules가 기존 travel/combat/choose/lap/abandon의 동일 결정 규칙을 제공한다. Session과 검증기는 이 순수 코드를 공유하되 상태/명령 소유권은 공유하지 않는다.
3. `MockRunVerifier.Verify(RunTicket trustedTicket, IReadOnlyList<RunCommandEntry> commands, CancellationToken ct=default)` → `RunVerificationResult`(accepted/code/message/failedSequence/payout/finalRun). ticket은 Mock 저장소에서 읽으며 제출 DTO가 티켓/최종 결과를 지정하지 않는다.
4. 로그 sequence는 1부터 연속, clientCommandId 고유/길이 제한. 각 명령 전 targetId=`region:position:Phase:turn`을 재현 상태와 일치시킨다. kind/arg 선택과 상태 전이는 순수 규칙으로 재검증한다. 결과 뒤 명령·미완료 런·잘못된 장비/슬롯/비용·다른 상태 대상은 거절한다.
5. 최대 2048명령/512KiB 제출, 재현은 명령당 한 번만 수행한다. 취소는 루프마다 확인한다. 잘못된 입력은 구체적 오류 코드/명령 순번으로 반환하며 프로필·활성 런·보상은 변경하지 않는다. 정정 제출은 새 요청 ID로 가능하되 동일 요청 ID의 본문 변경은 거절한다.
6. Approved 영수증에는 재현된 completedRegions/won/abandoned와 검증 버전, 지급액을 보관한다. PendingVerification 구형 영수증도 계속 읽는다. 이미 정산한 런은 같은 영수증을 반환하며 추가 지급하지 않는다.
7. 검증 거절은 활성 런을 유지한다. 로컬 파일의 오류 원문을 보존하고 UI에 검증 실패를 표시한다. 자동 포기/임의 보정으로 보상을 만드는 경로는 없다.
8. 시작/제출/포기 빈도는 권한 측 60초 창에서 각각 10/20/10회로 제한한다. 시간은 Mock 서버 clock(검사에서 주입 가능)이며 클라이언트 시간 필드는 없다. 같은 ID 재시도는 제한을 추가 소모하지 않는다. 시계 역행은 창을 재개방하지 않는다.
9. 골든 벡터는 고정 시드 3~5개로 승리/패배/포기/2·3눈/상점·교체·성장/재개를 포함한다. 각 단계 주요 상태와 RNG·턴 결과, 최종 지급을 버전 고정 JSON으로 보존하고 매 검사 시 재생 결과와 비교한다. 테스트가 실행될 때 기대 파일을 다시 생성하지 않는다.
10. 로그에서 결과를 계산하는 방식은 합법적인 다른 로그 구성·자동 플레이·미래 예측을 막지 못한다. 상태 전이를 깨는 누락/변조는 거절하나, 관찰하지 않은 모든 실제 행동의 누락을 증명하는 체계라고 주장하지 않는다.

## 파일·담당·위임

- 메인: 신규 Domain/RunCommandRules.cs; Domain/BackendContracts.cs·LoopRules.cs(저장 검증 및 RNG의 명시적 unchecked); Runtime/LoopSession.cs; Backend/MockGameBackend.cs·MockAuthorityStore.cs; Presentation/SymbolLoopApp.cs; 기존 Tests/EditMode/SymbolLoopTests.cs·BackendFoundationTests.cs, 신규 Tests/EditMode/BackendReplayIntegrationTests.cs; Tests/PlayMode/SymbolLoopFlowTests.cs. 필요한 신규 meta.
- 독립 구현 보조 1명: 신규 Backend/MockRunVerifier.cs, Tests/EditMode/RunReplayTests.cs, Tests/EditMode/ReplayGoldenVectors.json 및 meta, 해당 1:1 CodeMap. 공통 계약/기존 파일/Unity/Git 수정 금지. 입력은 본 계약과 RunCommandRules, 출력은 검증기·악성/골든 검사. 골든 JSON은 메인이 한 번 생성/확인 후 고정한다.
- 읽기 검토 보조 1명: 최종 검증/지급/구형 기록 보존 직접 결함만, 쓰기 없음.
- 최대 동시 2명(메인 포함), 누적 위임 2명, 재귀/중복 위임 없음. 메인이 공통 계약·INDEX·PLAN/REPORT·GDD/README·Unity 실행을 소유한다.
- 문서: 본 PLAN, Reports/SERVER_RUN_REPLAY_VALIDATION_REPORT.md, 해당 CodeMap/직접 관계 요약, INDEX, README, GDD. 완료 P1 산출물은 이력 그대로 유지.
- 증거: Temp/server-run-replay, Reports/Evidence/RunReplay. Tests/EditMode/ReplayGoldenVectors.json은 검증용 데이터 자산이며 Editor import로 meta를 받는다.
- 테스트 asmdef에는 이미 설치된 Newtonsoft.Json.dll의 precompiledReferences를 추가한다. 구형 P1 저장 형식 fixture와 알 수 없는 결과 필드 거절을 검사하며 새 패키지를 설치하지 않는다.

## 순서·AC·검증 상한

- [x] P1 PLAN/관련 색인·소스/보호 상태 확인, 기존 대기 기록 보존 결정.
- [x] 순수 명령 계약/독립 검증기, 형식·단계·비용·종료 검증 구현.
- [x] 새 제출의 서버 계산·원자 지급/중복 영수증, 구형 pending 유지, 빈도 제한 및 UI 반영.
- [x] 고정 JSON 골든 4시드/121명령 생성·확인, 재생/손상 로그/보상 위조/재개 검사.
- [x] EditMode 49/49, PlayMode 6/6 PASS. 보호719개 불변, 소유 C#28개 CodeMap/meta 누락0.
- [x] REPORT와 문서 동기화.

AC1 정상 승리/패배/중도 종료는 신뢰된 시작에서 재현되고 지급액은 서버 계산과 일치.
AC2 순서/중복/누락으로 불법인 상태 전이, 잘못된 선택/장비/슬롯, 결과 위조 입력, 지원 안 되는 규칙·크기 초과를 지급 없이 거절.
AC3 동일 런 10회/같은 요청 재시도·승인 후 로컬 저장 실패에서도 지급 1회, 이전 P1 pending 자동 지급 없음.
AC4 고정 벡터 각 단계 핵심 상태/RNG/최종 보상 일치. 공유 엔진이더라도 독립 시작 상태와 golden 기대값을 확인.
AC5 기존 게임 화면/연출/저장·재개 유지, 정산 승인/검증 실패/구형 대기 구분, 코드 요약·보호 경로 유지.
최초 통합 검토 1/1, 이후 필수 수정·재검증 0/2, 남은 필수 Finding 0. 검증 준비의 컴파일 참조/NUnit 조정은 REPORT에 기록했다. Android/실서버/P3 전체 다중 기기 복구·실제 공격 서비스 검증은 NOT_RUN/후속 범위.
