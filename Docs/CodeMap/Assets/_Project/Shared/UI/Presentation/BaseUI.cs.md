# BaseUI.cs
- 역할: 공통 UIData 및 BaseUI/BaseUI<TData>의 typed 바인딩·초기화·활성·닫기 수명. SharedUI는 uGUI만 참조하며 게임 타입을 모른다.
- 입력/동작: UIManager가 Attach하여 소유자를 고정한다. data null/할당 불가능 타입은 거절, Initialize는 한 번, Bind는 활성화 전에 실행한다. 같은 열린 화면 갱신은 재Bind하며 Unbind/disable을 거치지 않는다.
- 상태/수명: IsOpen/Manager/DataType 노출, protected typed Data는 열린 동안만 유지한다. Close는 한 번만 StopAllCoroutines/OnUnbind/입력 차단/비활성화하며 예외에도 Data를 finally로 제거한다. close에 게임 callback 없음.
- 관계: UIManager의 내부 전이 API, RunScreenView<TData>와 테스트 probe들이 상속. CanvasGroup은 authored 참조여야 한다.
- 검수: 최초 OnEnable 전에 유효 데이터, 중복 초기화/정리 없음, 실패한 바인딩과 예외 정리, 이전 callback/지연 동작 잔류 없음. UIManagerTests로 검증한다.
