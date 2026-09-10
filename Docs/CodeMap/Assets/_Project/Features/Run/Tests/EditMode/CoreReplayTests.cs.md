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

## 절차 지도 도입에 따른 구형 재연 경계 (2026-09-10)

- 보완 대상은 모드0 고정 fixture에 새 직렬화 필드가 추가된 차이다. 기존 ReplayBaseline.json과 모든 expectedHash를 그대로 유지한다. 전체 EditMode 최초 실행에서 기존 재연14개가 첫 ChooseNode의 전체 hash에서 실패했으며 명령 수락·sequence·rngState·phase 비교는 먼저 통과했다.
- 최초 실제 snapshot에서 config.world의 mapGenerationVersion/mapColumns/mapPathCount와 nodes/nodeHistory/selectedNode의 floor/lane은 모두0이다. 기존 RA-D projection 뒤 이29개 필드만 제외하면 원래 기대 hash `549999042D930D052E11CFCD21F24F9774D58362A6A8347FB28637D5771A7D51`과 정확히 일치한다. 새 필드를 포함한 hash는 실제 실패의 `038B99FFB0BB7F7B0902CF71BC82E3018DBA51F6645DFC46B39B4221D49CB232`다.
- 적용한 ProjectHistoricalMapFields는 정확한 world/노드 경로에서 신규 필드의 존재·정수 타입·0 값을 먼저 검사하고 해당 필드만 제거한다. 모드1이나 0이 아닌 좌표/설정을 legacy로 간주해 숨기지 않는다. 기존 ID/유형/childIds/시간/RNG/진행/내용 필드는 계속 원래 canonical hash에 포함한다.
- 증거: artifacts/procedural-campaign/edit-initial.json, replay-first-actual.json, replay-first-state-comparison.json. 오프라인 비교는 Mono/Json.NET의 기존 double R 형식으로 최초 명령7개의 기존 기대 hash와 현재 실패 hash를 모두 재현했다. 이는 전체 Unity 재연14개의 재실행 PASS를 대신하지 않는다.
- 최종 실행: Main이 replay-fix.patch를 소스에 적용하고 Unity EditMode의 고정 재연14개를 다시 실행하여 14/14 PASS(9.18초)했다. 증거는 artifacts/procedural-campaign/replay-final.json이다. 기존 golden fixture는 변경하지 않았다. 전체 작업 판정은 Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md를 따른다.
