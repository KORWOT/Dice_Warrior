# RunSession.cs
- 책임: 런 상태 소유와 단계별 원자 명령. New는설정/seed/시험카드/상한검증→깊은스냅샷→기본상태/가지.
- API: Roll/Reroll,ChooseNode/Fate/Action,ResolveEncounter,Buy/LeaveShop,ClaimReward,Equip/ReplaceDie,SetExplorationCap은단계/현재안정ID/자원을검사해bool수락반환.
- 직접사용: GameConfigData,JsonUtility,DiceRules,FateCardRules,ExplorationRules,CombatRules,GrowthRules. 화면RestTrainingReward와실제훈련은같은TrainingReward계산.
- 직접호출자: RunUIController의 화면 요청 delegate,RunTests/SaveTests/GUI. RunUIController가Checkpoint에LocalRunStore.Save를주입한다.
- 흐름: Apply는전체JSON복제→sequence+1→규칙→Result기록→Checkpoint(next)→성공시State교체. 저장실패는이전상태/난수/카드/자원을유지한다.
- 굴림: RefreshOffers에서최고패/운명력/독립카드확정. Reroll은획득권한+설정비용+카드단계+0..5선택확인,한면만추첨하고새제시카드와비용을동일저장. 재진입은굴림을호출하지않는다.
- 사건/보상: processedRewardIds와resolvedEventIds별도. 장비/주사위결정을끝낸뒤일반사건1회+1. 상점구매Reward→Shop은진척없고Leave시종료. Boss는추가일반진척없이Result. 상한은탐험Gold/XP만보정.
- 최근결과: Result를RunRecord로고정,새런승계는RunUIController담당. Guid는식별자만이고확률에사용하지않는다.
- 증거: Edit98/98,GUI8/8,실제SO3범주변경·기존스냅샷불변PASS. 파일저장은현재실제연결됨.


- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.

## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- 로비의 기본 모험가·행동·여섯 주사위 안내는 RunUIController가 config snapshot으로 구성한다. 기존 trial/cap 선택으로 New를 호출하며 이번 화면 개편이 새 캐릭터·영구 성장·재화 규칙을 추가하지 않는다.
- 노드 callback은 RunUIController.TravelToNode → ArriveAtNode → ChooseNode로 연결된다. 기존 Apply/checkpoint가 성공한 뒤에만 Widgets.AnimateNodeArrival을 기다리고 현재 단계와 DiceRollUI를 표시한다. 맵의 위치 이동은 런 명령이 아니다.
- DiceRollUI의 사용자 roll callback만 컨트롤러를 통해 Roll을 요청한다. 실제 주사위·패·카드·RNG를 기존 Apply에서 먼저 확정/저장하고 popup은 복사된 결과를 연출한다. 연출 종료·화면 재Bind·저장 재개가 Roll을 자동 호출하지 않으며 유료 Reroll의 단계·비용 계약은 그대로다.
- 완료 경로의 근거는 FinishEvent가 기록하는 resolvedEventIds와 ExplorationRules가 보관한 nodeHistory다. RunUIController가 두 기록의 ID 일치를 확인한 NodeState 복사본만 ExplorationUIData.completedNodes로 공급한다. 맵이 RunSession/RunState/저장소를 조회하거나 완료 여부를 추론하지 않는다.
- 문서 범위: 실제 소스와 이번 작업의 staged DTO/컨트롤러 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.


## 전투 피드백 직접 관계

- ChooseAction/Roll의 확정과 checkpoint는 기존처럼 즉시 1회다. RunUIController가 성공 전후 스냅샷의 표시만 지연하며 Session의 계산·저장 계약은 변경하지 않는다.
- 실행 증거: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
