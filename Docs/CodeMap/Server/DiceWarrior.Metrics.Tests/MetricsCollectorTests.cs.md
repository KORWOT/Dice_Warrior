# MetricsCollectorTests.cs

- 역할: 계측 보관/집계/개인정보/실패 경계를 검증하는 NUnit 테스트.
- 핵심 동작: 재시작 HMAC 안정성·동시 cold start·완료 중복 방지·실제0바이트·파일/표본/기간 한도·위조/과대 journal 필드 차단·디렉터리 장애·진단 allowlist·active/peak·UTC 날짜/nearest-rank 백분위수와 중복 제거를 검사한다.
- 입출력/상태: 테스트마다 OS 임시 폴더에 고유 UUID fixture를 만들고 TearDown에서 그 폴더만 지운다. 사용자 DB/키/저장을 읽거나 변경하지 않는다.
- 사용하는 대상: MetricsCollector/Scope, MetricsAggregation, MeasurementContracts, AuthorityDiagnostic, NUnit. 사용하는 쪽: DiceWarrior.Metrics.Tests의 dotnet test. 실제 파일 직렬화 재로드와 병렬 collector 생성이 관계 근거다.
- 검수 주의: 동시 collector는 같은 프로세스 병렬이지만 독립 객체/파일 잠금을 사용한다. 별도 호스트 프로세스 HTTP 관측은 루트 통합 테스트 책임이다. 테스트 통과가 Firebase 요금/운영 DAU를 보증하지 않는다.
