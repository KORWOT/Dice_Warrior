# CompetitionModifiers.cs

- 역할: 출처별 수치 보정을 정규화하고 정수 만분율로 한 번 반올림하는 순수 계산기.
- 핵심 동작: 동일 출처/규칙/대상 중복은1회, 내용 다른 중복/같은 그룹 정책 충돌 거절. UniqueBySource/Stackable/HighestWins 후 Flat→PercentAdd 합산→FinalMultiply 그룹→상성을 적용한다.
- 입출력: Normalize, Evaluate, EvaluateScaled; 값은 새 배열/정수 결과로 반환.
- 사용하는 대상: CompetitionTags.ValidateId, v4 Modifier DTO, BigInteger의 유한 정수 유리수.
- 사용하는 쪽: V4Content Binding 검증, Attributes/Elements 수치 합성, ModifierTests.
- 상태/수명: 호출 내 지역 계산만 하며 입력 컬렉션/DTO를 변경하지 않는다.
- 관계 근거: 실제 호출은 Content.ValidateBindings, Attributes.Bindings/Describe, Elements.SkillPreview/Resistance/Resolve.
- 검수 주의: 64항목/ID128/수치범위를 확인하고65번째 열거에서 중단한다. 중간 반올림이 없고 최종 양수 AwayFromZero, 음수0, 결과1억 초과는 거절. 임의 실행식/코드 없음.
