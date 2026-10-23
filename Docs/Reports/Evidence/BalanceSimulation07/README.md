# B0/C7 실행 증거

2026-10-23, 기준 HEAD `2420decbaea896973b8fd62110cc0cd54aa9a530`. 구현/해석은 [REPORT](../../BALANCE_SIMULATION_07_REPORT.md)를 따른다.

- `green.trx`: 신규 검사27/27 PASS. `initial.trx`의25PASS/1RED는 HP 비교·시련실패 빈도의 구현 전 기록이며 최종 결과가 아니다. `review-red.trx`는 B07-01 batch BLOCKED 덮어쓰기의 실제 재현 이력이다.
- `s9`:9경기 및 반복/명령재생/중간복원/관찰OFF36개 비교 PASS. `s90`:90경기, `p300`:300경기, `base3000`:기준선3,000경기 모두 실패0.
- `fail-command`/`fail-timeout`은 의도한 경계 입력이다. 각각 실패1·미실행8·exit1·재현 보존을 확인했다. 이 디렉터리의 BLOCKED를 실제 기준선 실패나 성공 표본으로 합산하지 않는다.
- `reproduction-audit.json`: 단계별 겹치는507쌍에서 ElapsedMs를 제외한 전체 결과와 상태/RNG/명령 해시 불일치0.3,399경기 각각 공동1위 분할합1.
- `stage-audit.json`: 보고서 생성까지 끝난 각 디렉터리의 실제 전체 바이트. summary의 RawOutputBytesBeforeReports는 manifest/seed/원자료 크기이며 최종 전체 크기와 구분한다.
- `baseline`/`user-before`/`protected-before`/`source-after-green`/`final-audit`는 코드·사용자 변경·환경·원본 보호 증거다. 기존 Q0/Q1 실행은 이번 검사 수에 포함하지 않는다.

대량 원자료는 저장소의 ignored `artifacts/balance-sim/run-01/<stage>/`에 보존했다. 정상 경기의 전체 체크포인트는 저장하지 않는다. 이 디렉터리에는 소형 요약·manifest·검증 증거만 복사했다. 각 `files.json`의 matches.jsonl/CSV 해시는 원자료 위치에서 검증할 수 있으며 파일 자체가 여기에 포함돼 있다는 뜻은 아니다. files.json은 자기 자신을 제외한 생성 시점 파일 해시 목록이다.

재실행 명령은 [Server README](../../../../Server/README.md)의 CLI 설명을 따른다. 같은 출력 경로는 재사용하지 않는다. 실제 Unity/HTTP/DB/Firebase/서버 권한 정산은 이번 실행에 없다.
