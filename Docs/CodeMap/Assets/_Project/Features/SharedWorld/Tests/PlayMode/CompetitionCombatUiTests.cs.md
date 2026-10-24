# CompetitionCombatUiTests.cs

- 역할: UI 개선 전투 배치·계획 연결·6면 조회의 실제 authored 프리팹 회귀 검사. 기존 CompetitionV5FlowTests와 별도 fixture이며 저장/게임 규칙 테스트를 복제하지 않는다.
- 입력/출력: 실제 UIRoot/CompetitionScreen·v5 콘텐츠, 별도 Camera/RenderTexture/EventSystem, 순수 격리 CompetitionState → NUnit assertion. 실제 사용자 저장·서버·App.Start를 사용하지 않는다.
- 핵심 검사:720×1280/1600·540×960과 안전영역에서1~3주사위·모든보관마스크의 실제roll/reroll/hold/release iterator 프레임을0/.15/.25/.4/.5/.75/1로 샘플링한다. 보관마스크는 각 대상 주사위의 실제 combat_hold 명령으로 구성하여 held/holdChanges/actionNumber의 유효 이력을 함께 유지한다(UI11-R02). 카드·영역·안내·다른카드·safe area 경계를 검사한다. 직선이동 대신 목적영역fade의 모든 프레임도 검사한다.
- 계획/입력: 실제 PlanSteps와Preview의 dieSlotIndices·화면기술연결·전체3개 개수·지속종류라벨을 확인한다. 실제 첫raycast가 의도한 Button인지 검사 후 pointerClick을 보내 해당instance 명령을 확인한다. 꽝 전체의 실행 가능도 유지한다.
- 상세:3직업×3슬롯×6물리면을 실제포인터로 조회한다. 이번굴림과조회중 표시,중복면,등록태그명,초기결과면,다른data를거친선택복원을 확인하고원본Snapshot불변을 비교한다.
- 사용하는 대상: CompetitionRules/CommandRules, V5Skills.PlanSteps/Preview, V5Area/Net, Unity UI/EventSystem/InputSystem, AssetDatabase. 사용하는 쪽: 메인의 지정 PlayMode 실행.
- 상태/수명: Editor에서만 컴파일하며 생성한UI/카메라/EventSystem/RenderTexture만 teardown에서 제거한다. 직접 UIRoot의 input group/캐시/안전영역을 격리 설정하고 App은 열지 않는다. 원본 프리팹은 읽기만한다. Frame 콜백 reflection은 production Animate의 같은함수를샘플링하는 검사전용이며 제품 동작을 대체하지 않는다.
- 검수 주의: 테스트코드작성과실제Unity결과는별개다. 외부수치/게임판정을단언하지않으며 실기dp/전체플레이통과를주장하지않는다. 실제패스수와증거는 V5_UI_IMPROVEMENT_11_REPORT에기록한다.
