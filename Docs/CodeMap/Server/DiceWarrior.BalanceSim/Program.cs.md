# Program.cs

- 역할: Unity·HTTP·DB를 사용하지 않는 순차 .NET 밸런스 실행 CLI.
- 핵심 동작: name/value 인자 검증→정본/시드 확인→신규 출력에 manifest/seed 선기록→MatchRunner 호출→JSONL flush→첫 실패에서 중단→통계/CSV/MD/성능/파일 해시 출력. 실패 시드만 명령 재현을 남긴다.
- 입출력: --repo/--output/--count/--master-seed 또는 --seeds/--max-commands/--timeout-seconds/--verify. 종료코드0은 배치 성공,1은 실행/재현 실패,2는 입력·출력 등 오류. output은 필수다.
- 재현: verify=true이면 경기마다 반복/명령 replay/1·5·10·20명령 후 JSON 복원/관찰OFF와 상태/RNG/명령/지표를 대조한다. 시간은 재현 지표에서 제외하고 검증 수행시간을 별도로 기록한다.
- 사용하는 대상: BalanceScenario, MatchRunner, Statistics/ReportWriter, LoopJson, Stopwatch/GC/Process, 로컬 git rev-parse HEAD. 사용하는 쪽: .NET 콘솔 진입점과 ScenarioTests. 서버/Unity/사용자 저장 호출은 없다.
- 상태/수명: 한 프로세스 한 배치. 정상 원자료는 요약 JSONL이며 전체 checkpoint는 저장하지 않는다. CLI 인자로부터 셸 문자열을 구성하지 않는다.
- 검수 주의: managed heap은 매 경기 뒤 샘플 최댓값, RSS는 프로세스 peak다. verification 수행 시 GC/할당/RSS에 그 작업도 포함됨을 표시한다. report 생성 전 raw 크기와 생성 후 전체 크기를 구분한다. batch 실패를 완료 표본만으로 PASS로 바꾸지 않는다.
