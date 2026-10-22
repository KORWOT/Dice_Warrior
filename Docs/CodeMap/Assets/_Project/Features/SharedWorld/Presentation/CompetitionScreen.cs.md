# CompetitionScreen.cs

- 역할: authored 12개 장소/4개 말/참가자 행/사건 선택지/AI 요약을 표시한다.
- 핵심 동작: CompetitionScreenData의 확정 state와 문자열/콜백을 바인딩한다. 720×1280 프레임을 safe area 크기에 균일 배율로 맞춘다. 참가자 색과 번호를 함께 쓰고 같은 칸의 말은 고정 2×2 위치로 분리한다. 개인 전투 중 같은 영역의 전투 무대를 표시한다.
- 입출력: title/subtitle/status/detail/history, CompetitionState, 최대8개 labels/actions/enabled, onClose → Text/RectTransform/CommonButtonView/Playback.
- 사용하는 대상: BaseUI/UIData/CommonButtonView, CompetitionPlayback, Unity uGUI, SymbolLoop 그래픽.
- 사용하는 쪽·관계 근거: CompetitionApp의 UIManager.Show, CompetitionAuthoring의 프리팹 필드, CompetitionFlowTests의 실제 포인터/화면 검증.
- 상태/수명: UIManager의 타입별 캐시 수명을 사용한다. OnUnbind는 재생 소유자에 중단을 알리고 버튼을 해제한다. 표시 중 규칙 실행·난수 소비·저장 쓰기는 없다.
- 검수 주의: places/cells/tileFrames는12개, pawns/roster는4개, 버튼은8개다. 참가자 위치/HP/완주/명예/순위는 같은 저장 사본에서 표시한다. 사건 가용성에 따라 버튼을 비활성화한다.
