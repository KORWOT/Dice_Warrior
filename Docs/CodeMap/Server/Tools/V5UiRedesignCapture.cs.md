# V5UiRedesignCapture.cs

- UI13 확장: Upgrade가 ShellAuthoring도 호출해 기존 프리팹의 본문 패널을 멱등 연결한다. LayoutCapture는 Docs/UI/V5Layout에 로비/이어하기/출전/보드/사건/전투/정보/관리/보상/결과12장을 기록한다. LayoutRatioAudit는390×844/360×640 가상안전영역에서 고정CTA·실제예상행·정보메뉴 경계/글자와6장을 확인한다. Context는 선택적 evidence 경로를 받아 UI13 격리 fixture를 별도 artifacts에 둔다. 기존 UI12 기록을 덮어쓰지 않는다.

- 역할: UI12의 승인된 프리팹 업그레이드와 실제 UI 정지/연출 프레임 캡처. Assets 밖의 Editor Pipeline 도구이며 제품 경로가 아니다.
- 입출력: Upgrade는 CompetitionScreen.prefab 하나에 CombatUiAuthoring/V5RedesignAuthoring을 적용·저장하고 GUID를 비교한다. Capture는 Docs/UI/V5Redesign의 주요 PNG12장과 manifest를 작성한다.
- 사용하는 대상: PrefabUtility, PreviewScene, 실제 UIRoot/CompetitionScreen, CompetitionApp/Session/Store/명령 엔진, 실제 Playback/Area의 animation frame callback.
- 사용하는 쪽·관계 근거: 메인이 UI12 PLAN의 단독 Unity 실행 경계로 run_script entry를 호출한다. 다른 Scene/Prefab/사용자 설정은 저장하지 않는다.
- 핵심 동작: 새 경기 이동은 실제 travel 결과. 전투·보상은 명시적 유효 fixture 후 실제 roll/hold/reroll/execute/lap 명령을 저장한다. arcane/arcane/sword 예시는 물리 index와 faceId가 맞는 격리 상태다. 그림은 실제 authored 화면이며 생성 시안이 아니다.
- 상태/수명: Context가 고유 artifacts 경로와 PreviewScene/카메라/RT/앱을 소유하고 Dispose로 정리한다. 로컬 격리 프로필만 사용하며 HTTP/Firebase/실제 사용자 저장을 읽지 않는다.
- 검수 주의: Sample은 실제 코루틴의 frame callback을 중간/마지막 비율로 평가하는 설명용 프레임 촬영이다. 실제 시간/포인터/중단재개 검증은 PlayMode 결과로 구분한다. 상태 노출·큐브 경계·문자 겹침은 이미지 확인까지 필요하다. 기존 Capture/RatioAudit는 UI12 REPORT, 현재 LayoutCapture/LayoutRatioAudit는 UI13 REPORT를 따른다.

- RatioAudit:390×844/360×640과3%/4%가상안전영역에서 실제 보드·D6트레이·CTA 경계/보드 글자 높이/이동 D6 41투영프레임의 제목 비충돌을 검사하고 artifacts에 추가 비율 사진4장과 결과를 기록한다.
- Capture 배치 순서: 가상 안전영역 적용→Canvas.ForceUpdateCanvases→요청된 app.Render→Canvas 갱신 후 촬영한다. PreviewScene의 이전 rect로 배율이 계산되는 것을 방지한다. render=false는 재Bind를 생략하여 실제 연출의 샘플 프레임을 보존한다. R04 수정·사용자 승인 추가1회에서 두 비율·총82프레임 및 사진4장 확인 PASS. 증거와 실기기 NOT_RUN은 REPORT에 기록한다.

## UI15 주요 화면과 비율

- `FeedbackCapture`는 Docs/UI/V5Feedback에 전투 준비/예상/정보/직접 메뉴/승리 성장/AI 조회/관전/최종 결과8장과 manifest를 작성한다. 기존 UI12/UI13 캡처 경로는 덮어쓰지 않는다.
- `MakeFeedbackVictory`는 고유한 격리 fixture에 레벨업 직전 XP/적 HP1을 설정한 후 실제 App 버튼·Session 저장·승리/AI 처리를 사용한다. 지급/순위를 직접 주입하지 않는다. 시험 상태와 실제 사용자 플레이를 구분한다.
- `FeedbackRatioAudit`는390×844/360×640 및3%/4% 모의 안전영역에서 예상/직접 메뉴, 최근 결과/AI 조회/관전의 텍스트/경계와 사진8장을 확인한다. Evidence는 artifacts/v5-combat-feedback-15/captures 아래에만 작성한다.
- 실제 시간·포인터·저장 불변은 PlayMode 검사가 담당하고 이 도구는 PreviewScene에 작성된 프리팹을 렌더한다. 파일 생성과 직접 시각 확인·실기기 검증을 각각 보고한다.

- UI15 수정1: 승리 촬영 fixture는 selected offer의 골드/XP/명예를 현재 원본 EncounterDefinition에서 복사한다. 기본0 보상 fixture를 성장 사례로 잘못 표시하지 않으며 실제 execute 후 레벨 증가가 없으면 촬영을 실패시킨다.

- UI15 수정2: 최종 결과의 두 헤더 버튼은 이미지의 실제 밝은 글자 픽셀이 없으면 캡처 실패로 처리한다. 미리보기에서 보인 누락은 보존된 초기 PNG와 새 경로로 연 최종 PNG에서 동일한 글자 픽셀478개로 확인되어 실제 파일 결함으로 재현되지 않았다. 촬영용 Text 강제 재초기화는 채택하지 않는다. 실제 PlayMode의 같은 경로도 픽셀 검사를 통과했다.
