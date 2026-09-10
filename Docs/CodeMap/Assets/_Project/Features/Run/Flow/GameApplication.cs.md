# GameApplication.cs

## RA-B 현재 계약 (2026-09-09)

Bootstrap의 저장 인자를 IRunStore로 일반화하여 파일/실패 주입 어댑터를 같은 조립 경계로 전달한다. 기본 구현 LocalRunStore와 경로, 기존 앱의 다른 저장소/시드 공급자 거절, inactive 조립→지속 수명은 유지한다. Controller가 직접 파일 구현에 의존하지 않게 연결한다.

실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다. 기존 기록은 이번 실행 증거를 대신하지 않는다.


## RA-A 현재 계약 (2026-09-09)

Bootstrap(prefab, store, seedSource)의 마지막 선택 인자로 ISeedSource를 Controller.Initialize에 전달한다. 이미 살아 있는 앱에 다른 store 또는 명시 공급자를 주입하면 거절한다. 새 전역 서비스 없음. 기존 inactive 조립→DontDestroyOnLoad→활성 수명과 SceneEntry 조립 진입을 유지한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

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
