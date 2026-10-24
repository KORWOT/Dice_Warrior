# CompetitionApp.V5Ui.cs

- 역할: v5 전용 로비·출전·사건·보상·계정·상세·결과 Shell DTO를 구성하는 App의 partial 표시 계층.
- 핵심 동작: 후보 choices와 공통 tools/tabs/back을 분리한다. 선택 즉시 확정인 보상과 출전의 적용 시점을 설명하고 profile/participant/definition의 현재 값을 읽는다. v5 전투는 기존 Area/Net에 위임한다. 구형 경기에는 App의 legacy Render를 유지한다.
- 입출력: Session Snapshot/Account Profile/화면 로컬 menu → CompetitionScreenData/V5ShellData. 변경 명령은 기존 Dispatch/StartMatch/AccountAction/Select 경계를 호출한다.
- 사용하는 대상: CompetitionV5UiText의 순수 표시 API, CompetitionElementView, V5DiceNetData, UIManager.Show.
- 사용하는 쪽·근거: CompetitionApp.Render의 UseV5Ui 분기가 직접 호출한다. 신규 화면 검사와 기존 V5Flow가 실제 authored Shell을 통해 포인터를 실행한다.
- 상태/수명: 상세 진입 전 메뉴와 Net data를 보존하고 뒤로에서 복귀한다. 조회/탭/뒤로는 Session 명령/RNG를 실행하지 않는다. Close/NewMatch는 임시 맥락을 비운다. busy 중 명령/뒤로를 차단한다.
- 실패·검수: 새 경기/포기/백업 확인에서 Back이 확정 콜백을 실행하면 안 된다. 계정 성공 표시에는 승인 후 프로필만 사용한다. 5장비 후보와 조회·메뉴는 독립 접근해야 한다. 원본/실제 입력 증거는 V5_UI_IMPROVEMENT_11_REPORT를 따른다.

- V5_UI_IMPROVEMENT_11: 계정 요청 첫 Render도 서버 대기 상태로 안내한다. 보드의 직전 AI 결과는 state.lastRound에서 읽으며 부족 재화는 음수가 아닌 부족량으로 표시한다.
