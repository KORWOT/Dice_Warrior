# STANDALONE_MOCK_SERVER_01 PLAN

IMPLEMENT · 2026-10-22 · 완료(M0~M3). 사용자 승인: 문서 검토 후 Unity Assets 밖 독립 Mock 서버 분리 및 M0~M3(HTTP 서버·SQLite·Unity 연결) 구현. 실제 증거는 [REPORT](../Reports/STANDALONE_MOCK_SERVER_01_REPORT.md)를 따른다.

## 계약·고정 범위

- 기준 main/61e811188e58740e1a0e329f10f6c9bd3e50533c. staged 없음. 기존 RP2/폰트/ProjectSettings 및 사용자 Packages 설정2개, 제공 원본 문서는 보존한다. Git stage/commit/push/branch/reset/stash는 이번 요청에 포함하지 않는다.
- 원본 제안은 Docs/Dice_Warrior_Standalone_Mock_Server_Firebase_Migration_Plan_v0.1.md. 현재 선택은 M0~M3이며 M4/M5 계측·대시보드·부하 규모 시험, 실제 Firebase/Cloud Run/기기/게임 규칙 변경은 제외한다. Orca 사용 금지.
- protocol1, symbol-loop-mvp-1, symbol-loop-replay-1 및 P2 골든4개/121명령/125상태 유지. P1 대기0, 최초20 개발 재화 유지. 기존 실제 저장은 열거나 이관하지 않는다.
- 서버는 Server/ 아래 .NET10(설치 SDK10.0.302) ASP.NET Core, SQLite 권한 저장. 공유 Domain은 동일 소스를 링크 컴파일하며 복사본을 만들지 않는다. Newtonsoft 직렬화는 동일 필드 순서/숫자 enum/null 포함·불변 문화권 계약으로 검증한다.
- 서버 소스 MockGameBackend/MockRunVerifier/MockAuthorityStore를 Server/DiceWarrior.Authority로 이동한다. 기존 파일 저장소는 테스트 호환 전용이며 실제 HTTP는 SQLite 저장소를 사용한다. Unity 테스트만 사용하는 netstandard2.1 Authority DLL을 Editor 전용으로 작성한다. 플레이어 코드/Factory는 이 DLL에 의존하지 않는다. 기존 .meta는 이동본에 보존한다.
- LoopJson을 순수 Domain으로 분리해 공유한다. Server 공유 라이브러리 AssemblyName은 SymbolLoop.Core로 맞춰 Unity 테스트 DLL이 기존 Core 어셈블리에 연결되게 한다. 새 중복 규칙/검증기 없음.
- IAuthorityRepository는 사용자별 원자 처리 경계다. SQLite는 일관된 스냅샷 읽기 → 트랜잭션 밖 재현 → 짧은 쓰기 트랜잭션에서 저장 버전 재검사/유니크 영수증 확정. WAL, busy timeout, 다중 연결 및 두 서버 프로세스 같은 DB 경쟁을 검사한다.
- HTTP 6개 경로는 원 IGameBackend 의미를 유지한다. 개발 신원 헤더는 Development+loopback만 허용하며 별도 body UID를 받지 않는다. 운영 모드는 개발 인증을 켜지 않고 실패한다. 요청/응답 상한·잘못된 JSON/URL ID 검사·5xx/단절/취소 결과 불명·같은 ID 재확인. 자동 인프로세스 fallback 없음.
- 서버 콘텐츠는 버전별 고정 JSON 스냅샷과 SHA256으로 관리하고 Unity 기본 Config/골든과 동치 검사한다. 개발 seed 고정은 명시적 테스트 서버 설정만 허용한다.
- Unity 기본은 HttpMock, endpoint/개발 ID/timeout은 로컬 개발 설정 파일에서 읽는다. 체크포인트는 endpoint+계정별 새 http-mock 경로로 분리해 기존 mock 데이터를 섞지 않는다. HTTP 서버 자동 시작 없음. 배포 빌드에서는 개발 모드 차단. Scene/Prefab 편집 없음.

## 허용 경로·담당·위임

- 메인: 공통 계약 결정, Domain/LoopJson.cs 추출 및 Runtime/LoopStore.cs, Server/MockServer 호스트·통합 시험/실행 스크립트, Server/README, Server/.gitignore의 서버 산출물 제외, 모든 PLAN/REPORT/INDEX/README/GDD 및 직접 CodeMap. Unity 실행·DLL import·최종 통합 담당.
- 구현 보조A: Server/DiceWarrior.Authority, DiceWarrior.Shared, DiceWarrior.Storage.Sqlite, 단위 Tests 프로젝트 및 Server/global.json/공통 빌드 파일. 기존 Mock3개 소스/meta 이동과 해당 서버 CodeMap. Unity 파일은 그 세 이동본 외 수정 금지. NuGet restore/build는 전용 출력 경로에서만 실행, Unity/Git 금지.
- 구현 보조B: Unity Backend/HttpGameBackend.cs·GameBackendFactory.cs·HttpBackendSettings.cs, Domain/BackendContracts.cs의 mode만, Presentation/SymbolLoopApp.cs, Tests/EditMode/HttpGameBackendTests.cs와 FlowTests HTTP 연동, 테스트 asmdef 및 해당 CodeMap. 기존 Mock테스트는 DLL 참조로 유지하고 실제 포인터 완주를 HTTP 서버에 연결. Unity 실행/Git/Server 파일 금지.
- 읽기 검토 보조C: 결과 통합 이후 권한/신원/HTTP복구/빌드 경계의 필수 결함만 검토. 쓰기·실행 금지.
- 동시 최대3명(메인 포함), 누적3위임, 재귀 금지. A/B 종료 후 C. 파일당 작성자1명. 같은 프로젝트 Unity는 메인만 직렬 실행.
- 명시 허용 테스트 DLL 자산: Assets/_Project/Features/SymbolLoop/Tests/Support/DiceWarrior.Authority.dll 및 해당 폴더/meta. Editor API로 Editor 전용/명시 참조로 설정. Scene/Prefab/Packages/기존 외부 DLL은 보호.
- 허용 증거: Temp/standalone-mock-server 및 Docs/Reports/Evidence/StandaloneMockServer. 서버 신규/변경 C#도 Docs/CodeMap/Server/...cs.md에 1:1 요약한다. 이동한 기존 요약은 새 경로로 옮기고 색인을 고친다.

## 순서·검증·종료

1. 기준 상태/보호 해시, 공통 JSON/규칙 경계 고정.
2. 서버 공유 컴파일·골든/계약 시험 → SQLite 원자 저장·권한 분리 → HTTP 호스트/실제 프로세스 시험.
3. Unity HTTP 어댑터/개발 설정/Editor 전용 테스트 DLL 연결 → 기존 EditMode/PlayMode 및 HTTP 실제 포인터 완주.
4. 독립 검토1묶음, 필수 수정·재검증 최대2회. 반복/예산은 단계로 초기화하지 않는다.
5. 문서/CodeMap/meta/보호 해시·실제 증거 정리. 필수 검증 미실행은 PASS로 표기하지 않는다. 이번 요청의 Git 쓰기는 하지 않는다.

필수 AC: 서버 단독 실행/재시작 영수증 지속, 6API와 기존 JSON/fingerprint 의미, 골든4개 전 단계 동치, 악성 로그 무지급, 동일 런 동시100제출/같은 revision 구매10경합 1회 반영, 사용자A/B 격리, 실제 HTTP 응답 유실/취소/서버 종료 뒤 원 요청 복구, Unity 로비→3지역→보스→50정산→성장→다음 런 포인터 흐름, 보호 파일 불변·문서/요약 완비. 실제 Android·Firebase, M4/M5의 규모별 지표는 NOT_RUN.

최초 검토1/1, 검토 후 수정·재검증2/2. 남은 필수 Finding0. 첫 묶음은 요청/계정 ID 계약과 Unity 비동기 검사 대기의 수정, 두 번째는 PlayMode asmdef 분류 복원이다. 새 검토를 열지 않고 직접 회귀와 필수 검사만 확인했다.

## 완료 기록

- M0: Domain 링크 컴파일/netstandard2.1, 동일 SymbolLoop.Core 이름, 공통 LoopJson, 4골든/125상태·JSON 문화권 검사 PASS. 버전별 콘텐츠 해시와 Unity Config 동치 PASS.
- M1: Development/loopback 전용 ASP.NET Core6API, 계정 헤더, 운영/외부 바인딩 기동 거절 PASS. 서버 소스는 Assets 밖이며 Editor 전용 테스트 DLL의 플레이어 참조 없음 PASS.
- M2: SQLite WAL/스냅샷/CAS/유니크 제약, 두 서버 프로세스100정산·10구매 경합·사용자 격리·재시작·rollback PASS. 독립 단위13/13, 실제 HTTP18/18.
- M3: HttpMock 기본/체크포인트 격리/자동 fallback 없음. Unity EditMode83/83, 실제 HTTP 포함 PlayMode7/7. 완주50정산·성장·다음 런과 응답 유실 뒤 재개/화면 재생성 복구 PASS.
- 사용자 승인으로 멈춘 Unity Editor를 한 번 강제 재시작했다. 이후 컴파일·검사를 완료했으며 실제 사용자 저장/Scene/Prefab을 변경하지 않았다. 임시 HTTP 시험 설정을 해제하고 소유 시험 서버를 종료했다.
- 코드 요약 Unity31개/Server15개 및 색인·README·GDD·REPORT 동기화. 보호 경로의 Git 기준 비교236개 변경0, 이동 meta3개 일치. 임시 before.json은 최종 시점에 없어 그 파일과의317개 해시 비교를 완료했다고 주장하지 않는다. 세부 증거 한계는 REPORT에 기록한다.
- Git stage/commit/push 없음. M4/M5·Firebase·기기 검증은 시작하지 않는다.

## 사용자 승인 커밋 (2026-10-22)

사용자가 M0~M3 코드·문서 정리 후 Git 커밋을 요청하고 기존 사용자 변경 제외를 명시했다. 위 구현 완료 시점의 Git 미실행 기록 이후, 이번 전달에는 정확한 파일 목록의 stage/로컬 commit만 허용한다. push·branch/reset/stash는 하지 않는다.

- 메인 단독으로 문서·증거와 현재 소스 해시를 확인하고 스테이징/커밋 목록을 대조한다. 구현 검토1/1·수정2/2를 유지하고 새 코드 검토나 Unity 검사를 반복하지 않는다.
- 기존 RP 설정2개·Pretendard 폰트·ProjectSettings.asset·사용자 Packages 설정2개·사용자 제공 원본 이전 계획 파일을 제외한다. 커밋 전후 SHA256을 비교한다.
- 서버가 새 체크아웃에서도 빌드 가능하도록 Unity 공통 ignore에 걸리는 수작성 서버 csproj6개만 Server/.gitignore에서 허용한다. bin/obj/DB/log는 계속 제외한다.
- 허용 변경은 해당 ignore 예외, 현행 README의 원본 참조 설명, 이 PLAN/REPORT의 승인 커밋 기록과 범위 증거다. 검사한 C#은 변경하지 않는다.
- 정확한 포함/제외 목록과 해시/검증 결과는 REPORT의 커밋 증거에 기록한다. 실제 커밋 해시는 Git 이력과 최종 응답에 제공한다.
