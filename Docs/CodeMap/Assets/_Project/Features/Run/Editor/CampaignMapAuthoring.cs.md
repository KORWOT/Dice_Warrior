# CampaignMapAuthoring.cs

- 역할/API: Editor 전용 Apply():string은 기존 버전1 이행을 보존하고, ApplyProcedural():string은 원본2개의 절차 지도 배치를 버전2로 작성한다.
- 고정 쓰기 대상: Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab, Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab만 LoadPrefabContents/SaveAsPrefabAsset/UnloadPrefabContents로 쓴다. 씬/폰트/FEEL/구성 SO/다른 자산/광역 SaveAssets 쓰기 없음.
- 사전 조건: 정지 Editor, 컴파일/import 완료, 모든 열린 scene clean, MainStage와 Prefab Stage 미사용. 기존 원본과 CommonButtonView/폰트 참조가 필요하다. 조건 불충족은 예외로 멈추며 잠금/환경 설정을 우회하지 않는다.
- 핵심 동작자산: 노드 원본의 root/frame/상속/GUID/폰트를 유지하고 원형 MapNodeGraphic을 추가한다. 실제 sprite 자리는 유지하며 투명한 기존 Image가 Button hit area를 제공한다. 화면의 기존 단일 ScrollRect/CanvasGroup/HUD 참조를 보존하고 위 진행/골드, 중앙 전체 지도, 아래 체력/수호·공개 설명·이동/복구 행동, 범례 접기를 편집 가능한 자식으로 작성한다.
- idempotency: ExplorationUI.layoutVersion와 ExplorationNodeView.layoutVersion를 각각 검사한다. 둘 다2이면 no-op, 한 원본만 이전 버전이면 그 원본만 작성한다. 버전은 해당 원본 작성 직후 저장에 포함한다. 새 고정 자식은 이름으로 재사용하며 중복을 생성하지 않는다.
- 직접 관계: Main의 ProceduralCampaignAuthoring → ApplyProcedural → ExplorationUI/CampaignMapView/ExplorationNodeView/MapNodeGraphic와 기존 Unity UI 컴포넌트. 자체 Unity 실행/메뉴 자동 호출 없음.
- 검수 주의: Main이 Editor API 호출·재호출 no-op·원본별 버전·GUID/FEEL/폰트 hash·두 비율 캡처를 검증한다. campaign_view 담당은 소스만 작성하며 자산 결과와 실제 실행 PASS는 Main 증거가 기준이다.
- 최종 회귀 보완: 화면 footer/rollChoices 높이108로 공통 버튼 최소 높이102를 담는다. 이미 이번 작업에서 작성된 버전2 원본에는 Main이 같은 두 LayoutElement만 Editor API로 보정했다. 이후 ApplyProcedural은 사용자 편집을 유지한다.

## CAMPAIGN_FLOW_POLISH 즉시 이동 원본

- 신규 공개 ApplyDirectTravel():string은 기존 버전2 ExplorationUI 원본만 LoadPrefabContents/SaveAsPrefabAsset/UnloadPrefabContents로 버전3에 이행한다. 이 진입점은 노드/전투/다른 원본을 저장하지 않는다.
- moveButton은 삭제하지 않고 비활성·interactable=false로 작성하여 기존 직렬화 참조/GUID를 보존한다. 기존 VerticalLayoutGroup은 비활성 항목의 공간을 제외한다. 하단 안내는 노드 탭 즉시 이동으로 갱신한다.
- 기존 정지/컴파일·import/dirty scene/Prefab Stage guard를 재사용한다. ExplorationUI.layoutVersion>=3이면 no-op이고, 버전2가 아니거나 필수 참조가 없으면 실패한다. 기존 Apply/ApplyProcedural API는 그대로 유지한다.
- Main의 CampaignFlowAuthoring이 정확한 허용 자산 범위에서 호출한다. 실제 작성·두 번 재적용·GUID/hash·화면비 검증은 Main 수행이며 이 작성 담당은 Unity를 실행하지 않는다.
