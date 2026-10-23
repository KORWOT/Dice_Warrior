# Statistics.cs

- 역할: B0/C7 배치의 유효 결과·실패·미실행을 분리하고 참가자별 통계, 시드 짝비교, 인간 관찰 기여를 순수 집계한다. 집계 전용 DTO도 같은 파일이 소유한다.
- 핵심 동작: 고유 seed/hero 표본틀과 참가자 4명, 순위·공동1위 분할·현재/최대 HP 비율·AI 고정 로스터를 검사한다. 인간은 모험가별, AI는 인간 모험가 시나리오와 slot/hero/policy 고정 조합별로 분리한다. 시련·탈락·rank1에 Wilson 95%, 결과 수치에 nearest-rank 분포를 제공한다. rank/laps/탈락 원인/시련 상태, 경기 종료·시련 성공자 수·공동순위·HP 타이브레이크 노출은 빈도로 기록한다.
- 입출력: `BuildSummary(BatchResult, bootstrapIterations=2000, bootstrapSeed=0xB0C70001)` → `SimulationSummary`; `Wilson95(successes, denominator)` → 구간 또는 표본0일 때 null; `Describe(IEnumerable<double>)` → 표본수·결측수·평균/min/p05/p25/p50/p75/p95/max. NaN/무한대와 불일치 입력을 거절한다.
- 사용하는 대상: `SimulationData`의 BatchResult/MatchResult/ParticipantResult/ScenarioManifest/PerformanceMetrics, `OutcomeCollector`의 HumanObservations/SkillObservation/DamageObservation/EnemyObservation, System 컬렉션/LINQ/Math.
- 사용하는 쪽: `ReportWriter.Write`, `StatisticsTests`. CLI는 ReportWriter 반환값으로 실행 요약을 받는다.
- 관계 근거: 신규 ReportWriter의 직접 BuildSummary 호출, 신규 검사에서 세 공개 API 호출, 관찰 DTO 속성의 읽기 참조. SharedWorld/Core 규칙 API·RNG·서버·Unity는 호출하지 않는다.
- 상태/수명: 입력 DTO를 변경하지 않고 실행마다 새 집계를 만든다. 3종 인간 경기가 모두 유효한 seed block만 짝비교에 넣고, local xorshift32와 안정된 metric seed로 결정적 percentile bootstrap을 수행한다. 정상 참가자 분모에서 실패를 제외하되 실패·NOT_RUN 수를 함께 유지한다. 경기 수가 전부 완료되어도 배치의 재현 검증 상태가 BLOCKED이면 요약도 BLOCKED를 보존한다.
- 검수 주의: 참가자 4명을 독립 경기로 보거나 AI 모험가·정책 효과를 분리해 해석하지 않는다. 분할1위는 Bernoulli가 아니므로 Wilson을 적용하지 않는다. 95% 짝구간은 다중 비교 보정 없는 탐색 결과다. 관찰 OFF/부재와 AI 전체 이력은 NOT_MEASURED/null이며 실패와 별개다. 부분 관찰 시 누락된 기술 발동 수는 알 수 없으므로 기술 분포 MissingCount=null이다. HP 타이브레이크 노출은 다른 상위 순위 요소가 같은 인간 포함 쌍의 HP 차이이며 순위가 얼마나 바뀌었는지의 인과 추정은 아니다.
