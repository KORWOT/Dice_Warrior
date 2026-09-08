# FateDiceWidgets.cs

- 역할: authored 화면의 반복 선택지/행동/운명 카드/주사위 항목과 CampaignMapView 연결을 맡는다. Canvas 및 화면 고정 레이아웃은 생성하지 않는다.
- 기존 API/키 유지: Choice/Choices/ShowDice/ActionCard/FateCard, Buttons의 die-0..5/node-ID/card-ID/fate-ID 등. ShowDice의 6개 항목/표시와 유료 재굴림 callback은 유지한다.
- 지도 변경: ShowMap은 mapContainer에 authored된 CampaignMapView.Bind로 위임하고 available ID 버튼만 진단 사전에 등록한다. 직접 root별 행을 반복 생성하던 지도 코드는 제거했다.
- 신규 API: AnimateNodeArrival(string id,float seconds):IEnumerator는 현재 CampaignMapView의 표시 전용 이동을 위임한다.
- Clear(preserveMap): 같은 ExplorationUI 재바인딩에서는 지도를 보존해 사라진 가지의 fade를 이어간다. 다른 화면으로 닫으면 지도와 반복 항목을 정리한다.
- 수명: 반복 항목은 local view callback을 해제하고 비활성화한 후 Play의 Destroy/Editor의 DestroyImmediate로 제거한다. authored 부모/텍스트/스타일은 소유하지 않는다.
- 관계: RunScreenView와 각 typed concrete UI가 사용한다. RunUIController는 노드 선택 저장 성공 후 AnimateNodeArrival를 호출한다. 화면 코드가 Session/규칙/저장소를 참조하지 않는 경계를 유지한다.
- 검증: 기존 6개 굴림 규칙을 변경하지 않았다. 이번 소스의 실행 검증은 Main 통합 결과 대기.
- LMD-C02: ShowMap의 마지막 optional completed 인수를 CampaignMapView.Bind로 그대로 전달한다. 완료 여부 계산과 저장 조회는 하지 않는다.



## 조합·카드·전투 피드백 (2026-09-08)

- AnimateCardSelection(key,seconds)는 Buttons의 실제 선택 카드 하나만 .07 비율까지 확대하고 배경색을 강조한다. unscaledTime을 사용하며 원래 scale/color는 finally, ResetSelectionFeedback 및 Clear에서 복원한다. 규칙/저장/난수는 호출하지 않는다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
