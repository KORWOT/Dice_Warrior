# ServerProcess.cs
- 역할: 통합 검사용 실제 독립 서버 프로세스 수명.
- 핵심 동작: 저장소 기준 빌드 DLL과 고유 loopback 포트/DB를 사용해 기동하고 healthz를 기다린다. Dispose는 자신이 시작한 프로세스만 종료한다.
- 입출력: DB/seed/fault/선택 extraArguments → URL/Database 및 IDisposable 호스트.
- 사용하는 대상: Process, TcpListener, HttpClient.
- 사용하는 쪽/근거: HttpAuthorityTests/TelemetryHttpTests. extraArguments는 Metrics:Directory 실패 fixture를 주입한다.
- 상태/수명: 테스트별 고유 프로세스. 표준 출력은 제한 버퍼로 기동 실패 진단에만 사용한다.
- 검수: 사용자 서버나 실제 DB를 사용하지 않는다. process kill은 이 객체가 소유한 process만 허용한다.
