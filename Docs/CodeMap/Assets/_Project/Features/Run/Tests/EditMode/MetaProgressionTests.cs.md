# MetaProgressionTests.cs

- 역할: 프로필/정산 원자성·소유·실패 경계 자동 검사.
- 검토 회귀: 같은 원본에서 생성한 두 RunSession의 상충 n+1 저장 거부, 첫 명령의 확정 후 응답 유실 재시도, sequence 유지 시간 autosave 및 시간 역행 거부를 추가했다.
- 핵심 동작·입출력: 독립 임시 경로에서 원본 복제·보유품 시작·구형 이관·실제 RunSession 완주/패배·정산 한번·저장 전 실패/확정 후 응답 유실·성장/다음 런·stale revision/foreign checkpoint/손상 보존을 검사한다.
- 사용하는 대상·사용하는 쪽·관계 근거: 실제 LocalPlayerDataStore, LocalMetaProgressionService, RunSession과 초기 설정을 사용한다. ProbeStore는 전/후 확정 실패만 주입한다.
- 상태·수명·검수 주의: 실제 사용자 경로를 사용하지 않는다. 실행 결과는 META_PROGRESSION_REPORT와 artifacts 증거를 따른다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
