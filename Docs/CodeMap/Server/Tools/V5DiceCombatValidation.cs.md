# V5DiceCombatValidation.cs

- 역할: Unity Pipeline에서 기존 PlayMode 실행이0건으로 끝나는 원인을 읽기 전용 진단한다. 실제 게임·서버 빌드에 포함하지 않는다.
- 핵심 동작/입출력: Editor의 run_script로 InspectDiscovery를 호출하면 실제 TestRunnerApi suite의 이름·RunState·검사 수와 Pipeline 필터가 선택한 이름을 반환한다. 일반/Explicit 허용 조회를 비교하며 검사를 실행하지 않는다.
- 사용하는 대상: Unity TestRunnerApi/ITestAdaptor, 로드된 PipelineTestRunner의 CollectTestNames. 사용하는 쪽: V5 작업의 메인 검증 명령.
- 관계 근거: CLI의 list_tests와 run_tests 실행 건수가 달라 실제 로드된 어댑터·선택 함수의 결과를 대조한다. 실제 선택25개를 확인하여 필터 단계와 runtime 실행 단계를 구분했다. Test Framework 캐시 원인과 도메인 재로드 후25/25 결과는 V5 REPORT에 기록했다.
- 상태/수명: 요청1개 동안 임시 TestRunnerApi를 만들고 callback 이후 finally에서 삭제한다. Scene/Asset/저장/전역 설정을 쓰지 않는다.
- 검수 주의점: Pipeline 내부 함수의 이름에 의존하는 일회성 진단 도구다. 0건을 PASS로 바꾸거나 Explicit 검사를 암묵적으로 실행하지 않는다.
