# AuthorityEndpoints.cs
- 역할: IGameBackend의6개 HTTP 경로와 healthz를 독립 권한 서비스에 연결하고 게임 API 요청만 계측한다.
- 핵심 동작: 개발 신원·경로/요청 ID·작업·512KiB JSON·중복키/추가필드를 검사한다. 정산 경로와 body runId가 일치해야 한다. 요청별 SQLite/MockGameBackend에 같은 observer를 전달한다.
- 입출력: HTTP JSON → BackendReply. 도메인200, 형식400/413/415, 신원401, 저장 불가503. body는 userId나 지급 결과를 입력받지 않는다.
- 사용하는 대상/쪽: Program이 등록; DevelopmentIdentityResolver/SqliteAuthorityRepository/MockGameBackend/LoopJson/MeasuredHttpStream/MetricsCollector 사용.
- 관계 근거: scope가 실제 body bytes·로그 직렬화 크기·명령 수·응답 완료와 authorityOutcome을 기록한다. Identify는 로컬 HMAC만 남긴다.
- 상태/수명: 요청별 scope를 finally/Dispose로 완료하고 원본 body를 복구한다. 권한 진입 직전 temporary-failure를 기록해 초기화 실패/취소를 미산정으로 유지하며 확정 reply로만 덮는다.
- 검수: before-abort는 권한 진입 전, after-abort/delay는 승인 결과가 남을 수 있다. 전송 실패와 정산 결과를 혼동하지 않는다. fault 기본false, 로그는 예외 타입만 출력하며 metrics 오류는 정산을 되돌리지 않는다.
