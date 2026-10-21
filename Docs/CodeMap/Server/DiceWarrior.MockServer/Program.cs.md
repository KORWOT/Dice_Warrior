# Program.cs

- 역할: 별도 ASP.NET Core 개발 서버 진입점.
- 핵심 동작: Development 환경 검사→설정/콘텐츠 검증→loopback 바인딩·512KiB Kestrel 한도→6개 경로 등록→서버 실행.
- 입출력: CLI/환경 설정→HTTP listener. Production 환경은 기동 실패한다.
- 사용하는 대상/쪽: MockServerSettings, AuthorityEndpoints, ASP.NET Core; dotnet run/Start-MockServer.ps1/실제 프로세스 검사가 실행한다.
- 관계 근거: 호스트 프로젝트는 Storage.Sqlite→Authority→Shared를 참조한다. Unity/Scene 참조 없음.
- 상태/수명: 프로세스 수명. 기본 로그를 warning으로 제한해 경로/요청 본문/신원 헤더를 출력하지 않는다.
- 검수: Firebase/운영 서버가 아니다. 개발 계정 헤더를 인증으로 해석하지 않는다.
