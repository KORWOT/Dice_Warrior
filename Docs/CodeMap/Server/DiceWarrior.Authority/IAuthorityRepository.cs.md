# IAuthorityRepository.cs

- 역할: 권한 서비스와 저장 기술 사이의 사용자별 원자 처리 계약, 공개 권한 문서/런/작업/변경 DTO, 공통 저장 검증.
- 핵심 동작: `Execute(Func<MockAuthorityDocument,MockMutation>,CancellationToken)`은 분리된 상태를 콜백에 주고 changed가 참일 때 저장 성공 후 응답을 공개한다. CAS 저장소는 콜백을 재실행할 수 있으므로 외부 부수 효과를 넣지 않는다.
- 입출력: 문서에는 프로필·활성 런·시작 티켓·제출 명령/영수증·ID별 응답·60초 빈도 창을 포함한다. BackendReply는 완료된 저장 작업의 값이다.
- 사용하는 대상: BackendContracts, LoopJson, LoopRules, MockGameBackend.ValidCommands. 사용하는 쪽: MockGameBackend, MockAuthorityStore, SqliteAuthorityRepository.
- 관계 근거: 기존 파일 저장 검증과 상한을 같은 AuthorityValidation으로 추출했다. Validate(document,userId)는 프로필/시작 티켓/작업 응답의 계정 일치, revision, 지급 산식, 명령 형식, 활성/완료 상태, 이력 상한을 확인한다. 기본 userId는 기존 mock-player-01이며 SQLite는 인증된 개발 ID를 전달한다.
- 상태/수명: DTO는 호출 스냅샷이며 저장소 밖의 공유 가변 상태가 아니다. 상한은 128런·1024작업·512KiB 제출·10000성장이다.
- 검수 주의: 인터페이스는 인증이나 원격 신뢰를 대신하지 않는다. 사용자별 저장소 생성과 신원 검증은 HTTP 호스트 책임이며 손상 문서를 초기화하는 계약이 없다.
