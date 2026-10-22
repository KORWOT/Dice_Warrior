# CompetitionCombat.cs

- 역할: 경기 시작 시 동결된 규칙 버전에 따라 개인 전투를 공통 엔진에 연결한다.
- 핵심 동작: 참가자의 전투 필드를 임시 Run으로 매핑한다. CompetitionDefinition의 shared-world-preview-1은 정확전체, preview-2는3눈 내2눈 부분조합을 허용한다. hp/turn/lastTurn을 반영하고 enemy는 후보 소유 참조를 사용한다.
- 입출력: Resolve(CompetitionDefinition,ParticipantState,Symbol[])는 버전별 판정을 제공하며 알 수 없는 버전을 거절한다. 기존 Resolve(Rules,ParticipantState,Symbol[])는 정확전체 동작을 유지한다.
- 사용하는 대상: SymbolCombat.Resolve와 기존 Run/TurnLog.
- 사용하는 쪽: CompetitionEncounterRules.Combat 및 어댑터 동치 검사.
- 관계 근거: 전투 처리의 직접 호출. 이동/지역/보드/완주/정산 함수는 호출하지 않는다.
- 상태/수명: 임시 Run은 호출 동안만 존재하며 저장하지 않는다. 호출자는 후보 상태 소유권을 보장한다.
- 검수: 오래된 저장은 동결된v1로 계속 재생한다. v2와v1 모두 기존 피해공식·장비 태그·보호막·반격·처치 후 생략을 유지한다. 기존84개 기호 배열 동치와 신규 사용자예시·부분조합·버전경계를 별도 검사한다.
