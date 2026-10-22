# CompetitionCombat.cs

- 역할: 신규 개인 전투 상태를 기존 정확 기호 전투 엔진에 연결한다.
- 핵심 동작: Rules와 참가자의 hp/power/guard/equipment/enemy/turn을 임시 Run으로 매핑해 SymbolCombat.Resolve를 호출하고 hp/turn/lastTurn을 반영한다. enemy는 후보 참가자 소유 참조를 그대로 사용한다.
- 입출력: Resolve(Rules,ParticipantState,Symbol[]) → TurnLog와 전달한 후보 참가자의 전투 변경.
- 사용하는 대상: SymbolCombat.Resolve와 기존 Run/TurnLog.
- 사용하는 쪽: CompetitionEncounterRules.Combat 및 어댑터 동치 검사.
- 관계 근거: 전투 처리의 직접 호출. 이동/지역/보드/완주/정산 함수는 호출하지 않는다.
- 상태/수명: 임시 Run은 호출 동안만 존재하며 저장하지 않는다. 호출자는 후보 상태 소유권을 보장한다.
- 검수: v1 전투 공식·1~3기호·장비 태그·보호막·반격·처치 후 기술 생략 유지. 모든84개 기호 배열에 대해 기존 엔진 로그/HP/적 상태와 비교한다.
