# UIManager.cs
- 역할: 직접 등록한 BaseUI prefab 목록을 구체 타입으로 검증하고 타입마다 한 인스턴스를 캐시한다. active 기본 화면 하나와 명시적 popup stack을 소유한다. Resources/string-path load/게임 계산 없음.
- API: Show<T>, ShowPopup<T>, CloseTopPopup, CloseAll, SetInputLocked, TryGetCached. 같은 화면은 재Bind, 다른 화면 전환은 이전 popup/화면 Close. 열린 screen과 popup에 같은 instance를 동시에 사용하지 못한다.
- 전이: 비활성 cache parent에서 생성해 최초 OnEnable 전에 Bind. 새 화면 Bind 실패 시 기존 화면/모달/포커스를 보존하고 실패 instance만 정리한다. 현재 자기 자신 재Bind 실패는 부분 상태를 남기지 않고 닫는다.
- 팝업: 서로 다른 타입 stack, 같은 popup 요청은 동일 instance를 재Bind하여 맨 위로 이동. blocker는 top 바로 아래, 아래 screen/popup은 입력 불가. 닫을 때 유효한 이전 selected object를 복원하며 stale focus를 제거한다.
- 입력/수명: global lock은 화면·popup 변경으로 해제되지 않는다. CloseAll은 소유 callback/data/활성뷰 참조를 정리하고 캐시는 유지, manager 파괴 시 정리한다. authored root geometry는 Reparent(false)에서 보존한다.
- 관계: UIRoot/BaseUI/uGUI EventSystem만 참조. RunUIController가 UI 선택과 command lock을 맡으며 views는 manager를 통해 전역 인스턴스를 검색하지 않는다.
- 검수: UIManagerTests13과 실제 UIRoot modal raycast/캐시 복귀 tests. close 예외 이후 일관성, one/type, 잘못된 등록/데이터/역할 충돌, 지연 이벤트 정리 포함.
- Scene 구조 이후: 제작용 UIRoot registry는 기존 7개 + TitleUI다. 관리자는 RunUIController의 persistent 자식으로 씬을 건너 유지된다. UIManager 자신은 SceneManager/GameApplication/저장소를 참조하지 않으며 화면 캐시 책임만 유지한다.

## 플레이 작업실 (2026-09-08)
- UIWorkbenchPreview가 폐기 가능한 PreviewSceneStage 안의 실제 UIRoot 복제본에서 등록 화면 하나를 Show한다. 입력을 잠그고 종료 시 전체 clone을 DestroyImmediate하며 재바인딩/CloseAll은 호출하지 않는다.
- 사용·검증: Docs/Reports/PLAY_WORKBENCH_REPORT.md.

## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- 제작용 UIRoot registry에 DiceRollAuthoring이 DiceRollUI를 추가한다. 기존 7개 런 화면과 TitleUI에 별도 주사위 popup이 더해지며, UIManager의 타입 등록·캐시·stack API는 그대로 사용한다.
- RunUIController.ShowDiceWindow가 ShowPopup<DiceRollUI>에 표시용 DiceRollUIData와 명시적 roll callback을 전달한다. 도착 후 굴림 대기에서는 popupBlocker와 화면별 CanvasGroup이 아래 맵/전투 입력을 막고, 실제 명령·연출 중에는 controller.SyncInputLock의 Busy 값이 전체 입력을 잠근다.
- 실제 Roll/checkpoint 이후의 연출이 끝나면 컨트롤러가 CloseTopPopup을 호출하고 현재 런 단계를 다시 표시한다. UIManager는 명령·난수·보상·저장을 계산하지 않으며 popup 재개 여부도 컨트롤러가 결정한다. 씬 이동 잠금과 명령 잠금은 같은 SyncInputLock 경계로 전달된다.
- UIWorkbenchPreview는 필요 시 실제 화면 위에 같은 DiceRollUI를 ShowPopup하고 입력을 잠근다. preview root는 종료 시 전체 폐기되며 제작 UI cache나 기본 저장소에 연결되지 않는다.
- 문서 범위: 실제 소스와 이번 작업의 staged popup/컨트롤러 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.
