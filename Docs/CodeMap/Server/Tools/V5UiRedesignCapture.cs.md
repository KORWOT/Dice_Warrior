# V5UiRedesignCapture.cs

- 역할: UI12의 승인된 프리팹 업그레이드와 실제 UI 정지/연출 프레임 캡처. Assets 밖의 Editor Pipeline 도구이며 제품 경로가 아니다.
- 입출력: Upgrade는 CompetitionScreen.prefab 하나에 CombatUiAuthoring/V5RedesignAuthoring을 적용·저장하고 GUID를 비교한다. Capture는 Docs/UI/V5Redesign의 주요 PNG12장과 manifest를 작성한다.
- 사용하는 대상: PrefabUtility, PreviewScene, 실제 UIRoot/CompetitionScreen, CompetitionApp/Session/Store/명령 엔진, 실제 Playback/Area의 animation frame callback.
- 사용하는 쪽·관계 근거: 메인이 UI12 PLAN의 단독 Unity 실행 경계로 run_script entry를 호출한다. 다른 Scene/Prefab/사용자 설정은 저장하지 않는다.
- 핵심 동작: 새 경기 이동은 실제 travel 결과. 전투·보상은 명시적 유효 fixture 후 실제 roll/hold/reroll/execute/lap 명령을 저장한다. arcane/arcane/sword 예시는 물리 index와 faceId가 맞는 격리 상태다. 그림은 실제 authored 화면이며 생성 시안이 아니다.
- 상태/수명: Context가 고유 artifacts 경로와 PreviewScene/카메라/RT/앱을 소유하고 Dispose로 정리한다. 로컬 격리 프로필만 사용하며 HTTP/Firebase/실제 사용자 저장을 읽지 않는다.
- 검수 주의: Sample은 실제 코루틴의 frame callback을 중간/마지막 비율로 평가하는 설명용 프레임 촬영이다. 실제 시간/포인터/중단재개 검증은 PlayMode 결과로 구분한다. 상태 노출·큐브 경계·문자 겹침은 이미지 확인까지 필요하다. 모든 체크와 현재 결과는 UI12 REPORT를 따른다.

- RatioAudit:390×844/360×640과3%/4%가상안전영역에서 실제 보드·D6트레이·CTA 경계/보드 글자 높이/이동 D6 41투영프레임의 제목 비충돌을 검사하고 artifacts에 추가 비율 사진4장과 결과를 기록한다.
- Capture 배치 순서: 가상 안전영역 적용→Canvas.ForceUpdateCanvases→요청된 app.Render→Canvas 갱신 후 촬영한다. PreviewScene의 이전 rect로 배율이 계산되는 것을 방지한다. render=false는 재Bind를 생략하여 실제 연출의 샘플 프레임을 보존한다. R04 수정·사용자 승인 추가1회에서 두 비율·총82프레임 및 사진4장 확인 PASS. 증거와 실기기 NOT_RUN은 REPORT에 기록한다.
