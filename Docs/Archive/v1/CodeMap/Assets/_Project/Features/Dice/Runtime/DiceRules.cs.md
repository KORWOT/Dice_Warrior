# DiceRules.cs

## RA-C 현재 경계 (2026-09-09)

Roll의 설정 인자가 RunRulesCatalog로 변경되고 Core asmref에 소속된다. 게임 난수·가중치·패 판정의 계산식, 입력검사, 소비 순서는 그대로다. RunApplication/FateCardRules/CombatRules/ExplorationRules와 기존 테스트가 호출한다. 정의 타입은 Fate/Domain/RunRulesCatalog.cs, 상태 난수의 소유는 호출자에게 있다.

기존 수식/행동 보존 및 Core 컴파일/전체 패·명령 재연은 ROGUELIKE_ARCHITECTURE_REPORT의 새 실행 증거를 따른다. 아래 기존 단계 증거를 RA-C PASS로 재사용하지 않는다.


- 원본: `Assets/_Project/Features/Dice/Runtime/DiceRules.cs`
- 기능/어셈블리: Dice / FateDice.Runtime
- Task: FATE_DICE_PROTOTYPE M1
- SHA256: `71A720D4F37F5D6B82D69004898CBBD726AE87819E77F2CC170CF7AA60C3DF05`

호출자가 소유하는 난수 상태로 가중 추첨·6D6 굴림을 수행하고, GDD 10종 패와 데이터 우선순위를 판정하는 정적 규칙 모음이다. Unity UI·시간·전역 난수를 참조하지 않는다.

## 입력·출력과 흐름
- `Next(ref uint)`: 0이 아닌 xorshift32 상태를 한 단계 갱신하고 출력한다. 0은 ArgumentOutOfRangeException이며 조용히 다른 seed로 바꾸지 않는다.
- `WeightedIndex(float[], ref uint)`: 유한·비음수·양의 합을 먼저 검사한다. 단일 난수로 누적 가중치를 선택한다. 후보가 하나여도 한 번 소비한다.
- `Roll(GameConfigData,string[],ref uint)`: 소유 ID 6개로 `GameConfigData.Die`를 찾는다. 모든 면/가중치를 먼저 검증한 뒤 6회 추첨하고 결과 6개와 최종 난수 상태를 반환한다.
- `Supports(HandKind,int[])`: 값 1..6의 6개 결과를 숫자별 빈도로 세어 한 패의 성립 여부를 반환한다. 서로 다른 두 묶음이 있어야 TwoPairs/FullHouse가 성립한다.
- `BestHand(int[],HandDefinition[])`: 10종 종류·우선순위 중복/누락을 거부하고 성립한 패 중 priority 최대인 원본 정의를 반환한다. fatePower는 선택에 사용하지 않는다.

## 직접 관계
- 읽는 대상: `Fate/Configs/FateDiceConfig.cs`의 GameConfigData.Die, DieDefinition, HandDefinition, HandKind.
- 확인된 호출자: `Fate/Runtime/FateCardRules.cs`의 DrawGrade/GenerateExploration/UniformIndex가 WeightedIndex를 직접 호출한다.
- 게임 호출자: `Run/Runtime/RunSession.cs`의 Roll이 DiceRules.Roll, Reroll이 WeightedIndex를 직접 호출한다. 두 명령의 RefreshOffers가 BestHand로 패·운명력을 확정한다.
- 추가 규칙 호출자: `Combat/Runtime/CombatRules.cs`의 NextIntent가 WeightedIndex로 적 예고를 추첨한다.
- 추가 탐험 호출자: `Exploration/Runtime/ExplorationRules.cs`의 CreateNode가 일반 노드 유형을 WeightedIndex로 추첨한다.
- 저장 검사 호출자: `Save/Runtime/LocalRunStore.cs`의 ValidateState가 BestHand로 저장된 주사위와 패·운명력의 일치를 검사한다. 검증 시 난수를 소비하거나 저장 주사위를 다시 굴리지 않는다.
- 검증 호출자: `Run/Tests/EditMode/RuleTests.cs`가 모든 공개 API를 직접 호출한다.
- 위 관계는 해당 심볼의 실제 소스 호출로 확인했다. 탐험·전투·재굴림·저장 검사 연결을 현재 소스에서 확인했다.

## 상태·검수
static mutable 상태가 없다. 직접 전달된 난수 상태만 변경하며 Roll은 오류 시 부분 난수 소비를 반영하지 않는다. 후보 정의나 SO를 변경하지 않는다. 반환 HandDefinition은 읽기 용도로 사용해야 한다.
RuleTests는 46,656개 순서 있는 결과 각각을 별도 파티션 오라클과 비교하고, 단독 성립 개수와 우선순위 적용 개수를 분리 확인한다. 경계·불법 입력·알려진 난수열·가중 분포·복원과 정확한 소비 횟수도 검사한다.
메인 실행 증거: `artifacts/fate-dice-prototype/m1-rule-red.json`, 초기 계약 31건 실패(의도된 NotImplemented). 초기 구현 GREEN 이력: artifacts/fate-dice-prototype/m1-rule-green.json에서 completed, total31/passed31/failed0/skipped0/inconclusive0, 4.53초. 최초 테스트 기준을 변경하지 않고 통과했다.

### M1 최종 검증
- 증거 루트: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-prototype/.
- m1-m2-editmode-regression.json: completed, 41/41 PASS(RuleTests32 + RunTests9), 실패/건너뜀/미확정0, 5.5초. 원본 결과의 테스트 이름으로 분류를 직접 확인했다.
- m2-gui-regression.json: completed, PlayMode2/2 PASS, 실패/건너뜀/미확정0, 0.85초. 이 GUI 결과는 M2 검증 범위이며 M3 이후 전체 게임 완료를 의미하지 않는다.
- M1 최초 검토1/1, 수정2/2. 위 오버플로를 포함한 해당 필수 결함은 최종 회귀에서 해결됐다. 코드/테스트 추가 수정 없이 결과와 직접 관계를 동기화했다.
### 최종 연결 및 M5 검증
- m4-save-regression.json: EditMode completed, 98/98 PASS, 실패/건너뜀/미확정0, 6.9초. RuleTests32 원본 코드는 변경되지 않았다.
- m5-gui-acceptance-check.json: PlayMode completed, 8/8 PASS, 실패/건너뜀/미확정0, 29.4초. 위 M2 GUI 결과는 초기 검증 이력이다.
- m5-so-edit-evidence.json: 실제 SO의 탐험/전투 등급 행을 제어된 100% Legendary 가중치로 변경하자 새 런 등급이 Uncommon,Common,Uncommon에서 Legendary,Legendary,Legendary로 바뀌었다. 이전 저장 스냅샷과 동일 RNG 재실행 상태가 유지됐고 SO 원본 복원도 확인됐다. 이 결과는 한 번의 표본으로 일반 확률 분포를 추정한 결과가 아니다.
- M1 검토1/1·수정2/2·필수 결함 해결 상태를 유지한다. 이번 동기화는 위 3개 규칙/테스트 C#의 변경 없이 실제 연결과 후속 검증 증거만 반영했다. Android 빌드·기기 검증 범위는 이 문서의 증거로 판단하지 않는다.

## 전투 피드백 직접 관계

- KoreanText.HandStage/HandSummary는 확정 hand/fatePower와 저장 config의 priority를 표시한다. 표시 시 판정·난수·카드 추첨을 다시 실행하지 않는다.
- 실행 증거: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
