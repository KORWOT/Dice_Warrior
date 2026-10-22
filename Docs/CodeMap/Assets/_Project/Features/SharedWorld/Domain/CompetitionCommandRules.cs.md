# CompetitionCommandRules.cs

- 역할: 인간 명령과 후속 AI 차례의 공통 결정적 실행 경계.
- 핵심 동작: travel/choose/combat/lap/abandon/advance를 검사하고 인간 사건 종료 명령 안에서 활성 AI의 한 차례와 라운드 증가를 적용한다. 전원 완료/탈락 또는30라운드 종료 후 추가 명령을 거절한다.
- 입출력: Target(state) → match/human/round/action/lap/position/phase/offer/turn 문자열; Apply(candidate,kind,arg) → 후보 변경; LegalChoices → 현재 사건 선택지.
- 사용하는 대상: CompetitionRules/Random/EncounterRules/Ai, 기존 Rules/Gear/DieSlot.
- 사용하는 쪽: CompetitionSession과 도메인/골든 검사. 현재 서버 C6 권한에는 연결하지 않는다.
- 관계 근거: Runtime Execute와 AI 내부 ApplyParticipant의 직접 호출. 완주 보상 AddXp에 state.definition을 전달해 동결 버전의 성장 규칙을 사용한다.
- 상태/수명: 호출자는 깊은 후보를 소유하고 저장 성공 뒤 공개한다. actionNumber는 인간 명령마다1 증가하며 AI는 인간 로그에서 재생한다.
- 검수: 첫2회 lap arg는 유효 주사위 ID, 세번째는 장비 ID. 초과 이동은 소멸하고 시작점0에 정지한다. 인간 완료/탈락 뒤 advance로 AI를 관전한다. v3의 성장 연결 외에 명령/종료/난수 소비는 바꾸지 않는다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4 이동 시 enemyId/typed로그만 함께 비운다. 최종 장비 보상 후 파생스탯을 갱신하고 final 프로필을 연결한다. 세션 후보→검증→원자저장과 명시Restart 흐름은 그대로다.
