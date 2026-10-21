# SqliteAuthorityRepository.cs

- 역할: 독립 Mock 서버의 사용자별 SQLite 권한 저장소. IAuthorityRepository를 구현한다.
- 핵심 동작: WAL/foreign_keys/FULL 동기화/busy_timeout을 사용하는 새 연결에서 일관된 읽기 트랜잭션으로 문서와 내부 version을 가져온다. 읽기를 끝낸 후 재현 콜백·검증·JSON 준비를 수행하고, 짧은 쓰기 트랜잭션에서 version CAS를 통과한 프로필/빈도 상태·신규 런·제출·작업 영수증만 함께 저장한다. CAS 경합은 최대128회 새 스냅샷으로 재실행한다.
- 입출력: `SqliteAuthorityRepository(string path,string userId)`와 Execute. profile/userId를 고정하며 SQL 매개변수로 격리한다. 사용자 최초 INSERT만 20재화를 지급한다. 기존 authority.json을 자동 이관하거나 읽지 않는다.
- 사용하는 대상: Microsoft.Data.Sqlite, Newtonsoft.Json, IAuthorityRepository의 모델/AuthorityValidation, LoopJson. 사용하는 쪽: MockServer의 MockGameBackend와 SqliteAuthorityTests.
- 관계 근거: profiles PK(user_id), runs PK(user_id,run_id), operations PK(user_id,request_id), submissions PK(user_id,run_id) 및 UNIQUE(user_id,receipt_id), FK로 원자 영수증/사용자 연결을 고정한다. 시작 티켓/기존 작업/완료 영수증은 변경·삭제할 수 없다.
- 상태/수명: 연결은 작업별 Dispose하고 풀링하지 않는다. DB 파일은 재시작 후 유지된다. 읽기 version은 공개 profile revision과 별개이며 거절/빈도/작업 기록 변경도 CAS에 반영한다. 제출 재현 동안 쓰기 잠금은 없다.
- 실패: DB/JSON/저장 검증 실패는 IOException, 취소는 OperationCanceledException이다. 커밋 이전 예외/취소는 트랜잭션 Dispose로 rollback한다. 지원하지 않는 스키마·손상 DB는 그대로 두며 자동 reset/backup 복원하지 않는다. 최대 경합 초과는 원 요청 재시도 대상이다.
- 검수 주의: 로컬 SQLite 서버 간 원자성 범위이며 Firebase 트랜잭션 구현이 아니다. 프로세스 외 파일 편집/삭제를 복원하지 않는다. 테스트는 실제 SQLite 연결 경합, 한 영수증 유니크 제약, INSERT 실패 rollback, 재시작 및 계정 격리를 사용한다.
