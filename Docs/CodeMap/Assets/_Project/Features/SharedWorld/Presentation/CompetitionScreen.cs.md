# CompetitionScreen.cs

- 역할: authored 12개 장소/4개 말/참가자 행/사건 선택지/AI 요약을 표시한다.
- GROWTH_04: growthMode가 있으면 보드/전투/주사위/연출 cue를 숨기고 작성된 Growth details 패널을 표시한다. adventurers는 카드3개, stats는 RGB3행+출처, skills/legacy는 긴 텍스트 영역을 바인딩한다. 필수 패널 누락은 조용히 우회하지 않고 오류다. 다음 Bind에서 원래 화면을 복구한다.
- TAG_STAT_ELEMENT_05: attributes 모드는 별도 authored attributeRows/attributeValues[6]/attributeSources[6]/attributeBody를 사용한다. v3 RGB3행은 그대로 유지하고 다른 상세 모드로 전환할 때6행을 숨긴다. element-text는 페이지가 분리된 기존 긴 텍스트 영역이다.6행 필수 연결 누락은 명시 오류이며 런타임 자산 생성으로 우회하지 않는다.
- 핵심 동작: CompetitionScreenData의 확정 state와 문자열/콜백을 바인딩한다. 720×1280 프레임을 safe area 크기에 균일 배율로 맞춘다. 참가자 색과 번호를 함께 쓰고 같은 칸의 말은 고정 2×2 위치로 분리한다. 개인 전투 중 같은 영역의 전투 무대를 표시한다.
- 입출력: title/subtitle/status/detail/history, CompetitionState, 최대8개 labels/actions/enabled, onClose → Text/RectTransform/CommonButtonView/Playback.
- 추가 입출력: growthTitle/summary/body, statValues/statSources/adventurerCards 배열 → growthPanel/statRows/adventurerCards와 Text 참조. RGB 색·기호는 Authoring이 작성한다.
- 사용하는 대상: BaseUI/UIData/CommonButtonView, CompetitionPlayback, Unity uGUI, SymbolLoop 그래픽.
- 사용하는 쪽·관계 근거: CompetitionApp의 UIManager.Show, CompetitionAuthoring의 프리팹 필드, CompetitionFlowTests의 실제 포인터/화면 검증.
- 상태/수명: UIManager의 타입별 캐시 수명을 사용한다. OnUnbind는 재생 소유자에 중단을 알리고 버튼을 해제한다. 표시 중 규칙 실행·난수 소비·저장 쓰기는 없다.
- 검수 주의: places/cells/tileFrames는12개, pawns/roster는4개, 버튼은8개다. 참가자 위치/HP/완주/명예/순위는 같은 저장 사본에서 표시한다. 사건 가용성에 따라 버튼을 비활성화한다.
