# SHARED_WORLD_COMPETITION_01 REPORT

2026-10-22 · IMPLEMENT · **C0~C5 완료**. 기준 main `517f91ebcad90acb789404ec60a451b14d9ce47c`. 사용자 승인 범위는 첫 플레이 가능 공동 세계이며 C6 서버 정산·C7 대량 시뮬레이션은 제외한다. 남은 필수 Finding0, 새 결정 요청 없음.

## 구현과 확정된 결정

`SharedWorld` 기능에 버전 JSON으로 정의한 고정12지명, 인간1+AI3, 개인 사건/진행/4종 RNG, 같은 합법 명령을 사용하는3종 AI, 완주/최종시련/명예/공동순위를 추가했다. 인간의 서버 조회 시작 성장·출전값을 네 참가자에게 동일하게 복사한다. 첫 두 완주에서 주사위를 골라1→2→3개로 늘리고 마지막 완주 장비 뒤 최종시련을 진행한다. 개인 완료/탈락은 건너뛰며 전원 종료 또는30세계라운드에서 끝난다. 인간도 AI도 한 전투64턴 상한을 사용한다.

새 경기 상태에는 세계 정의를 한 번만 보관하고 참가자는 개인 상태만 갖는다. 기존 SymbolCombat을 임시 전투 어댑터로 재사용하며 저장된 Run4개로 보드를 흉내내지 않는다. 정확한84기호 배열에서 기존 전투와 동치다. 사람의 마지막 사건 명령과 AI3명의 결과를 같은 후보 안에서 계산한 뒤 한 번 저장한다. 명령 로그에는 인간 선택만 남기고 AI·순위·난수 전체는 공용 규칙으로 재생한다.

체크포인트는 기존 endpoint+계정별 폴더 아래 `shared-world-preview-v1/checkpoint.json`, schema1이다. schema2/기존 실제 사용자 저장은 이관하지 않는다. 체크섬·revision/generation CAS·같은ID본문 재시도·백업/손상 원문·완료 기록 보존을 제공한다. 종료 경기 뒤 새 시작은 이전 완료 파일을 `.completed-*`로 보존한다. 이 로컬 파일은 권한 티켓이 아니다.

Title의 기존 로비에 **공동 세계 시험** 진입을 추가했다. 12칸과 네 말의 2×2 겹침 배치, HP/완주/명예/순위·장소 선택·AI요약, 인간 숫자/기호 굴림·칸 이동·적용된 스킬 연출을 연결했다. 새 Runtime만 기존 표시 모듈을 참조하며 역참조 대신 타입 독립 이벤트를 사용한다. 신규 프리팹과 Title의 직렬화 연결은 Editor API로 작성했다.

새 경기의 영구 재화 지급은0이며 기존 v1 정산 API로 새 경기 로그를 제출하지 않는다. 기존 M0~M4 호스트/SQLite/계측/요금 모델은 그대로다. `DiceWarrior.Competition.Shared`는 Unity와 같은 `SharedWorld.Core` 이름으로 동일 Domain 소스를 링크하고 별도 검사 프로젝트가 이를 검증한다.

## 실제 검증

| 검증 | 현재 결과 | 근거 |
|---|---|---|
| 신규 .NET 코어/저장/골든 | 23/23 PASS | [competition.trx](Evidence/SharedWorldCompetition/competition.trx) |
| 신규 Unity EditMode | 23/23 PASS | [unity-editmode-new.json](Evidence/SharedWorldCompetition/unity-editmode-new.json) |
| 신규 Unity PlayMode 화면·비동기 경계 | 5/5 PASS, 실패·생략0 | [unity-playmode-new.json](Evidence/SharedWorldCompetition/unity-playmode-new.json) |
| 로비 비동기 경계 RED | SW-R1 실제 재현 후 위5개 GREEN으로 해결 | [ui-lifecycle-red.json](Evidence/SharedWorldCompetition/ui-lifecycle-red.json) |
| 기존 서버 권한/SQLite/v1 골든 | 13/13 PASS | [legacy-authority.trx](Evidence/SharedWorldCompetition/legacy-authority.trx) |
| 기존 Unity EditMode | 83/83 PASS, 실패·생략0 | [unity-editmode-legacy.json](Evidence/SharedWorldCompetition/unity-editmode-legacy.json) |
| 기존 Unity PlayMode/HTTP | 7/7 PASS, 실패·생략0, 107.03초 | [unity-playmode-legacy.json](Evidence/SharedWorldCompetition/unity-playmode-legacy.json) |

신규 고정 골든은 정상/포기·관전2개, 인간48명령/전체50상태와 참가자별4 RNG 스트림이다. Unity와 .NET이 같은 고정 해시를 검사한다. 기존 골든4개121명령125상태는 수정하지 않았다. 초기 Start stub·프리팹 부재·저장 stub 및 신규 골든 부재의 의도된 RED를 각각 남겼다. 테스트 준비 중 존재 전 콘텐츠 로더 참조 오류1회와 기존 SQLite NuGet 캐시 누락은 성공 근거에서 제외했고, 실제 lock 버전 복원 후 기존13개가 통과했다.

## 검토·수정 예산

최초 독립 검토1묶음 완료, 수정·재검증1/2 사용 후 완료. 최초 정적 검토에서는 필수 Finding이 없었으나 같은 묶음의 로비 통합 증거 보강으로 SW-R1을 발견했다. 수정 후 해당 Finding과 직접 구성 회귀만 재검토했으며 미해결 필수 Finding은 없다.

- **SW-R1 · 해결:** 프로필 조회 대기 중 CompetitionApp을 비활성화하면 늦은 응답의 Open→Render가 비활성 화면을 재바인딩했다. 실제 포인터/지연 응답 검사에서 BindingRevision 기대4/실제5로 재현했다. Render에 활성 상태 검사를 추가해 비활성 중 표시를 막고 OnEnable에서 확정 상태를 표시한다. 기존 화면 소유권·성장 재조회·복귀를 포함한 최종 PlayMode5개가 모두 통과했다. 수정 후 독립 검토에서도 추가 필수 결함은 없었다.

기존 EditMode 최초 결과는82/83이며, 실패1개는 Title의 화면 등록이 전체1개라는 이전 구성 가정이었다. 승인된 신규 화면 등록에 맞춰 기존 SymbolLoopScreen 동일성/1개·신규 CompetitionScreen 정확한 프리팹/1개·총2개를 검사하도록 변경했다. 기존 바인딩과 v1 규칙·보상·저장·골든 기대값은 보존했다. [최초 결과](Evidence/SharedWorldCompetition/unity-editmode-legacy-initial.json)를 별도로 남겼다.

검사 모드를 바꾸는 도중 실행기가0건을 반복 반환했다. Unity API의 검색 목록에는 지정한 기존7개가 있었으나 직접 실행도0건으로 끝나므로 해당 응답은 PASS에 포함하지 않았다. 저장·정지된 Editor에서 공식 `EditorUtility.RequestScriptReload()`로 도메인을 갱신한 뒤 기존 EditMode83개와 PlayMode7개 실제 실행이 정상화됐다. 패키지/전역 설정/검사 조건은 수정하지 않았다.

## 보호·문서·한계

기존 사용자 변경8개와 v1 도메인/콘텐츠/골든/Packages/빌드목록의 기준18개 SHA256을 보관했고 검사 후18/18 동일했다. 기존 Title의 오브젝트/GUID는 보존하며 새 App과 UIManager 화면 등록만 추가했다. [Editor 재읽기](Evidence/SharedWorldCompetition/scene-readback.json)에서 두 화면 등록·공유 UI·콘텐츠/전투설정 연결을 확인했다. 기존 SymbolLoop 프리팹·공용 UI 프리팹·Config 자산은 쓰지 않았다. ORCA·실제Firebase·외부LLM·Git stage/commit/push/branch 변경 없음.

720×1280 및720×1600/상하 안전영역에서 자동 포인터·경계 검사와 캡처 확인을 수행했다. [라운드 요약](Evidence/SharedWorldCompetition/round-summary-720x1280.png), [기호 전투](Evidence/SharedWorldCompetition/combat-720x1280.png), [긴 화면 안전영역](Evidence/SharedWorldCompetition/safe-area-720x1600.png)에서 네 말·공동순위·AI 결과와 조작부가 표시된다. 실기기 조작 검증은 별도다.

소스/문서는 `git diff --check -- . ':(exclude)Assets/_Project/Scenes/Title.unity'` 통과. 전체 diff는 Editor가 Title의 빈 YAML 값에 기록한 공백을 지적하므로 전체 통과로 보고하지 않는다. 자산 직렬화를 수동 편집해 공백을 제거하지 않았다.

PLAN/GDD/Docs README/Server README, 신규 C#18개 및 수정한 기존 App/구성 검사2개의 요약, 직접 참조 관계와 CodeMap 색인을 갱신했다. 신규 C# 요약 누락0, 신규 기능의 `.meta` 누락0이다. [보호 감사](Evidence/SharedWorldCompetition/protected-audit.json)와 [검증 소스 해시](Evidence/SharedWorldCompetition/source-hashes.json)를 보관했다.

최종 검사 후 검증 소스30개 해시가 동일하고 staged0이다. 검사 전용 HTTP 서버는 고유 임시 DB로 실행했으며 PID/시작 시각/명령행을 확인해 해당 프로세스만 종료했다. Unity의 테스트 URL 환경변수를 제거했고 Title은 저장·정지·컴파일 완료 상태다([종료 상태](Evidence/SharedWorldCompetition/editor-final.json)). 사용자 서버/실제 저장/기존 사용자 변경은 정리 대상으로 삼지 않았다.

Android/실기기, 신규 C6 서버 권한 재현·정산, C7 대량 밸런스/성능/비용 모델 보정, 실제Firebase/과금/M5부하는 NOT_RUN. 새 경기의 로컬 시작 시드/프로필/상태는 서버 보상 근거로 신뢰할 수 없다. 승률·시험 명예값·최종 아트는 확정하지 않는다.

## 실행

1. 저장소 루트에서 `./Server/Start-MockServer.ps1` 실행.
2. Unity에서 `Assets/_Project/Scenes/Title.unity`를 열고 Play.
3. 로비에서 시작 주사위/무기를 선택한 뒤 **공동 세계 시험 → 시험 경기 시작**.
4. 이동→개인 사건→AI요약을 진행하고, 인간이 완료/탈락하면 다음 AI라운드를 관전한다. **기존 모험으로**로 v1 로비에 복귀한다.
