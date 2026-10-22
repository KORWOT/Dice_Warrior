# CompetitionAuthoring.cs

- 역할: 승인된 새 CompetitionScreen 프리팹과 기존 Title 연결을 Editor API로 작성한다.
- 핵심 동작: 고정12칸/네 말/참가자4행/주사위/전투 무대/버튼8개를 authored 참조로 만든다. 기존 Title을 열어 기존 SymbolLoopApp/UIManager를 유지하고 CompetitionApp 참조 및 새 화면 등록만 추가한다.
- 입출력: Build → Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab, Assets/_Project/Scenes/Title.unity. ContentPath의 JSON과 기존 SymbolLoopConfig를 참조한다.
- 사용하는 대상: AssetDatabase/PrefabUtility/EditorSceneManager, CompetitionScreen/App/Playback, 기존 SymbolLoopPlayback/Die/Glyph, 공용 UIRoot/CommonButtonView/Pretendard TTF.
- 사용하는 쪽·관계 근거: 명시 Editor 메뉴 또는 메인 에이전트의 CLI eval. 런타임에서 실행하지 않는다.
- 상태/수명: Play 중 또는 dirty Scene이면 거절한다. 임시 작성 오브젝트는 finally에서 제거한다. Title의 다른 오브젝트/GUID/빌드 씬 목록은 보존한다.
- 검수 주의: 기존 SymbolLoopAuthoring.Build를 호출하지 않는다. 폰트/공용 프리팹/기존 화면 프리팹/프로젝트 설정을 저장하지 않으며 SaveAssets 전체 저장을 사용하지 않는다. 실제 화면과 모바일 비율 검증은 별도 필요하다.
