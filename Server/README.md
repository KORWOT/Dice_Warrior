# Dice Warrior 독립 개발 서버

Unity와 별도 프로세스의 ASP.NET Core HTTP + SQLite 서버다. M0~M3 권한 분리에 이어 M4 로컬 사용량 계측·오프라인 비용 추정·대시보드를 제공한다. 실제 Firebase 연결·과금·배포와 M5 규모 부하 시험은 포함하지 않는다. [M0~M3 REPORT](../Docs/Reports/STANDALONE_MOCK_SERVER_01_REPORT.md)와 [M4 REPORT](../Docs/Reports/STANDALONE_MOCK_METRICS_04_REPORT.md)를 구분한다.

공동 세계 C0~C5는 새 `SharedWorld.Core` 순수 도메인을 `DiceWarrior.Competition.Shared`에서 동일 소스로 컴파일하고 `DiceWarrior.Competition.Tests`에서 검사한다. 기존 호스트의 v1 콘텐츠/HTTP/SQLite/계측은 변경하지 않는다. 새 경기는 기존 프로필 조회만 사용하며 시작/AI/순위는 별도 로컬 시험 기록이고 지급액은0이다. 새 권한 시작 티켓·AI 재현·정산 API는 C6 후속으로, 현재 API에 새 경기 로그를 제출하지 않는다. [공동 세계 PLAN](../Docs/Plans/SHARED_WORLD_COMPETITION_01_PLAN.md)을 따른다.

v5 굴림·보관·리롤·실행도 같은 순수 Core와 별도 로컬 체크포인트를 사용한다. 테스트 프로젝트는 신규 v5 정본을 추가로 링크하며 HTTP 계약은 변경하지 않는다. [V5 PLAN](../Docs/Plans/V5_DICE_COMBAT_09_PLAN.md) / [검증 REPORT](../Docs/Reports/V5_DICE_COMBAT_09_REPORT.md)를 따른다. 아래 B0/C7 실행은 여전히 동결 v4 정본 전용이며 v5 밸런스 시뮬레이터가 아니다.

## 실행

### B0/C7 오프라인 밸런스 실험

`DiceWarrior.BalanceSim`은 공유 Core만 참조하는 별도 .NET 콘솔이다. Unity·Mock HTTP·authority DB를 사용하지 않는다. 현재는 v4 정본 해시, growth0/balanced/blade, neutral-v1과 모험가3종만 허용한다. 정본/AI/기존 저장을 수정하지 않으며 결과로 보상을 지급하지 않는다.

```powershell
dotnet run -c Release --project Server/DiceWarrior.BalanceSim -- --count 3 --verify true --output artifacts/balance-sim/my-smoke
dotnet run -c Release --project Server/DiceWarrior.BalanceSim -- --count 1000 --output artifacts/balance-sim/my-baseline
dotnet test -c Release Server/DiceWarrior.BalanceSim.Tests
```

각 output은 존재하지 않는 새 경로여야 한다. `--count`는 시드 수이며 경기 수는3배다. 권장 순서는3→30→100→1000 시드이며 앞 단계 결과를 확인한 뒤 확대한다. 고정 master seed의 prefix를 공유한다. `--seeds <uint 배열 JSON>`은 명시 목록을 읽고 `--count`와 정확히 일치해야 한다. 명령2048/경기30초 제한, 첫 실패에서 중단하고 실패 원인·재현 명령을 남긴다. `--verify true`는 반복/명령재생/중간복원/관찰OFF 비교를 추가한다.

manifest/seed, matches JSONL, 요약 JSON/MD, 경기/참가자 CSV, 실패 JSONL, 재현/성능/파일 해시를 저장한다. AI 전체 기술/사건 이력은 미계측이며 결과는 자동 정책의 성적이다. 인간·AI의 시련 성공, 순위1, 공동1위 분할점수는 서로 다른 지표다. [B0/C7 PLAN](../Docs/Plans/BALANCE_SIMULATION_07_PLAN.md)과 [실제 보고](../Docs/Reports/BALANCE_SIMULATION_07_REPORT.md)를 따른다.

### Mock 서버

프로젝트 최상위의 [Start-MockServer.cmd](../Start-MockServer.cmd)를 더블클릭하면 서버를 실행한다. 게임 중에는 실행 창을 유지하고 종료할 때 `Ctrl+C`를 누른다. 시작 오류가 나면 메시지를 읽을 수 있도록 창에서 키 입력을 기다린다. 기본 대시보드는 `http://127.0.0.1:5087/dev/dashboard`다.

이 파일은 기존 PowerShell 스크립트를 호출하는 Windows 실행 도우미다. .NET SDK10.0.302(또는 호환10.0.3xx 패치)가 설치된 현재 개발 환경을 사용하며, SDK를 포함한 독립 배포 EXE는 아니다. 최초 빌드에서 NuGet 패키지 복원이 필요할 수 있다. 저장소 루트 PowerShell에서 기존 방식으로 실행해도 된다.

```powershell
./Server/Start-MockServer.ps1
```

기본 주소 `http://127.0.0.1:5087`, DB `Server/data/authority.db`. `Ctrl+C`로 종료한다. Unity Editor를 실행하지 않아도 동작한다. `GET /healthz`는 준비 상태와 콘텐츠 버전을 제공한다. 다른 포트/개발 DB는 `-Port 5090 -DatabasePath <절대경로>`로 지정한다. Production 환경이나 외부 인터페이스 바인딩은 시작 단계에서 거절한다.

CMD에도 같은 인수를 전달할 수 있다. 예: `./Start-MockServer.cmd -Port 5090 -DatabasePath "D:/Temp/Dice Warrior/authority.db"`(저장소 루트에서 실행). 인수를 생략하면 기존 기본 DB를 계속 사용하며 초기화하지 않는다. 이미 같은 포트의 서버가 실행 중이면 새 창의 오류를 확인하고 기존 서버를 사용한다. 실행 검증 범위는 [실행 도우미 REPORT](../Docs/Reports/LOCAL_SERVER_LAUNCHER_08_REPORT.md)에 기록한다.

대시보드는 서버 실행 후 [http://127.0.0.1:5087/dev/dashboard](http://127.0.0.1:5087/dev/dashboard)에서 연다. 사용자 게임 DB가 필요 없는 시험은 별도 `-DatabasePath`를 지정한다.

직접 실행:

```powershell
dotnet run --project Server/DiceWarrior.MockServer -- --environment Development --urls http://127.0.0.1:5087 --Mock:DatabasePath D:/UnityProject/Dice_Warrior/Server/data/authority.db
```

개발 계정은 `X-DiceWarrior-Dev-User` 헤더로 선택한다. 로컬 시험용 식별자이며 타인이 임의 계정명을 선택하지 못하게 하는 운영 인증이 아니다. 계정마다 첫 생성20재화가 한 번 지급된다. 구형 `authority.json`과 사용자 저장을 읽거나 자동 이관하지 않는다.

## Unity 연결

서버를 실행한 뒤 `Assets/_Project/Scenes/Title.unity`에서 Play하면 공동 모험 메인 로비가 열린다. 별도 설정 파일이 없으면 기본 주소와 `mock-player-01` 계정/15초 제한을 사용한다. HTTP 실패 때 인프로세스 Mock으로 전환하지 않는다. 서버가 없으면 연결 안내와 재확인 버튼이 나온다.

기존1인 모험판은 제거했지만 v1 계정/런/영수증을 삭제·자동 정산하지 않는다. 사용자 결정에 따라 이전 activeRunId가 있으면 그 계정의 영구 성장 구매를 보류하며 공동 모험 출전 선택/시작/재개는 허용한다. 신규 경기는 v1 시작/정산 API를 사용하지 않는다. 계정/HTTP 계약 자체는 이번 메인 전환에서 변경하지 않는다.

필요할 때만 `Application.persistentDataPath/symbol-loop-backend-v1/http-settings.json`에 다음 설정을 직접 작성한다. 설정 변경 뒤 화면/앱을 다시 연다.

```json
{"endpoint":"http://127.0.0.1:5087","userId":"mock-player-01","timeoutSeconds":15}
```

새 체크포인트는 `symbol-loop-backend-v1/http-mock/<endpoint+계정 SHA256>/checkpoint.json`에 저장한다. 기존 `mock/checkpoint.json`이나 더 오래된 `symbol-loop-v1/player.json`은 그대로 둔다. 주소/계정이 바뀌면 다른 체크포인트를 사용한다. 같은 서버 주소와 계정을 유지하면 서버 재시작 후 이어갈 수 있다. 개발 서버 DB를 임의 초기화하면 기존 클라이언트 진행의 권한 티켓은 복구할 수 없으므로 별도 시험 계정을 사용한다.

HttpMock은 Editor/개발 빌드에서만 허용하며 현재 연결 주소는 loopback으로 제한한다. Android 실제 기기는 localhost가 해당 기기이므로 현재 구성의 실기기 연결 대상이 아니다. LAN/HTTPS 인증·기기 수명 주기 검증은 후속 범위다.

## 코드·저장 경계

| 경로 | 역할 |
|---|---|
| DiceWarrior.Shared | Unity Domain 소스 링크 컴파일, 어셈블리 SymbolLoop.Core, netstandard2.1 |
| DiceWarrior.Authority | 정산 서비스/검증기/저장 인터페이스와 구형 파일 저장 회귀 대역, netstandard2.1 |
| DiceWarrior.Storage.Sqlite | 사용자별 DB 스냅샷/CAS·유니크 키·원자 트랜잭션, net10.0 |
| DiceWarrior.MockServer | 개발 신원·6개 HTTP 경로·버전별 콘텐츠, net10.0 |
| DiceWarrior.Metrics | 실제 계측/유한 journal·집계·Firestore 논리 모델·요금 JSON/비용 계산, net10.0 |
| DiceWarrior.Metrics.Tests | 수치·보관/개인정보·계측 장애·가격/무료 구간 검사 |
| DiceWarrior.Tests | 권한/저장/골든 단위 검사 |
| DiceWarrior.HttpTests | 실제 서버 프로세스·Unity HTTP 소스·Coordinator 연결 검사 |
| Tools | Unity Editor 전용 회귀 DLL import와 플레이어 경계/콘텐츠 확인 |

Unity 플레이어에는 SQLite·정산 서비스·검증기·파일 권한 저장소가 들어가지 않는다. Unity 기존 P1~P3 검사는 `Tests/Support/DiceWarrior.Authority.dll`을 **Editor 전용·명시 참조**로 사용한다. 이 DLL은 위 Authority 소스에서 재생성한다. Unity Core는 자체 asmdef로, 서버 Core는 같은 Domain을 링크해 컴파일한다. 규칙/JSON/검증기에 임의 복사본은 없다.

서버 콘텐츠 `Content/symbol-loop-mvp-1.json`은 P2 골든 규칙과 Unity Config에 동치다. 시작 시 공유 LoopJson으로 정규화한 SHA256 `A9FF4C352251F537C93752E9AF82ADBA2725465FEC8E868D9659512740910B38`을 확인한다. 콘텐츠가 달라지면 버전/벡터를 명시적으로 갱신하기 전 서버가 기동하지 않는다. 기본 시드는 서버 난수이며 `--Mock:Seed 33`은 명시적인 개발 시험 설정이다.

SQLite는 WAL·busy timeout과 사용자별 저장 버전 CAS를 사용한다. 재현은 쓰기 트랜잭션 밖에서 수행하고, 트랜잭션 안에서 최신 저장 버전을 재확인한 뒤 프로필·런·작업·영수증을 함께 확정한다. `(user_id,request_id)`와 런 정산/영수증 유니크 제약으로 중복 지급을 막는다. DB 파일은 서버 전용이며 손상을 새 계정으로 덮지 않는다. 보관 상한은 현행 Mock 계약(사용자당 런128/작업1024)을 유지한다.

## HTTP 계약

| Method | Path | 의미 |
|---|---|---|
| GET | /v1/profile | 최신 프로필/활성 티켓 |
| POST | /v1/runs | 새 런 승인 |
| POST | /v1/runs/{runId}/submit | 로그 재현 및 원자 정산 |
| POST | /v1/profile/upgrades | 영구 성장 |
| GET | /v1/operations/{requestId} | 원 요청 영수증 |
| GET | /v1/runs/{runId} | 티켓/정산/복구 명령 조회 |

protocol1/rulesVersion/verificationVersion 유지. JSON은 기존 Newtonsoft DTO의 선언 순서·정수 enum·null 포함·불변 문화권 직렬화를 사용한다. 수신 JSON 속성 순서와 공백이 달라도 DTO를 같은 방식으로 다시 직렬화해 `SHA256(kind+":"+JSON)` fingerprint를 계산한다. 변경된 본문의 같은 요청 ID는 거절한다. 알 수 없는 필드, 중복 키, 512KiB 초과 본문은 거절한다. 승인 결과는 기존 BackendStatus와 code로 구분한다.

요청/런 ID: 영문·숫자·`_-.:` 1~128자. POST와 조회 경로에 같은 규칙을 적용한다. 개발 계정 ID: 영문·숫자·`_-` 1~64자. 본문에 userId를 받아 권한을 바꾸지 않는다. 다른 계정의 런/작업은 찾을 수 없는 것으로 처리한다.

HTTP5xx·연결 단절·취소·시간 초과·잘못된 응답은 결과 불명이다. Coordinator가 원 pending ID/본문을 보존하고 조회 후 같은 본문을 재전송한다. 서버 승인 뒤 응답만 없어져도 영수증은 남는다. 로그에는 요청/응답 전문·개발 신원 헤더를 출력하지 않는다.

## 검사와 테스트 DLL 재생성

```powershell
dotnet test Server/DiceWarrior.Tests --logger trx
dotnet test Server/DiceWarrior.HttpTests --logger trx
dotnet test Server/DiceWarrior.Metrics.Tests --logger trx
dotnet build Server/DiceWarrior.Authority
unity command run_script --file Server/Tools/ImportUnityTestSupport.cs --entry ImportUnityTestSupport.Run --project-path D:/UnityProject/Dice_Warrior
```

독립 HTTP 검사는 고유 임시 DB와 자신이 시작한 서버 프로세스만 사용하고 종료한다. 두 서버 프로세스/100동시 정산은 정합성 시험이며 처리량·운영 동접 예측이 아니다.

Unity의 전체 PlayMode 검사는 실제 HTTP 연결을 포함한다. 별도의 고유 임시 DB·seed33 서버를 `--Mock:EnableFaults true`로 실행하고, `Temp/standalone-mock-server/unity-test-url.txt`에 주소를 적은 뒤 `VerifyUnityBoundary.Run`을 실행한다. 이 도구가 테스트 프로세스 환경 변수 `DICE_WARRIOR_HTTP_TEST_URL`을 설정한다. EditMode/PlayMode의 SymbolLoop 어셈블리를 각각 실행한다. 종료 후 `VerifyUnityBoundary.ClearTestContext`와 시험 서버 종료를 수행한다. 주소가 없으면 실제 HTTP 검사는 명시적으로 실패하며 인프로세스 대역으로 우회하지 않는다.

장애 헤더 `X-DiceWarrior-Test-Fault`의 `before-abort`/`after-abort`/`after-delay`는 명시적으로 fault가 활성화된 로컬 시험 서버에서만 처리한다. 기본 서버에서는 무시하며 실제 Firebase/운영 인증용 API가 아니다.

## M4 계측과 비용 대시보드

`/dev/dashboard`의 개요/API·런/비용/내보내기 메뉴에서 다음을 확인한다. 모든 /dev 경로는 loopback peer/Host와 동일 Origin을 요구하며, 대시보드 자체 조회는 게임 API 사용량을 늘리지 않는다.

- **Mock 실제 측정:** API 수·동시 요청·실패/중복, 평균/P50/P95/P99, 요청/응답 엔터티 bytes, 명령 수/로그 직렬화 크기, SQL provider 실행/실제 행 수, transaction/CAS/provider busy 오류·시간, 실제 재현 벽시계, 프로세스 CPU/working set, DB/WAL/SHM와 계측 파일 크기.
- **Firestore 가상:** endpoint와 확정 권한 결과에 따른 문서 읽기/쓰기/삭제·트랜잭션·신규 보관 bytes. SQL 횟수는 변환하지 않는다. 현재 직접 키 모델에는 쿼리/리스너/인덱스 스캔이 없어 그 추정은0이며 운영 관측0이라는 뜻이 아니다. 결과 불명 요청은 미산정 개수로 표시하고 CSV 숫자는 빈칸이다.
- **운영 실측:** 없음. 로컬 프로세스 CPU를 Cloud Run CPU로 바꾸거나 이 결과로 서비스 동접을 판정하지 않는다.

계측은 권한 DB 옆 `metrics/`에 별도 저장한다. `--Metrics:Directory <경로>`로 바꿀 수 있다. 기본 보관은7일, journal16개×4MiB, 조회 표본10,000건이다. 실제로 먼저 도달하는 한도를 적용하며 최대 표본은 전체 누적 요청 수가 아니다. 계정/런은 로컬 `identity.key` HMAC 가명만 남기고 body·명령 내용·원본 user/run/request ID는 저장하지 않는다. 키는 익명화 수단이 아니므로 폴더를 함께 보호한다. 기록 실패·만료·손상·상한 누락을 화면에 표시한다. 기록 실패는 게임 지급을 실패/rollback시키지 않는다. 프로세스 게이지는 현재 호스트, journal 집계는 같은 디렉터리의 보관 표본이다.

`GET /dev/metrics?from=2026-10-22&to=2026-10-22`의 날짜는 UTC 양끝 날짜 포함이다. 기본 응답 sample/run 그룹은 최대200개이며 JSON/CSV export는 제한된 전체 보관 표본을 사용한다. `GET /dev/metrics/export?format=json|csv`, `POST /dev/costs`(JSON 최대16KiB), `GET /dev/pricing`을 제공한다. 권한 데이터를 수정하는 관리 API는 없다.

가격 기본값은 사용자 결정인 **서울 asia-northeast3 / Firestore Standard / USD / 무료 할당량 미차감**이다. `DiceWarrior.Metrics/Pricing/seoul-standard-usd.json`에 확인일·공식 출처·단위·요율을 둔다. 변경 후 빌드/서버 재실행으로 배포한다. 지원하지 않는 지역/에디션은 거절하며 빠진 요율은 미산정이다. [Firestore](https://cloud.google.com/firestore/pricing), [Firestore 과금 규칙](https://firebase.google.com/docs/firestore/pricing), [Cloud Run](https://cloud.google.com/run/pricing), [네트워크](https://cloud.google.com/vpc/network-pricing)를 근거로2026-10-22 확인했다.

비용 화면에서 DAU·런·조회 빈도·중복/오류·재시도·문서/명령 bytes·보관일·초기 저장·인덱스 비율·활성 시간·vCPU/메모리 가정을 바꾼다. 기간1~31일, DAU0~1,000,000, 보관1~365일이며 동일 청구월 안의 일정 사용량을 가정한다. 최소는 오류/중복/재시도를 제거하고 크기/활성 시간 가정을 절반, 최악은 위험 비율과 크기/시간을 확대한다. 통계적 신뢰구간이 아니다.

무료 옵션은 남아 있는 계정 할당량을 확인하지 않고 전부 사용 가능하다고 가정한다. Firestore 작업은 일별, 저장은 GiB-day로 차감하며 Cloud Run은 Tier1의 USD 크레딧을 사용한다. Firestore→동일 리전 서버 전송은 무료 가정이고 Cloud Run→한국 Premium 전송은 별도다. Firestore 무료10GiB를 Cloud Run 전송에서 차감하지 않는다. 저장은 빈 런 이력에서 누적하며 만료 문서의 명시 삭제를 가정한다. Auth/SMS·Cloud Logging·배포/이미지·백업/PITR·TTL·최소 인스턴스·세금 등 제외 항목은 화면/JSON에 명시한다. 총액은 이 모델에 포함한 항목 합계이며 실제 청구액이 아니다.

## 후속 교체 지점

Firebase 전환 시 개발 신원 공급자를 검증된 ID token/App Check 처리로, IAuthorityRepository를 Firestore 원자 저장으로 교체한다. HTTP6개와 C# 규칙 재현을 유지하고 별도 동치/복구 검증이 필요하다. 실제 서비스 프로젝트·키·배포는 이번 작업에서 만들지 않는다. 기존 Mock 재화는 운영 데이터로 승격하지 않는다.
