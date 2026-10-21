# SeedAndPresentationTests.cs

## RA-B 상태 경계 fixture 전환 (2026-09-09)

세션의 State는 독립 표시 복사다. 준비 상태를 지역 RunState DTO에 구성한 뒤 새 RunSession 또는 격리 저장소에 전달한다. 규칙 기대값과 기존 버튼/저장/재연 assertion은 유지한다. 잘못된 저장 검사는 동일한 수정 DTO를 LocalRunStore.Save에 전달한다. 실제 실행 증거는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


- 역할: RA-A의 시드 공급·표시 설정·구형 저장 호환·규칙 불변을 검증하는 EditMode NUnit 테스트다. 매개변수 확장 기준 32개이며 실행 개수/결과는 메인의 실제 Unity 증거를 따른다.
- 핵심 동작: FixedSeedSource는 1/77/uint.MaxValue를 반복 반환하고 0을 거절한다. SystemSeedSource의 32회 결과는 모두 nonzero이며 Unity Random 상태를 소비하지 않는다. 무작위 연속 값의 불일치를 성공 조건으로 삼지 않는다. inactive RunUIController에서 Seed=0 거절 후 이전 고정 값 보존을 확인한다.
- 표시 입력/출력: 새 두 그룹의 독립 인스턴스와 .65/.9 기본값, 기존 rollSeconds보다 새 0/0의 우선 적용 및 저장 왕복, 탐험/전투 각 두 필드의 음수·NaN·양/음 무한값 16개 거절을 검사한다. 한 그룹만 없는 경우 해당 그룹에만 legacy fallback이 적용되어야 한다. RA-A-01의 두 회귀는 DeepCopy/SO Snapshot/New에서1.25/.9 유지, 반대쪽0 보존, null 원본과 복사 간 독립성을 검사한다.
- 복원: 실제 schema 1 payload에서 explorationDice/combatDice 필드 자체를 제거하고 SHA256 envelope를 작성한다. legacy .2→.65 및 1.25→1.25, 결과 .9 복원을 확인한다. 전체 상태의 presentation/playedSeconds만 정규화하여 규칙·모든 ID·후보·RNG를 비교하며 Load 전후 파일 bytes와 재저장 후 유효 timing을 확인한다. 새 설정은 원본 config가 바뀌어도 저장 값으로 복원해야 한다.
- 규칙 재연: 같은 RunSession.New(88) 시작 스냅샷을 깊게 복사하고 두 번째의 표시 설정만 바꾼다. 동일 노드/카드 ID와 재굴림 인자2/4로 노드→탐험 굴림/재굴림→전투/재굴림→행동→보상→보스→승리까지 실행한다. 모든 명령 경계에서 전체 규칙 JSON 및 각각의 실제 디스크 왕복을 비교한다. runId나 그를 포함한 node/card ID는 정규화하지 않는다. Reroll 권한/충전량만 동일한 테스트 시작 상태로 주입한다.
- 사용하는 대상: ISeedSource/FixedSeedSource/SystemSeedSource, RunUIController, RollPresentationSettings/PresentationSettings/GameConfigData, PrototypeAuthoring.CreateDefaults, RunSession/RunState/CombatRules, LocalRunStore/LocalSaveEnvelope, JsonUtility/Unity Random, NUnit/System.IO/SHA256. source를 공급하는 PlayMode 호출 계수는 SeedPresentationFlowTests가 맡는다.
- 사용하는 쪽/관계 근거: Unity EditMode Test Runner가 NUnit attribute를 통해 호출하며 제품 코드는 이 테스트를 참조하지 않는다. 기존 FateDice.EditMode.Tests asmdef의 Runtime/Editor/SharedUI 참조를 사용한다.
- 상태/수명: 매 테스트마다 고유 Temp/FateDiceSeedAndPresentationTests 하위 저장 경로를 만든다. 기본 사용자 저장의 존재/bytes는 읽기 비교만 하고, teardown은 정규화한 자기 임시 루트 경계 안에서만 정리한다. 원본 SO/Scene/Prefab/기본 저장에 쓰지 않는다.
- 검수 주의점: 실제 통합/실행 결과는 메인 REPORT를 따른다. 위임 에이전트는 Unity/Git를 실행하지 않았다. 초기 reflection RED 초안은 별도 initial-red/SeedAndPresentationTests.cs이며 최종 파일과 함께 컴파일하지 않는다. 최종 PASS/FAIL과 실제 개수는 메인이 ROGUELIKE_ARCHITECTURE_REPORT에 기록한다.
