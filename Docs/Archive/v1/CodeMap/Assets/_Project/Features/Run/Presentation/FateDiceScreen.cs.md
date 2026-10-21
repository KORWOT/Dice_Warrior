# FateDiceScreen.cs
- 역할: 기존 Scene의 script GUID와 외부 통합 진입점을 보존하는 RunUIController 파생 컴포넌트다.
- 입출력/관계: Scene의 기존 config/uiFont/visuals/uiPrefabs와 새 uiRootPrefab은 상속된 serialized 필드다. FateDiceGuiTests와 Editor authoring은 이 컴포넌트를 찾고, 실제 동작은 RunUIController로 위임된다.
- 상태/수명: 자체 상태·계산·뷰 생성기 없음. 상속 Awake/Update/종료 훅을 사용한다.
- 검수: GUID/meta 유지와 기존 Scene 데이터 유지, 기존 외부 Session/Store/Seed/Widgets API 및 실제 GUI 회귀를 확인한다.
- Scene 구조 이후: 이 facade와 FateDicePrototype은 독립 회귀/개발 fixture로 보존된다. 제작용 세 씬은 GameApplication prefab의 RunUIController를 사용하며 SceneStructureAuthoring은 이 기존 Scene에서 직렬화 자산 참조만 읽는다.
