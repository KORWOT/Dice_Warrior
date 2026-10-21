# MockAuthorityStore.cs

- 역할: 기존 Editor 회귀 검사의 Mock 계정 파일 저장소. Assets 밖 Authority 라이브러리에 유지하며 실제 HTTP 호스트는 SqliteAuthorityRepository를 사용한다. 실제 서버 보안 또는 프로세스 간 DB가 아니다.
- 핵심 동작: 절대화한 경로별 프로세스 내 잠금 아래 읽기→변경→검증→임시 파일 flush→원자 교체. schema 1 봉투에 SHA256 체크섬을 기록한다. 최초 생성은 20재화 프로필을 파일에 한 번 저장한다.
- 입출력: 공개 생성자 `MockAuthorityStore(string path)`. IAuthorityRepository.Execute는 동기 트랜잭션 콜백·CancellationToken을 받고 직렬화 복제한 BackendReply를 반환한다. 공개 자료형/검증기는 IAuthorityRepository.cs에 있다.
- 사용하는 대상: LoopJson, AuthorityValidation, BackendContracts, System.IO, Newtonsoft.Json. 사용하는 쪽: MockGameBackend를 만드는 기존 EditMode/PlayMode 테스트. 플레이어의 Factory에서는 생성하지 않는다.
- 관계 근거: MockGameBackend의 모든 권한 조회·변경은 Execute를 거친다. 프로필·티켓·제출 로그·정산 영수증·작업 응답·빈도 창을 같은 파일에 저장한 후 반환한다. 경로는 호출자가 지정하며 v1 실제 저장 경로는 접근하지 않는다.
- 상태/수명: 정상 primary의 이전 버전은 `.backup`에 보존. 최초 쓰기 전에 `.marker`를 durable하게 생성한다. marker 또는 backup이 있는데 primary가 없거나, marker/체크섬/스키마가 손상되면 자동 초기화·백업 복원하지 않는다. 사용자 취소를 초기화 및 각 파일 교체 전에 확인한다.
- 경계: 최대 128런, 1024작업, 런별 2048명령, 제출 512KiB, 전체 권한 파일 128MiB, 성장 10000. 한도 초과 시 기존 자료를 자동 삭제하지 않는다. 초기화 도중 중단되면 marker만 남고 명시 복구가 필요할 수 있다.
- 검수 주의: 파일 오류는 상위에서 TemporaryFailure로 표시한다. 저장 데이터 파싱/검증 오류는 IOException으로 감싼다. 내부 프로그래밍 오류는 일반 예외로 드러난다. File.Replace의 Android 장치 동작 및 다중 프로세스/사용자 파일 변조 방어는 이번 구현 보장 범위 밖이다.
- P2 저장 호환: schema/marker는 1 그대로다. 구형 파일에 없는 빈도 필드는 기본값 0/false로 읽는다. PendingVerification 영수증은 0지급으로 유지하고 Approved는 검증 버전/결과 범위 및 티켓 산식과 지급액 일치를 확인한다. 읽기만으로 구형 기록을 재작성·재검증·지급하지 않는다.
