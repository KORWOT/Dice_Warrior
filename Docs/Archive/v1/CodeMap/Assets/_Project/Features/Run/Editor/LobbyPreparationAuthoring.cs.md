# LobbyPreparationAuthoring.cs

- COMBAT_FEEDBACK 변경: 최초 로비 작성의 기본 heading과 캐릭터 도움말을 와일드 카드로 정정한다. 기존 version=1 원본을 재구성하지 않으며, 이미 작성된 원본의 정확한 기본 문구 갱신은 별도 CombatFeedbackAuthoring이 담당한다. trialHeading/choices와 설정 ID 계약은 그대로다. 실행 증거는 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다.

- 역할: 기존 MenuUI.prefab 하나를 준비 로비 레이아웃으로 갱신하는 Editor 전용 도구다. MenuPath는 Assets/_Project/Features/Run/Prefabs/MenuUI.prefab, 공개 Apply()의 반환형은 string이다. 메뉴 진입점은 Fate Dice/로비 준비 화면 적용이다.
- 가드: Play/전환/컴파일/import 중 실행을 거절한다. Prefab 또는 preview Stage를 닫고 저장된 clean 제작 씬에서 실행해야 한다. 기존 MenuUI/layout/header의 font와 root view/layout/CanvasGroup이 필요하다.
- 작성 순서: layoutVersion이 1 이상이면 자산 쓰기 없이 반환한다. PrefabUtility.LoadPrefabContents로 격리 편집하고 기존 root의 MenuUI/RunScreenLayout/CanvasGroup을 유지하며 자식 배치를 재작성한다. 필수 layout 연결을 검사한 뒤 version=1로 저장하고 finally에서 UnloadPrefabContents한다. 다른 프리팹이나 제작 씬을 저장하지 않는다.
- 상단/중앙: 고정 Header 84, 탭 68, 가변 높이 ScrollRect 한 개를 만든다. characterPanel은 기존 방랑자 이름/선택 표시/출발 정보와 단순 UI 도형 장식, settingsPanel은 시련/등급 행과 영향 범위 안내, growthPanel은 런 성장 설명과 영구 성장 미제공 안내를 담는다. 캐릭터만 초기 활성이다.
- 하단/글꼴: 탭 scroll 밖의 footer에 mainChoices, 오류, 최근 결과, notice를 둬 새 여정·이어하기가 내용 스크롤에 밀리지 않게 한다. 긴 설명은 Text의 preferredHeight와 VerticalLayoutGroup/ContentSizeFitter가 확장하고 중앙에서 스크롤한다. 모든 새 Text는 기존 MenuUI의 한글 font 참조를 사용하며 새 폰트/이미지/캐릭터 데이터를 만들지 않는다.
- 호환: 기존 RunScreenLayout 필수 필드와 seedHeading/InputField는 비활성 compatibility 자식에 연결한다. 일반 로비의 주사위 HUD/시드 입력을 보이지 않게 하면서 기존 직렬화 필드와 screen 공통 검증을 유지한다. 시련·등급 HorizontalLayoutGroup의 고정 행 높이는 기존 CommonButtonView 최소 높이보다 크다.
- 원본 편집: 카드/탭/하단 영역은 런타임 임시 scaffold가 아니라 실제 MenuUI 원본의 자식이다. 색상·문구·앵커·높이를 Editor에서 편집할 수 있다. 탭의 초기 색상은 MenuUI의 serialized palette를 읽고 이후에도 동일 필드로 바인딩한다. 도형 장식은 uGUI Image로만 구성하고 새 아트/아이콘 등록은 없다.
- 관계: MenuUI의 추가 필드와 기존 RunScreenLayout을 직접 연결한다. UnityEditor PrefabUtility/StageUtility/EditorSceneManager 및 uGUI만 사용하고 config/catalog/RunSession/LocalRunStore/GameApplication을 변경하거나 bootstrap하지 않는다. 실제 적용·GUID/자산 경계 검증은 메인 Unity 직렬 실행 책임이다.
- 검수 상태: 실제 통합 실행 결과는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
