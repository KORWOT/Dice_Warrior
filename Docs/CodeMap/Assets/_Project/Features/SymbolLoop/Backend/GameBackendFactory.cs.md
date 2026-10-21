# GameBackendFactory.cs

- 역할: 명시된 BackendMode로 IGameBackend 구현을 선택하는 단일 생성 경계.
- 핵심 동작: Mock이면 주입된 authorityPath의 MockAuthorityStore와 MockGameBackend를 생성한다. Firebase이면 모든 API에 Unsupported/FIREBASE_NOT_CONFIGURED를 반환하는 어댑터를 생성한다. 알 수 없는 enum 값은 ArgumentOutOfRangeException.
- 입출력: `Create(BackendMode mode, string authorityPath, Func<uint> seedSource=null)` → IGameBackend. Firebase 경로는 파일을 만들거나 Mock 초기 재화를 지급하지 않는다.
- 사용하는 대상: BackendContracts, MockAuthorityStore, MockGameBackend, Task/CancellationToken. 사용하는 쪽: SymbolLoopApp과 backend 경계 검증.
- 관계 근거: 앱은 생성된 IGameBackend를 GameBackendCoordinator에 주입한다. Firebase 미설정 어댑터도 GetProfile/StartRun/SubmitRun/Upgrade/GetOperationStatus/GetRunStatus 전체 계약에 명시적 Unsupported를 제공한다.
- 상태/수명: 반환된 어댑터는 소유 앱/coordinator 수명을 따른다. Firebase 어댑터는 상태가 없으며 호출 전 취소 토큰을 검사한다.
- 검수 주의: Firebase 실패 또는 미지원 상태에서 Mock으로 자동 전환하는 경로는 없다. Firebase SDK·인증·배포는 이번 범위 밖이다.
