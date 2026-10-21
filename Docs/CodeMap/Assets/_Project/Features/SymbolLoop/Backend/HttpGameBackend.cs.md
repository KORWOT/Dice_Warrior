# HttpGameBackend.cs

- 역할: Unity 비종속 `IGameBackend` HTTP 전송 구현. 서버 권한 저장·시드·정산 계산은 포함하지 않는다.
- 핵심 동작: 프로필, 시작, 제출, 성장, 작업 조회, 런 조회의 6개 v1 경로를 호출한다. POST 본문은 첫 await 전에 LoopJson으로 고정한다. 개발 신원은 검증된 루프백 주소의 `X-DiceWarrior-Dev-User` 헤더로만 전송한다.
- 입출력: `HttpBackendSettings`, 명시적 개발 실행 허용, 선택적 테스트 HttpMessageHandler → BackendReply. 요청 2MiB/응답 8MiB, 응답 상태 enum/필수 status·code/중복·미지 JSON 필드를 검사한다.
- 사용하는 대상: System.Net.Http, Newtonsoft, LoopJson, BackendContracts, HttpBackendSettings. 사용하는 쪽: GameBackendFactory, App.OpenHttp, EditMode 전송 검사, 실제 HTTP PlayMode 검사 및 서버 통합 시험의 링크 컴파일.
- 관계 근거: IGameBackend 6메서드 구현과 Factory/Create 및 테스트 생성자 직접 호출. Coordinator는 전송 예외를 결과 불명으로 분류하고 원 pending을 보존한다.
- 상태/수명: 인스턴스별 HttpClient를 소유하며 Dispose로 종료한다. 자동 리다이렉트·프록시·쿠키·기본 자격 증명은 끈다. 연결/5xx/3xx/잘못된 응답은 HttpRequestException, 내부 시간 초과는 TimeoutException, 호출자 취소는 OperationCanceledException이다.
- 검수 주의: 서버 미응답에서 로컬 Mock으로 전환하지 않는다. 게임 결과를 HTTP 승인 코드만으로 확정하지 않고 DTO 상태를 확인한다. 주입 handler는 명시적 테스트 전용이다. 실제 Firebase/Android/WebGL 전송 검증은 이 범위 밖이다.
- 요청 ID는 전송 전에 HttpBackendSettings.ValidateId로 검사하며 조회 경로와 같은 문자·길이 제한을 적용한다. 서버가 수락한 작업을 같은 ID로 조회할 수 있어야 한다.
