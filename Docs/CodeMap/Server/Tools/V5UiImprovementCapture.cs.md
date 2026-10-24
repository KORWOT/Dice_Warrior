# V5UiImprovementCapture.cs

- 역할: 승인된 CompetitionScreen.prefab 업그레이드와 v5 개선 주요12화면 캡처. Assets 외부 Pipeline run_script 도구이며 제품 실행 경로가 아니다.
- 입력/출력: Upgrade는 기존 프리팹 contents에 V5ShellAuthoring/CompetitionCombatUiAuthoring을 적용하고 해당 파일만 저장, GUID 유지 검사. Capture는 실제 프리팹/JSON을 PreviewScene에 표시해 Docs/UI/V5Improved PNG12장과 manifest를 작성한다.
- 사용하는 대상: Unity Editor PrefabUtility/PreviewScene/RenderTexture, App 격리 Open/OpenAccount, memory backend, 기존 Session 실제 명령/판정.
- 사용하는 쪽·관계 근거: 메인이 승인된 PLAN에 따라 Pipeline entry Upgrade/Capture로 호출한다. Title/공용 프리팹/설정 저장은 없다.
- 상태/수명: Editor Play 중에는 거절. 고유 artifacts fixture 경로만 사용하고 finally/Dispose에서 preview객체와RT를 정리한다. HTTP/Firebase/실제 사용자 저장을 읽거나 쓰지 않는다.
- 검수 주의: 전투 캡처는 명시된 합법 fixture/물리눈 조합이고 보상·결과는 실제 시작 이후 명령 진행이다. 캡처 버튼 호출은 화면자료용이며 실제 포인터 검증은 별도 PlayMode 검사의 결과를 따른다. manifest의 setup을 보존한다.

- BoundaryAudit: QA14/15/20/22/26의 경계 확인. 실제19회꽝 실행 뒤20번째 행동의 카운터/제한/처치우선, 정보→능력치→뒤로 불변,29→30→전체종료,백업조회취소,새게임확정·원문보존을 격리 Context에서 검사해 boundary-audit.json으로 남긴다.

- LapRoundBoundary: 합법30라운드3바퀴 보상 fixture에서 실제 UI 수령 콜백→시련 승리→AI3명→라운드제한 결과까지5조건을 확인한다. 초기 경계상태와 적HP1은 명시적으로 주입하며 자연진행 도달 증거와 구분한다. lap-round-boundary.json을 남긴다.
