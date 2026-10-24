# V5ShellView.cs

## 역할

v5 로비·출전·계정·사건·완주·결과·상세를 위한 authored 중앙 콘텐츠/선택 카드/조회 도구 바인더. 게임 규칙이나 저장을 실행하지 않는다. V5ShellData와 V5ShellAction은 CompetitionScreenData에서 전달하는 UI 전용 계약이며 저장 DTO가 아니다.

## 핵심 동작·입출력

- `Bind(V5ShellData)`: mode/title/summary/body/notice/showBoard, choices/tools/tabs/back를 Text·ScrollRect·CommonButtonView에 연결한다.
- choices는 authored6개 카드와 설명 Text를 재사용한다.6개를 넘으면 로컬 페이지로 순회한다. 모든 후보의 콜백은 입력한 자료와 같은 원본 항목을 유지한다.
- tools4개, tabs5개, back1개를 선택 카드와 별도 배치한다. 탭은3+2행, 도구는2열, 뒤로는 전체폭이다. 카드 최소104·나머지 버튼96 설계단위다. 실제 dp를 단정하지 않는다.
- 본문/카드 높이는 현재 텍스트의 preferredHeight로 계산한다. 선택된 카드는 `[선택]`과 색을 함께 표시한다. 모드가 같으면 로컬 스크롤/페이지를 유지하고 바뀌면 처음으로 복귀한다.
- V5_UI_REDESIGN_12의 showBoard는 `choices[0]`과 동일한 ID/콜백을 고정 `boardPrimary`에 바인딩한다. 보드에서는 선택 1개·헤더 도구 최대 2개·탭/뒤로 없음이 계약이며 일반 ScrollRect/카드는 숨긴다. BoardPrimary는 y1160/h96, notice는 y1100/h48, 제목/2줄 요약은 좌측, 도구 2개는 우측이다. App이 긴 AI 결과를 메뉴에 제공하고 Screen이 실제 보드/트레이를 표시한다.
- `SetBoardPresentation(stage,detail,busy)`는 고정 CTA 문구·안내·입력 상태만 바꾸며 heading과 원래 명령 콜백/ID를 유지한다. Playback이 저장된 결과의 굴림→공개→경로→도착에 맞춰 호출한다. 이 메서드는 Bind나 명령 실행·난수 계산을 하지 않는다.
- `ShowDiceGrowth(state,previousCount,progress,reducedMotion)`는 authored rewardGrowth/rewardGrowthLabel/rewardDice[3]를 사용해 실제 1→2/2→3 구성을 표시한다. 이전 개수+1만 허용하고 실제 마지막 주사위 종류를 표시한다. 모두 실제 6면을 연결하되 RevealResult=false/Pose1로 결과가 아닌 구성 전시임을 명시한다. 기존 큐브는 고정, 새 큐브만 CanvasGroup fade와 작은 배율 변화를 적용하며 reducedMotion이면 배율 변화가 없다. 메인 Playback이 진행 시간과 호출을 소유한다. 다음 Bind/Unbind에서 획득 표시를 닫는다.
- 공통 색은 잉크 네이비 #101B2D·슬레이트 #1C2B42·아이보리 #F4EEDF·보조 #B5C1D0·진행 앰버 #F3BD5C·선택 청록 #52D6CD를 사용한다. 본문 카드/페이지/탭의 기존 선택 및 상태 불변 계약은 유지한다.
- `ChoicePage`, `Data`는 읽기 전용 검사/관찰용이다. `Unbind`는 모든 소유 콜백과 로컬 조회 상태를 해제한다.

## 관계·수명

사용 대상: CommonButtonView/ButtonAppearance, Unity Text·RectTransform·ScrollRect, V5DieGraphic과 CompetitionV5Content.Faces. 사용하는 쪽: CompetitionScreen.OnBind/OnUnbind, V5ShellAuthoring/V5RedesignAuthoring의 직렬화 연결, CompetitionPlayback의 보드 단계/획득 표시. DTO 자료와 콜백은 CompetitionApp의 v5 표현 분기가 만든다. 런타임 GameObject 생성이나 콘텐츠/난수 재계산이 없다. UIManager가 캐시하는 CompetitionScreen과 같은 수명을 사용한다.

## 검수 주의

빈 action ID는 공용 버튼 계약 위반이다. tools4/tabs5를 초과하면 명시 오류이며, 후보 수는 로컬 페이지로 제한 없이 조회한다. back는 Action만 받으므로 요청 중 동작 제한은 호출자가 콜백을 감싸거나 null로 제공한다. 상세 모달이 열린 동안 실제 raycast 차폐는 v5Net이 담당한다. 선택/탭/스크롤은 게임 상태를 변경하지 않으며 invoke의 권한·중복 방어는 기존 App 경계에 남긴다.

- UI12 R01: ShowDiceGrowth는 확정 지급 이후임을 헤더/요약에도 표시한다. 이전 보상 선택의 미지급 문구를 남기지 않는다.
