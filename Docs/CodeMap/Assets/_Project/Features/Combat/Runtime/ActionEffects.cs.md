# ActionEffects.cs

- 역할/입출력/직접 관계/상태 수명: 아래 RA-D 계약을 따른다.

- ActionEffectDefinition과 Damage/Block 명시 처리기, EffectResolver가 기존 태그·등급·반올림으로 계산해 합산한다. CombatRules.Evaluate/Resolve가 같은 평가를 사용한다. null/빈 effects는 구형 두 계수, 명시 배열이 권위이며 IsValid가 미지원/음수/비유한/전체0을 거절한다. 규칙 RNG/상태를 쓰지 않고 결과 ActionEffect만 반환한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
