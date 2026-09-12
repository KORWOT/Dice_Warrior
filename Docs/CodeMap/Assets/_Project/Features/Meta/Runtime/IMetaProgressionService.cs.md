# IMetaProgressionService.cs

- 역할: 미래 서버 어댑터를 위한 비동기 메타 명령 계약.
- 핵심 동작·입출력: LoadoutRequest/StartRunRequest/SettlementRequest/GrowthRequest는 requestId·expectedRevision과 선택 의도만 전달한다. 정산량·비용·잔액·인증 UID를 요청에서 지정하지 않는다. Profile은 이미 로드된 읽기용 복사, RunStore는 런 체크포인트다.
- 사용하는 대상·사용하는 쪽·관계 근거: GameApplication이 구현을 주입하고 RunUIController.Meta가 명령을 소비한다. 현재 구현은 LocalMetaProgressionService.
- 상태·수명·검수 주의: Firebase 어댑터는 Auth UID와 서버 승인 상태를 공급해야 한다. 로컬 저장소 전체 JSON 업로드를 영구 잔액 확정 API로 쓰지 않는다. 실제 네트워크/Auth/Rules는 이번 범위 밖이다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
