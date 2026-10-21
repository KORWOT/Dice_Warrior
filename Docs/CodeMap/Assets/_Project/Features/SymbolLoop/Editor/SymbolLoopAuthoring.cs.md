# SymbolLoopAuthoring.cs

- 역할: 허용된 Screen 프리팹과 Title 연결을 Editor API로 작성.
- 핵심 동작: 720×1280 frame, 사각 12칸/말, 6면 주사위 4개(숫자1+기호3), 전투 캐릭터/체력/효과/상태, 8개 버튼을 로컬 직렬화 참조로 구성한다.
- 입출력: Build → SymbolLoopScreen.prefab, Title.unity, 동일 Title 단일 빌드 목록. Config는 없을 때만 생성하고 기존 수치 보존.
- 사용하는 대상: AssetDatabase/PrefabUtility/EditorSceneManager, Screen/App/Playback/Glyph/Die, 기존 UIRoot/CommonButtonView/Pretendard TTF, 내장 UI bar sprite.
- 사용하는 쪽/관계 근거: Dice Warrior 메뉴 및 명시 CLI eval. 런타임 호출 없음.
- 상태/수명: Play 또는 dirty Scene이면 거절. 임시 생성 객체를 prefab 저장 후 제거. 기존 Scene/Prefab GUID 유지.
- 검수: PLAN에 허용된 자산만 작성, SaveAssets 전체 저장 금지, 폰트·공통 프리팹 수정 금지. 전투/보드 겹침 및 실제 화면 비율 검증 필요.

- 칸 0은 좌하단, 1은 왼쪽 위 방향으로 이어져 시계 방향 12칸 경로를 이룬다.
