# UiPrototypeAuthoring.cs
- 역할: Editor 전용으로 허용된 UI 원본 4개와 표시 카탈로그를 새로 만들고 FateDicePrototype.unity에 직접 연결한다. 기존 원본/매핑은 초기값으로 덮어쓰지 않는다.
- CreateAndConnect: Play 또는 dirty scene이면 중단. 기존 FateDiceConfig.Snapshot과 직접 Font를 읽고 CommonButtonView 원본 및 Node/Action/Fate 구조 variant를 생성한다. PrefabUtility/AssetDatabase/EditorSceneManager만 사용한다.
- 원본 관계: Node/Action/Fate는 CommonButtonView.prefab의 variant이며 구조가 다른 카드만 artwork/grade/effect/tags 영역을 추가한다. 콘텐츠별 복제는 없다. 모든 로컬 component 참조를 직렬화한다.
- 표시 데이터: DefaultFateDiceVisuals.asset의 Action/Event 안정 ID, Node/Fate 공개 유형, Grade border/badge, ButtonPurpose 스타일을 초기 작성한다. Grade 색은 기존 게임 presentation 설정에만 남는다.
- CreateMappingProbe: 기존 Assets/TutorialInfo/Icons/URP.png를 읽어 다른 비율의 Sprite 2개를 UiMappingProbe.asset에 저장한다. 원본 파일/Importer를 수정하지 않는다. 외부 아트 생성/구매 없음.
- 입력/출력: LoadReferences는 직접 Prefab 참조 묶음, CreateAndConnect/CreateMappingProbe는 작성한 경로 설명을 반환한다.
- 직접 사용하는 대상: 4 View, VisualCatalog/UiPrefabReferences, FateDiceConfig, PrototypeAuthoring.ScenePath. 사용하는 쪽: Unity CLI eval/MenuItem. 런타임 Player 의존성 없음.
- 검증: 실제 Scene persistent references, 공통 prefab fontStyle 편집/복원과 화면 반영, 지속 매핑 편집/복원, 각 이미지 비율 맞춤은 FateDiceGuiTests.

## 전투 배치 변경 (2026-09-08)
- 초기 생성기/공통 원본은 변경하지 않는다. CombatLayoutAuthoring이 기존 ActionCardView variant를 portrait로 배치하고 CommonButtonView 기반 CombatDie variant를 추가한다. catalog/font/image 등록과 inherited fontStyle 변경은 없다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 기존 재사용 UI를 저작한 후 KoreanUiAuthoring.Apply가 기존 원본·파생 폰트와 정확한 기본 fallback 글리프에 한국어를 적용한다. 이 파일 자체의 생성 동작은 변경하지 않았다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
