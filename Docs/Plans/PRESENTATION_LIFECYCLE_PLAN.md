# 등급별 연출·종료 동기화 PLAN

상태: IMPLEMENT 완료. 2026-09-10 사용자 신규 계약: 일반 조합은 간단하고 짧게, 높은 등급은 화려하고 길게, 실제 연출 시작/종료로 전투 흐름 동기화, 10초 초과 오류 강제 종료 및 로그.

## 계약 / AC
- 기존 오라 완료 작업을 기반으로 새 등급별 모션과 수명 계약을 구현한다. DICE_COMBO_AURA의 2/2 보완 기록은 유지하고 재설정하지 않는다. 이번 새 계약의 최초 검토 1묶음 및 보완 최대 2묶음.
- 저장 priority 순위 comboStrength로 일반/문양/화려/최상위 4단계 연출을 선택한다. 색은 기존 hand별 편집값 유지. 일반은 링만, 문양부터 중앙 룬/순차 점등, 화려는 회전 궤도와 파동, 최상위는 추가 광선과 여러 파동. 단순 scale/색 변경만으로 동일 연출을 복제하지 않는다.
- 설정 자산에 각 단계 등장/강조/읽기/퇴장 시간을 편집 가능하게 작성한다. 기존 holdSeconds는 최소 읽기 시간으로 유지하고 0은 연출 생략한다. 따라서 이전 고정 총 hold 계약만 사용자 요청대로 대체된다. RNG/주사위 순서/조합 판정/카드·보상/저장 schema와 명령 확정 1회는 유지한다.
- 컨트롤러는 주사위/결과/카드/전투의 실제 완료를 기다린다. 카드 FEEL은 실제 player.IsPlaying 종료를 확인한다. 결과는 등장→강조→읽기→퇴장 및 외부 플레이어 완료 후 끝난다. 컨트롤러의 .22/.18 및 단순 action 대기는 제거한다. 노드 도착의 .35 설정은 view 쪽 편집 필드로 이동한다.
- PresentationPlayback은 한 연출 시퀀스의 Label/State/StartedAt/EndedAt/Started/Ended를 노출한다. 정상 Completed, 취소 Cancelled, 예외 Faulted, 제한 TimedOut을 구분한다. 이벤트는 시작/종료 각 1회. 저장 후 연출 시퀀스 전체 기준 실제 시간 10초 제한이며 timeScale=0에도 동작한다.
- 중첩 IEnumerator/WaitForSecondsRealtime도 매 프레임 감독한다. 10초 초과 시 중첩 연출 finally 정리, 효과/transform/popup/잠금 해제, 확정 snapshot 재표시, context/elapsed/limit 포함 오류 로그 1회. 예외/취소에 추가 명령·저장·RNG 없음. 재바인딩/비활성/삭제 시 잔여 연출이 다음 표시를 덮지 않는다.
- 지정 검증: 시작/끝 1회, 자연 종료·중첩 기다림·10초 실제 watchdog·timeScale0·중단·실패·뒤늦은 완료 안전, grade별 다른 구조/시간, 동일한 저장 결과, 실제 popup/전투 입력 경로, 두 화면 비율 캡처.

## 기준 / 허용 경로
- HEAD d8df4999174ad63bb73e0727ff0149cb55624db2, 기존 status와 보호 hash는 artifacts/presentation-lifecycle/baseline.json. 이전 미커밋 구현을 명시 확장하고 사용자/공급사/패키지 변경을 보존한다. commit/stage/branch 작업 없음.
- Main C#: Run/Presentation/{DiceRollUI,DiceResultFeedback,RunUIController,FateDiceWidgets,SelectionFeedback,CombatUI}.cs, Run/Configs/DiceFeedbackCatalog.cs, Run/Editor/DicePresentationAuthoring.cs, Exploration/Presentation/CampaignMapView.cs. 직접 영향 검사 DicePresentationEffectsTests/DiceAuraTests/CombatFeedbackTests/RunBoundaryFlowTests와 필요 새 PresentationFlowTests. 변경하지 않는 파일은 범위에 있어도 수정하지 않는다.
- 신규 Run/Presentation/PresentationPlayback.cs 및 Run/Tests/PlayMode/PresentationPlaybackTests.cs. SeedPresentationFlowTests의 이전 고정 hold 총길이 검사는 새 최소 읽기+실제 종료 계약으로 갱신한다. 대응 1:1 CodeMap/직접 관계/INDEX, 이 PLAN/REPORT, artifacts/presentation-lifecycle/ 검증 도구 및 C# 요약.
- Editor API로 허용하는 정확한 자산: Run/Configs/DiceFeedbackCatalog.asset, Run/Prefabs/DiceRollUI.prefab, Run/Prefabs/CombatUI.prefab, Combat/Prefabs/ActionCardView.prefab, Fate/Prefabs/FateCardView.prefab, Run/Prefabs/ExplorationUI.prefab(실제 CampaignMapView 소유 원본 확인). 기존 GUID/사용자 색·배치 보존. 씬/DefaultFateDice/manifest/lock/저장 파일 쓰기 금지.

## 담당 / 위임 상한
- Main: 계약/설정/Controller/결과/카드·전투 연결, 모든 Unity 실행, 자산 작성, 통합과 PLAN/REPORT/INDEX. 같은 파일 한 작성자.
- playback_worker: PresentationPlayback.cs + PresentationPlaybackTests.cs 및 두 CodeMap만 작성. IEnumerator를 중첩 구동하며 state/time/events/cancel/watchdog 계약 구현. Unity 실행 금지, RED 검사부터 Main에 전달.
- aura_worker: DiceAuraGraphic.cs + 해당 CodeMap만 작성. 합의된 complexity/phase/opacity 입력을 이용한 네 종류의 구조/모션. 게임 설정·다른 코드·Unity 실행 금지.
- lifecycle_review: 완료 시 지정된 Controller/정리/타이밍의 최초 검토만 읽기 전용. 작성/Unity 실행 금지.
- 동시 최대4명, 고유 보조3명, 재귀0. 독립 파일 반환 이후 Main이 통합한다.

## 진행
- [x] baseline / 기존 소스와 설치 API / RED
- [x] 등급별 authored 설정과 실제 완료 수명 구현
- [x] 지정 검사·회귀·두 비율 시각 확인
- [x] 최초 검토, 필요한 보완 최대2, CodeMap/REPORT/보호 검사

## 최초 검토와 보완 1/2
- 초기 reflection RED: tier 설정 부재1건, lifecycle 부재1건. Playback 독립16개는14 PASS/2 FAIL(동기 MoveNext의 종료 직전10초 경계, Ended 재진입) 확인 후 직접 원인을 수정했다.
- 통합 최초56개는53 PASS/3 FAIL. 실제 paused10초 watchdog(주사위/전투), FEEL 실제 종료, 등급 메시·기간, 기존 timing/0초/저장은 통과했다. 다음 검토 Finding3개가 추가 회귀 검사에서 실제 실패했다.
- PL-R01: CombatUI 재바인딩 후 옛 반격/옛 finally가 새 HUD를 덮는다. generation을 Impact/반격/읽기/정리에 적용한다.
- PL-R02: 공개 Playback.Cancel이 소유 popup/확정 화면을 복구하지 않는다. Cancelled도 정리/활성 상태 복구를 하며 비활성 중 Render는 생략한다.
- PL-R03: 동일 cached DiceRollUI 인스턴스의 새 binding을 이전 coroutine이 닫는다. 시작 BindingVersion 소유권을 확인하고 달라지면 이전 Render/Close를 생략한다.
- 검증 중 테스트 어셈블리의 공급사 직접 참조 컴파일 오류는 기존 reflection 패턴으로 해소했다. 그때 시작된 대기 테스트는 cancel_tests로 종료했으며, 발견0건 결과는 PASS에 포함하지 않는다. 실제56개 원문은 initial-suite.json.
- 보완 1/2 구현 후 지정 재검사에서 PL-R01~03 해결을 확인했다. 최종 판정은 아래 종료 기록을 따른다.

## 보완 2/2 / 직접 회귀 계약 갱신
- 보완1 후 Presentation 필터56/56 PASS, Aura32/32 PASS. PL-R01~03의 소스 및 세 개 대응 Passed 기록을 검토자가 확인했다.
- CombatFeedbackTests 최초14 중12 PASS/2 FAIL은 제거한 legacy summary Text의 .3초 확대만 검사하던 동일 원인이다. 새 전용 조합 배너/오라의 실제 scale 또는 phase 변화로 검증 대상을 옮겼다. 읽기 시간·실제6면·저장1회·복원·두 화면 비율 assertions는 유지한다.
- 이 테스트 계약 보정과 지정 재검사를 보완2/2로 기록한다. 추가 일반 개선은 하지 않는다. 완료 조건 미충족 시 미실행/차단을 명시한다.

## 종료 기록
- Presentation56/56, Aura32/32, Combat14/14, Boundary5/5, Workbench7/7 PASS. 총114회·중복 제거113개 지정 검사 통과, 컴파일 오류0.
- 네 단계 × 두 세로 비율 최종 PNG8개를 육안 확인했다. 시간·실제 FEEL 완료·paused10초 timeout·재바인딩 복구는 PlayMode 실행으로 검증했다.
- 프로젝트 C#102개 CodeMap 누락0·meta 누락0. 대응 색인/REPORT 동기화, 필수 Finding0, 보완2/2 종료.
- 설정 재적용 원본6개 hash 불변, 보호 파일5개 hash 불변, HEAD/branch/staging 유지. Unity 정지·MainStage·Title dirty false 확인. 상세 증거와 미실행 경계는 [REPORT](../Reports/PRESENTATION_LIFECYCLE_REPORT.md)를 따른다.
