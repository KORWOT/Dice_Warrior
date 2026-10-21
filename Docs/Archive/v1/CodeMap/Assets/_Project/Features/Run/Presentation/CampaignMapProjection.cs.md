# CampaignMapProjection.cs

- 원본: Assets/_Project/Features/Run/Presentation/CampaignMapProjection.cs
- 역할: 확정된 RunState의 지도·진척을 공개 전용 CampaignNodeUIData와 ExplorationUIData 필드로 변환하는 단일 표시 경계다. 지도 생성·명령 실행·저장·난수를 소유하지 않는다.
- 입력/API: Apply(ExplorationUIData target,RunState state)는 지도와 HUD 필드를 채운다. Build(RunState state)는 모드1의 공개 노드 배열을 반환하고 모드0은 null을 반환한다. null 인수는 ArgumentNullException으로 거절한다.
- 핵심 동작: nodes와 nodeHistory의 합집합을 floor/lane/id 순으로 정렬한다. active/available/resolvedEventIds를 각각 사용해 접근 가능·선택 가능·완료를 구분한다. unreachable은 활성 목록에 없고 완료하지 않은 기록이다. childIds는 새 배열로 복사하고 저장된 좌표·연결을 유지한다.
- 공개 범위: 현재 도착층(selectedNode.floor, 유효 선택 ID가 없으면 eventsResolved)에 저장된 world.previewDepth를 더한 층까지 type을 제공한다. 기본 제작값은 다음2층이며 Boss 표식은 거리에 관계없이 공개한다. 더 먼 일반 노드는 type=null이고 revealed=false다. 구체 사건·보상 정보는 전혀 전달하지 않는다.
- 현재 위치: HasSelectedNode는 객체의 null 여부보다 비어 있지 않은 id를 확인한다. schema1 JsonUtility 복귀에서 absent selectedNode가 빈 객체로 복원돼도 마지막 resolvedEventIds를 현재 위치로 쓰고 공개 깊이를 eventsResolved 기준으로 계산한다.
- 출력: mapFloors는 eventsToBoss(일반층 수), mapProgressLabel은 해결한 일반 사건/남은 층, mapGoldLabel/mapHealthLabel/mapWardLabel은 실제 금액·HP·수호·레벨의 한국어 문구다. 신규 모드1에서는 nodes/completedNodes를 빈 배열로 지워 미공개 원본 유형을 병행 DTO로 전달하지 않는다. 모드0의 기존 노드 배열은 유지한다.
- 관계 근거: RunUIController.Render와 UIWorkbenchPreview.Render가 Apply를 호출한다. Apply → Build/GrowthRules.Stats/KoreanText, 결과는 ExplorationUI → FateDiceWidgets → CampaignMapView가 소비한다. View에서 RunState를 조회할 필요가 없다.
- 상태/수명: static 순수 표시 변환이며 지속 캐시·이벤트 구독이 없다. 호출별 배열을 생성하고 입력 상태/규칙/게임 rngState를 변경하지 않는다.
- 검수 주의: 전체 연결 보존과 유형 은닉은 별개다. 접근 불가 가지를 완료로 오인하지 않고 빈 selectedNode 저장 복귀, 좌표 안정성, 원본 배열 비공유를 확인한다. 신규 생성 모드1/구형0 규칙 자체는 ExplorationRules/ProceduralMapGenerator 요약을 따른다.
- 계약·실제 검증: Docs/Plans/PROCEDURAL_CAMPAIGN_PLAN.md, Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md. 이 소스 요약은 실행 PASS를 주장하지 않는다.
