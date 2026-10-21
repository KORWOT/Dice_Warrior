# HttpBackendSettings.cs

- 역할: 독립 개발 Mock 서버의 로컬 접속 설정 및 체크포인트 격리 키.
- 핵심 동작: 16KiB 이하 JSON 설정을 읽고 파일이 없으면 `http://127.0.0.1:5087`, `mock-player-01`, 15초를 사용한다. Editor/개발 실행 여부, 루프백 원점, 계정 ID, 1~120초 제한 시간을 검사한다.
- 입출력: endpoint/userId/timeoutSeconds, 로컬 설정 경로, 영속 저장 루트 → 검증 URI 또는 `symbol-loop-backend-v1/http-mock/{SHA256(endpoint+user)}/checkpoint.json`.
- 사용하는 대상: LoopJson, System.IO/Net/Uri. 사용하는 쪽: Factory, HttpGameBackend, SymbolLoopApp, HttpGameBackendTests와 FlowTests.
- 관계 근거: App이 persistentDataPath의 http-settings.json을 읽어 OpenHttp에 전달한다. 전송 생성자는 Validate를 호출하고 checkpoint 경로는 동일한 정규화 원점+계정에 대응한다.
- 상태/수명: 설정 객체는 값만 보관한다. HttpGameBackend는 생성 시 endpoint·계정·timeout 값을 고정하므로 이후 설정 객체 변경이 실행 중 전송에 영향을 주지 않는다.
- 검수 주의: 기존 mock 체크포인트/authority 파일은 열거나 이관하지 않는다. credentials/path/query/fragment 및 비루프백 서버를 거절한다. 개발 ID는 실사용자 인증이 아니며 배포 모드에서 사용할 수 없다.
- ID 계약: 개발 계정은 `[A-Za-z0-9_-]{1,64}`, 요청/런 ID는 `[A-Za-z0-9._:-]{1,128}`이다. 같은 검증 함수를 POST와 GET 경로에 사용한다. 서버 신원/경로 계약과 경계값을 맞춘다.
