# STANDALONE_MOCK_SERVER_01 REPORT

2026-10-22 · IMPLEMENT · **M0~M3 완료**. 사용자 선택은 독립 서버·SQLite·Unity HTTP 연결까지다. 원본 제안의 M4/M5 계측·비용 대시보드·규모별 부하 시험은 이번 완료 범위에 포함하지 않는다.

## 구현 결과

권한 서비스·런 검증기·저장 인터페이스를 Unity Assets 밖 `Server/`로 분리했다. .NET10 ASP.NET Core 프로세스가 6개 HTTP API와 SQLite 권한 저장을 제공한다. 서버를 실행하는 데 Unity Editor는 필요하지 않다. [실행 안내](../../Server/README.md)의 `./Server/Start-MockServer.ps1`을 실행한 뒤 기존 Title 씬에서 Play한다. 기본 주소는 `http://127.0.0.1:5087`, 권한 DB는 `Server/data/authority.db`다.

Unity 기본 전송은 HttpMock이다. 로컬 이동·전투를 유지하면서 프로필·런 시작·제출·성장·작업/런 조회는 실제 HTTP로 처리한다. endpoint/개발 계정별 체크포인트를 새 `http-mock/<hash>` 경로로 분리했다. 서버가 응답하지 않아도 로컬 Mock으로 자동 전환하지 않는다. 기존 실제 사용자 저장 및 `authority.json`을 읽거나 새 DB로 이관하지 않는다.

Domain은 Unity와 서버가 같은 소스를 컴파일한다. LoopJson을 순수 Domain으로 추출했고 서버 Shared의 어셈블리 이름을 SymbolLoop.Core로 맞췄다. 서버 콘텐츠는 버전별 JSON과 정규화 SHA256 `A9FF4C352251F537C93752E9AF82ADBA2725465FEC8E868D9659512740910B38`로 고정하며 Unity Config 동치를 검사했다. protocol1/rules/verification 버전, 4골든·121명령·125상태, P1 대기 지급0/최초20 정책을 유지했다.

SQLite는 WAL·busy timeout·사용자별 저장 버전을 사용한다. 재현 계산을 쓰기 트랜잭션 밖에서 수행하고 트랜잭션 안에서 최신 버전/CAS와 유니크 키를 재확인하여 프로필·런·작업·영수증을 함께 확정한다. 구형 JSON 권한 저장소는 Editor 회귀 검사 전용으로 남았다. 권한 DLL은 Editor 전용·명시 참조이며 플레이어 컴파일 그래프에서 제외됨을 확인했다.

## 실제 검증

모든 검사는 고유 임시 저장/개발 계정을 사용했다. 과거 P3의 통과 수를 이번 결과로 재사용하지 않았다.

| 검증 | 최신 결과 | 증거 |
|---|---|---|
| 독립 .NET 권한/SQLite/골든 | **13/13 PASS**, 실패/스킵0 | [TRX](Evidence/StandaloneMockServer/sqlite-final.trx), [출력](Evidence/StandaloneMockServer/sqlite-final.txt) |
| 실제 HTTP 프로세스 통합 | **18/18 PASS**, 실패/스킵0 | [TRX](Evidence/StandaloneMockServer/http-final.trx), [출력](Evidence/StandaloneMockServer/http-final.txt) |
| Unity EditMode | **83/83 PASS**, 실패/스킵0 | [결과](Evidence/StandaloneMockServer/edit-final.json) |
| Unity PlayMode | **7/7 PASS**, 실패/스킵0, 106.77초 | [결과](Evidence/StandaloneMockServer/play-final.json) |
| Unity 컴파일/권한 분리/콘텐츠 | 컴파일 오류0, 플레이어 권한 DLL 참조 없음, Config 동치 | [컴파일](Evidence/StandaloneMockServer/unity-compile-final.json), [경계](Evidence/StandaloneMockServer/unity-boundary.json) |
| 기동 보호 | Production·외부 인터페이스 시작 거절 | [결과](Evidence/StandaloneMockServer/startup-guards.json) |
| 소스/관계/보호 | Unity31·Server15 요약 누락0, Unity meta 누락0, 이동 meta3개 일치, 보호 추적 경로236개 변경0 | [감사](Evidence/StandaloneMockServer/source-audit.json) |

독립 서버 시험은 동일 DB를 여는 두 서버 프로세스에서 같은 런을100번 제출하고 같은 revision으로10번 구매했다. 정산은 영수증1개/추가50재화 한 번, 구매는 승인1개/경합9개였다. 계정A/B 런·작업 조회와 제출 격리, 불법 로그·클라이언트 결과값 위조 무지급, 필드 순서가 다른 요청의 같은 fingerprint, 요청/계정 ID 경계도 확인했다. 저장 실패 rollback/재시작·취소·손상 DB의 자동 초기화 거절을 검사했다.

실제 소켓의 전송 전 단절, 지급 후 응답 유실, 지급 후 지연/호출자 취소에서 원 pending ID/본문을 보존했다. 서버를 종료하고 같은 DB로 다시 실행한 뒤 같은 요청으로 영수증과 잔액70을 복구했다. 과거 체크포인트 백업 복원도 서버 결과와 합치며 교체 전 원문을 보존했다. 100동시 제출은 정합성 시험이며 운영 처리량/동접 성능 측정이 아니다.

Unity PlayMode의 실제 포인터 완주는 HTTP 서버를 통해 로비→3지역→보스→50지급→성장→다음 런을 통과했다. 지급 후 응답 유실에서 pause/disable·화면 재생성·같은 ID 재확인·승인 영수증 복구/잔액70도 통과했다. [미확정 화면](Evidence/StandaloneMockServer/settlement-pending.png)과 [복구된 승인 화면](Evidence/StandaloneMockServer/settlement-recovered.png)을 직접 확인했다. 기존 칸별 이동·주사위·스킬/반격·연출 중단/재개·세로 배치 검사도 통과했다.

## 검토·실패·수정 기록

최초 독립 검토 **1/1**, 검토 후 수정·재검증 **2/2**, 남은 필수 Finding **0**. 구현 보조2명과 읽기 검토1명에 총3회 위임했으며 동시 최대3명(메인 포함), 재귀 없음. 메인이 공통 계약·문서·Unity 실행·통합을 담당했다.

| ID | 근거·영향 | 수정·해결 조건 |
|---|---|---|
| F-M03-01 | POST가 수락하는 요청 ID와 GET 경로/클라이언트 제한이 달라 일부 작업의 영수증 조회가 불가능했다. 기본 GUID UI 경로의 중복 지급을 입증한 결함은 아니다. | POST/GET 모두 `[A-Za-z0-9._:-]{1,128}`. `_buy`, 점/콜론,128자 조회 동치와 slash/공백/129자 무변경 거절 PASS. |
| F-M03-02 | 클라이언트 개발 ID128자와 서버64자 제한이 달랐다. | 별도 계정 규칙 `[A-Za-z0-9_-]{1,64}`로 일치.64자 승인/65자 거절 PASS. |
| F-M03-03 | 새 취소/시간 초과 검사 대역이 Unity 동기 NUnit 단언과 메인 스레드 문맥을 기다려 Editor가 멈췄다. | 대역 ConfigureAwait(false), 단언에 원 Task 반환. 사용자 승인 후 Editor 재시작,83개 EditMode PASS. |
| F-M03-04 | 테스트 DLL 연결 시 추가한 PlayMode asmdef의 Editor 플랫폼 제한 때문에 화면7개가 EditMode로 분류됐다. | 기존 플랫폼 분류 복원, 소스 UNITY_EDITOR 조건/Editor 전용 DLL 유지. PlayMode7개 PASS와 플레이어 참조 없음 재확인. |

첫 수정 묶음은 ID 계약과 검사 대기, 두 번째는 asmdef 직접 회귀다. 최초 SQLite 대역 실패는 [RED](Evidence/StandaloneMockServer/sqlite-red.trx), 초기 골든 DTO 누락 수정 과정은 [초기 결과](Evidence/StandaloneMockServer/sqlite-initial.trx)/[수정 후](Evidence/StandaloneMockServer/sqlite-green.trx)에 보존했다. 잘못 분류된 EditMode 혼합 실행은 [83통과/7실패](Evidence/StandaloneMockServer/edit-mixed-status.json)로 남겼으며 통과로 계산하지 않았다. 그 뒤 정확한 EditMode83/83, 수정된 PlayMode7/7을 실행했다.

Editor 재시작은 멈춘 PID를 확인한 뒤 사용자의 명시 승인으로 한 번 수행했다. 잠금 삭제나 중복 batchmode는 사용하지 않았다. 이후 컴파일·필수 검사를 마쳤으며 [시험 URL 해제](Evidence/StandaloneMockServer/unity-test-cleanup.json)와 자신이 시작한 시험 서버 종료를 수행했다. ORCA는 실행하지 않았다.

## 보호·문서·종료 범위

Git 기준은 main/`61e811188e58740e1a0e329f10f6c9bd3e50533c`. stage/commit/push/branch/reset/stash를 하지 않았다. 기존 RP 설정2개·Pretendard 폰트·ProjectSettings.asset 및 사용자 Packages 설정2개는 이번 편집 대상에서 제외했다. Scene/Prefab/Packages/Archive, 완료 P1~P3 문서/증거, 골든 등236개 추적 경로는 HEAD 비교에서 변경0이다. 이동한 Mock3개 `.meta`는 기존 내용과 일치한다. 플레이어 권한 DLL 참조 없음은 실제 Editor API로 확인했다.

시작 시 사용한 임시 `before.json`은 최종 감사 시점에 남아 있지 않았다. 따라서 그 임시 파일을 근거로317개 바이트 해시 불변을 주장하지 않는다. 보호 추적 경로는 Git 기준으로 별도 확인했으며 기존 사용자 미커밋 파일의 시작 시점 바이트 비교는 증거에 포함하지 않는다. 최종 소스 해시·골든/제안 문서 해시·테스트 DLL 일치는 감사 파일에 보관했다. 실제 사용자 저장은 검사에 사용하지 않았다.

README·현행 GDD·CodeMap 색인과 직접 관계를 갱신했고 Unity31개/Server15개 C#에 대응 요약이 있다. 제공한 이전 계획 원문과 완료 P1~P3 문서는 수정하지 않았다. 서버 bin/obj/DB/log/시험 산출물은 Server/.gitignore로 제외한다. Unity 회귀용 DLL과 meta는 별도 관리 대상이다. [최종 감사](Evidence/StandaloneMockServer/final-audit.json)에서 문서 링크 누락0·검사 후 소스 변경0·staged0·DLL 빌드본 일치를 확인했고 tracked diff whitespace 검사도 통과했다.

M0~M3 필수 AC를 위 증거로 충족했다. 실제 Firebase/Auth/App Check/Firestore/Cloud Run·Android/IL2CPP·두 기기·LAN/HTTPS 배포는 **NOT_RUN/후속**이다. M4/M5 계측·Firestore 가상 비용·대시보드·규모별 부하 시험은 **미구현/후속**이다. 현재 개발 계정 헤더는 운영 인증이 아니며 Mock 재화를 운영 데이터로 승격하지 않는다. 이번 작업은 M3에서 종료한다.

## 사용자 승인 커밋 (2026-10-22)

후속 요청에 따라 M0~M3 구현·서버 실행 안내·기획/CodeMap·PLAN/REPORT·실제 검사 증거만 로컬 커밋한다. 기존 RP 설정2개, Pretendard 폰트, ProjectSettings.asset, 사용자 Packages 설정2개와 사용자 제공 원본 이전 계획 파일은 제외하고 그대로 남긴다. 승인된 구현 계약은 저장소의 PLAN에 있으므로 원본 제안은 자동으로 추적에 포함하지 않는다.

완료 증거의 C#46개 SHA256과 현재 파일이 일치함을 확인했다. 코드 변경 없이 커밋 범위와 문서만 정리하므로 Unity/서버 시험을 반복하지 않는다. 위13/18/83/7은 구현 종료 때 실행한 결과이며 이번 커밋 단계의 새 실행으로 표기하지 않는다. Unity 공통 ignore가 서버 csproj도 제외하므로 Server/.gitignore에 서버 프로젝트 파일만 허용하는 예외를 추가했다.

포함 파일은 [커밋 목록](Evidence/StandaloneMockServer/commit-files.txt), 제외 파일의 커밋 직전 해시와 확인 결과는 [커밋 범위 감사](Evidence/StandaloneMockServer/commit-audit.json)에 보관한다. 실제 전달 커밋 해시는 Git 이력과 최종 응답에서 확인한다. push는 하지 않는다.

스테이징의 추가/삭제 경로123개가 정확한 목록과 일치했고 제외 파일7개는 변경되지 않았다. staged whitespace 검사는 Editor가 생성한 Support 폴더/meta와 Authority DLL/meta의 빈 값6곳만 지적했다. 두 meta의 생성 바이트는 유지하고 나머지 경로의 검사는 통과했다. 서버 프로젝트6개·Editor 테스트 DLL·직접 참조 문서가 모두 포함되는지 확인했다.
