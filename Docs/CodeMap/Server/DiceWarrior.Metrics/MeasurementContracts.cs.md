# MeasurementContracts.cs
- 역할: 로컬 요청 측정·보관 옵션·스냅샷·집계의 공통 DTO.
- 핵심 동작: 실제 HTTP 바이트/시간/status와 authorityOutcome·transport outcome을 분리한다. 진단 kind별 count/milliseconds와 HMAC 가명 키만 보관한다.
- 입출력: AuthorityEndpoints/RequestMeasurementScope가 RequestMeasurement를 채우고 Collector/Aggregation/MetricsEndpoints가 스냅샷·통계를 소비한다.
- 사용하는 대상: BCL 자료구조와 DateTimeOffset. 게임 DTO 또는 저장 스키마와 무관하다.
- 사용하는 쪽/근거: Collector의 bounded journal 직렬화, OperationModel의 명시적 endpoint/outcome 가정, dashboard API의 필드 계약.
- 상태/수명: RequestMeasurement는 요청 수명. Snapshot은 조회 시 새 객체이며 보관 구간과 현재 프로세스 게이지의 범위를 명시한다.
- 검수: completedRequests 누적 메타데이터와 제한 표본 requests를 구분한다. 프로세스 CPU는 개별 재현 CPU가 아니다. raw ID·본문 필드를 추가하지 않는다.
