# FateChoiceUI.cs

- 역할: BaseUI<FateChoiceUIData>의 독립 modal. public SelectedId/BindingVersion/Cards, confirmButton/closeButton, PlaySelection(string)/ResetPresentation()/RefreshLayout()를 제공한다. Cards는 읽기 전용 collection wrapper다.
- 입력/출력: authored panel/cardContent/ScrollRect/설명 Text/카드 원본/버튼6개/면6개와 공개 context/offers를 검증한다. 1~5개 제시 ID의 누락/중복을 거절한다. 타입/등급/일반 설명만 카드에 넘기며 세션/실제 사건/저장/RNG를 읽지 않는다.
- 선택: 카드 탭은 내부 선택과 금색 장식 및 confirm 활성만 바꾼다. offers.clicked를 실행하지 않는다. confirm/close/재굴림은 바인딩당 한 번 local 입력을 잠근 뒤 주입 callback을 요청한다. 재Bind와 재굴림은 선택을 지운다. 오래된 카드 callback은 BindingVersion으로 거절한다.
- 표시: 실제 FateCardView prefab을 반복 복제하여 가로 ScrollRect content에 둔다. 3개는 한 화면, 최대5개는 가로 스크롤이다. hud.dieClicked와 정확한6dice가 함께 있으면 authored6버튼/6DiceFaceView 한 행을 표시한다. 공개 RefreshLayout은 정지 Workbench가 활성 Canvas의 실제 layout을 마무리할 때 호출한다.
- 연출: PlaySelection 호출 순간 BindingVersion/연출 세대를 고정한다. 선택 카드의 실제 SelectionFeedback.Play가 FEEL 종료까지 진행한 후 Inspector exitSeconds 동안 panel의 localPosition과 CanvasGroup alpha로 퇴장한다. 정상 종료는 alpha0을 유지하며 소유자가 popup을 닫는다. 게임 선택 대기에는 coroutine이나 watchdog을 시작하지 않는다.
- 취소/수명: ResetPresentation은 연출 세대를 폐기하고 FEEL/원래 panel localPosition/localScale/alpha를 복원한다. 현재 iterator의 취소/예외도 finally로 복원하며 이전 iterator의 finally는 새 바인딩/새 연출을 초기화하지 않는다. OnUnbind는 BindingVersion 증가, presentation 취소, 카드/버튼 콜백 해제 및 runtime 복제 제거를 수행한다. OnDisable도 presentation을 취소한다.
- 직접 관계: FateChoiceUIData/FateOfferUIData, FateCardView, CommonButtonView, SelectionFeedback, DiceFaceView, FateDiceVisualCatalog의 ResolveFate/Grade/Button, UIManager/BaseUI, Unity uGUI layout. FateChoiceAuthoring이 정확한 prefab 연결을 작성한다. Controller/Workbench의 소비 연결은 메인 소유다.
- 검수/검증: 메인이 missing-popup 실제 RED 1/1을 확인한 후 구현했다. FateChoicePopupTests 7개가 실제 포인터/선택·요청 횟수/재Bind·재굴림/FEEL→exit/두 비율/5개 스크롤을 확인한다. 보조는 Unity를 실행하지 않았고 최신 GREEN은 NOT_RUN이다. 통합/시간제한/저장 증거는 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
