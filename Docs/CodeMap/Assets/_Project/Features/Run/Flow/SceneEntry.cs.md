# SceneEntry.cs
- 역할: Title/Lobby/InGame 씬마다 하나씩 작성되는 제작용 진입 컴포넌트.
- 입력: 직렬화한 GameSceneRole role와 GameApplication applicationPrefab 직접 참조.
- 동작: Start에서 Bootstrap(applicationPrefab) 후 EnterScene(role). 초기화 오류는 Scene initialization failed 메시지로 콘솔에 남긴다.
- 관계: SceneStructureAuthoring이 세 씬을 작성하고 GameApplication으로 명시적으로 연결한다. 기존 FateDicePrototype 및 UI 테스트 fixture에는 이 컴포넌트가 없다.
- 상태/수명: 해당 씬과 함께 제거되며 persistent UI/session을 소유하거나 닫지 않는다. 앱 중복 처리는 GameApplication에 위임.
- 검수: 실제 씬의 role/path/prefab 연결 및 Current=null에서 직접 Play, InGame 저장 없음→Lobby 복귀를 확인한다.

## 플레이 작업실 (2026-09-08)
- SceneEntryEditor는 같은 role/applicationPrefab 직렬화 필드를 한국어로 표시하고 해당 씬의 작업실 및 UI 원본 편집을 연결한다. 작업실 실패 시 이번 Play의 Entry만 비활성화했다가 종료 시 복원한다.
- 사용·검증: Docs/Reports/PLAY_WORKBENCH_REPORT.md.
