# RunState.cs

RA-C 현재 계약 · 2026-09-09

- 역할: schema1 저장 및 화면용 Runtime DTO 어댑터.
- 입출력/핵심 동작: RunState : RunStateData에 GameConfigData config만 더한다. ToCore는 규칙과 상태를 순수 CoreRunState로 명시 복사한다. FromCore는 규칙+독립 표시 설정을 기존 필드 구조로 조합한다. DeepCopy는 null/빈 값을 보존한다. LocalSaveEnvelope의 schema/checksum/payload 구조는 그대로다.
- 직접 사용하는 대상: RunStateData/CoreRunState/RunStateCopy, GameConfigData/PresentationSettings.
- 직접 사용하는 쪽: RunSession, LocalRunStore, Controller, 표시/규칙 테스트.
- 상태/수명·검수 주의: 이 DTO는 표시/저장 경계의 독립 복사이며 명령 실행 권위를 가지지 않는다. inherited public 필드의 실제 Unity JSON 호환을 CoreReplayTests에서 검증한다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.
