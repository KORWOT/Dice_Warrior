# Dice Warrior 독립 개발 서버

M0~M3: Unity와 별도 프로세스의 ASP.NET Core HTTP + SQLite 서버다. 실제 Firebase 연결·과금·배포, M4/M5 대시보드/부하 지표는 포함하지 않는다. 현재 구현/검증 결과는 [REPORT](../Docs/Reports/STANDALONE_MOCK_SERVER_01_REPORT.md)를 따른다.

## 실행

설치된 .NET SDK10.0.302를 사용한다. 저장소 루트 PowerShell에서 실행하고 이 터미널을 유지한다.

```powershell
./Server/Start-MockServer.ps1
```

기본 주소 `http://127.0.0.1:5087`, DB `Server/data/authority.db`. `Ctrl+C`로 종료한다. Unity Editor를 실행하지 않아도 동작한다. `GET /healthz`는 준비 상태와 콘텐츠 버전을 제공한다. 다른 포트/개발 DB는 `-Port 5090 -DatabasePath <절대경로>`로 지정한다. Production 환경이나 외부 인터페이스 바인딩은 시작 단계에서 거절한다.

직접 실행:

```powershell
dotnet run --project Server/DiceWarrior.MockServer -- --environment Development --urls http://127.0.0.1:5087 --Mock:DatabasePath D:/UnityProject/Dice_Warrior/Server/data/authority.db
```

개발 계정은 `X-DiceWarrior-Dev-User` 헤더로 선택한다. 로컬 시험용 식별자이며 타인이 임의 계정명을 선택하지 못하게 하는 운영 인증이 아니다. 계정마다 첫 생성20재화가 한 번 지급된다. 구형 `authority.json`과 사용자 저장을 읽거나 자동 이관하지 않는다.

## Unity 연결

서버를 실행한 뒤 기존 `Assets/_Project/Scenes/Title.unity`에서 Play한다. 별도 설정 파일이 없으면 기본 주소와 `mock-player-01` 계정/15초 제한을 사용한다. HTTP 실패 때 인프로세스 Mock으로 전환하지 않는다. 서버가 없으면 연결 안내와 재확인 버튼이 나온다.

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
dotnet build Server/DiceWarrior.Authority
unity command run_script --file Server/Tools/ImportUnityTestSupport.cs --entry ImportUnityTestSupport.Run --project-path D:/UnityProject/Dice_Warrior
```

독립 HTTP 검사는 고유 임시 DB와 자신이 시작한 서버 프로세스만 사용하고 종료한다. 두 서버 프로세스/100동시 정산은 정합성 시험이며 처리량·운영 동접 예측이 아니다.

Unity의 전체 PlayMode 검사는 실제 HTTP 연결을 포함한다. 별도의 고유 임시 DB·seed33 서버를 `--Mock:EnableFaults true`로 실행하고, `Temp/standalone-mock-server/unity-test-url.txt`에 주소를 적은 뒤 `VerifyUnityBoundary.Run`을 실행한다. 이 도구가 테스트 프로세스 환경 변수 `DICE_WARRIOR_HTTP_TEST_URL`을 설정한다. EditMode/PlayMode의 SymbolLoop 어셈블리를 각각 실행한다. 종료 후 `VerifyUnityBoundary.ClearTestContext`와 시험 서버 종료를 수행한다. 주소가 없으면 실제 HTTP 검사는 명시적으로 실패하며 인프로세스 대역으로 우회하지 않는다.

장애 헤더 `X-DiceWarrior-Test-Fault`의 `before-abort`/`after-abort`/`after-delay`는 명시적으로 fault가 활성화된 로컬 시험 서버에서만 처리한다. 기본 서버에서는 무시하며 실제 Firebase/운영 인증용 API가 아니다.

## 후속 교체 지점

Firebase 전환 시 개발 신원 공급자를 검증된 ID token/App Check 처리로, IAuthorityRepository를 Firestore 원자 저장으로 교체한다. HTTP6개와 C# 규칙 재현을 유지하고 별도 동치/복구 검증이 필요하다. 실제 서비스 프로젝트·키·배포는 이번 작업에서 만들지 않는다. 기존 Mock 재화는 운영 데이터로 승격하지 않는다.
