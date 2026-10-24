# CompetitionElements.cs

- 역할: v4 단일 DamageComponent·상성·변환·전투와 읽기용 기술/AI 예측.
- 핵심 동작: 기존 SymbolCombat.Plan의 부분조합만 재사용한다. 변환 타입에서 태그/Binding을 다시 평가하고 피해·보호량을 한 곳에서 계산한다. 적 보호막·처치 뒤 생략·반격1회를 유지한다.
- 입출력: SkillPreview/ActionTags/Enemy/Resistance/ScalingText/TypeName/MatchupScore; Resolve→구형 표시 TurnLog와 v4 typed log.
- 사용하는 대상: V4Content/Attributes/Modifiers, SymbolCombat.Plan, 깊은 복사한 Participant/Enemy.
- 사용하는 쪽: CompetitionCombat v4 분기, CompetitionAi 상성 점수, ElementView/Screen.
- 상태/수명: Resolve는 로컬 사본에서 성공한 후 HP/적/턴/두 로그만 게시한다. UI/AI 조회는 난수/상태를 바꾸지 않는다.
- 관계 근거: Combat.Resolve의 버전 dispatch와 AI.Score의 v4 분기, 화면 helper의 직접 참조.
- 검수 주의: Skill.tag는 DamageType이 아니다. 변환 후 이전 화염 보정 제거, 기본효과0 보존, 반격 저항→보호막 순서. 치명타/회피/재귀 반응은 없음.

## V5_DICE_COMBAT_09 직접 변경/관계

SkillPreview(def,p,V5SkillDefinition,enemy)와 ResolvePlanned(def,p,V5SkillDefinition[])를 추가한다. 기존 v4 Symbol 계획 이후의 수치·태그·피해·보호막·생략·반격 계산을 동일한 private helper로 추출하여 V5Skills.Preview/Execute가 공유한다. v4는 기존 faces를 그대로 기록하고 v5는 빈 Symbol[]만 기록하며 실제 FaceId/물리면 결과는 v5.lastTurn에서 보존한다. 빈 계획도 적 행동을 한 번 처리한다. 기존 SkillPreview(string) 호출과 v4 계산 순서는 유지한다.
