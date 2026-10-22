# TelemetryHttpTests.cs
- 역할: 실제 HTTP 경계를 통한 M4 측정·추정·실패 격리 회귀.
- 핵심 동작: dashboard/CSP/local Host/export, 정산50 및 동일ID/같은런 중복2, malformed JSON, 계측 폴더 장애, 지원 가격/미지원 지역, 권한DB 실패의 JSON/CSV 미산정, replay 거절과 before/after-abort·after-delay 복구를 확인한다.
- 입출력: 고유 임시 DB/seed33/golden 로그 → 실제 서버 HTTP·metrics JSON/CSV assertion.
- 사용하는 대상: ServerProcess, HttpGameBackend, HttpClient, NUnit, ReplayGoldenVectors.
- 사용하는 쪽/근거: DiceWarrior.HttpTests의 dotnet test; 프로젝트는 실제 host를 선행 빌드한다.
- 상태/수명: NonParallelizable fixture. teardown은 해석된 임시 루트 안만 재귀 삭제한다.
- 검수: transport 자체는 예외를 전파하고 Coordinator가 결과 불명을 분류한다. 이 테스트는 raw transport 예외를 기대한다. 계측 저장 완료와 HTTP 수신 사이의 짧은 비동기 간격을 제한 polling으로 기다린다. 실제 사용자 저장·Firebase 없음.
