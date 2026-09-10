# ProceduralMapGenerator.cs

- 역할/소속: FateDice.Core. 생성 버전1의 일반층 전체와 단일 보스를 런 시작 시 만드는 순수 절차 지도 생성기다.
- 입력/출력: Initialize(RunStateData)는 저장 규칙의 eventsToBoss(1..100), mapColumns(2..7), mapPathCount(2..12), initialSeed를 사용한다. floor1부터 N까지 일반층, N+1 단일 보스, 저장될 lane/안정ID/childIds/초기 availableNodeIds를 구성한다.
- 핵심 동작: 지역 mapRng를 initialSeed에서 고정 혼합해 파생한다. 2..min(columns,pathCount)의 고유 시작 lane을 고르고 pathCount개의 다음 층 인접 lane 경로를 만든다. 앞선 간선과 좌우 순서가 뒤집히는 후보를 제외하며 같은 자식 합류는 허용한다. 모든 시작에서 끝까지 경로를 만든 뒤 층·lane 순으로 노드와 유형을 고정하고 마지막 층을 한 보스로 연결한다.
- 직접 사용하는 대상: RunStateData, WorldSettings, NodeState, DiceRules.WeightedIndex. 지역 난수는 생성 버전1 안의 고정 순서이며 state.rngState를 읽거나 변경하지 않는다. Unity/시간/파일/표시 코드를 사용하지 않는다.
- 사용하는 쪽: ExplorationRules.Initialize가 mapGenerationVersion==1일 때 호출한다. RunApplication.New의 기존 후보 초기화·검증·checkpoint 경계 안에서 실행된다.
- 상태/수명: 전체 생성 성공 후에만 nodes/nodeHistory/availableNodeIds/nextNodeId/selectedNode/phase를 교체한다. 이벤트 진행 때 재호출하지 않으며 활성 가지/이력은 기존 PruneTo가 관리한다. 노드 ID는 runId와 nextNodeId를 사용하며 ID 생성에 난수를 소비하지 않는다.
- 검수 주의: seed 혼합 상수·후보 순서·경로 iteration 변경은 버전1 재현 계약 변경이다. 게임 RNG와 분리했지만 legacy 버전과 같은 시드의 주사위열 일치를 주장하지 않는다. 마지막 보스 간선만 열 인접 제한을 적용하지 않으며 모든 간선은 정확히 다음 층으로 연결한다.
- 검증: ProceduralMapTests의 여러 seed/층수 경계/비교차/도달성/가변 분기·합류/게임 RNG 독립/명령·디스크 재현 검사. 실제 실행 상태는 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
