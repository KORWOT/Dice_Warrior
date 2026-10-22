# CompetitionRules.cs

- 역할: 새 경기 시작·성장·명예·순위·전체 상태 검증의 순수 규칙.
- 핵심 동작: 정의·시작 프로필을 깊은 복사하고 인간1+AI3에 같은 시작 성장 HP/출전 값을 적용한다. 개인 컬렉션과 RNG는 따로 만든다. Rank는 시련성공→완주→명예→HP 내림차순, 같은 값은 공동순위다.
- 입출력: Start(definition,profile,seed,matchId,adventurerId=\"wanderer\") → CompetitionState; Human/Rankings은 현재 참가자 조회; Validate는 저장 가능한 구조 검사.
- 사용하는 대상: CompetitionContent/Random, LoopJson, 기존 Rules/Profile.
- 사용하는 쪽: CompetitionSession/Store 시작·검증, Command/Encounter 규칙, Presentation, Unity/.NET 검사.
- 관계 근거: 신규 Runtime과 도메인의 직접 호출. v1 Run/PlayerDocument 검증에는 연결하지 않는다.
- 상태/수명: 순수 함수/후보 변경. XP 공식은 기존 레벨별8배 요구량, 최대HP+6/공격+1/방어+1을 따른다. 명예는 고유 원천 ID 목록과 합계로 보관한다.
- 검수: 개인 완주3/주사위1~3/장비 슬롯·ID/명예 합계/claimed 연속 ID/탈락·완료·전투 상태를 확인한다. 구조 검사는 서버 권한 정산의 대체물이 아니다. 신규 경기의 영구 재화는 지급하지 않는다.
