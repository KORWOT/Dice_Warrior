# CampaignMapView.cs

- 역할: 주입된 공개 지도 DTO만 반복 노드와 경로로 표시한다. RunState/Session/저장/RNG를 조회하거나 게임 진행을 계산하지 않는다.
- 입력/API: BindCampaign(IReadOnlyList<CampaignNodeUIData>, currentId, selectedId, ExplorationNodeView, FateDiceVisualCatalog, Action<string>). 미공개 유형은 null을 유지하며 실제 floor/lane/childIds의 공개 복사본만 보유한다. 기존 Bind(active, available, selectedId, prefab, visuals, choose, completed)는 legacy 짧은 그래프와 archived 대안 fade를 지원한다.
- 핵심 동작: 신규 경로는 합집합의 노드를 ID당 한 번 만들고 실제 childIds만 점선으로 연결한다. 고정 floor/lane으로 배치하므로 노드 탭과 완료/접근불가 변경이 좌표를 재정렬하지 않는다. currentId는 파란 플레이어 위치를 지정한다. 버려진 가지도 잠금·낮은 채도의 경로로 남는다.
- authored 참조: edgeLayer/nodeLayer/playerMarker/currentLocation, rowSpacing, arrivalSeconds, 색상. 고정 화면 구조는 원본에 남고 이 View는 자신이 만든 노드/연결선만 소유한다.
- 출력/관계: FateDiceWidgets.ShowCampaignMap/ShowMap → 이 View → ExplorationNodeView/CommonButtonView/MapNodeGraphic/CampaignPathGraphic. legacy 연결은 Image를 유지한다. 노드 callback은 ExplorationUI의 단일 탭 이동 요청이며 이 View가 게임 명령을 직접 실행하지 않는다.
- 상태/수명: 공개 snapshot topology가 달라질 때만 반복 자식을 재작성한다. rebind/Clear/disable의 generation은 이전 도착 코루틴이 새 표시를 변경하지 못하게 한다. AnimateNodeArrival은 marker만 unscaled 시간으로 이동하고 비유한/음수 시간을 거절한다. Clear는 소유 자식만 Unbind/Destroy(정지 Editor는 DestroyImmediate)한다.
- 검수 주의: 같은 seed의 위치 안정성은 생성기 저장 좌표 계약과 함께 검증한다. 미공개 타입을 ResolveNode에 넘기지 않는다. legacy 위상 배치/완료 경로/fade는 기존 경로로 보존한다. 실제 자산 두 비율·입력·도착 수명·스크롤 검증은 Main의 PROCEDURAL_CAMPAIGN_REPORT 증거가 기준이며 소스 구조 확인은 실행 PASS가 아니다.
- 최종 회귀 보완: legacy 미래 노드 높이를92로 두어 새 원본의 하단25% 레이블에서 한글16px의 실제19px 선높이가 잘리지 않게 한다. 좌표/연결/RNG/공개 범위는 변경하지 않는다.

## CAMPAIGN_FLOW_POLISH 복귀 초점 계약

- OnEnable, 새 그래프, current/legacy 완료 경로 변경에만 focus를 요청한다. 캐시 화면이 다른 화면에서 돌아올 때도 활성화 시점에 새 요청을 잡는다.
- Runtime LateUpdate는 요청 프레임이 지난 뒤 Canvas 갱신→맵 좌표 갱신→Canvas 갱신을 거쳐 실제 viewport/content 크기가 있을 때 초점을 한 번 소비한다. 준비되지 않은 레이아웃에서 요청을 없애지 않는다.
- 초점 영역은 현재 노드·플레이어와 다음 available 노드의 높이 범위다. 이후 같은 데이터의 재바인딩·수동 스크롤·일반 크기 갱신은 초점을 다시 요청하지 않는다. 매 프레임 스크롤을 강제하지 않는다.
- 앵커 기준 노드 높이는 nodeLayer.rect.yMin을 더해 로컬 좌표로 바꾼 뒤 content 공간으로 변환한다. 중앙 pivot의 절반 높이를 초점에 잘못 더하지 않는다.
- 정지 Workbench는 LateUpdate를 받지 않으므로 최종 활성 원본에서 명시 호출되는 RefreshLayout에 한해 편집기 초점을 적용·소비한다. 실제 복귀/수동 스크롤/두 비율 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 Main 실행 증거를 따른다.
