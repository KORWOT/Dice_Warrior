# ExplorationNodeView.cs

- 역할: 개별 공개 노드 ID/유형/그림과 입력·선택·fade를 공통 프레임에 연결한다. 진행/도달성/보상/RNG를 계산하지 않는다.
- 입력/API: 기존 Bind(nodeId,type,label,visual,style,selectable,selected,callback)와 새 BindCampaign(publicNode,current,selected,visual,style,preview). 출력은 NodeId, legacy Type, nullable PublicType, Revealed, IsFading이다. 신규 화면은 PublicType만 소비하며 미공개 type에서 기존 Type의 enum 기본값을 표시하지 않는다.
- 핵심 동작: 기존 artwork 우선순위 icon→artwork를 보존한다. 그림이 없으면 authored MapNodeGraphic이 실제 공개 유형 아이콘을 그린다. 미공개는 점선 빈 원/다이아몬드/미발견이며 Event의 물음표와 구별한다. 이동 가능 발광 링, 미리보기 ticks, 파란 현재 상태, 완료 체크, 접근불가 자물쇠를 별도 표시한다.
- authored 대상: CommonButtonView frame, MapNodeGraphic mapGraphic, 원본별 layoutVersion. CommonButtonView listener 소유와 색상/폰트 복원 계약을 유지한다. ApplyMapAppearance는 같은 어셈블리의 CampaignMapView가 legacy 완료 상태를 추가 표시할 때만 호출한다.
- 상태/수명: Bind/Unbind/OnDisable은 이전 fade를 취소하고 generation을 올린다. fade 시작 즉시 Button/CanvasGroup 입력 차단, 유한 비음수 초만 허용, 0초는 즉시 숨김. 완료는 alpha0/숨김만 수행하여 ID를 유지한다. Unbind는 ID/공개 유형을 비운다.
- 직접 관계: CampaignMapView → ExplorationNodeView → CommonButtonView, MapNodeGraphic, VisualArtwork/ButtonAppearance. 실제 이벤트 ID나 RunState가 전달되지 않는다.
- 검수 주의: 불가 노드도 화면에는 남지만 입력 불가여야 한다. Rebind 후 이전 listener/그림/코루틴이 남으면 안 된다. 실제 원본, fade, 좌표·입력 회귀는 Main 통합 실행 증거로 판정한다.
