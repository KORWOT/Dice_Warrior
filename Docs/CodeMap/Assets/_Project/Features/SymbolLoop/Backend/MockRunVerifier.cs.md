# MockRunVerifier.cs

- 역할: P2 Mock 정산용 독립 명령 로그 검증기. 클라이언트 최종 결과 대신 권한 저장소의 RunTicket과 명령만 받는다.
- 핵심 동작: 지원 규칙 버전·시드·시작 프로필/로드아웃을 검증한 뒤 rules/profile을 깊은 복사해 새 Run을 생성한다. 최대 2048개·UTF-8 JSON 512KiB, ID/target 128자·arg 1024자, 허용 종류를 제한한다. sequence 1..N, 명령 ID 고유성, 실행 전 target, 합법적인 단계/선택/비용, 실행 후 상태를 차례로 확인한다. 결과 뒤 명령과 미완료 런을 거절한다.
- 입출력: `Verify(RunTicket, IReadOnlyList<RunCommandEntry>, CancellationToken)` → RunVerificationResult. 승인 시 독립 finalRun과 시작 티켓 산식의 checked payout, 거절 시 구체적 code/failedSequence 및 0지급·null finalRun을 반환한다. 취소는 OperationCanceledException으로 전달한다. `VerificationVersion`은 규칙 버전과 구분된 BackendProtocol.VerificationVersion의 alias다.
- 사용하는 대상: BackendContracts, LoopJson, LoopRules, RunCommandRules, MockAuthorityStore의 용량/성장 상한.
- 사용하는 쪽: MockGameBackend 새 제출 처리, RunReplayTests.
- 관계 근거: MockGameBackend가 저장소에서 찾은 티켓을 전달하며, 본 검증기는 저장소·프로필·클라이언트 상태를 직접 쓰지 않는다. RunReplayTests가 고정 기대값과 악성 입력으로 Verify를 호출한다.
- 상태/수명: 호출별 복사한 명령·규칙·프로필·런만 소유한다. 원본 티켓/로그를 변경하거나 결과와 alias하지 않는다. 명령 복사와 재현 각 루프에서 취소를 확인한다.
- 검수 주의점: 이 코드는 파일 자체 변조 방지나 실제 서버 인증이 아니다. 합법적으로 구성된 다른 로그/자동 플레이/미래 예측까지 차단하지 않는다. 기존 P1 대기 기록 유지와 원자 지급·멱등은 호출 측 책임이다. 실패 예외를 사용자 원문 그대로 노출하지 않는다.
