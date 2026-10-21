# GameBackendFactory.cs

- 역할: 명시된 BackendMode로 IGameBackend 구현을 선택하는 단일 생성 경계.
- 핵심 동작: HttpMock은 설정 파일을 읽어 HttpGameBackend를 생성한다. legacy Mock enum은 명시적으로 거절하며 테스트는 Authority DLL의 구현을 직접 주입한다. Firebase는 전 API에 Unsupported/FIREBASE_NOT_CONFIGURED를 반환한다.
- 입출력: `Create(mode, settingsPath, seedSource=null, developmentAllowed=false)` → IGameBackend. 기존 Firebase 호출 서명은 유지한다. HTTP 시드는 클라이언트에서 지정할 수 없다.
- 사용하는 대상: BackendContracts, HttpBackendSettings, HttpGameBackend, Task/CancellationToken. 사용하는 쪽: SymbolLoopApp과 backend 경계 검증.
- 관계 근거: 앱은 생성된 IGameBackend를 GameBackendCoordinator에 주입한다. Firebase 미설정 어댑터도 GetProfile/StartRun/SubmitRun/Upgrade/GetOperationStatus/GetRunStatus 전체 계약에 명시적 Unsupported를 제공한다.
- 상태/수명: HTTP 전송은 생성한 앱이 dispose한다. Firebase 어댑터는 상태가 없으며 호출 전 취소 토큰을 검사한다.
- 검수 주의: Firebase 실패 또는 미지원 상태에서 Mock으로 자동 전환하는 경로는 없다. Firebase SDK·인증·배포는 이번 범위 밖이다.
- 플레이어 Factory에는 MockAuthorityStore/MockGameBackend/MockRunVerifier 참조가 없으며 HTTP 미응답도 인프로세스 Mock으로 전환하지 않는다.
