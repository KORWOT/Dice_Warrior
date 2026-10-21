# 주사위 조합 오라 연출 PLAN

상태: IMPLEMENT 완료 / 2026-09-10 참고 이미지 3장에 따른 기존 결과 연출 확장. 지정 검사 62/62 PASS, 두 화면 비율 최종 6캡처 확인. [REPORT](../Reports/DICE_COMBO_AURA_REPORT.md)

## 승인 범위와 AC
- 사용자 요청: 한 줄의 여섯 주사위 주위에 색상별 이펙트, 조합별 빛나는 연출. 기존 결과창/색상 연출의 확장이며 새 게임 규칙은 없다.
- 결과 이름을 주사위 위에 크게 배치하고, 조합 참여 주사위만 링·빛·반짝임을 표시한다. 중앙 문양/빛줄기는 강도에 따라 달라지되 눈금과 글자를 가리지 않는다.
- 투 페어 파랑, 트리플 페어 금색+보라, 식스카인드 적색+금색을 참고한다. 나머지 조합도 고유 색을 가진다. 강도는 저장 규칙 priority 기반 comboStrength를 사용한다.
- 주사위 순서/값/최종 hand/게임 RNG/저장/확정 순서/roll 및 hold 0초 계약을 유지한다. 참고 이미지의 등급 숫자·확인·보상 버튼은 게임 기능으로 도입하지 않는다.
- 720×1280 및 720×1600, 정지 미리보기, 재바인딩/닫기, 조합 기여 주사위의 정확성, 눈금/이름의 실제 메시를 검증한다.
- 벡터 UI 메시로 링·광선·반짝임을 작성하여 해상도 독립/Canvas 정렬을 유지한다. 기존 FEEL/TMP/Text Animator/All In 1 배너를 재사용하고 외부 이미지 생성/패키지 추가는 하지 않는다.

## 기준/보호/허용 파일
- HEAD d8df4999174ad63bb73e0727ff0149cb55624db2, 기존 DICE_PRESENTATION_EFFECTS 미커밋 변경과 사용자 Plugins/Samples/Packages/Title 변경을 보존하고 그 위에 명시 확장한다. baseline.json에 현재 status/hash를 기록한다.
- 코드: Run/Presentation/DiceResultFeedback.cs, Run/Configs/DiceFeedbackCatalog.cs, Run/Editor/DicePresentationAuthoring.cs; 신규 Run/Presentation/DiceAuraGraphic.cs, DiceComboHighlights.cs; 신규 Run/Tests/PlayMode/DiceAuraTests.cs. 필요 시 기존 DicePresentationEffectsTests의 직접 표시 계약만 확장한다.
- 자산 쓰기: Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab, Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset. 신규 컴포넌트 .meta 및 코드 1:1 CodeMap, INDEX, 이 PLAN/REPORT. 해당 자산은 live Editor API로만 수정하고 version 2 migration으로 사용자 값을 보존한다.
- Scene/카드 prefab/공급사 파일/게임 규칙/manifest/lock 쓰기·커밋은 제외한다. 필요 검증 도구는 artifacts/dice-combo-aura/에 두고 C#이면 대응 CodeMap을 작성한다.
- 이전 DICE_PRESENTATION_EFFECTS의 2/2 보완 이력은 유지하며 여기서 초기화하지 않는다. 이번 새 오라/참여 표시 계약에 한해 최초 통합 검토와 최대 2회 보완. 이전 결과 문서 미완료도 실제 증거만 정리한다.

## 위임
- 메인: 전체 통합/PLAN/REPORT/색인, UI 메시/수명/설정/Prefab 작성, 모든 Unity 실행 직렬 담당.
- effect_tests: DiceComboHighlights.cs와 DiceAuraTests.cs 및 각 CodeMap만 작성. 반환 계약 Groups(HandKind,int[6]) -> int[6], 비참여 -1, 참여 그룹 0 이상, 입력/RNG 불변. 확정 hand에 필요한 최소 개수만 표시. 중복 후보는 숫자 오름차순·입력 index 순으로 결정. straight는 가능한 1..5 우선. 실제 테스트의 독립 기대값으로 검증.
- effect_asset_api: 기존 연출/셰이더/새 메시의 직접 수명·성능·겹침 검토만 읽기 전용. 공유 코드 수정/Unity 실행 금지.
- 동시 최대3명, 고유 보조2명, 재귀0. 같은 파일 동시 작성 금지.

## 진행/검증
- [x] 기존 상태/참여 판정 계약과 실제 RED
- [x] 오라 메시/결과 레이아웃/색상 자산 작성
- [x] 컴파일 및 새 검사, 기존 presentation/미리보기/규칙 회귀
- [x] 두 세로 비율의 결과 캡처/편집 상태/저장 보호/CodeMap/REPORT

## 최초 검사 / 보완 1
- Groups 실제 RED1건 확인 뒤 구현. 초기31검사는 순수29 PASS, UI2 FAIL이며 같은 CanvasRenderer 누락 원인(DCA-R01). RequireComponent와 이미 작성된 version2 원본의 누락 컴포넌트만 복구하는 Editor 경로를 추가한다.
- DCA-R02: 별의 흰 중심색 보간이 opacity까지 올리므로 RGB 보간 후 원래 alpha를 유지한다. 지정 메시/수명 검사와 실제 캡처로 재검증한다.
- DCA-R03: 초기 색 migration이 RGB만 비교해 사용자 alpha 편집을 덮을 수 있었다. RGBA 전체를 비교하고 독립 색 fixture로 확인한다.
- 최초 시각 캡처의 갈색 사각 패널이 참고 오라를 가렸다. 정해진 popup 원본만 투명 패널/밝은 주사위 면/넓은 색 번짐으로 보완한다. version3는 초기 version2 오라를 중복 생성하지 않고 이 외형과 RequireComponent의 실제 디스크 직렬화를 한 번 저장한다.
- 동일 최초 검토 묶음, 최초 보완 1/2 진행. 기존 Task 보완2/2와 별개인 새 오라 계약 범위다.

## 시각 보완 2
- 보완1 후 신규32검사 PASS, 두 세로 비율 3조합의 실제 prefab 캡처 확인. DCA-R01~03 소스 재검토도 해결 조건 충족.
- DCA-R04: 중앙 문양의 아래 별빛이 보조 요약과 가까우며, 1280 화면의 뒤쪽 플레이어 상태가 이름 배경에 겹친다. 아래 별빛 크기를 줄이고 요약을 26px 내려 분리, 배경 dim을 강화한다. version4에서 정확한 DiceRollUI만 한 번 저장한다.
- 이번 2/2 보완 후 새32/기존presentation9/Editorpreview7/CoreReplay14와 최종 6캡처를 확인한다. 미해결 필수 결함이 있으면 완료로 처리하지 않는다.

## 종료
- 위 지정 검사 62개 모두 PASS. 컴파일 오류 0. 720×1280/720×1600 각각 투 페어·트리플 페어·식스 카드의 실제 원본 프리팹을 PreviewScene에서 렌더링하고 확인했다. 고정 결과를 넣은 시각 fixture이며 무작위 플레이 캡처로 주장하지 않는다.
- 최종 자산 migration은 presentationVersion4/auraVersion1이다. 반복 적용 시 두 자산 hash가 유지된다. 최초 version2 계획 이후 보완 이력은 위에 보존했다.
- 보완 2/2 사용 완료, DCA-R01~04 해결, 남은 필수 Finding 없음. Workbench 결과 파일 회수 실패로 동일 fixture를 한 번 재실행했고 최종 7 PASS 원문을 보존했다. 새 수정/검토 예산을 부여한 것이 아니다.
- manifest/lock/Title/DefaultFateDice/사용자 저장/HEAD의 baseline 대비 hash 불변, staged 변경 없음. 프로젝트 소유 C# 99개 CodeMap 누락 0, 신규 .meta 확인.
- 이번 작업에서 native Computer Use 클릭과 모바일 실기기 빌드·성능 측정은 실행하지 않았다. 실제 UI 입력 경로는 지정 자동 PlayMode 검사로 확인했다.
