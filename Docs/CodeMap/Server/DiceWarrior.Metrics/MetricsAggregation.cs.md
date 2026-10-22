# MetricsAggregation.cs

- 역할: 한 MetricsSnapshot의 제한된 요청 표본을 전체/endpoint/UTC 날짜/익명 런별로 집계한다.
- 입출력: `Overall(IEnumerable<RequestMeasurement>)`는 MeasurementAggregate, `ByEndpoint/ByDay/ByRun`은 정렬된 List를 반환한다. ByRun은 익명 런 키가 없는 요청을 제외한다.
- 핵심 동작: 요청/실패/중복 요청 수, 실제 요청·응답·로그 바이트, 명령 수, 진단별 count/ms 합, 익명 사용자 고유 수와 요청 벽시계 평균·P50/P95/P99/max. percentile은 nearest rank이며 실패 요청도 포함한다. 같은 요청 안의 duplicate-operation/submission 관측이 여러 번이어도 중복 요청 수는1이다.
- 사용하는 대상: RequestMeasurement, MeasurementAggregate, DiagnosticTotal. 사용하는 쪽: MockServer 개발 metrics 응답/대시보드 및 MetricsCollectorTests. 실제 수치의 입력은 collector가 검증한 레코드이며 비용 모델 SQL 변환을 수행하지 않는다.
- 상태/수명: static 순수 집계로 원본 레코드와 전역 상태를 변경하지 않는다. 비어 있으면 평균/백분위수/최댓값0, UTC 날짜로 자정 경계를 정한다.
- 검수 주의: activeUsers는 전달 표본에서만 중복 제거한 사용자 수다. ByDay의 사용자 수를 날짜 간 합산해 전체 DAU로 쓰지 않는다. truncated 표본의 합계를 전체 부하 실측으로 오인하지 않는다.
