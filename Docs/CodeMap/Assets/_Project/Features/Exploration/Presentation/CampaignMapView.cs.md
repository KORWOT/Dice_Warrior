# CampaignMapView.cs

- 역할: Controller가 전달한 공개 NodeState/available/selected 스냅샷만 세로 지도에 표시한다. 게임 상태, 이력, Session, 저장소, 규칙 RNG를 읽거나 변경하지 않는다.
- authored 필드: edgeLayer, nodeLayer, playerMarker, currentLocation, rowSpacing, availableColor/futureColor/arrivedColor/completedColor. 씬의 고정 지도 구성을 runtime에서 다시 만들지 않는다.
- Bind: roots가 바뀌면 소유 동적 항목을 재구성하고 NodeId별 한 ExplorationNodeView를 만든다. DAG의 위상 순서/최장 층 깊이와 안정적인 부모 평균 순서로 합류를 한 지점에 배치한다. 실제 childIds만 연결하며 공유 자식의 선은 부모별 유지한다.
- 상태: available만 입력 가능, selected는 도착 색상, 미래는 비활성 표시다. Prune 후 전달 집합에서 빠진 노드만 기존 NodeView.FadeOut으로 숨기고 해당 선의 alpha도 끝점과 함께 줄인다. Controller가 resolvedEventIds 순서로 확인해 전달한 completed 목록만 완료 경로로 병합한다. nodeHistory에서 방문 경로를 추측하지 않으며 미선택 가지는 포함하지 않는다.
- 배치: 현재 위치 아래, 선택 노드 그 위, 다음 노드 위쪽. 한 줄 9개까지 기존 viewport 폭에 배치하고 10개 이상에서만 같은 ScrollRect의 가로 pan을 연다. 선택 노드 기본 116×106, 미래 최대64×80, 글자19/16. 실제 표시/터치 검증은 Main 통합 검수 대상이다.
- AnimateNodeArrival(string,float): 저장 성공 후 Controller가 호출하는 표시 전용 IEnumerator. marker만 부드럽게 이동하며 generation/비활성/닫기 시 중단된다. 종료 후 Controller Render가 새로운 선택 스냅샷을 바인딩한다.
- 수명: Clear는 자신이 만든 노드와 선만 Unbind/제거한다. Play에서는 Destroy, 정지 Editor에서는 DestroyImmediate. Inspector 고정 layers/marker/text는 보존한다.
- 관계: FateDiceWidgets.ShowMap/AnimateNodeArrival → 이 View → ExplorationNodeView/CommonButtonView. CampaignMapAuthoring이 기존 ExplorationUI mapContainer에 구성 요소와 참조를 만든다.
- 검증: 초기 자산 계약 RED 이후 실제 통합 결과는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
## LMD-C02 첫 수정 묶음
- Bind 마지막 optional IReadOnlyList<NodeState> completed 입력 추가. 활성 노드는 연결 완전성을 검증하고, 완료 노드는 현재/완료 ID에 포함되지 않은 archived child 연결만 제외한 복사본을 표시한다.
- completed 순서의 indegree0 노드부터 위상 층을 시작하여 완료 경로는 아래, 다음 available과 미래는 위에 배치한다. completed-only ID는 1회/비선택/낮은채도와 완료 문구로 표시한다.
- 현재 마커는 마지막 완료 지점에 위치한다. 완료 목록이 늘거나 선택이 바뀌면 배치 후 available 근처로 스크롤하며, available 터치 크기는 깊이에 관계없이 유지한다.
- 저장 스키마/Session/규칙 RNG 변경 없음. 실제 완료2개·버린가지제외·ID유일·저장재개 동일성 검증은 LOBBY_MAP_DICE_REPORT의 실행 결과를 참조한다.
