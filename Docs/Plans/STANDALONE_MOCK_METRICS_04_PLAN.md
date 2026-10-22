# STANDALONE_MOCK_METRICS_04 PLAN

IMPLEMENT · 2026-10-22 · 완료. 승인: M4 사용량 계측·비용 추정·로컬 대시보드와 문서. 실제 Firebase 연결 금지. 원안5절/M4를 구현하며 M5 규모 부하 시험·M6 배포·게임 변경·Git 커밋은 제외한다. [REPORT](../Reports/STANDALONE_MOCK_METRICS_04_REPORT.md)에 최신 검증과 한계를 기록했다.

## 목표·고정·AC

- 기준 main/f51a4782d8aefd984f80243fa0c194b02ba85102, staged 없음. 사용자 RP2/폰트/ProjectSettings/Packages2/원본 제안7개와 Assets 게임 소스·Scene/Prefab·Packages·기존 완료 문서/증거를 보호한다. 기준 해시는 Temp가 아닌 artifacts/standalone-metrics-04/before.json에 보관한다.
- Mock 실제 HTTP/SQLite/프로세스CPU·메모리/파일 크기, Firestore 논리 작업 가정, 운영 실측 없음의 세 범주를 분리한다. SQL 횟수를 Firestore 문서 수로 변환하지 않는다. 재현 벽시계는 실제 호출만 계측하며 프로세스CPU를 개별 재현 CPU로 오인 표시하지 않는다.
- 메타데이터만 기록한다. body/로그/토큰/UID/runId/requestId/DB 경로 전문을 저장·표시하지 않는다. 로컬 비밀 HMAC의 가명 사용자/런 키로만 DAU·런별 집계한다. 계측 파일은 권한 DB와 분리하고 유한 보관·크기·누락 표시를 둔다. 기록 실패는 정산을 실패/rollback시키지 않는다.
- Development+loopback만 /dev/dashboard, /dev/metrics, /dev/metrics/export, /dev/costs를 제공한다. 외부 JS/CDN·Firebase SDK·서비스 호출 없음. 쓰기 API와 연결하지 않는 조회/시나리오 계산 화면이다.
- 요금 JSON은 지역/에디션/통화/확인 날짜/공식 출처와 가정을 갖는다. 추정은 저장량·전송·Cloud Run CPU/메모리/요청을 별도 항목으로 표시한다. 미지원 지역/에디션/누락 요율은 무료0으로 위장하지 않고 미지원/미산정으로 표시한다. 무료 할당량은 명시 선택으로만 차감한다. 사용자가 서울 Standard/USD·무료 미차감 기본값을 확정했다. 일별/API/런 표는 실제 SQL·로그·재현과 가상 R/W/D·문서 작업 부분 비용을 비교하며 전체 시나리오 비용과 구분한다.
- 성공 조건: 정상 골든 런·동일 요청/같은 런 재제출·검증 거절/형식 오류·실제 응답 유실/재시도에서 실제 카운터와 가상 읽기/쓰기 구분, 계측 장애 무영향, 알려진 손계산 비용/무료 구간·경계, JSON/CSV 개인정보 미노출·한도, 실제 대시보드 조작/표시 확인, 기존 서버13+HTTP18 회귀, CodeMap/문서/보호 감사.

## 설계·공통 계약

- Server/DiceWarrior.Metrics(net10)은 계측 집계·유한 파일 보관·논리 OperationPlan/가격/시나리오를 담당한다. Authority(netstandard2.1)에 선택적 안전 진단 observer만 추가하여 게임 DTO·fingerprint·보상/저장 의미를 바꾸지 않는다. SQLite는 같은 observer로 SQL/행/트랜잭션/CAS/잠금 대기 관측을 보고한다.
- AuthorityDiagnostic(string kind,long count=1,double milliseconds=0), IAuthorityTelemetry.Record(AuthorityDiagnostic), AuthorityTelemetry.Emit(observer,kind,count,milliseconds)는 예외를 삼키는 계측 경계다. kind는 sql-select/sql-insert/sql-update/sql-delete/sql-other/sql-rows/transaction-begin/transaction-commit/transaction-rollback/cas-retry/sql-busy/replay/duplicate-operation/duplicate-submission. replay의 count=명령 수, milliseconds=실제 검증 벽시계 합계다.
- HTTP는 실제 읽은 요청/쓴 응답 UTF8 바이트를 관측한다(압축/헤더/TLS/클라이언트 수신 보장은 미포함). 요청별 RequestMeasurement에 endpoint, startedUtc, durationMs, requestBytes,responseBytes,statusCode,outcome,domainCode,commandCount,logBytes,userKey,runKey,diagnostics를 저장한다. endpoint는 profile/start/settle/grow/operation/run. diagnostics는 kind별 count/milliseconds 사전이다. outcome은 approved/rejected/conflict/temporary-failure/transport-error/invalid-request.
- MetricsCollector(directory,authorityDatabasePath), Begin(endpoint)→RequestMeasurementScope(IAuthorityTelemetry,IDisposable). scope.Measurement은 DTO, scope.Identify(userId,runId)는 가명 키만 설정. scope.Dispose는 한 번 완료/저장하며 실패를 외부로 던지지 않는다. Snapshot()은 MetricsSnapshot을 반환하며 records(제한된 샘플), retained/truncated/dropped/retention 설명, active/peakRequests, processCPU/processMemory, authority/metrics 파일 바이트를 제공한다. Aggregate는 날짜·endpoint·run 기준 합계/평균/P50/P95/P99/max와 가명 DAU를 반환한다.
- FirebaseOperationModel.Describe(RequestMeasurement)→OperationEstimate(reads,writes,deletes,indexEntryReads,queryMinimumReads,transactionAttempts,storageBytes,egressBytes,assumptions). 버전 문자열을 제공한다. 중복/실패/조회도 모델링하며 CAS 재시도는 가정으로 명시한다. JSON 비용 카탈로그/Scenario 입력→CostEstimator.Estimate의 최소/기준/최악 결과를 제공한다. SQL 카운터는 입력으로 사용하지 않는다.
- 서버 콘텐츠/규칙·SQLite 권한 스키마/Unity HTTP 계약은 유지한다. observer 추가로 Authority DLL이 바뀌면 기존 Editor API import를 사용하며 meta/GUID를 보존하고 Unity 컴파일/경계 및 EditMode만 직접 확인한다. 화면/플레이 변화가 없으면 PlayMode 반복은 요구하지 않는다.

## 담당·허용 경로·상한

- 메인: AuthorityDiagnostic.cs/MockGameBackend.cs 계측 지점, Metrics DTO/프로젝트/테스트 프로젝트, MockServer 호스트·HTTP 계측·/dev API·wwwroot 대시보드, HttpTests 통합, 실행 안내·공통 색인/PLAN/REPORT·직접 CodeMap·증거. Unity 직렬 실행/통합 담당.
- 보조A: MetricsCollector.cs, MetricsAggregation.cs, SqliteAuthorityRepository.cs의 선택적 observer와 실제 SQL/트랜잭션 계측, Metrics.Tests/MetricsCollectorTests.cs와 SqliteMetricsTests.cs, 해당 CodeMap. 공통 DTO/Authority/호스트/Git/Unity는 수정·실행 금지.
- 보조B: FirebaseOperationModel.cs, CostEstimator.cs, PricingCatalog.cs, Pricing/*.json, Metrics.Tests/CostEstimatorTests.cs, 해당 CodeMap. 공식 문서 요율 조사 및 출처를 함께 기록한다. collector/호스트/Unity/Git 금지.
- A/B 완료 뒤 읽기 검토C: 숫자 의미·계측 실패 경계·개인정보·추정/실측 혼동과 회귀만 검토. 쓰기 금지. 동시 최대3(메인 포함), 누적3위임, 재귀 금지. 파일당1작성자. 새 UI는 별도 서버 정적 자산만 허용한다.
- artifacts/standalone-metrics-04 및 Docs/Reports/Evidence/StandaloneMetrics의 격리 fixture/증거를 허용한다. 사용자 저장/실제 서버 DB로 시험하지 않는다. 기존 완료 문서 재개방 없음. Git stage/commit/push/branch 없음.

## 순서·검증

1. 계약/공통 DTO와 기준 해시 → 계측/가격 모델의 실패 재현 시험 → 최소 구현.
2. HTTP/SQLite/replay 계측 연결과 /dev API·대시보드, 정상/중복/오류 샘플 수집.
3. 기존 서버·HTTP 회귀, 새 비용/계측 시험, 실제 브라우저 화면/시나리오/내보내기 확인. Unity 필요 범위 직렬 확인.
4. 최초 검토1묶음, 이후 수정·재검증 총2회. 새 일반 개선은 후속으로 두며 필수 결함은 숨기지 않는다.
5. 최신 실제 증거/소스와 보호 해시·CodeMap·색인·REPORT를 정리하고 M4에서 종료한다.

최초 검토1/1, 통합 수정·재검증2/2. 1차: 미산정 권한 결과/CSV 누락·활성 시간 라벨 수정. 2차: 원안 M4 대비 일별/API/런별 문서 작업 부분 비용과 로그/재현 비교 연결을 보완하고 직접 HTTP/브라우저만 재확인. 최초 시험 작성 중 raw HTTP 예외를 Coordinator 결과로 잘못 기대한3건과 asmdef 필터 오기의0건 실행은 성공 근거에서 제외했다. 남은 필수 Finding0.

최종: 서버13/13, HTTP25/25, 계측/비용32/32, Unity EditMode83/83 PASS. 실제 브라우저 개요·집계·비용 변경·내보내기 경로 확인. 보호536파일 중 의도한 Editor 테스트 DLL1개만 변경, meta/GUID와 사용자7개 변경 그대로. Server28개 C# 요약 누락0, Git stage/commit/push 없음. M5·실제 Firebase/과금/클라우드 성능·Android와 이번 PlayMode 재실행은 NOT_RUN.

## 후속 커밋 승인 (2026-10-22)

완료 보고 후 사용자가 M4 커밋을 승인했다. 기존67개 M4 파일 목록으로만 staging/로컬 commit하며 사용자7개 변경은 제외한다. 구현/검증 범위를 재개방하지 않고 보호 해시·목록·기존 증거·staged diff를 확인한다. 소스 변경 없이 승인 기록만 추가하며 push/branch 변경은 하지 않는다. 기존 읽기 검토C를 커밋 목록/증거 감사에만 재사용한다.
