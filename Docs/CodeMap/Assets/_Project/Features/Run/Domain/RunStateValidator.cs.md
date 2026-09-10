# RunStateValidator.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 저장 및 명령 후보의 순수 불변조건 검증.
- 입출력/핵심 동작: Validate(RunStateData)는 schema/규칙/ID/graph/phase/자원/패/확정 카드/보상/기록 의미를 검사하고 RunStateValidationException을 던진다. 상태/RNG/시간을 변경하지 않는다. null과 비어 있는 inline DTO는 phase/ID에 따라 같은 부재 의미로 허용한다.
- 직접 사용하는 대상: RunStateData.Rules 및 순수 규칙.
- 직접 사용하는 쪽: RunApplication, Runtime RunSession/LocalRunStore.
- 상태/수명·검수 주의: 기존 Runtime 파일을 GUID 유지 이동했다. LocalRunStore만 파일 경로를 붙인 InvalidDataException으로 감싼다. Runtime 카탈로그 Validate override는 표시 설정까지 확인한다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.

- RA-D: pendingReward.addActionId와 absent reward의 공백 여부, ShopRules.Validate의 고정 상품/가격/상점 수명을 명령·저장 경계에서 검사한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


## 절차 지도 버전1 검증 (2026-09-10)
- legacy0의 고정 available.Count==branchCount, 대운명 임계 및 기존 DAG/이력 검사는 유지한다. 버전1의 available 수는 가변이며 현재 진척+1 층에 있어야 한다.
- active+history 합집합에서 floor/lane 유효·좌표 중복·정확히 다음 층/일반 인접 lane·일반 막다른 길 없음·일반 자식1..3·단일보스 종점·시작 루트2..min(columns,pathCount)·고립 노드 없음·좌우 간선 교차 없음을 검사한다. 보스 간선은 인접 lane 제한만 제외한다.
- 완료 ID 순서는 각 일반층1..eventsResolved의 연결된 실제 노드여야 한다. 다음 available은 마지막 완료 노드 childIds와 같고, 선택 노드도 완료 경로에 연결되어야 한다. 치명적 사건 보상으로 현재 노드까지 resolved에 기록된 Result는 현행 의미대로 허용한다.
- selectedNode의 저장 복사 일치에는 floor/lane을 포함한다. Map의 inline absent Node는 기존 기본필드와 좌표0을 요구한다. 검증은 상태·RNG·디스크를 변경하지 않는다.
- 직접 사용/사용자 및 실패 경계는 동일하다. 새 generationVersion 검증은 RunRulesCatalog, 실제 생성은 ProceduralMapGenerator다. 실행 결과는 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
