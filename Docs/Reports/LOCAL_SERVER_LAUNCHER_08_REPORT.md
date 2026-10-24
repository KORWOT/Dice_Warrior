# 로컬 서버 더블클릭 실행 REPORT

2026-10-23 · **PASS** · [PLAN](../Plans/LOCAL_SERVER_LAUNCHER_08_PLAN.md).

## 결과와 사용법

프로젝트 최상위 [Start-MockServer.cmd](../../Start-MockServer.cmd)를 추가했다. Windows에서 더블클릭하면 기존 `Server/Start-MockServer.ps1`이 실행되고 서버 로그 창이 유지된다. 기본 주소는 `http://127.0.0.1:5087`, 대시보드는 `/dev/dashboard`, DB는 기존 `Server/data/authority.db`다. `Ctrl+C` 또는 실행 창 닫기로 종료한다. 시작 실패 시 메시지와 exit code를 표시하고 키 입력을 기다린다.

기존 Port/DatabasePath 인수를 그대로 전달하고 `%~dp0`로 PS1을 찾으므로 현재 작업 디렉터리에 의존하지 않는다. SDK 검색 실패 때 설치 안내를 표시한다. Windows PowerShell의 `-ExecutionPolicy Bypass`는 해당 프로세스에만 적용하며 시스템 정책을 변경하지 않는다. .NET SDK10.0.302/호환10.0.3xx와 기존 저장소가 필요한 실행 도우미이며 SDK를 포함한 배포 EXE는 아니다. 기존 PS1·서버/게임 C#·저장 계약을 변경하지 않았다.

## 실제 확인

| 확인 | 결과 |
|---|---|
| 실제 CMD → PowerShell → .NET 서버 기동 | PASS, 별도 CWD와 공백 포함 시험 DB 경로, 포트59339 |
| GET /healthz | HTTP200, status ok/development-mock/protocol1/symbol-loop-mvp-1 |
| GET /dev/dashboard | HTTP200, 7,852bytes |
| 잘못된 Port1000 | exit1, 기존 포트 범위 오류·CMD 종료 코드·pause 안내 유지 |
| SDK 없는 PATH를 해당 자식 프로세스에만 적용 | exit1, SDK 설치·pause 안내, 원 PATH 복원 |
| 읽기 전용 경로·인수·실패 전달 검토 | 필수Finding0 |
| 보호 | 사용자12파일 SHA256 변화0, 기존 PS1 해시 일치, staged0 |

실제 기동 준비는 약10.19초였다. 시험은 ignored `artifacts/local-server-launcher/run-01/fixture with spaces/authority.db`를 인수로 지정했고, 기존 사용자 DB/API 계정은 사용하지 않았다. 시험용 CMD의 소유 PID26808과 자식만 종료했다. 첫 sandbox 실행은 로컬 소켓 접근 제한으로 준비 확인이 불가했으며, 격리 시험을 외부 실행 권한으로 진행해 위 결과를 얻었다. 이 환경 문제를 제품 결함이나 PASS로 세지 않았다.

CLI에서 CMD 자체를 실행했다. Explorer에서 실제 더블클릭하는 화면·사용자 Ctrl+C 입력은 NOT_RUN이다. Unity·전체 서버/게임 회귀 검사는 실행하지 않았고, 직접 실행 도우미 확인으로 한정했다. 기본5087 서버를 계속 실행해 두지는 않았다.

증거는 ignored `artifacts/local-server-launcher/run-01/`의 baseline/user-before, smoke-owned/smoke-result 및 stdout/stderr, invalid-port, missing-sdk, final-audit에 남겼다. 문서/실행파일만5개 변경했고 C# 변경이 없어 CodeMap은 비적용이다. Server/Docs README의 직접 안내를 동기화했다. 최초검토1묶음, 수정·재검증0/2, 남은 필수Finding0. Git 커밋/푸시는 하지 않았다.

## 사용자 승인 커밋 체크포인트 (2026-10-24)

구현 완료 후 사용자가 실행 도우미와 관련 문서의 로컬 커밋을 승인했다. 기준은 `d4b9a79c0530161352d4fd2f9b5dd7281064b453`이며 위5파일만 포함한다. v5가 이미 커밋된 두 README에는 실행 도우미 안내 변경만 남아 있음을 확인했다. 사용자 원문·폰트·설정13파일은 제외하고 커밋 전후 SHA256을 대조한다.

현재 CMD는 v5 작업 전 보존 해시와 같고 기존 PS1은 실행 도우미 baseline 해시와 일치한다. smoke-result의 health/dashboard200, 실패2경로의 exit1·pause 증거도 위 표와 대조했다. 제품 변경이 없어 서버를 다시 실행하지 않았으며 이전 검증 결과와 미실행 UI 범위를 그대로 유지한다. 정확한 파일 목록·제외 파일 해시·staging/커밋 후 감사는 ignored `artifacts/local-server-launcher/run-01/commit-*`에 기록한다. 푸시는 요청되지 않았다.
