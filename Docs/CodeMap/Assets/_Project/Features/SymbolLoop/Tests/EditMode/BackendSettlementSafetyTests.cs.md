# BackendSettlementSafetyTests.cs

- 역할: P3 정산·비동기 복구·두 세션 경합의 실제 Mock 파일/조정기 경계를 검증한다.
- 핵심 동작: 완료 전 백업에서 원 영수증과 최신 성장/재화 복구, 서버의 다른 새 활성 런 복원, 승인 pending을 과거 백업에서 재시도, 실제 동시10정산/변경 본문 거절, 두 로컬 세션의 같은 revision 성장 구매 및 재로드, 정산/구매 경합을 확인한다.
- 입출력: 고정 seed33, 격리 authority/checkpoint, Task/Barrier → NUnit 단언. 전송 전/지급 후 단절·취소와 await 중 호출자 입력 변조도 주입한다.
- 사용하는 대상: MockGameBackend/MockAuthorityStore, LoopStore/Session, GameBackendCoordinator, Run DTO와 기존 Finish helper. Unity EditMode runner가 호출한다.
- 관계 근거: 임시 파일을 실제 재조회하여 잔액/revision/영수증/원문 보존을 판정한다. 두 기기 모델은 서로 다른 checkpoint와 같은 권한 경로다.
- 상태/수명: Temp/DiceWarriorSettlement/고유ID만 사용하고 허용 절대 루트 확인 후 정리한다. 동시 작업은 전용 worker와 10초 Barrier 제한을 사용한다. 사용자 저장에 접근하지 않는다.
- 검수: 실제 Firebase 인증/네트워크/두 기기나 프로세스 간 파일 공유를 검증한 것으로 해석하지 않는다. 단절은 HttpRequestException, 취소는 OperationCanceledException 고장 주입이다. 결과 파일·실행 횟수는 P3 REPORT에 기록한다.
- 늦은 응답 회귀: 첫 런의 프로필 조회를 보류한 동안 두 런을 정산한 뒤 과거 응답을 반환한다. 최신 체크포인트 바이트·두 번째 영수증·잔액120이 보존되고 오래된 조회는 준비 완료가 되지 않아야 한다.
