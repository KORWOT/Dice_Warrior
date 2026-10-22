# MockGameBackend.cs
- 역할: IGameBackend 권한 서비스. 프로필 조회·런 시작·재현 정산·영구 성장·원 작업/런 조회를 처리한다.
- 핵심 동작: 입력 스냅샷과 fingerprint, 요청 ID 멱등성, revision/rate/보관 상한 검사. 저장된 시작 티켓과 로그를 MockRunVerifier로 재현하고 지급/영수증을 repository 변경 단위로 확정한다. P1 대기 영수증은 지급0으로 보존한다.
- 입출력: BackendRequest/CancellationToken → BackendReply. 실제 저장은 IAuthorityRepository가 담당한다.
- 사용하는 대상: AuthorityValidation, LoopJson, Rules, MockRunVerifier, AuthorityTelemetry.
- 사용하는 쪽/근거: AuthorityEndpoints가 SQLite와 요청별 observer를 주입한다. 기존 서버/Unity Editor 검사는 observer 생략이 가능하다.
- 상태/수명: store/seed/rules/clock/telemetry는 생성 시 주입. 재현 상태는 요청 안에서 새로 생성하며 호출자 객체를 권한 상태에 보관하지 않는다.
- 계측: 동일 fingerprint 작업 재사용은 duplicate-operation, 이미 제출한 런의 새 ID는 duplicate-submission. 실제 Verify 호출만 Stopwatch와 finally로 replay 명령 수·벽시계 합계를 보고한다. CAS로 callback이 재실행되면 실제 재현도 다시 계측된다.
- 검수: observer 예외 무영향. 진단으로 지급/빈도/fingerprint 의미를 바꾸지 않는다. 플레이어에는 포함하지 않고 Editor 전용 테스트 DLL만 재생성한다.
