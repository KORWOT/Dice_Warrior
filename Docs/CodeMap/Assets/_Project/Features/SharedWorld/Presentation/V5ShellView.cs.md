# V5ShellView.cs

## 역할

v5 로비·출전·계정·사건·완주·결과·상세를 위한 authored 중앙 콘텐츠/선택 카드/조회 도구 바인더. 게임 규칙이나 저장을 실행하지 않는다. V5ShellData와 V5ShellAction은 CompetitionScreenData에서 전달하는 UI 전용 계약이며 저장 DTO가 아니다.

## 핵심 동작·입출력

- `Bind(V5ShellData)`: mode/title/summary/body/notice/showBoard, choices/tools/tabs/back를 Text·ScrollRect·CommonButtonView에 연결한다.
- choices는 authored6개 카드와 설명 Text를 재사용한다.6개를 넘으면 로컬 페이지로 순회한다. 모든 후보의 콜백은 입력한 자료와 같은 원본 항목을 유지한다.
- tools4개, tabs5개, back1개를 선택 카드와 별도 배치한다. 탭은3+2행, 도구는2열, 뒤로는 전체폭이다. 카드 최소104·나머지 버튼96 설계단위다. 실제 dp를 단정하지 않는다.
- 본문/카드 높이는 현재 텍스트의 preferredHeight로 계산한다. 선택된 카드는 `[선택]`과 색을 함께 표시한다. 모드가 같으면 로컬 스크롤/페이지를 유지하고 바뀌면 처음으로 복귀한다.
- showBoard이면 기존 보드/참가자/주사위 공간을 위해 중앙 스크롤 시작점을870으로 둔다. App은 짧은 summary와 Map의 적은 공통 도구를 전달한다. 보드/연출의 활성화 자체는 CompetitionScreen이 담당한다.
- `ChoicePage`, `Data`는 읽기 전용 검사/관찰용이다. `Unbind`는 모든 소유 콜백과 로컬 조회 상태를 해제한다.

## 관계·수명

사용 대상: CommonButtonView/ButtonAppearance, Unity Text·RectTransform·ScrollRect. 사용하는 쪽: CompetitionScreen.OnBind/OnUnbind, V5ShellAuthoring의 직렬화 연결. DTO 자료와 콜백은 CompetitionApp의 v5 표현 분기가 만든다. 런타임 GameObject 생성이나 콘텐츠/난수 재계산이 없다. UIManager가 캐시하는 CompetitionScreen과 같은 수명을 사용한다.

## 검수 주의

빈 action ID는 공용 버튼 계약 위반이다. tools4/tabs5를 초과하면 명시 오류이며, 후보 수는 로컬 페이지로 제한 없이 조회한다. back는 Action만 받으므로 요청 중 동작 제한은 호출자가 콜백을 감싸거나 null로 제공한다. 상세 모달이 열린 동안 실제 raycast 차폐는 v5Net이 담당한다. 선택/탭/스크롤은 게임 상태를 변경하지 않으며 invoke의 권한·중복 방어는 기존 App 경계에 남긴다.
