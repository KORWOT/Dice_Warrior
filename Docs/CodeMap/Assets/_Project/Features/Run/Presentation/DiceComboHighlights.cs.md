# DiceComboHighlights.cs

- 역할: 이미 확정된 hand와 여섯 결과에서 오라로 표시할 최소 참여 index와 그룹을 선택하는 순수 표시 helper다. 최종 hand·우선순위·게임 규칙을 다시 판정하지 않는다.
- 입력/출력: public static Groups(HandKind hand, int[] values)는 매 호출 새 int[6]을 반환한다. 비참여 -1, 참여 그룹 0 이상이며 입력 values를 수정/정렬하지 않는다. null/길이 6 아님/1..6 밖의 눈은 ArgumentException이다. 미지원 hand 또는 불성립 값은 전부 -1이다.
- 핵심 동작: 같은 눈은 Pair/Triple/FourKind/FiveKind/SixKind에 필요한 2/3/4/5/6개만 입력 index 순으로 선택하며 후보 눈은 숫자 오름차순이다. TwoPairs는 작은 눈부터 두 그룹 4개, ThreePairs는 세 그룹 6개다. FullHouse는 가장 작은 triple 후보를 그룹0, 별개 가장 작은 pair 후보를 그룹1로 구성한다. Straight는 1..5 우선, 불가할 때 2..6에서 눈마다 첫 index 한 개씩만 사용한다. FullStraight는 여섯 개 전부 그룹0이다.
- 사용하는 대상: Core의 HandKind와 System.ArgumentException만 직접 사용한다. UnityEngine/Random, DiceRules.BestHand/Supports, RunSession/저장/API는 호출하지 않는다.
- 사용하는 쪽/관계 근거: DiceResultFeedback이 확정 DiceRollUIData를 표시할 때 참여 오라를 결정하도록 공개한 계약이다. 실제 연결은 메인이 같은 Task의 feedback 파일에 통합한다. DiceAuraTests는 reflection으로 공개 Groups를 호출해 독립 기대 배열과 대조한다.
- 상태/수명: static 가변 상태·캐시·정적 난수 없이 호출마다 counts/result를 소유한다. 후보 전체 성립 확인 전에 부분 그룹을 반환하지 않는다. 잘못된 입력에서는 반환값이 없다.
- 검수 주의: 같은 눈 4개를 서로 다른 두 페어로 쪼개지 않으며 ThreePairs는 6개 입력 제약으로 2+2+2일 때만 가능하다. 표시 helper이므로 입력된 hand가 가능한 더 높은 hand와 달라도 해당 hand의 최소 기여만 표시한다. 반대로 규칙 데이터의 hand를 새로 선택하지 않는다.
- 검증 상태: 메인이 artifacts/dice-combo-aura/red.json에서 missing-helper 실제 RED 1건을 확인한 뒤 구현했다. 보조는 Unity 실행을 하지 않았으며 현재 GREEN/통합 자산 검증은 NOT_RUN이다. 최종 실행·판정은 DICE_COMBO_AURA_REPORT를 따른다.
