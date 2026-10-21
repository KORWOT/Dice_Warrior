# CampaignPathGraphic.cs

- 역할: 한 개의 공개 지도 연결을 가벼운 uGUI 점선 mesh로 그린다.
- 입력/출력: RectTransform 길이·두께·회전과 Graphic.color, Inspector dashLength/gapLength → 같은 Rect 안의 반복 quad. 양 끝 노드나 게임 상태를 직접 읽지 않는다.
- 사용하는 대상: uGUI MaskableGraphic/VertexHelper/CanvasRenderer, Mathf. 사용하는 쪽: CampaignMapView가 신규 지도 경로마다 한 인스턴스를 만들고 위치·색상을 갱신한다. legacy 경로는 기존 Image를 사용한다.
- 상태/수명: mesh dirty 때 재생성하며 입력 raycast를 차단하지 않는다. 객체 수명·Destroy는 생성자인 CampaignMapView 소유다. 자체 코루틴·RNG·전역 참조 없음.
- 검수 주의: 길이0은 mesh0, dash/gap은 최소1로 보호한다. 실제 두 화면비의 선 가독성은 Main 통합 캡처로 검증한다.

- 캡처 보완: MaskableGraphic을 상속해 ScrollRect의 RectMask2D 안에서만 렌더링한다. 지도 선/노드가 상단 HUD나 하단 조작 영역으로 넘치지 않는다.
