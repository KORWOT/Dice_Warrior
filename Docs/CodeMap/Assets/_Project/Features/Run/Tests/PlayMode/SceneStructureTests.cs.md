# SceneStructureTests.cs
- 역할: 실제 Title/Lobby/InGame assets와 GameApplication prefab를 사용하는 제작용 씬 구조 통합 검증.
- 입력: Editor AssetDatabase의 authored prefab/씬, 테스트마다 Temp 아래 고유 LocalRunStore를 Bootstrap 전에 주입. 기본 사용자 저장에 gameplay를 실행하지 않는다.
- 검사: assets/9종 registry/build첫Title, Title→Lobby→new→InGame, 반복menu/continue의 동일 UI owner, 단일 Canvas/EventSystem, 중복/실패 load lock, 다른store 재주입 거절.
- 저장: 직접 InGame의 missing/corrupt Lobby fallback 및 명시 archive, 유효 offer/RNG/dice/reroll/history/fullState(시간만 정규화) 보존, load 자체의 디스크bytes 불변, 실제 reroll/전체journey/result→Lobby/new 최근 결과 유지.
- 관계: GameApplication/SceneFlowController/RunUIController/actual typed screens/RunSession/LocalRunStore/SceneManager와 Unity Test Runner를 사용한다. 조건부 Editor API 때문에 Editor PlayMode 테스트다.
- 수명: UnityTearDown은 자신이 만든 persistent app을 제거하고 Current null 확인 후 격리 저장만 지운다. 기존 prototype fixture의 독립성을 지킨다.
- 검수 한계: 대부분 tests는 명시 Bootstrap 후 SceneEntry가 현재 앱을 재사용한다. SceneEntry cold Start는 별도 실제 Editor direct-scene Play 및 자산 참조 증거로 확인하며 실행 보고에 구분한다.

## 한글 UI 적용 (2026-09-08)
- 씬 진입/저장·실패 안내의 한국어 기대를 반영했다. 손상 저장 bootstrap 경로와 잘못된 목적지의 진단 Warning만 명시적으로 기대하고 실제 scene/state/raycast/저장 비교를 유지한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.


## 로비·주사위 팝업 어댑터 (LOBBY_MAP_DICE)
- 제작 registry는 기존 8개 화면과 6dice DiceRollUI popup 총 9개 concrete type을 요구한다.
- trial/cap은 공개 settingsTab을 연 뒤 기존 버튼을 호출한다. roll은 top DiceRollUI.rollButton을 사용한다. 기존 navigation fixture가 Roll에서 메뉴로 갈 때는 popup을 명시적으로 닫는다. 새 LobbyCampaignFlowTests가 열린 modal 아래 입력 차단을 별도 실제 raycast로 검사한다.
- 기존 scene singleton/디스크 bytes/카드·RNG·history/전체 여정 oracle는 유지한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
