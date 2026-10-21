# BackendReplayIntegrationTests.cs

- 역할: P2의 재현 검증과 실제 Mock 원자 정산·로컬 복구를 통합 검증한다.
- 핵심 동작: 위조 이동/누락/최종 결과 필드 거절 후 자산·활성 런 유지, 최신 규칙 변경과 무관한 시작 티켓 지급, 새 ID 10회 재제출 지급 1회, 구형 P1 파일의 무지급/읽기 무변경, 지급 응답 유실과 로컬 저장 실패 후 재개, 영속 빈도 제한/시계 역행/동일 ID 예외를 검사한다.
- 입출력: 고정 서버 시드 33, 주입 시계·규칙, 테스트별 authority/checkpoint 임시 경로 → NUnit 비동기 단언 5개.
- 사용하는 대상: MockGameBackend/MockAuthorityStore/Coordinator/Session/LoopStore/LoopJson/RunCommandRules, SymbolLoopTests.Finish. 구형 파일 fixture와 미지원 결과 필드 검사를 위해 이미 설치된 Newtonsoft.Json.dll을 테스트 asmdef에 명시 참조한다.
- 사용하는 쪽/관계 근거: SymbolLoop.EditMode.Tests assembly의 Unity Test Runner가 실제 파일/백엔드 경계를 실행한다.
- 상태/수명: 각 테스트는 고유 Temp/DiceWarriorReplay 폴더만 사용하고 허용 절대 루트 확인 후 정리한다. 실제 사용자 저장/백업은 접근하지 않는다.
- 검수: 구형 authority는 격리 fixture에서만 구성한다. FailingStore와 LostSubmissionReply는 이미 지급된 결과 복구를 재현한다. P3 전체 다중 기기/프로세스 동시성이나 Firebase 검증을 대신하지 않는다.
- P3: LostSubmissionReply의 GetRunStatus를 내부 백엔드로 전달한다. 지급 응답 유실→로컬 Commit 실패→재시작 동일 영수증 복구를 P3 조정기에서도 검증한다. 별도 프로세스나 실제 다중 기기는 후속이다.

- M0~M3 의존성: Mock 권한 타입은 Server/DiceWarrior.Authority 소스에서 빌드한 Editor 전용 Tests/Support/DiceWarrior.Authority.dll을 테스트 asmdef가 명시 참조한다. 실제 HTTP/SQLite 시험은 Server/DiceWarrior.HttpTests가 담당한다.
