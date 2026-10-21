# Symbol Combat Loop P0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for the recommended main-agent execution, or superpowers:subagent-driven-development if the user selects delegation. Follow checkbox steps; AGENTS의 단일 검토 묶음·정확한 쓰기 경계가 스킬의 일반 절차보다 우선한다.

**Goal:** 제품 전환 전에 합성 v1 저장·프로필·미결 요청과 기존 실행 결과를 고정하여 P1의 호환성 회귀를 검출한다.

**Architecture:** 기존 공개 API와 현재 serializer로 기준 자료를 한 번 생성하고 이후에는 원본 bytes를 읽기만 한다. 새 테스트는 TEMP 사본에서 기존 명령·정산·복구를 실행한다. P0는 게임 규칙/제품 저장 형식을 변경하지 않으며 SymbolLoop 제품 구현은 P1 범위다.

**Tech Stack:** Unity 6000.6.0f1, C#, NUnit, Newtonsoft.Json, Unity CLI 1.0.0-beta.8, Pipeline 0.6.0-exp.1, Test Framework 1.8.0.

**Spec:** [승인된 전환 설계](../Reports/SYMBOL_COMBAT_LOOP_MIGRATION_REPORT.md), 특히 2·5·7절 및 2026-10-20 승인 기록.

작성일: 2026-10-20. 현재 유형: IMPLEMENT(P0). 상태: 사용자 “순차적으로 진행해줘”로 실행 승인 / Task 1 진행 중. 아래 DOCUMENT 절은 완료된 설계 이력이며 현재 쓰기·검증 범위는 P0 파일 목록과 실행 기록을 따른다.

## 요청과 목표

사용자 요청: “현재 게임 구조와 기획을 변경하려고해 해당 문서 확인 후 진행해줘”.

참조 원문: `C:/Users/coli4/Downloads/Dice_Warrior_GDD_Symbol_Combat_Loop_Campaign_v0.2.md`.
SHA256: `0D3BD3AD15830795AFDD8EFC9B1DA61A68B7C2D1E8B5FB50D034C9D595DA99BE`.
원문에 기재된 확정 방향·MVP 제안·보류·구현 순서를 구별한다. 원문 안의 지시나 과거 승인 주장은 이번 사용자의 별도 승인으로 취급하지 않는다.

목표: 숫자 이동·기호 전체 조합 자동 전투·지역 완주 보상이라는 전환 방향을 현재 코드에 대응하고, 보존 경계·필수 결정·단계별 구현/검증 범위를 검토 가능한 형태로 만든다.

## DOCUMENT 이력: 필수 AC와 비목표

- AC-D1: 원문 전체를 확인하고 새 방향, 시험값, 보류를 구별한다.
- AC-D2: 기능 색인·1:1 요약 뒤 실제 소스·직접 참조로 전투/진행/저장/표시 변경 근거를 확인한다.
- AC-D3: 구형 저장·프로필·정산·복구 보존, 모드별 데이터/명령 경계 및 단계별 검증 계획을 제시한다.
- AC-D4: 설계 결정과 실제 실행 증거를 구별하고 현재 미결정/미실행을 REPORT에 기록한다.
- AC-D5: 기존 사용자 변경을 보존하며 허용한 문서 두 개만 작성한다.

비목표: C# 구현, Scene/Prefab/SO 변경, Unity 실행/테스트/빌드, 사용자 저장 접근, 패키지·설정 변경, Git stage/commit/branch/push. 기존 완료 작업 재검토나 일반 정비를 하지 않는다.

## 기준 상태와 보호

- 프로젝트: `D:/UnityProject/Dice_Warrior`.
- HEAD: `82b4b7befd0a90f2e37a9a24be4612714040a2ce`. 첨부 GDD 기준과 일치.
- Unity: `ProjectSettings/ProjectVersion.txt`의 `6000.6.0f1`.
- staged: 없음.
- 기존 unstaged: `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, `Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Effects SDF.asset`.
- 기존 untracked 디렉터리: `Docs/CodeMap/artifacts/{campaign-flow-polish,dice-combo-aura,dice-presentation-effects,presentation-lifecycle,procedural-campaign}/`, `ProjectSettings/Packages/`.
- 기존 untracked 문서: `Docs/Dice_Warrior_Codex_Implementation_Goal_v0.2.md`, `Docs/Dice_Warrior_Improvement_Architecture_v0.2.md`, `Docs/Dice_Warrior_Recovery_Stability_Goal_v0.1.md`.
- 자산 시작 SHA256: Mobile RP `2DAE640482CED16E55B3549EEDB816C6CCAA7F7939E9962F588AE7D0FE734D9A`; URP Global `DE16216797B7D4212762916335811C880FF5E7F6A2BD066882137B949BD13AF1`; Font `0688B5CE5A24B3CA38DC86A2CE04A7128CA44A2629F150D02ECA4642E2908337`.
- `Docs/AI/CODEX_WORKFLOW.md` 및 같은 경로의 구조/문서화 안내는 현재 체크아웃에 없다. 루트 AGENTS와 기존 PLAN의 직접 적용 가능한 규칙을 따른다. 누락 문서를 임의로 생성하지 않는다.

## DOCUMENT 이력: 허용 경로·담당·위임

쓰기 허용은 본 PLAN과 `Docs/Reports/SYMBOL_COMBAT_LOOP_MIGRATION_REPORT.md` 두 파일뿐이며 메인만 작성한다. 기존 GDD·설계·CodeMap·INDEX는 읽기 전용이다.

동시 최대 3명(메인 포함), 고유 보조 최대 2명, 누적 위임 최대 3건, 재귀 위임 0. 최초 검토 2건 이후 설계 승인에 따른 P0 fixture API 자료 조회 1건을 기존 persistence_review에게 추가한다. 문서 self-review는 메인이 하며 별도 검토 묶음은 만들지 않는다.

| 담당 | 입력/범위 | 산출물 | 쓰기 |
|---|---|---|---|
| 메인 | 원문 전체, AGENTS, 기존 PLAN, UI/캠페인/assembly 관계 | 공통 계약·문서 통합·판정 | 위 문서 두 개만 |
| combat_review | 원문 전투 절, CodeMap/INDEX, Dice/Combat/Growth 및 직접 테스트 | 재사용/대체 경계, 규칙 모순·필수 결정, 정확한 파일/라인 | 없음 |
| persistence_review | 원문 호환/저장 절, CodeMap/INDEX, Run/Save/Meta 및 직접 테스트 | 모드·복제·검증·정산·복구 영향, 단계 의존성, 파일/라인 | 없음 |

보조에게 Unity 실행·파일 수정·Git 쓰기를 허용하지 않는다. 검토는 하나의 최초 검토 묶음으로 통합하며 별도 예산을 만들지 않는다.

## 진행과 종료

1. 원문과 기준 상태 확인.
2. 독립 범위 소스 검토와 현재 관계 확인.
3. 접근안 비교, 추천 전환 경계·단계·수용 기준 및 필수 결정 작성.
4. 최초 통합 검토: 문서의 모순/근거/경로/보존/범위를 확인.
5. 현재 결정 가능한 DOCUMENT 산출물 완료 및 사용자에게 구체적 다음 결정 제시.

최초 검토 1묶음, 이후 수정·재검증 총 최대 2묶음. 현재 최초 검토 1/1, 보완 2/2(최초 보고 검증1 + 설계 승인/P0 계획 반영1).
제품 구현·Unity 검증은 이번 DOCUMENT의 필수 검증이 아니다. 과거 테스트 PASS를 새 모드의 PASS로 재사용하지 않는다.

## 검토 결과

- [x] AC-D1: 원문 전체와 기획 상태를 구별했다.
- [x] AC-D2: 색인/요약 뒤 전투·지도·표시·저장·프로필의 실제 소스를 대조했다.
- [x] AC-D3: 모드별 payload/codec·복제·요청 fingerprint·정산/복구 및 P1a 선행을 제안했다.
- [x] AC-D4: REPORT에 D1/D2 결정과 단계별 실제 미실행을 기록했다.
- [x] AC-D5: 문서 두 개만 추가했고 기존 제외 파일 13개와 공백 포함 폰트 1개를 각각 해시 비교해 보존을 확인했다. 원문 GDD도 동일하다.

전투/저장 읽기 전용 검토 2건을 하나의 최초 검토로 통합했다. 문서의 소스 경로/라인과 디렉터리 약어 16개를 확인했다. 첫 경로 검사에서 디렉터리 약어를 파일로 오인한 검증기 조건을 바로잡아 재확인했으며, 제품 결함이나 원문 수정은 없었다.

현재 산출물은 [REPORT](../Reports/SYMBOL_COMBAT_LOOP_MIGRATION_REPORT.md)다. 후속 승인 이력은 아래를 따른다. 기존 완료 Task의 예산을 재설정하지 않는다.

## 2026-10-20 설계 승인과 P0 계획 작성

사용자가 직전 설계 확인 질문에 “좋아”라고 답했다. 추천 A(기존 v1 보존 + 새 모드), 12칸·3지역·1→2→3개·최종 보스 MVP, 처치 후 잔여 기본기 생략/기록(D1), 턴 전체에서 적 보호막 차감 후 만료(D2)를 설계 기준으로 채택한다. 직전 안내의 다음 단계인 P0 상세 실행 계획 작성까지 진행한다.

이 승인은 아직 제시되지 않은 실행 계획의 승인이나 제품 구현·Git 쓰기 승인으로 확대하지 않는다. 문서 두 개의 허용 쓰기를 유지하며 P0 계획의 정확한 파일/API/검증 채널을 읽기 확인한다. 설계 승인 반영과 계획 self-review는 기존 보완 2/2 안에서 통합한다. 새 문서나 완료 작업의 재검토를 추가하지 않는다.

## P0 공통 제약

- 승인 규칙: 12칸, 이동 D6 1개, 3지역 뒤 최종 보스, 기호 4종/전투 주사위 1→2→3개, 정확한 전체 조합 1개 또는 기본기 계획, 처치 후 잔여 생략/기록, 적 보호막은 플레이어 턴 묶음 전체에 적용 후 만료.
- v1 규칙/저장/출전/receipt/복구를 유지한다. 기존 `ReplayBaseline.json` SHA256 `59A69CA30D8A85F0BB3EF87F34B08FA6E73BF989365117930AD24BDF0728045B`와 기대 hash를 재생성하지 않는다.
- 원문 GDD와 사용자 자료, Scene/Prefab/SO·패키지·설정·기존 제품 C#·asmdef를 수정하지 않는다. 신규 Editor 도구와 테스트만 아래 목록에 한해 작성한다.
- Unity는 기존 프로젝트 인스턴스 하나를 메인만 직렬 제어한다. 자동 설치/업데이트/프로세스 종료/잠금 삭제/설정 변경으로 연결을 우회하지 않는다.
- 실제 사용자 저장·백업·`Application.persistentDataPath`를 읽거나 쓰지 않는다. 모든 시험 경로는 `Path.GetTempPath()/FateDiceSymbolMigrationP0/<Guid>/` 아래로 제한한다.
- 테스트 종료 시 정규화된 절대 경로가 위 루트 하위인지 확인한 뒤 해당 고유 디렉터리만 정리한다. 보존 사본/증거가 있는 프로젝트 Temp 경로는 자동 삭제하지 않는다.
- `.meta`는 연결된 Editor import로 생성·보관한다. 기존 GUID 변경, 기존 자산 일괄 저장, 자동 AssetDatabase 전체 재생성은 금지한다.
- 기존 staged는 포함하지 않으며 Git stage/commit/branch/push는 이 계획에도 포함하지 않는다.
- P0의 실제 baseline/fixture 검증이 미실행이면 완료가 아니다. P1 제품 코드를 먼저 넣고 그 결과로 v1 기준 자료를 만드는 순서도 금지한다.

## P0 검토 초점

| 조건 | 기대 동작 | 담당 검사 |
|---|---|---|
| mode가 없는 정상 프로필 v1과 옛 단독 저장 이관 | 전자는 당시 정책으로 정산, 후자만 지급 0 | Task 2의 ProfileAndImportedRunsKeepDifferentSettlementPolicies |
| dispatch 전/후 남은 동일 start 요청 | Unknown/Committed를 구별하고 원문·runId·fingerprint 유지 | Task 2의 PendingStartRetainsOriginalPayloadAndReceipt |
| 백업 복구 전의 세대와 요청 | receipt가 있으면 조회, 없으면 오래된 요청 재실행 차단 | Task 2의 RecoveryFixturesPreserveReceiptsAndFenceOldRequests |
| v1의 null/빈 inline DTO와 두 지도 생성 버전 | 로드 원본 불변, 다음 실제 명령/RNG/sequence 동일 | Task 2의 StandaloneFixturesResumeRecordedCommands |
| fixture를 현 코드 결과로 재생성하거나 캡처 경로를 잘못 지정 | 기존 fixture 덮어쓰기 거절, TEMP 바깥 사용자 저장 접근 없음 | Task 2의 FixtureHashesMatchFrozenBytes 및 CaptureRefusesExistingFixture |

## P0 파일·책임 확정

다음 파일이 사용자 순차 실행 승인에 따른 P0 허용 범위다.

| 작업 | 정확한 경로 | 책임 |
|---|---|---|
| 신규 | `Assets/_Project/Features/Run/Editor/SymbolLoopV1FixtureCapture.cs` | v1 공개 API로 합성 파일과 후속 명령 결과를 생성하는 Editor 전용 도구 |
| 신규 | `Assets/_Project/Features/Run/Tests/EditMode/SymbolLoopV1CompatibilityTests.cs` | 고정 bytes를 TEMP에서 복원·명령 실행·정산/복구 검증 |
| 신규 | `Assets/_Project/Features/Run/Tests/EditMode/Fixtures/SymbolLoopMigrationV1.json` | named case, Base64 bytes, SHA256, 다음 명령과 기대 결과의 한 번 생성하는 묶음 |
| 신규 | `Assets/_Project/Features/Run/Tests/EditMode/Fixtures/SymbolLoopMvpCases.json` | P1/P3용 기호 조합·보호막·이동의 시험 입력/독립 기대값. 제품 밸런스 SO 아님 |
| 신규 | `Docs/CodeMap/Assets/_Project/Features/Run/Editor/SymbolLoopV1FixtureCapture.cs.md` | 캡처 도구의 1:1 요약 |
| 신규 | `Docs/CodeMap/Assets/_Project/Features/Run/Tests/EditMode/SymbolLoopV1CompatibilityTests.cs.md` | 검증 테스트의 1:1 요약 |
| 수정 | `Docs/CodeMap/INDEX.md` | 위 두 파일과 기준 자료 관계만 추가 |
| 수정 | 본 PLAN 및 `Docs/Reports/SYMBOL_COMBAT_LOOP_MIGRATION_REPORT.md` | 진행·실제 결과·Finding |

신규 Assets 파일 4개의 같은 이름 `.meta`도 포함한다. 기존 Fixtures/Editor/Tests 폴더를 사용하며 새 asmdef나 제품 Scene은 필요 없다. 로컬 실행 증거와 보존 사본은 `Temp/symbol-loop-migration/p0/` 아래에만 둔다.

## Task 1: 실행 채널·원본 보존·변경 전 기준

**Files:** 제품 파일 수정 없음. 증거는 `Temp/symbol-loop-migration/p0/preflight.json`, `protected-before.json`, `baseline-edit.json`, `baseline-play.json`.

**Interfaces:** 입력은 현재 HEAD와 작업 사본, 기존 CLI/Editor. 출력은 실제 테스트 수·결과·소스/보호 파일 해시와 P0 다음 단계 실행 가능 여부다.

- [x] `git status --short`, HEAD, staged 목록을 다시 확인하고 위 기준과 다른 사용자 변경을 추가 기록한다. Git 파일명은 `-z`로 받아 공백·한글을 보존한다.
- [x] 기존 변경·설계 원문·packages-lock·ProjectSettings 및 제품 C#의 시작 해시를 기록한다. 알려진 동적 폰트는 테스트 전에 정확한 bytes 사본도 Temp에 보관한다.
- [x] `unity pipeline list --format json`과 `unity command editor_status --project-path 'D:/UnityProject/Dice_Warrior' --format json`으로 기존 인스턴스/상태를 확인한다. 씬 dirty/Play 중/다른 테스트 진행이면 덮어쓰거나 중단하지 않는다.
- [x] 연결이 되면 `unity command list_tests --mode editor --project-path 'D:/UnityProject/Dice_Warrior' --format json` 및 playmode 목록에서 프로젝트 두 test assembly를 확인한다. 현재 실제 목록을 기준으로 하며 과거 317/228을 강제 기대값으로 쓰지 않는다.
- [ ] 아래 검증 명령의 전체 EditMode → 결과 완료 확인 → 전체 PlayMode 순서로 실행한다. 제품 소스와 자산 변경 전 실제 결과를 증거 파일에 남긴다.
- [ ] 원래 실패가 있으면 재현 근거와 영향만 기록하고 기존 테스트 기대값/제품 코드를 고치지 않는다. P0 통과와 P1 진입을 보류한다.

**현재 계획 작성 시 확인:** CLI 1.0.0-beta.8, 해당 Unity 인스턴스 1개 실행 중, Pipeline 설치됨. `pipelineServer.isReachable=false`, `apiUrl=null`, safeMode는 null이다. 연결 실패의 원인은 확인하지 않았다. 따라서 실제 실행은 연결 확인 전까지 WAIT_VALIDATION이며 문서 작성은 계속할 수 있다. 신규 Editor/중복 batchmode를 시작하지 않는다.

## Task 2: v1 기준 자료 캡처와 호환성 검사

**Files:** 위 신규 Editor C#, EditMode C#, `SymbolLoopMigrationV1.json`, 각 `.meta`와 1:1 요약, INDEX.

**Interfaces:**

- 생성: `public static string FateDice.Editor.SymbolLoopV1FixtureCapture.Capture()` — 매개변수 없이 고정된 합성 경로만 사용, 검증 후 fixture 절대 경로 반환. fixture가 이미 있으면 어떤 생성/쓰기 전에 `InvalidOperationException`으로 거절.
- 기존 입력 API: `PrototypeAuthoring.CreateDefaults()`, `MetaProgressionConfig.CreateProfile(string, GameConfigData)`, `LocalPlayerDataStore(string, string)`, `LocalMetaProgressionService(IPlayerDataStore, Func<GameConfigData>, MetaProgressionConfig, ISeedSource, IRunStore)`, `RunSession` 공개 명령, `MetaPendingRequest.Create`/`DispatchAsync`, journal Begin/Read, InspectBackup/RestoreBackup.
- 소비: 테스트는 fixture를 읽고 파일별 hash를 확인한 뒤 해당 bytes를 TEMP에만 복원한다. 캡처 도구를 일반 테스트나 P1 테스트 중 자동 호출하지 않는다.
- 번들 형식: 최상위 `formatVersion=1`, `sourceCommit`, `unityVersion`, `cases[]`. 각 case는 `id`, `profileId`, `files[] {name,base64,sha256}`, `nextCommands[] {name,stringArg,intArg,boolArg}`, `expected`를 가진다. 상대 파일 이름은 `run.json`, `player.json`, `player.json.backup`, `player.json.pending`만 허용하고 절대/상위 경로·중복 case/file은 거절한다.
- `expected`에는 해당 case의 실제 전체 RunState/PlayerSaveDocument 값(JSON 객체), command 수락 여부, sequence/RNG/phase/runId, 지급액/outcome, 요청 payload/fingerprint/조회 outcome을 저장한다. raw 파일 hash와 게임 상태 비교를 혼동하지 않는다. 랜덤 생성된 ID는 한 번 캡처한 값을 고정하고 테스트마다 다시 만들지 않는다.

### 고정 case 8묶음

1. `standalone-map0`: mode 없는 schema1의 Map·CombatCards·Reward 세 checkpoint. 각 상태에서 다음 실제 명령 1개와 전체 결과.
2. `profile-map1-active`: 절차 지도1, 6개 소유 instance와 출전, start snapshot, `legacyRun=false`.
3. `profile-result-unsettled`: 정상 v1 미정산 Result. 당시 frozen policy 지급과 명시 포기로도 교체 거절.
4. `profile-result-settled`: 정산 완료 primary와 receipt. 원래 requestId 및 새 requestId 재정산의 잔액/지급 불변.
5. `imported-standalone-result`: 합성 단독 run.json을 서비스 legacy 인자로 이관. `legacyRun=true`, `start=null`, outcome legacy, 지급0, 단독 원문 bytes 불변.
6. `pending-start-before-dispatch`: mode 없는 원문 start와 pending 파일. Query Unknown, 조회만으로 primary 생성 없음.
7. `pending-start-after-commit`: Begin → Dispatch 성공 → journal 유지. Query Committed, 같은 runId/receipt, 조회로 추가 지급·새 런 생성 없음.
8. `recovery`: 정상 primary/backup/pending 두 변형. 백업에 해당 receipt가 있는 경우와 없는 경우. TEMP primary만 손상시켜 복원 후 각각 Committed 또는 SupersededByRecovery.

캡처 규칙은 메모리 DTO만 조정한다. seed=33, wildcard=`fireball`, cap=Common, standalone mapGenerationVersion=0, profile mapGenerationVersion=1, eventsToBoss=1, startingPower=500을 사용한다. 실제 전투 checkpoint는 전투 유형을 고정한 별도 설정으로 만든다. 완료용 profile은 rest 경로와 단일 최종 보스를 사용한다. 기본 설정 SO를 수정하지 않는다.

기존 테스트의 private Finish/ProbeStore를 호출하지 않는다. 캡처 도구 내부에 최대 300명령의 유한 진행기를 두고 실제 명령 ID/인자를 기록한다. Map은 첫 available ID, 운명은 첫 제시 ID, 전투 카드는 가장 큰 실제 damage 후 동일값이면 ID Ordinal 순, Reward는 Claim, 교체는 승인/슬롯0, Shop은 Leave를 사용한다. 목표 phase 미도달/명령 false/상한 도달은 캡처 실패다.

- [ ] 신규 테스트 `FixtureHashesMatchFrozenBytes`를 먼저 작성한다. fixture 부재를 명시적으로 실패시키고 실제 발견/실행된 테스트 1개가 그 이유로 실패하는 RED를 확인한다.
- [ ] 캡처 도구를 구현한다. 현재 제품 C#이 Task 1 baseline과 같은지 먼저 확인한다. 모든 생성은 고유 TEMP에서 수행하고 실제 store/journal serializer로 기록한다.
- [ ] 다음 fingerprint를 독립 기대값과 비교한다: journal은 `MetaJson.Hash(profileId + "\n" + kind + "\n" + payload)`; start operation은 기존 `MetaJson.Hash("start:" + MetaJson.Encode(abandonActiveRun))`. 종류가 다른 두 fingerprint를 혼용하지 않는다.
- [ ] 모든 case를 현재 loader/validator로 검증한 뒤 TEMP에서 완성한 번들만 `FileUtil.CopyFileOrDirectory`와 대상 `AssetDatabase.ImportAsset`로 신규 fixture 경로에 복사한다. 캡처 실패 시 불완전 fixture를 남기지 않는다. 기존 파일을 대체하지 않는다.
- [ ] `unity command eval 'return FateDice.Editor.SymbolLoopV1FixtureCapture.Capture();' --project-path 'D:/UnityProject/Dice_Warrior' --format json`으로 한 번 캡처한다. 이후 fixture SHA256을 REPORT에 고정한다.
- [ ] 아래 이름/단언으로 나머지 검사를 작성한다. fixture 자체는 이후 읽기 전용이다.

| 테스트 | 필수 단언 |
|---|---|
| FixtureHashesMatchFrozenBytes | bundle 버전/기준 커밋, 8묶음과 변형 존재, 파일 SHA256 일치, case/file ID 중복 없음, 원문에 v2 mode 없음 |
| CaptureRefusesExistingFixture | Capture 재호출 거절, 기존 fixture bytes/mtime 불변 |
| StandaloneFixturesResumeRecordedCommands | 두 지도 버전의 로드 원문 bytes 불변, 기록한 명령을 같은 ID/인자로 실행하면 전체 snapshot/sequence/RNG 일치 |
| ProfileAndImportedRunsKeepDifferentSettlementPolicies | 정상 v1 지급=`events * frozen currencyPerEvent + wonBonus`, imported만0, `legacyRun`/start/소유품 보존, 미정산 결과 교체 거절 |
| SettlementRetriesKeepSingleReceipt | 같은/다른 requestId 재정산에서도 지급1회, 동일 run receipt, 원래 보유품/성장 재화 보존 |
| PendingStartRetainsOriginalPayloadAndReceipt | 전/후 dispatch의 Unknown/Committed, mode 없는 원문 문자열·fingerprint·requestId·revision·세대 보존 |
| RecoveryFixturesPreserveReceiptsAndFenceOldRequests | 복원 후 receipt 포함 요청 조회 가능, 미포함 이전 세대 재시도 거절, pending 원문 불변, 복구 세대만 합법적으로 갱신 |
| InvalidLegacyCopiesFailWithoutOverwrite | TEMP 복사본의 envelope schema=999 및 checksum 오염 각각 거절, 실패 뒤 원문 bytes 불변 |

- [ ] 필터로 위 class의 실제 검사 전부를 실행해 PASS·실패0·skip/inconclusive0을 확인한다. 결과0건은 PASS가 아니다. 두 테스트가 같은 case를 읽어도 저장/복원 디렉터리는 공유하지 않는다.
- [ ] 캡처 도구/테스트의 CodeMap을 작성하고 INDEX에 직접 관계만 추가한다. fixture 생성과 재연 검사의 책임을 구분한다.

회귀 기대값에는 기록된 게임/소유/정산 필드를 전부 포함한다. 미래 P1에서 추가된 mode 필드를 단순 무시하거나 fixture를 재생성해 통과시키지 않는다. 구형 codec이 보장할 projection의 존재·타입·구형 기본값 검사는 P1 실행 계약에 명시한다.

## Task 3: 새 규칙 시험 입력과 P0 완료 증거

**Files:** `SymbolLoopMvpCases.json` 및 `.meta`, PLAN/REPORT. 기존 제품 규칙/설정 수정 없음.

**Interfaces:** 이 JSON은 후속 SymbolCombo/AutoCombat/LoopBoard 테스트의 독립 오라클 입력이다. P0에서 새로운 제품 resolver나 DTO를 만들어 소비하지 않는다.

- [ ] `formatVersion=1`, `symbols=[sword,staff,fire,shield]`, `maxCombatDice=3`, `boardTileCount=12`, `regionCount=3`, `travelValues=[1,2,3,4,5,6]`를 기록한다.
- [ ] 시험 전용 기본 효과를 sword damage5, staff damage4, fire damage6, shield block5로 둔다. 등록 조합은 sword+staff→arcSlash damage12, sword+staff+fire→flameArcSlash damage20만 둔다. 이 수치는 기능 경계 오라클이며 실제 콘텐츠 밸런스 채택이 아니다.
- [ ] 독립 기대값을 기록한다: sword→기본기1; sword+staff→조합1; sword+staff+fire→상위1; sword+staff+shield→기본기3; sword×3→기본기3. 순열은 조합 판정과 계획 구성만 동일하다.
- [ ] D1 case: 적 HP5, [sword,shield]는 적용1/사망 생략1/반격0. [shield,sword]는 적용2/생략0/반격0. 둘 다 전투 종료 후 플레이어 보호막0.
- [ ] D2 case: 적 HP100/보호막7, [shield,sword,staff]는 적 최종 HP98/보호막0, 플레이어 보호막5를 얻는다. 적 Attack8 이후 플레이어 HP는3 감소하고 보호막0. 이것이 첫 기본기에서 적 보호막을 만료하는 구현을 거절하는 오라클이다.
- [ ] 이동 case: (위치0,눈1)→타일1, (0,6)→타일6, (9,5)→완주 대기/일반 타일 사건0/초과 소멸을 기록한다. 완주 보상·최종 보스의 제품 동작은 P3/P4에서 검증한다.
- [ ] 신규 코드와 요약을 포함한 최신 컴파일/전체 EditMode를 검증한다. 기존 baseline 전체 PlayMode 뒤 제품/UI/자산이 그대로이고 신규 코드가 Editor/tests 전용이면 PlayMode를 불필요하게 반복하지 않는다. 새 실행 영향/실패가 있으면 해당 회귀만 근거를 기록하고 추가한다.
- [ ] 보호 파일/fixture/원문/제품 소스 해시 및 Git 범위를 비교한다. 동적 폰트 등 보호 대상이 달라졌다면 자동 복원·새 상태 승인 없이 차이와 정확한 사본을 남기고 완료를 보류한다.
- [ ] REPORT에 실제 명령·실행 수·시간·실패/skip·fixture 해시·보존 결과를 기록한다. P0 성공 시에도 기호 전투·순환 보드·모드 저장 구현은 NOT_RUN/미구현이다. P1a로 넘어가려면 별도 상세 실행 범위를 확정한다.

## 검증 명령과 결과 판정

아래는 **승인된 P0 실행** 명령이다. 실행 전 metadata가 바뀌었는지만 확인하고 현재 지원하지 않는 옵션을 추측해 반복하지 않는다. 설치된 `TestCommands.cs:22–29`에서 mode/filter/filter_type/async_tests/timeout 인자를 확인했다.

```powershell
unity command recompile --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command recompile_status --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command run_tests --mode editor --filter FateDice.EditMode.Tests --filter_type assembly --async_tests true --timeout 900 --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command test_status --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command run_tests --mode playmode --filter FateDice.PlayMode.Tests --filter_type assembly --async_tests true --timeout 900 --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command test_status --project-path 'D:/UnityProject/Dice_Warrior' --format json
unity command run_tests --mode editor --filter SymbolLoopV1CompatibilityTests --filter_type testName --async_tests true --project-path 'D:/UnityProject/Dice_Warrior' --format json
```

명령을 한꺼번에 실행하지 않는다. async 접수 뒤 같은 실행의 terminal 결과를 확인한 다음 다음 명령을 실행한다. 완료 응답/실제 수/실패0이 PASS의 근거이며 접수 success는 PASS가 아니다. 연결/Editor 부재는 WAIT_VALIDATION, 실제 검사 실패는 FAIL로 구분한다. headless ticking이 필요하면 현재 상태를 먼저 확인하고 기존 프로젝트 설정을 자동 변경하지 않는 범위에서만 진행한다.

## 실행 방식·예산·완료 조건

- 권장 방식: **메인이 현재 세션에서 순차 실행(Native)**. 단일 Unity·캡처 시점·공유 fixture 때문에 여러 작성자로 나눌 이점이 작다.
- 현재 계획 작성은 메인만 썼으며 기존 persistence_review에 API 사실 조회 1건을 추가해 누적 위임3/3, 고유 보조2명이다. self-review는 메인이 수행했다.
- 기존 DOCUMENT 이력은 최초 검토1/1·보완2/2로 유지한다. 이름이나 세션을 바꾸어 초기화하지 않는다.
- P0 실행은 아직 시작되지 않은 테스트 도구/합성 fixture 산출물의 첫 검증이다. 실행 승인 시 P0의 최초 산출물 검증1묶음과 이후 수정·재검증 총 최대2묶음을 적용하고, CodeMap/보조/각 Task에 별도 예산을 주지 않는다. 이전 DOCUMENT 결과나 완료된 RECOVERY_STABILITY를 다시 감사하지 않는다.
- P0 실행 중 보조 위임은 하지 않는 안을 권장한다. Unity 실행·공통 계약·모든 파일·최종 검증은 메인 소유다.
- 필수 AC: 변경 전 실제 전체 검사, 합성 8묶음 고정 bytes와 재연 검사 PASS, 새 규칙 오라클 데이터, 정확한 1:1 요약/색인/REPORT, 보호 대상 불변. 필수 미검증·기존 실패·보존 차이를 남긴 채 P0 COMPLETE로 판정하지 않는다.
- Android 빌드/실기기는 P0 테스트 도구 범위에 비적용이며 P5 통합 검증으로 남는다. 기존 실행 기록을 신형 모드 검증으로 사용하지 않는다.

**계획 self-review:** 승인된 설계는 전환 REPORT와 연결했고, 이 계획은 그중 P0만 수행한다. P1a~P5 제품 구현은 범위 밖임을 명시했다. 기존 public API/fixture 경로/단일 작성자/테스트 채널을 소스와 대조했고, 위 5개 검토 초점을 Task 2에 배정했다. 이후 사용자의 순차 실행 요청으로 실행 계획과 메인 순차 방식이 승인됐다.

## P0 실행 기록 / 2026-10-20

- 사용자 승인: “순차적으로 진행해줘”. 메인이 현재 체크아웃에서 Task 1 → 2 → 3을 순차 실행한다. 추가 보조 위임·worktree·branch·commit은 하지 않는다.
- 스킬 적용: executing-plans, test-driven-development, unity-pipeline. 연결 실패는 systematic-debugging으로 원인을 확인한다. 별도 스킬 장부·추가 리뷰 예산 대신 본 PLAN/REPORT를 사용한다.
- P0 최초 산출물 검증: 미실행. 수정·재검증 0/2. DOCUMENT 이력의 1/1·2/2는 그대로 유지한다.
- Task 1: HEAD/staged/기존 변경 재확인 및 686개 보호 파일 해시·폰트 원본 사본 확보. 씬 dirty=false, Play stopped, 기존 검사 없음. 실제 목록 EditMode317/PlayMode228.
- 연결 복구: Pipeline 서버는 실행되지만 descriptor가 없어 CLI 발견 실패. Unity 메뉴 Stop/Start 후 샌드박스 CLI 호출 때 다시 사라졌고, 재시작 후 정상 사용자 환경(`require_escalated`)에서만 CLI를 호출하자 descriptor 전/후 존재 및 editor_status ready를 확인했다. 이후 Unity CLI는 정상 사용자 환경에서만 실행한다. 패키지/설정/잠금/제품 파일 변경 없음.
- UI 경로: 최초 Orca 연결은 종료됐다. 사용자 “오르카는 실행하지말고 해결해봐” 이후 Orca를 실행하지 않고 Codex Windows 도구(@oai/sky)로 기존 Unity의 Pipeline 메뉴만 제어했다. 이후에도 Orca를 실행하지 않는다.
- 실제 baseline EditMode: completed, 317/317, 실패/skip/inconclusive 0, 47.35초. PlayMode 실행 중.
- Task 2/3: 미착수. 변경 전 실제 baseline이 확보되기 전 신규 C#/fixture를 생성하지 않는다.
