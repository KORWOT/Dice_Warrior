# MetaRequestJournalTests.cs

- 역할: 영속 미결 요청의 원문 보존·파일 실패·복구 세대와 결과 조회 계약을 검증한다.
- 동작/입출력: TEMP 프로필에서 요청 사본 변경, 다른 ID의 Begin/Clear, JSON/해시/버전/다른 프로필 오류, 쓰기 잠금 실패, 백업 복원 이전 요청/이미 포함된 영수증을 통제한다. 원본 bytes·단일 기록·무지급·조회 결과를 확인한다.
- 관계: 실제 LocalPlayerDataStore/LocalMetaRequestJournal/LocalMetaProgressionService/MetaRequestCoordinator 및 시험 기본 설정을 사용한다.
- 수명/검수: GUID TEMP 경로만 삭제한다. 여기서 GetAwaiter().GetResult는 즉시 완료되는 실제 로컬 서비스/조회용이며 지연 응답은 별도 RecoveryStabilityAsyncTests의 TCS로 검증한다. 실행 수와 결과는 RECOVERY_STABILITY_REPORT를 따른다.
