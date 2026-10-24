# CompetitionV5Data.cs

- 역할: v5 문자열 FaceId, 6면 배치, 스킬 레시피, 전투 선택 상태와 확정 로그·순수 예상 DTO.
- 핵심 동작: V5Definition은 faces/diceLayouts/skills와 리롤1·보관변경12·AI 정책 버전을 동결한다. V5SelectionState는 AwaitRoll/Selection, 전투 턴 번호·리롤·굴림 번호·보관 변경 횟수와 결과 배열을 저장한다.
- 입출력: V5FaceDefinition/V5DieLayout/V5SkillDefinition, V5ParticipantState/V5SelectionState/V5FaceResult/V5CombatLog, V5CombatPreview/V5PreviewSkill 및 비저장 V5PlannedSkill. 결과는 dieInstanceId/slotIndex/faceIndex/faceId/held를 함께 보존한다.
- 사용하는 대상: Newtonsoft.Json Required, 기존 DamageType/V4SkillDefinition. Symbol enum과 기존 Rules/Skill 직렬화는 확장하지 않는다.
- 사용하는 쪽: CompetitionDefinition.v5/ParticipantState.v5, V5Content/Dice/Skills, V5 화면·재생 및 저장/복원 검사.
- 관계 근거: Content의 검증, Dice의 명령 reducer, Skills의 Plan/Preview/Execute가 이 DTO를 직접 읽고 쓴다.
- 상태/수명: 정의는 경기 시작에 복사된다. 선택은 전투 중에만 존재하며 실행 뒤 다음 AwaitRoll 또는 null로 바뀐다. 확정 로그는 다음 이동/전투 시작까지 유지한다. 실행 애니메이션 단계는 저장하지 않는다.
- 검수 주의: 새 부모 필드만 null 직렬화를 생략하여 v1~v4 JSON을 보존한다. v5 필수 필드는 Required.Always/AllowNull로 누락을 거절한다. Preview는 현재 선택의 실제 HP 피해·흡수·보호·생략 상태를 반환하며 RNG를 포함하지 않는다.

## V5_UI_IMPROVEMENT_11

V5PlannedSkill은 skill 정의와 dieSlotIndices를 가진 순간 조회 DTO다. V5PreviewSkill에도 같은 슬롯 배열을 추가했다. Participant/Selection/CombatLog 저장 스키마는 불변이며 정의 복사나 명령 기록에 이 두 표시 DTO를 넣지 않는다. CompetitionV5Skills.PlanSteps/Preview가 생산하고 전투 표시와 신규 순수 투영 검사가 소비한다.
