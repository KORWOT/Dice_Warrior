# SeedPresentationFlowTests.cs

## RA-B 상태 경계 fixture 전환 (2026-09-09)

세션의 State는 독립 표시 복사다. 준비 상태를 지역 RunState DTO에 구성한 뒤 새 RunSession 또는 격리 저장소에 전달한다. 규칙 기대값과 기존 버튼/저장/재연 assertion은 유지한다. 잘못된 저장 검사는 동일한 수정 DTO를 LocalRunStore.Save에 전달한다. 실제 실행 증거는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


- 역할: RA-A의 실제 새 여정/이어하기 입력, 주사위 표시 시간, 저장 확정·RNG 경계를 검증하는 PlayMode UnityTest 18개다. 실행 여부와 결과는 메인의 실제 Unity 증거를 따른다.
- 입력/출력: 실제 GameApplication.prefab 및 Lobby/InGame.unity를 사용한다. Bootstrap의 마지막 ISeedSource 인자에 명령 소비 계수/정해진 결과를 제공하는 CountingSource를 주입하며, LocalRunStore는 초기화 전 고유 임시 경로를 주입한다. source는 게임 규칙 대신 시드 경계만 대체한다.
- 시드: getter/반복 EnterLobby/렌더는 호출0, 두 새 여정은 각각 호출1이며 211을 반복 반환해도 허용한다. 별개 runId를 유지하며 실제 규칙 RNG·노드유형·첫 굴림의 여섯 눈/패/운명력/후보유형·등급·원본ID 재현을 확인한다. Seed setter의 고정 공급자 선택/0거절 이후 재시작도 원래 source를 소비하지 않는다.
- 이어하기/거절: 탐험/전투 Cards 저장에서 source 호출0, initialSeed/RNG/전체 상태/후보 ID/디스크 bytes 보존과 주사위 popup 미생성을 확인한다. source=0의 첫 시작은 Session/파일을 만들지 않으며 기존 런이 있을 때는 같은 Session/전체 state/파일 bytes/로비를 유지한다. 의도된 seed Warning만 LogAssert.Expect한다.
- 표시 시간: 저장된 탐험 .2/.8, 전투 .8/.2초와 legacy rollSeconds=1.6 fixture로 각 phase의 굴림/결과 유지시간을 각각 관측한다. 탐험/전투의 일반 굴림 및 실제 die-2 재굴림을 실행하며 Checkpoint가 표시 이전에 정확히1회 저장해야 한다. 고정된 여섯 면과 전체 조합 문구가 hold 동안 표시되고 Busy가 유지된 후 카드가 raycast 가능해야 한다.
- 0시간: 탐험/전투 및 재굴림에서 0/0은 최대 두 표시 프레임 안에 Busy/주사위 모달을 종료하고 과거 .65 최소시간보다 빠르게 완료해야 한다. 굴림만0/hold=.3은 즉시 확정 면을 보여주면서 유지하며, 굴림=.3/hold만0은 굴림 후 추가 유지가 없어야 한다. .3초 단독 단계의 실제 완료 시점은 .28 이상/.55 미만이다.
- 저장/RNG: 실제 UI 클릭 전 RunSession 복제에서 동일 Roll 또는 Reroll(2)를 실행해 oracle을 만든다. UI 명령 직후·결과 유지 중·종료 후 전체 state와 LocalRunStore.Load를 비교하고 playedSeconds 및 lastResult.playedSeconds만 정규화한다. timing/ID/후보/RNG는 정규화하지 않는다. 확정 저장 bytes가 연출 중/후 변하지 않아야 한다. 재굴림은 단일 주사위, 비용 및 제시 ID 교체도 확인한다.
- 사용하는 대상: GameApplication/SceneFlowController/RunUIController, ISeedSource, LocalRunStore/RunSession/RunState, RollPresentationSettings, DiceRollUI/DiceFaceView/KoreanText, MenuUI/RunPhase, UIManager/Widgets, 실제 uGUI GraphicRaycaster/EventSystem pointer 입력, UnityEditor AssetDatabase/PlayModeWindow, NUnit/UnityTestTools.
- 사용하는 쪽/관계 근거: Unity PlayMode Test Runner가 UnityTest/UnitySetUp/UnityTearDown으로 호출한다. 기존 FateDice.PlayMode.Tests asmdef의 Runtime/SharedUI/uGUI 참조를 사용하며 제품 코드가 테스트를 호출하지 않는다. 기본한구간완주 회귀는 기존 FateDiceGuiTests/SceneStructureTests와 EditMode 재연 테스트를 메인이 함께 실행한다.
- 상태/수명: 자기 앱만 Destroy하며 Current 정리를 확인한다. 기본 사용자 저장은 존재/bytes 비교만 하고 고유 Temp/FateDiceSeedPresentationFlowTests 아래 파일만 만든다. teardown은 정규화한 자기 루트 경계 안에서만 정리한다. 원본 SO/Scene/Prefab/GUID를 수정하지 않는다. 모든 조건 대기는 최대10초다.
- 검수 주의점: 위임 에이전트는 staging 작성과 기존/통합 API 정적 확인만 수행했고 Unity/Git 실행은 하지 않았다. 원본18개는 실패를 숨기거나 Assert를 완화하지 않으며 타이밍은 실제 Editor 프레임 스케줄에 따른 허용 오차를 명시한다. Android 기기/사람 조작 증거를 대신하지 않는다. 최종 PASS/FAIL은 ROGUELIKE_ARCHITECTURE_REPORT의 실제 실행 결과로 확정한다.


## 등급별 연출 수명 (2026-09-10)

phase별 roll/시드/저장 oracle/0초 계약은 유지한다. 이전 hold 정확 총길이만 tier.Duration(hold) 상·하한으로 갱신했다. 다른 phase의 시간 혼입 및 숨은 최솟값을 계속 검증한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

탐험 Cards 진입/이어하기/굴림·재굴림 종료는 FateChoiceUI 1개를 요구하고 CombatCards는 popup 없음으로 구분한다. DiceRollUI 종료, 0초, tier.Duration(hold), Busy/Checkpoint1회/seed호출/전체 state·RNG·디스크 bytes oracle는 유지한다. 카드와 die-2 입력은 top FateChoiceUI의 실제 authored 참조를 통해 raycast한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.
