# RunRulesCatalog.cs

- 역할/소속: FateDice.Core의 순수 규칙 스냅샷과 기존 규칙 열거형·정의 DTO. SO·표시 설정·엔진 속성·파일 IO를 포함하지 않는다. 기존 제작 속성의 용도는 주석에 보존했다.
- 입출력: version/dice/fate/combat/growth/world 전체 데이터에서 Validate()가 오류 경로 배열을 반환하고, Action/Enemy/Equipment/Die/Event가 안정 ID로 정확히 조회한다. 검사 범위·수치·조회 의미는 기존 GameConfigData의 규칙 부분과 같다.
- 복제: DeepCopy()는 순수 RunRulesCatalog를 새로 만들고 CopyRulesTo(target)는 각 그룹을 RulesCopy로 독립 복제한다. null/빈 배열·중첩 null과 문자열 ID는 그대로 유지한다. 대상 null은 ArgumentNullException이다.
- 직접 사용하는 대상: RulesCopy, System/LINQ 및 같은 파일의 규칙 DTO. 사용하는 쪽은 CoreRunState/RunStateCopy와 Dice/Fate/Combat/Growth/Exploration 규칙, 순수 상태 검증, RunApplication이다. Runtime GameConfigData는 상속하여 표시 설정과 표시 검증만 결합한다.
- 상태/수명: 공유 불변 카탈로그를 가장하지 않는 런별 독립 가변 DTO다. 외부 제작/표시 스냅샷을 통해 세션이 가진 정의를 변경할 수 없도록 경계에서 명시 복제한다. 저장된 실제 정의를 사용하며 최신 SO를 ID로 재조회하지 않는다.
- 검수: 신규 규칙 필드에는 검증과 RulesCopy를 함께 추가해야 한다. stable ID·등급·가중치·계수·RNG 순서가 구조 변경으로 바뀌지 않아야 한다. RA-C 실행 결과는 메인의 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.

- RA-D: Action.effects/Reward.addActionId/World.shopPriceMultipliers 필드를 정의한다. 효과는 EffectResolver.IsValid, 보상은 기존 actionId 참조, 가격은 선택적5개 양수유한값을 검증한다. null/빈 effects와 배율은 구형 의미를 유지한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
