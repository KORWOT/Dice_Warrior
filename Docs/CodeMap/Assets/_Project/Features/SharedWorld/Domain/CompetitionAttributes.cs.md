# CompetitionAttributes.cs

- 역할: v4 6스탯 출처·계정 HP·CON 파생 HP·장비/패시브 Binding 읽기와 참가자 일관성 검사.
- 핵심 동작: 기본+레벨+조건 Modifier를 합성한다. HP=30+CON×6+계정HP. RefreshStats는 최대HP 증가분만 현재HP에 가산하여 이중 성장을 막는다.
- 입출력: Preview/Describe→새 V4StatBreakdown, Bindings→새 정규 Modifier, RefreshStats/ValidateParticipant.
- 사용하는 대상: CompetitionV4Content/Tags/Modifiers, 동결 모험가/장비/레벨, LoopJson 깊은 복사.
- 사용하는 쪽: CompetitionRules 시작/검사/XP, EncounterRules/CommandRules 장비교체, Elements/UI.
- 상태/수명: 공유 mutable 스탯을 두지 않는다. 선택 미리보기는 임시 participant, 실제 계정HP는 경기 시작 값이다.
- 관계 근거: Rules/Encounter/Command/Elements의 직접 호출; v3 CompetitionStats는 재사용하지 않는다.
- 검수 주의: AI와 인간의 계정HP동일, 구형v4필드금지, enemyId와 선택사건/최종 일치, typed/표시 log 일치. 기본회피/치명타는 별도 계산하지 않는다.

## V5_DICE_COMBAT_09 직접 변경/관계

Preview/Describe/RefreshStats/ValidateParticipant의 버전 경계만 UsesAttributes로 확장하여 v5도 기존6스탯·HP·Modifier·typed 로그 계산/검증을 그대로 사용한다. 선택/실제 면/계획ID 정합성은 V5Content가 별도로 검증한다. 수치나 기존 typed 로그 직렬화 계약은 변경하지 않는다.
