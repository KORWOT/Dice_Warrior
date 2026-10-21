# BackendContracts.cs

- 역할: Unity 비종속 백엔드 프로토콜 1, 규칙 버전 symbol-loop-mvp-1, 검증 버전 symbol-loop-replay-1, 요청/응답/티켓/제출 DTO와 IGameBackend.
- 핵심 동작: Task와 CancellationToken 계약, 승인/거절/충돌/검증 대기/결과 불명/일시 실패/미지원 구분. DTO는 데이터만 보관한다.
- 입출력: BackendRequest(버전, ID, revision, 출전 선택, 명령 로그) → BackendReply(프로필, 시작 티켓, 영수증). 클라이언트 지급액·승리·UID·시드 권한 입력 없음.
- 사용하는 대상: System, Task, Profile/Rules. 사용하는 쪽: Session, Coordinator, Mock 저장소/어댑터/Factory, App, 검사.
- 관계 근거: IGameBackend의 직접 구현/생성자 주입과 PlayerDocument 필드 참조.
- 상태/수명: 요청은 pending에 같은 본문으로 저장, 응답/티켓은 깊은 사본으로 소비. 버전 미지원 시 거절한다.
- P2 입출력: RunVerificationResult는 accepted/code/message/failedSequence/payout/finalRun을 반환한다. SubmissionReceipt는 승인된 검증 버전·완료 지역·승리·포기와 지급액을 저장한다. Request 형식은 바뀌지 않아 P1 요청 fingerprint를 유지한다.
- 검수: 기존 P1 영수증은 PendingVerification/지급 0을 유지한다. 새 제출의 Approved는 MockRunVerifier 통과 후에만 생성한다. 타입/인터페이스 존재가 실서버 보안을 의미하지 않는다.
- P3: IGameBackend.GetRunStatusAsync(serverRunId,ct)는 읽기 전용으로 소유 런의 시작 티켓/원 영수증/저장 명령과 최신 프로필을 반환한다. BackendReply.commands는 이 복구 응답에만 담으며 제출 요청·fingerprint 형식은 그대로다. 활성 런은 submission=null이다. 저장된 P1/P2 응답에 새 필드가 없어도 null로 읽는다.
