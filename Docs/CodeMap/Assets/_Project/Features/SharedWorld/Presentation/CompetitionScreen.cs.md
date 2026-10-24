# CompetitionScreen.cs

- V5_UI_REDESIGN_12: v5 보드에서 기존 12장소 순서를 유지한 620×620 정사각형(4×4 외곽), 참가자 4개 가로 카드, 아래 D6 트레이 660×224를 적용한다. 카드에는 나/AI·참가자 ID·순위·HP·완주/명예를 구분하고 인간 말에는 색 이외의 `나` 표식을 둔다. 장소명은 실제 정의로 표시한다.
- 구형 보존: 최초 OnBind에서 기존 board/roster/tray/cue의 RectTransform·Text/이미지 색·문자 크기를 인스턴스별로 1회 캡처한다. 매 Bind에 이 기준을 복구한 뒤 v5 보드만 별도 좌표를 적용한다. authoring은 이 기존 노드의 시작 좌표를 바꾸지 않는다. 반복 바인딩이 변경된 좌표를 새 기준으로 덮어쓰지 않는다. SymbolLoopPlayback의 최초 numericHome 캐시는 Snap 뒤 현재 모드의 실제 숫자 주사위 Rect를 재적용해 분리한다.
- `PawnPosition`은 v5일 때 실제 타일 크기의 하단 네 고정 위치를 계산하고 구형은 기존 2×2 식을 유지한다. 같은 칸 네 말도 겹치지 않는다. 기존 cells/pawns/board/8buttons 직렬화 참조를 바꾸거나 런타임 노드를 생성하지 않는다.
- `CompetitionScreenData.reducedMotion`은 App의 로컬 보기 설정을 Playback와 v5Area의 ReducedMotion에 전달한다. 상태·계정 저장이나 RNG를 건드리지 않는다. v5 board의 재생 안내는 Shell 고정 notice에 표시하며 legacy cue 노드는 보존·숨김 처리한다.
- 새 authored 입력 `v5RosterCards[4]`는 V5RedesignAuthoring이 추가한다. 새 배경·아이보리/보조 글자·트레이 색은 패키지 팔레트를 적용하고, v1~v4 복귀 시 캡처한 원본 색/배치를 복원한다. 720×1280 safe area 균일 스케일 계약과 전투/상세 형제 뷰 구조는 유지한다.

- V5_UI_IMPROVEMENT_11: CompetitionScreenData.shell와 authored v5Shell을 추가했다. shell이 있으면 기존 title/status/detail/history·8버튼·성장 패널을 숨기고 새 카드/도구/탭 바인더를 사용한다. 기존 content,8buttons,보드/말/성장 참조는 보존하며 shell이 없는 v1~v4 경로는 기존대로 동작한다.
- 새 v5 비전투 중앙영역: showBoard이면 기존 참가자·보드·주사위·cue를 유지하고 나/AI·참가자 번호·순위·완주목표를 구분한다. 참가자 텍스트는14px로 표시하고 구형에서는16px로 복구한다. 현재 위치는 보드 강조와 App 요약에 나타난다. 비보드에서는 Playback.Snap 이후 combatStage/tray/cue/board를 숨겨 로비·선택·사건·보상의 빈 보드 반복을 없앤다.
- Shell은 기존 content 아래, v5Area/Net은 기존 형제 구조다. 전투는 content를 숨기며 Net의 불투명 배경/차폐는 그대로다. OnUnbind는 shell 콜백도 해제한다. 실제 프리팹 참조는 V5ShellAuthoring을 통한 main의 허용 자산 작업으로 작성한다.

- V5_DICE_COMBAT_09: CompetitionScreenData의 v5Combat/v5Command/v5DiceInfo/v5CombatInfo/v5Notice 및 v5NetData를 추가했다. authored v5Area/v5Net 필드가 필요한 상태에서 없으면 명시 오류다.
- V5 표시 수명: v5 전투에만 기존 content 전체를 숨기고 독립 Area를 바인딩한다. 상세/로비/구형 경기는 기존 content를 다시 표시한다. Net은 형제 중 마지막 authored 모달이며 배경 raycast로 뒤 입력을 차단한다. 원래 보드/버튼 8개/성장 필드는 보존한다.
- 바인딩 순서: 기존 Playback.Snap 후 Area.Bind로 실패 notice와 콜백을 최종 적용한다. Unbind는 기존 버튼과 새 두 뷰의 콜백을 해제한다. 런타임 UI 생성은 없다.

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

- V5_UI_IMPROVEMENT_11: v5 보드의 숫자 주사위 라벨은 이동 주사위·1~6칸으로 표시한다. 공유 SymbolLoop/구형 정의의 표시는 변경하지 않는다.

- UI12 R03: 이동 D6의 모든 회전 투영 경계와 제목 간격을 확보하도록 v5 전용 numeric rect를 (260,62,140,140)으로 조정한다. Legacy 크기와 GUID는 불변이다.
