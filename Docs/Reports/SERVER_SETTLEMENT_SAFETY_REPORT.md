# SERVER_SETTLEMENT_SAFETY (P3) REPORT

2026-10-21 · IMPLEMENT · **구현·검증 완료**. [PLAN](../Plans/SERVER_SETTLEMENT_SAFETY_PLAN.md)과 사용자 승인에 따라 P3까지만 구현하고 관련 P1~P3 미커밋 작업을 로컬 커밋으로 전달한다. P4는 시작하지 않는다.

## 결과와 계약

같은 런의 중복 정산은 원 영수증을 반환하며 한 번만 지급한다. 요청 ID와 본문을 저장한 뒤 전송하므로 응답 유실·단절·취소·승인 후 로컬 저장 실패에서도 같은 요청을 재확인한다. 같은 ID로 본문을 바꾸면 거절한다. 결과 불명은 `보상 검증 대기`로 표시하며 미결 요청과 실제 예외 진단을 보존한다.

과거 체크포인트에 정산 결과가 없어도 읽기 전용 런 조회에서 원 영수증·시작 티켓·승인 명령과 최신 프로필을 받아 결과를 복구한다. 서버에 다음 활성 런이 있으면 이전 정산을 먼저 합친 뒤 새 티켓을 복원한다. 교체 전 로컬 원문은 `.reconciled-<id>.json`에 보존한다. 같은 활성 런의 남아 있는 진행은 유지한다. 늦게 도착한 과거 프로필은 최신 완료 런과 재화를 덮지 못한다.

이전 사용자 결정인 **P1 대기 기록의 지급0 보존**, 최초20 Mock 재화, protocol1/`symbol-loop-mvp-1`/`symbol-loop-replay-1`, 전투·경제·주사위/스킬 연출은 유지한다. 새 사용자 결정이 필요한 항목은 없다.

## 변경과 직접 관계

- `BackendContracts`/`MockGameBackend`/Factory: `GetRunStatusAsync`와 선택적 응답 명령 사본을 추가했다. 조회는 재검증·지급·빈도 소비·작업 기록 추가를 하지 않는다. 요청 DTO와 기존 fingerprint는 바꾸지 않았다. Firebase 선택은 명시적 미지원이다.
- Coordinator/Session: 원 요청 사본과 동일 본문 재전송, 승인 영수증 재조회, 과거 결과 재구성, 최신 프로필 합치기, 원문 보존을 연결했다. 이미 로컬 확정된 요청의 후속 조회만 실패하면 승인 결과는 유지한다. Coordinator의 동시 진입은 원자적으로 차단한다.
- LoopStore: 같은 절대 경로의 읽기·Commit·복원·pending 비교/정리를 같은 잠금으로 보호한다. revision/generation 충돌은 재로드를 요구한다. 자동 정리는 지정한 pending ID만 대상으로 하며 백업/오류 원문은 고유 파일로 보존한다.
- App: 미확정 정산 표시, 같은 요청 재확인 후 결과 화면 복귀, 백업 복원 중 새 pending 보호를 연결했다. pause/disable은 요청을 지우지 않으며 파괴 취소 후에도 디스크 요청이 남는다.
- 새 EditMode 파일2개와 기존 어댑터/PlayMode를 수정했다. 신규 C# 요약2개 및 직접 변경·관계 요약10개, INDEX(소유 C#30개), README/GDD/PLAN/본 REPORT를 동기화했다.

## 실제 검증

Unity 6000.6.0f1의 기존 Editor 한 개를 CLI/Pipeline으로 직렬 사용했다. Orca·중복 batchmode·실제 사용자 저장·기기 조작은 사용하지 않았다. 모든 저장 시험은 격리한 고유 임시 경로에서 실행했다.

| 검증 | 실제 결과 | 증거 |
|---|---|---|
| 구현 전 최소 회귀 재현 | 과거 백업의 미정산 상태가 그대로 남는 실패1건 확인 | [RED](Evidence/SettlementSafety/red-backup.json) |
| 최초 통합 EditMode / PlayMode | 65/65, 7/7 PASS | [Edit](Evidence/SettlementSafety/edit-initial.json), [Play](Evidence/SettlementSafety/play-initial.json) |
| 검토 수정 후 최종 EditMode | **66/66 PASS**, 실패/스킵0, 10.72초 | [최종 Edit](Evidence/SettlementSafety/edit-final.json) |
| 검토 수정 후 최종 PlayMode | **7/7 PASS**, 실패/스킵0, 106.31초 | [최종 Play](Evidence/SettlementSafety/play-final.json) |
| 보호 경로·요약·meta | **728개 해시 불변**, 소유 C#30개, 요약/meta 누락0 | [소스 감사](Evidence/SettlementSafety/source-audit.json) |

실제 병렬 worker/Barrier로 같은 런 정산10회, 두 독립 로컬 세션의 같은 revision 구매, 정산/구매 경합을 실행했다. 권한 파일 재조회로 지급 한 번과 한 변경만 승인됨을 확인했다. 로컬 Commit의 승자/패자 재로드, 복원 세대에 의한 오래된 입력 차단, 새 pending을 오래된 Clear/Discard가 지우지 못함도 확인했다.

전송 전·지급 후 단절, 지급 후 취소, 응답 유실 뒤 로컬 저장 실패/재시작, 승인 pending을 정산 이전 백업에서 재시도하는 경우를 검사했다. 서버 재화는 재시도 뒤 정확히 한 번 변하며 원 요청/오류/교체 전 체크포인트가 남는다. 성장까지 진행된 최신 프로필을 과거 백업에 합치는 경우와 다음 활성 런 복원도 통과했다. 첫 런 조회를 늦춘 동안 두 런을 정산한 회귀에서는 최신 파일 바이트·두 번째 영수증·잔액120이 유지됐다.

PlayMode는 실제 포인터 입력으로 캠페인 완주·50지급·성장 구매·다음 런, 칸별 이동·숫자/기호 주사위·조합/기본기/반격·중단 후 재개·세로 배치를 다시 확인했다. 추가 검사는 서버 지급 후 응답 유실 → pause/disable → 검증 대기 → 화면 재생성 → 같은 ID 재확인 → 영수증 복구/잔액70을 통과했다. [미확정 화면](Evidence/SettlementSafety/settlement-pending.png)과 [복구된 승인 화면](Evidence/SettlementSafety/settlement-recovered.png)을 직접 확인했다. 화면 자동검사는 실제 Android 수명 주기 검증을 대신하지 않는다.

## 검토와 수정 예산

최초 검토 **1/1**, 검토 후 수정·재검증 **1/2**, 남은 필수 Finding **0건**. 독립 저장 구현1명과 읽기 검토1명에 순서대로 위임했으며 동시 최대2명(메인 포함), 누적2명, 재귀 위임 없음. 공통 계약·문서 통합·Unity·Git은 메인이 담당했다.

| ID | 근거와 영향 | 해결·확인 |
|---|---|---|
| F-P3-01 | RestoreActiveRun의 다른 티켓 경로에 권한 revision 역행 차단이 없어 늦은 첫 런 응답이 새 완료 런을 덮을 수 있었다. | 티켓 복원 전 최신 디스크 authority revision 검사. `LateFirstRunProfileCannotOverwriteASecondSettledRun` PASS. |
| F-P3-02 | App.Restore의 ID 없는 자동 pending 정리가 관찰 뒤 새로 생긴 요청을 제거할 수 있었다. | 관찰한 로컬 ID가 있을 때만 지정 ID로 정리하고 현재 pending 재조회. Store의 늦은 Clear/Discard 경합 검사와 손상 복구/미결 정산 PlayMode PASS. |

두 Finding을 같은 첫 수정 묶음에서 해결한 뒤 위 최종 전체 검사를 실행했다. 이후 C# 변경 없이 문서/증거/커밋만 정리했다. 불필요한 추가 반복은 하지 않았다.

## 보호·Git·종료 범위

기준 HEAD는 `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`, 브랜치는 main, 최초 staged 없음. 기존 P1/P2 및 이번 P3의 승인된 코드·meta·골든·CodeMap·기획·PLAN/REPORT·검증 증거85개를 정확한 경로 목록으로 커밋한다. staged 목록과 검토 목록의 일치를 확인했다. 이 REPORT를 포함한 전달 커밋의 해시는 Git 이력과 최종 응답에서 확인한다. push는 하지 않는다.

Scene/Prefab/Packages/Archive, 완료 P1/P2 PLAN/REPORT/증거, P2 골든, 원본 로드맵은 이번 작업 시작 이후 바이트가 변하지 않았다. 원본 로드맵은 내용 수정 없이 추적하므로 그 안의 체크박스는 작성 당시 제안이다. 실제 진행 상태는 현행 README와 단계별 REPORT를 따른다.

기존 RP 설정2개, Pretendard 폰트 자산, ProjectSettings.asset 및 ProjectSettings/Packages의 사용자 설정2개는 보존하고 커밋에서 제외한다. 실제 사용자 저장의 열기/삭제/이관도 없다.

문서의 로컬 링크 누락은0이다. staged whitespace 검사는 내용 그대로 보존하는 원본 로드맵의 Markdown 줄바꿈4곳과 Unity 생성 Backend/골든 meta의 빈 값6곳만 지적했다. 세 파일의 기존 바이트는 보존했으며 나머지 staged 파일 검사는 통과했다. Git 전역 설정이나 줄바꿈 정책은 변경하지 않았다.

P3 AC1~AC5는 위 검증으로 충족했고 AC6의 문서/보호 검사도 통과했다. 사용자 승인 커밋으로 전달하고 **P3에서 종료**한다. 동시성 보장은 같은 프로세스에서 같은 절대 경로 잠금을 공유하는 Mock 범위다. 별도 프로세스의 같은 파일 직접 공유는 지원하지 않는다. 실제 두 기기·네트워크·Firebase/Auth/Functions/Emulator(P4), Android/IL2CPP(P5)는 **NOT_RUN/후속**이다. Mock은 운영 보안 서버가 아니며 미래 예측·합법 로그 자동 구성까지 방어하는 기능은 이번 범위에 없다.
