# CompetitionV4Content.cs

- 역할: v4 콘텐츠의 완전성/참조/상한 검증과 문맥별 태그 스냅샷 조립.
- 핵심 동작: 3모험가·모든 기술/장비·10일반적+최종 적의 exact coverage, 계수합/Owner/조건/중첩/속성 변환 위치를 검사한다. Source/Skill/Action/Equipment/Place/Target/DamageComponent를 각각 구성한다.
- 입출력: Validate(definition), IsEnabled, ElementTag, Tags(def,p,skillId,type,enemy).
- 사용하는 대상: CompetitionTags, CompetitionModifiers, v4 DTO와 기존 동결 combat/world.
- 사용하는 쪽: CompetitionContent.Validate, CompetitionAttributes.Describe/Bindings, CompetitionElements.SkillPreview.
- 상태/수명: 사용할 때 읽기 스냅샷을 만들며 정적 가변 경기/계정 상태를 두지 않는다.
- 관계 근거: Content.Validate의 직접 호출과 Attributes/Elements의 Tags 호출로 연결된다.
- 검수 주의: 구형 정의는 v4 payload를 거절한다. Source 스탯 보정은 Source/Equipment 조건만 허용해 행동→기본스탯 재귀 의존을 막는다. 태그가 효과를 직접 실행하지 않는다.
