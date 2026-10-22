# CompetitionRules.cs

- 역할: 새 경기 시작·런 성장·명예·순위·전체 상태 검증.
- 핵심 동작: 정의와 시작 프로필을 깊은 복사하고 참가자별 RNG/목록을 만든다. v1/v2는 기존 균일 스탯/AI 로스터를 유지한다. v3는 선택한 모험가와 정책별 AI(전쟁=검사/지혜=룬술사/행운=수호자)를 배치하고 동일한 계정 HP 보정·주사위·무기를 적용한다.
- 입출력: Start(definition,profile,seed,matchId,adventurerId="wanderer") → CompetitionState; Human/Rankings 조회; Validate 저장 구조 검사. 내부 AddXp(definition,participant,amount)는 동결 버전의 런 성장만 적용한다.
- 사용하는 대상: CompetitionContent/Stats/Random, LoopJson, 기존 Rules/Profile.
- 사용하는 쪽: CompetitionSession/Store, Command/Encounter 규칙, Presentation, Unity/.NET 검사.
- 관계 근거: 직접 Start/Validate/AddXp/조회 호출. v1 Run/PlayerDocument/영구 정산 검증에는 연결하지 않는다.
- 상태/수명: XP 요구량은 현재 레벨×8. 레벨마다 HP+6, v3 RGB 각+1이며 ARC는 동결 기본값과 레벨에서 계산한다. power/guard 저장값은 STR/GRD와 일치해야 한다. 새 경기는 레벨1/XP0이며 기존 accountHp만 다시 적용한다.
- 검수: 기존 완주·주사위·장비·명예·전투 검사를 유지한다. v3는 accountHp와 startingProfile 일치, 정규화 XP, 최대HP/STR/GRD 일치, AI 모험가 매핑을 검사한다. 구형 참가자는 새 growth 필드를 가질 수 없다. 순위는 시련성공→완주→명예→HP이며 동점 공동순위다. 영구 재화/권능 포인트 지급이 없다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4 시작 시 인간 선택과 AI3종/동일 계정 보정을 동결한다. 검증은 CompetitionAttributes.ValidateParticipant, XP상승은 RefreshStats로 CON 파생 HP 증가분 한 번만 적용한다. 기존 v1~v3 분기/순서는 유지한다.
