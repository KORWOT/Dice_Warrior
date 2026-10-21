# SymbolLoopApp.cs

- 역할: Title 조립, Mock 권한 프로필과 로컬 캠페인·결과·복구·연출 화면 연결.
- 핵심 동작: Open에서 Factory/Coordinator 생성, 프로필 재조회. Dispatch는 비동기 요청 종료 후 확정 전후 사본으로 기존 이동/전투 연출. UI는 새 제출의 재현 검증/승인 지급, 구형 P1 대기, 실패/요청 결과 불명을 구분한다.
- 입출력: authored UI/Config, Open(path,seedSource,backend,mode). seedSource는 격리 Mock 시험용 주입이며 게임 명령이 시드를 지정하지 않는다.
- 사용하는 대상: Session/Store/Coordinator/Factory/Config, Shared UI, Screen/Playback. Title 및 FlowTests가 사용한다.
- 관계 근거: 기존 Title의 App 컴포넌트/프리팹 직렬화 연결을 유지한다. 런 경로는 persistentDataPath/symbol-loop-backend-v1/mock/checkpoint.json, 독립 authority.json.
- 상태/수명: 요청 중/초기화 중/연출 중 입력을 차단한다. pause/disable은 연출만 중단하며 미결 요청을 삭제하지 않는다. 파괴는 취소를 알리고 결과 불명 요청을 보존한다.
- 검수: 기존 symbol-loop-v1 파일 접근/이관 없음. Firebase 미지원에는 시작 성공/Mock 자동 전환 없음. Android 실제 수명 주기 검증은 별도다.

- 복원은 세션 생성 성공과 별개로 디스크 pending을 먼저 확인한다. 정상 백엔드 pending은 복원 후 보존/재시도, 손상된 요청은 원문을 보존하고 자동 취소하지 않는다.
- P2 결과: `기록 검증 · 보상 받기`로 제출한다. 승인 뒤 승리/완료 지역/지급액은 서버 영수증에서 표시한다. 기존 P1 영수증은 대기/0지급으로 표시하며 자동 재제출 버튼을 만들지 않는다.
- P3: 정산 통신 중/결과 불명은 `보상 검증 대기 · 지급 결과 미확정`으로 표시한다. 재시작 뒤 같은 요청이 확정되면 결과 화면을 바로 표시한다. pause/disable은 요청을 버리지 않으며 승인 응답 유실 후에도 같은 ID로 재확인한다. FlowTests가 실제 포인터와 화면 재생성으로 확인한다.
- 백업 복원 시 자동 정리는 사전에 관찰한 로컬 pending ID를 지정한다. 백엔드 pending 또는 관찰 후 새로 생긴 요청은 제거하지 않으며 복원/정리 후 현재 pending을 다시 읽는다.
