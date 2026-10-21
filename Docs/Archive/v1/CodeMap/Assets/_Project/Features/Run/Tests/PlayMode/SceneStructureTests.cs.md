# SceneStructureTests.cs

## RA-A 현재 계약 (2026-09-09)

ProductionJourneyUsesRealViewsThroughResultAndReturnsToLobby만 Seed=33을 명시하여 기존 완주·전투/장비/보상 관측 기대를 보존한다. 다른 씬/저장/중복 bootstrap 검사는 유지한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 역할: 실제 Title/Lobby/InGame assets와 GameApplication prefab를 사용하는 제작용 씬 구조 통합 검증.
- 입력: Editor AssetDatabase의 authored prefab/씬, 테스트마다 Temp 아래 고유 LocalRunStore를 Bootstrap 전에 주입. 기본 사용자 저장에 gameplay를 실행하지 않는다.
- 검사: assets/10종 registry/build첫Title, Title→Lobby→new→InGame, 반복menu/continue의 동일 UI owner, 단일 Canvas/EventSystem, 중복/실패 load lock, 다른store 재주입 거절.
- 저장: 직접 InGame의 missing/corrupt Lobby fallback 및 명시 archive, 유효 offer/RNG/dice/reroll/history/fullState(시간만 정규화) 보존, load 자체의 디스크bytes 불변, 실제 reroll/전체journey/result→Lobby/new 최근 결과 유지.
- 관계: GameApplication/SceneFlowController/RunUIController/actual typed screens/RunSession/LocalRunStore/SceneManager와 Unity Test Runner를 사용한다. 조건부 Editor API 때문에 Editor PlayMode 테스트다.
- 수명: UnityTearDown은 자신이 만든 persistent app을 제거하고 Current null 확인 후 격리 저장만 지운다. 기존 prototype fixture의 독립성을 지킨다.
- 검수 한계: 대부분 tests는 명시 Bootstrap 후 SceneEntry가 현재 앱을 재사용한다. SceneEntry cold Start는 별도 실제 Editor direct-scene Play 및 자산 참조 증거로 확인하며 실행 보고에 구분한다.

## 한글 UI 적용 (2026-09-08)
- 씬 진입/저장·실패 안내의 한국어 기대를 반영했다. 손상 저장 bootstrap 경로와 잘못된 목적지의 진단 Warning만 명시적으로 기대하고 실제 scene/state/raycast/저장 비교를 유지한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.


## 로비·주사위 팝업 어댑터 (LOBBY_MAP_DICE)
- 제작 registry는 기존 8개 화면과 DiceRollUI/FateChoiceUI popup 총 10개 concrete type을 요구한다.
- trial/cap은 공개 settingsTab을 연 뒤 기존 버튼을 호출한다. roll은 top DiceRollUI.rollButton을 사용한다. 기존 navigation fixture가 Roll에서 메뉴로 갈 때는 popup을 명시적으로 닫는다. 새 LobbyCampaignFlowTests가 열린 modal 아래 입력 차단을 별도 실제 raycast로 검사한다.
- 기존 scene singleton/디스크 bytes/카드·RNG·history/전체 여정 oracle는 유지한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

제작 registry는 8개 화면과 DiceRollUI/FateChoiceUI 두 팝업, 총10개 concrete type을 요구한다. 기존 여정·이어하기 fixture는 테스트 소유 config clone/snapshot의 mapGenerationVersion=0으로 고정한다. 노드 preview 후 move, 운명 카드 선택 후 confirm을 각각 실제 UI callback으로 호출하고 카드 선택 자체의 state 불변을 확인한다. 재굴림은 top FateChoiceUI의 실제 die 버튼을 사용하며 메뉴 fixture는 열린 modal을 명시적으로 닫는다. clone은 테스트 teardown에서 제거한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.


## PROCEDURAL_CAMPAIGN 보완 묶음 2 직접 회귀

저장 재개 paid reroll은 top FateChoiceUI의 실제 die-2 버튼을 먼저 보관한다. 기존 Press 라우팅으로 첫 입력 후 같은 버튼 callback을 다시 호출하므로, 표시가 DiceRollUI로 바뀐 뒤 옛 Widgets 키를 찾는 오류 없이 중복 명령 방어를 검사한다. EventSystem 단일 수 assertion/Warning 처리는 완화하지 않는다.

근거: artifacts/procedural-campaign/play-initial.json의 실제 실패. 이번 담당은 Unity를 실행하지 않았으며 수정 후 검증은 Main의 마지막 통합 실행 대기다. 기존 EventSystem 경고를 기대 로그로 등록하거나 전역 객체를 삭제하지 않는다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

Press(node)는 실제 노드 callback 한 번만 호출하며 추가 move를 호출하지 않는다. 기존 WaitScene/Unlocked의 Controller.Busy 대기로 첫 전투 진입이 끝난 뒤 다음 주사위 입력을 실행한다. fate tap→confirm과 재굴림의 이전 callback 중복 검사는 유지한다. GameApplication/Canvas/EventSystem 단일 수 및 예상 외 로그 검사는 그대로다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
