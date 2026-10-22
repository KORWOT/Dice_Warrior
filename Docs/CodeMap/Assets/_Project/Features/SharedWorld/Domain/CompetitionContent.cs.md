# CompetitionContent.cs

- 역할: 공동 경기의 세계/사건/AI/모험가·RGB 정의와 버전 검사.
- 핵심 동작: 기존 12개 지명/무료 합법 사건/장비·주사위 참조 검사에 v3 로스터·기본 스탯·패시브 태그·기술 계수를 추가한다. v1/v2는 기존 4종 이상, v3는 wanderer/scholar/vanguard 3종과 war/wisdom/luck 정책을 요구한다.
- 입출력: Load(json) → CompetitionDefinition; Validate(definition)는 잘못된 정의를 ArgumentException으로 거절한다.
- 사용하는 대상: LoopJson, LoopRules.ValidateRules, CompetitionData, CompetitionStats.IsEnabled, LINQ.
- 사용하는 쪽: CompetitionRules.Start/Validate, CompetitionStats.Preview, Runtime/Editor 콘텐츠 로딩과 검사.
- 관계 근거: 직접 호출 및 Content/shared-world-preview-1.json(기존v2)/shared-world-preview-3.json(신규v3)의 필드 계약.
- 상태/수명: 상수와 순수 검사만 소유하며 파일/Unity/네트워크 접근이 없다. RulesVersion=v2, LegacyVersion=v1을 유지하고 StatsVersion=v3을 추가한다.
- 검수: v3만 nullable growth/stats를 허용하며 누락·중복/없는 기술/0 분모/잘못된 태그를 거절한다. 현재 승인된 레벨 증가 HP6/RGB1을 요구한다. 기존 JSON 경로/GUID·v1/v2 콘텐츠와 골든을 변경하지 않으며 저장된 정의를 자동 승격하지 않는다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4 버전을 별도로 허용하고 v4 3모험가 및 CompetitionV4Content.Validate를 호출한다. 기존 ValidateGrowth와 v3 산식은 그대로이며 새 버전을 RGB 검사로 보내지 않는다.
