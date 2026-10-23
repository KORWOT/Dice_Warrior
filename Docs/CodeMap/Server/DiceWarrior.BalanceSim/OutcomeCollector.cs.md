# OutcomeCollector.cs

- 역할: 인간 Apply 직전/직후 읽기 관찰, 종료 계약 검증, 참가자 결과 행 및 관찰 DTO 생성이다.
- 핵심 동작: travel 후 제시/합법 사건, choose 선택/비용/실제 골드와 고정 XP·명예·제시 heal, lap 선택/인벤토리 변화, 전투 진입/턴/스킬/피해 타입/적별 누계를 수집한다. ValidateEnded는 실제 종료 원인과 Rankings의 공동순위를 확인한다.
- 입출력: Before/After는 collector의 HumanObservations만 갱신한다. Participants는 rank1인 k명에 각각1/k를 기록하고 HP/최대HP/비율, 시련/탈락/완주/잔여XP/레벨/명예/골드를 투영한다.
- 사용하는 대상: CompetitionRules.Human/Validate/Rankings, CommandRules.LegalChoices, ParticipantState의 일반/typed 마지막 인간 전투 로그와 현재 Offer/장비/주사위다.
- 사용하는 쪽/관계 근거: MatchRunner가 각 Apply의 양측에서 호출하고 ReportWriter/Statistics가 MatchResult.Observations와 참가자 행을 읽는다. MatchRunnerTests의 인공 전투/동률 fixture가 과잉피해·방어흡수·생략·분할점수를 검사한다.
- 상태/수명: 경기마다 하나이며 직전 인간 단계/선택/장비/주사위/골드 스냅샷과 누계만 보유한다. 정상 경기는 상세 체크포인트/전투 로그 대신 스킬별 실제 피해 표본만 보관한다.
- 검수 주의: 실제 HP 피해는 typed.damageApplied, 보호막 흡수는 해당 SkillLog의 enemyShieldBefore-after다. damageBeforeShield-실제 피해는 overkill을 포함하므로 흡수로 계산하지 않는다. 반격 피해는 reward/level 회복과 섞인 상태 HP차가 아니라 retaliationDamage를 사용한다. AI 전체 기술/사건/생애 최대 전투턴은 NOT_MEASURED/null이며 마지막 AI 로그를 전체 이력으로 간주하지 않는다.
