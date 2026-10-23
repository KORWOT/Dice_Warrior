# MatchRunner.cs

- 역할: 정본 v4와 고정 시나리오의 순수 .NET 비권위 경기 실행/명령 replay 경계다.
- 핵심 동작: scenario.Validate 후 결정적 matchId로 CompetitionRules.Start를 호출한다. 매 인간 명령은 sequence/Target/clientCommandId를 확인하고 CompetitionCommandRules.Apply로만 진행한다. 명령 상한·경기 벽시계 상한·미완성/종료 후 replay를 BLOCKED로 남긴다.
- 입출력: Run/Replay는 MatchExecution(Result, FinalState, Commands)을 반환한다. 정상 Result에는 참가자/관찰 누계·최종 상태/RNG/명령 해시가 있다. 실패는 단계/명령번호/대상·예외 타입과 부분 명령/상태를 보존한다.
- 사용하는 대상: BalanceScenario, HumanDecisionPolicies, OutcomeCollector, 공유 CompetitionRules/CommandRules와 LoopJson이다. HTTP·Unity·사용자 저장·서버 DB를 호출하지 않는다.
- 사용하는 쪽/관계 근거: Program의 단계별 batch와 재현 검사, MatchRunnerTests의 동일 입력/replay/JSON 복원/관찰 OFF/상한 검사가 Run/Replay를 호출한다.
- 상태/수명: 실행별 새 상태/명령목록/collector를 소유한다. 복원 옵션은 지정 Apply 후 LoopJson 직렬화/역직렬화와 Validate를 수행한다. 정상 전체 상태/명령은 검증용 메모리 객체이며 원자료 CSV/JSONL에는 MatchResult만 기록한다.
- 검수 주의: elapsed는 재현 지표에서 분리한다. 벽시계 한도는 각 명령 사이와 최종 완료에 검사하며 진행 중인 동기 Core 호출을 강제 중단하지 않는다. Core는 AI/전투 루프 자체 상한을 가진다. 관찰 OFF도 필수 인간 max encounterTurns만 추적해 참가자 결과를 같게 유지한다.
