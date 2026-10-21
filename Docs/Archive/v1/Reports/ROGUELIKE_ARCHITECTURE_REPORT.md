# ROGUELIKE_ARCHITECTURE_REPORT

2026-09-10 · IMPLEMENT · RA-A/B/C/D PASS. 코드 및 필수 로컬 검증 완료. Android 빌드 PASS, 실기기 NOT_RUN. 최신 판정은 마지막 2026-09-10 절이다.

시작 HEAD 83d82766795dfd5528a492f82c59d433ba556339/main. 기존 RA PLAN/REPORT 없음 확인 후 지정 두 파일만 생성. 실제 Goal은 Docs/Dice_Warrior_Codex_Implementation_Goal_v0.2.md에 있으며 Docs/Goals에 복제하지 않았다. 이전 GitHub 공개 확인은 이 Goal 구현 진척에 포함하지 않는다.

## 구현/직접 변경
- 일반 새 여정 ISeedSource/SystemSeedSource, 작업실/테스트 FixedSeedSource. GameApplication/Controller 초기화 주입, Seed setter 고정 호환·0 거절. getter/렌더/이어하기의 공급자 비소비.
- 탐험/전투 각각 굴림/결과 유지 설정(.65/.9초 기본). 새 0 설정은 실제 wait 생략. 저장→면 정착→결과 유지→카드 표시, 재굴림 같은 경계.
- DefaultFateDice.asset만 Editor API로 두 그룹 6행 추가; 규칙 JSON 동일 검사. Scene/Prefab 재생성 없음.
- 구형 JSON 시간 필드 누락은 LocalRunStore의 실제 config.presentation JSON 경로에서 판별. 설치된 Newtonsoft.Json 3.2.2 사용; 패키지/lock/asmdef 미변경. schema1/체크섬/원본 파일/현재 SO 독립 유지.
- 기존 GUI/씬/한국어의 시드33 재연을 명시 주입. 승리·보상·카드·raycast assertion 유지. CombatFeedback authored 검사도 실제 SO의 두 hold 설정 경계로 이동.
- 신규 공급자3/테스트2 포함 현재 C#73개와 1:1 CodeMap73개, 직접 관계/INDEX 동기화.

## 실제 검증
증거 루트: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/roguelike-architecture/

| 실행 | 결과 | 증거 |
|---|---|---|
| 변경 전 전체 EditMode | PASS 128/128, 14.72초 | a-baseline-edit-status.json |
| 변경 전 실제 GUI 완주/재시작 | PASS 1/1, 39.63초 | a-baseline-play-status.json |
| 초기 계약 RED | FAIL 0/3: 공급자/시간 그룹 부재, 0 setter 미거절 | a-red-status.json |
| 초기 계약 구현 후 | PASS 3/3, .35초 | a-green-initial-status.json |
| 확장 계약 첫 실행 | FAIL 29/30: 구형1.25초→.65 복원 | a-contract-edit-status.json |
| 첫 호환 보완/전체 | FAIL 157/158: 동일 구형 시간 실패 | a-full-edit-status.json |
| 실제 JSON 경로 보완 후 전체 | PASS 158/158, 16.84초 | a-full-edit2-status.json |
| 최초 검토 전체 PlayMode | PASS 95/95, 194.22초 | a-full-play-status.json |

구형 시간 실패는 최초 검토 전 TDD/증분 검증의 동일 원인이다. 초기값 없는 DTO에도 Unity가 inline 객체를 생성하므로 DTO 누락 판별을 폐기하고 실제 JSON 경로를 사용했다. 과거 FAIL을 유지하며 현재 통과 증거와 구분한다. 이 개발 검증은 최초 검토 이후 수정 예산에 집계하지 않는다.

## 보존/환경
- 기존 Editor PID64968, Unity6000.6.0f1/Pipeline ready. status 초기 미검색은 기존 Editor 종료가 아니었고 추가 실행/잠금 삭제 없음. reload 후 포트가 바뀔 수 있어 project-path를 항상 지정.
- 실제 사용자 저장 SHA256 0B1FD1C9EF1FB971D0229C3CAB2DCBBBB122F17741A8B267F8FC5052101EA71C. staging 백업 및 격리 시험 경로 사용. 중간 저장 bytes 동일.
- 기존 meta/AGENTS/GDD/설계/Goal/manifest/lock/ProjectSettings/URP 보호 해시 변경 없음, 최종 재확인 예정.
- AndroidPlayer/SDK/NDK/OpenJDK 설치 확인. 최신 Android 빌드 NOT_RUN(미실행), adb devices 목록 비어 있음; 실기기 NOT_RUN(연결 기기 없음). 환경 부재로 가정하지 않으며 Editor 검증으로 대신하지 않음.
- 사용자 기존 변경 보존. Git staging/commit/push/merge/브랜치 변경 없음.

## 고정 예산/남은 일
| 단위 | 상태 | 최초 검토 | 수정·재검증 | 필수 Finding |
|---|---|---|---|---|
| RA-A | PASS | 1/1 | 1/2 | RA-A-01 해결 |
| RA-B | PASS | 1/1 | 3/(기본2+추가1) | RA-B-01/02 해결 |
| RA-C | PASS | 1/1 | 0/2 | 없음 |
| RA-D | PASS | 1/1 | 1/2 | RA-D-01 해결 |

수정 합계5/8(B-02 추가1회 사용자 승인). 보조 위임4/4(A/B 테스트 및 해당 최초 검토, C 분리 준비, D 독립 분포 준비). root만 실제 프로젝트 쓰기와 Unity 실행. 최종 통합 별도 예산 없음.
현재 남은 필수 코드/Editor 검증은 없다. Android 실기기는 기기 부재로 NOT_RUN이다. 아래의 이전 실패/중단 기록은 이력이며 마지막 2026-09-10 판정이 최신이다.
## RA-A-01 (최초 검토 당시 Finding / 아래 수정1에서 해결)
유효한 null 그룹과 legacy1.25에서 DeepCopy/New가 .65로 바뀌는 문제가 a-review-timing-copy-probe.json의 live Editor probe로 확인됐다. 해당 phase만 실효 legacy값으로 복제하고 반대 phase의 명시0과 원본 독립성을 보존한다. Snapshot/New를 포함하는 회귀를 추가한다. 전체 Play95 통과와 이 별도 결함을 구분하며 수정 후 검증 전 RA-A 완료로 표시하지 않는다.

## RA-A 수정1 완료 / RA-B 시작
- a-fix1-red-status.json: null 두 phase 복제 회귀 FAIL0/2. a-fix1-edit-status.json: 전체160/160 PASS(13.16초). a-fix1-play-status.json: 전체95/95 PASS(193.94초).
- RA-A-01 해결: GameConfigData.DeepCopy에서 누락 그룹의 실효 legacy 시간만 독립 복제. Snapshot/New와 반대 phase 명시0/원본 null 보존 회귀 통과. 수정1/2, 합계1/8. 추가 미해결 필수 Finding 없음.
- a-protection-check.json: 보호171개 mismatch0, 사용자 저장 동일. C#73/CodeMap73의 1:1 존재와 역방향 orphan0, meta누락0. 기존 Scene/Prefab/.meta 유지. scoped git diff --check PASS, staged0.
- 후속: B 계획 구체화 완료 후 상태/저장 구현 시작. B 신규 테스트와 기존 fixture 이전은 보조 위임B-1(누적2/4), runtime/실제 프로젝트/Unity는 root 소유. Android 빌드와 연결 기기 없는 실기기는 아직 별도 미실행이다.

## RA-B 구현 및 최초 검토
- IRunStore와 깊은 표시 복사, 직렬 명령/화면 token, 시간 배치 저장, 후보 순수 검증→저장→확정, 저장 이후 표시 오류 복구를 구현했다. Scene/Prefab/SO 변경 없음.
- b-red-status.json: 소유권 RED FAIL0/1 → b-green-initial-status.json PASS1/1.
- b-edit-status.json: 전체 Edit181/181 PASS(14.62초). b-flow-status.json: 실제 UI 경계3/3 PASS(2.03초). 전체 Play98 및 최초 코드 검토 진행 중이며 B 완료 전이다.
- 전체 Play 시작 시 sandbox에서 Pipeline 탐색 실패. 동일 기존 Editor를 승인된 실행 권한으로 확인하여 정상 시작했다. 새 Editor/잠금 삭제/설정 변경 없음.
- B 최초1/1, 수정0/2, 수정 합계2/8. C 분리 조사 위임 포함 누적3/4.


## RA-B 최초 검토 결과 / 수정1
- 첫 전체 실행은 검색0개 완료(b-play-zero-discovery-status.json), PASS 아님. ScriptReload 후 재실행 b-play2-status.json: 91/98 PASS,7 FAIL(191.32초). 최초 코드 검토의 추가 Finding 없음.
- RA-B-01: CombatFeedbackTests.CombatSave의4개 직접 State 쓰기 때문에 준비 피해/방어 oracle7개 실패. prepared snapshot→new RunSession으로 fixture만 이전한다. 기대수치/assertion/런타임 규칙을 그대로 유지. B 수정1/2, 총2/8. C 구현은 B 통과 전 대기.
- b-protection-check.json 보호171개 mismatch0/사용자 저장 동일/C#78·CodeMap78·누락0. scoped diff --check PASS.


- b-fix1-edit-status.json: Edit181/181 PASS(14.61초). b-fix1-play-status.json 전체 Play 진행 중. b-fix1-protection-check.json 보호 동일. 보조 신규 생성은 thread limit로 실패하여 완료된 A 프로세스를 D-1 별개위임에 재사용; 누적4/4이며 추가위임 없음.


## RA-B 종료 / RA-C 착수
- b-fix1-edit-status.json 181/181 PASS(14.61초), b-fix1-play-status.json 98/98 PASS(196.54초). CombatFeedback7실패 모두동일기대값통과, RA-B-01해결. B 최초1/1 수정1/2 총2/8.
- c-baseline-capture.json: RA-C 변경 전 코드로 7시나리오273명령 고정 재연 기준 확보. 이후 같은초기JSON/선택ID/인자/시간/전체상태hash로 비교하며 기준을새코드에맞춰갱신하지않는다. C 구현은 B PASS확인후시작.


## RA-C 개발 검증
- FateDice.Core 실제 noEngineReferences:true/명시참조없음. Domain/Application과 Runtime facade/표시 설정 분리. 기존 규칙5개는 타입/Rules접근 기계치환이며 상태52필드/정의18개 명시복제 대응을 확인했다. 기존 스크립트2개 GUID동일 이동, C#86/CodeMap86 대응.
- c-red-status.json:14/15PASS(7시나리오×연속/매명령복원),Core부재1FAIL 의도RED(7.14초). c-core-status.json:19/19PASS(6.64초). c-edit-status.json:199/200PASS, 시간 JSON파서1FAIL(20.09초).
- c-duration-red-status.json:실제 파일 저장에서 .01×100 시간 double값이1ULP 변하는회귀0/1FAIL 재현(.11초). 현재·최근결과playedSeconds를 저장된JSON숫자에서 정확읽도록 LocalRunStore를보완하고 ProbeStore는명시복제로변경했다. 기존전체상태동등/assertion유지. C 최초검토전개발이며최초0/1·수정0/2/전체2/8 유지.
- c-protection-check.json:비수정보호167개동일, 승인asmdef4개는정확staging일치, 이동meta동일, 사용자저장과DefaultFateDice.asset 및고정재연fixture해시동일. Scene/Prefab/패키지/ProjectSettings 미변경. 전체Edit재실행중, Play/최초검토는아직미실행.


- c-edit2-status.json: 전체201/201 PASS(20.35초). 시간bit/전체상태 동일성 보완 후통과. C 최초1/1(동일C-1 Domain검토+root Application/adapter/전체Play검증)시작,수정0/2·총2/8 유지.


- 최초 C root Application/facade/저장 경계 검토: 추가 필수 Finding 없음. 최신 scoped diff --check는 CombatFeedbackTests.cs 및 CodeMap의 EOF 빈 줄2개 형식 경고(FAIL)를 반환했다. FOLLOW_UP 형식 정리이며 기능 회귀/완료 차단으로 승격하지 않는다. 이전B시점diff PASS와현재출력을구분한다. C 보호167개 및허용asmdef4개/이동GUID2개/원본SO/사용자저장/고정fixture 동일 재확인.


## RA-C 완료 / RA-D 준비
- c-edit2-status.json 전체201/201PASS20.35초. c-play-status.json 전체98/98PASS195.82초. 최초C코드검토필수Finding없음. C최초1/1수정0/2, 총2/8. D 시험수치·호환정책·정확파일·검증계약을PLAN에기록했다.
- D-1 독립수학분석은실제Python 전수계산완료이며게임RNG표본은미실행이다. 기본/Ember1/2/6개 운명력기대값3.046424897/3.030992798/3.054288599/3.520814164,1개교체감소5/324. 입력pin/계산/효율은agent-d산출물이며수치를게임강화보장으로주장하지않는다.


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

- 수정1 최신 d-fix1-edit-status.json 228/228 PASS(40.89초). 200만표본/340비교 전부통과 및 d-fix1-distribution-actual-{0,1,2,6}.json 수집. 보호167개/91요약/저장해시동일. 전체Play 수정1 실행중. Android 연결기기0 재확인(2026-09-09); 실기기는 NOT_RUN이며 최신 빌드와 구분한다.

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
## 최종 수용 판정 / 2026-09-10 / B-02 추가 승인분 완료

**코드 변경 및 필수 로컬 검증 완료. RA-A/B/C/D PASS.** B-02 해결. Android 최신 ARM64 IL2CPP 빌드 PASS이며, 연결 기기가 없어 실기기 터치·세로 비율·SafeArea·중단/복귀는 **NOT_RUN**이다. 이 플랫폼 항목까지 전체 검증 완료로 표현하지 않는다. Goal 6절의 기기 부재 분리 기준을 적용한다.

### 추가 수정의 원인과 강도
- B-02는 anchoredPosition getter가 RectTransform layout을 갱신하면서 작은 local 좌표를 반올림한 뒤 원위치가 캡처되는 문제다. 무연출 getter probe에서 (-.000014,59.0000153)→(0,59)를 직접 재현했다. CombatUI는 localPosition을 먼저 보관하고 그 뒤 anchored 흔들림 기준을 읽는다. 종료/중단은 그 원래 localPosition으로 복원한다. 실제 제품 diff는 이전 수정2 대비 캡처 순서 변경과 설명 주석1개다.
- 기존 정상 종료 회귀는 layout이 끝난 정확한(.125,59.125,0) 분수 좌표를 독립 assertion으로 확인한 후 검사한다. 원래 tiny fractional 값은 별도 같은 프레임의 실제 Impact→ResetFeedback 회귀에 그대로 유지한다. 관찰 자체가 원위치를 바꾸지 않도록 anchored 기대값(360,-441)은 고정 fixture에서 독립 상수로 지정한다. exact 비교, 실제 pointer, 상태/RNG/저장 횟수/파일 bytes 검사는 완화하지 않았다.
- b-fix3-diagnostic-status.json FAIL0/1, b-fix3-flow-status.json FAIL4/5와 진단 trace/probe를 보존했다. b-fix3-red-status.json은 제품 수정 전 실제 중단 복원 FAIL0/1(.41초), b-fix3-green-status.json은 수정 후5/5 PASS(2.46초)다. 임시 제품 진단 메시지는 제거했다.

### 최신 트리의 실제 실행

| 필수 범위 | 최신 결과 | 원본 증거 |
|---|---|---|
| 전체 EditMode | 228/228 PASS, 40.89초, skipped0/inconclusive0 | b-fix3-edit-status.json |
| 전체 PlayMode | 102/102 PASS, 197.9초, skipped0/inconclusive0 | b-fix3-play-status.json |
| Android ARM64 IL2CPP | Succeeded, 112.14초, errors0/warnings980 | b-fix3-android-build-status.json, buildId build_f636c82bce3f |
| 실기기 | adb devices0 → NOT_RUN | b-fix3-android-devices.txt |

최신 소스/asmdef/asmref105개 해시는 b-fix3-code-hashes.json에 고정했고, 위 실행 후 동일성을 재검사했다. 전체 검증 뒤 제품·시험 코드 변경 없음. 과거 PASS나 수정 전 결과를 최종 트리의 증거로 재사용하지 않는다.

APK: C:\Users\coli4\OneDrive\문서\ChatGPT\Dice_Warrior\artifacts\roguelike-architecture\Android\DiceWarrior-RA-B3.apk, 45390567bytes, SHA256 081285635652F49EE2959D5BA74D74E5BD980CF375822FA56F07FE9589288DD2. APK ZIP의 manifest/classes.dex 및 arm64-v8a의 libil2cpp/libunity/libmain을 확인했다. BuildReport totalSizeBytes는 APK 파일 크기로 사용하지 않는다. Unity shader/diagnostic/RuntimePipelineConfig 관련 경고는 b-fix3-android-warning-summary.json에 분리하며 추가 설정/패키지 변경은 하지 않았다.

### 요구 사항별 근거와 판정

| 요구/불변식 | 실제 코드·검사 근거 | 판정 |
|---|---|---|
| RA-A: 일반 새시드/고정 주입/0 처리/이어하기 비소비 | SystemSeedSource, FixedSeedSource, GameApplication/Controller; SeedAndPresentationTests32 및 SeedPresentationFlowTests의 공급자 호출·고정·0 실패·이어하기 | PASS |
| RA-A: 탐험/전투 각 굴림/hold와0, RNG/결과 불변 | 표시 그룹·legacy 복원, 실제 각 phase roll/reroll 시간/0/한쪽0 검사 및 전체명령열 동일성 | PASS |
| RA-B: 독립 스냅샷/단일명령/시간배치/중복거절 | RunApplication.Commit의 후보→검증→저장복사→상태교체, RunSession facade; RunBoundaryTests21, CoreBoundaryTests | PASS |
| RA-B: 저장실패 불변·저장후 UI 복구 | 실제 LocalRunStore 실패/AfterSave 표시장애 및 pointer retry; RunBoundaryFlowTests5의 전체상태·파일·정확위치 검사 | PASS |
| RA-C: 순수 Core/무순환·명시복사 | 실제 FateDice.Core compiled references/noEngineReferences 검사, 값 전체/가변 별칭/null·빈DTO 확인, CoreBoundaryTests6 | PASS |
| RA-C: 기존 결과/구형저장·ID·RNG | 고정7시나리오273명령의 연속/매명령 디스크복원 CoreReplayTests14, RuleTests32의46,656패, SaveTests32 | PASS |
| RA-D: 효과조합·기존계산·미지원거절 | Damage/Block fixed handlers와 공유 Evaluate, 독립30damage/23block oracle·반격/태그/invalid 정의 검사 | PASS |
| RA-D: 정확히1종 런 획득→추첨→태그→사용→복원 | 실제 SO6개 중 신규 ember_slash1개; ContentExtensionTests22 및 실제 보상/장착/굴림/카드 pointer 흐름 | PASS |
| RA-D: 중복·새런/원본비오염·구형필드 | 독립 소유/64draw 동일RNG·원본JSON/new run 검사, 구형schema1에서 새카드/규칙 주입 없음 | PASS |
| RA-D: 등급가격 입장확정·표시/차감/재개 | ShopRules offers snapshot, 모든5등급 exact 가격·실패원자성·구형저장7gold 및 실제 Rare상점 표시13/15/20/23·구매/재개 | PASS |
| 분포/자원 효율 | 고정 독립 수학fixture와 실제게임RNG200만회(기본/Ember1/2/6),340비교0FAIL; 비용과 미검증 최적전략/승률 구분 | PASS |
| 기존 한구간·패배/재시작·모바일 Editor UI | 전체 Play의 실제 uGUI raycast/완주/패배/재시작·한글·씬·연출 회귀. 물리 터치와 구분 | PASS |
| 사용자자료/GUID/문서/Git 보호 | b-fix3-protection-check.json 보호167동일, 허용asmdef4·이동meta2동일, 소스91/CodeMap91/누락0, 사용자save hash동일; HEAD/staged불변 | PASS |

의도적 변경은 일반 새 여정의 시드 공급, 표시 시간 조절, treasure_0의 잔불 베기 추가보상1종, 등급 상점배율[1,1.1,1.25,1.5,1.75]뿐이다. 기존 카드 수치/태그/일반재굴림/운명 상한/휴식훈련/한구간 규칙은 유지한다. A~C 고정재연의 새필드 legacy projection은 독립 assertion을 거쳐서만 수행하며 원래 fixture bytes는 불변이다. 특수주사위1개가 평균운명력을 낮출 수 있다는 결과(-5/324)는 FOLLOW_UP 밸런스로 남긴다.

### 보호·문서·현재 diff·예산
- 추가 수정 대상 CombatUI.cs, RunBoundaryFlowTests.cs의 CodeMap과 직접 INDEX만 동기화했다. 새 운영문서/AGENTS/설계/GDD/Goal 변경 없음. 모든 프로젝트 소유 non-meta 자산131개의 meta가 존재한다.
- 이번 빌드의 preloadedAssets 임시 저장은 b-fix3-settings-restore.json에 기록한다. ProjectSettings 최종 bytes는 시작보호hash 1140BB073C3AECC870725ADBA1EFFE633BE36CD20D4FE44FEDF7788DD59F01FA와 일치하고 사용자 save/SO/ReplayBaseline 보호해시도 동일하다. 다른 dirty 자산을 일괄 저장하지 않는다.
- 이 빌드의 시작04:16:13/14 UTC에 Mobile_RPAsset 및 UniversalRenderPipelineGlobalSettings도 URP의 사전필터/stripped runtime cache를 저장했다. 설치된 ShaderBuildPreprocessor의 UpdateShaderKeywordPrefiltering→SetDirty→SaveAssetIfDirty 코드와 정확한 변경 필드/쓰기 시각을 확인했다. 그 생성 필드 및 runtime list3개 제외 차이만 있는지 검사하고 이전 clean HEAD blob의 정확 bytes로 복원·백업했으며, Editor API로 해당2개만 다시 import했다. 최종 Assets/Settings와 ProjectSettings diff는0이다. 게임용 그래픽 설정을 새로 설계하거나 빌드 산출물을 수정한 것이 아니다.
- HEAD 83d82766795dfd5528a492f82c59d433ba556339, staged0, status entries153(tracked50/untracked103). Git staging/commit/push/merge/branch 변경 없음. 전수 status 및 tracked diff-stat은 b-fix3-git-status.txt/b-fix3-diff-stat.txt. 기존 사용자 변경을 포함한 status를 구현 산출물로 모두 주장하지 않는다.
- diff --check는 앞서 남긴 Unity serializer 빈 addActionId 후행공백30개와 이전 CombatFeedbackTests/CodeMap EOF빈줄2개의 형식 FOLLOW_UP으로 FAIL이다. 실제 기능검증 PASS와 별도 기록한다. B-02 필수 Finding은 해결되었으며 미해결 필수 Finding은 없다.
- 최초 검토 A/B/C/D 각각1/1. 수정 A1/2, B3/(기본2+사용자추가1), C0/2, D1/2; 합계5/8. 위임4/4, 추가 위임/재귀 없음. 최종 통합은 같은 B-02 승인 묶음에 포함했으며 새 예산으로 세지 않았다.
- RA-A~D 로컬 구현 목표는 종료한다. 실기기 검증, shader/진단 경고 정리와 특수주사위 밸런스는 명시된 후속 항목이다. 실기기 부재를 코드 실패로 취급하거나 반대로 기기 검증을 PASS로 표시하지 않는다.
## Git 커밋 승인 / 2026-09-10

- 사용자의 후속 요청 `좋아 깃 커밋 먼저 진행해줘`에 따라 이 구현의 staging/로컬 commit을 승인 범위에 추가한다. 앞 절의 Git 작업 없음은 구현 완료 당시 이력이며, push/merge/branch 변경은 이번 요청에 포함하지 않는다.
- 정확한 대상은 구현 코드·asmdef/asmref·기존 허용 DefaultFateDice.asset·신규 meta/고정 테스트 fixture, 대응 CodeMap/INDEX, 이 PLAN/REPORT의149개 파일이다. 기존부터 untracked였던 설계/Goal 원문2개와 ProjectSettings/Packages 개인별 설정2개는 제외한다. APK/실제 사용자 저장/임시 검증 로그는 포함하지 않는다.
- 커밋 사전 확인: HEAD83d82766795dfd5528a492f82c59d433ba556339/main, 기존 staged0. 소스/어셈블리105개가 마지막 전체 검증 해시와 동일하며 보호된 프로젝트 파일167개·SO·고정Replay fixture와 GUID/.meta도 동일하다. 완료된 실행 증거는 Edit228/228, Play102/102, Android Succeeded(오류0/경고980); 커밋만을 위해 Unity 시험/빌드를 반복하지 않았다. 실기기 NOT_RUN은 그대로다.
- 실제 LocalLow 저장 파일은 이전 검증 뒤 갱신되어 과거 save 해시와 다르다. 현재 저장을 읽기만 했으며 과거 값으로 복원하지 않는다. 프로젝트 보호파일 불일치와 구분하고 Git 대상에도 포함하지 않는다. 과거 해시를 전제하는 보호 스크립트의 이번 실행 FAIL은 이 차이이며 원본 결과를 commit-preflight-protection.json에 보존했다.
- 커밋 전 exact path manifest·staged 파일 목록·내용 동일성 및 diff --check를 확인한다. 기존 serializer 후행공백/EOF 형식 FOLLOW_UP을 소스 수정 없이 보존한다. 커밋 메시지에 구현 범위와 실제 검증 시점/한계를 기록한다.
- staging 실측:149개 exact 경로와 working-tree→Git 정규화 blob149개 모두 일치. staged diff는149files +8993/-1005(이 확인 문장 추가 전). diff --check는83건(허용 SO의 빈 필드 후행공백30, 신규 Unity meta의 빈 필드 후행공백51, 기존 C#/CodeMap EOF빈줄 각1)으로 FAIL이며 기능 검증과 별도인 형식 FOLLOW_UP이다. 제외4개는 모두 untracked 상태로 유지한다.
