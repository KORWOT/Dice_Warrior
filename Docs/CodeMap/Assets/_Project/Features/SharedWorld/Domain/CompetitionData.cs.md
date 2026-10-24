# CompetitionData.cs

- 역할: 공동 경기의 버전 콘텐츠·고정 지리·모험가·개인 참가자·사건·인간 명령 DTO.
- 핵심 동작: definition에 세계와 규칙을 동결한다. ParticipantState는 Run 없이 개인 HP/위치/완주/장비/주사위/방문과 4개 RNG를 가진다. v3는 definition.growth(레벨 증가와 기술 계수), adventurer.stats(기본 RGB와 패시브), participant.growth(accountHp)를 추가한다.
- 입출력: CompetitionDefinition/CompetitionState, EncounterOffer/Choice, ParticipantSummary, CompetitionCommandEntry. 명령은 sequence/kind/arg/targetId/clientCommandId이며 AI 결과 입력이 없다.
- 사용하는 대상: System/List, Newtonsoft.Json의 필드별 NullValueHandling, SymbolLoop Rules/Profile/DieSlot/Enemy/TurnLog.
- 사용하는 쪽: SharedWorld 도메인, CompetitionSession/Store, CompetitionApp/Screen/Playback, Unity/.NET 검사.
- 관계 근거: 도메인의 직접 타입 참조, Runtime state/commands 필드와 화면 바인딩. 신규 타입은 서버 계정/BackendRequest에 연결되지 않는다.
- 상태/수명: 시작 시 정의·프로필을 깊은 복사한다. v3 현재 RGB는 동결 기본값+런 레벨에서 계산하며 기존 power/guard는 STR/GRD의 검증 가능한 저장값이다. accountHp는 기존 profile.growth×5를 동결한다. lastRound는 AI 요약 최대3개다.
- 검수: 새 중첩 필드만 null 직렬화를 생략하여 v1/v2 JSON/해시를 보호한다. 기존 Rules/Skill/Profile과 전역 LoopJson 설정은 변경하지 않는다. 후원자 정책과 모험가 능력, 영구 계정 성장과 런 레벨을 구분한다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

nullable definition.v4와 participant.v4만 추가해 v1~v3 JSON/hash를 그대로 보존한다. 신규 자료는 CompetitionV4Data로 분리한다. 실제 상태는 정의·레벨·장비에서6스탯을 파생하고 typed 결과/enemyId/계정HP를 보존한다.

## V5_DICE_COMBAT_09 직접 변경/관계

definition.v5와 participant.v5를 각각 V5Definition/V5ParticipantState의 nullable 필드로 추가한다. null 직렬화를 생략하여 기존 v1~v4 JSON/hash를 보존한다. 신규 FaceId·선택·로그 DTO는 CompetitionV5Data에만 정의하며 기존 Symbol/Rules/Skill/TurnLog 필드는 변경하지 않는다.
