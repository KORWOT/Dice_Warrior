# 로컬 서버 더블클릭 실행 PLAN

2026-10-23 · IMPLEMENT · 기준 `bd9f32a671e7b7b6934aad8902ff7a5a08f151e5` / main / staged0.

## 계약과 범위

사용자는 로컬 서버를 실행할 별도 실행파일을 요청했다. 기존 `Server/Start-MockServer.ps1`을 재사용하는 저장소 최상위 `Start-MockServer.cmd`를 제공한다. Windows에서 더블클릭하면 현재 설치된 .NET SDK로 기존 Mock 서버를 실행하고 로그를 표시한다. 기본 loopback5087·Server/data/authority.db와 기존 PowerShell의 Port/DatabasePath 인수 계약을 유지한다. 경로 공백과 다른 작업 디렉터리에서도 실행되어야 하며 시작 실패 메시지는 창에 남긴다. 종료는 Ctrl+C 또는 실행 창 닫기다.

이는 기존 개발 환경용 진입점이다. SDK를 포함한 독립 배포 EXE·자동 설치·브라우저 자동 열기·서비스 등록·게임/서버 C# 변경·기본 사용자 DB를 사용하는 시험·Unity/ORCA·Git stage/commit/push는 범위 밖이다. 이전 사용자 변경12파일을 해시로 보호한다. `Docs/AI`는 현재 없으므로 AGENTS와 기존 서버 PLAN을 따른다.

## 파일과 담당

- 메인 단독 작성: 신규 `Start-MockServer.cmd`, 이 PLAN, `Docs/Reports/LOCAL_SERVER_LAUNCHER_08_REPORT.md`, 기존 `Server/README.md`, `Docs/README.md`의 직접 실행 안내.
- 증거/격리 DB: ignored `artifacts/local-server-launcher/run-01/`. 제품 DB·metrics는 읽거나 변경하지 않는다.
- 기존 runner 보조1명은 실행 진입점·인수/경로·실패 전달의 직접 결함만 읽기 전용으로 검토한다. 입력은 기존 PS1/새 CMD/이 PLAN, 산출물은 필수 Finding 또는 없음. 쓰기·실행·재위임 금지. 동시 최대2명, 누적 보조1명. 최초 검토1묶음, 최초 검증 뒤 수정·재검증 최대2회.
- C#·직접 의존 계약 변화가 없어 CodeMap 추가/수정은 비적용이다. 전체 테스트 추가·제품 회귀 재실행도 하지 않는다.

## 진행과 필수 검증

- [x] baseline/사용자12파일 해시 보존.
- [x] CMD 구현과 직접 실행 안내 갱신.
- [x] 격리 포트·DB로 CMD를 실제 실행해 healthz와 dashboard HTTP200 확인. 공백 포함 DB 경로와 다른 CWD, 잘못된 포트의 실패 코드/메시지 전달 확인. 시작한 시험 프로세스만 종료.
- [x] 읽기 전용 검토 결과 통합, 사용자 해시/변경 범위/문서 링크/diff 확인, REPORT 작성.

최초 검토1묶음·필수Finding0, 수정·재검증0/2. 기존 사용자12파일 SHA256 변화0, 기존 PS1 불변, staged0. CMD 실제 기동은 격리59339포트에서 healthz/dashboard200이며 시작 실패2경로(잘못된 포트/SDK 없음)의 메시지·exit1·pause 안내를 확인했다. 첫 sandbox 시도는 loopback 접근 제한으로 중단됐고, 허용된 외부 실행에서 같은 CMD를 확인했다. 실제 Explorer 더블클릭/눈으로 보는 콘솔·Ctrl+C UI는 NOT_RUN이며 자동 CLI 실행과 구분한다. 상세 증거는 [REPORT](../Reports/LOCAL_SERVER_LAUNCHER_08_REPORT.md)에 기록했다. 구현 완료, Git 커밋/푸시 없음.

## 사용자 승인 커밋 (2026-10-24)

사용자가 별도로 남은 실행 도우미와 관련 문서의 로컬 커밋을 승인했다. 기준은 v5 커밋 `d4b9a79c0530161352d4fd2f9b5dd7281064b453`, main, staged0이다. 대상은 위5파일만이며 이후 추가된 원문을 포함한 기존 사용자13파일은 해시로 보호하고 제외한다. 기존 검증 자료와 CMD/PS1 해시를 대조하며 제품 변경이나 서버 재실행은 하지 않는다. 정확한 staging 목록·커밋 후 파일 목록·남은 변경을 확인하고 푸시는 하지 않는다. 메인은 Git/문서를 맡고 기존 runner 보조1명은 증거와 범위를 읽기 전용으로 대조한다. 같은 동시2명·누적보조1명 상한이며 새 제품 검토·쓰기·재위임은 하지 않는다.
