# SceneNavigation.cs
- 역할: 제작용 씬 역할(Title/Lobby/InGame)과 RunUIController가 사용하는 최소 이동 인터페이스 IRunSceneNavigation을 정의한다.
- 입출력: IsTransitioning, RequestLobby/RequestInGame의 bool 접수 결과. 이미 이동 중이거나 현재 씬/로드 불가이면 false다.
- 관계: SceneFlowController가 구현, RunUIController가 생성 시 주입받는다. SceneEntry/GameApplication은 GameSceneRole을 전달한다. 화면 View는 이 인터페이스를 참조하지 않는다.
- 상태/수명: enum/interface 자체 상태 없음. 게임 진행 RunPhase와 씬 역할을 분리한다.
- 검수: 실제 SceneManager 전환과 UIManager 화면 전환의 책임을 혼동하지 않는다. SCENE_STRUCTURE_REPORT 참조.
