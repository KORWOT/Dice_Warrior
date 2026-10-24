# V5RedesignAuthoring.cs

- 역할: UI11 CompetitionScreen에 UI12 보드 고정 진행 버튼, 참가자 카드 배경 4개, 주사위 증가 전시를 추가하는 Editor 전용 authoring이다.
- 입력/출력: `public static Upgrade(CompetitionScreen screen)`은 호출자가 연 prefab contents를 받는다. screen.v5RosterCards, v5Shell.boardPrimary/rewardGrowth/rewardGrowthLabel/rewardDice 참조를 작성하며 prefab/scene 저장·열기·SaveAssets를 하지 않는다.
- 핵심 동작: 기존 board/cells/pawns/roster/tray의 좌표·색을 수정하지 않는다. Screen이 최초 인스턴스에서 이 원본을 캡처한 뒤 v5 레이아웃으로 전환하므로 v1~v4 복귀의 기준이 보존된다. v5 전용 Shell 제목/보조 문구만 팔레트와 글자 크기를 적용한다.
- 추가 노드: 기존 Participants 아래 비활성 카드 Image 4개(뒤쪽 sibling), Shell 아래 CommonButtonView 기반 Fixed board advance, 전용 획득 표시 루트/슬레이트 패널/V5DieGraphic 3개/문구다. 획득 큐브 각각 CanvasRenderer·CanvasGroup을 authored로 갖추며 raycast를 받지 않는다. 메모리 오브젝트를 런타임에 생성하지 않는다.
- 사용하는 대상: AssetDatabase·PrefabUtility.InstantiatePrefab/RecordPrefabInstancePropertyModifications, 공용 Pretendard-Regular.ttf/CommonButtonView.prefab의 읽기 참조, CompetitionScreen/V5ShellView/V5DieGraphic, Unity uGUI.
- 사용하는 쪽/관계 근거: 메인의 허용 CompetitionScreen.prefab 업그레이드 경로가 호출한다. Screen은 참가자 카드 입력을 요구하고 Shell은 보드/획득 표시에서 해당 새 참조를 검증한다. 신규 UI 표시는 이 authoring 없이는 오류를 내며 임의 런타임 대체를 하지 않는다.
- 상태/수명: 이미 연결된 노드는 중복 생성하지 않는다. 같은 이름의 미연결 노드나 불완전한 배열은 명시 오류다. 기존 GUID/프리팹 참조를 재생성하지 않는다.
- 검수 주의점: Unity 실행과 실제 prefab 저장은 메인 담당이며 이 소스 작성만으로 자산 반영이나 PlayMode PASS가 되지 않는다. 허용 파일은 CompetitionScreen.prefab 하나이고 Title/폰트/공용 UI 프리팹을 저장하지 않는다. 스크린 기준 좌표 720×1280은 물리 dp 검증을 대신하지 않는다.
