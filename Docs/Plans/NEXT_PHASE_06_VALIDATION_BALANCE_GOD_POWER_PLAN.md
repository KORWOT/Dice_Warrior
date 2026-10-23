# NEXT_PHASE_06 · Q0/Q1 PLAN

2026-10-23 · IMPLEMENT(검증 및 발견된 직접 결함 수정) · 기준 `859ac227e809db7263f7670d34539f7ff1bab8c9`, main, staged0.

최종 상태: **Q0/Q1 PASS · 완료**. 필수 Finding0, 수정/재검증3회 사용/승인상한4회. 아래 장애·중간 종료는 이력이고 최종 판정은 마지막 완료 기록과 REPORT를 따른다.

## 범위와 계약

사용자가 [제공 계획](../Dice_Warrior_Next_Phase_06_Validation_Balance_God_Power_Plan_v0.1.md)을 확인한 뒤 진행을 요청했다. 원문의 제안/예시는 명령으로 취급하지 않고 첫 실행 묶음 Q0→Q1을 채택한다. 실제 최신 소스의 컴파일/.NET/EditMode/PlayMode와 격리 Mock HTTP 여정이 증거를 남기며 통과하는 것이 목표다. B0/C7 밸런스 조정, G0 신격·권능, E0 확률/상태 효과, C6 정산, D0 Android/Firebase는 이번 범위가 아니다. 기존 v1~v4 정의/골든/보상0 정책은 동결한다. 자동 commit/push는 하지 않는다.

## 기준·보호·검증 채널

- 현재 사용자 변경11개(기존10+제공 원문1)와 SHA256, HEAD/staged는 `artifacts/next-phase-06/{baseline,user-before}.json`에 기록했다. 기존 설정/폰트/원문/DB/계정 저장은 편집·테스트·staging하지 않는다.
- 이전 완료 PLAN/REPORT/증거는 과거 기록으로 보존한다. 새 증거는 `Docs/Reports/Evidence/NextPhase06/{Q0,Q1}/`, 작업중 출력은 `artifacts/next-phase-06/`만 사용한다.
- 메인만 기존 Unity Editor를 직렬 제어한다. 상태/버전/Play/dirty/컴파일부터 확인하며 중복 Editor/ORCA/검색 파일 수동 생성/인증 변조/강제 종료를 하지 않는다. 필요 시 복구 선택을 사용자에게 묻는다.
- Q1은 사용 중인5087/실제authority.db를 재사용하지 않는다. 고유 임시 DB/포트/계정/저장 루트의 Development 서버만 실행하고 소유 PID만 정리한다. 서비스 인증/실제 Firebase/공동 경기 정산 요청은 하지 않는다.

## 역할·허용 경계

- 메인: 공통 계약/환경 조사/Unity/.NET/서버 실행/증거/PLAN/REPORT/README/직접 CodeMap·색인. 제품 결함을 발견하면 ID·직접 근거·정확한 수정 파일·회귀 검사를 이 PLAN에 먼저 추가한다.
- 기존 보조 domain: 읽기 전용으로 Q0 요구와 현재 검사/구형 골든/저장 검증의 대응 및 누락을 조사한다. 입력은 원문 Q0, 색인·직접 CodeMap/검사 원본. 산출물은 필요한 기존 실행 목록과 실제 누락된 검증만. 쓰기/Unity/Git/프로세스 실행 금지.
- 기존 보조 integration: 읽기 전용으로 Q1 실제 HTTP 경로/격리 설정/기존 테스트 및 최소 재현 하네스를 조사한다. 입력은 원문 Q1, Server README, 직접 CodeMap/원본. 산출물은 API·포트·DB·Unity 주입 방법과 정확한 후보 파일. 쓰기/Unity/Git/프로세스 실행 금지.
- 동시최대3명/누적보조2명, 같은 파일 단독 작성. 재귀/중복 위임 금지. 최초검토1묶음·최초 산출물 후 수정/재검증 총2회(현재0). 보조 구현이 필요하면 같은 인원을 재사용하고 파일 경계를 구체화한다.
- 현재 쓰기 허용: 이 PLAN, 대응 `Docs/Reports/NEXT_PHASE_06_VALIDATION_BALANCE_GOD_POWER_REPORT.md`, 위 증거/작업 출력, `Docs/README.md`. 새 하네스/제품 C#은 조사 후 파일·API 단위로 추가한다. Scene/Prefab 쓰기 허용0, 자산 재생성 없음.

## 순차 실행·필수 AC

1. Q0 preflight: 현재 버전/Editor/Pipeline·검사 진입점/콘텐츠·보호 해시를 기록한다. 연결 장애가 지속돼도 .NET/문서·Q1 설계 조사는 진행한다.
2. Q0: 공유 규칙 .NET, 직접 계정 권한 회귀, Unity 컴파일/EditMode/PlayMode를 실행한다. 최신 PlayMode 실제 실행이 없으면 Q0 BLOCKED/NOT_RUN이다. 0 tests를 통과로 세지 않는다.
3. Q0: 태그/타입 누락 거절·부분조합/변환/상성·4RNG 읽기 불변·v1~v3 보존·v4 재현·잠금/CAS/백업·UI 포인터/2세로비율을 기존 검사와 대조한다. 실제 누락만 추가하며 같은 구현을 확인하는 불필요한 검사는 만들지 않는다.
4. Q1: Q0 필수 오류 해결 후 실제 HTTP 신규 프로필/재시도/오프라인·오류, 모험가 선택/다음 경기 적용, v4 새 게임/사건/전투/AI, 종료·저장·재개, 구형active 구매 보류, 취소/확인 및 원문 보존, 경기 종료 후 영구 재화/정산 호출0, 서버 재시작/재진입, M4 계측/가상 비용 구분을 확인한다.
5. 최종: 실제 결과·카운트·실행 시점·NOT_RUN을 REPORT에 기록한다. 보호 해시/CodeMap·meta/링크/Git 상태와 자신이 시작한 프로세스 정리를 확인한다. 필수 실패/실행 불능을 PASS로 마감하지 않는다.

## 조사 결과 및 검증 보강 경계

- 사용자가 저장 후 직접 Editor 재시작을 선택·수행했다. PID60424/port7800, Unity6000.6.0f1, ready/compiling=false/playMode=stopped로 연결 회복을 확인했다. 이전44732를 에이전트가 종료하지 않았다.
- .NET 공유118/118, 서버권한13/13을 새로 실행해 통과했다. HTTP 검사는 이전 NuGet 경로가 Unity Temp 아래에 있어 재시작 후 패키지가 없어 빌드 불능이었다. 패키지 버전/설정을 바꾸지 않고 `artifacts/next-phase-06/nuget`로 고정 버전을 restore한다.
- 보조 domain 단독 쓰기 허용: `Assets/_Project/Features/SharedWorld/Tests/PlayMode/CompetitionFlowTests.cs`, `Tests/EditMode/CompetitionV4StoreTests.cs`와 각각 CodeMap. v4 앱에서 v1/v2 화면 재개→취소불변→확정 전환, 유효JSON 체크섬 불일치/손상 백업 복원 거절 원자성,720×1600 inset의 모든 상세/모험가 카드만 추가한다. 기존 제품/기대값/자산 변경 금지. .NET/Unity 실행은 메인만.
- 기존 고정 증거는 SymbolLoop v1 4벡터/125상태, SharedWorld v1 2벡터/50해시, v3 시작·종료3쌍이다. v2는 리터럴 부분 조합 및 동일 입력 재현이며 별도 전체 골든이 있다고 주장하지 않는다. v4 포인터 자연 종료·재개 전체 여정은 Q1에 통합한다.
- 보조 integration 단독 쓰기 허용: 신규 `Assets/_Project/Features/SharedWorld/Tests/PlayMode/CompetitionHttpFixture.cs`, `CompetitionHttpFlowTests.cs`, 각각 .meta/CodeMap. 제품 API/asmdef/기존 FlowTests는 수정하지 않는다. 실제 HttpGameBackend+허용 fault 헤더를 붙이는 handler, authored UI/Raycast와 명시 격리 root/endpoint/account 환경변수로 여정을 검증한다. 일반 사례와 동일 endpoint/DB/client-root의 재시작 Prepare/Resume를 분리해 메인이 순차 실행한다. 누락된 시험 설정/5087/외부주소는 FAIL이며 대역으로 대체하지 않는다. 임시 helper는 자신의 GUID 하위만 소유하고 재시작 fixture의 최종 정리는 메인 담당이다. 실제503은 기존 HTTP의 directory-as-DB 격리검사로 확인하고 서버 제품에 새 fault는 추가하지 않는다.
- Q1 메인 허용: `artifacts/next-phase-06/`의 고유 고정 포트/서버 PID/DB/client-root/로그/환경변수 주입·해제 스크립트(검증용), 관련 실행 소스 CodeMap. 테스트 계정/DB/endpoint 재시작 시 입력 스냅샷·revision·pending·선택 기록을 비교한다. 기존 서버5087은 건드리지 않는다.
- Editor 사용자 재시작 직후 사용자 소유 Pretendard-Effects SDF.asset의 SHA256이 최초 기록과 달라진 것을 발견했다. 해당 파일을 편집/복원하지 않고 재시작 전후 해시·발견 시점을 기록한다. 나머지 사용자 파일과 새 변경 여부를 최종 검사하며 폰트 변화 원인을 에이전트 수정으로 단정하지 않는다.

## 최초 검증·검토 Finding

- Q1-01: 신규 `CompetitionHttpFlowTests.cs.meta` GUID가33자리라 Editor 로그가 invalid GUID를 기록하고 해당 스크립트를 import하지 않았다. 목록은 기존25개뿐이고 Q1 실행은0개였으므로 PASS로 세지 않았다. 신규 미연결 검사 meta만 유효한32자리 고유 GUID로 수정하고, 정상 import·30개 발견·Q1 실제 실행으로 해결을 확인한다. 최초 산출물 후 수정/재검증1회 시작(상한2회). 기존 자산 GUID나 제품 소스는 변경하지 않는다.
- 실행 정책: Q1 5개는 Category `Q1Http`+Explicit이며 일반 PlayMode 회귀에서 제외한다. 개별 exact name과 Pipeline `--include_explicit true`를 함께 사용한다. Prepare→소유 서버 재시작→Resume 순서는 메인이 제어한다.
- Q1-02: 정상 import 후 Unity 내 NUnit에는 NonParallelizable 속성이 없어 CS0246이 발생했다. 메인이 신규 검사 파일의 해당 속성만 제거하고 명시 개별 직렬 실행 정책을 유지한다. 수정/재검증2회차(상한2회)이며 이후 실패는 숨기지 않고 해당 상태와 함께 보고한다.
- Q1-03 OPEN: 실제 journey 첫 프로필 승인 후 로컬 저장에 실패했다. checkpoint250자+임시 접미사=287자이며 같은 시험 경로의 직접 저장에서 DirectoryNotFoundException을 재현했다. 신규 fixture/서버 root를 짧게 하고 임시/보존 파일까지 경로 길이 검사 후5여정 재실행이 해결 조건이다. 기존2회 소진으로 추가2회 여부를 질문했다. 답변 전 수정하지 않으며 Q0 PASS/Q1 BLOCKED로 남긴다.
- 현재 종료 정리: 기본 PlayMode25/25(24.07초), Edit202/202, 공유119/119·권한13/13·HTTP25/25·계측32/32. Q1 1FAIL/4NOT_RUN. 환경변수 해제, 소유PID13240 종료, Title clean/stopped. 보호57파일 불변, 사용자 폰트의 재시작 직후 변화 외 추가 변화0, staged0/commit없음.

## 추가 승인 · 2026-10-23

사용자가 Q1 신규 검사2개·검증 스크립트·관련 문서의 수정/재검증을 최대2회 추가 승인했다(총상한4회, 이전사용2회 유지). 메인이 단독 작성하며 추가1회차는 Q1-03 경로 단축이다. 서버 root는 충돌 검사한 q1-4자리 폴더, 사례 폴더는 j/p/l/r로 줄이고 계정별 GUID/endpoint 해시 격리는 유지한다. 원자 저장·백업·기록 보존 접미사까지260자 미만을 파일 접근 전 검사한다. 기존 보조 domain은 해당 경계와 실제 실패 결과만 읽기 전용 확인하며 최초 검토 범위를 다시 확장하지 않는다. Unity/서버 실행은 메인만 직렬 수행한다.

## 최종 완료 기록

- Q1-03 CLOSED: account183자/경기207자/최장 접미사 포함259자, 실제 HTTP5여정 각각1/1 PASS. 최종 R3 증거는 REPORT에 연결했다. 자연 경기45명령·13라운드 전원완료·네 피해 타입 관찰·서버 프로필 불변을 확인했다.
- profile fault2회 후 기존 경기 보존/재시도, legacy active 성장 구매 보류·pending 자동전송0을 확인했다. Prepare 이후 소유 서버17652→20704를 같은55619 endpoint/DB로 재시작하고 Resume에서 ID/body·파일/revision 보존 및 성장1회 적용을 확인했다.
- 승인된 추가2회 중1회만 사용했다(누적3/4). 읽기 전용 지정 경로/직접 회귀 검토에 추가 Finding은 없었다. Q0의 공유119/119·권한13/13·HTTP25/25·계측32/32·Edit202/202·Play25/25 결과는 영향받는 코드가 그대로인 상태에서 보존했다.
- Q1 환경변수 해제·마지막 소유PID20704 종료·Title clean/stopped를 확인했다. 보호57파일 변화0, 기존 재시작 때 확인된 사용자 폰트 변화 외 추가 변화0, staged0/HEAD 유지/commit 없음이다.

## 후속 커밋 승인 · 2026-10-23

사용자가 이번 Q0/Q1 검증 보강과 문서의 로컬 Git 커밋을 명시 승인하고 기존 사용자 변경 제외를 요청했다. 메인은 `artifacts/next-phase-06/commit-files.txt`의90파일(검사/메타/CodeMap/색인·PLAN/REPORT14+증거76)만 정확히 staging한다. 기존 사용자11파일의 현재 해시를 `commit-excluded-before.json`에 보관하고 커밋 후 그대로인지 확인한다. 시작 staged0이며 최종 검증 당시 C#4개의 해시와 현재 파일의 차이0을 확인했다. 보조 domain은 이 목록/제외/최종 증거 일치만 읽기 전용 확인한다. 새로운 제품 검토·테스트 반복·branch 변경·push는 이번 커밋 범위가 아니다.
