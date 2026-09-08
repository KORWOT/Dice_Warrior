# UIRoot.cs
- 역할: authored Canvas/safeArea/screenLayer/popupLayer/overlayLayer/cacheLayer/inputGroup/popupBlocker 참조를 관리한다.
- 동작: ApplySafeArea는 현재 Screen.safeArea/해상도를 정규화한 anchor로 적용하고 offset을 정리한다. cacheLayer는 비활성 상태를 유지한다. 화면 prefab 자체의 anchor/offset/typography를 재작성하지 않는다.
- 관계: UIRoot.prefab에 UiStructureAuthoring가 작성, RunUIController가 Instantiate/갱신, UIManager가 레이어·blocker·전체 입력그룹을 사용한다. EventSystem/InputSystem 생성은 Editor authoring 책임이다.
- 상태/수명: RunUIController의 자식. 제작용 Scene에서는 상위 GameApplication이 DontDestroyOnLoad 수명을 소유하고 prototype에서는 해당 Scene 수명이다. UIRoot 자체에는 숨은 singleton/전역 탐색이 없다. 실제 popup 없는 경우 blocker가 입력을 먹지 않아야 한다.
- 검수: portrait1280/1600 safe area, 올바른 참조/parent 및 inactive cache, layer 순서와 UIManagerTests/실제 modal raycast 테스트.
