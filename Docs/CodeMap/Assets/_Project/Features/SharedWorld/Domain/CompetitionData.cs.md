# CompetitionData.cs

- 역할: 공동 세계 시험 모드의 버전 콘텐츠·고정 지리·개인 참가자·사건 제안·경기·인간 명령 DTO.
- 핵심 동작: 보드와 전투 규칙은 CompetitionState.definition에 한 번 보관한다. ParticipantState는 Run을 저장하지 않고 개인 HP/위치/완주/장비/주사위/전투/방문·4개 난수를 가진다.
- 입출력: CompetitionDefinition → CompetitionState, EncounterOffer/Choice, ParticipantSummary, CompetitionCommandEntry. 명령에는 sequence/kind/arg/targetId/clientCommandId만 있고 AI 결과 입력은 없다.
- 사용하는 대상: System/List 및 기존 SymbolLoop Rules/Profile/DieSlot/Enemy/TurnLog.
- 사용하는 쪽: SharedWorld 도메인, CompetitionSession/Store, CompetitionApp/Screen/Playback, 새 EditMode/.NET 검사.
- 관계 근거: 같은 기능 폴더 도메인의 직접 타입 참조; Runtime 세션의 state/commands 필드와 화면 바인딩.
- 상태/수명: 정의와 시작 프로필을 시작 시 깊은 복사한다. 개인 목록·배열을 참가자마다 생성한다. lastRound는 최근 AI 차례 요약 최대3개이다.
- 검수: 저장된 세계관 표시명과 안정 ID를 구분한다. 성장 스냅샷은 인간/AI 동일, 후원자는 전투 능력이 아닌 AI 정책 테마다. 새 DTO는 v1 PlayerDocument/BackendRequest에 추가되지 않는다.
