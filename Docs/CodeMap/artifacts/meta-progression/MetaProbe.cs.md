# MetaProbe.cs

- 역할: 메타 작업의 한정 Editor 실행 보조 도구.
- Regression은 MetaProgressionTests의 SetUp/ConcurrentSessionCandidatesCannotOverwriteACommittedCommand/TearDown만 반사 호출하여 지정 회귀의 실제 RED를 기록한다. TestRunner 전체 실행 결과와 별도다.
- FocusGameView는 Unity Editor GameView에 입력 포커스만 요청한다. 테스트의 가상 마우스 비활성 문제가 포커스 조건에서도 재현되는지 확인하기 위한 것으로, 입력 전역 설정·장치·게임 로직을 변경하지 않는다.
- 핵심 동작·입출력: Author는 MetaProgressionAuthoring.Apply를 호출한다. Inspect는 씬 dirty/play/compile과 authored meta config/menu version/컨테이너, production meta 파일 존재 여부, InputSettings의 유효성/자산 경로/background 및 Editor 모드를 읽는다. 입력 설정을 변경하지 않는다.
- 사용하는 대상·사용하는 쪽·관계 근거: Unity CLI run_script에서 호출하며 Assets 밖에서 임시 컴파일한다.
- 상태·수명·검수 주의: 사용자 프로필 생성/런 시작/Scene 저장을 실행하지 않는다. 작성 대상은 본 PLAN과 Authoring의 고정 경로다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
