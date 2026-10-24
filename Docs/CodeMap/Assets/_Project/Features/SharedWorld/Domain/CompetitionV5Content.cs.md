# CompetitionV5Content.cs

- 역할: shared-world-preview-5의 콘텐츠·참가자 상태와 물리적인 6면 결과 정합성을 검사한다.
- 핵심 동작: 공용4/직업3/꽝1의 ID·소유권, 각 die의 공용4/직업1/꽝1, 레시피 중복·참조·수치·타입·스케일·태그를 검사한다. $class를 동결 adventurerId의 직업 면으로 해석한다.
- 입출력: IsEnabled/Validate, Face(def,id), Faces(def,adventurerId,dieDefinitionId)→6개 정의, ValidateParticipant(state,p). 내부 ValidateResults는 슬롯·인스턴스·물리면 인덱스·FaceId 일치를 요구한다.
- 사용하는 대상: CompetitionV4Content.ValidateEffect의 공통 수치/태그 검증, V5Skills.Plan의 확정 레시피 검증, BackendProtocol.MaxCommands.
- 사용하는 쪽: CompetitionContent/Rules, V5Dice/Skills, v5 얼굴/6면 UI와 검사.
- 관계 근거: Content.Validate와 Rules.Validate의 직접 호출, Dice가 변이 전 참가자를 검사하고 Skills가 입력 결과를 검사한다.
- 상태/수명: 읽기 전용이다. 비-v5 정의/참가자에 v5 payload가 있으면 거절한다. v5 maxCombatTurns20·리롤1·보관12·정책 버전, matchId≤100·actionNumber≤2048을 요구한다.
- 검수 주의: 빈칸/알 수 없는 enum·타입·면·외부 직업면으로 fallback하지 않는다. 선택은 Combat에서만 존재하고 AwaitRoll은 빈 결과, Selection은 1~2번 굴림과 남은 리롤의 일치가 필요하다. lastTurn은 실제 결과에서 재계산한 계획 및 기존 표시 로그와 일치해야 한다.
