# FateDiceWidgets.cs

- 역할: authored 화면의 반복 선택지/행동/운명 카드/주사위 항목과 CampaignMapView 연결을 맡는다. Canvas 및 화면 고정 레이아웃은 생성하지 않는다.
- 기존 API/키 유지: Choice/Choices/ShowDice/ActionCard/FateCard, Buttons의 die-0..5/node-ID/card-ID/fate-ID 등. ShowDice의 6개 항목/표시와 유료 재굴림 callback은 유지한다.
- 지도 변경: ShowMap은 mapContainer에 authored된 CampaignMapView.Bind로 위임하고 available ID 버튼만 진단 사전에 등록한다. 직접 root별 행을 반복 생성하던 지도 코드는 제거했다.
- 이동 API: AnimateNodeArrival(string id):IEnumerator는 현재 CampaignMapView의 표시 전용 이동과 원본 arrivalSeconds를 사용한다.
- Clear(preserveMap): 같은 ExplorationUI 재바인딩에서는 지도를 보존해 사라진 가지의 fade를 이어간다. 다른 화면으로 닫으면 지도와 반복 항목을 정리한다.
- 수명: 반복 항목은 local view callback을 해제하고 비활성화한 후 Play의 Destroy/Editor의 DestroyImmediate로 제거한다. authored 부모/텍스트/스타일은 소유하지 않는다.
- 관계: RunScreenView와 각 typed concrete UI가 사용한다. RunUIController는 노드 선택 저장 성공 후 AnimateNodeArrival를 호출한다. 화면 코드가 Session/규칙/저장소를 참조하지 않는 경계를 유지한다.
- 검증: 기존 6개 굴림 규칙을 변경하지 않았다. 이번 소스의 실행 검증은 Main 통합 결과 대기.
- LMD-C02: ShowMap의 마지막 optional completed 인수를 CampaignMapView.Bind로 그대로 전달한다. 완료 여부 계산과 저장 조회는 하지 않는다.



## 조합·카드·전투 피드백 (2026-09-08)

- AnimateCardSelection(key,seconds)는 Buttons의 실제 선택 카드 하나만 .07 비율까지 확대하고 배경색을 강조한다. unscaledTime을 사용하며 원래 scale/color는 finally, ResetSelectionFeedback 및 Clear에서 복원한다. 규칙/저장/난수는 호출하지 않는다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.


## 주사위 조합 연출 (2026-09-10)

AnimateCardSelection은 버튼에 작성된 SelectionFeedback이 있으면 FEEL 재생을 위임한다. 기존 선택 시간과 마지막 ResetSelectionFeedback을 유지하고 이전 fallback과 동시에 scale/color를 쓰지 않는다. Clear/중단 시 소유 효과를 정지·복원한다.
검증 상태: DICE_PRESENTATION_EFFECTS_REPORT의 실제 결과를 따른다.


## 등급별 연출 수명 (2026-09-10)

AnimateCardSelection(key)은 실제 SelectionFeedback.Play 완료를 기다린다. 컴포넌트 없는 선택지의 fallback pulse는 context.presentation.actionSeconds를 직접 소비한다. AnimateNodeArrival(id)은 CampaignMapView.arrivalSeconds를 사용한다. Controller는 시간 추측 없이 반환 수명을 기다린다. 표시·저장 경계는 유지한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.

## 절차 지도 표시 연결 (2026-09-10)

- ShowCampaignMap(parent,nodes,currentId,selectedId,preview)는 공개 CampaignNodeUIData 목록을 authored CampaignMapView.BindCampaign에 위임한다. parent 또는 authored map 컴포넌트가 없으면 명시적으로 실패하며 대체 Canvas/레이아웃을 생성하지 않는다.
- 입력은 공개 전체 그래프와 현재/미리보기 선택 ID, 노드 미리보기 콜백이다. context의 explorationNode 원본과 visual catalog를 함께 전달한다. 지도 배치·유형 공개·도달성 판정·이동 명령을 여기서 계산하지 않는다. available인 실제 노드 버튼만 Buttons["node-"+id]에 등록한다.
- 기존 ShowMap은 모드0 표시 API로 유지된다. 다른 authored map으로 바뀌면 이전 map을 정리하고, 같은 ExplorationUI 재바인딩은 Clear(preserveMap:true)로 지도 수명을 보존한다. 반복 항목과 진단 Buttons는 바인딩마다 정리한다.
- 별도 FateChoiceUI는 자신의 카드/여섯 주사위/선택·확정 콜백을 소유한다. Widgets의 FateCard API는 기존 재사용 경로로 남으며 Controller의 신규 운명 popup 선택 연출은 FateChoiceUI.PlaySelection으로 직접 연결된다.
- 사용하는 쪽과 관계: ExplorationUI → ShowCampaignMap/ShowMap → CampaignMapView; RunUIController → AnimateNodeArrival; 다른 RunScreenView → 반복 선택지·카드·주사위 API. RunState/RunSession/저장소·난수 참조를 추가하지 않는다.
- 검수 주의: 레거시 id 키, 재바인딩 시 callback 제거, authored map 파괴 경계, 새 popup과 Widgets의 선택 연출 이중 소유 금지. 실제 실행 상태는 Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md를 따른다.
