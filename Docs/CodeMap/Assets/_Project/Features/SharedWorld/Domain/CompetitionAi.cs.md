# CompetitionAi.cs

- 역할: 전쟁·지혜·행운 정책의 합법 선택과 AI 한 차례의 유한 실행.
- 핵심 동작: 동일 LegalChoices에 콘텐츠 정수 가중치를 적용한다. 낮은 HP의 회복 우선, 필요 없는 중복 장비/주사위·만피 회복 가중치0, 동점은 개인 AI RNG로 결정한다.
- 입출력: Choose(state,participant) → offered choice ID; 내부 PlayTurn → 이동/HP/명예/완주/결과 ParticipantSummary.
- 사용하는 대상: CompetitionCommandRules.ApplyParticipant, CompetitionEncounterRules.LegalChoices, CompetitionRandom.
- 사용하는 쪽: CompetitionCommandRules.CompleteRound, 도메인/골든 검사.
- 관계 근거: AI도 사람과 같은 ApplyParticipant travel/choose/combat/lap을 호출한다.
- 상태/수명: 명령의 후보 상태 안에서만 동기 실행한다. 이동+선택/완주+최대64전투턴의 한 차례를 끝내고 요약한다.
- 검수: 전투 강제 승리·무상 아이템·다른 확률 없음. 완료/탈락은 호출자가 건너뛴다. 연속 실행 상한은 maxCombatTurns+3이며 초과는 후보 확정을 실패시킨다. 외부 LLM/API·벽시계 없음.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4에서만 합법 전투 후보에 실제 보유면의 평균 상성 점수, 지정 보물에 제한된 행운 점수를 더한다. 읽기 계산은 RNG를 소비하지 않으며 실제 동점 선택만 기존 aiRng를 사용한다. 전투는 사람과 같은 Command/Encounter/Elements를 통과한다.

## V5_DICE_COMBAT_09 직접 변경/관계

v5 전투는 V5Dice.AiCommand가 현재 공개 결과만 보고 고른 5명령을 같은 ApplyParticipant에 전달한다. 꽝이 있으면 슬롯 순 비꽝 보관→1회 리롤→실행, 꽝이 없으면 즉시 실행한다. 유한 내부 명령 상한은20×6+3이며 기존 버전의 maxCombatTurns+3은 유지한다. v5 상성 점수는 V5Skills.MatchupScore로 실제 보유6면과 전용 단일 스킬을 읽고 기존 행운/사건 선택 가중치를 공유한다.
