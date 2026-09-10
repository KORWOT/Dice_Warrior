# CombatFeedbackTests.cs

## RA-A 현재 계약 (2026-09-09)

2026-09-10 연출 수명 확장: 결과 pulse 확인 대상은 legacy summary Text의 고정 .3초 확대에서 실제 DiceResultFeedback 배너 scale/참여 오라 phase 변화로 이동했다. 두 세로 비율에서 가시적인 모션이 반드시 있어야 하며, 읽기 시간·여섯 면·저장1회·원래 scale 복원 검사는 유지한다. 실제 실행 상태는 PRESENTATION_LIFECYCLE_REPORT.

AuthoredFeedback 검사는 실제 DefaultFateDice SO의 두 phase 결과 유지 설정이 .8초 이상인지 요구한다. 기존 prefab의 미사용 legacy 필드 반사 검사를 실제 runtime 설정 경계로 이동했다. 나머지 면/텍스트/피드백/저장/중단 assertion 유지. 고정 Combat fixture도 두 RollPresentationSettings를 명시한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 역할: COMBAT_FEEDBACK의 실제 원본·표시·입력/저장 경계를 검사하는 PlayMode 테스트 14개.
- 최소 RED: 실제 CombatUI/DiceRollUI 프리팹을 읽고 public actionFeedback/damageFeedback Text, hitFlash Image 연결 및 읽을 수 있는 결과 유지 시간을 검사한다. reflection을 사용해 새 필드가 없는 기존 코드도 컴파일한 뒤 명확한 assertion으로 실패한다.
- 입력/출력: 실제 GameApplication prefab/config snapshot과 InGame Scene, 고유 Temp LocalRunStore를 Bootstrap 전에 주입한다. 소유 app을 폐기하고 자기 임시 폴더만 삭제한다. 사용자 기본 저장의 존재/bytes를 앞뒤 비교한다. 원본 SO/프리팹/씬·기본 저장에는 쓰지 않는다.
- 조합: 역순·희소 priority와 사용자 혼합 label로 8/10 단계 및 state의 운명력7을 요구한다. 단일/여러줄 HandSummary 호출 전후 JSON이 같아야 한다.
- 굴림: 720×1280/1600의 실제 raycast/pointer로 굴린 직후 RunSession.Roll oracle와 state/디스크를 비교한다. Checkpoint는 실제 저장 delegate를 보존한 계수기로 1회를 확인한다. 중복 callback은 추가 저장을 만들지 않으며 여섯 실제 결과/label/단계/운명력을 읽는 동안 Busy 유지, 결과 pulse 및 종료 시 transform 복원을 요구한다. 종료 후 실제 세 카드 raycast를 확인한다.
- 선택/수치: clone config의 위력10·방어6, 행동 계수1/.5, grade비1, 적 위력12/방어7로 손계산 가능한 fixture를 만든다. HP30/적20/수호2/적수호4에서 공격10·수호획득3·적 HP감소6·내 HP감소7이다. 완전방어(양쪽 HP감소0), 적방어(수호7), 처치(남은 HP3만 감소/반격·후속 의도 RNG 없음), 패배(남은 HP4만 감소)로 실제 수치와 단계를 구별한다.
- 선택 수명: 명령/디스크는 클릭 시 먼저 1회 확정하고 기존 세 카드·HP가 선택 pulse 동안 남는다. 선택 카드만 확대되고 복원된 뒤 IsFeedbackPlaying이 시작한다. 공격→적 반응 중 실제 HP Text/fill과 상세 피해/막힘/수호를 관측하고, 최종 Reward/Result/Combat Render는 Busy 종료 시점까지 기다려야 한다. arena 흔들림/flash와 메뉴 위치 고정·완료 시 원래 위치/회전/color 복원을 검사한다. 1600 높이에도 같은 전체 흐름을 실행한다.
- 실패/중단: 의도된 Checkpoint IOException Warning 하나만 LogAssert.Expect한다. 실패 후 HP/cards/state/저장 bytes 유지와 성공 연출 부재를 검사한다. 실제 흔들림 도중 CloseAll은 피드백/transform/color를 정리하며 app 폐기/저장 재개 후 이미 확정한 명령을 재실행하지 않아야 한다.
- 표시: 실제 활성 Text의 safe area·preferredHeight·렌더링 mesh와 raycastTarget=false를 확인한다. 줄 잘림이나 빈 mesh를 무시하지 않는다.
- oracle/상태: 실제 RunSession 명령의 전체 JSON을 UI state와 LocalRunStore.Load 양쪽에서 대조한다. playedSeconds 및 lastResult.playedSeconds만 정규화한다. 연출 중/후 저장 bytes도 동일해야 한다. 모든 대기는 최대10초이며 예상 외 로그를 무시하지 않는다.
- 직접 관계: GameApplication/SceneFlowController/RunUIController, CombatUI/DiceRollUI/DiceFaceView/ActionCardView, KoreanText, RunSession/LocalRunStore, UIManager/UiRoot, UnityEditor.AssetDatabase/PlayModeWindow, uGUI/EventSystem/NUnit/UnityTestTools. Main이 D 프로젝트 통합 및 Unity 실행을 직렬 소유한다.
- CFB-B01(수정 묶음1): 초기12개 실행은 11 PASS/1 FAIL이었다. 1600 높이의 닫힌 주사위 좌표를 Vector3 정확 동등으로 비교해 expected/actual이 둘 다 (-174.00,-93.00,0.00)인 상태에서 실패했다. baseline은 실제 Canvas 정리 후이며 비교는 rolling 재바인딩·CloseToCache 재부모화 후다. 720×1600의 비정수 Canvas 배율 아래 RectTransform 재계산의 비트 일치 대신 0.001 이하 거리와 G9 진단을 요구한다. 닫힌 뒤 기존 검사에 더해 결과가 보이는 동안에도 여섯 좌표가 복원되어야 한다. 값·hold·저장·회전·scale 검사는 유지한다.
- CF-A01(수정 묶음1): 굴림 중 및 IsRolling=false인 결과 유지 중 두 시점에 Controller.enabled=false→true를 실행한다. 실제 Roll 직후 이미 확정한 RunSession/디스크 bytes를 대조하며, 재활성화 후 Busy=false/남은 popup 없음/CombatCards와 실제 제시 ID·raycast/상호작용 가능/Checkpoint1회를 요구한다. 재활성화가 RNG·상태·디스크를 변경하거나 이전 popup으로 카드를 가리는 회귀를 검사한다.
- 검수 상태: 초기 authored 검사 실제 RED 1/1 실패(Missing authored feedback reference actionFeedback), 초기 확장12개 11 PASS/1 FAIL을 Main이 확인했다. 동일 작업 최초 검토 묶음의 CFB-B01/CF-A01만 수정 묶음1에 반영해 14개 통합 후 최종 전체 PlayMode 77/77 PASS(신규 14개 포함, 182.88초), EditMode 128/128 PASS(12.67초)를 확인했다. 실제 증거는 Docs/Reports/COMBAT_FEEDBACK_REPORT.md 및 artifacts/combat-feedback/play-final.json, edit-final.json에 있다. Editor 포인터 검사는 Android 기기/사용자 직접 클릭 증거를 대체하지 않는다.

## RA-B 상태 소유권 호환 (2026-09-09)
CombatSave는 HP/적HP/양쪽 보호막을 독립 ReadSnapshot에 설정한 뒤 new RunSession으로 구성한다. State 표시 복사에 직접 쓰지 않는다. 실제 피해량/피드백/시각/레이캐스트 oracle는 유지하며 RA-B-01 회귀와 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.

