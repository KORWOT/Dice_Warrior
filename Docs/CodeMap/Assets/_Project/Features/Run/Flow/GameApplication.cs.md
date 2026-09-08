# GameApplication.cs
- 역할: 제작용 씬 전체에서 RunUIController/SceneFlowController/UIRoot/세션 수명을 소유하는 명시적 앱 루트.
- 입력: inactive authored GameApplication prefab, optional LocalRunStore. 기본 경로는 persistentDataPath/FateDiceLocal/run.json.
- 동작: 자기 루트 참조 검증 → inactive Instantiate → flow.Initialize(controller) → controller.Initialize(store,flow) → DontDestroyOnLoad → 활성화. 초기화 전에 저장소를 주입하며 로드 자체는 저장하지 않는다.
- 출력/중복: Bootstrap은 현재 앱을 반환한다. Current는 bootstrap 경계에만 있고 화면은 조회하지 않는다. 살아 있는 앱에 다른 store를 전달하면 거절한다.
- 수명: SubsystemRegistration 및 OnDestroy로 domain reload를 끈 Editor에서도 static 참조를 정리한다. 초기화 실패 시 생성 루트 전체 비활성/제거. 스스로 scene 검색이나 자동 global bootstrap을 하지 않는다.
- 관계: production SceneEntry가 Bootstrap/EnterScene 호출, 실제 EnterScene은 sceneFlow에 위임. SceneStructureAuthoring이 prefab 참조 작성. SceneStructureTests는 초기화 전 격리 store 주입.
- 검수: Current 하나, Canvas/EventSystem 하나, 씬 간 같은 controller/UI 객체, cold Play/종료 재진입. 실패 경로와 기본 저장 비생성도 검증.

## 플레이 작업실 (2026-09-08)
- PlayWorkbenchSession은 Editor BeforeSceneLoad에서 기존 Bootstrap(prefab, store)에 테스트 저장소를 주입한다. 런타임 코드·기본 저장 경로는 변경하지 않았다.
- 사용·검증: Docs/Reports/PLAY_WORKBENCH_REPORT.md.
