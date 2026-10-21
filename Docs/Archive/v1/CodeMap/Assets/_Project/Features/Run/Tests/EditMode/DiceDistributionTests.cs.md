# DiceDistributionTests.cs

RA-D · 독립 분포 fixture와 실제 게임 RNG 비교 · 2026-09-09

- 원본: `Assets/_Project/Features/Run/Tests/EditMode/DiceDistributionTests.cs`
- 기능/어셈블리: Run 검증 / FateDice.EditMode.Tests.
- 역할: 기본/Ember 1개/2개/6개 구성의 실제 `DiceRules.Roll`, `BestHand`, `FateCardRules.DrawGrade` 표본을 사전에 고정한 독립 수학 결과와 비교한다. 기존 `RuleTests`의 46,656개 전수 패 오라클은 그대로 두며 재구현·중복 실행하지 않는다.

## 입력과 핵심 동작

- 고정 입력은 `Fixtures/DiceDistributionExpected.json`이다. Unity 및 게임 RNG를 실행하지 않은 별도 정수 가중 전수 열거·분수 계산 결과에서 선택한 네 구성의 패 질량, 운명력 질량, 등급 확률과 자원 효율을 그대로 옮겼다. 원 모델·결과·입력 SHA256은 출처 정보이며 테스트 실행 결과로 기대값을 갱신하지 않는다.
- 실제 `FateDiceConfig.DefaultAssetPath`의 SO를 `AssetDatabase.LoadAssetAtPath`로 읽고 독립 `Snapshot()`을 만든다. 주사위 ID/면/가중치, 10종 패 enum/priority/fatePower, 운명력 범위, 탐험/전투 등급 행, 등급 배율, 제시 슬롯 수와 자원 비용/보상 관련 필드를 고정값과 대조한다. D의 신규 행동 등 무관한 데이터 추가를 허용하도록 전체 asset SHA는 비교하지 않는다. Common 상점 배율 1은 직접 확인한다.
- 8개 고정 비영 seed마다 62,500회, 구성당 500,000회·총 2,000,000회 굴린다. 한 표본은 6면 추첨→실제 채택 패→탐험 3슬롯 등급(Legendary 상한)→전투 3슬롯 등급(Common 상한 인자) 순서다. 전투 등급은 탐험 상한의 제한을 받지 않아야 한다. 패 판단이나 가중 난수 구현을 테스트에 복제하지 않는다.
- 각 슬롯은 별도의 500,000건으로 비교한다. 같은 운명력을 공유하는 3슬롯을 1,500,000건의 독립 표본으로 합치지 않는다. 전체 카드 생성의 유형/내용/ID 추첨과 런 명령·재굴림은 이 통계 표본의 대상이 아니다.

## 고정 허용오차와 출력

- 표본 실행 전에 선언한 family는 `4 × (패 10 + 운명력 9 + 면 36 + 탐험·전투 각 3슬롯의 등급 30) = 340`개다. `alpha=1e-4`, `L=ln(2×340/alpha)`, `ceil(L/3 + sqrt(2Np(1-p)L + L²/9))`을 절대 count 오차 한계로 적용한다. 확률 0 또는 1은 정확 개수를 요구한다. 기본 SixKind가 관측 0이면 실패할 수 있는 충분한 표본 수임을 별도 확인한다.
- 이상적 독립 모형에서의 Bernstein/union 한계와 결정적 xorshift32 고정 seed의 회귀 한계를 구분한다. 이 시험은 PRNG의 독립성 증명이나 희귀 사건의 정밀 추정·모든 상황에서의 성장 보장이 아니다.
- `TestContext.WriteLine`에 비교별 실제/기대 count, 오차, 허용폭, N 및 자원 요약을 남긴다. 테스트 승인 경로 `Path.GetTempPath()/FateDiceDistribution/actual-{0|1|2|6}.json`에도 시각·표본·seed/최종 RNG·340개 중 구성별 85개 비교·자원 지표를 남긴다. 비교 실패 시에도 JSON을 먼저 기록하며 다음 실행은 같은 구성의 파일을 덮어쓴다. 파일에는 sampling contract 및 독립 기대값 출처가 포함된다.
- 자원 요약은 Common 기준 12골드/3충전, 재굴림 1충전, Ember 16골드와 4골드/충전의 환산값을 명시한다. 실제 묶음·상품당 구매 제한과 구분한다. 한 Ember의 정확 평균 운명력 변화 `-5/324`, 슬롯별 Rare+ 확률의 한계, 별도 독립 이론의 선택적 한 번 재굴림 효율도 명시한다. 카드 가치·HP·남은 여정과 승률의 최적 전략을 뜻하지 않는다.

## 직접 관계와 상태

- 직접 사용하는 대상: `FateDiceConfig.Snapshot`, `RunRulesCatalog`의 주사위/패/등급/성장/상점·보상 정의, `DiceRules.Roll/BestHand`, `FateCardRules.DrawGrade`, NUnit, UnityEditor AssetDatabase, 기존 Newtonsoft.Json, 위 fixture.
- 직접 사용하는 쪽: Unity EditMode Test Runner. 제품 런타임 호출자는 없다.
- 관계 근거: 위 공개 API의 실제 소스와 호출 인자를 확인했다. 새 Common 배율 필드는 RA-D 확정 계약 `WorldSettings.shopPriceMultipliers`를 사용한다.
- 상태/수명: 테스트마다 SO의 독립 스냅샷과 지역 RNG를 사용한다. SO·게임 저장·전역 RNG를 변경하지 않는다. 파일 부수효과는 위 테스트 전용 임시 실측 JSON 네 개뿐이다.
- 검수 주의: fixture 자체의 기본 조합 수·질량 합·등급 확률 합·평균 및 한 Ember 역효과를 검사한다. 표본 결과를 보고 기대값·seed·허용폭을 바꾸면 안 된다. 하위 에이전트는 staging 파일과 정적 확인만 수행했다. Unity 컴파일/실행은 `NOT_RUN`; 실제 실행·통합 증거는 메인이 `ROGUELIKE_ARCHITECTURE_REPORT.md`에 기록한다.
