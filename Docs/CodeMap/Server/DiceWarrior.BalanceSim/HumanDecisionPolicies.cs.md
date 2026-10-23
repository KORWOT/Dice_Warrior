# HumanDecisionPolicies.cs

- 역할: B0/C7 분석 실행기에만 쓰는 `neutral-v1` 인간 자동 입력 정책이다. 제품 AI나 규칙을 변경하지 않는다.
- 핵심 동작: 완료/탈락은 advance, Map은 travel, Combat은 combat, LapReward는 시작 주사위/세 번째 aegis, Encounter는 HP50% 미만 Heal100 또는 XP+heal+honor 최대를 선택한다. 동점은 동결 Offer 순서를 보존한다.
- 입출력: `Next(state, policyId)`는 현재 Target·다음 sequence·결정적 command ID를 가진 CompetitionCommandEntry를 반환한다. 미지원 정책/활성 단계·완료 경기·합법 선택 부재는 거절한다.
- 사용하는 대상: CompetitionRules.Human, CompetitionCommandRules.Target/LegalChoices와 읽기 전용 참가자/Offer/시작 프로필이다. RNG 조회/소비·게임 상태 변경은 없다.
- 사용하는 쪽/관계 근거: MatchRunner.Run이 다음 인간 명령을 만들 때 호출하며 MatchRunnerTests가 Offer 동점·HP 경계·상태 JSON 불변을 검사한다.
- 상태/수명: 상태를 보유하지 않는 정적 함수다. 반환 명령만 실행기가 보유한다.
- 검수 주의: Heal 우선은 50% 이하가 아닌 미만이며 값100도 일반 점수와 비교한다. ID 정렬·무작위 타이브레이크·미래 RNG 관측을 추가하지 않는다.
