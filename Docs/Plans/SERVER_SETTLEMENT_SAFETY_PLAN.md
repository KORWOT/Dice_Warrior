# SERVER_SETTLEMENT_SAFETY (P3) PLAN

IMPLEMENT · 2026-10-21 · 구현·검증 완료, 사용자 승인 로컬 커밋으로 전달.
승인: 사용자 “P3 중복 정산·비동기 복구·동시 실행 안전성…문서…P3까지만 진행하고 커밋”.
기준: [원본 로드맵 P3](../Dice_Warrior_Next_Work_MockBackend_Firebase_Roadmap_v0.1.md), 완료 P1/P2 PLAN/REPORT.

## 목표·고정 범위

정산·성장·시작의 확정 결과는 동일 요청으로 복구하고, 두 세션의 같은 revision 경합에서 영구 자산은 한 번만 변한다. 과거 백업을 복원해도 서버의 최신 프로필과 해당 런 영수증이 우선하며, 대체되는 로컬 진행과 오류 원문은 보존한다. P3 완료 후 관련 P1~P3 미커밋 작업을 정확한 파일 목록으로 커밋하고 멈춘다.

- HEAD/main `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`, 기존 staged 없음. 기존 P1/P2 미커밋 작업은 승인된 연속 기준이다. reset/stash/브랜치 변경 없음.
- 기준 상태/해시: `Temp/server-settlement-safety/before.json`. 기존 사용자 RP 두 설정·폰트·ProjectSettings/ProjectSettings.asset·ProjectSettings/Packages 두 파일을 편집/스테이징하지 않는다.
- Scene/Prefab/Packages/Archive, 원본 로드맵, 완료 P1/P2 PLAN/REPORT/증거 및 P2 골든 JSON을 보존한다. 원본 로드맵은 내용 변경 없이 관련 문서와 함께 추적한다.
- P1 기존 pending 무지급, 최초20 Mock 재화, protocol1/규칙/검증 버전·전투·경제·연출을 유지한다. Firebase/P4/P5/기기/아트/다중 프로세스 파일 DB는 비목표다. Orca 금지.
- Unity Editor 하나만 직렬 사용. 검사는 격리 고유 임시 파일만 사용한다. 같은 프로세스의 같은 절대 경로는 잠금과 revision/generation으로 보호하고 두 기기 모델은 서로 다른 로컬 경로+공유 Mock 권한 경로로 시험한다. 별도 프로세스가 동일 파일을 직접 공유하는 구성은 지원하지 않는다.

## 계약·설계

1. `IGameBackend.GetRunStatusAsync(string serverRunId, CancellationToken)`을 읽기 전용으로 추가한다. Mock은 소유 런의 티켓·제출 영수증·저장 명령 사본과 최신 프로필을 반환한다. 활성 런은 submission=null, 기존 P1 pending은 재검증/지급 없이 원 영수증을 반환한다. 요청 DTO는 변경하지 않아 기존 fingerprint를 유지한다. BackendReply에 `commands`만 추가한다.
2. Session은 승인된 P2 로그/티켓으로 결과를 재구성해 영수증을 연결할 수 있다. 같은 런의 과거 Map/Combat 백업도 복구한다. P1 pending은 결과 검증을 새로 하지 않고 0지급 종료로 표시한다. 서버 영수증만 조회해도 새 지급/요청 ID를 만들지 않는다.
3. Coordinator 초기화는 로컬 런과 서버 활성 런을 대조하고 필요한 런 상태를 조회한다. 서버에 정산된 로컬 런은 먼저 종료 결과를 합친 뒤 새 활성 런을 복원한다. 완료 상태/새 티켓으로 교체되는 로컬 체크포인트는 `.reconciled-<id>.json` 사본으로 먼저 보존한다. 일치하는 활성 런의 로컬 명령 진행은 유지한다. 조회 실패/소유권 불일치는 진행을 임의 초기화하지 않는다.
4. 확정 응답은 먼저 원 요청 본문을 서버에 재전송해 fingerprint를 확인한다. 정산 승인 시 최신 런 조회로 결과/프로필을 복구하고, 시작/성장 승인 후 최신 프로필/활성 런을 재확인한다. 이미 적용한 로컬 영수증도 최신 서버 잔액을 뒤로 돌리지 않는다.
5. 타임아웃/취소/전송 단절은 결과 불명으로 pending을 유지한다. 오류의 실제 예외 정보는 진단 파일에 보존하고 화면에는 요약을 표시한다. 정산 미확정 화면은 검증 대기/미지급이며 승인처럼 보이지 않는다. 같은 요청 재시도만 허용한다.
6. LoopStore의 읽기/복원/미결 입력 compare→정리도 Commit과 같은 경로 잠금으로 보호한다. `DiscardPending(string expectedId=null)`은 자동 처리 시 지정 ID만 보존 이동한다. 명시 사용자 취소는 손상 원문을 읽지 않고 보존할 수 있다. Commit 경합은 한 후보만 성공하고 패자는 재로드 후 새 입력을 만든다.
7. 기존 Mock 원자 지급/요청 멱등 구조는 유지한다. 진짜 동시 Task/barrier로 같은 런 정산10회·같은 revision 구매·정산/구매 경합을 확인한다. 결과와 잔액은 실제 파일 재조회로 판정한다.

## 담당·허용 파일

- 메인: Domain/BackendContracts.cs, Backend/MockGameBackend.cs·GameBackendFactory.cs·GameBackendCoordinator.cs, Runtime/LoopSession.cs, Presentation/SymbolLoopApp.cs; 새 Tests/EditMode/BackendSettlementSafetyTests.cs; 필요한 기존 테스트 어댑터(BackendFoundationTests.cs, BackendReplayIntegrationTests.cs, PlayMode/SymbolLoopFlowTests.cs)와 직접 회귀 검사. 해당 CodeMap, INDEX/README/GDD/PLAN/REPORT, 증거/커밋 담당.
- 독립 구현 보조 1명: Runtime/LoopStore.cs, 새 Tests/EditMode/LoopStoreConcurrencyTests.cs 및 두 CodeMap만 작성. API: `PreserveCheckpoint()`→보존 파일 경로, `DiscardPending(string expectedId=null)`, `RecordBackendFailure(Command, BackendReply, string diagnostic=null)`를 제공. 저장 잠금·동시성 검사. 다른 소스/공통 계약/Unity/Git 수정 금지.
- 읽기 검토 보조 1명: 최종 결과 복구/중복지급/경합/보호 범위만 검토, 쓰기/Unity/Git 없음.
- 동시 최대2(메인 포함), 누적2위임, 재귀 금지. 구현 보조 종료 후 검토 보조 사용. 같은 파일 작성자 한 명, 공통 계약과 Unity 실행은 메인 전담.
- 허용 문서: 본 PLAN, Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md, 직접 코드 요약, INDEX/README/GDD. 새 증거 Temp/server-settlement-safety 및 Reports/Evidence/SettlementSafety. 완료 문서는 이력 보존.
- 커밋 허용: 현재 승인된 SymbolLoop P1~P3 코드/meta/테스트/골든, 해당 CodeMap·색인·기획·원본 로드맵·PLAN/REPORT/증거. 정확한 파일 목록과 staged diff 검토 후 로컬 commit만 한다.

## 실행·AC·검증 예산

- [x] P1/P2 계약·원본 P3·현재 코드/저장 경계와 Git 상태 확인.
- [x] 격리 실패 재현 검사 작성: 과거 백업 정산 복구 실패1건 확인 후 전송 단절, stale 요청, 두 세션 경합 검증.
- [x] 조회 계약/복구 합치기/미결 상태 UI, 저장 잠금 구현.
- [x] EditMode/PlayMode 최초 전체 65/65·7/7 PASS, 검토 보완 후 최종 66/66·7/7 PASS.
- [x] 직접 코드 검토·보호728개 해시 불변·C#30개 CodeMap/meta·REPORT 동기화.
- [x] 관련85개 파일의 정확한 stage 목록/diff 검토 완료. 사용자 파일6개를 제외하고 이 PLAN을 포함하는 승인된 로컬 커밋으로 전달 후 종료.

AC1 같은 런 동시10제출/같은 ID 재시도/ID본문 위조에 지급 한 번.
AC2 응답 유실·네트워크 단절·저장 실패/재시작에서 원 요청 복구, 오류/진행 원문 보존.
AC3 로컬 과거 백업/완료 전 상태에도 서버 정산 영수증·최신 재화/성장 복구, 서버가 새 활성 런을 가진 경우 순차 합치기.
AC4 동일 revision 구매·정산 경합은 한 유효 변경, 로컬 Commit/복원/미결 정리는 최신 입력을 지우지 않으며 재로드로 진행 가능.
AC5 실제 UI 검증 대기·승인 지급과 pause/disable 재개, 기존 플레이/연출 유지.
AC6 문서/요약/meta/보호 경로, 사용자 파일 제외 커밋 완료. P4를 시작하지 않는다.

최초 검토 1묶음(1/1), 이후 필수 수정·재검증 총 최대2회(1/2 사용). 검증 전 회귀 재현은 필요한 최소 사례에 한정한다. 성공/차단 시 종료하며 불필요한 반복을 하지 않는다.

## 최초 검토 Finding

- F-P3-01: RestoreActiveRun의 다른 티켓 경로에 authority revision 역행 차단이 없어 늦은 프로필 응답이 새 완료 런/재화를 과거 런으로 덮을 수 있음. 티켓 복원 전 최신 디스크 revision을 확인하고 지연 조회 회귀로 해결 확인.
- F-P3-02: App.Restore의 자동 DiscardPending()이 관찰한 ID 없이 실행되어 그 사이 생긴 새 요청까지 활성 pending에서 제거할 수 있음. 관찰한 로컬 입력 ID가 있을 때만 지정 ID로 정리하며, 복원 후 다시 읽어 현재 pending을 화면에 반영.
- 최초 EditMode 65/65 PASS. 최초 검토 1/1 진행 완료, 위 두 직접 결함을 첫 수정·재검증 묶음에서 처리한다.
- 두 Finding은 해결 완료. F-P3-01 지연 프로필 회귀와 F-P3-02 지정 ID pending 보호/복구 검사가 통과했다. 최종 증거·범위·미검증은 [REPORT](../Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md)에 기록했다. 추가 결정 없음. P4 착수 없음.
