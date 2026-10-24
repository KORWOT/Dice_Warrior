# CompetitionAuthoring.cs

- v5 정보창 배경은 불투명도1로 작성/갱신한다. 선형 색상 공간에서 뒤 전투 글자가 비치는 것을 막으며 기존 공용 UI 배경은 바꾸지 않는다.

- V5_DICE_COMBAT_09: 별도 UpgradeV5 메뉴/공개 API를 추가했다. 기존 Build는 v4 작성 경로로 보존한다. UpgradeV5는 검증한 V5ContentPath JSON을 읽고 PrefabUtility.LoadPrefabContents로 같은 CompetitionScreen.prefab을 열어 V5 Combat/Information 두 subtree와 Screen 참조만 추가한다. 이미 연결된 subtree는 재생성하지 않으며 이름만 남은 미연결 subtree는 거절한다.
- 보호: 기존 프리팹의 content/8버튼 계약을 확인하고 기존 노드를 파괴하거나 재생성하지 않는다. 저장 후 UnloadPrefabContents를 finally로 보장한다. Title에서는 기존 CompetitionApp.content만 v5 TextAsset으로 바꾸며 UIManager 배열/다른 참조/GUID/.meta를 수정하지 않는다. Play 진입 중/Play 중 또는 열린 Scene 하나라도 dirty면 쓰기 전에 거절한다. 실제 자산 실행은 메인 담당이다.
- authored V5 구성: safe area 부모에 stretch한 Area, 상단 배율 프레임, 독립 Roll/Hold 영역, 3개 인스턴스 카드, 최초 굴림, 하단 고정 Reroll/Execute, HP/의도/예상/실패 공간. Net은 전체 raycast 차단 배경, 3슬롯 탭, 인덱스 0..3 가로+4 위+5 아래의 실제 6면, 상세 페이지/닫기/전투 관리 버튼을 연결한다. 공용 CommonButtonView 인스턴스와 비레이캐스트 V5FaceGraphic을 사용한다.
- 수정2회차 F04: FaceGraphic 생성은 CanvasRenderer를 함께 포함한다. 이미 작성된 prefab은 v5Area.faces3개/v5Net.faces6개의 소유 hierarchy·중복없음을 검증한 뒤 누락 CanvasRenderer만 추가한다. 기존 face GameObject/RectTransform/Graphic, legacy subtree와 공용 prefab은 재생성/수정하지 않는다. 실제 UpgradeV5 재실행은 메인이 담당한다.
- 검증 관계: CompetitionV5FlowTests가 실제 authored 참조, 포인터 hit, safe area 하단, 화면 텍스트 및 캡처를 검증한다. 코드 작성/원본 확인은 Unity 실행 PASS가 아니다.

- 역할: 공동 모험 메인의 CompetitionScreen 프리팹과 Title 연결을 Editor API로 작성한다.
- 핵심 동작: 고정12칸/네 말/참가자4행/주사위/전투 무대/버튼8개를 authored 참조로 만든다. 기존 Title의 UIManager에 CompetitionScreen 하나만 등록하고 CompetitionApp의 UI/콘텐츠/전투 Config를 연결한다. 구형 App·화면 생성 경로는 없다.
- GROWTH_04: GrowthDetails는 동일720×1280 프레임 안에 선택 카드3개, RGB 색/검·방패·지팡이 아이콘3행, 출처/장비, 주사위·스킬 텍스트 영역을 작성한다. 패널은 기본 비활성이고 Screen이 기존 참조를 바인딩한다.
- TAG_STAT_ELEMENT_05: ContentPath는 별도 shared-world-preview-4.json이다. 기존 RGB3행과 카드/버튼을 보존하고, 같은 상세 패널에6스탯 각44px 행/색상/아이콘/출처와 HP 본문을 별도로 작성한다. 실제 Scene/Prefab 갱신은 메인이 승인된 두 자산에 Editor API로 실행한다. 코드 작성 자체는 자산 검증 PASS가 아니다.
- 입출력: Build → Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab, Assets/_Project/Scenes/Title.unity. ContentPath의 JSON과 기존 SymbolLoopConfig를 참조한다.
- 사용하는 대상: AssetDatabase/PrefabUtility/EditorSceneManager, CompetitionScreen/App/Playback, 기존 SymbolLoopPlayback/Die/Glyph, 공용 UIRoot/CommonButtonView/Pretendard TTF.
- 사용하는 쪽·관계 근거: 명시 Editor 메뉴 또는 메인 에이전트의 CLI eval. 런타임에서 실행하지 않는다.
- 상태/수명: Play 중 또는 dirty Scene이면 거절한다. 임시 작성 오브젝트는 finally에서 제거한다. Title의 다른 오브젝트/GUID/빌드 씬 목록은 보존한다.
- 검수 주의: 폰트/공용 프리팹/전투 Config/프로젝트 설정을 저장하지 않으며 SaveAssets 전체 저장을 사용하지 않는다. 구형 전용 App 오브젝트/프리팹 제거는 MAIN_02에서 명시한 일회성 Editor API 정리다. Build는 다른 Scene 오브젝트를 검색·삭제하지 않는다. 실제 화면과 모바일 비율 검증은 별도 필요하다.
