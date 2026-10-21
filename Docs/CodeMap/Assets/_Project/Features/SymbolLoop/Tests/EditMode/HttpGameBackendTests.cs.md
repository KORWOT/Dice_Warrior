# HttpGameBackendTests.cs

- 역할: HTTP 어댑터 계약·실패 보존·개발 연결 경계를 격리한 메시지 핸들러로 검증한다.
- 핵심 동작: 6개 경로/헤더/원 요청 본문, await 중 입력 변경, domain Conflict, 잘못된 JSON·status·중복 필드·미지 필드, 3xx/5xx, 응답 상한, 호출자 취소/시간 초과, Coordinator의 동일 pending 보존을 검사한다.
- 입출력: 합성 HttpResponseMessage와 요청, 임시 체크포인트 → 단언/예외. 설정 검사는 사용자/endpoint별 경로 분리, release/원격/credentials 거절과 인프로세스 Factory 미지원도 확인한다.
- 사용하는 대상: HttpGameBackend, HttpBackendSettings, Factory, Coordinator, Session/Store, NUnit, HttpMessageHandler. 사용하는 쪽: SymbolLoop.EditMode.Tests.
- 관계 근거: 실제 adapter 메서드와 Coordinator.ExecuteAsync를 호출한다. 실제 서버/SQLite/네트워크 검증은 별도 서버 시험 및 PlayMode 포인터 검사에서 수행한다.
- 상태/수명: 각 테스트의 adapter를 dispose한다. 임시 폴더는 DiceWarriorHttpTransport 아래 GUID이며 정리 시 절대 경계 검사를 한다. 실제 사용자 설정·저장에 접근하지 않는다.
- 검수 주의: 합성 handler 결과가 네트워크 성공의 증거는 아니다. HTTP 실서버 연결 검사를 대체하지 않는다.
- 취소/시간 초과 대역은 ConfigureAwait(false)를 사용하고 동기 NUnit 예외 단언에 원 Task를 반환한다. Unity 메인 스레드 문맥을 기다리는 순환 대기로 Editor가 멈추지 않게 한다.
