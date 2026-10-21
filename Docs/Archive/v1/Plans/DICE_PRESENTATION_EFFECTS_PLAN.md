# Dice Presentation Effects Implementation Plan

상태: IMPLEMENT / 구현·67개 지정 검사 완료. 마지막 정지 preview 육안 확인은 Esc로 중단했으며 새 DICE_COMBO_AURA 요청에서 확장 결과를 검증한다. 증거는 대응 REPORT에 정리했다.

## 승인된 방향과 고정 계약
- 앞선 대화의 조합 이름/대표 색/짧은 등장/결과 유지/FEEL+Text Animator+All In 1 Sprite Shader 역할 분담을 구현한다.
- 탐험/전투 주사위는 6개를 한 행으로 표시한다. 모바일 세로 720x1280 및 720x1600에서 경계/가독성을 확인한다.
- 규칙/RNG/조합 판정/카드 등급/저장 순서를 바꾸지 않는다. 기존 조합 label 및 priority를 표시 근거로 삼으며 카드 등급과 조합을 동일시하지 않는다.
- 기존 명령/체크포인트 1회 확정 후 연출을 재생한다. 연출 중 중복 입력 차단, 종료/닫기/재바인딩 시 원래 transform/color를 복원한다.
- 기존 탐험/전투별 roll/hold 설정과 명시적 0초 계약을 보존한다. 실제 연출 연결/한글 폰트/에디터 작성 프리팹을 검증한다.
- 기준 commit d8df4999174ad63bb73e0727ff0149cb55624db2. 기존 Packages lock 및 Plugins/Samples/TMP/설치 복구 문서 등 사용자 변경을 보존한다. 조사 후 정확한 변경 파일/자산과 baseline을 고정하고 구현한다.
- 실행 중인 이 프로젝트를 사용한다. 추가 에디터/작업 사본을 열지 않으며 씬/프리팹은 파일 단위 허용을 확정한 뒤 Editor API로만 작성한다.
- 공급사 C#/DLL 수정, 게임 규칙 변경, 이미지 생성/구매, 기존 자산 일괄 이동, 커밋은 제외한다.

## 위임과 쓰기 경계
- 메인: 현재 UI/컨트롤러/작성 도구/설정 조사, 공통 계약/PLAN/REPORT/INDEX, 실제 프로젝트 변경 및 모든 Unity 실행.
- 보조 effect_asset_api: Text Animator/All In 1/FEEL 현재 API와 로컬 FEEL 캐시 조사. 처음에는 읽기 전용, 산출물은 API/설치/수명 주의점 보고. Unity 실행/설치/공급사 수정 금지.
- 보조 effect_tests: 기존 주사위/피드백 테스트와 회귀 계약 조사. 처음에는 읽기 전용, 산출물은 최소 의미 있는 검증안. 구현 위임 시 별도로 지정된 테스트 파일/1:1 요약만 쓴다.
- 동시 최대 3명(메인 포함), 고유 위임 최대 2명, 재귀 0. 동일 파일 동시 작성 금지. 리뷰/검증은 한 묶음으로 통합하며 초기 검증 후 수정/재검증 최대 2회.

## 진행
- [x] 현재 계약/API/저장 보호/레이아웃/검증 지점 확인 및 구현 파일 고정.
- [x] 최소 회귀 검사 실패 확인, 에셋 임포트/표시 구현/프리팹 작성.
- [x] Unity 컴파일/지정 회귀/두 세로비/실제 버튼 입력과 플레이 연출 확인. 최종 정지 preview 육안 확인의 중단은 REPORT에 별도 표시.
- [x] 코드 1:1 요약/직접 관계/INDEX/REPORT, 범위/기존 변경 보존 확인.

## 구현 파일과 자산 허용
- 메인 기존 C#: Run/Presentation/DiceRollUI.cs, RunUIData.cs, RunUIController.cs, FateDiceWidgets.cs, CombatUI.cs; Run/Editor/UIWorkbenchPreview.cs. 실제 필요한 직접 연출 연결만 수정한다.
- 메인 신규 C#: Run/Configs/DiceFeedbackCatalog.cs, Run/Presentation/DiceResultFeedback.cs, Run/Presentation/SelectionFeedback.cs, Run/Editor/DicePresentationAuthoring.cs. 필요 없는 선택 연출 파일은 만들지 않는다.
- Runtime/Editor/PlayMode.Tests asmdef에는 TMP/Text Animator/MoreMountains.Tools의 실제 필요한 참조만 추가한다. Core asmdef는 불변.
- 정확한 프로젝트 자산 허용: Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab, CombatUI.prefab; Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab; Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab; 신규 Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset, Assets/_Project/Features/Run/Materials/ComboRibbon.mat, Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Effects SDF.asset 및 신규 .meta. 기존 파일의 GUID와 사용자 작성 값은 버전 migration으로 보존한다.
- 외부 에셋: 로컬 FEEL 6.1 캐시에서 원본 바이트 그대로 선별한 core 517개 항목을 Unity ImportPackage로 신규 Assets/Feel 아래 설치한다. 정확한 경로/GUID/원본·선별본 hash는 artifacts/dice-presentation-effects/feel-import-manifest.json에 고정. 선별 prefix의 framework 기본 리소스도 함께 들어왔다. 실제 import 후 검사에서 pathname 후행 메타데이터를 바로잡아 기본 SO 6개/예제 Scene 6개/Prefab 2개를 확인했다. 정확한 파일은 feel-verify.json.serialized_assets. 제작 씬/build 목록에는 연결하지 않는다. 전체 demos/NiceVibrations 코드는 제외하고 해당 license/readme만 포함. 공급사 코드 수정 없음. 컴파일로 실제 의존성을 검증한다.
- 보조 테스트 작성: Run/Tests/PlayMode/DicePresentationEffectsTests.cs 및 대응 CodeMap만. 기존 시간/저장/의미 검사는 유지한다.
- 모든 프로젝트 C# 변경의 대응 1:1 CodeMap과 직접 관계/INDEX는 메인이 최종 통합한다.

## 상세 표시 계약
- DiceRollUI.result는 기존 Text와 전체 HandSummary를 유지하고 새 resultFeedback에 큰 TMP 조합 이름을 별도 연결한다.
- DiceRollUIData에 comboName, hand, comboStrength, holdSeconds를 추가한다. 이름은 현재 저장된 규칙의 label을 KoreanText.Content로 변환하고 강도는 현재 priority 순위로 산출한다. 규칙을 재판정하지 않는다.
- DiceResultFeedback의 comboName/TMP, textAnimator, reveal/MMF_Player, ribbon/Image, catalog는 작성된 참조다. catalog.Resolve(HandKind)는 hand/color/textEffect/punchScale을 갖는 DiceHandFeedbackStyle을 반환한다. 색은 조합 ID 기준이며 카드 Grade에 의존하지 않는다.
- FEEL은 배너 전체 pulse/alpha 등, Text Animator는 글자 내부 변형, All In 1은 배너 Image 재질에 사용한다. TMP 재질을 Sprite shader로 대체하지 않는다. 동일 속성에 두 연출 엔진을 겹쳐 쓰지 않는다.
- 0초 hold에서는 벤더 애니메이션을 시작하지 않고 즉시 정착한다. 정상 종료/중단 시 FEEL StopFeedbacks+RestoreInitialValues, 텍스트 효과 정지, 주사위의 원래 회전/위치/크기/색을 복원한다.
- UI/자산 확인을 위한 Unity autotick은 현재 세션 값만 보존/복원하며 프로젝트/전역 hook 설정은 변경하지 않는다.
- 사용자 추가 승인: 현재 미저장 Assets/_Project/Scenes/Title.unity 변경을 그대로 저장해도 된다고 응답했다. 해당 씬에는 사용자가 이미 만든 변경만 SaveScene으로 보존하고 본 기능의 요소를 추가하지 않는다. 저장 후 해시를 보호 기준으로 삼는다.

## 검증 묶음
- 새 자산/한 줄 배치/주사위와 표시 수명 검사의 실제 RED를 먼저 확인한다.
- 새 DicePresentationEffectsTests, 기존 SeedPresentationFlowTests/CombatFeedbackTests/RunBoundaryFlowTests, CoreReplayTests를 직렬 실행한다. 기존 사용자 저장 hash와 schema/규칙 원본은 보존한다.
- Unity 내 실제 버튼/Pointer 경로로 굴림·결과·카드 선택·전투를 확인하고 두 세로비 캡처로 주사위 6개와 한글 조합 배치를 검수한다.

## 초기 검증 및 보완 1/2
- 신규 8개 실제 PlayMode 검사 최초 PASS. 컴파일 전 테스트 API 오류와 필터 0개 실행은 검증으로 계산하지 않음.
- DPE-R01: 글자 모션을 짧은 등장 시간에 정착시키고 긴 hold 동안 색/이름 유지. 긴 hold 회귀 추가.
- DPE-R02: 전역 SaveAssets를 catalog/font/material별 SaveAssetIfDirty로 교체. 작성 도구의 나머지 사용자 자산 자동 저장 방지.
- 두 Finding은 같은 최초 리뷰 묶음, 함께 1차 보완. 지정 기존 회귀/시각 검증은 계속 진행 중.

## 직접 회귀 보완 2/2
- DPE-R03: 정지 Dice preview의 inactive Bind에서 Text Animator.SetText가 TMP의 아직 초기화되지 않은 canvas를 참조했다. 실제 OpenDice 예외 stack으로 확인. 편집 모드의 정지 표시는 TMP.text만 설정하여 정상 Canvas 활성화 때 메시를 생성한다. 런타임 연출 경로는 유지한다.
- 직접 회귀 검사 허용: Assets/_Project/Features/Run/Tests/EditMode/PlayWorkbenchTests.cs 및 대응 CodeMap에 Dice preview의 실제 메시/6개 한 행/정리/원본 보존 검사를 추가한다. 기존 검증 조건은 유지한다.
- 보완 후 신규 presentation 9개와 PlayWorkbenchTests를 실행한다. 규칙/저장/전투 경로는 수정하지 않았으므로 이미 통과한 지정 51개 증거를 유지한다.
- artifacts/dice-presentation-effects/PresentationProbe.cs는 실제 버튼 입력 뒤 제한된 캡처/상태 기록만 하는 검증 도구이며 대응 CodeMap을 관리한다.
