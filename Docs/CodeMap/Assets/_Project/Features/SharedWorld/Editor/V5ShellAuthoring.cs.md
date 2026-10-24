# V5ShellAuthoring.cs

## 역할과 입출력

`public static Upgrade(CompetitionScreen screen)`는 호출자가 연 CompetitionScreen 프리팹 콘텐츠에 v5 주요 화면용 subtree를 추가한다. 입력은 기존 content가 연결된 Screen, 출력은 screen.v5Shell과 그 authored 자식/참조다. 프리팹 저장·Scene 열기/저장·SaveAssets는 호출하지 않는다.

## 핵심 동작

기존 프리팹을 파괴하지 않고 `content/V5 Main Shell` 아래 header/summary/notice, ScrollRect/RectMask2D/본문, 선택 카드6개와 설명, 로컬 페이지 버튼, tools4/tabs5/back를 작성한다. 연결된 v5Shell이 이미 있으면 다시 만들지 않는다. 같은 이름의 미연결 subtree는 오류로 중단한다. 공용 폰트와 CommonButtonView 프리팹은 읽기·인스턴스 참조만 사용한다.

선택 카드의 설명은 해당 버튼 안에 있고 raycastTarget=false다. 카드/탭/도구의 실제 배치는 V5ShellView가 내용에 맞춰 조정하며 authored 기준은720×1280이다. 새 Text에는 CanvasRenderer를 함께 작성한다. 기존 Preview footer 참조는 v5 표시 때만 숨기고 구형 복귀 시 복구하는 용도로 연결한다.

## 관계·수명·검수

사용 대상: AssetDatabase·PrefabUtility.InstantiatePrefab/RecordPrefabInstancePropertyModifications, CompetitionScreen/V5ShellView, CommonButtonView, Unity uGUI. 사용하는 쪽: 메인의 승인된 prefab-only 업그레이드 실행. 기존 CompetitionAuthoring.UpgradeV5와 달리 Title을 건드리지 않는다.

반드시 main이 Play/dirty 상태를 확인하고 허용된 CompetitionScreen.prefab에만 호출한다. 기존 GUID/로컬 노드/공용 자산을 재생성하지 않는다. 이 코드 작성만으로 실제 자산 반영이나 모바일 검증 PASS가 되지 않는다.
