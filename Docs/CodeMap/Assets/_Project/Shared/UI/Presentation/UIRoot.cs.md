# UIRoot.cs

- 역할: authored Canvas/safeArea/screenLayer/popupLayer/overlayLayer/cacheLayer/inputGroup/popupBlocker 참조.
- 동작/입출력: ApplySafeArea는 Screen.safeArea/해상도를 정규화한 anchor로 적용하고 cache를 비활성화한다. 화면 자체의 typography/배치를 다시 만들지 않는다.
- 사용하는 대상: UnityEngine/uGUI. 사용하는 쪽: UIManager, SymbolLoopApp.Update, SymbolLoopAuthoring, FlowTests.
- 관계 근거: 기존 UIRoot.prefab의 GUID/레이어를 유지해 Title에 연결. 구형 UIManager.prefabs만 해제했다.
- 상태/수명: Title 씬 수명. singleton/전역 탐색 없음. popupBlocker는 popupLayer의 직접 자식이다.
- 검수: 필드 누락, safe area, 캐시 비활성, popup 없는 화면 입력. 장치 safe area 변경 실검증은 별도.
