# CampaignMapAuthoring.cs

- 역할/API: Editor 전용 static Apply():string. ExplorationUI.prefab과 ExplorationNodeView.prefab 두 기존 원본만 명시된 경로로 이행한다.
- guard: Play/전환, 컴파일/import, dirty scene, Prefab Stage 및 임시 Preview Stage를 거절한다. LoadPrefabContents/UnloadPrefabContents만 사용해 unsaved additive scene 충돌을 만들지 않는다.
- idempotency: ExplorationUI.layoutVersion >=1이면 변경 없이 반환한다. 버전은 화면 구성 후 마지막 저장에 기록한다. 광역 SaveAssets/씬 저장/config 변경 없음.
- Node original: CommonButtonView variant root/컴포넌트와 기존 그림/폰트/fontStyle 상속을 유지한다. 아이콘 위/한글 유형 아래의 작은 노드 레이아웃과 기본106 높이를 작성한다.
- Exploration original: root/RunScreenLayout/CanvasGroup/기존 ScrollRect를 보존한다. compact HUD, 넓은 지도 viewport, 기존 안내/roll/fate section을 재배치하고 mapContainer에 CampaignMapView 및 editable edge/node layers, 현재 위치 Text, player marker를 작성한다.
- 자산: 외부 그림/폰트/새 이미지 자산 없음. 기존 폰트와 Unity builtin UI sprite만 사용한다. 두 프리팹 외 자산 저장 없음.
- 관계: Main이 소스 통합 후 Apply를 실행한다. CampaignMapView/ExplorationUI 신규 serialized 필드에 직접 연결하며 생산 UI는 그 원본을 복제한다.
- 검증: B는 Editor API 소스만 작성했고 Unity 실행/자산 수정은 하지 않았다. 실제 authoring, 2회 no-op, 보호 GUID/상속, GUI 검증은 Main 증거로 기록해야 한다.
