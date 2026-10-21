# ExplorationUI.cs

- 역할: 공개 캠페인 지도, 상단 진행/골드, 하단 체력/수호·경로 설명과 접는 범례를 cached typed 화면에 표시한다. 입력은 ExplorationUIData이며 Core/저장/RNG를 읽지 않는다.
- authored 참조: 기존 layout/instructions/mapContainer/rollChoices/fateChoices/campaignMap을 보존한다. 버전2는 progressLabel/goldLabel/healthLabel/wardLabel/selectedDetails/moveLabel/legendLabel, moveButton/legendToggle/legendBody를 직접 참조한다.
- 핵심 동작: CampaignNodeUIData가 있으면 Widgets.ShowCampaignMap, 없으면 기존 ShowMap으로 짧은 legacy 지도를 표시한다. 지도는 운명 선택 중에도 남고 카드 자체는 별도 FateChoiceUI가 소유한다. Controller의 roll/reopen 선택은 rollChoices에 그대로 표시한다. HUD의 원래 stats/situation Text는 진단용 원문을 보존하되 dedicated 표시 문자열로 화면을 구성한다.
- 입력/출력: 이동 가능한 노드 탭에서 IsOpen/활성/현재 bindingVersion/화면 CanvasGroup 입력/available·완료·접근불가를 확인하고 제출 latch를 건 뒤 주입한 chooseNode(id)를 한 번 호출한다. SelectedNodeId와 공개 설명은 진행 요청의 표시 상태이며 실제 이동은 Controller의 저장·도착 연출 책임이다. moveButton/moveLabel의 직렬화 참조는 유지하지만 버튼은 항상 비표시·입력 불가이고 Widgets에는 map-legend만 추가 등록한다.
- 상태/수명: bindingVersion이 이전 노드 callback을 거절한다. 각 Bind에서 자신의 범례 listener만 해제하고 선택/submit 상태를 초기화한다. 성공 요청은 다음 Bind까지 잠기며 저장 실패도 Controller가 현재 상태로 RefreshView하여 재시도를 복원한다. callback이 직접 예외를 던지면 동일 바인딩에서만 이전 선택/submit을 복원한다. 동기 rebind가 발생하면 이전 호출이 새 상태를 덮어쓰지 않는다. Unbind는 자체 listener/선택을 정리하고 기본 RunScreenView가 Widgets.Clear를 호출한다.
- 관계 근거: RunUIController/UIWorkbenchPreview → ExplorationUIData → 이 View → FateDiceWidgets/CampaignMapView. CampaignMapAuthoring.ApplyProcedural이 실제 원본의 참조와 편집 가능한 배치를 작성한다. UIManager가 modal 입력과 화면 cache를 관리한다.
- 검수 주의: 단일 탭 뒤 저장1회·정확한 노드 이동, 빠른 중복·재바인딩 후 이전 delegate 거절, 저장 실패의 현 위치/RNG/save 보존·재시도를 Main이 검증한다. 기존 Button 아래 ScrollRect와 InputSystemUIInputModule의 drag 시작시 eligibleForClick 해제를 유지하며 실제 down→드래그→up으로 이동 불발을 검사한다. 소스 구조만으로 PASS를 주장하지 않는다. CAMPAIGN_FLOW_POLISH의 승인된 즉시 이동 계약이 이전 PROCEDURAL_CAMPAIGN의 선택 후 이동 버튼 계약을 대체한다.
