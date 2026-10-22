# GameBackendCoordinator.cs

- 역할: 로컬 체크포인트와 비동기 IGameBackend 사이의 미결 요청/승인 적용 조정.
- 핵심 동작: 요청 사본 고정 → pending 저장 → 작업 상태 조회 → 동일 본문 발송/fingerprint 확인 → 정산 런 조회 → 승인 응답 로컬 저장 → 지정 ID pending 제거 → 최신 권한 재확인. 결과 불명은 보존, 확정 거절은 오류/입력 원문 보존 후 지정 ID만 해제한다.
- 입출력: LoopSession/LoopStore/IGameBackend 주입, InitializeAsync/ExecuteAsync, Busy/ProfileReady/LastReply.
- 사용하는 대상: Session, Store, BackendContracts. 사용하는 쪽: SharedWorld의 CompetitionAccount, 격리 통합 검사.
- 관계 근거: CompetitionAccount가 계정 세션과 함께 생성하며 서버 조회/영구 성장/기존 미결 요청 재확인에 사용한다. 신규 공동 경기 명령은 CompetitionSession으로 분리한다.
- 상태/수명: Interlocked로 같은 조정기의 동시 진입을 차단한다. 초기화는 pending이 있으면 캐시 revision을 바꾸지 않는다. Cancellation/Timeout/HttpRequestException은 결과 불명으로 원 요청과 예외 진단을 보존한다. 이미 로컬 승인 저장까지 끝난 뒤 최신 프로필 조회만 실패하면 승인 결과를 유지하고 재조회 필요로 표시한다.
- 검수: 서버 승인 후 로컬 저장 실패는 ID를 바꾸지 않는다. 단순 조회만으로 다른 요청 본문을 승인하지 않는다. 같은 프로세스의 두 세션/격리 로컬 경로를 검증하며 실서버/실제 다중 기기는 후속 범위다.

- 초기 조회는 활성 RunTicket도 대조하여 시작 전 백업 복원을 복구한다. 서버에 확인된 작업이 없는 stale 로컬 토큰만 확정 거절로 보존/해제하며, 이미 승인된 요청은 로컬 token 갱신 후에도 같은 응답을 적용한다.
- P3 복구: 로컬 미완료 런과 서버 활성 런이 다르면 GetRunStatus로 해당 런의 영수증을 먼저 확인한다. 완료 전 백업을 서버 결과로 합치기 전에 PreserveCheckpoint로 원문을 보존하고, 최신 프로필을 다시 읽은 뒤 다른 세션의 새 활성 런을 복원한다. 일치하는 활성 런의 로컬 진행은 유지한다. 조회 실패/불일치는 임의 초기화하지 않는다.
- 검증 관계: BackendSettlementSafetyTests가 과거 백업/새 활성 런/단절/동시 요청/입력 객체 변조를 확인한다. BackendReplayIntegrationTests는 지급 후 기기 저장 실패 복구를 계속 확인한다.
