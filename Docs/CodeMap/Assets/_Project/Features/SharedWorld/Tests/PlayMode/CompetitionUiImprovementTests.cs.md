# CompetitionUiImprovementTests.cs

- 역할: v5 UI 개선의 진행 차단·읽기 전용 복귀·출전 설정·계정 피드백·스크롤 터치 회귀를 실제 authored CompetitionScreen/UIRoot와 EventSystem으로 확인하는 PlayMode 검사 6개다.
- 핵심 동작: 실제 Session.Start 경로부터 travel/choose/combat_roll/combat_reroll/combat_execute/lap 명령을 순차 실행해 세 번째 LapReward까지 도달한다. 허용 초기 프로필 growth=20을 네 참가자에게 동일하게 동결하며 completedLaps·전투 결과·저장 명령을 주입하지 않는다. 중간 연출은 직접 Session 명령으로 생략하고 최종 장비 수령은 실제 포인터를 사용한다.
- 보상 검사: 장비 5개 카드의 효과 비교·스크롤 접근, 주사위/능력치 조회·모험 메뉴·로비·이어하기의 상태/파일 원문 불변, 세 번째 lap 명령 1회·claim 1개·최종 시련 진입과 재개 후 보존을 확인한다. 실제로 방문한 이동/사건/전투/보상 단계에서 Back이 명령을 실행하지 않는지도 확인한다.
- 출전 검사: 실제 계정 선호 저장에서 주사위/무기/모험가를 연속 선택하고 선택 표시·다음 경기 안내·재개한 계정의 출전 값, 진행 중 경기 불변을 비교한다. 포기 확인·새 모험 확인에서 HandleBack은 취소만 수행하며 교체 사본이나 경기 명령을 만들지 않는다.
- 계정 검사: 메모리 IGameBackend의 지연 승인·승인 성공·재화 부족·통신 실패 후 미확정 요청 안내, 구매 잠금과 명시적 재확인 진입을 검사한다. 별도 구형 활성 런 fixture에서는 보류 사유·구매 불가·기존 원문 보존과 새 공동 경기 시작 가능 여부를 확인한다. 신규 1인 시작·정산 API 호출은 모두 금지한다.
- 화면 검사: 720×1280, 720×1600, 360×640 RenderTexture와 3/4% 가상 안전영역에서 긴 한국어 선택 카드 8개·로컬 페이지·도구·돌아가기의 포인터와 경계를 확인한다. 카드 클릭 전에 ScrollRect로 중앙을 노출한 뒤 실제 EventSystem 첫 raycast의 Button이 대상과 같은지 검증한다. 설계 높이 96 단위는 확인하되 물리 장치의 dp PASS를 뜻하지 않는다.
- 입력/출력: 실제 shared-world-preview-5.json·허용 prefab, 고유 artifacts/v5-ui-improvement-11/tests/8자리ID 경로의 계정/경기 → NUnit 결과와 세 번째 보상 첫 화면/5번째 카드/최종 시련 및 3해상도 Shell PNG. 외부 콘텐츠 정의나 실제 사용자 저장·HTTP 서버를 사용하지 않는다.
- 사용하는 대상: CompetitionApp.Open/OpenAccount/Render/HandleBack, CompetitionSession.Prepare/Execute/Snapshot, CompetitionCommandRules.LegalChoices, V5ShellView의 Bind 결과·scroll·페이지·CommonButtonView.BoundId, UIManager, LoopStore/LoopSession 및 IGameBackend 프로토콜.
- 사용하는 쪽/관계 근거: SharedWorld.PlayMode.Tests asmdef의 Unity Test Runner가 실행한다. App이 실제 prefab을 표시하고 테스트는 메뉴 DTO의 공개 ID와 실제 Button 포인터로 UI 경로를 선택한다. 도메인 진행은 Session 저장 경계를 지나며 원본 상태를 직접 수정하지 않는다.
- 상태/수명: 검사별 앱/이벤트/카메라/렌더 타깃을 생성·해제한다. ignored 경로의 검사 저장·PNG는 증거로 남긴다. 실제 Unity 실행과 prefab import는 메인 작성자가 직렬 수행한다.
- 검수 주의점: 소스 작성과 정적 diff 검사를 실제 Unity PASS로 취급하지 않는다. 첫 통합 검수는 메인의 V5_UI_IMPROVEMENT_11 작업 묶음에 포함하며 별도 수정 횟수를 만들지 않는다. 테스트가 새 public 제품 API를 추가하거나 게임 규칙을 변경하지 않는다.

- V5_UI_IMPROVEMENT_11: 회차1 직접 회귀: 사건 선택 실제포인터→AI 진행안내→lastRound표시→포기확정/개인종료·재개를 추가했다(총6개).
