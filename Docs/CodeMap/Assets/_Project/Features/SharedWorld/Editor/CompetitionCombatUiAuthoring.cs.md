# CompetitionCombatUiAuthoring.cs

- 역할: v5 UI 개선의 전투/6면·전투정보 subtree를 기존 CompetitionScreen 프리팹 안에서 배치하는 Editor 전용 도구.
- 입출력: `public static void Upgrade(CompetitionScreen screen)`은 메인이 연 프리팹의 Screen만 받는다. 실제 자산 저장·Scene 열기/저장·SaveAssets·공용자산 변경은 하지 않는다.
- 핵심 동작: 기존 Area/Net 소유를 확인하고 HP바/숫자 대상 정렬,192높이 작은 Glyph 전투장면,352높이 단일 트레이,200×248 고정 슬롯과160×180 V5DieGraphic3개,96높이 정보/행동 버튼, 전체3기술 예상, 스크롤 가능한 긴 실패/상세 본문을 작성한다. holdZone은 숨기고 holdHint를 같은 트레이로 옮기며 기존2D faces 참조는 보존해 숨긴다. Net의 stats 옆에96높이 motion 버튼을 추가한다.6면 상세 물리위치·중복과 기존 object 참조를 보존한다.
- 사용하는 대상: V5DiceAreaView.SlotPosition, 기존 Area/Net/Text/CommonButtonView/V5FaceGraphic, V5DieGraphic/LoopGlyphGraphic, PrefabUtility.RecordPrefabInstancePropertyModifications, EditorUtility.SetDirty. 새 Cube/장면/ScrollRect/RectMask2D와 필요한 제목은 Editor API로 추가한다. motion은 기존 공용 버튼 프리팹의 별도 instance를 사용하며 공용자산을 저장하지 않는다.
- 사용하는 쪽·관계 근거: 메인 작성 프리팹 업그레이드 호출이 Upgrade에 열린 Screen을 전달한다. CompetitionCombatUiTests가 실제 작성된 배치/포인터/프레임 경계를 검증한다.
- 상태/수명: 명시 호출 동안만 프리팹 contents를 수정한다. 저장과 Unload는 호출자 소유다. 기존 노드를 임의 삭제·재생성하지 않으며 동일 이름의 scroll/제목이 있으면 재사용한다.
- 검수 주의: 편집 코드 실행과 실제 프리팹 저장/PlayMode PASS를 구분한다.96은720설계단위이며 실제dp 검증이 아니다. Cube 메시의 전체 회전/도약/그림자 경계는 실제 PlayMode 샘플과 캡처로 확인한다. 단일 트레이의 slots는 부모를 바꾸거나 카드 자체를 회전하지 않는다. Net.motion 콜백/표시 데이터는 메인 소유 Net/App에서 연결한다.
