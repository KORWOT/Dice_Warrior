# LobbyCampaignFlowTests.cs
- 역할: 실제 GameApplication/제작 Lobby·InGame/등록 MenuUI·ExplorationUI·DiceRollUI를 사용하는 LOBBY_MAP_DICE 통합 PlayMode 검사 6건이다. 첫 asset 검사1건을 컴파일 가능한 reflection으로 작성해 실제 missing DiceRollUI RED 후 구현했다. 기존 5건을 유지하고 C02 완료 경로 회귀 1건을 추가했다.
- 입력/격리: 실제 SO snapshot, seed33/88, 테스트별 Temp LocalRunStore를 Bootstrap 전에 주입한다. 상황 fixture는 clone nodeWeights만 Combat/Event/Treasure로 고정하고 실제 RunSession.New/ChooseNode/Roll/ChooseFate/ResolveEncounter/ClaimReward와 필요한 Equip/ReplaceDie를 사용한다. 소스 SO/제작씬/기본 저장을 변경하지 않는다.
- 자산 oracle: 6개 서로 다른 자식 DiceFaceView 및 각 Graphic의 CanvasRenderer, popup title/detail/result/rollButton, 준비 로비3패널·3탭·표시 필드, MenuUI layoutVersion1/ExplorationUI layoutVersion2, mapContainer와 동일 GameObject의 CampaignMapView 및4연결 참조를 요구한다.
- 준비 로비: 실제 pointer로 탭·시련·상한·새 여정·menu/continue를 누른다. 패널1개 활성/설정 rebinding 유지/재진입 캐릭터 초기화/hidden seed/no campaign map/실제 config 표시/영구 성장 추후 안내를 검사한다. 설정 변경 자체는 저장을 만들거나 기존 여정을 바꾸지 않는다.
- 맵/도착/굴림: 720×1280·720×1600에서 실제 graph ID1회/child 위쪽/실제edge+현재출발선/48px·safe·viewport·raycast를 확인한다. ChooseNode checkpoint가 도착연출 전에 확정되고 중복 클릭은 무효이며, 이동 후 popup6면 미굴림0이 나타난다. 열린 modal 아래 menu pointer/직접callback이 차단된다. 명시 CloseTopPopup 후 open-dice로 재열어도 state/RNG가 동일하다.
- 결과: 실제 popup roll pointer 직후 Cards state와 디스크가 oracle.Roll과 같고 rolling/Busy=true다. 중복 클릭/애니메이션 매 프레임은 RNG를 추가 소비하지 않는다. 최종 6값이 실제 dice와 같고 popup 종료 후 실제 FateCard offered IDs와 터치 가능을 확인한다.
- 재개/유료: ExplorationRoll/Cards·CombatRoll/Cards 네 저장 상태의 진입이 파일 bytes와 전체 상태를 유지하며 Roll은 DiceRollUI, ExplorationCards는 FateChoiceUI를 연다. 실제 roll 후 oracle와 비교한다. 실제 사건 보상으로 얻은 paid reroll은 비용1회·목표1die만 변경하고 무료 roll popup을 열지 않는다.
- C02 완료 경로: TwoResolvedNodesRemainOnTheCampaignPathAcrossSaveReopen은 실제 보물 노드 2개의 보상·장비/주사위 결정을 끝낸 Map을 만든다. resolvedEventIds가 선택한 두 ID의 순서와 같고, 이력에 버린 가지도 실제로 있는 fixture를 사용한다. 활성 노드와 완료 노드만 같은 nodeLayer에 ID당 1개 표시하며 버린 가지는 표시하지 않는다. 완료 노드의 재선택 차단, 아래→위 순서, 완료→완료 및 완료→현재 선택지의 실제 연결선을 요구한다. 저장 종료/재개 두 번에 걸쳐 ID·좌표·전체 state(RNG/cards 포함)·디스크 bytes가 보존되는지 검사한다.
- 비교/수명: playedSeconds만 정규화한 전체 RunState를 UI와 LocalRunStore 양쪽에서 비교한다. 자기 app만 Destroy하고 Current null을 확인한 뒤 자기 임시 폴더만 정리한다.
- 직접 관계: GameApplication/SceneFlowController/RunUIController/MenuUI/CampaignMapView/ExplorationUI/DiceRollUI/DiceFaceView/FateCardView/UIManager/RunSession/LocalRunStore; Editor AssetDatabase·PlayModeWindow, uGUI Raycaster/EventSystem, NUnit/UnityTestTools.
- 검수 한계: 이 코드는 Editor PlayMode 포인터 테스트이며 Android 실기기나 native Unity 직접 클릭 증거를 대체하지 않는다. 최종 6건의 실행 통과와 별도의 native 클릭 증거는 LOBBY_MAP_DICE_REPORT.md에 기록했다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

MenuUI.layoutVersion=1, ExplorationUI.layoutVersion=2와 AnimateNodeArrival(string) API를 요구한다. 노드 포인터 탭은 SelectedNodeId와 상태/저장 불변을 확인하고 별도 move 포인터에서만 기존 ChooseNode oracle를 적용한다. 탐험 Cards는 FateChoiceUI 1개, 전투 Cards는 popup 없음으로 구분하며 카드/유료 주사위는 실제 popup 참조를 조회한다. legacy 0 snapshot으로 기존 수제 경로·C02 가지 fade/완료 경로 oracle를 유지한다. 버전1 전체 이력 표시는 신규 ProceduralCampaignFlowTests 범위다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

ArrivalAndSixDiceCommitBeforeAnimationAtBothPortraitRatios는 최초 노드 탭 직후 Busy=true, 도착 완료 전 popup 없음, 이미 확정된 ChooseNode oracle와 저장 상태를 요구한다. 같은 노드의 이전 callback 재호출·플레이어 이동·공유 노드/경로 좌표·정확 굴림·재개 및 비용 검사를 유지한다. 별도 이동 버튼이나 preview 대기 상태를 요구하지 않는다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
