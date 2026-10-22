# CompetitionTags.cs

- 역할: v4의 분류 태그·Owner·출처·질의·충돌 판정을 제공하는 순수 코어. 태그 자체는 피해/수치/외부 호출을 실행하지 않는다.
- 핵심 동작: 카탈로그의 단일 부모 forest와 허용 Owner를 검증한다. Build는 Raw Grant를 보존하고 현재 stage 이하의 Grant만 같은 Owner 안에서 해소한다. Reject는 충돌을 거절하고 Priority는 최고 우선순위 태그가 하나일 때만 선택한다. 서로 다른 최고 태그의 동률은 거절한다.
- 입출력: TagDefinition/TagConflictGroup/TagGrant/TagQuery, CompetitionTags.Validate/Build → TagSnapshot. Has와 Matches는 지정 Owner의 ALL/ANY/NONE을 평가하고 includeChildren=true일 때만 부모 질의에 자식을 포함한다. WithoutSource는 해당 sourceKind/sourceId만 뺀 새 스냅샷을 만든다.
- 사용하는 대상: System, Collections, LINQ, InvariantCulture. Unity/저장/난수/네트워크와 기존 SymbolLoop DTO에 의존하지 않는다.
- 사용하는 쪽: CompetitionV4Content의 카탈로그/Binding 질의 검증과 Tags 컨텍스트 구성, CompetitionAttributes.Bindings의 조건 판정, CompetitionTagTests의 직접 API 검사.
- 관계 근거: Validate/Build/Matches 직접 호출. 같은 Domain/*.cs가 Unity SharedWorld.Core와 .NET DiceWarrior.Competition.Shared에서 공유 컴파일된다. Skill Identity나 장비/장소 태그를 Action으로 자동 전파하는 경로는 없다.
- 상태/수명: 입력 정의/그룹/Grant를 복사한다. Raw와 Effective는 읽기 전용 컨테이너뿐 아니라 매 조회 시 요소까지 복사한다. Raw는 모든 출처·억제된 태그·미래 stage를 보존하고 Effective는 Owner/tag당 최고 우선순위 대표1개다. 출처 제거 시 Raw를 다시 해소하므로 억제된 다른 태그가 복원된다.
- 검수: 정의128/그룹32/Grant512/질의 전체항목32, ID128자(영숫자 . _ - : /), 표시명128자. 스트리밍 Grant는513번째에서 중단한다. ownerKind/ownerId/sourceKind/sourceId/ordinal 중복, 미등록/순환/잘못된 Owner와 중복·겹치는 충돌 그룹을 거절한다. 정렬은 Owner→tag→sourceKind/sourceId→ordinal→stage→priority이며 문자열은 Ordinal이다. 질의는 모든 항목을 먼저 검증해 단락 평가로 잘못된 NONE/ANY를 숨기지 않는다. Reaction/치명타/회피/Modifier의 실행은 이 파일 책임이 아니다.

- TSE05-F1: Grant/Query의 Owner를 생략하면 invalid(-1)로 시작하므로 Source 기본값으로 조용히 해석하지 않고 기존 검증에서 거절한다. 실제 누락 JSON4사례 중 Owner2사례와 기존 태그11개를 함께 검사한다.
