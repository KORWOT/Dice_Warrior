# Program.cs
- 역할: 독립 ASP.NET Core 개발 Mock 호스트 진입점.
- 핵심 동작: Development 확인 → 설정/콘텐츠 검사 → loopback·512KiB 제한 → MetricsCollector 생성 → 권한6개 및 /dev API 등록.
- 입출력: CLI/환경 설정 → 로컬 HTTP listener. Metrics:Directory 생략 시 권한 DB 옆 metrics 폴더 사용.
- 사용하는 대상: MockServerSettings, AuthorityEndpoints, MetricsEndpoints, MetricsCollector, ASP.NET Core.
- 사용하는 쪽/근거: Start-MockServer.ps1/dotnet run/ServerProcess. 프로젝트는 Storage.Sqlite와 Metrics를 참조한다.
- 상태/수명: Collector는 호스트당 하나, 요청 scope는 별도. 계측 폴더 장애는 기동/권한 호출을 막지 않는다.
- 검수: warning 수준·예외 타입 로그로 요청/신원/경로 원문을 피한다. Firebase 서비스 호출·운영 인증·외부 바인딩은 없다.
