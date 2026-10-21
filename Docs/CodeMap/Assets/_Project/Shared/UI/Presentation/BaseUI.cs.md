# BaseUI.cs

- 역할: UIData 및 typed BaseUI의 바인딩·초기화·활성·닫기 수명.
- 동작/입출력: UIManager가 Attach/BindData/Activate/CloseView를 호출한다. 타입 검사 후 최초 초기화, 매번 바인딩, 닫힐 때 코루틴/콜백/입력을 정리한다.
- 사용하는 대상: UnityEngine CanvasGroup. 사용하는 쪽: UIManager, SymbolLoopScreen : BaseUI<LoopScreenData>.
- 관계 근거: SymbolLoopScreen 상속과 UIManager의 직접 내부 API 호출. 구형 RunScreenView/RunUIController는 삭제되었다.
- 상태/수명: BindingRevision은 유효 바인딩/닫기마다 증가. Data는 열린 동안만 유지하고 예외에도 정리한다.
- 검수: authored CanvasGroup, 최초 OnEnable 이전 바인딩, 재바인딩/닫기 후 콜백 잔류. 새 흐름 검증은 SymbolLoopFlowTests, 과거 증거는 Archive/v1.
