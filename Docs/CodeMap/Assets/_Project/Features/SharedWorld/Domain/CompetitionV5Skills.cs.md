# CompetitionV5Skills.cs

- 역할: 문자열 FaceId 기반의 결정적 스킬 계획과 실제 계산을 공유하는 예상/실행.
- 핵심 동작: 현재 물리면 결과를 검증하고 계획에서만 꽝을 제외한다. 정확한3면 조합 우선, 높은 priority와 SkillId Ordinal 순의2면 조합 하나, 남은 슬롯 순 단일 스킬을 선택한다. 전부 꽝이면 빈 계획이다.
- 입출력: Plan(def,p,results)→V5SkillDefinition[], Preview(def,p)→V5CombatPreview. 내부 Execute는 실제 결과·계획ID·noOp를 보존하고 MatchupScore는 보유6면의 평균 단일 피해 상성을 조회한다.
- 사용하는 대상: V5Content.ValidateResults/Faces/Signature, CompetitionElements.ResolvePlanned/SkillPreview, LoopJson의 참가자 복사.
- 사용하는 쪽: V5Dice의 실행, V5Content의 저장 로그 검증, CompetitionAi.Score, V5 UI/Playback/검사.
- 관계 근거: 직접 Plan/Preview/Execute/MatchupScore 호출. v4 수치/태그/Modifier 계산을 복제하지 않고 Elements overload를 사용한다.
- 상태/수명: Plan과 Preview는 원본을 바꾸지 않는다. Preview는 사본에서 동일 resolver를 사용하여 보호막 소모·치명 처치 뒤 생략을 반영한다. Execute만 실제 participant와 v5.lastTurn을 변경한다.
- 검수 주의: 꽝을 기존 Symbol 값으로 대체하지 않는다. 모두 꽝도 실제 실행에서는 적 행동1회가 진행된다. v5 확정 얼굴은 V5CombatLog에, typed 수치는 v4.lastTurn에, 표시 로그는 기존 lastTurn에 남으며 구형 faces 배열은 빈 배열이다. 읽기 조회는 난수·보상·AI 차례를 실행하지 않는다.
