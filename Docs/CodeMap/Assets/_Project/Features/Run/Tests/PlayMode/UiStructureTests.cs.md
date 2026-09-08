# UiStructureTests.cs
- 역할: 실제 UIRoot와 7개 전체 화면 prefab의 존재/스크립트/고정 ScrollRect/Text/InputField 구조, 실제 prefab 편집 전파와 캐시 복귀, 실제 EventSystem modal 입력 차단을 검증한다.
- 오라클: Editor API로 Menu 원본 padding/heading를 임시 수정하고 Scene 재로드 후 실제 인스턴스에서 값 확인. 메뉴→탐험→메뉴는 동일 화면 instance이며 Close가 시간 외 저장 상태를 바꾸지 않는다. finally로 원본 bytes를 정확히 복구한다.
- 입력: 전체 EventSystem.RaycastAll의 맨 위 대상에 pointer down/up/click 실행. popup과 global lock 동안 뒤 NEW 입력이 Session/저장을 만들지 않고, 닫은 뒤에도 lock이 유지되며 해제 후에만 실제 Map 시작을 확인한다.
- 관계/수명: 실제 FateDicePrototype Scene와 prefab, FateDiceScreen/Controller/Manager/7화면, 격리 LocalRunStore. 프로브 자산은 Editor API에서만 수정하고 finally 복원, 임시 테스트 디렉터리는 정리한다.
- 검수: 0건 실행은 PASS가 아니다. UI_STRUCTURE_REPORT에 실제 case 수/결과·출력경로를 기록한다. 기존 GUI14와 View7의 플레이 의미를 대체하지 않고 추가 검증한다.
## 로비 구조 어댑터 (LOBBY_MAP_DICE, 최종 수정 묶음 2)
- EditedWholeScreenLayoutAppearsInActualSceneAndCachedReentry는 이동한 TRIAL WILDCARD의 고정 Transform 경로를 사용하지 않는다. 원본 편집/복원 모두 MenuUI.trialHeading 직접 참조를 사용하고, 루트 padding은 MenuUI.layout의 VerticalLayoutGroup으로 접근한다.
- 실제 화면은 UIManager.ActiveScreen의 MenuUI와 layout/trialHeading을 읽는다. settingsTab을 열어 heading이 활성 상태인지 확인한 뒤 원본의 문자열·Italic 편집이 표시되는지 검사한다. 캐시 재진입 후에도 동일 인스턴스·padding·활성 heading 문자열/스타일을 확인한다.
- 기존 실제 scene 로드/격리 저장, 메뉴→탐험→메뉴, 단일 Canvas/EventSystem, playedSeconds만 정규화한 저장 state 비교, finally의 Editor API 원본 복구 및 원본 prefab bytes 일치 검사를 모두 유지한다. 자산7종/modal/global lock 테스트의 검증 내용은 바꾸지 않는다.
- 최종 수정 묶음 2 실행 결과 대기. 이 문서 갱신은 실행 통과 주장이 아니다.
