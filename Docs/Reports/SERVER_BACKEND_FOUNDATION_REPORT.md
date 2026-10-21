# SERVER_BACKEND_FOUNDATION_01 REPORT

2026-10-21 · **P0+P1 완료** · 기준 HEAD `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`.
[PLAN](../Plans/SERVER_BACKEND_FOUNDATION_PLAN.md), [원본 로드맵](../Dice_Warrior_Next_Work_MockBackend_Firebase_Roadmap_v0.1.md).
현재 변경은 미커밋이며 stage/commit/push를 수행하지 않았다.

## 결과와 사용자 결정

기존 보드·주사위·기호 전투·연출을 유지하면서 런 시작/영구 성장/기록 제출을 IGameBackend 경계로 분리했다.
사용자는 P1 정산 정책으로 **검증 대기 보존 + 성장 확인용 Mock 재화 최초 1회 지급**을 선택했다.
따라서 새 개발 프로필의 최초 재화는 20, 런 기록 제출은 PendingVerification, 지급액은 0이다.
리플레이 합법성 검증이나 실제 보상 지급이 구현되었다는 의미가 아니다.

- 순수 Core 계약: 프로토콜 1, `symbol-loop-mvp-1`, 비동기 API/취소/오류·결과 불명, 시작 티켓·명령 로그·제출 영수증.
- Mock 권한 저장: 체크섬, flush, 원자 교체/정상 백업, 초기화 marker, 경로별 프로세스 내 잠금. 재조회/재시작은 최초 재화를 다시 지급하지 않는다.
- 시작 티켓: 서버 시드·규칙·성장·출전 선택을 보존한다. UI/로컬 프로필의 성장 수치를 새 런 권한으로 사용하지 않는다.
- 성장: 백엔드가 비용/잔액/revision/활성 런 여부를 확인한다. 같은 요청 ID+본문은 같은 결과, 다른 본문은 거절한다.
- 로컬 실행: 저장 성공한 명령만 순서/선택/ID와 함께 기록한다. 주사위/HP/승리/재화 결과를 정산 권한 입력으로 보내지 않는다.
- 제출: 명령 형식과 보관 한도만 확인하여 검증 대기로 저장한다. 같은 런 반복 제출도 보상을 지급하지 않으며 이후 새 런을 시작할 수 있다.
- 복구: 요청을 디스크에 flush한 뒤 발행한다. 응답 유실·취소·로컬 저장 실패 시 원 요청을 보존하여 재조회/재전송한다. 손상 checkpoint를 복원해도 backend pending은 유지한다.
- 백업에 없는 활성 승인 런은 시작 티켓으로 복구하고, ID가 일치하는 기존 진행/RNG/명령 로그는 초기화하지 않는다. 권한 프로필은 재조회하여 로컬 백업의 과거 잔액으로 되돌리지 않는다.
- 생성 지점은 한 곳이며 Firebase 선택 시 Unsupported를 반환한다. SDK/클라우드/인증 연결은 없다.

## 저장·보호 범위

새 실제 경로:

- `Application.persistentDataPath/symbol-loop-backend-v1/mock/checkpoint.json` — schema2 로컬 진행/권한 캐시/명령 로그와 pending.
- 같은 폴더 `authority.json` — 개발용 프로필/승인 런/제출/작업 결과. backup/marker 포함.

이전 `symbol-loop-v1/player.json`과 백업은 열거나 삭제/이관하지 않는다. 기존 진행은 그 원본에 남으며 새 Mock 계정으로 자동 승격하지 않는다.
원본 로드맵, 기존 사용자 설정/폰트 변경, Scene/Prefab/Packages를 포함한 보호 파일 **509개 해시 동일**.
Archive/v1 변경 없음. 사용자 자산 변경 없음. 모든 검사는 고유 임시 저장소를 사용했다.

Mock 저장소는 단일 프로세스 개발 대역이다. 128런/1024작업/2048명령/512KiB 제출/128MiB 전체 파일 한도 초과 시 명시 거절하며 자동 삭제하지 않는다.
권한 파일 손상·주 파일 소실 시 자동 초기화/백업 잔액 복원을 하지 않는다. 손상된 식별 불가능 pending도 원문을 보존하고 자동 취소하지 않는다.
이 경우 수동 진단이 필요하며, 전체 운영 복구 정책/보관 정리는 P3 이후 범위다.

## 실제 검증

| 검사 | 실제 결과 | 근거 |
|---|---|---|
| 변경 전 완주→정산→성장→다음 런 | 1/1 PASS | 기존 회귀 `CompleteCampaignSettlesOnlyOnceAndGrowthAppliesToNextRun`, 0.032초/실행 4.23초 |
| 최종 컴파일 | errors 0 | Pipeline recompile_status completed, failed=false; 리로드 중 일시 연결 종료 후 확인 |
| 최초 EditMode | 31/33, 실패 2 | [초기 JSON](Evidence/BackendFoundation/edit-initial.json), 같은 구형 정산 검증 조건 실패 |
| 수정 후 EditMode | **35/35 PASS**, 7.32초 | [최종 JSON](Evidence/BackendFoundation/edit-final.json) |
| 실제 PlayMode | **6/6 PASS**, 105.74초 | [최종 JSON](Evidence/BackendFoundation/play-final.json) |
| 보호/문서/meta | 보호 509개 동일, C# 24개 누락 0 | [소스 검사](Evidence/BackendFoundation/source-audit.json) |
| 실제 캡처 | Mock 20/성장0, 승리 후 검증 대기·지급0 문구 확인 | [로비](Evidence/BackendFoundation/lobby-mock.png), [결과](Evidence/BackendFoundation/result-pending.png), [지연 승인 후 재개](Evidence/BackendFoundation/approved-after-pause.png) |

PlayMode 6건: 실제 포인터 캠페인 완주/기록 제출/성장 구매/HP65 다음 런, 칸별 이동·중단 복귀, 기호 주사위·스킬 순서, 세로/safe area, 손상 checkpoint+미결 start 복원, 지연 승인 중 pause/disable/중복 입력 차단.
EditMode는 별도 권한 저장, 캐시 변조, 서버 시작 입력, 성장 비용/revision, 같은 요청 10회, ID 본문 변경 거절, 서로 다른 어댑터의 동시 구매, 응답 유실, 승인 후 저장 실패/재시작, 시작 전 백업/성장 백업 복구를 포함한다.

첫 PlayMode dispatch는 도구 running/Editor stopped 및 완료 증거 누락으로 취소했다. 실제 실행으로 집계하지 않았다.
컴파일 완료 후 실행한 6개 결과만 PASS로 기록한다. 과거 테스트 수를 이번 결과로 재사용하지 않았다.

## 검토·수정 횟수와 관계 갱신

최초 통합 검토 **1/1**, 후속 수정·재검증 **1/2** 사용. 추가 검토 묶음을 만들지 않았다.
메인이 공통 계약/세션/UI/검사/문서/Unity 실행을 담당하고, 보조 1명은 Mock 3개 파일, 보조 1명은 읽기 전용 권한·복구 검토를 담당했다. 동시 최대 2명, 누적 2명, 재귀 위임 없음.

| Finding | 원인 / 해결 | 확인 |
|---|---|---|
| BF-01 | 구형 settled payout 산식이 새 0지급을 거절 → 검증 대기 영수증 계약 적용 | EditMode 정상 완주/중복 제출 및 UI 완주 PASS |
| BR-01 | Session 생성 실패 때 pending 미조회/복원 중 폐기 → 독립 조회와 backend 원문 유지 | 손상 저장+미결 승인 복원 PlayMode PASS |
| BR-02 | lobby backup 복원 후 서버 활성 티켓 무시 → 시작 티켓 복구/동일 진행 보존 | EditMode 두 경우 PASS |
| BR-03 | pending 디스크 flush 누락 → flush 후 rename | 코드 확인, pending/restart 검사 PASS; 실제 전원 차단 시험은 NOT_RUN |

신규 6개 C#과 수정 7개 C#의 1:1 CodeMap, 직접 관계가 바뀐 Config 요약, INDEX, README, GDD를 동기화했다. 총 프로젝트 소유 C# 24개에 요약/meta 누락이 없다. 남은 필수 Finding 없음.

## 미실행·후속 범위

- P2 독립 리플레이 검증, 악성/위조 로그 적법성 판정과 골든 벡터: **NOT_IMPLEMENTED**.
- P3 두 기기/프로세스 간 전체 정산 원자성·재화 지급 복구·보관 정리: **NOT_IMPLEMENTED**. 이번 잠금/재시도 검사는 P1의 로컬/Mock 경계에 한정한다.
- Firebase SDK/Auth/Functions/Firestore/App Check/Emulator 및 배포: **NOT_IMPLEMENTED / NOT_RUN**.
- Android ARM64 IL2CPP/실기기/실제 OS 중단·네트워크·전원 장애: **NOT_RUN**.
- 조건부 3/4눈, 경제 밸런스, 아트 변경 없음.

다음 순서는 로드맵의 `SERVER_RUN_REPLAY_VALIDATION (P2)`다. 기존 시작 티켓과 명령 기록을 독립 재실행해 합법성을 판정한 뒤에야 영구 런 보상 지급을 연결할 수 있다.
