# CompetitionCombat.cs

- 역할: 동결된 경기 버전에 따라 개인 전투를 공통 SymbolCombat 엔진에 연결한다.
- 핵심 동작: v1은 정확전체 조합, v2는 부분 조합과 기존 power/guard, v3는 부분 조합과 기술별 RGB 계수/모험가 패시브를 사용한다. 참가자 전투 필드만 임시 Run에 담고 HP/turn/lastTurn을 반영한다.
- 입출력: Resolve(CompetitionDefinition,ParticipantState,Symbol[])는 알려진 v1/v2/v3만 수용한다. 기존 Resolve(Rules,ParticipantState,Symbol[])는 정확전체/기존 공식을 유지한다.
- 사용하는 대상: CompetitionStats.Describe/DamageBonus/BlockBonus, SymbolCombat.Resolve, Run/TurnLog.
- 사용하는 쪽: CompetitionEncounterRules.Combat 및 Unity/.NET 전투 검사.
- 관계 근거: 인간/AI 공통 사건 전투의 직접 호출. 이동·지역·보드·보상·정산에 임시 Run을 사용하지 않는다.
- 상태/수명: 호출 중 RGB 사본을 한 번 계산한다. enemy는 호출자가 소유한 후보 참조이며 임시 Run을 저장하지 않는다.
- 검수: v3 계수 합산 후 정수 내림1회, 태그 장비/패시브 가산, 보호막 흡수·HP 차감·처치 이후 생략·반격1회는 공통 엔진에서 처리한다. 기본 피해/보호0에는 보너스가 새 효과를 만들지 않는다. 저장된 v1/v2를 자동 승격하지 않는다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4는 CompetitionElements.Resolve로 분기한다. SymbolCombat.Resolve의 기존 태그 장비 자동가산을 호출하지 않아 중복 효과를 피한다. v1/v2/v3 어댑터와 전투 RNG 소비는 유지한다.
