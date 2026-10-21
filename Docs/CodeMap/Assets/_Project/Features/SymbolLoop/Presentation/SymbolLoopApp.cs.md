# SymbolLoopApp.cs

- 역할: Title 게임 조립, 로비/캠페인/결과/저장 복구와 연출 수명 관리.
- 핵심 동작: 명령 pending 저장→Execute→pending 제거 성공 이후 확정 전후 사본으로 이동/전투 재생. 같은 revision의 영수증 재시도는 연출하지 않는다. 완료 후 최종 Render.
- 입출력: authored UIManager/Config, Open(path,seedSource) → Session과 LoopScreenData. IsPresenting은 명령/재생 중 여부.
- 사용하는 대상: LoopSession/LoopStore/Config, UIManager, Screen/Playback. 실제 경로는 persistentDataPath/symbol-loop-v1/player.json.
- 사용하는 쪽/관계 근거: Title 직렬화 컴포넌트, Authoring, 격리 FlowTests. Find/singleton 없음.
- 상태/수명: busy와 UIManager 입력 잠금으로 중복 명령 차단. App 코루틴이 nested iterator를 펼쳐 예외에도 잠금 해제. Render, 화면 닫기, OnDisable, OnApplicationPause(true)는 재생 취소/최종 상태 복귀. 파괴 시 UI 닫음.
- 검수: 저장 실패는 미결 입력 UI, 시각 실패는 저장 상태 표시. 취소/재개가 RNG·보상·턴을 재실행하면 안 됨. 새 로드의 로비에서 이어하기는 결과를 바로 표시. Android Portrait 요청, 화면/safe area 변경 적용.
