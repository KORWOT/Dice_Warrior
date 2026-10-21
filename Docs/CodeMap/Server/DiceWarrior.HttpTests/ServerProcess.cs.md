# ServerProcess.cs

- 역할: HTTP 통합 검사가 소유하는 독립 dotnet 서버 프로세스 수명.
- 핵심 동작: 저장소의 빌드된 호스트를 빈 루프백 포트/고유 DB/seed/fault 설정으로 실행하고 healthz가 준비될 때까지 제한된 시간 대기. 실패/Dispose에서 자신이 시작한 프로세스만 종료한다.
- 입출력: 임시 DB/seed→URL과 IDisposable. stdout/stderr는 초기 오류용 최대4KiB만 보관.
- 사용하는 대상/쪽: System.Diagnostics.Process/HttpClient/TcpListener; HttpAuthorityTests.
- 관계 근거: UI/Unity 없이 실제 OS 프로세스와 HTTP 소켓을 시험한다.
- 상태/수명: 테스트 using 범위, 강제 종료로 SQLite WAL 재시작 복구도 확인. Dispose는 반복 가능하다.
- 검수: 임의 기존 서버/Editor를 종료하지 않는다. 포트 선택 후 바인딩 실패는 재시도가 아니라 명시적 시험 실패다.
