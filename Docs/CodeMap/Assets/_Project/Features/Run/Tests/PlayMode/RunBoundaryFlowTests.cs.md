# RunBoundaryFlowTests.cs

- 역할: RA-B의 실제 UI 명령·저장 후 표시 장애·이전 화면 callback·재시도를 검사하는 PlayMode UnityTest 5개다. 실제 GameApplication.prefab과 InGame.unity를 사용하며 실행 결과는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
- 핵심 동작: 실제 GraphicRaycaster/포인터 down/up/click으로 행동 카드, cap-cycle, 주사위 굴림 버튼을 누른다. DiceRollUI.rollButton은 CommonButtonView이므로 실제 uGUI Button인 .rollButton.button을 포인터 검사에 전달한다. 버튼이 활성·상호작용 가능하고 실제 raycast의 첫 대상인지 검사하며 모든 비동기 대기는 10초 상한이다.
- 표시 실패: action command의 LocalRunStore.Save 성공 직후 Controller에만 할당한 비영속 VisualCatalog 복제의 defaultEvent를 null로 만들어 다음 Render를 실패시킨다. 원본 SO를 편집하지 않는다. 정확히 1회 warning, 잠금/전투 feedback·arena transform 복원, 독립 command oracle와 같은 확정 상태/디스크를 확인한다. 복제 필드를 복구한 뒤 RefreshView 두 번이 추가 저장·명령·RNG·비용 없이 CombatUI/roll popup을 다시 표시해야 한다.
- 오래된 입력: 실제 맵 cap 버튼으로 Common→Uncommon을 확정한 다음 이전 표시 DTO의 cap delegate를 지연 dispatch로 호출한다. 이미 Busy=false이고 Map 단계가 일치해도 이전 token은 거절되어 전체 규칙 상태/파일/저장 횟수가 같아야 한다. 표시 DTO의 노드 자식과 시간 설정 변경도 런에 쓰지 못한다. protected Data의 reflection은 오래된 UI delegate를 확보하는 테스트 관찰에만 사용한다.
- 저장 실패: 탐험 굴림의 격리 저장 장애 시 기존 전체 규칙/카드/주사위/RNG/파일이 유지되고 실제 roll 버튼으로 재시도할 수 있어야 한다. 복구 후 저장 1회와 독립 oracle 결과가 일치하고 운명 카드가 다시 raycast 가능해야 한다.
- 입출력/직접 관계: GameApplication/RunUIController/RunSession/IRunStore/LocalRunStore, FixedSeedSource, CombatUI/ExplorationUI/DiceRollUI/UIManager, FateDiceVisualCatalog, BaseUI/ExplorationUIData, uGUI/EventSystem/Unity Test Framework를 사용한다. ProbeStore는 실제 LocalRunStore를 감싸 저장 장애와 저장 후 표현 장애만 주입한다.
- 상태/수명: 매 테스트 고유 Temp/FateDiceBoundaryFlowTests에만 저장한다. 기본 사용자 저장 존재/bytes를 전후 비교하고 자기 앱/비영속 visual clone만 파괴한다. 정규화한 자기 임시 루트 아래만 삭제한다. 컴퓨터 조작/Android 물리 터치 검증을 의미하지 않는다.
- 검수 주의점: 표시 프레임 시간은 실제 Controller.Update로 증가하므로 비교 시 playedSeconds와 lastResult.playedSeconds만 정규화한다. 원본 run/node/offered ID, 비용, 피해, 진행, RNG는 정규화하지 않는다. 디스크 bytes와 저장 횟수는 별도로 고정 비교한다. 1000 enemy HP는 연출 실패 뒤 CombatRoll을 보장하는 격리 fixture이며 기본 밸런스 변경이 아니다.
- RA-B-02 수정2: 실제 CombatUI.PlayFeedback에 anchor 변환으로 손실되는 고정 분수 localPosition을 주어 종료 뒤 local/anchored의 완전동등을 추가 검증한다. 0/1 FAIL RED 확인 후 제품 복원을 수정하며 기존 실제 pointer·저장 횟수·상태·bytes·원위치 assertion은 변경하지 않는다.

- B-02 추가 수정3: 정상 종료는 RectTransform 초기 layout을 완료하고 local(.125,59.125,0)을 독립 exact assertion으로 확인한 뒤 실제 coroutine을 실행한다. 초기 미확정 좌표를 종료 oracle로 쓰지 않는다. 별도 InterruptedFeedback 회귀는 기존 tiny fractional 값을 유지하며 같은 프레임에 실제 IEnumerator/Impact를 한 단계 진행한 뒤 ResetFeedback의 local/anchored 완전동등 복원을 검사한다. 진단으로 무연출 ForceUpdate만으로도 초기 좌표가 바뀜을 확인했으며 테스트 허용오차는 추가하지 않았다.

- 중단 회귀는 anchored getter의 관측 부작용을 피하려고 fixture의 정확 anchored 기대값(360,-441)을 독립 상수로 둔다. 제품 PlayFeedback이 local을 캡처하기 전에 시험 코드가 anchored를 읽지 않는다. 실제 Impact로 변위가 생긴 뒤 ResetFeedback의 exact local/anchored 복원을 확인한다.
