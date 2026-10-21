# BackendFoundationTests.cs

- 역할: P1 권한 분리·요청 멱등·저장/복구 경계를 격리 파일과 고장 주입으로 검사.
- 핵심 동작: 최초 20재화, 캐시 변조, 서버 시작 입력, 성장 비용/revision/fingerprint, 경합, P2 완주 승인/50지급 및 같은 요청 10회 중복 무지급, 버전/소유권/크기 거절, Firebase 미지원, 손상 fail-closed, 승인 후 저장 실패/응답 유실 재개와 백업 캐시 재조회.
- 입출력: 고정 서버 시드, 임시 LoopStore/MockAuthorityStore, FaultStore/LostReplyBackend → NUnit 비동기 단언.
- 사용하는 대상: BackendContracts/Mock/Factory/Coordinator/Session/Store 및 기존 Finish 도우미. Unity EditMode runner가 호출한다.
- 관계 근거: 생성자 주입으로 실제 Mock 저장 파일과 별도 체크포인트를 사용한다.
- 상태/수명: 테스트별 고유 임시 폴더, 허용 루트 확인 후 삭제. 사용자 저장/백업은 사용하지 않는다.
- 검수: 기존 P1 권한 경계는 유지하면서 정상 제출 기대값만 P2 승인으로 갱신했다. 악성 로그/골든은 RunReplayTests, 원자 정산/구형 기록/빈도는 BackendReplayIntegrationTests가 맡는다. Mock 내부 파일 자체 변조 방어는 목적이 아니다.
- P3: LostReplyBackend가 읽기 전용 GetRunStatus 계약을 내부 백엔드로 전달한다. 기존 승인 후 저장 실패/성장/백업 회귀는 계속 실행한다.

- M0~M3 의존성: Mock 권한 타입은 Server/DiceWarrior.Authority 소스에서 빌드한 Editor 전용 Tests/Support/DiceWarrior.Authority.dll을 테스트 asmdef가 명시 참조한다. 실제 HTTP/SQLite 시험은 Server/DiceWarrior.HttpTests가 담당한다.
