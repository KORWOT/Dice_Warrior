# CompetitionElements.cs

- 역할: v4 단일 DamageComponent·상성·변환·전투와 읽기용 기술/AI 예측.
- 핵심 동작: 기존 SymbolCombat.Plan의 부분조합만 재사용한다. 변환 타입에서 태그/Binding을 다시 평가하고 피해·보호량을 한 곳에서 계산한다. 적 보호막·처치 뒤 생략·반격1회를 유지한다.
- 입출력: SkillPreview/ActionTags/Enemy/Resistance/ScalingText/TypeName/MatchupScore; Resolve→구형 표시 TurnLog와 v4 typed log.
- 사용하는 대상: V4Content/Attributes/Modifiers, SymbolCombat.Plan, 깊은 복사한 Participant/Enemy.
- 사용하는 쪽: CompetitionCombat v4 분기, CompetitionAi 상성 점수, ElementView/Screen.
- 상태/수명: Resolve는 로컬 사본에서 성공한 후 HP/적/턴/두 로그만 게시한다. UI/AI 조회는 난수/상태를 바꾸지 않는다.
- 관계 근거: Combat.Resolve의 버전 dispatch와 AI.Score의 v4 분기, 화면 helper의 직접 참조.
- 검수 주의: Skill.tag는 DamageType이 아니다. 변환 후 이전 화염 보정 제거, 기본효과0 보존, 반격 저항→보호막 순서. 치명타/회피/재귀 반응은 없음.
