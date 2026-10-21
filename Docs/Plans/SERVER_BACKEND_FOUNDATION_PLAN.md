# SERVER_BACKEND_FOUNDATION_01 PLAN

유형 IMPLEMENT · 2026-10-21 · 완료(P0+P1). 사용자 요청: 로드맵을 확인해 다음 작업 진행.
기준 명세: [Mock/Firebase 로드맵 v0.1](../Dice_Warrior_Next_Work_MockBackend_Firebase_Roadmap_v0.1.md)의 P0+P1.
계획 작성·실행 스킬을 적용하되 사용자가 승인한 현재 작업을 이 세션에서 이어서 수행한다.

## 목표와 확정 정책

- 로컬 전투·이동·연출을 유지하고 런 시작, 영구 프로필과 성장 권한을 비동기 IGameBackend 뒤로 분리한다.
- 사용자 답변으로 P1 정산은 PendingVerification으로 보존한다. 승리/지역/잔액을 제출해 보상을 받는 경로는 없다.
- 새 개발 계정 `mock-player-01`에 성장 시험용 20재화를 최초 생성 때 한 번 지급한다. 재실행·재조회는 추가 지급하지 않는다.
- 제출 후 다음 런은 가능하며, 미검증 제출 기록은 독립 권한 저장소에 남는다. UI에 개발용 Mock과 검증 대기를 표시한다.
- P2 리플레이 검증기, P3 전체 동시성/복구 보장, Firebase SDK·인증·배포, Android 실기기는 이번 범위 밖이다.
- 기존 3눈 규칙, 경제 수치, 프리팹/씬/패키지/공통 UI는 변경하지 않는다.

## 기준·보호

HEAD `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`, main, 기존 staged 없음.
기존 수정 RP 설정 두 파일, Pretendard-Effects SDF, ProjectSettings/Packages 설정 두 파일 및 사용자 로드맵 원본은 보호한다.
기준 상태/해시: `Temp/server-backend-foundation/before.json`. Archive/v1, 모든 Scene/Prefab/meta 기존 GUID, Packages, 실제 v1 저장/백업은 쓰지 않는다.
새 실제 경로는 `persistentDataPath/symbol-loop-backend-v1/mock/checkpoint.json`과 `authority.json`으로 분리한다.
기존 `symbol-loop-v1/player.json`은 읽거나 이관하지 않는다. 기존 진행은 원본에 그대로 보존된다.
Orca, Git stage/commit/branch/push, 전역 설정/패키지 변경 없음. Unity PID 27876/port 7801 단일 Editor만 직렬 사용.
현재 Docs/AI 상세 운영 파일은 저장소에 없으므로 사용자 제공 AGENTS.md 본문을 적용한다.

## 공통 계약

- `protocolVersion=1`, `rulesVersion=symbol-loop-mvp-1`. 기존 Rules 기본 콘텐츠·12칸/3지역·1~3눈·RNG 소비·성장/전투 수치를 동결한다.
- BackendContracts는 순수 Core 타입이며 Unity 타입이 없다. API는 Task<BackendReply> + CancellationToken.
- GetProfileAsync, StartRunAsync, SubmitRunAsync, UpgradeAsync, GetOperationStatusAsync. 요청 DTO는 BackendRequest 하나로 공통 필드와 선택 입력을 명시한다.
- 요청: protocolVersion, rulesVersion, requestId, expectedRevision, kind, serverRunId, startingDie/Weapon, upgrade, commands. 클라이언트 UID·시드·최종 승리·지급액 입력 없음.
- 응답: Approved/Rejected/Conflict/PendingVerification/UnknownAfterTimeout/TemporaryFailure/Unsupported, code/message, 권한 Profile 사본, RunTicket, SubmissionReceipt.
- RunTicket은 서버 runId/seed/rulesVersion/규칙 사본/시작 프로필을 포함한다. 백엔드가 검증한 출전 선택만 적용한다.
- 명령 로그: 순번·kind·arg·targetId·clientCommandId. 로컬 저장 성공 때만 같이 저장. P1은 로그를 보관할 뿐 합법성 검증을 주장하지 않는다.
- 같은 requestId/본문은 저장된 응답 재전송, 다른 본문은 거절. 예상 프로필 revision으로 성장 경합을 차단한다.
- Mock 저장소는 프로필/런/작업 결과를 한 파일의 원자 교체로 저장하고 정상 primary를 backup으로 보존한다. 손상/주 파일 소실 시 자동 초기화/과거 잔액 복원 금지.
- Mock은 프로세스 내부 개발 대역이다. 파일 경로 단위 프로세스 내 잠금, 최대 128런/1024작업/2048명령/512KiB 제출 한도. 초과 시 명시 오류; 자동 삭제하지 않는다. 실제 서버 DB 구조로 이 파일 형식을 고정하지 않는다.
- Firebase 선택 시 Unsupported. Mock으로 자동 전환하지 않는다.
- 세션은 영구 재화를 직접 지급/차감하지 않는다. coordinator가 백엔드 응답을 받아 로컬 캐시/런을 저장한다. 로컬 캐시 변조는 권한 프로필에 반영되지 않는다.
- 백엔드 호출 전에 같은 요청 본문을 pending에 저장한다. 응답 불명/기기 저장 실패는 같은 ID로 재시도. 확정 거절은 원문 보존 후 입력 해제. 요청 결과 불명 상태의 취소/새 ID 발급 금지. 원문이 유효한 backend pending은 로컬 백업 복원 뒤에도 유지하고 같은 요청으로 재조회하며, 식별할 수 없는 손상 요청은 보존/차단한다.
- 앱 pause/disable은 연출만 중단한다. 진행 중 백엔드 작업은 중복 발행하지 않고 재개/다시 열기 때 동일 pending을 확인한다.

## 허용 파일과 담당

메인: Domain/BackendContracts.cs 신규, Domain/LoopData.cs, Domain/LoopRules.cs의 저장 검증만, Runtime/LoopSession.cs, Runtime/LoopStore.cs, Backend/GameBackendCoordinator.cs 신규, Presentation/SymbolLoopApp.cs, Tests/EditMode/SymbolLoopTests.cs, Tests/EditMode/BackendFoundationTests.cs 신규, Tests/PlayMode/SymbolLoopFlowTests.cs. 새 C#/폴더 meta 포함.
보조 작성자 1: Backend/MockAuthorityStore.cs, Backend/MockGameBackend.cs, Backend/GameBackendFactory.cs와 각 CodeMap만. 공통 계약 변경 금지, Unity/Git/자산 변경 금지.
보조 읽기 검토자 1: 최종 변경의 권한·미결 요청/복구 직접 결함 검토, 파일 쓰기 없음.
동시 최대 2명(메인 포함), 누적 위임 최대 2명, 재귀/중복 위임 없음. 메인이 공통 계약·색인·PLAN/REPORT·통합과 Unity 실행을 담당한다.
문서: 본 PLAN, Reports/SERVER_BACKEND_FOUNDATION_REPORT.md, 해당 C# 1:1 CodeMap, CodeMap/INDEX.md, README, GDD v0.3의 저장/권한 설명. 원본 로드맵은 변경하지 않는다.
증거: Temp/server-backend-foundation 및 Docs/Reports/Evidence/BackendFoundation의 실제 최종 결과.

## 구현 순서와 필수 검증

- [x] 원문/현행 색인·대상 코드·기존 PLAN 확인, 사용자 P1 정산 정책 확정.
- [x] 변경 전 `CompleteCampaignSettlesOnlyOnceAndGrowthAppliesToNextRun` 1/1 PASS (0.032초, 전체 4.23초).
- [x] 계약과 독립 Mock 저장소/어댑터 작성. 변조, 중복 ID, revision, 지원 안 되는 모드, 손상 저장 경계 검사.
- [x] 로컬 세션을 런 엔진으로 제한하고 Coordinator에 저장된 비동기 요청·승인 적용·복구 연결.
- [x] 기존 authored UI에서 시작/성장/정산 대기·처리 중/오류/재개 표시. 기존 로비→보스→검증 대기→다음 런 검증.
- [x] 새 핵심 EditMode 및 직접 영향 기존 검사 1묶음, PlayMode 전체 1묶음. 타임아웃 후 재시도, 승인 후 로컬 저장 실패, pause/늦은 응답, 과거 백업 캐시로 권한 재화 덮어쓰기 방지 확인.
- [x] 실제 새 저장/권한 파일 무손실, 보호 파일 해시, CodeMap/meta 관계 확인; 최종 REPORT.

## AC / 종료

AC-01 기존 전체 게임 루프가 Mock 승인 시드로 완주하고 검증 대기 후 새 런 가능.
AC-02 로컬 캐시·런 변조로 권한 재화 증가 없음; 개발 재화 최초 1회 20.
AC-03 시작은 서버 티켓의 시드/규칙/성장/출전만 사용.
AC-04 성장 비용/가능 여부는 backend가 검증.
AC-05 중복/결과 불명/재시작/로컬 저장 실패 시 승인 프로필·진행 보존.
AC-06 기존 저장·아카이브·사용자 자산·Scene/Prefab/패키지 무손실.
AC-07 실제 검증만 PASS, 나머지는 NOT_RUN; CodeMap/INDEX/REPORT 동기화.
최초 통합 검토 1회, 필수 결함 수정·재검증 최대 2회. 시작 0/1, 0/2. 작업 중 작성·컴파일 피드백과 최종 검토를 구분하며 불필요한 반복 전체 검사는 하지 않는다.

## 최초 검토와 1차 수정

- 최초 통합 검토 1/1: EditMode 31/33, 읽기 전용 보조 검토 1회 통합. 초기 PlayMode dispatch는 Editor stopped/running 도구 불일치로 실행 증거가 없어 취소했으며 PASS로 세지 않았다.
- BF-01: 구형 settled=계산 보상 조건이 PendingVerification/0지급을 거절. 저장 검증 계약 수정.
- BR-01: checkpoint 손상 때 pending을 먼저 읽지 않아 복원 과정에서 원 요청 폐기 가능. Session과 독립적으로 읽고 backend 요청은 복원 후에도 보존.
- BR-02: lobby backup에 없는 서버 활성 런 복구 경로 누락. 승인 티켓으로 시작 지점 복원, 같은 ID 진행은 유지.
- BR-03: pending 쓰기에 디스크 flush 누락. FileStream Flush(true) 후 rename 적용.
- 직접 회귀: 아직 발행하지 않은 stale backend 입력은 서버 미확인 상태 확인 후 오류/원문 보존하고 해제. backend 종류인데 본문이 없는 손상 입력은 취소 불가.
- 수정/재검증 1/2: EditMode 35/35 PASS, 7.32초. PlayMode 6/6 PASS(105.74초). 대응 CodeMap/INDEX/GDD/README/REPORT 완료.

- 최종: 필수 AC-01~07 충족. 보호 파일 509개 해시 동일, C#24개 요약/meta 누락0, staged 없음. 실제 캡처 3개와 결과 JSON4개를 Reports/Evidence/BackendFoundation에 보존. 원본 로드맵 수정 없음. P2~P5와 실기기 검증은 후속 범위이며 이번 PASS에 포함하지 않는다.
