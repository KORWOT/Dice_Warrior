# RunSession.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 기존 공개 진입점을 유지하는 Runtime facade.
- 입출력/핵심 동작: New/생성자는 정의와 표시를 검증·분리하고 실제 명령/시간/저장은 RunApplication으로 위임한다. bool 명령, State/ReadSnapshot, Phase, token, Checkpoint 호환 API 유지. CheckpointAdapter는 순수 후보를 독립 Runtime 저장 DTO로 조합하며 callback getter/setter의 기존 의미를 유지한다.
- 직접 사용하는 대상: RunApplication, RunState, PresentationSettings, IRunStore.
- 직접 사용하는 쪽: RunUIController/Workbench 및 기존 Run/Save/GUI 테스트.
- 상태/수명·검수 주의: private application이 상태 변경을 독점한다. 표시 설정은 세션 수명 동안 독립 보관하고 복사로만 내보낸다. JsonUtility/게임 계산식/파일 I/O가 facade에 없다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.
- RA-D 직접 관계: 기존 명령 facade를 그대로 사용한다. RunApplication/RunStateCopy가 새 action effects·획득 ID·고정 shopOffers까지 독립 저장 DTO로 전달하고 Runtime facade에는 카드/가격 분기를 추가하지 않는다.
