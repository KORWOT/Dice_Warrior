# CombatLayoutTests.cs

## RA-B 상태 경계 fixture 전환 (2026-09-09)

세션의 State는 독립 표시 복사다. 준비 상태를 지역 RunState DTO에 구성한 뒤 새 RunSession 또는 격리 저장소에 전달한다. 규칙 기대값과 기존 버튼/저장/재연 assertion은 유지한다. 잘못된 저장 검사는 동일한 수정 DTO를 LocalRunStore.Save에 전달한다. 실제 실행 증거는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.

- 역할: 실제 GameApplication.prefab/InGame.unity/CombatUI를 사용하는 전투 통합 PlayMode5개.
- 입력: 실제 config Snapshot/RunSession Seed33 경로로 공개 전투를 만들고 고유 Temp LocalRunStore를 Bootstrap 전에 주입한다. fractional HP/shield/earned paid reroll/offeredCards=5는 저장 검증을 통과하는 격리 fixture다. 기본 사용자 저장/SO를 바꾸지 않는다.
- 검사: authored header/arena/가로 Grid/ScrollRect/장식 renderer·raycast;720x1280/720x1600 실제 HPfill/예고/누적턴, arena 공간, 주사위6/카드3 동시표시·48px·안전영역·실제 raycast roll/menu.
- oracle: 독립 RunSession.Roll/Reroll/ChooseAction의 전체 상태와 UI 클릭/디스크 저장을 비교한다(playedSeconds 제외). 재굴림 비용/비대상 주사위/원본 ID/제시 ID/효과를 검사한다.
- 5장: 실제 생성 목록과 View 전체 ID가 일치하고 가로 위치를 이동해 각 카드를 raycast한다. 마지막 offered ID 선택도 oracle와 비교한다. 실제 drag 이벤트 경로는 Main 별도 probe 증거다.
- 상태/수명: 소유 GameApplication/고유 임시 fixture를 teardown한다. 해상도 height는 Unity uint API에 맞춘다. Editor 직렬 검증이며 Android 물리 터치 증거가 아니다.
- 직접 관계: GameApplication/SceneFlowController/RunUIController/CombatUI/RunSession/CombatRules/GrowthRules/LocalRunStore/ActionCardView, uGUI/EventSystem/NUnit/UnityTestTools. COMBAT_LAYOUT_REPORT 참조.

## 한글 UI 적용 (2026-09-08)
- 적 예고/결과/체력·위력·방어력/재굴림 문구 기대를 한국어로 갱신했다. 두 화면 비율·카드3/5·드래그/재굴림/저장·수치 oracle는 그대로다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.


## 주사위 팝업 조회 (LOBBY_MAP_DICE)
- Button("roll")만 top DiceRollUI의 실제 rollButton을 반환한다. 기존 실제 Raycaster·pointer 및 전체 상태/수치/카드·paid reroll/두 비율 oracle는 유지한다. menu와 행동/개별 주사위는 기존 Widgets key를 사용한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
