# ScenarioTests.cs

- 역할: 분석 입력·정본 보호와 출력 이탈/덮어쓰기 방지 회귀 검사.
- 핵심 동작: 고정 시드 1000개의 nonzero/중복 없음/단계 prefix·재현 해시, 잘못된 seed/정책/모험가/장비/변조 정본/누락 파일 거절, output 신규 경로·기존 경로·상위 이탈, 잘못된 CLI 인자를 검사한다.
- 입출력/대상: NUnit3, BalanceScenario, Program, 복사된 v4 정본 fixture. 실제 권한 서버나 게임 저장을 사용하지 않는다.
- 사용하는 쪽: DiceWarrior.BalanceSim.Tests의 dotnet test; 관계는 프로젝트 참조와 직접 호출이다.
- 상태/수명: 출력 경계 fixture는 test working directory의 고유 GUID 하위에만 생성·정리한다. 원본 콘텐츠를 수정하지 않는다.
- 검수 주의: 이 검사는 정책 최적성/실제 인간 승률의 증거가 아니다. 최종 실행 수는 BALANCE_SIMULATION_07_REPORT를 따른다.
