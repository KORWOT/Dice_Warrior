# CompetitionCombatUiAuthoring.cs

- 역할: v5 UI 개선의 전투/6면·전투정보 subtree를 기존 CompetitionScreen 프리팹 안에서 배치하는 Editor 전용 도구.
- 입출력: `public static void Upgrade(CompetitionScreen screen)`은 메인이 연 프리팹의 Screen만 받는다. 실제 자산 저장·Scene 열기/저장·SaveAssets·공용자산 변경은 하지 않는다.
- 핵심 동작: 기존 Area/Net 소유를 확인하고 HP바/숫자 대상 정렬,144높이 작은 Glyph 전투장면, y376의352높이 단일 트레이,200×248 고정 슬롯과160×180 V5DieGraphic3개를 작성한다. UI13은 제목 y748와3개 예상 행(y792+80*i,h72)의 Text628×56/font22를 멱등 연결한다.6면/실패 안내는 y1040/h96에 모으고 고정 footer는 유지한다. holdZone/기존2D faces는 참조를 보존해 숨긴다. Net의 stats 옆에 새96높이 menu 버튼을 연결하고 기존 motion/leave/abandon/관리제목은 숨긴다.6면 상세 물리위치·중복과 기존 object 참조를 보존한다.
- 사용하는 대상: V5DiceAreaView.SlotPosition, 기존 Area/Net/Text/CommonButtonView/V5FaceGraphic, V5DieGraphic/LoopGlyphGraphic, PrefabUtility.RecordPrefabInstancePropertyModifications, EditorUtility.SetDirty. Cube/장면/ScrollRect/RectMask2D/예상 행과 제목은 Editor API로 연결한다. menu는 기존 공용 버튼 프리팹의 별도 instance를 사용하며 공용자산을 저장하지 않는다.
- 사용하는 쪽·관계 근거: 메인 작성 프리팹 업그레이드 호출이 Upgrade에 열린 Screen을 전달한다. CompetitionCombatUiTests가 실제 작성된 배치/포인터/프레임 경계를 검증한다.
- 상태/수명: 명시 호출 동안만 프리팹 contents를 수정한다. 저장과 Unload는 호출자 소유다. 기존 노드를 임의 삭제·재생성하지 않으며 동일 이름의 scroll/제목이 있으면 재사용한다.
- 검수 주의: 편집 코드 실행과 실제 프리팹 저장/PlayMode PASS를 구분한다.96은720설계단위이며 실제dp 검증이 아니다. UI13은 트레이 바깥 위치와 작은 장면 배치만 바꾸며 큐브 내부 크기·SlotPosition은 변경하지 않는다. Cube 메시의 전체 회전/도약/그림자와 실제 예상3행/좁은 화면 경계는 PlayMode 샘플과 캡처로 확인한다. Net.menu callback과 원본 data 복귀는 App에서 연결한다. information의 내용 기반 크기는 V5DiceNetView가 적용하고6면 배치로 복원한다.
