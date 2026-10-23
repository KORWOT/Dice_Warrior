# MatchRunnerTests.cs

- 역할: 신규 비권위 인간 정책/경기 실행/관측의 결정론·실패 경계 NUnit 검사다.
- 핵심 동작: neutral 동점 Offer 순서와 HP50% 경계·상태 불변, 인간3모험가의 동일 입력/replay/여러 JSON 복원/관찰 OFF 해시와 참가자 결과, 명령 상한/잘못된 Target/잘린 replay/종료 후 명령, HP피해·방어흡수·overkill·생략, 공동1위 분할과 AI 미계측을 검사한다.
- 입출력: 테스트 출력에 복사된 정본 shared-world-preview-4.json, 고정 seed123, 메모리 인공 관찰 fixture를 사용한다. 파일/DB/사용자 저장/Unity를 변경하지 않는다.
- 사용하는 대상: BalanceScenario.LoadDefinition, MatchRunner, HumanDecisionPolicies, OutcomeCollector, 공유 Core와 LoopJson, NUnit이다.
- 사용하는 쪽/관계 근거: DiceWarrior.BalanceSim.Tests의 dotnet test가 직접 API를 실행한다. TestCase3개로 인간 모험가 비교 조건을 고정한다.
- 상태/수명: 검사별 새 시나리오·경기와 메모리 명령목록을 사용한다. 타임스탬프/경과시간은 재현 비교에서 제외한다.
- 검수 주의: 인공 관찰 fixture는 집계 산식만 검증하며 정상 제품 경기를 대신하지 않는다. 9/90/300/3000 실제 단계별 경기 수와 성공 판정은 Program 실행 증거를 별도로 요구한다. 테스트 실행은 메인이 직렬 담당한다.
