# CompetitionContent.cs

- 역할: 별도 shared-world-preview-1 콘텐츠를 읽고 세계/사건/AI 정의의 구조·참조를 검사한다.
- 핵심 동작: 신규 rules/AI/score 버전을 v1과 분리한다. 12개의 고유 지명, 0번 출발점, 각 개인 단계의 최소2개 사건 및 무료 합법 선택지, 장비/주사위 참조, 4개 이상 모험가와 정책3개를 검사한다.
- 입출력: Load(string json) → CompetitionDefinition; Validate(definition)는 부적합 입력을 예외로 거절한다.
- 사용하는 대상: LoopJson, LoopRules.ValidateRules, CompetitionData, LINQ.
- 사용하는 쪽: CompetitionRules.Start/Validate, 새 Runtime/Editor의 콘텐츠 로딩 및 도메인 검사.
- 관계 근거: CompetitionRules의 직접 호출, Content/shared-world-preview-1.json과 같은 필드 계약.
- 상태/수명: 정적 상수와 순수 검사만 보관한다. 파일/Unity API/네트워크 접근이 없다.
- 검수: 실제 기본값은 콘텐츠의 30라운드/64전투턴/3완주이다. 검사 범위는 최대30/64이며 테스트용 더 작은 상한도 유효하다. 콘텐츠는 별도 세계·버전이며 기존 symbol-loop-mvp-1.json을 대체하지 않는다.
