# CompetitionElementView.cs

- 역할: v4/v5의 모험가 카드·6스탯·HP 출처·기술 타입/계수·상성·태그와 효과 출처를 표시하는 별도 읽기 전용 도우미다. 기존 RGB CompetitionGrowthView를 변경하지 않는다.
- V5_DICE_COMBAT_09 F01: v5 상세5탭을 복원한다. PossibleV5는 보유 슬롯의 실제6면 조합(최대216개)을 v5 Plan으로 순수 열거하며 신규 전용 SkillId를 legacy 기술 배열에 조회하지 않는다. 기술별 effect를 SkillPreview/Tags에 명시 전달한다. 다음 출전 임시 참가자도 같은 읽기 경로를 쓴다.
- V5 계산/효과: 실제6면 목록 후 가능한 기술별 현재 능력치·기본값·계수·타입 전환·상성·보호막 전 결과·최종 반올림 순서를 설명한다. 효과 탭은 장착 장비/고유 능력과 현재 조건에서 적용되는 수치·규칙ID·출처를, 태그 탭은 Raw/Effective·소유 대상·출처를3개씩 표시한다. 표시에서 효과를 다시 적용하지 않는다.
- V5 적 상세: 현재 HP/최대HP/보호/기준 공격/의도, v4 enemy 정의의 공격타입/약점/저항/identityTags 이름과ID를 읽는다. 내 피해 저항·마지막 저장 결과는 다음 페이지로 분리하며 마지막 신규 기술 이름은 d.v5.skills에서 찾는다.
- 핵심 동작: Roster는 기본 제공3종의 다음 출전 예상값을 표시한다. Details는 저장 참가자의 Describe 또는 로비 Preview를 사용하고 stats/skills/elements/tags/effects를 분리한다. 기술3개·태그 Grant4개·Modifier4개씩 페이지를 나누며 현재 보유 주사위에서 가능한 기술만 기존 Plan(...,true)로 열거한다.
- 입출력: CompetitionScreenData, 동결 definition, profile, 선택 ID/참가자, 상세 탭/페이지 → 표시 문자열/6행 배열과 페이지 수. EncounterSummary는 선택 전 전투 후보의 약점/저항을, CombatSummary는 적 공격 타입/약점/저항과 마지막 저장 결과의 피해 타입/실제 피해를 짧게 반환한다.
- 사용하는 대상: CompetitionAttributes의 Preview/Describe/Label/Target, CompetitionElements의 SkillPreview/Enemy/TypeName/ActionTags/ScalingText/Resistance, TagSnapshot.Raw/Effective, 기존 SymbolCombat.Plan. 계산·태그 스냅샷은 코어 API를 사용하고 표시에서 보정을 다시 적용하지 않는다.
- 사용하는 쪽·관계 근거: CompetitionApp.Render의 명시 v4/v5 분기 → CompetitionScreen의 attributes 또는 element-text 모드. CompetitionFlowTests와 CompetitionV5FlowTests가 실제 포인터·전체 페이지·스냅샷/파일 바이트 동치로 표시의 비변경성을 검사한다.
- 상태/수명: 정적/무상태. 로비의 임시 ParticipantState는 Preview 계정HP·출전장비/주사위만 담는 표시 입력이다. Start/명령/난수/저장을 호출하지 않는다. 경기 상세에는 저장된 definition과 참가자를 전달한다.
- 검수 주의: 같은 태그 중복과 효과 중첩을 혼동하지 않는다. Raw/Effective 여부·Owner·출처를 분리해 표시한다. 타입은 기호/legacy skill.tag와 별개다. 치명타·회피는 현 v4 계약의0%, 영구 권한/보상은 제공하지 않는다. 실제 배치/검사 결과는 TAG_STAT_ELEMENT_05_REPORT의 증거를 따른다.
