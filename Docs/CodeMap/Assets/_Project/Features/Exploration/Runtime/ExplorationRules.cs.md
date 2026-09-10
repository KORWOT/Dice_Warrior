# ExplorationRules.cs

## RA-C 현재 경계 (2026-09-09)

상태 인자는 RunStateData, 설정 접근은 Rules로 변경되고 Core asmref에 소속된다. 그래프 생성·가지 정리·이력·보스 전환·노드 ID·단일 RNG 소비 순서는 그대로다. RunApplication이 명령 후보에서 호출하고 Runtime 지도는 확정 표시 DTO만 사용한다.

기존 수식/행동 보존 및 Core 컴파일/전체 패·명령 재연은 ROGUELIKE_ARCHITECTURE_REPORT의 새 실행 증거를 따른다. 아래 기존 단계 증거를 RA-C PASS로 재사용하지 않는다.

- 역할: 활성 탐험 노드 생성, 프리뷰 확장, 선택 경로 정리와 사건 완료 후 다음 Map 진입. UI 참조 없음.
- Initialize: 활성 노드/available IDs/이력을 초기화하고 기존 branchCount/previewDepth/nodeWeights로 생성한다.
- PruneTo: roots에서 childIds를 따라 HashSet으로 도달 가능한 활성 ID를 구한다. 도달 불가능한 NodeState를 nodeHistory로 옮긴 뒤 활성 목록에서 제거한다. ID/유형/연결과 RNG/진척은 바꾸지 않는다. 공유 자식은 한 번 방문하고 유지한다.
- Advance: 선택 노드의 자식을 다음 available로 설정하고 선택 노드를 이력에 보관한다. 기존 threshold 도달 시 다음 루트만 Boss로 변환하고 childIds를 비운다. 이력은 보관 시점의 기록이며 보스 전환 규칙을 새로 만들지 않았다.
- 직접 사용: RunState/NodeState/GameConfigData, DiceRules.WeightedIndex. 호출자: RunSession.New/ChooseNode/보상 후 진행.
- UI 관계: RunUIController가 활성 노드·available IDs와 검증한 완료 경로를 DTO로 공급한다. ExplorationUI → FateDiceWidgets → CampaignMapView는 전달받은 공개 membership/ID/연결만 표시하고 제거된 가지를 fade한다. Widgets와 맵은 이 규칙이나 nodeHistory를 직접 읽지 않으며 fade가 기록을 삭제하지 않는다.
- 검증: A→C,D / B→C,E 합류 oracle, 선택/보상 후 이력·RNG·디스크 왕복 및 기존 경로/보스 회귀.

## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- nodeHistory에는 선택하지 않아 제거된 가지와 사건을 끝낸 노드가 함께 들어 있다. 표시용 완료 경로는 RunUIController가 resolvedEventIds 순서로 nodeHistory의 일치 노드를 찾아 ID/유형/childIds를 복사한 ExplorationUIData.completedNodes다. 이력을 전부 완료 경로로 취급하지 않는다.
- CampaignMapView는 이 DTO와 활성 그래프를 단일 ID로 배치하고 전달된 실제 연결선으로 분기/합류를 표현한다. 완료 노드는 비선택 스타일로 남고, 보상 이후 및 저장 재개에도 컨트롤러가 같은 기록에서 표시 데이터를 재구성한다. 맵 자체는 RunState/nodeHistory/RNG를 조회하지 않는다.
- 세로 배치·현재 위치·도착/fade·preview 레이아웃은 Presentation 책임이다. Initialize/PruneTo/Advance의 공개 깊이, 합류 보존, Boss 전환과 노드 생성 RNG는 기존 RunSession 명령 경계에서 실행된다.
- 문서 범위: 실제 소스와 이번 작업의 staged DTO/컨트롤러/맵 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.
