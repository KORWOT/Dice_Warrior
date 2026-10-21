# MapNodeGraphic.cs

- 역할: 그림이 없는 공개 지도 노드의 원형 프레임·실제 유형 아이콘·상태 배지를 uGUI mesh로 그린다.
- 입력/API: Bind(nullable type,tint,canTravel,done,blocked,preview,player,symbol), SetSelected(bool). 출력은 현재 Rect 내부의 링/아이콘/체크/잠금 mesh뿐이다. 타입은 이미 공개된 값만 받는다.
- 핵심 동작: Combat 교차검, Event 물음표, Treasure 상자, Shop 가게, Rest 모닥불, Boss 왕관. null은 점선 빈 링과 다이아몬드로 표현하여 Event와 구분한다. authored 실제 sprite가 있으면 유형 그림만 생략하고 상태 링/배지는 유지한다. 현재 위치는 파랑, 가능 경로는 발광, 선택은 네 방향 ticks, 완료는 체크, 접근불가는 자물쇠로 표시한다.
- 사용하는 대상: uGUI MaskableGraphic/VertexHelper/CanvasRenderer, NodeType, Unity 수학/색상. 사용하는 쪽: ExplorationNodeView.Bind/BindCampaign 및 legacy 미리보기의 SetSelected, 그림이 없는 FateCardView.Bind의 공개 유형 도형. CampaignMapAuthoring이 기존 Node 원본에, FateChoiceAuthoring이 FateCardView 원본에 이 Graphic과 직접 참조를 작성한다.
- 상태/수명: 공개 표시 플래그만 보유, Bind 때 mesh dirty. raycast 비차단. 원본/복제 노드와 수명을 공유하며 게임 저장·RNG·런 상태를 접근하지 않는다.
- 검수 주의: 공개 타입 null에서 실제 아이콘을 그리지 않아야 한다. 원형 배지 크기와 밝기 외 상태 구분, 실제 sprite 우선순위는 Main 원본/캡처 검증 대상이다.

- 캡처 보완: MaskableGraphic을 상속해 ScrollRect의 RectMask2D 안에서만 렌더링한다. 지도 선/노드가 상단 HUD나 하단 조작 영역으로 넘치지 않는다.
