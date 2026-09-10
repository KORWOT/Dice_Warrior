# CoreReplayTests.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 분리 전 고정 초기 상태와 273명령의 결과 보존 검사.
- 입출력/핵심 동작: RA-B 코드에서 미리 확보한 7시나리오/273명령과 각 전체 상태 hash를 사용한다. 연속 실행 및 명령마다 실제 LocalRunStore 저장/복원하는 두 방식에서 ID/인자/시간/RNG/sequence/phase와 모든 JSON 필드를 비교한다.
- 직접 사용하는 대상: Fixtures/ReplayBaseline.json, RunSession/LocalRunStore, NUnit, 기존 Newtonsoft.Json.
- 직접 사용하는 쪽: Unity EditMode Test Runner.
- 상태/수명·검수 주의: 정규화는 JSON 객체 property 순서만 정렬한다. 선택 ID/시간/runId 등을 제외하지 않는다. 격리 임시 저장만 쓰며 사용자 저장은 사용하지 않는다. 새 코드 결과로 고정 기대값을 재생성하지 않는다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.

- RA-D: C 이전 재연 fixture/기대hash는 그대로 둔다. 새 effects/addActionId/배율이 비어 있고 상점 snapshot이 기존 ID/기본가격과 같음을 먼저 검사한 뒤 그 신규 필드만 구형 projection에서 제외한다. 기존 모든 필드/명령ID/선택/시간/RNG hash는 계속 비교한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
