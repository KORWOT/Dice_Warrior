# MetaProgressionFlowTests.cs

- 역할: 실제 로비·결과 버튼의 두 세로 해상도 통합 검사.
- 검토 회귀: 출발 저장 후 응답 오류 → 와일드 설정 변경 → 실제 이어하기 버튼으로 원래 시작 구성을 복원한다. 런 레벨업 후 HP를 영구 성장 전후 비교의 기준으로 고정하고 초기 HP와 혼동하지 않는다.
- 핵심 동작·입출력: 격리 프로필을 GameApplication에 주입한다. 720×1280/1080×2400에서 실제 authored GraphicRaycaster/포인터 입력으로 보유 장비·주사위·와일드/출발과 정산/성장을 검증한다. 스크롤로 버튼을 노출한 뒤 실제 hit target을 검사한다.
- 사용하는 대상·사용하는 쪽·관계 근거: production MenuUI/GameApplication prefab, Lobby/InGame Scene, RunUIController와 LocalMetaProgressionService를 사용한다.
- 상태·수명·검수 주의: 사용자 기존 run/새 meta 파일 존재 여부와 bytes 보존을 검사한다. 전투 전체 연출 검사는 기존 회귀가 담당하며 이 테스트는 결과 경계를 실제 RunSession으로 준비한다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
