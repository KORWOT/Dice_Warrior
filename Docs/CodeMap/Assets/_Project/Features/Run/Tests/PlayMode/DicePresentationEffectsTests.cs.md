# DicePresentationEffectsTests.cs

- 역할: DICE_PRESENTATION_EFFECTS의 실제 popup 자산 연결, 여섯 주사위 한 행, 조합 이름/색상, 연출 수명과 저장 경계를 확인하는 PlayMode 시험 8개다. 신규 표시 API는 reflection으로 읽어 제품 타입 추가 전에도 시험이 컴파일되고 명시적 missing-member assertion으로 RED를 낼 수 있다.
- 핵심 동작: 실제 GameApplication.prefab 및 InGame.unity를 사용한다. 720×1280/720×1600 각각 탐험·전투를 실행하며 실제 pointer/raycast Roll 이후 6면의 한 행·비겹침·48px·safe area/row 내부, TMP 이름/색/문자 mesh/높이, 설정한 roll/hold의 유지와 종료를 검사한다. 기본 result Text의 전체 HandSummary 계약을 유지한다.
- 자산 계약: DiceRollUI.resultFeedback의 comboName(TMP), 동일 오브젝트의 TextAnimator_TMP, MMF_Player reveal, 입력 비차단 ribbon/AllIn1 shader와 모든 HandKind의 catalog.Resolve 연결을 검사한다. 공급사 코드를 수정하거나 구현하지 않는다.
- 의미/입출력: snapshot의 priority를 1000,977,954…로, label을 사용자 한글 문자열로 구성한 실제 Roll 결과를 확인한다. Controller가 전달한 hand/comboName/정규화된 comboStrength/holdSeconds와 실제 이름·catalog 색이 일치해야 한다. 출력은 NUnit assertion이며 원본 SO/프리팹을 변경하지 않는다.
- 상태/수명: 정상 종료 외에 rolling/reveal 중 CloseTopPopup, reveal 중 ShowPopup 재바인딩, 0/0초 종료를 검증한다. 닫힌 runtime popup의 첫 die에 분수 localPosition·비영 rotation·비단위 scale을 둔 뒤 실제 표시를 시작하고 같은 프레임 닫기에서 각 대상의 localPosition/localRotation/localScale/color를 완전동등 비교한다. 후속 프레임에도 효과/이전 이름이 재개하지 않아야 한다.
- 저장 경계: 실제 RunSession.Roll 복제로 독립 명령 oracle를 만들고 실제 Checkpoint delegate를 보존하며 호출 1회, 중복 입력 무효, 연출 중/후 전체 런 및 디스크 state/RNG·저장 bytes 불변을 검사한다. playedSeconds 및 lastResult.playedSeconds만 정규화한다. 기존 SeedPresentationFlowTests의 phase별 roll/reroll 및 한쪽 0 회귀는 변경하지 않는다.
- 사용하는 대상: GameApplication/RunUIController/RunSession/LocalRunStore/FixedSeedSource, DiceRollUI/DiceRollUIData/DiceFaceView/UIManager, KoreanText/HandKind, reflection으로 DiceResultFeedback/DiceFeedbackCatalog/TMP/TextAnimator/FEEL, uGUI/EventSystem/SceneManager, UnityEditor AssetDatabase/PlayModeWindow, NUnit/UnityTestTools.
- 사용하는 쪽/관계 근거: 기존 FateDice.PlayMode.Tests가 Unity Test Runner에서 발견한다. 직접 GameApplication.Bootstrap, UIManager.ShowPopup/CloseTopPopup, DiceRollUI.CompleteRoll 및 실제 포인터 경로를 실행한다. 제품 코드가 시험을 호출하지 않는다. 신규 vendor assembly를 시험 asmdef에 직접 추가하지 않는다.
- 격리/검수 주의: 고유 Temp/FateDicePresentationEffectsTests 경계 안의 저장만 생성/삭제하고 기본 사용자 저장은 존재/bytes 전후 비교만 한다. 자기 app만 파괴하며 원본 자산·설정·Scene/Prefab·meta를 쓰지 않는다. 모든 조건 대기는 최대 10초다. Android 실기기/사람 클릭 증거를 대신하지 않는다.
- 검증 상태: 보조 에이전트의 시험/요약 작성 및 정적 검토만 수행. Unity 컴파일·RED·GREEN은 NOT_RUN이며 메인의 직렬 실행 및 DICE_PRESENTATION_EFFECTS_REPORT에 따른다. 수정/재검증 횟수는 Task의 공통 예산을 따른다.

- 최초 검증 후 DPE-R01 회귀를 추가하여 총 9개. 긴 hold 중 등장 종료 후 mesh/scale 정착과 이름/색 유지 검사. mesh는 TMP.mesh를 reflection으로 읽으며 소유 mesh를 파괴하지 않는다.


## 등급별 연출 수명 (2026-09-10)

총 종료시간 기대값을 tier.Duration(phase별 최소 hold)로 바꾸고 Playback.Completed를 함께 검사한다. 긴 읽기는 고정 .45초 대신 Reading 진입 후 정착 메시를 비교한다. 저장1회/포인터/두 비율/0초/취소 원래 assertions 유지.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

굴림과 result effect가 끝난 뒤 탐험은 FateChoiceUI 1개, 전투는 popup 없음으로 구분한다. 0/0 탐험도 FateChoiceUI를 즉시 표시하되 DiceResultFeedback은 재생하지 않는다. 기존 FEEL/TMP 실수명·정확한 transform 복원·single checkpoint·RNG/bytes·6dice 행 oracle는 유지한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.
