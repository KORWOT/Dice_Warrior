# SymbolLoopScreen.cs

- 역할: authored 텍스트·사각 보드·전투 무대·주사위·선택 버튼을 바인딩.
- 핵심 동작: LoopScreenData의 표시용 Run 사본과 문자열을 고정 슬롯에 배치하고 Playback.Snap으로 확정 상태 표시. 타일 유형별 아이콘/현재 칸 강조, 8개 버튼 상한.
- 입출력: App의 DTO/Action/onClose → Text/Image/CommonButtonView/Playback.
- 사용하는 대상: Shared BaseUI/UIData/CommonButtonView, SymbolLoopPlayback/LoopGlyphGraphic, uGUI.
- 사용하는 쪽/관계 근거: App의 UIManager.Show, Authoring의 prefab 필드, FlowTests.
- 상태/수명: UIManager 캐시 수명. OnUnbind는 onClose로 재생 소유자에 중단을 알린 뒤 버튼을 해제. 720×1280 content를 safe area에 균일 확대/축소.
- 검수: 전역 탐색/규칙 실행 없음. 보드는 이동·사건, 전투 무대는 Combat, focus는 로비/결과에서만 표시.
