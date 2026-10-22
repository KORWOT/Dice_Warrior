# AuthorityDiagnostic.cs
- 역할: 권한·SQLite가 호스트에 전달하는 선택적 진단 계약.
- 핵심 동작: kind/count/milliseconds 불변 관측값과 IAuthorityTelemetry.Record, 예외를 삼키는 AuthorityTelemetry.Emit을 제공한다.
- 입출력: 고정 진단 이름·횟수·벽시계 시간 → observer. 사용자/런 ID·본문·SQL 원문은 전달하지 않는다.
- 사용하는 대상: System만 사용. netstandard2.1이며 Unity/계측 파일 저장에 의존하지 않는다.
- 사용하는 쪽/근거: MockGameBackend의 replay/duplicate, SqliteAuthorityRepository의 SQL/행/transaction/CAS/busy가 Emit을 호출한다. Metrics.RequestMeasurementScope가 Record를 구현한다.
- 상태/수명: 요청에 전달하는 선택적 observer. null·observer 예외가 정산 결과를 바꾸지 않는다.
- 검수: 진단 값은 보상·fingerprint·저장 계약에 사용하지 않는다. 새 kind는 collector allowlist와 함께 정의한다.
