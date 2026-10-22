# SHARED_WORLD_FLOW_COMBO_03 PLAN

IMPLEMENT · 2026-10-22 · 완료. 기준 `c9e8a57`, staged0. 실제 증거는 [REPORT](../Reports/SHARED_WORLD_FLOW_COMBO_03_REPORT.md)를 따른다.

## 요청·계약

- 사용자가 모험 종료 버튼과 3주사위에서2눈 조합 누락을 지적했다. 추가 답변으로 모험은 보존하고, 별도의 새 게임 버튼을 경고/확인한 경우에만 현재 진행을 초기화하도록 확정했다.
- 모험 중 `모험 종료`는 저장된 진행을 그대로 두고 로비로 돌아간다. 로비의 `이어하기`는 같은 경기/명령/RNG를 재개한다. `새 게임`은 진행 중 경기가 있을 때 경고와 취소/확정을 제공한다. 취소는 상태·파일 불변, 확정은 새 경기 ID/초기 상태로 원자 교체한다. 이전 진행은 복구 사본으로 보존하며 서버 조회/저장 실패는 현재 경기를 잃게 하지 않는다. 기존 계정 재화·성장·구형v1 기록은 초기화하지 않는다.
- 새 공동 경기 규칙 v2: 등록된3눈 조합 우선, 없으면 등록된2눈 부분 조합1개 + 남은 눈 기본기. 남은 기본기를 눈 순서대로 먼저 실행하고 조합을 마지막에 실행한다. 예: 지팡이·지팡이·검 → 마력탄(비전 기본기)·검기. 조합 후보가 겹치면 콘텐츠 배열 순서로 고정한다. 각 눈은 한 번만 소비하며 피해/보호/장비·처치 뒤 생략/반격1회는 유지한다. 사람/AI 동일.
- 기존 SymbolLoop v1과 기존 SharedWorld v1 저장/골든은 기존 정확전체 조합을 유지한다. 새 경기부터 `shared-world-preview-2`를 동결하고 v1/v2 모두 읽는다. 과거 골든 값을 새 구현 결과로 덮어쓰지 않는다. 콘텐츠 파일 경로/GUID는 유지한다.
- 혼합 발동 연출은 각 기술의 기본기/조합 여부를 구분한다. 숫자/기호 주사위와 적용 로그 순서·처치 후 생략은 유지한다.

## 범위·보호

- 도메인: SymbolLoop/Domain/SymbolCombat.cs의 기존2인자 API를 보존한 부분조합 오버로드, SharedWorld/Domain/{CompetitionCombat,CompetitionContent,CompetitionEncounterRules}.cs, 기존 Content/shared-world-preview-1.json의 버전 값, SharedWorld/Tests/EditMode/{CompetitionRulesTests,CompetitionGoldenTests}.cs 및 필요한 부분조합 검사 파일.
- 로비/저장: SharedWorld/Presentation/CompetitionApp.cs, Runtime/{CompetitionSession,CompetitionStore}.cs, Tests/EditMode/CompetitionStoreTests.cs, Tests/PlayMode/CompetitionFlowTests.cs. ICompetitionStore 변경 시 직접 구현만 동기화한다.
- 연출: SymbolLoop/Presentation/SymbolLoopPlayback.cs. Scene/Prefab 쓰기 없음. 필요 시 별도 명시 범위 갱신 전 자산 변경 금지.
- 문서: 이 PLAN/REPORT, GDD/README/CodeMap 색인, 수정 C#1:1 요약과 직접 관련 관계만 갱신. 새 증거는 Reports/Evidence/SharedWorldFlowCombo에 보관한다.
- 기존 사용자 변경8개와 실제 계정/DB, 서버 v1 콘텐츠/고정 골든/설정/패키지/자산 GUID를 보호한다. ORCA/Firebase/실사용자 저장 시험 없음. 기본5087서버는 유지한다. 구현 후 사용자 후속 요청으로 이번 변경·문서·증거의 로컬 커밋을 승인받았다. push는 범위 밖이다.

## 역할·검증·상한

- 메인: 계약·공통 API, App/Session/Store/저장·PlayMode 검사/연출, GDD/색인/PLAN/REPORT, 모든 Unity 및 최종 .NET 실행/통합/보호 감사.
- 보조 domain: 위 도메인·규칙/골든 검사·콘텐츠 버전과 해당 CodeMap만 작성. .NET 단일 신규 실패 재현은 메인과 직렬 조정 후 가능, Unity/Git/다른 파일/재귀 위임 금지.
- 보조 review: 최초 한 검토 묶음에서 부분조합·버전 보존/새게임 원자교체·표시 경계를 읽기 전용 검토. 구현/Unity/Git 쓰기 금지.
- 동시 최대3명(메인 포함), 누적 보조2명, 동일 파일 작성자1명. 검토1묶음, 최초 검증 후 수정·재검증 총2회. 최종1/1,0/2, 미해결 필수 Finding0.
- 필수 검증: 사용자 예시와 화염탄/중복눈/3눈 우선/중복소비 금지/후보 경합·장비·처치 생략, v1골든 불변·v2 재현; 종료→이어하기·새게임 경고/취소/확정/재진입, 이전 파일 보존과 실패·오래된 화면 CAS; 혼합 기술2개 실제 연출. 변경 관련 .NET/Unity EditMode/PlayMode를 실행하고 실제 수치를 REPORT에 기록한다. 동일 소스/무관 서버 검사를 반복하지 않는다.

## 완료 확인

- .NET38/38, Unity EditMode121/121, PlayMode15/15 PASS. 종료/재개·새 게임 취소/확정/실패와 혼합 스킬 표시의 실제 포인터·프레임 검사를 통과했다.
- 보호15파일(사용자8 포함)·검증 대상14파일 SHA256 동일, C#47개 CodeMap/meta 누락0, 기존 meta 변경0. Title 저장·Play 정지, Scene/Prefab 쓰기 없음.
- GDD/README/색인·수정 요약/직접 관계·REPORT 갱신 및 최초 검토1묶음 종료. 실제 사용자 계정/DB와 HTTP 전체 통합·Android는 검사하지 않았다. 구현 완료 시점에는 커밋/push하지 않았다.

## 사용자 승인 커밋

2026-10-22 후속 요청으로 모험 종료·새 게임·부분 조합 코드와 문서·증거44파일을 로컬 커밋한다. 기존 사용자 변경8파일은 제외한다. 메인이 정확한 파일 목록과 보호/소스 해시를 확인하고 staging/commit/커밋 후 대조를 수행한다. 기존 보조 review는 동일 작업의 커밋 파일 목록·검증 증거만 읽기 전용 확인한다(추가 보조 없음, 동시 최대2명, Unity/Git 쓰기 금지). 완료된 구현 검토를 재개하지 않는다. 검증 대상14파일과 보호15파일의 해시가 동일해 테스트를 반복하지 않는다.
