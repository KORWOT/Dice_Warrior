# FateCardRules.cs

## RA-C 현재 경계 (2026-09-09)

DrawGrade는 RunRulesCatalog, 상태를 받는 생성/검사는 RunStateData와 Rules를 사용한다. Core asmref에 소속되며 Runtime/표시 타입을 참조하지 않는다. 보장 유형·슬롯별 추첨·ID 생성·중복/빈 등급 대체·단일 RNG 소비 및 성공 때만 RNG를 확정하는 순서는 그대로다. RunApplication과 기존 규칙/재연 테스트가 호출한다.

기존 수식/행동 보존 및 Core 컴파일/전체 패·명령 재연은 ROGUELIKE_ARCHITECTURE_REPORT의 새 실행 증거를 따른다. 아래 기존 단계 증거를 RA-C PASS로 재사용하지 않는다.


- 원본: `Assets/_Project/Features/Fate/Runtime/FateCardRules.cs`
- 기능/어셈블리: Fate / FateDice.Runtime
- Task: FATE_DICE_PROTOTYPE M1
- SHA256: `A7BFC58616EC0DA7C182133E0951F00E51F2A02B4BB241B7D82EFB751593656A`

확정된 운명력과 런 소유 난수로 탐험/행동 카드 목록을 만든다. 카드 표시·실행·저장은 소유하지 않는다.

## 입력·출력과 흐름
- `DrawGrade(GameConfigData,int,bool,Grade,ref uint)`: 운명력을 설정 범위로 제한하고 목적별 표의 해당 minimumPower 행을 선택한다. 탐험의 상한 초과 가중치는 상한 등급에 모으고, 전투는 탐험 상한을 적용하지 않는다.
- 가중치 집계는 double을 사용하고 전체를 같은 비율로 축소한 뒤 DiceRules.WeightedIndex로 한 번 추첨한다. 원본 배열을 변경하지 않는다.
- `GenerateExploration(RunState)`: 슬롯 0은 선택 노드 유형, 나머지는 selectedTypeBias를 double로 곱하고 전체를 동일 비율로 축소한 유형 가중치로 뽑는다. 유한 float 최대값의 곱이 Infinity가 되는 문제를 피하며 원본 배열은 변경하지 않는다. 각 슬롯은 독립 등급 추첨 후 정확한 type/grade 사건 풀에서 내용을 확정한다. 비어 있는 사건 풀은 오류다.
- `GenerateActions(RunState)`: 소유 stable ID를 중복 제거한 풀에서 슬롯별 등급을 추첨한다. 해당 원본 등급이 비면 전체 소유 풀을 균등 추첨한다. 보장 공격/방어·와일드 우대가 없으며 중복 제시를 허용한다.
- 결과 OfferedCard에 offered grade와 contentId를 분리한다. 수치 보정이나 원본 ActionDefinition 변경은 하지 않는다. ID는 runId:card:sequence:slot이며 ID 생성에 난수를 소비하지 않는다.

## 직접 관계
- 직접 호출: `Dice/Runtime/DiceRules.cs`의 WeightedIndex, `Fate/Configs/FateDiceConfig.cs`의 GameConfigData.Action.
- 직접 읽기: GameConfigData의 dice/fate/combat/world 설정, `Run/Runtime/RunState.cs`의 선택 노드·운명력·상한·소유 행동·sequence/runId.
- 직접 쓰기: 성공 시 RunState.rngState 한 필드. List<OfferedCard>는 반환하며 state.cards를 대입하지 않는다.
- 확인된 호출자: `Run/Tests/EditMode/RuleTests.cs`, `Run/Runtime/RunSession.cs`의 RefreshOffers가 현재 단계에 따라 GenerateActions/GenerateExploration을 직접 호출한다. Roll과 획득 자원을 사용하는 Reroll이 RefreshOffers로 카드 목록을 확정한다.

## 상태·검수
전체 생성은 로컬 난수 변수로 계산하여 실패 시 런 난수 상태를 소비하지 않는다. 성공해도 HP·진척·sequence·선택 카드 등은 변경하지 않는다. RunSession은 반환 결과와 갱신 난수를 명령의 복제 상태에 넣고 Checkpoint 후 함께 확정한다.
탐험 내용은 생성 시 결정되지만 화면 공개 정책은 이 클래스 밖에서 지켜야 한다. 행동 원본 등급과 제시 등급의 수치 배율 처리는 행동 실행 계층의 책임이다.
RuleTests는 유형 보장·편향·독립 등급·상한 확률 집계·전투 상한 분리·빈 등급 소유 풀 균등·원본 불변·RNG 재현을 확인한다. 확률 테스트는 고정 seed 표본의 허용 오차이며 정확 빈도 보장을 뜻하지 않는다.
초기 구현 GREEN 이력: artifacts/fate-dice-prototype/m1-rule-green.json에서 completed, total31/passed31/failed0/skipped0/inconclusive0, 4.53초. 최초 테스트 기준을 변경하지 않고 통과했다. 초기 RED 증거는 `artifacts/fate-dice-prototype/m1-rule-red.json`이다.

M1 마지막 수정묶음 2/2: 메인 m1-weight-overflow-red.json에서 유한 최대 nodeWeights와 bias가 config 검증을 통과한 뒤 float곱으로 실패하는 회귀 RED 1/1을 확인했다. double곱/공통 축소 수정 후 신규 경계 포함 32건이 최종 통합 회귀에서 모두 통과했다. 위 31건 GREEN은 수정 전 이력이다.

### M1 최종 검증
- 증거 루트: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-prototype/.
- m1-m2-editmode-regression.json: completed, 41/41 PASS(RuleTests32 + RunTests9), 실패/건너뜀/미확정0, 5.5초. 원본 결과의 테스트 이름으로 분류를 직접 확인했다.
- m2-gui-regression.json: completed, PlayMode2/2 PASS, 실패/건너뜀/미확정0, 0.85초. 이 GUI 결과는 M2 검증 범위이며 M3 이후 전체 게임 완료를 의미하지 않는다.
- M1 최초 검토1/1, 수정2/2. 위 오버플로를 포함한 해당 필수 결함은 최종 회귀에서 해결됐다. 코드/테스트 추가 수정 없이 결과와 직접 관계를 동기화했다.
### 최종 연결 및 M5 검증
- m4-save-regression.json: EditMode completed, 98/98 PASS, 실패/건너뜀/미확정0, 6.9초. RuleTests32 원본 코드는 변경되지 않았다.
- m5-gui-acceptance-check.json: PlayMode completed, 8/8 PASS, 실패/건너뜀/미확정0, 29.4초. 위 M2 GUI 결과는 초기 검증 이력이다.
- m5-so-edit-evidence.json: 실제 SO의 탐험/전투 등급 행을 제어된 100% Legendary 가중치로 변경하자 새 런 등급이 Uncommon,Common,Uncommon에서 Legendary,Legendary,Legendary로 바뀌었다. 이전 저장 스냅샷과 동일 RNG 재실행 상태가 유지됐고 SO 원본 복원도 확인됐다. 이 결과는 한 번의 표본으로 일반 확률 분포를 추정한 결과가 아니다.
- M1 검토1/1·수정2/2·필수 결함 해결 상태를 유지한다. 이번 동기화는 위 3개 규칙/테스트 C#의 변경 없이 실제 연결과 후속 검증 증거만 반영했다. Android 빌드·기기 검증 범위는 이 문서의 증거로 판단하지 않는다.- RA-D 직접 관계: GrowthRules.Grant의 addActionId로 늘어난 런 소유 집합도 GenerateActions의 동일 등급/균등/중복제거 경로를 사용한다. ID별 분기·가중치 변경 없이 신규 콘텐츠 시험에서 실제 Roll 추첨을 확인한다.
