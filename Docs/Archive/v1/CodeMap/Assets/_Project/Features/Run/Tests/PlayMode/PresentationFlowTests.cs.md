# PresentationFlowTests.cs

- 역할: 등급별 메시·타임라인 및 실제 UI 입력과 컨트롤러 watchdog의 PlayMode 통합 검사 9개.
- 입력: 실제 GameApplication/DiceRollUI 원본, 고유 Temp/FateDiceLifecycleTests 저장소, 고정 시드33, 10초를 넘도록 늘린 표시 전용 roll/impact 기간.
- 핵심 동작: 네 tier의 시간·구조·alpha/Rect 경계, 실제 FEEL이 등장 시간보다 길 때 조기 중단 없음, 읽기/퇴장/완료, paused10초 watchdog 두 경로, 취소/재바인딩 소유권을 검증한다.
- 출력/기대값: 시작·끝·저장1회, 실제 10초 전 busy 유지/후 TimedOut, context/경과/limit 오류 로그, 독립 RunSession 단일 명령 oracle·저장 bytes 불변.
- 관계: NUnit/UnityTestRunner가 실제 uGUI GraphicRaycaster/포인터 경로로 명령한다. 공급사 API는 reflection으로 읽어 테스트 어셈블리 의존성을 늘리지 않는다.
- 상태/수명: 테스트 앱/임시 Canvas/catalog clone 제거, timeScale·사용자 저장 존재/bytes 보존 검사. 정규화한 자기 Temp 루트 안에서만 파일을 삭제한다. 원본/기본 저장 쓰기 없음.
- 검수: 실기기 성능이나 수동 Computer Use 결과로 주장하지 않는다. 실행 결과는 PRESENTATION_LIFECYCLE_REPORT.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

탐험 굴림 watchdog/공개 cancel 종료는 소유 DiceRollUI를 닫고 확정된 ExplorationCards의 FateChoiceUI를 표시한다. 실제 offered ID 집합이 popup.Cards와 같은지 확인한다. controller disable 직후에는 popup 없음 기대를 유지한다. 시작/종료/저장 횟수, 실제10초 watchdog, stale binding 및 디스크 bytes oracle는 유지한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.
