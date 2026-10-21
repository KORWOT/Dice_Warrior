# MockGameBackend.cs

- 역할: IGameBackend의 개발용 권한 대역. 영구 성장·출전 선택·서버 시드·런 식별자를 검증/생성하고 새 제출을 독립 재현 후 정산한다.
- 핵심 동작: 시작은 버전·revision·단일 활성 런·보유 규칙의 주사위/무기를 검증하고 고정 규칙 및 시작 프로필 사본을 담은 티켓을 반환한다. 성공한 시작·성장·첫 제출은 프로필 revision을 증가시킨다. 성장은 HP 종류, 활성 런 없음, 비용 `20*(growth+1)`, 잔액과 상한을 권한 프로필로 검사한다.
- 정산: 소유 런·활성 런·revision·명령 형식 확인 후 저장된 시작 티켓으로 MockRunVerifier를 실행한다. 거절은 자산과 활성 런을 유지한다. 승인 시 지급·영수증·런 종료·작업 응답을 함께 원자 저장한다. 새 ID의 같은 런 제출도 원 영수증과 현재 프로필만 반환한다. 기존 P1 pending은 검증/지급 없이 유지한다.
- 입출력: `MockGameBackend(MockAuthorityStore, Func<uint> seedSource=null, Rules rules=null, Func<long> nowMilliseconds=null)`; 모든 IGameBackend 메서드는 Task<BackendReply>와 CancellationToken을 사용한다. 기본 시드는 RandomNumberGenerator의 0 아닌 uint, 시각은 UTC Unix 밀리초다. 주입 규칙은 생성 때 복제·검증하나 정산은 시작 때 동결한 규칙만 사용한다. 요청 DTO에 클라이언트 사용자 ID·시드·최종 결과·지급액 입력은 없다.
- 사용하는 대상: MockAuthorityStore.Execute, MockRunVerifier.Verify, BackendContracts, LoopJson, LoopRules.ValidateRules. 사용하는 쪽: GameBackendFactory 및 GameBackendCoordinator의 IGameBackend 경계, EditMode 검증.
- 관계 근거: 요청 전체 필드와 호출 작업 종류를 SHA256 fingerprint로 묶고 ID별 응답을 권한 파일에 함께 기록한다. 같은 ID/본문은 최초 응답을 그대로 복제 반환하고, 다른 본문은 REQUEST_ID_REUSED로 거절한다. 버전·revision 등 확정 거절도 작업 한도 내에서 저장한다. 잘못된 ID/초대형 요청/작업 한도 거절은 보관하지 않는다.
- 상태/수명: GetProfile은 최신 권한 프로필과 활성 티켓 사본을 반환한다. GetOperationStatus는 저장된 결과 또는 OPERATION_NOT_FOUND를 반환한다. 없음은 성공을 의미하지 않으며 호출자가 같은 요청 ID로 재전송한다. 파일 작업은 즉시 실행한 뒤 완료 Task를 돌려주고, 파일 접근 실패만 TemporaryFailure로 변환한다.
- 빈도: 권한 파일의 고정 60초 창에서 시작 10/새 제출 20/승인된 포기 10회를 제한한다. 같은 ID 재시도와 정산된 런 영수증 조회는 소모하지 않는다. 역행 시계는 창을 초기화하지 않는다. 제한에 걸린 요청 ID의 확정 거절은 시간이 지나도 그대로다.
- 검수 주의: 2048명령/512KiB/128런/1024작업 한도를 자동 삭제로 우회하지 않는다. 취소/저장 실패 후 원 요청으로 결과를 조회하며 새 ID로 자동 변경하지 않는다. 합법적인 대체 로그·자동 플레이를 판별하거나 Mock 파일 자체 변조를 방어하는 운영 보안 경계는 아니다.
- P3 읽기 복구: GetRunStatusAsync는 소유 런의 티켓·제출 명령/영수증·최신 프로필 사본을 반환한다. 새 작업 ID·빈도 소모·지급·파일 재작성은 없다. P1 pending도 재검증하지 않는다. 일반 작업 응답에는 전체 로그를 중복 저장하지 않으며 복구 조회에서만 포함한다. 두 세션의 동시 정산/구매는 MockAuthorityStore의 동일 경로 트랜잭션과 revision으로 직렬화한다.
