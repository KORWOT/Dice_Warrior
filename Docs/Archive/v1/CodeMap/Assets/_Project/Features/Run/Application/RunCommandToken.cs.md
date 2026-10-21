# RunCommandToken.cs

RA-C 현재 계약 · 2026-09-09

- 역할: runId/sequence 불변 명령 토큰.
- 입출력/핵심 동작: 생성 시 식별자/순번을 보관하는 불변 값 객체. UI 표시 snapshot에서 캡처하여 현재 런/순번과 다른 입력을 거절한다.
- 직접 사용하는 대상: System 기본 타입.
- 직접 사용하는 쪽: RunApplication/RunSession, RunUIController, 경계 테스트.
- 상태/수명·검수 주의: 기존 Runtime 파일을 GUID 유지 이동했다. 토큰 생성/검사는 RNG를 쓰지 않는다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.
