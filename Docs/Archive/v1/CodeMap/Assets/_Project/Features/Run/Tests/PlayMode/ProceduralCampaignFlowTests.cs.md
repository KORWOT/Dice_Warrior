# ProceduralCampaignFlowTests.cs

- 역할: 새 절차 지도와 전용 운명 팝업을 실제 제작 프리팹·SceneFlow·포인터 입력·파일 저장으로 통합 검사한다.
- 핵심 동작: 공개 DTO의 먼 유형 차단, 3층 진행 후 디스크 왕복 위치/공개 범위, 두 세로 비율에서 노드 탭 즉시 이동→굴림→카드 선택/확정→사건→보상→지도 좌표 보존을 확인한다.
- 입력/출력: 앱 프리팹의 설정을 복제해 버전1·5열/5경로·10일반층·휴식 유형을 고정한다. 고정 seed33과 별도 CountingStore/LocalRunStore를 쓰고 NUnit 결과를 출력한다. 기존 사용자 저장은 바이트 비교만 한다.
- 수명: GameApplication을 테스트마다 만들고 파괴하며, 승인된 제작 InGame 씬을 읽는다. 임시 저장 폴더는 테스트 전용 경로 안에 있을 때만 정리한다. Time.timeScale을 복구한다.
- 관계: RunUIController/RunSession/UIManager, CampaignMapProjection, ExplorationUI, DiceRollUI, FateChoiceUI, PresentationPlayback, GraphicRaycaster를 실제 연결로 소비한다. 규칙 Session을 별도 oracle로 실행하고 저장 횟수/정규화된 상태를 비교한다.
- 검수: 카드 탭은 저장하지 않고 confirm 중복은 저장1회, 닫기/재열기/유료 재굴림, 저장 실패 재시도, 실제10초 초과 생각 시간, 실제10초 watchdog/로그, cancel/disable/재바인딩 소유권을 검사한다. 렌더 캡처와 실기기 테스트를 대체하지 않는다. 실행 결과와 첫 실패/보완 근거는 PROCEDURAL_CAMPAIGN_REPORT와 artifacts/procedural-campaign에 기록한다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

원본 계약 이름은 AuthoredMapSupportsImmediateTravelAndPublicFloorSnapshots이며 이동 버튼이 없거나 비활성이어야 한다. 두 비율의 실제 노드 포인터 한 번에서 saves+1/Busy/도착 전 popup 없음/독립 ChooseNode 상태·디스크 oracle를 확인한다. 이전 노드 callback을 직접 재호출해 저장이 추가되지 않는지 검사하고 전체 노드 좌표 고정도 유지한다. FateChoiceUI 배경 차단은 사라진 move 버튼 대신 실제 지도 메뉴를 대상으로 한다. fate 선택·확정·보상·RNG·소유권·watchdog 검사는 그대로다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
