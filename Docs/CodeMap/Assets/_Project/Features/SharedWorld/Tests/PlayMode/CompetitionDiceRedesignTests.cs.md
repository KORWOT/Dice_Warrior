# CompetitionDiceRedesignTests.cs

- 역할: UI12 주사위 트레이의 공개 시점·고정 보관 슬롯·중단/저장 복구·동작 줄이기·이동 경계·추가 주사위 표시를 실제 App과 authored 화면으로 확인하는 PlayMode 검사6개.
- 핵심 동작: 첫 굴림과 부분 리롤 중 움직이는 큐브/결과 라벨/그룹/예상을 숨기고 착지 후 논리 faceIndex와 가장 정면인 물리면을 대조한다. 보관 큐브는 모든 샘플에서 같은 위치·회전·배율·면·ID를 유지한다. 실제 저장 파일을 잠가 실패 후 원문/확정 상태를 보존하고 잠금 해제 뒤1명령만 적용한다. 굴림/리롤 중 두 번째 콜백, Close→Resume의 명령/파일 불변을 검사한다.
- 비교/경계: 동일 격리 초기 문서로 일반/동작 줄이기의 roll→hold→reroll→execute를 실행해 전체 경기 상태/RNG와 명령 종류·인자를 비교한다. 실제 travel은10번칸/D6=6의 명시 fixture에서10→11→0으로 움직이며 눈6/실제2칸을 안내하는지 확인한다. 실제 lap 지급으로1→2→3개가 다음 전투 트레이에 나타나고 마지막 장비 선택은3개를 유지하는지 확인한다.
- 입출력: category `V5UiRedesign12`와 통합 실행용 `V5UiImprovement11`. 실제 v5 JSON·UIRoot/CompetitionScreen prefab·720×1280 카메라·EventSystem → NUnit 단언. 고유 `artifacts/v5-ui-redesign-12/tests/<8자리>/match.json`만 사용하며 사용자 프로필/DB/서버를 열지 않는다.
- 사용하는 대상: CompetitionApp.Open/Session/Close/Render/ReducedMotion/SetReducedMotion, CompetitionStore.Commit/ValidateDocument, CompetitionContent/Random/V5Content/V5Skills, V5Shell.boardPrimary/choices/scroll, Area.cubes/SlotPosition/preview/dice, Cube.RevealResult/ResultFaceIndex/FrontFaceIndex/Held/RollProgress.
- 사용하는 쪽·관계 근거: SharedWorld.PlayMode.Tests asmdef를 통한 메인 Unity Test Runner. Tap은 현재 ScrollRect 노출 후 실제 첫 raycast가 대상 Button임을 단언하고 포인터 클릭을 보낸다. 도메인 명령은 App 입력→Session 저장 경계를 통과한다.
- 상태/수명: 검사별 앱·이벤트·카메라·렌더 타깃을 생성/해제한다. 전투 개수·완주 대기·이동 경계·일반/축소 비교의 초기 문서 재설정은 명시적 유효 격리 fixture이며 자연 경기 진행 증거로 주장하지 않는다. 실제 lap/travel/전투 조작은 명령과 저장을 거친다. 파일·경기 비교에서 클라이언트 명령 ID와 문서 revision이 달라질 수 있는 양 모드 비교는 state와 kind/arg를 대조한다.
- 검수 주의점: 메인만 Unity를 실행한다. 작성/정적 확인은 PASS가 아니며 실제 결과는 V5_UI_REDESIGN_12 REPORT를 따른다. 메인 기존 Flow 검사와 전투 담당의 기하 경계 검사를 대체하지 않는다. 테스트 파일/meta 생성 및 실제 prefab 연결은 메인 import/검증으로 확인한다.

- UI12 R01 회귀: 실제 lap 입력 직후 수령완료 헤더와 미지급 문구 부재를 확인한다.
