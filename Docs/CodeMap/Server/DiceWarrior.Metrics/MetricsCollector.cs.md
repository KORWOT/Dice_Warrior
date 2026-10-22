# MetricsCollector.cs

- 역할: 독립 Mock 호스트의 요청 메타데이터를 권한 DB와 별도 폴더에 유한 보관하고, 로컬 조회용 안전한 표본을 반환한다. 서버/게임 권한을 결정하지 않는다.
- 입출력: `MetricsCollector(directory, authorityDatabasePath, MetricsOptions?)`, `Begin(endpoint)` → `RequestMeasurementScope`, `Snapshot()` → `MetricsSnapshot`. scope는 `Measurement`, `Identify(userId, runId)`, `IAuthorityTelemetry.Record`, `Dispose`를 제공한다.
- 핵심 동작: 요청 시작부터 Dispose까지 Stopwatch 벽시계, 실제 바이트의 0 값을 보존하고 완료를 한 번만 기록한다. 동시 요청 수/최대 동시 요청과 현재 호스트의 CPU 누적 차이·working set·권한 DB/WAL/SHM·계측 파일 크기를 읽는다. CPU는 재현 전용 CPU가 아니다.
- 개인정보: 폴더별 32바이트 로컬 비밀로 사용자/사용자별 런 HMAC을 만든다. raw UID/runId/requestId/body/SQL/경로를 레코드에 넣지 않는다. endpoint/outcome/authorityOutcome/domainCode/진단 종류는 상수 allowlist이며 ID·HMAC·숫자·날짜도 검증한다. 익명 키는 여러 프로세스가 동일 디렉터리 잠금 아래 최초 한 번 만들며 비밀이 손상되면 임의 교체하지 않는다.
- 보관/수명: collector별 UUID journal을 사용하고 공통 `metrics.lock`의 최대 약200ms 유한 대기로 프로세스 간 쓰기를 직렬화한다. 기본 7일, 16개×4MiB, snapshot 최대10000개 최신 레코드. 한 레코드는8KiB다. 기간·파일·표본 제한/잘못된 줄은 누락 플래그와 카운터로 표시하며 무제한 파일 읽기를 하지 않는다. creation time을 지난 journal은 통째 제거하므로 일부 최신 줄도 함께 제외될 수 있다. 모든 집계는 snapshot의 제한된 표본 기준이다.
- 카운터: completedRequests는 작은 retention 상태에 기록한 누적 완료 수이며 retained 표본 수와 다르다. droppedRecords는 보관/크기/표본 제한 및 현재 프로세스 기록 실패로 알려진 누락 수다. 손상·과대 파일은 정확한 행 수를 복구할 수 없으므로 invalidRecords/truncated를 함께 사용한다. 기록 직후 비정상 프로세스 종료의 전역 완료 카운터는 완전한 감사 원장이 아니다.
- 실패: 권한 DB와 잠금·트랜잭션을 공유하지 않는다. 디렉터리 충돌·I/O·잠금 실패는 계측의 persistenceAvailable=false와 누락으로 나타나고 authority 예외/rollback을 일으키지 않는다. 실제 파일 DB 내용은 읽거나 바꾸지 않는다. 200ms 잠금 대기는 요청 완료 지연에 포함되기 전의 계측 저장 시간이다.
- 사용하는 대상: MeasurementContracts DTO, AuthorityDiagnostic/IAuthorityTelemetry, System.Text.Json, HMACSHA256, Process, FileStream. 사용하는 쪽: MockServer HTTP 계측/개발 조회와 MetricsCollectorTests. 관계 근거는 Begin/Identify/Record/Dispose/Snapshot 호출이다.
- 검수 주의: 로컬 디렉터리에 쓸 수 있는 신뢰 경계 안에서 다중 프로세스를 지원한다. HMAC은 인증/암호화가 아니며 로컬 키 노출 시 익명성이 약해진다. 계측 journal을 과금 원장이나 Firebase 실제 청구 근거로 사용하지 않는다.
