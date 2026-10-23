# B0/C7 밸런스 기준선 시뮬레이션 PLAN

2026-10-23 · IMPLEMENT · 기준 `2420decbaea896973b8fd62110cc0cd54aa9a530` / main / staged0.

## 목표와 계약

사용자가 [사전 검토 및 실행 계획](../Dice_Warrior_B0_C7_Balance_Simulation_Review_and_Plan_v0.1.md)을 확인 후 작업 진행하도록 요청했다. 원문을 요구사항 자료로 읽고 이번 실행 계약은 B0-0~3, C7-1 기준선, C7-2 결과 검토·후속 우선순위로 둔다. 실제 수치 조정·비정본 정책 교체·추가 민감도 실험은 결과를 보고 별도 결정한다. 문서의 구체적인 neutral-v1 및 기준 시작조건을 그대로 채택한다.

공유 Core를 ProjectReference로 호출하는 별도 .NET 10 CLI와 검사 프로젝트를 Server 아래 추가한다. Unity/HTTP/DB 실행, 제품 규칙/AI/콘텐츠/골든/저장/화면 변경, 의존성 업데이트는 하지 않는다. 최초 구현 계약에서는 Git stage/commit/push를 제외했으며, 구현 완료 후 로컬 커밋은 아래 사용자 추가 승인에 따른다. 사용자가 제공한 원문·폰트·설정 등 기존 변경12파일은 그대로 보호한다. AGENTS가 참조하는 Docs/AI 운영 문서는 현재 없으므로 주어진 AGENTS와 기존 CodeMap 형식을 따른다.

## 필수 조건·구현 선택

- v4 콘텐츠 원본 SHA256 고정, 동일 seed set × wanderer/scholar/vanguard, growth0/balanced/blade, AI 기존 고정 조합.
- neutral-v1: 완료/탈락 advance, Map travel, Combat combat, LapReward 시작주사위 또는 aegis, Encounter HP50% 미만 Heal 우선100 그 외 xp+heal+honor; 동점 offer 순서 유지. 숨은 RNG 조회/소비 없음.
- master seed는 실행기 상수로 고정하고 사전에 manifest로 저장한다. 고유 nonzero uint seed1000을 생성해 앞3/30/100/1000을 단계별 사용한다. 생성기/목록/해시와 실행기 소스 해시 기록.
- Target/Apply만 경기 진행에 사용한다. BackendProtocol.MaxCommands 이하 명령 상한, 경기별 시간 상한, batch fail-fast와 실패 최소 재현을 둔다. 정상 결과 전체 checkpoint 대량 기록 금지.
- 인간 명령 직전/직후 읽기 관찰만 수행한다. AI 전체 전투·사건 이력은 NOT_MEASURED. 인간 실제 HP 피해와 방어 흡수, 스킬 생략을 구분한다.
- 참가자별 성공/탈락/순위/분할1위/HP/비율/XP/명예/골드, 경기 종료 이유/라운드/명령/시련 성공자수, 결측·실패·성능을 기록한다. Wilson95% 및 시드 짝 차이의 결정적 bootstrap을 제공하며 자동 정책 성적/AI 조합 교란을 명시한다.
- 재실행·명령 replay·중간 JSON 재개·관찰 ON/OFF에서 최종 상태/RNG/명령 해시가 같아야 한다. 상태와 시간 측정은 별도 취급한다.

## 허용 경로·담당

- 메인: 공통 계약·신규 `Server/DiceWarrior.BalanceSim/{DiceWarrior.BalanceSim.csproj,Program.cs,BalanceScenario.cs,SimulationData.cs}` 및 신규 검사 csproj, 실행·보호 감사·PLAN/REPORT·Docs/README.md·Docs/CodeMap/INDEX.md·Server/README.md(존재 시)·직접 관계 CodeMap. 프로젝트 생성/프로세스 실행은 메인만.
- 보조 runner: 먼저 기존 Domain/CodeMap의 API·관측 근거를 읽기 전용 조사. 계약 확정 후 신규 `HumanDecisionPolicies.cs`, `MatchRunner.cs`, `OutcomeCollector.cs`, `DiceWarrior.BalanceSim.Tests/MatchRunnerTests.cs` 및 각각 1:1 CodeMap 단독 작성.
- 보조 report: 먼저 집계·통계·출력 계약을 읽기 전용 조사. 계약 확정 후 신규 `Statistics.cs`, `ReportWriter.cs`, `DiceWarrior.BalanceSim.Tests/StatisticsTests.cs` 및 각각 1:1 CodeMap 단독 작성.
- 원자료/작업 증거: 고유 `artifacts/balance-sim/<run>/`, 소형 보고 증거 `Docs/Reports/Evidence/BalanceSimulation07/`. 기존 artifacts나 증거를 덮어쓰지 않는다.
- 동시 최대3명(메인+보조2), 누적 보조2명, 재귀/중복 위임 없음. 같은 파일은 한 작성자. 공통 DTO는 메인만 수정한다. 최초 검토는 두 보조의 서로 다른 범위 확인을 합친1묶음, 최초 산출물 후 수정·재검증 총2회 상한. 현재0회.

## 단계와 완료 기준

- [x] B0-0: baseline·기존 사용자12파일·규칙/콘텐츠/골든/설정/패키지/자산 SHA256, 환경, 사전 정책·시드 manifest 보존.
- [x] B0-1: 잘못된 입력/중복·0 seed/버전·해시/장비·정책 거절, 정책 순서·상한·실패 기록 검사. 실제9경기와 재현/복원 비교 PASS.
- [x] B0-2: 90경기, 통계 인공 동률/분모/결측/실패 검사 및 집계 일치 PASS.
- [x] B0-3: 300경기 파일럿, p50/p95·처리량·RSS/heap/GC·출력크기 실측, 오류0과 성능 적합 확인.
- [x] C7-1: 3000경기 기준선 완료·실패/미실행 구분, paired 비교/분포/CI/재현 증거·원자료 해시 기록.
- [x] C7-2/종료: 실제 결과의 해석·후속1~2변수 우선순위, 보호 해시/코드 요약/색인/REPORT 확인. 실험 PASS와 밸런스 적합 판정을 구분한다.

## 진행

- 기준 HEAD는 원문과 일치, staged0. 현재 사용자 변경은 기존11파일+이번 원문1개다. 이전 Q0/Q1 통과는 과거 증거로만 참조하고 새 실행 결과로 세지 않는다.
- B0-0 완료: `artifacts/balance-sim/run-01` 신규 생성, ignore 확인. 사용자12·보호87파일과 현재 SDK10.0.302/32논리CPU, 콘텐츠 `06DF1643383740F8D60551952CC36FAA1D5A0E130FDA7FB3A85D5632E11E26BA` 기록. 기존 작업 경로에서 신규 서버 프로젝트만 쓰며 사용자 변경은 섞지 않는다.
- 구현 선택: master `0xB0C70001`, xorshift32(13,17,5), 명령2048/경기30초, 순차실행. 메인 신규 `ScenarioTests.cs`/CodeMap도 허용하여 seed/정본해시/입력/출력 재사용 및 경로 이탈 거절을 검증한다. DTO는 SimulationData.cs가 단독 계약이며 집계 결과/관찰 DTO는 해당 담당 파일 내 정의한다. 새 csproj의 정확한 기존 버전 패키지 lock 생성은 허용하되 기존 패키지는 변경하지 않는다.
- 검사 선작성 RED25/25 확인 후 구현. 최초 통합은25PASS/1예상RED(HP 동점해소 노출·시련실패 빈도)였고 해당 빈도를 구현했다. sandbox 안의 첫 dotnet 실행은 build 출력 진척이 없어 소유 세션만 취소했으며 --disable-build-servers/UseSharedCompilation=false/직렬 실행으로 확인했다. 기존 버전 캐시 restore 성공, NU1801 네트워크 인덱스 경고는 실행 성공과 구분한다.
- 최초 통합검토1묶음: 메인 통계, runner 보조는 메인 입력/배치, reporting 보조는 runner/관찰 경계를 나눠 확인한다. B07-01: 완료 경기 수만으로 batch 검증 실패를 PASS로 덮는 결함, 회귀검사 실제RED 확인. BS07-R1: raw 출력크기를 최종크기처럼 해석할 위험, 필드명을 RawOutputBytesBeforeReports로 명확화하고 최종전체크기는 외부audit로 별도 실측. 이 둘을 1회차 수정·재검증 묶음으로 처리한다(누적1/2 시작).
- 1회차 종료:27/27 검사 PASS, B07-01/BS07-R1 CLOSED, 다른 필수Finding0.9/9와 각 경기 반복/replay/중간복원/관찰OFF PASS,90/90 PASS,파일럿300/300 PASS(8.1356초,p95 42.2208ms,RSS113291264byte). 명령상한1의 의도된 CLI 실패는1실패/8미실행·exit1·재현파일로 보존. 보호87/사용자12 해시변화0, staged0. 본실험3,000경기 시작, 코드 동결.
- 최종 완료:3,000/3,000 PASS·실패/미실행0,73.4487초/p95 32.6427ms/40.84경기초. 단계 간507쌍의 시간 제외 전체결과 및 해시 일치,3,399경기 모두 분할1위합1. 시간상한 주입도1실패/8미실행·원인·재현 보존. 결과 해석/미계측/paired CI와 후속 인간정책·HP비교 민감도 제안은 REPORT에 기록했다. 추가 민감도 실행/규칙 변경 없음.
- 최종 감사:보호87/사용자12/검증된C#11 해시변화0, 원자료62파일 해시 검증, CodeMap11개/직접관계4개·색인·README 동기화, 문서 링크 누락0. HEAD/main/staged0 유지,commit/push없음. 최초검토1묶음·수정/재검증 누적1/2, 남은 필수Finding0. 작업 완료.

## 구현 완료 후 로컬 커밋 승인

- 사용자가 B0·C7 구현·검증 결과·문서의 커밋을 요청하고 기존 사용자 변경 제외를 명시했다. 승인 범위는 기존 `artifacts/balance-sim/run-01/task-files.txt`의 정확한73파일이며 branch 변경/push는 제외한다.
- 메인이 파일 단위 staging·커밋·전후 해시 감사를 담당한다. 기존 runner 보조1명에게 동일73파일/제외12파일 경계와 검증 기록의 읽기 전용 확인만 위임한다. 새 위임 인원·쓰기·제품 재검토·검사 실행은 추가하지 않는다.
- 검증 완료 당시 소스11·보호87·사용자12 해시와 현재 상태를 비교한다. 코드 변화가 없으면 기존27검사/기준선3000경기 증거를 재실행 결과로 표현하지 않으며 불필요한 반복 실행을 하지 않는다. PLAN/REPORT에 커밋 승인 이력만 보충한다.
- 커밋 목록과 staged 목록의 완전 일치, `git diff --cached --check`, 커밋 후73파일 목록·빈 index·남은 사용자12파일·보호 해시를 확인한다. 커밋 전후 감사는 ignored `artifacts/balance-sim/run-01/commit-*`에 기록한다. 구현 종료 시점의 evidence/final-audit.json은 당시 미커밋 상태를 나타내므로 덮어쓰지 않는다.
