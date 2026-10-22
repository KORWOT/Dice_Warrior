# CompetitionCommandRules.cs

- 역할: 인간 명령과 후속 AI 차례의 공통 결정적 실행 경계.
- 핵심 동작: travel/choose/combat/lap/abandon/advance를 검사한다. 인간 사건 종료 명령 안에서 살아 있는 AI3의 한 차례와 라운드 증가를 적용한다. 전원 완료/탈락 또는30라운드 종료 후 추가 명령은 거절한다.
- 입출력: Target(state) → match/human/round/action/lap/position/phase/offer/turn 문자열; Apply(candidate,kind,arg) → 후보 변경; LegalChoices → 현재 사건 선택지.
- 사용하는 대상: CompetitionRules/Random/EncounterRules/Ai 및 기존 Rules/Gear/DieSlot.
- 사용하는 쪽: CompetitionSession과 도메인/골든 검사. 향후 서버 C6은 이 동일 소스를 재사용할 수 있으나 현재 권한 서버에 연결하지 않았다.
- 관계 근거: Runtime Execute의 직접 Apply 호출, AI 내부 ApplyParticipant 호출.
- 상태/수명: 호출자는 깊은 후보를 소유하고 성공한 저장 뒤에만 공개한다. actionNumber는 성공한 인간 명령마다1 증가한다. AI 명령은 인간 로그에서 재생되므로 별도 제출 입력이 아니다.
- 검수: 첫2회 lap arg는 유효 주사위 ID(빈 값은 기존 순서 기본값), 세번째는 유효 장비 ID. 초과 이동은 소멸하고 시작점0에 정지한다. 인간 완료/탈락 뒤 advance로 AI만 한 라운드를 관전한다. worldRound와 전투turn을 혼동하지 않는다.
