# HttpAuthorityTests.cs

- 역할: 실제 서버 프로세스·SQLite·Unity HTTP/Coordinator 소스의 통합 계약 검사.
- 핵심 동작:4골든 전체상태/지급,2프로세스100정산·10구매,실제 소켓 단절/취소·강제종료/백업복구,계정격리·위조거절·fingerprint필드순서·ID/계정경계 확인.
- 입출력: 기존 골든/고유 임시 DB·체크포인트→NUnit 결과. 실제 사용자 파일을 열지 않는다.
- 사용하는 대상/쪽: ServerProcess, HttpGameBackend/Settings, LoopSession/Store/Coordinator(동일 Unity 소스 링크), LoopRules/RunCommandRules/DTO; dotnet test.
- 관계 근거: 서버와 테스트의 런은 별도 프로세스/객체다. HTTP 영수증/프로필과 DB 재시작 결과로 판정한다.
- 상태/수명: 테스트당 Temp/DiceWarriorHttpTests/고유ID, 프로세스 종료 뒤 절대 경로 검증 후 정리. 환경 변수/전역 설정은 바꾸지 않는다.
- 검수: 실제 루프백 HTTP 소켓을 사용하는 정합성 검사다. 처리량·외부 네트워크·모바일 동작은 측정하지 않는다. 개발 fault만 명시적으로 활성화한다.
