# CompetitionAi.cs

- 역할: 전쟁·지혜·행운 정책의 합법 선택과 AI 한 차례의 유한 실행.
- 핵심 동작: 동일 LegalChoices에 콘텐츠 정수 가중치를 적용한다. 낮은 HP의 회복 우선, 필요 없는 중복 장비/주사위·만피 회복 가중치0, 동점은 개인 AI RNG로 결정한다.
- 입출력: Choose(state,participant) → offered choice ID; 내부 PlayTurn → 이동/HP/명예/완주/결과 ParticipantSummary.
- 사용하는 대상: CompetitionCommandRules.ApplyParticipant, CompetitionEncounterRules.LegalChoices, CompetitionRandom.
- 사용하는 쪽: CompetitionCommandRules.CompleteRound, 도메인/골든 검사.
- 관계 근거: AI도 사람과 같은 ApplyParticipant travel/choose/combat/lap을 호출한다.
- 상태/수명: 명령의 후보 상태 안에서만 동기 실행한다. 이동+선택/완주+최대64전투턴의 한 차례를 끝내고 요약한다.
- 검수: 전투 강제 승리·무상 아이템·다른 확률 없음. 완료/탈락은 호출자가 건너뛴다. 연속 실행 상한은 maxCombatTurns+3이며 초과는 후보 확정을 실패시킨다. 외부 LLM/API·벽시계 없음.
