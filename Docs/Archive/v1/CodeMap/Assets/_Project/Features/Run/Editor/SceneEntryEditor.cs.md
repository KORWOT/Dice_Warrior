# SceneEntryEditor.cs

- 역할: SceneEntry 전용 Inspector다. 제작 씬의 실제 role/applicationPrefab 직렬화 필드를 유지하면서 정지 미리보기와 원본 UI 편집의 진입점을 보여준다.
- 입력/출력: OnInspectorGUI에서 SerializedObject.Update, role의 한국어 Popup, applicationPrefab의 PropertyField, ApplyModifiedProperties를 사용한다. SceneEntry 런타임 필드 이름·role enum·앱 연결 계약을 바꾸지 않는다.
- 시작점 매핑: Title 역할은 WorkbenchStartPoint.Title, Lobby는 Lobby, InGame은 Map으로 작업창을 연다. 원본 참조가 없으면 shortcut 버튼을 비활성화한다.
- 동작: ‘이 씬의 플레이 작업실 열기’는 PlayWorkbenchWindow.OpenFor(entry.applicationPrefab, point)를 호출한다. 정지 상태의 ‘기본 UI 원본 편집’은 OpenSource를 사용하고 오류는 해당 entry를 context로 Warning에 남긴다. 화면이나 저장소를 생성하지 않는다.
- 제작 안내: UI가 Play에서 생성되므로 정지 시 작업실 미리보기로 확인하고 원본 프리팹에서 배치를 편집하도록 안내한다. 자동으로 씬/프리팹을 저장하는 기능은 없다.
- 관계: SceneEntry가 직렬화 대상이며 GameSceneRole/WorkbenchStartPoint는 Inspector의 진입점 매핑, PlayWorkbenchWindow는 실제 탐색과 Stage 처리 책임이다. RunSession/LocalRunStore/GameApplication.Bootstrap을 호출하지 않는다.
- 이전 PLAY_WORKBENCH 작업 검수: 당시 EditMode 128/128 통과(작업실 검사 6개 포함). 실제 실행·직접 클릭 및 보존 경계는 Docs/Reports/PLAY_WORKBENCH_REPORT.md를 참조한다.

## 로비·캠페인 진입 (LOBBY_MAP_DICE)
- Inspector의 역할 표시는 타이틀/준비 로비/캠페인 인게임이다. 기존 GameSceneRole 값은 유지하며 InGame의 작업실·기본 원본 진입점은 Combat 대신 Map이다. 실제 씬 전환이나 저장을 실행하지 않는다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
