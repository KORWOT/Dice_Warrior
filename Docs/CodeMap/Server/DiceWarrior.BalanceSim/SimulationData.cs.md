# SimulationData.cs

- 역할: 실행기·관찰기·통계·보고 간 공통 입력/결과 계약.
- 핵심 자료: ScenarioManifest의 HEAD/정본·소스·시드 해시/환경/정책, BatchResult의 요청·실제 결과·상태, MatchResult의 최종 상태/RNG/명령 해시와 참가자/관찰·실패, PerformanceMetrics의 시간/메모리/GC/출력, RunOptions의 관찰·복원 옵션.
- 입출력: 단순 public 속성 DTO이며 게임 진행/난수/정산을 수행하지 않는다. 참가자 rank1 분할값과 절대HP/비율, 잔여XP를 분리하고 AI의 최대 전투턴은 nullable로 둔다.
- 사용하는 대상: CompetitionState/CompetitionCommandEntry, OutcomeCollector의 HumanObservations. 사용하는 쪽: Program, MatchRunner, OutcomeCollector, Statistics, ReportWriter 및 신규 검사3종. 직접 생성/참조로 관계를 확인한다.
- 상태/수명: 한 배치 또는 경기 수명. MatchExecution의 전체 상태/명령은 재현 검사/실패 재현에만 사용하며 정상 JSONL에는 MatchResult만 쓴다.
- 검수 주의: ElapsedMs는 재현 비교에서 제외한다. RawOutputBytesBeforeReports는 verification/report 생성 전 manifest/seed/경기 원자료 크기이며 최종 전체 산출물 크기가 아니다. 미계측 null과 실제0, PASS와 BLOCKED를 혼동하지 않는다. 결과는 권한 기록/지급 영수증이 아니다.
