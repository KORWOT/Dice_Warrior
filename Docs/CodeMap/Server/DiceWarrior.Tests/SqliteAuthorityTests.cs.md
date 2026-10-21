# SqliteAuthorityTests.cs

- 역할: 실제 SQLite 저장소와 MockGameBackend의 영속성·원자성·사용자 격리 회귀 검사 8개.
- 핵심 동작: 재연결 후 최초20 중복 지급 없음/원 영수증 유지, 같은 요청 ID를 사용하는 두 계정 격리, 10개 구매 경합 1회 비용, 콜백이 대기하는 동안 다른 연결 구매 성공과 CAS 재실행, 100개 새 ID 정산의 50재화1회/유니크 제약, SQLite trigger 실패에 의한 profile/receipt rollback 후 재시도, 악성 로그 무지급, 취소/손상 원문 보존을 확인한다.
- 입출력: 테스트마다 GUID 임시 DB. 기대값은 최초20/성장20/완주50, revision, 작업·정산 행 개수 및 골든 고정 명령을 사용한다.
- 사용하는 대상: SqliteAuthorityRepository, MockGameBackend, 실제 Microsoft.Data.Sqlite 연결, GoldenReplayTests.Read, NUnit. 사용하는 쪽: dotnet test DiceWarrior.Tests.
- 관계 근거: 별도 저장소 인스턴스와 연결을 동시에 사용한다. 느린 콜백의 첫 실행만 대기시켜 writer lock 밖 실행과 최신 상태 재시도를 직접 관찰한다. 테스트용 trigger는 fixture DB에만 삽입하며 실제 사용자 저장에 접근하지 않는다.
- 상태/수명: fixture마다 임시 폴더를 만들고 테스트 작업 완료 후 삭제한다. 대기 작업은 finally에서 해제한다.
- 검수 주의: HTTP 프로세스 종료/두 서버 프로세스 검사는 별도 호스트 통합 시험 책임이다. 본 검사는 같은 프로세스의 별도 SQLite 연결을 검증한다.
