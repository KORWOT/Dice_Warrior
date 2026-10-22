# CompetitionV4Data.cs

- 역할: v4 전용 태그/6스탯/피해 타입/Binding/typed 전투 결과 DTO.
- 핵심 동작: AttributeValues는6정수, DamageType은4종이다. identityTags와 grantedActionTags, 속성과 수치 target을 별도로 가진다.
- 입출력: CompetitionV4Definition, V4ParticipantState, V4StatBreakdown, V4CombatLog/SkillResult.
- 사용하는 대상: TagDefinition/Query, 순수 System/List. Unity나 계정 백엔드 의존 없음.
- 사용하는 쪽: V4Content/Attributes/Modifiers/Elements, CompetitionData nullable 필드, ElementView와 검사.
- 상태/수명: 정의는 시작 때 CompetitionDefinition과 함께 동결, Participant에는 accountHp/enemyId/마지막 typed 결과만 저장.
- 관계 근거: 실제 참조는 신규 코어·표시 helper의 타입 서명과 CompetitionData.v4 필드.
- 검수 주의: 구형 DTO/JSON에 필드를 끼워 넣지 않는다. 치명타/회피/Reaction 권한 타입은 이번에 없다.

- TSE05-F1: 기술 피해 타입과 적 공격 타입의 누락 기본값은 invalid(-1)이며 콘텐츠 검증이 거절한다. Physical로 자동 취급하지 않는다.
