# AuthorityEndpoints.cs

- 역할: IGameBackend의6개 HTTP 경로와 healthz를 독립 권한 서비스에 연결.
- 핵심 동작: 개발 신원·경로/요청 ID·메서드종류·512KiB JSON·중복키/추가필드 검사 후 계정별 SQLite/MockGameBackend 호출. 정산 경로와 body runId가 일치해야 한다.
- 입출력: HTTP JSON→BackendReply. 도메인 결과는200/BackendStatus, 형식 오류400/413/415, 신원401, 저장 불가503. unknown5xx/단절은 클라이언트 pending 보존 대상으로 본다.
- 사용하는 대상/쪽: DevelopmentIdentityResolver, SqliteAuthorityRepository, MockGameBackend, LoopJson; Program이 등록한다.
- 관계 근거: 조회는 계정 저장소의 런/operation만 반환한다. body는 userId를 정의하지 않으며 결과/재화 추가 필드를 거절한다.
- 상태/수명: 요청별 저장소/서비스. RequestAborted 전달, 응답 후단 장애가 지급 트랜잭션을 되돌리지 않는다.
- 검수: 개발 fault 옵션은 기본false이며 활성화한 loopback 서버에서만 before-abort/after-abort/after-delay를 수행한다. 로그는 예외 타입만 출력한다. POST와GET ID문법은 같아야 한다.
