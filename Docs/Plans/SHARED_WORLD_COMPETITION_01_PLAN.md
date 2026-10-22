# SHARED_WORLD_COMPETITION_01 PLAN

IMPLEMENT · 2026-10-22 · C0~C5 완료. 사용자가 범위·시험 규칙·성장 동기화를 확정했으며 구현·필수 검증·문서 갱신을 마쳤다.

## 목표·근거·범위

- 요청: `Dice_Warrior_Shared_World_AI_Competition_Migration_Plan_v0.1.md`를 확인하고 개편 구현. 문서의 DOCUMENT 상태와 권장 시험값을 사용자 확정으로 오인하지 않는다. 원문은 보존한다.
- 목표: 한 경기의 고정 네임드 보드에서 인간과 AI가 개인 진행도·사건·난수로 동일 규칙을 사용하고, 저장/재현 가능한 경쟁을 진행한다.
- 근거: 제공 문서 1~10절, 현재 GDD v0.3, SymbolLoop CodeMap, M0~M3 및 M4 완료 PLAN/REPORT. `Docs/AI` 경로는 현 checkout에 없으므로 제공 AGENTS.md 및 현재 PLAN 형식을 따른다.
- 확정 범위: C0~C5 첫 플레이 가능 버전. 새 경기 서버 시작/정산 C6, 대량 밸런스·비용 보정 C7은 후속이다. 신규 경기는 개발 시험 모드로 명시하고 영구 재화 지급·차감을 하지 않는다. 기존 v1 HTTP 정산은 유지한다.
- 확정 시험값: 인간1+AI3, 인간→전쟁/지혜/행운 AI 순서. AI는 자기 사건을 한 차례 안에서 해결한다. 개인3바퀴 후 최종 시련, 전원 완료/탈락 또는30라운드 종료. 순위는 시련성공→완주수→명예→현재HP 내림차순, 모두 같으면 공동순위다. 인간의 시작 성장 전력을 AI에도 같은 스냅샷으로 적용하고 주사위1→2→3개를 유지한다.
- 고정12지명/개인단계 난도/같은칸 중복/독립 사건·상점, 초기 세계효과 없음. 기존 스킬·장비·주사위 전투 판정 재사용, 후원 신은 AI 정책 테마이며 새 전투 능력을 만들지 않는다.

## 기준·보호

- 기준: main `517f91ebcad90acb789404ec60a451b14d9ce47c`, staged 없음.
- 기존 사용자 변경: Mobile_RPAsset.asset, UniversalRenderPipelineGlobalSettings.asset, Pretendard-Effects SDF.asset, ProjectSettings.asset, com.unity.ai.assistant/Settings.json, com.unity.testtools.codecoverage/Settings.json, 독립 Mock 서버 원안, 이번 Shared World 원안. 임의 편집·삭제·reset·stash·staging하지 않는다.
- 기존 v1 규칙/콘텐츠/해시/골든/저장/서버 정산·M4 계측 유지. 신규 모드/저장 경로/규칙 버전으로 격리한다. 원본 사용자 저장/실제 authority DB로 시험하지 않는다.
- ORCA 실행, 실제 Firebase/외부 LLM/실제 온라인 PvP, 4눈 각성, 승인 없는 Git stage/commit/push/branch 변경, 무관한 정비는 제외한다.
- Scene/Prefab 허용은 `Assets/_Project/Scenes/Title.unity`의 기존 오브젝트 보존 연결 추가와 신규 `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab` 두 파일이다. Editor API만 사용한다. 기존 SymbolLoopAuthoring.Build는 Title 재생성이므로 실행 금지. 기존 프리팹/Config/GUID·빌드 씬 목록 유지. Unity 실행은 메인만 직렬 수행한다.

## 담당·위임 상한

- 메인: 공통 계약, PLAN/REPORT/색인/GDD, 저장/서버 영향 확인, 통합·보호 감사·Unity 실행·최종 판정. 공유 문서 단독 작성자.
- 보조 A(competition_domain): SharedWorld/Domain/*.cs와 Core asmdef, Content/shared-world-preview-1.json, Tests/EditMode/CompetitionRulesTests.cs 및 테스트 asmdef, Server/DiceWarrior.Competition.Shared 프로젝트 및 Server/DiceWarrior.Competition.Tests의 도메인/골든 검사, 해당 CodeMap. 먼저 DTO/공개 인터페이스를 메인/B에 알리고 이후 변경을 협의한다. Runtime/Presentation/Editor/공통 문서/Git/Unity 쓰기 금지.
- 보조 B(competition_presentation): SharedWorld/Presentation/{CompetitionApp,CompetitionScreen,CompetitionPlayback}.cs, Editor/CompetitionAuthoring.cs와 Editor asmdef, Tests/PlayMode/CompetitionFlowTests.cs 및 asmdef, 기존 SymbolLoopApp.cs의 타입 독립 진입/표시중단/복귀 연결, 해당 CodeMap. Scene/Prefab 작성 코드는 만들되 실제 Editor 실행은 메인에게 넘긴다. Domain/Runtime/공통 문서/Git/Unity 쓰기 금지.
- 메인 추가 소유: SharedWorld/Runtime/{CompetitionSession,CompetitionStore}.cs 및 Runtime asmdef, Tests/EditMode/CompetitionStoreTests.cs 및 서버 링크 검사, 콘텐츠 meta/자산 실제 작성, 검증 증거. 신규 프로젝트 간 csproj 연결 변경은 소유자와 순차 협의한다.
- 직접 구성 회귀 추가 소유: 기존 `SymbolLoop/Tests/EditMode/SymbolLoopTests.cs`의 Title prefab 등록 검사1문장. 신규 화면 등록으로 전체 `Single()`은 더 이상 맞지 않으므로 기존 SymbolLoopScreen 정확히1개/동일 프리팹 및 신규 CompetitionScreen1개/총2개를 검사한다. 기존 v1 규칙·보상·저장·골든 기대값은 변경하지 않는다. 이 변경의 근거와 이전82/83 결과를 REPORT에 명시한다.
- 필요 시 보조 C는 지정 범위 통합 검토 전용. 동시 최대 4명(메인 포함), 누적 최대 3명 위임, 재귀 위임 금지. 조사 담당자의 구현 재사용은 경계를 이 PLAN에 갱신한 뒤 지시한다. 파일당 한 작성자.

## 필수 검증과 완료 기준 (상세 계약 전)

- 고정 지리/개인 RNG·사건/합법 AI/명령 로그 전체 상태 재현/완주와 점수 중복 방지/유한 종료.
- 후보 복제→검사→저장 성공→공개. 저장 실패·재진입·중간 복구에서 AI 차례/보상 중복 없음.
- 기존 v1 골든과 해당 직접 회귀 보존. 서버 공유 소스와 Unity의 새 경기 상태 동치.
- C5 포함 시 실제 Unity 화면/4말/칸 이동·주사위·스킬·AI 요약, 720×1280 및 720×1600과 safe area 확인.
- C6 포함 시 시작 스냅샷/인간 로그 기반 권한 재현, 위조/중복/재시도/동시 정산 검증. 실제 Firebase는 포함하지 않는다.
- 수정한 C#의 1:1 CodeMap 및 직접 관계/색인/REPORT 동기화. 미실행 검증은 NOT_RUN으로 구분한다.
- 최초 검토 1묶음, 이후 수정·재검증 총 2회. 검토 1/1 완료, 수정·재검증 1/2 사용 후 완료. SW-R1은 로비의 지연 프로필 응답이 비활성 새 화면을 재바인딩하는 직접 회귀이며 실제 RED 후 최소 활성상태 guard를 추가했다. 최종 PlayMode5/5와 지정 재검토로 해결, 남은 필수 Finding0. 완료된 과거 작업은 재개방하지 않았다.

## 구현 계약·순서

1. 도메인: `DiceWarrior.SharedWorld` 순수 C#, SharedWorld.Core와 서버 DiceWarrior.Competition.Shared(동일 AssemblyName)에서 동일 소스 사용. 신규 모드/세계/AI/점수 버전은 v1 상수와 분리. 장소/사건 정의는 버전 JSON 데이터, 현재 경기에는 불변 복사본을 보관한다. `CompetitionRules.Start/Validate`, `CompetitionCommandRules.Target/Apply`가 저장·UI의 공통 경계다.
2. 참가자는 Run을 저장하지 않는다. 기존 SymbolCombat에 필요한 필드만 임시 Run으로 매핑하는 어댑터로 전투 공식을 재사용한다. 개인 이동/전투/사건/AI 난수는 고정 문자열 혼합으로 분리하고 사건 제안은 착지 시 고정한다. 보상·명예는 경기/참가자/완주·사건 ID로 중복 방지한다. 일반승리1/정예승리2/최종시련5/발견1 명예를 시험값으로 콘텐츠에 명시한다. 인간/AI 공통 전투64턴 상한 실패를 사용한다.
3. 인간 사건을 마무리하는 명령 안에서 AI3명의 사건 및 라운드 증가를 후보에 반영한다. 인간이 먼저 완료/탈락하면 `advance`로 AI만 한 라운드 진행. 모든 AI는 인간과 같은 합법 선택·비용·전투 함수를 사용한다. 인간 명령 로그만으로 전체 상태를 재현한다.
4. 저장: 별도 CompetitionDocument/schema1, endpoint+계정 해시의 `shared-world-preview-v1` 경로. 명령ID/본문/순서/대상·revision/generation을 검사하고 후보복제→검증→원자 파일저장→공개. 실패 시 기존 공개 상태/명령 로그 불변, 중복 ID 재실행 금지. 손상 자동초기화 금지, 명시 백업복구와 원문 보존. 종료 경기도 새 경기 시작 전 기록을 보존한다.
5. 화면: 기존 Title v1 로비의 명시 '공동 세계 시험' 진입, 별도 화면. 신규 Runtime→SymbolLoop.Runtime만 참조하며 역참조 대신 타입 독립 이벤트로 연결. 서버 프로필 동기화 성공 뒤 시작 성장/출전값을 동결한다. 새 화면 실행 중 legacy Render는 억제한다. 12지명/4말2×2오프셋/순위·HP·완주·명예/개인사건/AI요약, 숫자와 기호 굴림·칸별 이동·확정 TurnLog 스킬 연출. 비활성/화면닫기 후 확정상태 복귀.
6. 검증: 의미 있는 순수규칙·저장 실패/경합·골든을 먼저 작성하고 해당 실패를 확인 후 구현한다. 새 .NET 검사 및 Unity EditMode로 공유 코어 동치, 실제 PlayMode 포인터/모바일비율/재진입 검사. 기존 v1 골든/83 EditMode/7 PlayMode와 직접 영향 서버 회귀를 확인한다. 대량 시뮬레이션·실제Firebase·Android는 NOT_RUN.
7. 최초 통합 검토 한 묶음 후 필수 결함만 최대2회 수정/재검증. 신규/수정 C# CodeMap·직접 관계/색인·GDD·README·REPORT와 실제 증거를 갱신하고 종료한다. 현재 checkout에서 기존 사용자 변경과 쓰기 경로가 겹치지 않으므로 별도 branch/worktree 전환 없이 승인된 프로젝트에서 작업한다.

## 인터페이스 충돌 점검

| 생산/소비 | 점검 결과 |
|---|---|
| A 도메인 → 메인 저장/B 화면 | DTO/API를 먼저 고정하고 전달; 이후 변경을 협의하여 동시 불일치 방지 |
| 메인 세션 → B 화면 | Snapshot/Start/Prepare/Execute/Reload/RestoreBackup 경계, 저장 성공 후만 연출 |
| B 신규 화면 → 기존 SymbolLoopApp | 기존 App은 신규 타입을 참조하지 않아 순환 asmdef 방지 |
| Unity Core ↔ 서버 공유 Core | 같은 소스와 AssemblyName, 구형 v1 Core 유지 |
| A/B/메인 문서 | 각 담당 C# 요약만 개별 작성; 색인/PLAN/REPORT/GDD는 메인만 작성 |
| Scene/Prefab ↔ 기존 사용자 변경 | 허용 자산2개만 Editor API, 사용자 변경8개·v1 콘텐츠/골든18기준 해시 보존 |

## 진행

- [x] 원안의 확정 방향·시험 제안·필수 미결정 구분, Git 기준/사용자 변경 확인.
- [x] 범위·종료/순위·성장 전력 질문 제출.
- [x] 직접 원본 영향 확인 및 사용자 답변 반영.
- [x] 상세 계약/허용 경로/구현 순서 확정.
- [x] 구현·관련 검증·통합 검토. 신규 .NET23/Unity Edit23/Play5, 기존 .NET13/Unity Edit83/Play7 전부 PASS.
- [x] 문서·보호 감사·최종 보고. 보호18해시 동일, 신규18개 C# CodeMap/meta 누락0, 기존 수정2개 요약 갱신. [REPORT](../Reports/SHARED_WORLD_COMPETITION_01_REPORT.md)에 실제 증거를 기록했다.
