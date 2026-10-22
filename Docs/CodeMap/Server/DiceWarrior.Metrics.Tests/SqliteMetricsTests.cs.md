# SqliteMetricsTests.cs

- 역할: SQLite 실측 진단과 선택적 observer 장애의 권한 무영향을 검증한다.
- 핵심 동작: 프로필 조회의 실제3SELECT/1반환행/1읽기트랜잭션, 실패 영수증 INSERT의 rollback, 별도 연결 경쟁의 CAS 재시도/폐기 transaction, 예외 observer에서도 한 번 구매·동일 영수증 반환·중복 소비 없음 확인. 동결 victory 명령 로그를 실제 backend+SQLite의 throwing observer로 정산하고 같은 작업/새 작업 ID의 같은 런 재제출에도50보상 한 번·최종70잔고를 확인한다.
- 입출력/상태: UUID 임시 폴더의 SQLite DB만 사용하고 종료 시 해당 경로를 삭제한다. trigger 장애 주입은 fixture DB에만 설치한다. Sink는 진단만 수집하고 ThrowingSink는 매번 IOException을 던진다.
- 사용하는 대상: SqliteAuthorityRepository, MockGameBackend, AuthorityDiagnostic, Microsoft.Data.Sqlite, NUnit. 사용하는 쪽: DiceWarrior.Metrics.Tests. 실제 SQL 명령과 BackendReply/프로필 잔고가 검증 근거다.
- 검수 주의: provider 내부에서 처리한 busy 재시도 횟수를 추측하지 않는다. 직접 SELECT counter assertion은 초기 스키마·프로필 생성 이후 수집한다. 실패 command도 실행 시도이며 rollback을 통해 최종 상태와 구별한다.
