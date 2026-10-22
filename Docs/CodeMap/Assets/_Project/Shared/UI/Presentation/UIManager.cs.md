# UIManager.cs

- SharedWorld 직접 관계: Title은 CompetitionScreen 하나만 등록하고 CompetitionApp이 로비/경기/계정 복구 상태를 표시한다. 구형 App과 화면 등록은 제거했다. UIManager 소스/공용 프리팹은 변경하지 않는다.

- 역할: 명시 등록된 BaseUI 프리팹의 타입별 캐시, 기본 화면 1개와 팝업 스택.
- 동작/입출력: Show/ShowPopup/CloseTopPopup/CloseAll/SetInputLocked. 비활성 캐시 아래 생성 후 바인딩하고 활성 레이어로 옮긴다. 새 바인딩 실패 시 이전 화면/포커스를 보호한다.
- 사용하는 대상: UIRoot/BaseUI/EventSystem. 사용하는 쪽: CompetitionApp의 Show<CompetitionScreen>/CloseAll, CompetitionAuthoring의 직접 prefab 등록, CompetitionFlowTests.
- 관계 근거: Title 씬에 CompetitionScreen 하나만 등록한다. 공용 UIRoot 프리팹 등록 배열은 비워 범용 루트로 유지한다.
- 상태/수명: Title 씬 수명, 캐시는 관리자 소유. 기존 persistent GameApplication/구형 화면 등록은 제거되었다. 게임 상태/저장/RNG 의존 없음.
- 검수: 등록 참조, 레이어 입력 차단/캐시 수명, 화면 1개 재바인딩, 파괴 정리. 실제 새 버튼 raycast는 FlowTests에서 확인한다.
