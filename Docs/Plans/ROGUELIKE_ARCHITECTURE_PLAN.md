# ROGUELIKE_ARCHITECTURE_PLAN

모드 IMPLEMENT · 2026-09-10 · 상태 RA-A/B/C/D PASS. 코드/필수 로컬 검증 완료, Android 빌드 PASS, 실기기 NOT_RUN. 승인된 설계 v0.2의 A→B→C→D를 구현한다. 기본 기술은 Unity 6000.6.0f1, 기존 C#/uGUI/Pipeline이며 새 패키지를 설치하지 않는다.

## 기준과 보호
- 실제 프로젝트: D:/UnityProject/Dice_Warrior. 시작 HEAD 83d82766795dfd5528a492f82c59d433ba556339, main. staged 없음.
- 사용자 변경: Assets/TutorialInfo/Icons/URP.png, Docs/Dice_Warrior_Improvement_Architecture_v0.2.md, Docs/Dice_Warrior_Codex_Implementation_Goal_v0.2.md, ProjectSettings/Packages/ 아래 설정. 변경하지 않는다.
- Goal 실제 위치는 Docs/ 바로 아래다. Docs/Goals/에 복제하지 않는다. AGENTS.md, GDD, 설계/Goal, 기존 PLAN/REPORT는 읽기 전용이다. 없는 Docs/AI 보조 문서를 만들지 않는다.
- 사용자 저장과 기존 GUID/.meta를 보존한다. 자동 시험은 임시 경로를 주입하며 기본 저장 존재/바이트를 전후 비교한다. Git staging/commit/push/merge/브랜치 변경 금지.
- 유지 규칙: 6주사위/10패/단일 RNG 소비 순서, 패 우선순위와 운명력 분리, 슬롯별 등급/유형 보장/중복 허용, 와일드 비우대/소유 ID 집합, 빈 풀 원본 불변, 태그·반올림·보호막·처치 후 반격 억제, 사건당 정확히 +1, 1주사위+후보 재생성 원자 재굴림, 운명 상한 적용 범위, HP/위력/수호/3장비.
- E(계정·Firebase·영구 경제·서버 정산), 외형 재제작, 신규 시스템/패키지/전역 설정은 제외한다. 기존 화면과 한 구간 루프를 유지한다.

## 고정 단위와 예산
| 단위 | 수용 조건/필수 검증 | 의존 | 상태 | 최초 검토 | 수정·재검증 |
|---|---|---|---|---|---|
| RA-A | 새 여정 공급자 호출, 고정 주입·0 거절·이어하기 RNG/후보 보존, 탐험/전투 각각 굴림/결과 유지, 표시 변경 시 규칙 불변, 영향 테스트+기존 루프 | 없음 | PASS | 1/1 | 1/2 |
| RA-B | IRunStore, 단일 상태 변경, 깊은 표시 복사, 시간 기록, 후보→검증→저장→확정, 저장 실패 전체 불변·중복/오래된 입력 거절·UI 복구+루프 | A PASS | PASS | 1/1 | 3/(기본2+추가1) |
| RA-C | FateDice.Core/noEngineReferences, Domain/Application, SO/규칙/상태/표시 분리, 명시 복제·순수 검증, schema1·null/빈 DTO·ID 호환, 46,656 오라클·명령 재연·독립성+루프 | B PASS | PASS | 1/1 | 0/2 |
| RA-D | Damage/Block 조합과 공유 평가, 신규 카드 정확히1종 AddAction→실제 추첨→태그→사용→복원, 중복 정책, 확정 등급 가격, 구형 저장, 주사위 분포/효율, 전체 Edit/Play·완주·패배/재시작 | C PASS | PASS | 1/1 | 1/2 |

수정 합계 5/8(B-02 사용자 추가 승인1회 포함). 최초 검토 한 묶음 뒤 결함 수정만 집계한다. TDD/증분 컴파일은 최초 검토 전 개발이며 별도 설계 검토가 아니다. 같은 원인의 Finding은 원래 단위와 횟수를 승계한다. 최종 통합 검증에 새 예산 없음.

## 담당/위임
메인이 공통 계약·런타임·설정·자산·CodeMap 색인·PLAN/REPORT·Unity 실행을 소유한다. 동시 최대3명(메인 포함), 누적 보조 위임 최대4건, 재귀 위임 금지. 다른 에이전트는 Unity 실행과 실제 프로젝트 쓰기를 하지 않는다. staging 산출물만 메인이 검토 후 통합한다.
- 위임 A-1 (누적1/4 사용): RA-A 새 테스트와 요약 작성 완료, root 통합. 같은 위임에서 현재 소스/계약/테스트의 RA-A 최초 검토를 읽기 전용 수행한다. 메인 코드 검토와 전체 Play 최초 실행을 한 묶음으로 통합한다. agent의 실제 프로젝트/Unity/Git 쓰기는 금지한다.

## RA-A 변경 계약과 정확한 대상
- ISeedSource.NextSeed(): 새 여정 한 번당 한 번 요청. SystemSeedSource는 암호 난수 공급으로 nonzero uint를 반환(게임 RNG와 독립); FixedSeedSource는 nonzero 고정 입력. 0은 현재 RunSession.New와 작업실 계약대로 명시적으로 거절하며 저장/기존 런은 유지한다. 무작위 연속 값 불일치를 성공 조건으로 삼지 않는다.
- GameApplication.Bootstrap/RunUIController.Initialize의 마지막 선택 인자로 공급자를 주입한다. 기존 Seed setter는 고정 공급자 선택 호환 API로 유지하고 getter/렌더/이어하기는 공급자를 호출하지 않는다. 일반 초기화는 새 공급자, 작업실은 FixedSeedSource를 명시한다.
- PresentationSettings의 explorationDice/combatDice에 각각 rollSeconds, resultHoldSeconds. 신규 설정 기본 .65/.9초(현행 실제값). 두 그룹은 독립이고 0은 지연 없이 다음 표시로 진행하며 숨은 최소값을 적용하지 않는다. 음수·NaN·무한값은 설정 오류다.
- 기존 rollSeconds와 DiceRollUI.resultHoldSeconds는 구형 직렬화/제작 호환값으로 숨겨 보존한다. 구형 저장에 새 timing 그룹이 없으면 기존 실제 굴림 max(.65, legacy rollSeconds)와 .9 결과 유지로 읽는다. 새 그룹이 있으면 값을 그대로 사용한다. 복원에 최신 SO를 덮어씌우지 않는다.
- 롤 명령은 먼저 저장되고 확정 결과만 표시한다. 재굴림도 현재 탐험/전투 그룹을 따른다. 표시 수명/시간이 규칙 RNG를 쓰지 않는다.

변경/추가 파일 (모두 Assets/_Project/ 상대):
- 추가 Features/Run/Runtime/ISeedSource.cs, Features/Run/Runtime/FixedSeedSource.cs, Features/Run/Runtime/SystemSeedSource.cs (각 신규 meta).
- 수정 Features/Run/Presentation/RunUIController.cs, DiceRollUI.cs: 주입/각 phase 시간 사용과 0 종료.
- 수정 Features/Run/Flow/GameApplication.cs, Features/Run/Editor/PlayWorkbenchSession.cs: 조립 주입.
- 수정 Features/Fate/Configs/FateDiceConfig.cs: 표시 그룹/호환/검증.
- 수정 Features/Save/Runtime/LocalRunStore.cs: 구형 JSON의 새 시간 그룹 누락을 JSON의 실제 config.presentation 경로에서 판별하여 저장된 legacy 시간으로 복원한다. Unity는 초기값 없는 DTO에도 inline 객체를 생성하므로 누락 검출에 사용하지 않는다. 기존 설치된 Newtonsoft.Json 3.2.2의 JObject만 저장 어댑터에 사용한다(패키지/lock/asmdef 변경 없음). schema/체크섬/규칙 검증/원본 파일/현재 설정 SO는 변경하지 않는다.
- 수정 Features/Run/Editor/PrototypeAuthoring.cs, UIWorkbenchPreview.cs: 제작 기본값/미리보기 동일 시간.
- 필요 시 수정 Features/Run/Tests/PlayMode/CombatFeedbackTests.cs: 기존 결과 읽기 계약을 새 SO 표시 설정 경계로 이동(검사 강도 유지).
- 수정 Features/Run/Tests/PlayMode/FateDiceGuiTests.cs, SceneStructureTests.cs: 기본 33에 의존하던 완주 재연을 명시적 고정 시드 주입으로 전환. 기존 승리·상태·보상·레이캐스트 assertion은 유지.
- 수정 Features/Run/Tests/PlayMode/KoreanUiTests.cs: 새 여정 뒤 Combat 첫 노드/유형을 기대하는 한국어 회귀도 고정33 명시. 의미적 기대값 유지.
- 추가 Features/Run/Tests/EditMode/SeedAndPresentationTests.cs 및 Features/Run/Tests/PlayMode/SeedPresentationFlowTests.cs (+meta): 계약·실제 입력·시간·실패·복원.
- 자산 허용: Features/Fate/Configs/DefaultFateDice.asset만 Editor API로 새 두 그룹을 .65/.9로 명시 저장. 기존 다른 규칙 값/GUID 유지. Scene/Prefab 재생성 없음. DiceRollUI.prefab은 변경하지 않고 legacy 필드 유지.
- 위 C# 1:1 Docs/CodeMap 요약 및 직접 관계/INDEX만 동기화.

RA-B/C/D는 위 수용 조건으로 고정된 단위다. 정확한 신규/이동 경로와 계약은 해당 단계 시작 전 현재 소스에 근거해 이 문서에 등록한다. 앞 단계 검증 없이 종속 단계 구현을 완료 처리하지 않는다. D 시험 카드 수치·가격 배율은 D 코드 수정 전에 별도 절에 기록한다.

## 실행/검증
- 연결된 Editor PID64968, Pipeline localhost7801, Unity 6000.6.0f1. CLI는 C:/Users/coli4/AppData/Local/Unity/bin/unity.exe. --project-path D:/UnityProject/Dice_Warrior --json --no-banner로 대상 지정. 기존 autotick16ms 사용. status 초기 검색 실패 뒤 pipeline list/editor_status로 ready를 확인했다. 추가 Editor 실행/잠금 삭제 없음.
- 변경 전 관련 EditMode 및 PlayMode 루프 기준을 새로 실행한다. TDD 실패 확인→구현→영향 검증. run_tests 비동기 핸들 완료를 test_status로 확인하며 0개 실행은 PASS가 아니다. 재로드 필요 시 기존 EditorUtility.RequestScriptReload 사용.
- 같은 시작 스냅샷/명령열의 패·ID·등급·피해·보상·진척·RNG를 비교. 새 런 비교 시 runId/playedSeconds만 명시 정규화하고 선택 ID/인자를 유지한다.
- 증거는 staging artifacts/roguelike-architecture/ 아래 원본 출력/보호 해시/시험 도구로 남긴다. 운영 문서는 이 PLAN과 대응 REPORT 두 개만 생성한다.
- Unity 검증 불가 시 WAIT_VALIDATION, 실제 실패 FAIL. Android 모듈/기기는 현재 환경 조사 후 없으면 해당 항목만 NOT_RUN. 설치/ProjectSettings 전환이 추가로 필요하면 해당 부분만 보류한다.

## 진행
- [x] 현재 HEAD·사용자 변경·원문·실행 채널 확인.
- [x] RA-A 변경 전 기준/실패 사례, 구현, 영향/루프 검증, 최초 검토.
- [ ] RA-B 구현/검증: 최초 PASS 후 최종 통합에서 B-02 발생, 수정2 회귀 미해결.
- [x] RA-C 구현/검증.
- [ ] RA-D 구현/전체 검증/완료 감사.

## RA-A 최초 검토 및 수정1
- RA-A-01: 유효한 null 시간 그룹을 가진 설정을 JsonUtility로 복제하면 legacy 1.25초가 .65초로 변한다. a-review-timing-copy-probe.json에서 DeepCopy와 New 모두 재현. Snapshot/DeepCopy/New의 실효 시간과 원본 독립성 보존이 해결 조건이다.
- 최초 검토 범위의 추가 필수 Finding 없음. 전체 PlayMode 95/95 PASS(194.22초). 수정1에서 기존 SeedAndPresentationTests에 양 phase null 복제 회귀를 추가하고 GameConfigData.DeepCopy의 누락 그룹만 원본 실효값으로 독립 복원한다. C# 1:1 요약을 갱신한다.
- 검증: 추가 회귀 RED, 전체 EditMode, 영향 SeedPresentationFlowTests와 기존 RealButtonsCompleteSectionAndRestart 스모크. A 최초1/1, 수정1/2, 전체1/8로 승계한다.

## RA-B 변경 계약 (A 필수 검증 PASS 후 구현)
- 상태 소유: RunSession 생성 시 입력 RunState를 깊게 복제하고 private state만 명령 후보로 변경한다. State 호환 getter와 ReadSnapshot()은 독립 복사만 반환한다. 기존 typed RunUIData는 이 복사에서 생성하며 배열/설정/카드/노드/보상/기록을 통해 원본을 바꿀 수 없다. 코어 분리 전 JSON 복제를 사용하되 RA-A의 null 시간 실효값을 보존한다.
- 저장: IRunStore는 Exists/Load/Save/Archive의 단일 로컬 슬롯 계약이다. Controller/Bootstrap에 인터페이스를 주입한다. RunSession 생성/새 런의 마지막 선택 인자로 저장소를 전달하며 새 런은 초기 상태 저장 성공 후 반환한다. Checkpoint delegate 호환 API는 기존 검사/독립 규칙 호출을 위해 유지하되 외부에는 후보의 복사만 전달한다. Live state 별칭을 주지 않는다.
- 후보 검증: RunStateValidator를 기존 LocalRunStore에서 동일 조건으로 추출한다. 이는 B의 저장소 구현과 독립된 후보 검증을 위한 직접 필요 파일이며 C에서는 Core로 소속만 옮긴다. LocalRunStore는 해당 검증 오류를 기존 파일 경로 포함 InvalidDataException으로 변환한다. 파일 저장·체크섬·schema/JSON 호환은 유지한다.
- 명령: bool API에 마지막 선택 인자 RunCommandToken(runId,sequence)을 추가한다. lock + 실행 중 플래그로 상태 검사부터 후보 검증/저장/확정을 직렬화하며 저장 callback 재진입은 거절한다. UI는 화면을 그린 snapshot의 token을 callback에 캡처한다. 상태가 바뀐 후 도착한 이전 입력/상점·보상·재굴림 등 ID 없는 명령도 token 불일치로 거절한다. 과거 호출자는 bool API의 현재 상태 검사를 유지한다.
- 시간: RecordElapsed(seconds)는 유효한 비음수 유한값만 pending 시간에 누적하고 매 프레임 저장하지 않는다. 다음 수락 명령 또는 SaveCheckpoint()에서 후보 시간에 합쳐 저장하며 실패 시 pending과 확정 상태를 유지한다. 성공 시 pending을 비운다. Result/로비/전환 중의 누적은 기존 정책대로 금지한다. 시간 저장만으로 명령 sequence나 RNG를 변경하지 않는다.
- 새 런 기록: New의 선택 인자 previousResult를 독립 복사해 초기 저장에 포함한다. Controller의 lastResult 직접 쓰기를 제거한다.
- UI 오류: 명령 저장/확정 오류와 이후 피드백·렌더 오류를 구분한다. 공개 RefreshView는 확정 snapshot만 다시 표시하고 명령을 재실행하지 않는다. 실패한 연출/렌더는 잠금과 transform을 정리하고 다음 표시를 재시도할 수 있다.

정확한 대상 (Assets/_Project/ 상대, 기존 meta 유지):
- 추가 Features/Save/Runtime/IRunStore.cs, Features/Run/Runtime/RunCommandToken.cs, Features/Run/Runtime/RunStateValidator.cs (+ 신규 meta).
- 수정 Features/Run/Runtime/RunSession.cs, Features/Save/Runtime/LocalRunStore.cs, Features/Run/Presentation/RunUIController.cs, Features/Run/Flow/GameApplication.cs.
- 수정 Features/Run/Tests/EditMode/RunTests.cs, SeedAndPresentationTests.cs: 세션 직접 mutation fixture를 독립 상태 준비→새 세션으로 변경. 기존 수치/행동/저장 assertion은 유지. 순수 규칙 fixture와 명령용 유효 checkpoint fixture의 차이를 명시한다.
- 수정 Features/Run/Tests/PlayMode/FateDiceGuiTests.cs, CombatLayoutTests.cs, SeedPresentationFlowTests.cs: 실제 버튼 검사의 준비 상태를 저장 전에 독립 DTO에서 구성한다. 기존 oracle/raycast/시간/비용 검사는 유지.
- 추가 Features/Run/Tests/EditMode/RunBoundaryTests.cs 및 Features/Run/Tests/PlayMode/RunBoundaryFlowTests.cs (+meta): 소유권·저장 실패·재진입·오래된 token·시간·실제 UI 저장 성공 뒤 표시 복구 검증.
- 변경 C# 대응 CodeMap과 직접 참조 요약/INDEX. Scene/Prefab/SO/패키지/설정 변경 없음.
- 검증: 신규 소유권 RED→구현, 전체 EditMode와 기존 GUI 완주 및 새 UI 경계 PlayMode. 저장 실패는 전체 JSON(시간 포함)/HP/비용/RNG/제시 ID/처리 ID를 검사한다. 구형 저장은 기존 SaveTests와 현재 seed/timing 호환 검사를 유지한다.

- RA-A 수정1: Edit160/160(13.16초), Play95/95(193.94초) PASS. RA-A-01 해결. 보호171개/사용자 저장 동일, CodeMap73개 전수 대응 및 meta 확인. A 종료→B 시작. 일시적인 main-thread timeout 후 기존 비동기 결과를 수집했으며 중복 실행/잠금 삭제 없음.
- 위임 B-1(누적2/4): B 신규 테스트·기존 준비 fixture의 캡슐화 호환 변경·대응 요약을 staging에 작성한다. 메인 runtime/계약/PLAN/REPORT/Unity/실제 통합과 파일 작성 경계를 분리한다. 재귀 위임 없음.


- B 시간 저장 세부: 새 상태/Checkpoint 저장소 교체 뒤 첫 SaveCheckpoint는 반드시 실행한다. 저장 성공한 동일 상태에 pending시간0인 반복 flush는 쓰기를 생략한다. 저장 실패는 clean으로 표시하지 않는다. sequence int.MaxValue 증가로 생기는 잘못된 후보는 기존 카운터 검증에서 저장 전에 거절한다.


- 위임 C-1(누적3/4): B 실행 검증 중 현재 코드/직접 CodeMap/설계를 읽고 RA-C 최소 분리의 정확한 파일과 구형 저장·표시 설정 호환 경계를 제안한다. B PASS 전 구현 금지. root 계약 확정 후 같은 위임의 staging 전용 독립 파일 작업으로만 이어갈 수 있다. 새 운영 문서/실제 프로젝트/Unity/Git/재귀 위임 금지.


## RA-B 최초 검토 묶음
- Edit181/181(14.62초), 신규 실제 UI3/3(2.03초) PASS. 전체 Play98 실행 및 B 코드/계약 검토를 최초1/1 묶음으로 시작한다. 저장 경계 검토는 동일 B-1 위임, UI와 보호/실행은 root. 수정0/2, 합계1/8.
- C-1 조사안은 Runtime RunState/RunSession 호환 facade, Core RunRulesCatalog/RunStateData/CoreRunState/RunApplication으로 분리한다. B PASS 후 정확한 구현 계약을 확정하며 현재는 설계 준비만 수행한다.


## RA-C 확정 기술 계약 (B 전체 검증 PASS 이후 착수)
- Core는 실제 규칙과 명령 후보/검증/저장 확정을 소유한다. 기존 Runtime RunSession은 공개 API를 유지하는 얇은 facade이며 병렬 규칙 엔진을 만들지 않는다. IRunStore와 파일 직렬화는 Runtime에 남고 Core에는 Action<CoreRunState> 체크포인트 callback만 전달한다.
- RunRulesCatalog에는 version/dice/fate/combat/growth/world와 순수 검증/조회/명시 깊은 복제를 둔다. GameConfigData는 이를 상속하고 presentation만 추가하여 기존 SO 및 schema1 config의 flat 필드명을 보존한다. 규칙 카탈로그와 표시 설정은 독립 복사하며 불변 공유를 도입하지 않는다.
- 순수 RunStateData는 config를 제외한 기존 상태 필드와 abstract Rules getter를 가진다. CoreRunState.config는 RunRulesCatalog, Runtime RunState.config는 GameConfigData이며 CopyFields/ToCore로 명시적으로 전체 상태를 변환한다. 목록/배열/중첩 DTO/기록 및 null/빈 값은 독립 복사한다. 저장 JSON에서 null이 빈 DTO로 읽히는 기존 의미는 phase와 ID 기반 검증으로 유지한다.
- Runtime RunSession은 생성 시 저장된 표시 설정만 따로 복사한다. Core 후보/복사에는 Unity JSON과 Unity 값이 없다. checkpoint/export 시 Core 규칙과 독립 표시 복사로 기존 RunState를 조합하며 최신 SO를 읽지 않는다. RA-A null timing의 실효값 .65/legacy 및 명시0을 보존한다.
- RunApplication은 RA-B 명령/잠금/token/시간/후보→검증→callback→확정 코드를 계산식/RNG 소비 순서 변경 없이 이동한다. RunStateValidator와 RunCommandToken은 GUID를 보존하여 Domain/Application으로 이동한다. Runtime 저장과 facade 시작 경계에서 표시 설정도 검증한다.
- Core asmdef noEngineReferences:true, references:[] 및 overrideReferences:true/precompiledReferences:[]로 Unity와 외부 플러그인 참조를 막는다. 기존 규칙 5개 폴더는 asmref로 Core에 소속시켜 불필요한 파일 이동을 피한다. Runtime/Editor/EditMode/PlayMode에 Core 직접 참조를 추가한다. SharedUI 참조는 바꾸지 않는다.

정확한 대상 (Assets/_Project/ 상대):
- 신규 Features/Fate/Domain/RunRulesCatalog.cs, RulesCopy.cs; Features/Run/Domain/RunStateData.cs, CoreRunState.cs, RunStateCopy.cs.
- 신규 Features/Run/Application/RunApplication.cs.
- 수정 Features/Fate/Configs/FateDiceConfig.cs; Features/Run/Runtime/RunState.cs, RunSession.cs; Features/Save/Runtime/LocalRunStore.cs.
- GUID 보존 이동 Features/Run/Runtime/RunStateValidator.cs → Features/Run/Domain/RunStateValidator.cs 및 RunCommandToken.cs → Features/Run/Application/RunCommandToken.cs.
- 수정 기존 Dice/Runtime/DiceRules.cs, Fate/Runtime/FateCardRules.cs, Combat/Runtime/CombatRules.cs, Growth/Runtime/GrowthRules.cs, Exploration/Runtime/ExplorationRules.cs: 인자 RunStateData/RunRulesCatalog 및 Rules 접근으로 치환. 계산식/ID/순서 변경 없음.
- 신규 Features/Run/Domain/FateDice.Core.asmdef 및 Fate/Domain, Run/Application, 위 5개 규칙 Runtime 폴더의 FateDice.Core.asmref(각 신규meta). 기존 Features/FateDice.Runtime.asmdef, Run/Editor/FateDice.Editor.asmdef, Run/Tests/EditMode/FateDice.EditMode.Tests.asmdef, Run/Tests/PlayMode/FateDice.PlayMode.Tests.asmdef에 Core참조 추가(기존meta 유지).
- 신규 Features/Run/Tests/EditMode/CoreBoundaryTests.cs, CoreReplayTests.cs 및 Fixtures/ReplayBaseline.json(+meta). 기존 테스트의 공개 호환 API와 assertion 유지. 변경 C# 1:1/이동 CodeMap, 직접 관계 및 INDEX 갱신. Scene/Prefab/SO/패키지/ProjectSettings 변경 없음.
- C-1 위임은 catalog/RulesCopy/상태DTO/RunStateCopy 및 5개 규칙의 staging만 소유한다. root는 나머지·실제 통합·Unity·PLAN/REPORT/INDEX를 담당한다. 기존 위임을 이어가며 누적3/4 유지.

검증 기준:
- 변경 전 실제 B 코드로 7개 시나리오/273명령 재연 기준을 확보했다(c-baseline-capture.json, ReplayBaseline.json). 초기 전체 저장 JSON, 각 명령명/선택 ID/인자/경과시간 및 모든 상태 필드의 정규화 SHA256을 보관한다. 정규화는 JSON 객체 property 순서 정렬만 하며 runId/시간/RNG/ID/어떤 필드도 제외하지 않는다. 일반 경로·각5유형·패배, 재굴림·구매·보상·장비·특수주사위·보스 명령이 포함된다.
- Core 부재/참조 차단 RED와 기존 명령 재연을 먼저 실행한다. 분리 후 위 고정 초기 JSON/명령을 그대로 실행하여 단계마다 전체 상태 hash/RNG/sequence를 비교하고 Save/Load를 끼워 재연해도 같아야 한다. 기존 46,656 패 오라클과 전체 Edit/Play도 다시 실행한다.
- Core 어셈블리 참조/컴파일, SO·Core 카탈로그·표시·저장 callback·외부 DTO 독립성, 명시 복제의 null/빈 데이터와 legacy schema1/시간을 검사한다. fixture의 기존 결과를 새 코드로 갱신하여 통과시키지 않는다.

## RA-B-01 / 수정1
- 전체 Play98 중91 PASS/7 FAIL. CombatFeedbackTests.CombatSave의 State.hp/enemyHp/shield/enemyShield 직접 쓰기4곳이 B 표시 복사 경계로 인해 fixture에 적용되지 않았다. 기존 수치 oracle 실패이며 제품 계산식을 바꾸지 않는다.
- 직접 대상 추가: Features/Run/Tests/PlayMode/CombatFeedbackTests.cs 및 해당 CodeMap. ReadSnapshot 준비 DTO에4값을 적용한 뒤 new RunSession(prepared)로 구성하고 기존 Roll/Save 및 모든 기대 피해량/실제 UI assertion 유지. 실패7개가 RED증거, 영향 CombatFeedback 및 전체 Edit/Play 재실행. B 최초1/1 수정1/2, 총2/8. C는 계속 대기.


- 위임 D-1(누적4/4): 앞 단계 실행 대기 중 특수 주사위의 현재 정의/독립 수학 분포·실제 RNG 표본/효율 검증을 준비한다. 실제 Unity/프로젝트/Git 쓰기와 C 통과 전 D 구현은 금지. staging 분석 산출물만 허용. C PASS 후 같은 위임에서 지정 테스트 파일만 이어서 작성한다. 추가/재귀 위임 없음.


- D-1 신규 spawn은 도구 thread limit로 생성되지 않았다. 완료된 A 보조 프로세스에 별개 D-1 준비를 배정하고 이 신규 업무를 누적4/4로 집계했다. A 검토 재개방/예산 초기화가 아니다. B/C 구현 검증 경계는 유지한다.


- RA-B 수정1 완료: Edit181/181(14.61초), Play98/98(196.54초) PASS. RA-B-01 해결. B 최초1/1 수정1/2/합계2/8. RA-C 확정 계약으로 구현 시작하며 기존위임 C-1 재개, 누적4/4 유지.


- C 고정 재연의 JSON object-order 정규화는 기존 설치된 Newtonsoft.Json을 테스트 어셈블리에서 명시 참조한다. EditMode asmdef overrideReferences:true/precompiledReferences:[nunit.framework.dll,Newtonsoft.Json.dll]; 패키지/lock/전역 설정 변경 없음. 첫 개발 컴파일에서 자동 plugin 참조가 없어 오류가 발생했으며 해당 테스트 경계를 명시한다.


- C 개발 전체 Edit199/200: 시간값100×.01의 JSON 재해석이 1.0000000000000007→1.0000000000000009로 바뀌어 경계 테스트 실패. 명시 복사 전환으로 숨겨졌던 double 정밀도 문제가 드러났다. 실제 LocalRunStore에서 현재·최근결과 duration bit 동일성 회귀를 먼저 추가한다. 인메모리 ProbeStore의 Copy helper는 RunState.DeepCopy로 이전하되 기존 전체 상태 동등 assertion은 유지한다(직접대상 RunBoundaryTests.cs/CodeMap 추가). 필요 시 LocalRunStore에서 기존 Newtonsoft JSON 파서로 두 double만 정확 복원하며 schema/payload/체크섬/파일을 바꾸지 않는다. 아직 C 최초 검토 전 개발 검증이며 실제 FAIL을 보고서에 보존한다.


- C 개발 보완 후 c-edit2-status.json Edit201/201(20.35초) PASS. 46,656패·고정273명령 연속/매명령저장·시간bit보존 포함. 현재 C 최초1/1 검토 묶음을 시작한다. 같은 C-1에서 Domain/순수참조/복제 검토, root가 Application/facade/저장/실행/보호를 검토한다. 전체Play98 실행 포함. 수정0/2·전체2/8 유지.


- 최초 C 검토의 root Application/facade/저장 점검에서 필수 Finding 없음. 현재 git diff --check는 B 수정 파일 CombatFeedbackTests.cs 및 해당 CodeMap의 마지막 빈 줄2건을 표시한다. 기능/파서 오류가 아닌 일반 형식 개선으로 FOLLOW_UP에 남기며 검증 PASS로 숨기지 않는다. C 필수 실행은 계속 진행한다.


## RA-C 종료 / RA-D 사전 계약
- C 전체 Edit201/201(20.35초), Play98/98(195.82초) PASS. 최초 코드 검토 필수 Finding 없음. 최초1/1·수정0/2, 총2/8. C 보호/현재 SO/사용자 저장/고정 재연 해시 유지. D는 아래 파일별 계약 등록 후 착수한다.

RA-D 확정 시험 수치와 정책:
- 신규 행동은 정확히1종: id ember_slash, 표시명 잔불 베기, Uncommon, Damage 위력계수1.6, 태그 attack/fire/physical. 기존 Damage 처리기를 쓴다. 기본3행동/와일드2행동에는 추가하지 않는다. treasure_0의 기존 보상에 addActionId만 더한다. 기존 골드/XP/장비/주사위 보상을 제거하지 않는다. 동일 actionId 재획득은 소유 목록에 추가하지 않고 나머지 보상/사건 종료는 그대로 처리한다.
- ActionEffectKind Damage/Block, ActionEffectDefinition(kind,coefficient) 배열을 추가한다. 기존 damageCoefficient/blockCoefficient는 schema1 및 기존 제작 호환 필드로 보존한다. effects가 null/빈 구형 정의는 두 기존 계수에서 효과를 구성한다. 명시 effects가 있으면 그 목록이 권위다. 기존 정의와 수치/연산/RNG는 그대로 유지한다.
- EffectResolver에 Damage/Block 처리기를 명시 등록한다. 효과 배열 순서대로 같은 기존 태그·등급 비율·AwayFromZero 반올림/최소효과/상한을 적용해 damage/block을 합산한다. 미리보기와 실제 CombatRules.Resolve가 같은 평가 결과를 쓴다. 미지원 kind/음수·비유한 계수/전체무효 효과는 정의/시작 검증에서 거절한다. 카드ID별 Session/UI 분기 없음.
- WorldSettings.shopPriceMultipliers=[1,1.1,1.25,1.5,1.75] (Common~Legendary). null/빈 배열인 구형 런은 전등급1을 사용한다. 가격은 저장된 기본가격×상점 encounter의 activeGrade 배율을 double로 계산해 AwayFromZero 정수화하고 int 최대에 제한한다. 기본가격0은0. 품목4종/재고·재입고/휴식훈련은 변경하지 않는다.
- 입장 시 List<ShopOffer>(productId,price)를 확정해 표시/구매/저장/재개가 동일 값을 사용한다. 추가 RNG를 소비하지 않는다. 구매 후 동일 상점 복귀는 재생성하지 않고 새 노드의 Shop 입장 때만 확정한다. 구형 저장에서 snapshot이 없으면 저장된 world.shop 기본가격만으로 호환 snapshot을 구성한다. 최신 SO/새 카드/새 가격표를 구형 런에 주입하지 않는다.
- 새 선택 저장 필드(effects/addActionId/shopPriceMultipliers/shopOffers)는 명시 복제/순수 검증/legacy 파일 어댑터에 동시에 반영한다. 구형 pendingReward와 소유/제시 카드의 원래 IDs/가격/규칙/RNG를 유지한다.
- C 고정 재연 fixture 자체와 hash 기대값은 바꾸지 않는다. D의 추가 필드만 구형 값임을 별도 확인한 후 이전 schema 필드의 전체 hash를 비교하는 호환 projection을 시험에 명시한다: 비어 있는 effects/addActionId/배율과 원본 기본가격과 같은 legacy shopOffers만 추가필드로 분리한다. 기존 필드/선택ID/명령인자/RNG/시간은 그대로 검사하며 신규 필드의 비구형 값이 들어오면 projection 전에 실패한다. 새 D 동작은 추가 필드까지 독립 full-state 검사로 검증한다.
- 신규 카드는 기존 visual catalog의 defaultAction fallback을 사용하고 한국어 정의명을 표시한다. 새 이미지/Prefab/Scene 제작 없음. RewardText에는 일반 addActionId 표시, Shop UI에는 확정 가격만 연결한다.

정확한 파일 대상 (Assets/_Project/ 상대):
- 신규 Features/Combat/Runtime/ActionEffects.cs (정의/명시 처리기/공유 resolver; 해당 Core asmref 적용).
- 신규 Features/Shop/Domain/ShopRules.cs 및 FateDice.Core.asmref(+meta); 가격 계산·고정 목록·구형 호환만 담당.
- 수정 Features/Fate/Domain/RunRulesCatalog.cs, RulesCopy.cs; Features/Run/Domain/RunStateData.cs, RunStateCopy.cs, RunStateValidator.cs.
- 수정 Features/Combat/Runtime/CombatRules.cs, Growth/Runtime/GrowthRules.cs; Features/Run/Application/RunApplication.cs, Features/Run/Runtime/RunSession.cs, Features/Save/Runtime/LocalRunStore.cs (필요한 호환 경계만).
- 수정 Features/Run/Editor/PrototypeAuthoring.cs, Features/Run/Presentation/RunUIController.cs. DefaultFateDice.asset만 Editor API로 신규카드1종/treasure_0 AddAction/가격배율을 연결한다. 원본 기존 값/GUID/Prefab/Scene 보호.
- 신규 Features/Run/Tests/EditMode/ContentExtensionTests.cs, DiceDistributionTests.cs, Fixtures/DiceDistributionExpected.json; Features/Run/Tests/PlayMode/ContentExtensionFlowTests.cs (+meta).
- 직접 수정 Features/Run/Tests/EditMode/CoreReplayTests.cs (위 구형 projection), 필요 시 RuleTests/RunTests/SaveTests와 PlayMode의 신규 가격·보상 영향을 받는 fixture만 계약 보존 방식으로 조정한다. 기존 기대 행동을 약화하지 않는다. 변경 C#1:1/직접관계/INDEX 동기화.
- D-1은 DiceDistributionTests/expected fixture/해당 CodeMap만 staging 소유하며 root가 나머지 D구현·테스트·데이터·실제Unity·문서를 소유한다. 누적4/4, 추가 위임 없음.

필수 실행:
- D 계약 부재 RED→효과/콘텐츠/가격 구현→실제 획득/독립 태그 수치/추첨/사용/중복/실패·재개 검증. 실제 UI 버튼의 보상/카드/상점 가격 일치 검사.
- 기본/Ember1/2/6개 독립 분포 모델과 실제 게임 RNG 8고정seed×62,500=구성당500,000·총2,000,000표본 비교. 패/운명력/슬롯등급 분포를 사전 Bernstein/union 한계로 비교하며 이상적 독립 모형의 alpha=1e-4와 결정적 PRNG 표본의 한계를 구분한다. 게임 규칙을 결과에 맞춰 바꾸지 않는다.
- D 종료의 최신 전체 Edit/Play, 한구간완주·패배/재시작, Android 모듈이 있는 실제 최신 APK 빌드. 기기 연결 없음은 실기기 항목만NOT_RUN. 보호/CodeMap/미해결Finding/예산최종감사.

- RA-D 개발 착수: d-red-status.json 신규 계약3개 모두 FAIL(0/3,.07초)로 부재 확인. 효과·AddAction·상점 가격 snapshot과 구형 호환을 구현 중이다. D 최초0/1·수정0/2, 전체2/8.
- 실제 통계 실행 전 고정 family는340=4구성×(10패+9운명력+36슬롯면+30슬롯등급), alpha1e-4 Bernstein/union이다. 이전184 준비안에서 운명력/슬롯 개별항을 추가했으며 실측 전 고정했다. 각 구성500,000표본/총2,000,000을 유지하고 temp/FateDiceDistribution/actual-{0,1,2,6}.json 원본을 수집한다.

## RA-D 개발 검증 / 최초 검토
- d-authoring.json: 새 행동6종 중 추가1종 ember_slash, treasure_0 AddAction, 가격배율을 Editor API로 연결. 기존 필드 전체 JSON projection 동일과 GUID fe77e0228b579cc47973d60ffdc64c29 확인. 구형 런에 새 SO 주입 없음.
- d-content-status.json 21/21 PASS(.84초), d-edit-status.json 전체227/227 PASS(41.23초): 기존46,656패·C고정273명령 재연·200만 실제 RNG 표본 포함. 뒤에 새 런/원본 비오염 1개를 추가하여 최종 전체를 다시 실행한다.
- d-flow-status.json 1/2 PASS,1 FAIL(2.87초). d-flow-diagnostic-status.json은 leave가 footer 메뉴에 가림을 확인한0/1 FAIL(.61초). 스크롤 바깥 버튼을 준비 없이 누른 새 fixture의 문제로, 기존 GUI 테스트와 같은 ScrollRect 가시성 준비/viewport 포함 검사를 추가했다. 제품/Prefab/기대값/assertion을 변경하지 않았고 d-flow2-status.json 2/2 PASS(2.91초).
- 새 카드 실제 포인터 보상→장비→노드→굴림→카드 피해30/저장 통과. 상점 Rare13/15/20/23 표시와 실제 차감/상품 소모/재개/퇴장 통과. 최초 검토 전 개발 FAIL 이력을 유지한다.
- D 최초 검토1/1 시작: root가 효과→소유→후보저장→가격수명→구형 snapshot/JSON 및 직접관계·SO 보존을 확인하고 최신 전체 Edit/Play와 Android 빌드/보호 검증을 함께 수집한다. 현재 필수 Finding 없음, D 수정0/2·총2/8. 별도 최종 통합 예산 없음.
- d-final-edit-status.json 최신 전체228/228 PASS(41.42초). 주사위4구성×500,000표본·340개 사전고정 비교 전부통과, d-final-distribution-actual-{0,1,2,6}.json에 실측과 오라클/허용범위 기록. Ember1개 평균운명력 감소(-5/324)는 정책 변경 없이 FOLLOW_UP으로 유지한다.
- 전체Play 첫요청은0개검색완료(d-final-play-zero-discovery-status.json)이며 PASS아님. 완료확인후 기존Editor RequestScriptReload로 검색상태를 갱신해 동일전체시험을 재요청한다. 중복실행/에디터추가 없음.
- d-protection-check.json 보호167개/허용asmdef4개/이동meta2개 동일, C#91·CodeMap91·누락0, 사용자저장 동일. DefaultSO의 이번허용변경은 GUID동일/기존값동일 검증 후 hash34B7E5D36EB3D023597FFEE99AA73945DC7C49408BD841962BB25FBA4DA8443B로 고정했다. C고정fixture 해시동일.
- 최신 diff --check FAIL(exit2): Unity Editor serializer가 빈 addActionId에 생성한 후행공백30개 및 이전B EOF 빈줄2개. 자산을손편집하거나직렬화형식을변경하지않으며 기능/의존성결함으로승격하지않는다. 형식 FOLLOW_UP과실제필수테스트PASS를구분한다.
- Android 최신 빌드 정확 대상: 기존 Editor BuildPipeline, target=Android, 현재 활성 씬 목록, profile 변경 없음, options 기본 DetailedBuildReport. 출력은 프로젝트 Temp/ra-d-android/DiceWarrior-RA-D.apk(검증 전용/ignored)이며 성공 시 staging artifacts/roguelike-architecture/Android/로 복사한다. 추가 설치/서명/배포·새 Editor·ProjectSettings 수정은 하지 않는다. dry_run과 실제 build/build_status를 구분한다.

## RA-D-01 / 수정1
- d-final-play-status.json 전체100중99 PASS/1 FAIL(196.79초). KoreanUiTests.CustomLabelsAndDescriptionsRemainVerbatimWhileDefaultChromeIsKorean의 names dictionary가 기존5개ID만 포함하여 실제6개정의를 순회할 때 ember_slash에서 KeyNotFoundException이 발생했다. 제품/UI 문자열 계산의 실패가 아닌 신규콘텐츠를 반영하지 않은 고정 fixture 누락이다.
- 해결 조건: 기존5개 사용자명과 모든 원문보존·한글·레이캐스트·전체상태·저장 bytes assertion을 유지하고 신규ID의 독립 사용자명 Ember Cut / 사용자 잔불을 추가한다. 직접대상 KoreanUiTests.cs/CodeMap을 D 허용 목록에 포함한다. 최초D1/1, 수정1/2, 총3/8. 새검토묶음/한도추가 없음.
- 실제 실패를 RED로 보존하고 수정 후 영향 검사·최신 전체 Edit/Play와 Android/보호 검증을 같은 수정1묶음에서 확인한다. 아직 D완료 아님.

- 수정1 최신 d-fix1-edit-status.json 228/228 PASS(40.89초). 반복 확인된 검색누락을 방지하기 위해 완료된 EditMode 뒤 RequestScriptReload, 전체Play 재실행 중. Android adb devices 재확인 결과 연결기기0: 실기기 터치/세로비율/SafeArea/중단복귀 NOT_RUN. 빌드 자체는 다음 실제 실행 대상이다.

## RA-B-02 / 최종 통합에서 발견 / B 수정2
- d-fix1-play-status.json 99/100 PASS, RunBoundaryFlowTests.SavedActionSurvivesPresentationFailureAndRefreshDoesNotExecuteItAgain의 원래 위치 완전동등 검사1개 FAIL(196.98초). RA-D-01 한국어 검사는 통과했다. 새 예산을 만들지 않고 오류후 연출복구의 소속 RA-B에 귀속한다: B 수정2/2, D 수정1/2, 전체4/8.
- b-arena-precision-probe.json: Unity RectTransform의 local(-.000014,59.0000153)→anchored(360,-441)→local(0,59) 왕복에서 작은 좌표가 사라짐을 실제 Editor 임시 객체로 재현했다. localPosition 직접 복원은 완전동등이다. 표시 숫자를 반올림하거나 허용오차를 넓혀 기존 assertion을 완화하지 않는다.
- 직접 대상: Features/Run/Presentation/CombatUI.cs와 Run/Tests/PlayMode/RunBoundaryFlowTests.cs 및 대응CodeMap. 기존 프리팹 컴포넌트 PlayFeedback에 고정 분수좌표 RectTransform을 연결하는 추가 회귀 RED를 먼저 확인한다. arena 연출은 anchored 좌표를 계속 쓰고, 종료·중단 복원에는 원래 localPosition을 보존해 왕복 정밀도 손실을 피한다. Scene/Prefab/GUID 변경 없음.
- 기존 전체상태·저장 횟수/bytes·실제 pointer·정확 anchored 위치 assertion을 유지하고 localPosition 완전동등 회귀도 추가한다. 영향 RunBoundaryFlow/CombatFeedback 및 최신 전체 Edit/Play/Android를 같은 B 수정2 묶음에 포함한다. 원래 B-02 실패원인이 남으면 B 추가예산 없이 BLOCKED다.

## RA-B 수정2 재검증 실패 / 한도 도달
- b-fix2-red-status.json: 실제 CombatUI.PlayFeedback 분수 localPosition 완전동등 회귀0/1 FAIL(.48초).
- 제품 수정: CombatUI가 연출 시작 localPosition을 보관하고 Impact 종료/ResetFeedback에서 직접 복원한다. b-fix2-flow-status.json: 3/4 PASS, 추가 분수좌표회귀1 FAIL(2.29초). 기존 SavedActionSurvivesPresentationFailureAndRefreshDoesNotExecuteItAgain은 원래 완전동등 assertion으로 PASS이나 신규 강한 회귀는 미해결이다. 허용오차/기대값/시험 삭제로 통과시키지 않는다.
- b-fix2-loaded-field-check.json 및 b-fix2-loaded-method-check.json에서 실제 로드된 CombatUI에 신규필드와 ResetFeedback의 해당 필드 참조가 있음을 확인했다. 오래된 소스가 실행됐다고 가정하지 않는다. 초기 eval 문법오류는 별도 증거이며 후속 읽기 체크가 정상 완료됐다.
- RA-B-02 미해결. B 최초1/1·수정2/2 한도 도달로 추가 코드 수정/재검증 순환을 중단한다. A1/2+B2/2+C0/2+D1/2=전체4/8. 다른 단위의 남은 예산을 B-02에 전용하지 않는다. RA-D-01 한국어 사전오류는 수정1 전체Play에서 통과했으며 D 콘텐츠/UI 자체는 검증되었으나 통합 완료는 보류한다.
- 현재 코드 변경은 미완료(B-02). 전체최신통합 PASS 아님. d-fix1-edit-status.json 228/228은 CombatUI 추가수정 전 트리, d-fix1-play-status.json99/100도 같은 이전트리이다. 최신부분검증 b-fix2-flow3/4를 과거전체PASS로 덮지 않는다. 동일실패를 이유/조치없이 반복하지 않는다.
- 독립 Android 최신 빌드는 dry_run valid=true 후 기존Editor에서 실행 중이다. 결과/원본보호 확인만 계속하며 B/D를 완료 처리하지 않는다. 다음 승인 필요 항목은 B-02 한정 추가수정·재검증 예산이다.

## 이전 중단 상태 / 2026-09-09 (2026-09-10 추가 승인으로 해소)

코드 변경 완료: **아니오**. 전체 검증 완료: **아니오**. RA-B-02의 분수 좌표 복원 회귀1개가 남아 있으며 B의 승인된 수정2/2를 소진했다. 추가 수정/시험 순환은 승인 대기다. A/C는 필수 실행을 통과했고 D 콘텐츠/가격은 구현·개별 검증되었지만 B 의존 통합 완료는 보류한다.

| 단위 | 구현/책임 | 현재 판정 | 최초/수정 |
|---|---|---|---|
| RA-A | 일반 새 시드·고정 주입, 탐험/전투 각각 굴림·결과 유지 | PASS | 1/1,1/2 |
| RA-B | 후보 상태→검증→저장→확정, 독립 표시복사, 오류후 UI 복구 | BLOCKED: B-02 최신회귀1 FAIL | 1/1,2/2 |
| RA-C | FateDice.Core noEngineReferences, 규칙/상태/표시·어댑터 분리 | PASS | 1/1,0/2 |
| RA-D | Damage/Block 조합, 잔불 베기1종 획득/추첨/태그/사용/복원, 등급가격 snapshot | 기능 구현, 통합 WAIT_VALIDATION | 1/1,1/2 |

누적 수정4/8(상한을 다른 단위에 전용하지 않음), 위임4/4. RA-A-01/RA-B-01/RA-D-01 해결, **RA-B-02 미해결**. 새로운 필수 Finding/검토 예산을 만들지 않았다.

실제 실행 구분:
- d-fix1-edit-status.json: Edit228/228 PASS40.89초. **최종 CombatUI 위치수정 전 트리**이며 현재 전체PASS로 재사용하지 않는다. C 고정273명령·46,656패 및 200만 실제 RNG 표본의340개고정비교 전부통과. 기대 fixture는 재생성하지 않았다.
- d-fix1-play-status.json: 같은이전트리 전체99/100 PASS,1 FAIL196.98초. 실제 한구간완주/패배/재시작, 한국어 회귀수정은 PASS; 이때 B-02 발생.
- b-fix2-red-status.json: 분수좌표 새회귀0/1 FAIL. b-fix2-flow-status.json: **현재코드 관련4개중3 PASS/1 FAIL**2.29초. 기존 실제버튼·저장·오류후 복구검사는 통과했지만 추가 localPosition 완전동등은 실패했다.
- d-android-build-status.json: **현재소스 Android ARM64 IL2CPP 빌드 Succeeded**, build_b9380325f29b,196.38초,오류0·경고982. APK 실제파일46,005,787bytes(약43.9MiB), manifest/classes.dex/arm64 libil2cpp·libunity·libmain 포함 확인. BuildReport totalSizeBytes는APK실제크기로 표시하지 않는다.
- APK: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/roguelike-architecture/Android/DiceWarrior-RA-D.apk, SHA256 5C95141039D329890C5C4D62F6CE1B4F649D5C2EF0457DDCA6CCCF24D04928FB. 빌드성공은 미해결UI회귀나실기기검증을대체하지않는다.
- 빌드경고: shader variant 성능/구성978건, DebugOccluder/DebugOcclusionTest stripping2건,진단symbol설정1건,RuntimePipelineConfig없음1건. Player의Pipeline은비활성이며기존Editor CLI연결과구분한다. 새설정/패키지/Shader정비는 FOLLOW_UP이다.
- Android 실제기기: adb devices0. 터치·세로비율·SafeArea·굴림/카드/보상 중 중단복귀 **NOT_RUN**. 추가설치/기기추정 없음.

보존 및 현재 diff:
- 빌드가 InputSystem_Actions를 임시 preloadedAssets에 넣고 메모리만빈목록으로원복하여 디스크1개변경이남았다. Editor조회목록0 확인, SaveAssetIfDirty는ProjectSettings를flush하지않았다. 복원후보가 시작보호SHA256 1140BB073C3AECC870725ADBA1EFFE633BE36CD20D4FE44FEDF7788DD59F01FA와완전일치하는경우만 빌드전원본bytes로복원했다. 다른dirty자산일괄저장 없음. d-settings-original-restore.json/보호검사에근거한다.
- 최종 d-protection-check.json 보호167개 mismatch0,허용asmdef4개정확일치,이동meta2개동일,사용자run.json hash동일. C#91/CodeMap91/역방향orphan0/모든프로젝트자산131개의meta누락0. 기존Scene/Prefab/GUID/AGENTS/GDD/설계/Goal/패키지/ProjectSettings보존.
- DefaultSO는허용된A시간설정과D신규카드/보상/가격배율만EditorAPI로반영. 기존값JSONprojection동일/기존GUID유지. 구형저장은snapshot자체만사용하고새카드/가격배율을소급주입하지않는다.
- HEAD83d82766795dfd5528a492f82c59d433ba556339,staged0,브랜치/commit/push/merge작업없음. 추적프로젝트코드/요약diff50files +764/-1005; 신설/이동파일과기존사용자untracked는final-git-status.txt에전수기록(과거참고commit으로reset안함).
- diff --check는Unity serializer의빈addActionId 후행공백30개와이전CombatFeedbackTests/CodeMap EOF빈줄2개로FAIL이다. 기능테스트PASS로숨기지않으며형식 FOLLOW_UP으로유지한다.

주요 변경 파일 책임:
- Features/Run/Runtime/ISeedSource.cs,FixedSeedSource.cs,SystemSeedSource.cs: 새시드공급과고정시험주입.
- Features/Run/Application/RunApplication.cs + Runtime/RunSession.cs: 순수명령소유/Runtime호환facade 및저장성공후확정.
- Features/Run/Domain/RunStateData.cs,CoreRunState.cs,RunStateCopy.cs,RunStateValidator.cs:순수상태·복사·후보검증.
- Features/Fate/Domain/RunRulesCatalog.cs,RulesCopy.cs + Configs/FateDiceConfig.cs:저장된규칙카탈로그와SO표시분리/명시복제.
- Features/Combat/Runtime/ActionEffects.cs,CombatRules.cs + Growth/Runtime/GrowthRules.cs: 효과명시조합·공유계산·중복없는AddAction.
- Features/Shop/Domain/ShopRules.cs + RunUIController.cs:입장시확정가격·표시·차감·재개연결.
- Features/Save/Runtime/IRunStore.cs,LocalRunStore.cs:로컬저장계약·구형시간/정확double/상점snapshot호환.
- Features/Run/Presentation/CombatUI.cs:연출위치복원 보완시도. B-02가남아검증완료아님.
- 변경C#1:1요약과직접관계/INDEX갱신. 범위외운영문서/계정경제/E단계없음.

다음: B-02만 추가수정·재검증1묶음 승인 후, 임시RectTransform의초기갱신/프레임/앵커재계산과실제CombatUI복원을분리해원인을확정한다. 기대값·정확동등검사·저장불변을낮추지않는다. 해결뒤해당영향+최신전체Edit/Play/Android를확인해야코드와통합완료를판정할수있다. 현재 추가수정안은승인되지않았고백그라운드계속작업을약속하지않는다.

## RA-B-02 추가 수정3 / 사용자 승인 2026-09-10
- 사용자가 B-02 한정 수정·재검증 1묶음 추가 요청에 `승인`으로 답했다. 기존 횟수를 유지하여 B 수정3/(기본2+추가1), 전체5/8로 기록한다. 다른 단위·위임 상한은 그대로이며 추가 위임은 하지 않는다.
- 현재 소스/어셈블리105개 SHA256이 이전 최종 기록과 전부 동일, HEAD83d82766795dfd5528a492f82c59d433ba556339/staged0. 원래 B-02 분수좌표 회귀1 FAIL을 이어받는다.
- 첫 조사: 임시 정밀 진단으로 PlayFeedback 진입·Impact 복원·ResetFeedback·테스트 종료 시 local/anchored/보존값을 round-trip 문자열로 관측한다. 임시 진단은 최종 코드에서 제거한다. 실제 Unity 프레임에 의한 변화와 제품 복원 오류를 분리한 후 최소 수정한다. 기존 exact assertion/파일 bytes/상태 oracle은 약화하지 않는다.
- 직접 수정 대상은 기존 CombatUI.cs/RunBoundaryFlowTests.cs와 각 CodeMap, 이 PLAN/REPORT뿐이다. Scene/Prefab/패키지/설계 원문/Git 변경 없음. 영향 검사 뒤 최신 전체 Edit/Play, Android build와 보호 검사로 닫는다. 추가 실패시 승인 묶음 안의 원인 기반 TDD만 허용하며 새 검토 예산은 만들지 않는다.
### B-02 수정3 원인 확정 및 조치
- b-fix3-precision-trace.txt: 테스트 예상 local(-.000014,59.0000153,0)이 PlayFeedback 진입 전에 이미(0,59,0)이다. 저장된 원위치 및 두 복원 지점/종료는 모두(0,59,0)로 일치한다.
- b-fix3-unanimated-probe.json: 아무 연출도 하지 않은 임시 RectTransform에 ForceUpdateRectTransforms만 호출해 동일 손실을 재현했다. 따라서 수정2 후 잔여 FAIL의 원인은 coroutine 시작 이전 초기 layout 값으로 만든 fixture oracle이다. 실제 오류복구 테스트의 완전동등 assertion은 유지한다.
- 정상 종료 회귀는 초기 anchor layout을 끝내고 정확히(.125,59.125,0)인 분수좌표를 먼저 독립 assertion으로 고정한 뒤 원래 local/anchored exact 비교를 수행한다. 허용오차·비교 제거 없음.
- 원래 아주 작은(-.000014,59.000015) 값도 별도 중단 회귀에 유지한다. 같은 프레임에서 실제 PlayFeedback/Impact 한 단계를 진행하고 ResetFeedback을 호출해 Unity 초기갱신이 끼어들기 전의 preimage를 완전동등 비교한다. 실제 흔들림 발생/연출 상태도 확인한다. 임시 제품 진단은 제거하고 수정2의 localPosition 직접복원을 유지한다.
- 원인 추가확정: b-fix3-getter-probe.json에서 anchoredPosition **getter 자체**가 local(-.000014,59.0000153)을(0,59)로 재계산함을 확인했다. 명시 ForceUpdate/다음프레임 없이도 발생한다. 따라서 제품의 localPosition 캡처를 anchoredPosition 조회보다 먼저 해야 한다. 중단 회귀의 anchored 기대값은 고정 fixture에서 독립 상수(360,-441)로 지정해 테스트의 관측 자체가 preimage를 변경하지 않게 한다. 작은 local값과 모든 exact assertion은 유지한다. 먼저 제품 순서 변경 전 이 강화된 중단 회귀의 RED를 확인한다.
## 최종 실행 결과 / 2026-09-10
- B-02 추가 수정3 완료: local 캡처를 anchored getter보다 앞세움. 중단 RED0/1→관련 GREEN5/5, 최신 전체 Edit228/228·Play102/102·Android 빌드 PASS. 원본 증거/요구별 수용표/보호해시는 REPORT 마지막 절 참조.
- 코드 및 필수 로컬 검증 완료. 실기기 NOT_RUN(연결0), 형식/빌드경고/특수주사위 밸런스 FOLLOW_UP. B3/(기본2+추가1), A1/C0/D1, 합계5/8·위임4/4. 추가 코드 변경/검토를 이어가지 않는다.

## Git 커밋 승인 / 2026-09-10

- 사용자의 후속 요청 `좋아 깃 커밋 먼저 진행해줘`에 따라 이 구현의 staging/로컬 commit을 승인 범위에 추가한다. 앞 절의 Git 작업 없음은 구현 완료 당시 이력이며, push/merge/branch 변경은 이번 요청에 포함하지 않는다.
- 정확한 대상은 구현 코드·asmdef/asmref·기존 허용 DefaultFateDice.asset·신규 meta/고정 테스트 fixture, 대응 CodeMap/INDEX, 이 PLAN/REPORT의149개 파일이다. 기존부터 untracked였던 설계/Goal 원문2개와 ProjectSettings/Packages 개인별 설정2개는 제외한다. APK/실제 사용자 저장/임시 검증 로그는 포함하지 않는다.
- 커밋 사전 확인: HEAD83d82766795dfd5528a492f82c59d433ba556339/main, 기존 staged0. 소스/어셈블리105개가 마지막 전체 검증 해시와 동일하며 보호된 프로젝트 파일167개·SO·고정Replay fixture와 GUID/.meta도 동일하다. 완료된 실행 증거는 Edit228/228, Play102/102, Android Succeeded(오류0/경고980); 커밋만을 위해 Unity 시험/빌드를 반복하지 않았다. 실기기 NOT_RUN은 그대로다.
- 실제 LocalLow 저장 파일은 이전 검증 뒤 갱신되어 과거 save 해시와 다르다. 현재 저장을 읽기만 했으며 과거 값으로 복원하지 않는다. 프로젝트 보호파일 불일치와 구분하고 Git 대상에도 포함하지 않는다. 과거 해시를 전제하는 보호 스크립트의 이번 실행 FAIL은 이 차이이며 원본 결과를 commit-preflight-protection.json에 보존했다.
- 커밋 전 exact path manifest·staged 파일 목록·내용 동일성 및 diff --check를 확인한다. 기존 serializer 후행공백/EOF 형식 FOLLOW_UP을 소스 수정 없이 보존한다. 커밋 메시지에 구현 범위와 실제 검증 시점/한계를 기록한다.