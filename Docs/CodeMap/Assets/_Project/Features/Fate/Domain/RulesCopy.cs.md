# RulesCopy.cs

- 역할/소속: FateDice.Core의 규칙 정의별 명시 깊은 복제. JSON·Reflection·Unity·파일 IO 없이 모든 필드를 직접 옮긴다.
- 입출력: Hand/Die/GradeRow/Dice/Fate/Action/Intent/Enemy/Combat/Tag/Modifier/Equipment/Level/Reward/Event/Product/Growth/World는 같은 DTO 형식의 독립 복사본을 반환한다. 객체 null, 배열 null, 빈 배열, 배열 안의 null, 문자열의 null/빈 값은 보존한다.
- 직접 관계: RunRulesCatalog.CopyRulesTo가 다섯 설정 그룹을 복제한다. RunStateCopy와 RunApplication은 Reward로 대기 보상/규칙 보상을 분리한다. 각 DTO는 RunRulesCatalog.cs에서 정의한다.
- 상태/수명: 정적 가변 상태가 없고 원본 및 RNG를 변경하지 않는다. primitive/string 배열은 배열 자체를 복제하고 참조형 정의 배열은 원소까지 새로 만든다. 불변 문자열만 공유한다.
- 검수: 새 정의 필드는 해당 복제 함수에 반드시 추가한다. null을 기본 객체로 정규화하거나 ID/숫자/배열 순서를 보정하지 않는다. 신규 효과/보상 필드가 이후 추가될 때도 이 복제를 함께 확장한다. 통합 실행은 메인 REPORT에서 확인한다.

- RA-D: Effect 원소/effects 배열/addActionId/가격배율 배열을 명시 복사하며 기존 null/빈 값 의미와 순서를 유지한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
