# LoopStoreConcurrencyTests.cs

- 역할: P3 로컬 저장의 같은 경로 동시 실행·복구 세대·미결 정리·진단 원문 보존 계약을 검증한다.
- 핵심 동작: 실제 Task.Run/Barrier로 같은 revision Commit의 한 후보만 성공하고 패자가 재로드 후 진행하는지, 동일 pending의 동시 쓰기가 멱등인지 확인한다. 별도 작업에서 새 pending을 게시한 뒤 늦은 Clear/Discard가 이전 ID로 지우지 못함을 검사한다. 복구로 revision이 같아도 generation이 달라 stale 후보를 거절하는지, 고유 백업/체크포인트 사본과 진단 원문이 byte/필드 단위로 보존되는지 검증한다.
- 입출력: 격리 checkpoint 파일, 두 LoopStore 인스턴스, Task/Barrier와 유한 이벤트 대기 → NUnit 6개 검사. worker는 Unity API를 사용하지 않는다.
- 사용하는 대상: LoopStore/LoopJson, PlayerDocument/Command/BackendReply, System.IO/Threading/Tasks, NUnit. 사용하는 쪽: SymbolLoop.EditMode.Tests assembly의 Unity Test Runner.
- 관계 근거: 새 테스트는 LoopStore 공개 API로 파일을 실제 재조회한다. 프로필 fixture는 단순 로컬 문서이며 백엔드 지급이나 사용자 저장을 변경하지 않는다.
- 상태/수명: 각 검사는 Temp/DiceWarriorStoreSafety/<고유ID>만 사용하고 정리 전 절대 허용 루트를 확인한다. Barrier/작업 대기는 각각 10초 제한이며 finally에서 취소로 대기자를 해제하고 worker 완료를 기다린 뒤 fixture를 정리한다.
- 검수: 동일 프로세스/동일 절대 경로 보장만 검증한다. 기존 malformed pending 명시 취소 검사는 SymbolLoopTests가 유지한다. 다중 프로세스 파일 공유·장치 내구성·Firebase 동시성 검증을 대신하지 않는다. 실제 실행 결과는 P3 REPORT에 기록한다.
