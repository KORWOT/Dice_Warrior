# UIManagerTests.cs
- 역할: 가벼운 concrete test UI/데이터와 authored-style root fixture로 shared UI lifecycle/cache/type/input/popup/failure 계약13개를 검증한다.
- 오라클: 최초Bind가 OnEnable보다 먼저, initialize1회, 같은화면rebind는disable/close0회, 다른화면/CloseAll은 owned cleanup 정확히1회·Data제거·coroutine취소·callback실행0. one/type와 잘못된 데이터/등록/역할 충돌 거절.
- 모달: top순서/blocker바로아래/하위group차단, 같은타입재정렬, 유효한 이전focus복원, global lock이 popup/화면 변경 후 유지. 실제포인터통합은 UiStructureTests에서 별도검증한다.
- 실패: 새Bind실패는 기존화면/popup/focus보존, 자기rebind실패는부분뷰정리, throwingUnbind후에도전부close/manager참조정리. root safeArea/inactivecache와 authoredview anchor보존.
- 상태/수명: 각 테스트가 소유 객체·이전선택을 추적해 teardown정리한다. RunSession/store 또는 실제 사용자데이터를 사용하지 않는다.
- 관계: BaseUI/UIManager/UIRoot/uGUI만 동작대상. Main이 직렬 Unity CLI로 RED13→GREEN 실행 증거를 기록한다.
