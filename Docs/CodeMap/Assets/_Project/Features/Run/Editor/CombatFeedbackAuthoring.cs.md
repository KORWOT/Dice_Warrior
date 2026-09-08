# CombatFeedbackAuthoring.cs

- 원본: Assets/_Project/Features/Run/Editor/CombatFeedbackAuthoring.cs. Editor 전용 정적 도구이며 런타임 hook/자동 실행은 없다.
- 역할/API: Apply()는 로비의 정확한 기본 와일드 카드 문구, 전투 피드백 자식과 참조, 주사위 결과 표시 공간을 기존 프리팹에 작성하고 변경한 프리팹 수를 반환한다. 메뉴 진입점은 Fate Dice/전투 피드백 원본 적용이다.
- 허용 출력: MenuPath는 Run/Prefabs/MenuUI.prefab, CombatPath는 Run/Prefabs/CombatUI.prefab, DicePath는 Run/Prefabs/DiceRollUI.prefab이다. 다른 프리팹·씬·config·게임 상태·저장·글꼴·이미지를 작성하지 않는다.
- 가드/실패: Play 및 전환/컴파일/import 중이거나 Prefab/Preview Stage, dirty 제작 씬이면 거절한다. 기존 세 프리팹과 필요한 typed 참조·한글 font가 있어야 한다. contents 로드는 항상 finally에서 Unload하며, 저장 실패를 예외로 보고한다. broad SaveAssets를 사용하지 않는다.
- 로비: 정확히 일치하는 기본 시련 카드/TRIAL WILDCARD와 두 기존 도움말 문장만 교체한다. 참조와 사용자 지정 문구는 유지한다. 일치하는 변경이 없으면 저장하지 않는다.
- 전투: CombatUI.feedbackVersion이 1 미만일 때만 기존 arena 아래 비입력 hitFlash Image, actionFeedback/damageFeedback Text와 어두운 외곽선을 추가한다. 기본 actionFeedbackSeconds=.38, shakePixels=9, feedbackHoldSeconds=.25를 설정한다. flash는 alpha0이며 피드백 Text 뒤에 있다. 예시 문구는 Prefab 편집에서 위치를 보여주고 실제 View Bind가 지운다. 공격은 무대 중앙 상단, 피해는 우측 중앙에 두어 우상단 적 예고와 좌하단 플레이어 상태를 피한다.
- 조합 레이아웃: 기존 전투 hand 영역을 폭65%/18pt/48높이로 늘리고 운명력과 재굴림을 남은 영역에 배치한다. 주사위 row의 상단을52, 전체 영역 높이를154로 바꾸어 두 줄 hand와 여섯 주사위를 분리한다. 카드·HP·다른 화면 배치는 재생성하지 않는다.
- 주사위: DiceRollUI.feedbackVersion이 1 미만일 때 결과 Text를 중심y=-205/550×90/24pt/wrap으로 설정하고 resultHoldSeconds=.9를 작성한다. 기존 여섯 dice/rollButton 참조와 위치·색상·규칙을 보존한다. 결과 하단-250은 기존 굴리기 버튼 상단-259보다 위에 있다.
- 수명/재실행: CombatUI/DiceRollUI의 feedbackVersion=1을 저장한 뒤 후속 Apply는 해당 화면의 사용자 편집과 원본 bytes를 보존하며 저장하지 않는다. 기존 .meta/GUID를 유지하는 PrefabUtility 저장만 한다.
- 직접 관계: CombatUI/DiceRollUI의 공개 피드백 필드 및 MenuUI의 Text 자식, RunScreenLayout, UnityEditor PrefabUtility/AssetDatabase/StageUtility와 uGUI를 사용한다. 수동 메뉴 또는 메인 Unity CLI eval이 호출한다. 게임 규칙·RunSession·LocalRunStore 의존이 없다.
- 검수: 실제 적용·재실행 bytes 불변·720×1280/1600 레이아웃·직접 클릭 실행과 완료 판정은 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다. staging 파일 작성 자체는 Unity 실행 통과를 의미하지 않는다.
