# CompetitionCombatUiAuthoring.cs

- 역할: v5 UI 개선의 전투/6면·전투정보 subtree를 기존 CompetitionScreen 프리팹 안에서 배치하는 Editor 전용 도구.
- 입출력: `public static void Upgrade(CompetitionScreen screen)`은 메인이 연 프리팹의 Screen만 받는다. 실제 자산 저장·Scene 열기/저장·SaveAssets·공용자산 변경은 하지 않는다.
- 핵심 동작: 기존 Area/Net 소유를 확인하고 HP바/숫자 대상 정렬,230높이 Roll/Hold 영역, 중심pivot180×148 카드·고정slot 위치,96높이 정보/행동 버튼, 전체3기술예상 공간, 스크롤 가능한 긴 실패/상세 본문, 별도 모험 메뉴 제목을 작성한다.6면 물리위치·중복과 기존 object 참조를 보존한다.
- 사용하는 대상: V5DiceAreaView.CardPosition, 기존 Area/Net/Text/CommonButtonView/V5FaceGraphic, PrefabUtility.RecordPrefabInstancePropertyModifications, EditorUtility.SetDirty. ScrollRect/RectMask2D와 필요한 제목은 Editor API로 추가한다.
- 사용하는 쪽·관계 근거: 메인 작성 프리팹 업그레이드 호출이 Upgrade에 열린 Screen을 전달한다. CompetitionCombatUiTests가 실제 작성된 배치/포인터/프레임 경계를 검증한다.
- 상태/수명: 명시 호출 동안만 프리팹 contents를 수정한다. 저장과 Unload는 호출자 소유다. 기존 노드를 임의 삭제·재생성하지 않으며 동일 이름의 scroll/제목이 있으면 재사용한다.
- 검수 주의: 편집 코드 실행과 실제 프리팹 저장/PlayMode PASS를 구분한다.96은720설계단위이며 실제dp 검증이 아니다. 카드 회전 범위 전제는180×148 중심pivot·x115/330/545·y140·최대±6도/1.03배이다.
