# CompetitionGoldenTests.cs

- 역할: 동결된 인간 명령48개·경기 전체상태50개의 Unity/.NET 재현 비교.
- 핵심 동작: normal(seed456)와 abandon-spectate(seed789) 두 벡터에서 인간 kind/arg/target를 그대로 적용한다. AI 포함 전체 상태 SHA256와 각 참가자 이동/전투/사건/AI 난수를 매 명령 비교한다.
- 입출력: CompetitionGoldenVectors.json → 모든 상태/난수 비교 및 종료 확인. en-US/ko-KR/tr-TR/fr-FR 문화권, 중간 직렬화 복원 포함.
- 사용하는 대상: CompetitionRules/CommandRules/LoopJson/NUnit와 CompetitionRulesTests의 콘텐츠 로더.
- 사용하는 쪽: Unity SharedWorld.EditMode.Tests 및 .NET Competition.Tests의 동일 소스 링크.
- 관계 근거: 같은 csproj 테스트 파일 링크와 골든 JSON 출력 복사.
- 상태/수명: 골든은 읽기 전용. 누락 시 별도 출력 폴더 observation 파일만 생성하고 반드시 실패한다. 기대 파일을 자동 덮어쓰지 않는다.
- 검수: 최초 관측 이후 지명/라운드/명령 경로와 독립 uint FNV 계산의 시작 난수 일치를 확인해 동결했다. 기존 v1 골든은 변경하지 않는다. 골든 재생 성공과 실제 UI/서버 권한 검증은 별도다.
