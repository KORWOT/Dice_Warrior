# DiceAuraTests.cs

- 역할: 확정된 HandKind의 표시 참여 index와 그룹을 독립 리터럴 예시로 확인한다. 신규 DiceComboHighlights 타입은 reflection으로 읽으므로 제품 구현 전에도 컴파일하고 명시적 missing-helper assertion으로 RED를 낼 수 있다.
- 핵심 동작: 10종 hand의 최소 참여 개수, 숫자 오름차순 후보/입력 index 우선순위, 3개 페어의 세 그룹, FullHouse triple=0/pair=1, Straight 1..5 우선 및 중복 첫 index, FullStraight 여섯 개를 14개 독립 예시로 검증한다. 반환으로 BestHand를 다시 판정하거나 기대값을 제품 helper로 계산하지 않는다.
- 입력/출력: HandKind와 int[6]을 전달하며 int[6]의 -1/0 이상 그룹을 검사한다. 미지원 hand/불성립 모양 13예시는 전부 -1을 요구한다. null/잘못된 길이/0/7은 ArgumentException으로 거절한다.
- 불변/수명: 원본 values의 값·순서를 보존하고 반환 배열은 독립 소유한다. 반환값 수정 후 반복 호출도 동일해야 하며 같은 동기 호출 구간 전후 UnityEngine.Random.state를 비교한다. 사용자 저장·Scene/Prefab/SO 및 전역 RNG에 쓰지 않는다.
- 사용하는 대상: reflection을 통한 DiceComboHighlights.Groups, 타입 검색 기준인 DiceRollUI의 Runtime assembly, HandKind, NUnit, UnityEngine.Random.State. ExceptionDispatchInfo는 reflection wrapper를 벗겨 제품 예외를 검증할 때만 사용한다.
- 사용하는 쪽/관계 근거: FateDice.PlayMode.Tests의 Unity Test Runner가 Test/TestCaseSource를 발견한다. 제품 코드에서 이 시험을 호출하지 않는다. 기대 배열은 테스트 파일의 손으로 확인한 상수다.
- 실제 UI 경계: 실제 DiceRollUI.prefab의 독립 dieAuras 6개와 crest 1개, CanvasRenderer/입력 비차단/소유 hierarchy 연결을 검사한다. 두 번째 UI 검사는 비활성 Canvas 아래 prefab 복제를 생성하고 활성화한 뒤 실제 DiceResultFeedback.Show를 호출한다. hold0 ThreePairs는 여섯 오라/그룹1 accent를 정지 표시하고, TwoPairs 재Show는 네 참여면만 남기며 짧은 등장 후 Phase1에 정착해야 한다. ResetFeedback 및 renderer.SetVisual/Clear의 공개 데이터 보존·Visiblefalse·빈 mesh를 확인한다.
- UI 수명/격리: 자기 Canvas/prefab만 생성·파괴한다. Unity 6.6 CanvasRenderer.GetMesh()가 반환한 메시를 읽기만 하며 파괴하지 않는다. GameApplication·저장소·사용자 저장·Scene 이동을 생성하지 않는다. 반복 프레임 뒤 Clear도 메시를 남기지 않아야 한다. 기존 조합 연출/주사위 회귀는 변경하지 않는다.
- 검수 주의: 현재 29개 순수 검사와 2개 실제 prefab 검사, 총31개다. 메인은 artifacts/dice-combo-aura/red.json에서 missing-helper RED 1건을 확인했으며 보조의 Unity 실행과 최신 GREEN/통합 검증은 NOT_RUN이다. 메인이 Unity를 직렬 실행하고 DICE_COMBO_AURA_REPORT에 기록한다. 사용자 요청 이미지의 시각 품질·720×1280/1600·실기기 검증을 이 검사가 대신하지 않는다. 같은 Task의 최초 통합 검토/보완 예산을 따른다.
- 통합 후 총32개: 사용자 alpha/RGB를 유지하는 palette migration fixture 1개를 추가했다. UI 검사는 모든 오라의 최대 강도/등장 중간 phase에서 실제 메시가 rect 안에 있고 Graphic opacity0을 흰 빛 중심까지 유지하는지도 확인한다. 최신 실제 결과는 artifacts/dice-combo-aura/aura-tests-final.json과 REPORT를 따른다.


## 등급별 연출 수명 (2026-09-10)

모션 정착은 .4초 고정 대기 대신 최대3초 안에 Reading 진입을 확인한다. 기여index/색/0초/phase1/Reset/alpha/정점 범위 검사는 유지한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
