# MetricsEndpoints.cs
- 역할: 로컬 대시보드 정적 파일·계측 조회/내보내기·오프라인 비용 계산 API.
- 핵심 동작: /dev/dashboard(.js/.css), /dev/metrics, /dev/metrics/export, /dev/costs, /dev/pricing. loopback peer+Host/Origin 검사, CSP/no-store/nosniff. UTC from/to 날짜 필터와 제한 표본 집계.
- 일별/API/런별 실제 SQL·로그·재현 집계에 가상 R/W/D와 문서 작업 USD를 추가한다. 작업 USD는 저장/전송/Cloud Run/무료 할당량/세금을 제외한 부분 비용이며 가격 누락·미산정 요청이 있으면 합계를 null로 둔다.
- 입출력: 저장 계측/16KiB 시나리오 JSON → 실제/가상 분리 JSON·CSV·정적 UI. 잘못된 날짜/형식/시나리오는400, 가격 읽기 실패503.
- 사용하는 대상: MetricsCollector/MetricsAggregation/FirebaseOperationModel/CostEstimator/PricingCatalog/LoopJson.
- 사용하는 쪽/근거: Program.Map, wwwroot/dashboard.js의 same-origin fetch 및 다운로드 링크, TelemetryHttpTests.
- 상태/수명: 조회별 snapshot; 가격 JSON은 요청 시 읽는다. dashboard/health 호출은 게임 요청 계측에 포함하지 않는다.
- 검수: JSON unsupported 개수·가정을 유지한다. CSV는 estimateSupported/classification을 보존하고 미산정 숫자를 빈칸으로 쓴다. export는 body·원본 ID를 포함하지 않고 보관 표본 한도를 따른다. 외부 CDN/Firebase SDK/서비스 요청 없음.
