# FATE_DICE_PROTOTYPE_PLAN

상태: COMPLETE / 추가 UI 계약 f91fba13 필수 확인1~9 완료 (2026-09-08). 이전 기본 루프 M1~M5 완료 증거와 수정 횟수 유지. 기준: 사용자 Goal(c6fb0644, 2026-09-07), AGENTS v0.5, Docs/Fate_Dice_GDD_v0.1.md. 추가 운영 문서는 사전 조건이 아니다.

## 목표와 보호
실제 Game View에서 새 런→가지 탐험→5유형 사건/전투/성장→일반 사건 10회→보스→결과→재시작, 로컬 저장/이어하기를 구현한다. AC-01~09를 모두 실제 검증한다. Android는 환경에 따라 별도 검증 상태, Firebase/영구 계정/출시 품질은 후속 범위다.

기준 경로 D:/UnityProject/Dice_Warrior, main, HEAD 433edddc872433ead6db52a872ea0fcac659d92e, Unity 6000.6.0f1. uGUI 2.6.0, InputSystem 1.20.0, TestFramework 1.8.0, Pipeline 0.6.0-exp.1, CLI 1.0.0-beta.8. 기존 게임 코드/asmdef/테스트 없음. Scene은 Assets/Scenes/SampleScene.unity. 실행은 연결된 Editor만 사용한다.
기존 변경: 삭제 Assets/Editor.meta, Assets/Editor/HubForceResolve.cs(.meta); 수정 Assets/TutorialInfo/Icons/URP.png, Packages/manifest.json, Packages/packages-lock.json, ProjectSettings/ProjectSettings.asset, ProjectSettings/QualitySettings.asset; untracked .agents/, AGENTS.md, Docs/, ProjectSettings/Packages/. 전부 보존. Packages/ProjectSettings/기존 자산/GDD/AGENTS 수정, 외부 설치, commit/push/merge는 범위 밖이다.

## 고정 마일스톤과 검증 예산
| 단계 | 범위와 필수 수용 기준 | 의존성 | 상태 | 최초 검토 | 수정/재검증 |
|---|---|---|---|---|---|
| M1 | 유효 SO/스냅샷·런 상태 계약, RNG/6D6/10패/독립 등급·유형 보장, 전용 Scene/기본 화면. 컴파일·46,656 경우와 경계·연결 확인 | 없음 | 완료 | 1/1 | 2/2 |
| M2 | 예고→굴림→실제 행동 선택→플레이어 승패→생존 적 행동→승패 GUI. 방어 만료·빈 등급 수치 보정 | M1 | 완료 | 1/1 | 0/2 |
| M3 | 가지 탐험/미리보기, 5유형 실제 효과, 정확히 사건당+1, 보스/결과/재시작 | M2 | 완료 | 1/1 | 1/2 |
| M4 | XP/레벨/장비 교체·포기/ANY ALL NONE/획득 재굴림/주사위 교체를 실제 루프에 연결 | M3 | 완료 | 1/1 | 0/2 |
| M5 | 저장 경계/손상 보존, 실제 GUI 완주·패배·2세로비, SO3종 변경/기존 런 스냅샷, CodeMap/보고 | M4 | 완료 | 1/1 | 1/2 |

의도한 TDD 최초 실패와 결함 수정을 구별한다. 각 최초 검토는 한 묶음, 이후 원인별 수정 묶음+지정 재검증 최대2회(전체10). 이전 결함은 원래 단계 예산을 유지한다. 필수 Finding에 ID/위치/근거/영향/해결 조건을 아래 누적한다. 환경 오류도 원인을 확인하고 반복한다. 한도 뒤 필수 결함은 차단 보고한다.

## 담당과 유한 위임
메인: 공통 SO/DTO, RunSession, GUI, Scene/.asset, 색인/PLAN/REPORT, Unity 실행과 최종 통합. 동시 최대4명(메인 포함), 전체 보조 위임 최대3건, 재귀 위임 금지. A: GDD 주사위 패 독립 오라클/검수(읽기 전용). B: DiceRules/FateCardRules와 해당 RuleTests+대응 요약(계약 전달 뒤 전용 파일). C: LocalRunStore와 SaveTests+대응 요약(계약 전달 뒤 전용 파일). 각 쓰기 파일은 한 작성자만 소유한다. Unity/자산 저장/테스트는 메인만 순차 실행한다. 보조 결과를 메인이 실제 소스와 실행으로 확인한다. 보조별 별도 검토 예산 없음.

## 임시 결정 (채택 / 이유 / 수정 위치 / 적용 시점)
- 일반 사건10, 카드3, 앞길2단계, 한 구간 보스1 / 요청 시작값 / DefaultFateDice.asset의 world / 새 런.
- 패 우선순위는 Pair < TwoPairs < Triple < Straight < FullHouse < ThreePairs < FourKind < FullStraight < FiveKind < SixKind, 운명력은 별도 필드 / 전통적 묶음 우선의 시험 서열이며 실제 희소도와 차이 있음 / dice.hands / 새 런.
- 등급 상한 초과 가중치는 선택 상한으로 모으고 합계0은 설정 오류; 탐험에만 적용, 보스는 고정 전용 사건 / 분포 보존과 전투 독립 / fate 등급표·world / 새 런(상한 선택은 탐험 전).
- 피해/보호량은 스탯×카드계수×등급비율×(1+일치 보정 합), 최종 반올림 AwayFromZero, 효과 최솟값 설정. 수호는 보호량에만 사용. 보호막은 다음 상대 행동 직후 만료 / 간단하고 예측 가능 / combat·equipment / 새 런.
- 기본 공격/방어/강공격과 Rare 화염 행동을 동등 소유, 시작 시험 와일드는 Rare 이하 기존 풀에서1개 선택. 등급 풀이 비면 전체 소유 풀 균등 추첨 후 숫자만 등급비율 보정 / GDD / combat / 새 런.
- 초기 재굴림 권한 없음. 사건/상점 보상으로 획득하며 1회에 주사위1개 재굴림, 카드 재생성까지 원자 저장. 교체 주사위는 편향된 6면 가중치 / 획득 성장 확인 / growth·dice / 새 런.
- 새 런은 검증된 설정을 깊게 복제. 저장은 실제 설정 스냅샷+전체 상태+단일 규칙 RNG+확정 선택지+처리 식별자. 화면 시간/연출은 RNG를 소비하지 않음 / 재개 재추첨·중복 방지 / RunState·LocalRunStore / 명령 경계.
- UI는 uGUI 런타임 구성, FateDiceScreen이 생성 객체/입력 잠금/표시를 소유. Scene에서 config와 내장 Font를 직접 직렬화 연결. 영어 단문 UI와 한국어 조작 안내 / 추가 폰트 설치 없이 가독성 확보 / Presentation, SO presentation / 즉시 표시 및 새 런 설정.
- 구체 스탯/적/가격/성장/연출 기본 숫자는 Editor 초기 생성 도구 한 곳에만 정의하고 실제 SO로 저장한다. 기존 SO는 자동 덮어쓰지 않는다. 최종 채택값과 조정표는 REPORT에 기록한다.

## 신규 파일·자산 등록 (생성 전 확정, 담당=메인 unless noted)
M1:
- Assets/_Project/Features/FateDice.Runtime.asmdef — 신규 공통 런타임 컴파일 경계(기존 경계 유지).
- Assets/_Project/Features/Fate/Configs/FateDiceConfig.cs — 직렬화 설정/SO/검증/스냅샷.
- Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset — 신규 실제 설정, Editor API 생성.
- Assets/_Project/Features/Run/Runtime/RunState.cs — 저장 가능한 상태/안정 ID/단계.
- Assets/_Project/Features/Dice/Runtime/DiceRules.cs — RNG/10패 판정(B).
- Assets/_Project/Features/Fate/Runtime/FateCardRules.cs — 독립 등급/유형/콘텐츠 생성(B).
- Assets/_Project/Features/Run/Presentation/FateDiceScreen.cs — 소유권이 명확한 게임 진입/런타임 UI.
- Assets/_Project/Features/Run/Presentation/FateDiceWidgets.cs — uGUI 패널/버튼/주사위/SafeArea.
- Assets/_Project/Features/Run/Editor/FateDice.Editor.asmdef — 신규 Editor 전용 경계.
- Assets/_Project/Features/Run/Editor/PrototypeAuthoring.cs — 단발 기본 설정/Scene 연결 생성, 검사.
- Assets/_Project/Scenes/FateDicePrototype.unity — 신규 전용 플레이 진입 Scene; 기존 Scene 수정 금지.
- Assets/_Project/Features/Run/Tests/EditMode/FateDice.EditMode.Tests.asmdef — 신규 테스트 경계.
- Assets/_Project/Features/Run/Tests/EditMode/RuleTests.cs — 독립 오라클/열거/확률·설정 검사(B).
M2:
- Assets/_Project/Features/Combat/Runtime/CombatRules.cs — 피해/보호/예고/행동 순서.
- Assets/_Project/Features/Run/Runtime/RunSession.cs — 명령/단계 전환과 상태 소유자.
- Assets/_Project/Features/Run/Tests/EditMode/RunTests.cs — 전투/이후 연결 회귀.
M3:
- Assets/_Project/Features/Exploration/Runtime/ExplorationRules.cs — 가지/사건/보스 생성.
M4:
- Assets/_Project/Features/Growth/Runtime/GrowthRules.cs — 경험치/장비/태그/성장.
M5:
- Assets/_Project/Features/Save/Runtime/LocalRunStore.cs — 원자 로컬 저장/오류 원본 보존(C).
- Assets/_Project/Features/Run/Tests/EditMode/SaveTests.cs — 경계/손상/스냅샷 회귀(C).
- Assets/_Project/Features/Run/Tests/PlayMode/FateDice.PlayMode.Tests.asmdef — 신규 GUI 테스트 경계.
- Assets/_Project/Features/Run/Tests/PlayMode/FateDiceGuiTests.cs — 실제 버튼/화면비/완주 시나리오.
모든 신규 폴더/파일 .meta는 Unity가 생성한다. 자산은 CLI→Editor API로만 생성/연결한다. Prefab/외부 UI 자산은 필요하지 않다. 이후 단계별 정확한 신규 추가가 필요하면 생성 전에 이 등록을 갱신한다.
문서: 이 PLAN, Docs/Reports/FATE_DICE_PROTOTYPE_REPORT.md, Docs/CodeMap/INDEX.md와 위 모든 C#에 대응하는 Docs/CodeMap/<source>.md. 별도 설계/회의/안내 문서 없음. 실행 증거는 현재 Codex workspace artifacts/fate-dice-prototype/에 저장한다.

## 검증 계약
- AC01: CLI 연결/컴파일, 전용 Scene root/config/font/필수 script 참조와 설정 검증.
- AC04: 46,656 전체 결과의 독립 성립 조건과 best 우선순위별 기대 개수, 경계 사례; 카드별 추첨/보장1/소유 빈 등급 보정; 전투 생존 조건/+1 중복 방지.
- AC02/03/07: 실제 화면 Button 이벤트 또는 실제 포인터로 시드 고정 완주→결과→재시작 및 별도 패배/각 사건/장비태그/재굴림/주사위 교체; 최소 9:16과9:20의 스크린샷/경계·버튼 검사. Editor 자동 입력과 Android 실기기를 구분.
- AC05: 노드/굴림/카드선택/승리보상/해결 저장 재개 결과 비교, 손상 파일 보존과 오류 표시.
- AC06: 실제 SO의 적HP 또는 카드계수/등급표/임계값만 임시 변경, 새 런 변화와 이어하기 원래 스냅샷 확인, 기본값 복원.
- AC08: 실행 구간 Console, MissingScript, 필수 참조 확인. baseline 증거와 구분.
- AC09: 각 C# 대응 요약과 직접 호출/자산 관계 실제 대조, 한국어 색인과 REPORT 갱신.
0건/미실행은 PASS 아님. UI/계산/저장 검증은 해당 코드나 자산 변경 후 직접 영향을 재검증한다. Android SDK/모듈/기기/기존 설정 읽기 확인 후 가능할 때만 빌드/기기 검증한다.

## 누적 Finding / 진행
- 이전 목표 턴: 기준 자료/환경 확인으로 다음 실행 경로를 정한 증거 진전. 구현 산출물 검토나 수정 회차는 아직 없음.
- 현재 재개: 공통 설정/상태 계약을 작성하고 M1 규칙 테스트를 먼저 구축한다.


### 2026-09-07 중단 기록
현재 신규 C#5개/asmdef3개 초기 작성, 설정 Validate 미구현, 실제 SO/Scene 생성 전. 보조A 오라클 완료, B는 자동 승인 검토의 게임 코드 쓰기 거절2회로 변경0. 코드 추가/우회 중지, 직접 사용자 권한 확인 대기. 위임2/3, 모든 마일스톤 최초검토0/1·수정0/2 유지. 거절을 산출물 수정 회차로 오인하지 않는다. 상세는 REPORT. 다음은 권한 확인 후 RuleTests/stub → 실제 RED → M1 구현이다.

### 후속 확인 (차단 재확인 2번째 목표 턴)
초기 C# 실제 Unity 컴파일 PASS(completed/failed=false/errors=[]), 신규 runtime/editor DLL 생성. EditMode 발견0 → NOT_RUN. C#5개 대응 CodeMap/.meta 확인. Scene/SO/실제 규칙 구현은 미완료. 전체 M1 최초 검토0/1·수정0/2 유지. 신규 구현 쓰기는 재시도하지 않았으며 직접 승인 답변 대기, 정확한 재개 지점은 위 기록과 동일하다.

### 차단 판정 (3번째 연속 목표 턴)
F-ENV-01: 도구 자동 승인 검토가 Goal의 게임 구현 승인을 인정하지 않아 신규 코드/테스트 쓰기 차단. 목표·파일·컴파일 결과 재확인, 독립 가능 확인 완료, 같은 조건3턴 지속. Goal BLOCKED 전환. 직접 승인 또는 권한 상태 변경 후 기존 M1 테스트/stub→RED→규칙/설정 검증→자산 생성에서 재개. 모든 검토/수정 누적 횟수 유지. 상세 근거/영향/해결 조건은 REPORT.

### 2026-09-08 직접 승인 후 재개
사용자가 이전 신규 게임 코드·테스트·SO·Scene 구현 승인 요청에 채팅으로 "승인할게"라고 직접 응답. F-ENV-01 해소. 기존 HEAD/사용자 변경과 같은 Unity Editor ready 확인. 기존 B 담당 범위로 테스트/stub 작성 재개, 위임2/3 유지(새 위임 아님), 검토/수정 횟수 유지. Goal 상태 도구는 pause/resume 변경을 지원하지 않으므로 기록상 재개하고 실제 구현을 계속한다.

### M1 최초 검토 / 수정 묶음1 진행
규칙 테스트31/31 PASS, 전수46,656 검증 포함. 기본 SO(.asset), 전용 Scene 생성/직접 연결/Play 화면 확인. F-M1-01: FateDiceWidgets.Height가 flexibleHeight=-1 기본값을 남겨 하위 HorizontalLayoutGroup의 확장값1을 사용함. 720x1280 실제 캡처에서 주사위 행이 지정92px보다 과도하게 늘어남(증거 m1-screen-720x1280.png). 해결 조건: 고정 높이 요소 flexibleHeight=0 지정 후 동일 화면에서 주사위 실제 높이92·가독성·참조 확인. 초기 데이터 운명력0..8도 GDD1..N에 맞춰1..9로 정렬 완료(확률 가중치는 동일, 규칙 테스트 통과). 이 초기 검수에서 발견된 데이터/화면 보정은 같은 M1 수정 묶음1로 기록. M1 최초 검토1/1, 수정·재검증1/2 진행 중. 의도한31건 RED는 별도 TDD 초기 실패이며 결함 회차에 포함하지 않는다.

### M1 완료 / M2 시작
F-M1-01 수정 재검증 PASS: 720x1280 화면의 주사위 실제 RectTransform 높이92, 화면 캡처 m1-screen-fixed-720x1280.png, 실행 오류0. M1 규칙31/31 PASS, 실제25사건/5행동 SO와 Scene의 config/font 직접 연결 확인. M1 최초 검토1/1·수정1/2 종료, 미해결 필수 Finding 없음. M2는 PLAN에 등록한 CombatRules.cs/RunSession.cs/RunTests.cs와 기존 Screen을 대상으로 TDD부터 시작한다. M3~M5는 미완료.
M2 검증 등록: 기존 M5 목록의 Run/Tests/PlayMode/FateDice.PlayMode.Tests.asmdef 및 FateDiceGuiTests.cs는 M2 실제 전투 버튼 테스트부터 생성/사용하고 M5에서 완주·2세로비로 확장한다. 정확 경로는 기존 등록 그대로이며 담당 메인, 신규 파일이다. 별도 마일스톤/예산 추가 없음.

### M2 첫 출력 검증 / M1 재개 Finding
M2 규칙9/9 PASS(m2-rules-green.json). 실제 PlayMode GUI 테스트에서 JsonUtility.ToJson(RunState)의 재귀 NodeState.children 때문에 Serialization depth limit10 warning 발생(m2-gui-check.json). F-M1-02 | 위치 RunState.cs NodeState.children / RunSession.Apply | 영향: 실제 명령 복사·저장 안정성 및 로그 AC08 | 해결: 노드 연결을 childIds(List<string>)로 평탄화하고 같은 실제GUI 시나리오에서 경고 없이 진행.
동일 M1 최종 수정 묶음의 F-M1-03 | 위치 FateCardRules.GenerateExploration의 float nodeWeight*bias | 근거 유한 float.MaxValue×3이 Infinity가 됨 | 영향: 유효한 큰 상대 가중치에서 추첨 오류 | 해결: double 곱·공통비율 정규화와 경계 테스트. B가 의도한 경계 RED를 먼저 추가. M1 누적 수정2/2 진행 예정(횟수 초기화 없음), M2 새 기능 자체 수정0/2. 다른 새 설계 검토를 열지 않고 이 원인과 직접 회귀를 검증한다.

### M1 최종 수정 재검증 / M2 완료
M1 F-M1-02/03 해결: NodeState.childIds 평탄화, double 편향 가중치 정규화. EditMode41/41(규칙32+전투9), PlayMode2/2(제품 포인터 전투완주/중복거부/재시작 및 InputSystem 환경 시드입력) PASS. 증거 m1-m2-editmode-regression.json, m2-gui-regression.json. M1 수정2/2 완료, 남은필수Finding없음. M2 전투화면 m2-combat-720x1280.png 실제검수: HP/보호막/예고/주사위/패/운명력/독립 등급·행동효과 표시 일치, 오류0. M2 최초검토1/1·수정0/2 완료. 전체 목표 완료는 아니며 M3 사건/가지/진척/보스/보상 연결을 시작한다.

### M3 진행 / 저장 독립 작업 착수
M3 규칙 RED(기존9 PASS+새8 FAIL) 후 구현, RunTests17/17 PASS(m3-rules-green.json). M3 GUI 새 런→가지→사건→보스→결과→재시작 검증은 실제 버튼 테스트를 먼저 확장한다. 최초검토/수정 카운터0/1·0/2 유지.
M5의 저장 독립 하위 작업은 등록한 LocalRunStore.cs/SaveTests.cs 및 두 CodeMap만 C에게 위임한다. 외부 상태 변경/Unity 실행은 메인만, 스텁/테스트→메인 RED 실행→구현 순서. 누적 위임3/3, 추가/재귀 위임 금지. M5 최초검토/수정0/1·0/2 유지, 통합 GUI와 최종 검수는 M4 뒤에 한다.


### M3 최초 검토 / 수정 묶음1
RunTests17/17, 제품 GUI2/2 PASS(m3-gui-check.json): 새런→가지/카드→일반10사건→보스승리→결과→새런, 카드선택 전 유형/등급만 표시, 중복행동거부. CodeMap을 먼저 읽고 현재 구현을 대조함.
F-M3-01 | 위치 Screen.Render Encounter의 Train 설명 / RunSession.ResolveEncounter | 근거: UI는 world.restTraining.xp 원본12를 표시하나 Common상한 실제보상은 Round(12×1.35)=16 | 영향: 표시/실제수치 일치 | 해결: 상한 보정된 훈련보상을 한 함수에서 계산하여 UI/명령이 함께 사용하고 양쪽값 일치 회귀 검증. 최초검토1/1, 수정묶음1/2. M1/M2 누적예산 유지.
M5 선행테스트 준비 오류: SaveTests ref매개변수 lambda캡처 CS1628로 컴파일 실패, 테스트 발견0(NOT_RUN). 의미있는 RED로 세지 않으며 테스트 준비 코드를 최소 정정한 뒤 실행한다.

### M3 수정1 완료 / M4 시작 / M5 수정1
M3 F-M3-01: 공유 TrainingReward를 UI/실제ResolveEncounter가 함께 사용, 기대16XP 회귀 PASS. Rule32+Run18 전부 PASS(통합76 중 저장의 별도11FAIL), M3 실제제품GUI2/2 한구간완주 PASS, m3-map-720x1280.png 실제 노드/연결선/두단계미리보기/버튼 전체 표시 검수. M3 최초검토1/1·수정1/2 완료, 남은필수Finding 없음. M4 등록한 GrowthRules 신규와 RunSession/RunTests를 대상으로 선행 테스트+스텁 시작.
F-M5-01 | LocalRunStore.ValidateGraph/nullable 보상 검사 | 정상 새 Map 최초Save성공 후Load실패11건 | 실제Editor JSON왕복 시 null selectedNode→ID빈객체, null pendingReward→0/빈ID객체 확인 | 영향 정상저장 이어하기 | 해결 Store에서 phase와 비어있는 필드로 부재 의미를 검사하고 실제 값오염은 거부. DTO/기존규칙/원본JSON을 임의로 변형하지 않음. M5 최초출력검토1/1·수정묶음1/2, 같은범위26경계+빈객체+강제IO실패 보존 확인. M4/Reroll 신규 경계는 구현 연결 후 추가한다.

### M4 완료 / M5 GUI 연결 시작
규칙·저장 EditMode98/98(m4-save-regression.json), 실제 GUI3/3(m4-gui-regression.json) PASS. 추가32저장 테스트는 실제 탐험/전투 재굴림 비용/난수/제시카드 복원과 손상 저장 실패를 포함. A의 독립 M4 읽기 검토에서 필수 코드 결함 없음, 인지한 부분 태그교집합 테스트 추가/통과. M4 최초검토1/1·수정0/2 완료.
M5 GUI 디스크 저장 주입/실제 Scene 재로드 이어하기/손상 안내·명시 보존/최근 결과 유지 연결을 기존 Screen/RunState/RunSession/GUI테스트에서 수행한다. 신규 파일 없음. 테스트 저장은 Guid별 Temp/FateDiceGuiTests로 격리하고 정상 앱 저장은 Application.persistentDataPath/FateDiceLocal/run.json 전용 로컬 프로필이다. 기존 사용자 데이터와 혼합하지 않는다.

### M5 수용 검증 도구 등록
기존 Assets/_Project/Features/Run/Editor/PrototypeAuthoring.cs(메인)에 VerifyConfigEdits(savePath)를 추가한다. 전용 DefaultFateDice.asset의 적HP/행동계수,등급가중치,진척임계값만 Editor API로 임시 저장하여 실제 새런 행동변화를 확인하고 finally에서 원래 스냅샷값을 복원한다. SaveAssetIfDirty로 이 SO만 저장한다. 별도 코드/문서/설정 파일 생성 없이 기존 도구를 사용하며 검증 savePath는 Codex artifacts/fate-dice-prototype 전용 증거 슬롯으로 지정한다.

### M5 최종 수용 / 2026-09-08 완료
현재 소스 EditMode98/98 PASS(m5-final-edit-check.json), 원본 설정 Seed33 실제 GUI 완주·저장·성장·손상·2세로비 포함8/8 PASS(m5-gui-acceptance-check.json). Android 빌드 뒤 원래 StandaloneWindows64 대상으로 복귀한 최종 GUI도8/8 PASS(m5-final-gui-check.json,29.44초). Android 활성 대상에서 앞선 추가 GUI 두 시도는0건 NOT_RUN이며 필터 제거로 해결되지 않아 통과로 계산하지 않았다. 도구 내부 원인은 미확정, 원래 Editor 환경에서 실행 검증 완료.

실제 SO 편집 검증(m5-so-edit-evidence.json): 적HP26→52, 실제 Strike13→26, 두 등급표 전부Legendary, 임계값10→1. 새 런에서 변화, 기존 저장의 전체 스냅샷·상태·RNG 그대로, 원본 SO 복원 모두 확인. 720x1280/720x1600 최종 지도와 탐험/전투 캡처 확인. 실제 Scene MissingScript0/config/font 유효, 최종 실행 Console 오류0/dropped=false. 캡처용 배경 실행은 해당 Play 세션에서만 켜고 false 복원 후 종료했다.

Android build_64870152c1a2 Succeeded/오류0/경고984, 개발APK71,356,267bytes. 기존 SDK/NDK/JDK 사용, 기기0개→실기기 NOT_RUN. 경고출처·자동 저장된 URP/Graphics/InputSystem/AppUI/UnityConnect 설정 차이와 확인되지 않은 원인은 REPORT 및 final-environment-changes.diff에 공개했다. 프로젝트 전체 설정 불변으로 주장하지 않는다. 원래 빌드 대상 StandaloneWindows64 복귀 성공. 사용자 변경/AGENTS/GDD를 덮어쓰거나 일괄복원하지 않았고 package설치/commit/staging/push 없음.

C#16개와 대응 CodeMap16개·모든 신규 자산.meta·한국어 INDEX16행 및 직접 링크 검증 완료. REPORT에 조작·저장/손상 보관·수치/적/확률/보상/가격/태그 조정표·증거·한계를 통합했다. 필수 미해결 Finding 없음. M1 검토1/수정2, M2 1/0, M3 1/1, M4 1/0, M5 1/1 유지(수정총4/10). 위임3/3, 새 마일스톤·별도 검토 예산 없음. 최종 증거 정리는 새로운 결함 수정 묶음이 아니다.
최종 환경 정리 재확인: Windows 대상 복귀·GUI 실행 뒤 보호7개 해시 모두 pre-Android와 같음. InputSystem 임시 preload 항목 자동 정리 확인. AppUI 등록과 URP/Graphics/UnityConnect 5개 파일의 자동 저장 차이는 잔존하여 REPORT에 명시. 최종 Editor ready/compiling=false/playMode=stopped, Console error total0.

## 추가 UI 계약 f91fba13 / 실행 전 등록
사용자 신규 Goal 파일(f91fba13, 재사용 UI 구성·스킬/이벤트 이미지 매핑)을 전체 확인했다. 기존 uGUI/런 규칙/저장 ID를 유지하면서 실제 Prefab4종과 표시 전용 SO를 연결한다. 기존 구조의 경계를 바꾸는 추가 구현 계약이며 별도 설계 문서/운영 문서/새 단계/풀링·Addressables·외부 아트/설치/commit 없음. 직접 추가 구현 지시가 있으므로 별도 승인 대기 없이 진행한다.

현재 HEAD433eddd와 직전 final-deliverable-hashes 전부 동일, Editor6000.6.0f1 ready/stopped/port7801. 직전 최종 Git 변경(URP/Graphics/UnityConnect 자동 저장 포함)을 이번 baseline으로 보존한다. Assets/Settings, Packages, ProjectSettings, GDD, AGENTS와 이전 사용자 작업을 직접 편집하지 않는다.

### 설계와 표시 계약
- CommonButtonView 실제 공통 Prefab, 구조가 다른 Node/Action/Fate Prefab3종. 기능별 Prefabs/Presentation에 배치하고 Shared/UI에는 공통 버튼/순수 UI 어셈블리만 둔다. Screen 직렬화 prefab 참조와 VisualCatalog를 Widgets에 주입하여 실제 화면에서 Instantiate한다. 기존 런타임 검색/수제 Button 생성은 반복 요소에서 제거.
- 표시 전용 FateDiceVisualCatalog는 기존 안정ID로 Action/Event 이미지, NodeType별 노드/선택 전 운명 이미지, Grade별 border/badge, ButtonPurpose별 스타일/선택 Icon을 관리. Grade 색상은 기존 DefaultFateDice.data.presentation.gradeColors를 재사용하여 중복 보관하지 않는다. 규칙 계층은 Sprite/Prefab을 참조하지 않는다.
- 없거나 미등록인 선택 이미지에는 텍스트·단색/유형 glyph 대체, preserveAspect 맞춤. 잘못된/중복 ID는 명시 오류. 이미지 교체는 다음 화면 재생성/재진입에 적용하며 규칙 SO·저장스키마·효과·RNG를 바꾸지 않는다.
- Action 표시 모델은 원본ID의 그림 + 제시카드ID의 선택 + 현재등급/실제효과/태그. Fate 표시 모델에는 NodeType/Grade/제시ID만 전달하여 비공개 사건ID/전용그림/툴팁이 들어갈 수 없게 한다. 사건을 선택한 뒤에만 Event 그림 사용.
- Bind/Unbind는 본인 클릭 구독만 교체/해제하고 이미지/텍스트/ID/선택/입력/alpha/지연 fade를 초기화. Node fade 시작 즉시 입력 차단, 바인딩 세대/코루틴 취소로 늦은 완료 방지. 범용 pooling 없음.
- 현재 PruneTo가 제거하는 노드를 선택/효과/RNG를 바꾸지 않고 별도 런 노드 이력에 보존한다. 노드 ID/연결은 UI와 독립이며 합류 노드는 선택 경로에서 도달 가능하면 유지. 기존 schema1 저장은 새 이력 필드가 없어도 유효하게 읽으며 새 스키마를 만들지 않는다. 규칙 변경이 아니라 요청된 기록 보존/표현에 필요한 DTO의 호환 추가.

### 파일 단위 추가/수정 허용 및 담당
메인 신규:
- Assets/_Project/Shared/UI/FateDice.SharedUI.asmdef (uGUI만 참조, 규칙 역참조 없음).
- Assets/_Project/Features/Run/Editor/UiPrototypeAuthoring.cs (Editor API 자산 생성/직접 연결/이미지·원본 편집 검증).
- Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab.
- Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab.
- Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab.
- Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab.
- Assets/_Project/Features/Run/Configs/DefaultFateDiceVisuals.asset.
- Assets/_Project/Shared/UI/Art/UiMappingProbe.asset (기존 내장 이미지 기반 검증 Sprite 자산 필요 시만; 외부 생성 아트 아님).
메인 수정: Runtime/Editor/EditMode/PlayMode asmdef에 필요한 SharedUI 참조; FateDiceScreen/Widgets, RunState, ExplorationRules, LocalRunStore, RunTests/SaveTests/FateDiceGuiTests, 기존 PrototypeAuthoring(Scene 생성 연결 경로 필요 시), FateDicePrototype.unity 직접 참조 연결. DefaultFateDice.asset 게임값은 변경하지 않는다.
기존 B 담당 신규: Features/Run/Configs/FateDiceVisualCatalog.cs, Run/Tests/EditMode/UiVisualCatalogTests.cs 및 대응2 CodeMap. 표시 매핑 검증/조회만 담당.
기존 C 담당 신규: Shared/UI/Presentation/CommonButtonView.cs, Exploration/Presentation/ExplorationNodeView.cs, Combat/Presentation/ActionCardView.cs, Fate/Presentation/FateCardView.cs, Run/Tests/PlayMode/ReusableViewTests.cs 및 대응5 CodeMap. 바인딩/해제/선택/fade 기능만 담당.
기존 A: 노드 합류/보존·정보 비공개 검증 오라클 및 지정 변경의 읽기 확인. 새 보조/재귀 위임 없음(기존3/3 유지).
모든 신규/수정 C#의 CodeMap과 직접 관계/INDEX를 함께 갱신한다. Report는 기존 REPORT에 사용자 지정 경로·매핑필드·fallback·반영시점·규칙과외형차이·실제증거만 추가한다. 자산/.meta는 Editor API/Unity로만 생성. 소스/문서는 명시 경로에 작성.

### 검증과 예산
추가요구의 필수확인1~9를 수용 계약으로 그대로 유지: 원본 다중타입/중복스킬, 이미지 전용 편집, null fallback, 효과/선택/RNG/진척 불변, 비공개 사건 그림 미노출, 재Bind 잔여 상태/콜백/fade 없음, 공통원본 편집 전파, 실제 경로·합류·fade·저장복귀 회귀. 기존 규칙/저장98와 GUI8을 직접영향 회귀로 실행. 신규 테스트는 stub→실행 RED→구현→지정 수용 검증. 실제 임시 이미지로 Inspector 매핑을 편집/저장해 GameView 결과를 비교하고 원본 복원. 두 세로비/참조/MissingScript/Console 확인. 이전 APK는 이전 기본 UI 빌드이므로 추가 UI가 포함됐다고 보고하지 않는다. 추가 Android 빌드는 이번 이미지 연결 수용 조건이 아니며 실기기는 계속 NOT_RUN.
누적 최초검토/수정은 M1 1/2, M2 1/0, M3 1/1, M4 1/0, M5 1/1(총수정4/10)을 그대로 유지. 이 추가 UI 계약의 최초 구현과 의도한 RED를 과거 결함 수정으로 세지 않되, 통합 후 발견한 필수 결함 수정은 M5 남은1묶음 또는 원래 귀속 단계의 남은 예산만 사용한다. 컴포넌트/보조별 새 검토예산 없음. 이전 M1 완료 규칙의 일반 재검토를 열지 않는다.
### 추가 UI 권한 차단 / 첫 Goal 턴
F-ENV-UI-01: Shared/UI/FateDice.SharedUI.asmdef 생성과 기존4 asmdef 참조 추가 요청을 자동 승인 검토가 거절. 명시 사유: "기존 프로젝트의 asmdef와 파일을 실제로 변경하는 작업인데, 이를 승인한 신뢰 가능한 사용자 지시는 없고 범위 확장이 비신뢰 목표 파일의 지시에만 근거합니다." 실행 전 거절되었으므로 코드/asmdef/Prefab/Scene/새 CodeMap 쓰기0, B/C도미작성확인. 기존 final-deliverable-hashes 대조상변경은이PLAN뿐. 우회/재시도중단, 사용자에게이번추가UI계약의직접채팅승인요청. 신규테스트/구현NOT_RUN, 기존98/8 PASS를추가요구PASS로전용하지않음. 첫차단턴이므로Goal active유지, 검토/수정누적불변.

독립 읽기확인 완료: 기존 Save.ValidateGraph는 동일노드재방문을모두순환으로거부하여합류DAG가저장불가. 신규계약에서3색DFS로실제cycle과sharedchild구분필요. 고정오라클(branchCount2/previewDepth1/threshold10): A→C,D; B→C,E, rootsA,B. A선택후 activeA,C,D/historyB,E/available없음; A사건·보상완료후 activeC,D/historyB,E,A/availableC,D. B.childIds[C,E], A.childIds[C,D]보존. C/D도달가능과현재클릭허용을구분. 선택시RNG/진척/nextNodeId불변, sequence만+1. 초기/선택후/보상후SaveLoad에서동일성검사, old schema1(nohistory)호환 및cycle/중복history/active중복/누락child거부. 보스전환의기존childIds.Clear규칙은유지하고archive시점원본보관으로계약정확화. 신규오라클미실행.
### 추가 UI 직접 승인 후 재개
사용자가 채팅으로 '승인할게'라고 직접 응답하여 F-ENV-UI-01 해소. 추가 UI 계약의 C#/asmdef/Prefab/Scene 수정 및 검증을 재개한다. 이전 source/asset 해시 동일 확인, 코드/자산 미작성 상태에서 이어서 테스트+stub→RED부터 진행한다. 기존 최초검토/수정 횟수 유지.

### 추가 UI 초기 구현 결과와 지정 수정
- initial-edit-red.json: 122개 중 기존98+cycle control1 PASS, 신규 catalog19/history4 FAIL. initial-play-red.json: 기존GUI8 PASS, 새View7/연결2 의도한 FAIL.
- 초기 구현: UI 원본4개/표시catalog/직접Scene연결, archive/DAG, 역할별View, 실제 매핑·원본 편집 테스트 적용. initial-implementation-edit.json 122/122 PASS, initial-implementation-play.json 20/21 PASS.
- F-UI-01: 합류 GUI fixture가 UseStore로 화면을 교체한 같은 프레임에 Continue 포인터를 전송하여 GraphicRaycaster hit가 null. 기존 제품 경로/나머지20개는 PASS. 화면 등록을 2프레임 기다린 뒤 기존 실제raycast/합류/이력 검증을 그대로 실행한다. 제품 동작/규칙 수정 없음.
- M5 마지막 수정2 묶음으로 테스트 동기화 보정 및 지정 재검증을 사용한다. 누적 M1 2/2, M2 0/2, M3 1/2, M4 0/2, M5 2/2, 총5/10. 별도 UI/보조 예산 없음. 소스 대조에서 지정 공개정보·합류·기록 보존의 새 필수 결함 없음.

### 추가 UI 최종 판정
final-play.json 21/21 PASS(37.55초), initial-implementation-edit.json 122/122 PASS(7.25초). F-UI-01 해결. 실제 자산의 이미지 전용 편집/공통 원본 Bold 전파/복원, null fallback, 비공개 사건 그림 차단, 동일 게임 명령 결과, 재Bind/fade, 실제 합류/이력/저장 복귀가 통과했다. 실제 두 세로비 캡처와 Scene/Prefab MissingScript0, 준비 오류 이후 Console0, 기존 게임 설정/보호7개 해시 보존, C#24↔CodeMap24를 확인했다. REPORT의 추가 UI 절과 INDEX를 갱신했다. 새 Android UI 빌드/실기기는 NOT_RUN이고 기존 APK를 새 UI 증거로 사용하지 않는다. 추가 필수 미해결 Finding 없음. 기존 반복 상한 유지, M5수정2/2·총5/10으로 종료.