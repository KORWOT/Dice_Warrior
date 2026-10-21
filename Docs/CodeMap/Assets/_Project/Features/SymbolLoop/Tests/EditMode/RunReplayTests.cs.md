# RunReplayTests.cs

- 역할: P2 독립 재현의 고정 기대값·명령 변조·입력 소유권·상한을 검사하는 EditMode 테스트와 명시적 골든 작성 도구.
- 핵심 동작: 버전 고정 ReplayGoldenVectors.json을 읽어 초기/매 명령 뒤 RNG·보드·단계·HP/성장/골드·주사위·장비·적·기술/반격 결과를 비교한다. 중간 Run 직렬화/역직렬화 후 남은 명령 재현과 검증기 최종 결과/지급액도 확인한다. 승리·패배·완주 후 포기·상점/장비/주사위 교체 및 1~3개 슬롯·2/3기호 조합 관찰을 요구한다.
- 입출력: 고정 티켓/명령/명시적 상태 데이터 → NUnit 단언. sequence·순서·필수 이동 누락·중복 ID·target·종료 뒤 입력·결과 인수·잘못된 장비/슬롯·비용 부족·규칙 버전·티켓 손상·명령/문자열/바이트 상한·취소를 검사한다.
- 사용하는 대상: MockRunVerifier, RunCommandRules, LoopRules, LoopJson, BackendContracts, Unity Application.dataPath, NUnit.
- 사용하는 쪽: Unity EditMode runner; 메인의 명시적 Editor 호출만 `GenerateGoldenVectors`/`InspectGoldenCandidate`를 사용한다.
- 관계 근거: 테스트는 체크인된 JSON의 각 상태를 기대값으로 읽고 변경하지 않는다. GenerateGoldenVectors는 테스트 attribute가 없으며, 파일이 이미 있으면 덮어쓰지 않고 실패한다.
- 상태/수명: 테스트별 메모리 복사만 사용하며 실제 사용자 저장/백업을 열지 않는다. GenerateGoldenVectors만 허용된 골든 JSON 경로에 최초 작성한다. InspectGoldenCandidate는 메모리에서만 고정 시드 정책 요약을 반환한다.
- 검수 주의점: 생성기 출력은 자동 정답이 아니므로 최초 생성 후 사람/메인이 별도 확인하고 고정해야 한다. 매 검사에서 기대값을 생성하지 않는다. 명령 결과와 원본 입력을 비교하여 독립성을 확인하며, JSON 해시만 저장하는 대체 검사는 없다. 백엔드 원자 지급/구형 영수증·빈도 제한은 BackendReplayIntegrationTests 담당이다.
- 고정 데이터: 승리 seed33/35명령/50지급, 패배 seed51/41명령/30지급, 1지역 후 포기 seed19/13명령/10지급, 상점·교체 seed1/32명령/50지급. 총 121명령/125상태. 네 벡터 재생과 다섯 경계 검사로 총 9개 실행 결과를 만든다. Unity 번들 NUnit에 맞춰 순차 단언을 사용한다.
