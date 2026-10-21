# SceneFlowController.cs
- 역할: 세 제작용 씬의 실제 비동기 Single 로드와 진입 수락, 입력 잠금을 소유한다.
- 입력: 명시적 RunUIController 및 title/lobby/inGameScenePath. RequestLobby/RequestInGame은 접수 성공 여부, CurrentRole은 완료한 UI 역할, IsTransitioning은 진행 상태.
- 동작: 활성/중복/Busy/현재씬/CanStreamedLevelBeLoaded 검사 → lock → LoadSceneAsync → SceneEntry 역할 진입 완료를 기다린 뒤 unlock. 완료한 로드의 entry 수락만 5초 대기 한도이며 Unity 로드 자체는 취소 가능하다고 가장하지 않는다.
- 직접 진입: Title→controller.EnterTitle, Lobby→EnterLobby, InGame→TryEnterInGame. 저장이 없거나 손상된 InGame은 Lobby로 복귀한다. 진행 중인 로드에서는 같은 lock 아래 fallback을 한 번 수행한다.
- 실패: entry 불일치/로드 불가/초기 예외/미수락은 오류 메시지를 controller로 전달하고 잠금을 복원한다. 새 게임 규칙이나 보상/저장 명령을 실행하지 않는다.
- 관계: IRunSceneNavigation 구현. GameApplication이 initialize 및 SceneEntry의 역할 전달, RunUIController가 이동 요청. UIManager와 View는 씬을 모르며 controller.SyncInputLock/ReportNavigationError만 연결된다.
- 수명/검수: GameApplication과 같은 persistent 루트. 실제 씬 로드, 중복 요청, 실패 후 UI 사용, 저장 상태 보존, fixture teardown 검증.

## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- Title → Lobby(모험가·세팅·성장 안내 준비) → InGame(캠페인 맵과 전투)의 세 씬 역할을 유지한다. Lobby 진입은 RunUIController.EnterLobby, 저장된 런의 InGame 진입은 TryEnterInGame에 위임하며 맵/전투/주사위 popup 사이에 별도 씬 로드를 추가하지 않는다.
- 새 여정·이어하기를 처리한 RunUIController가 RequestInGame을 호출한다. 메뉴 복귀는 경과 시간 저장 성공 뒤 RequestLobby를 호출하며, 씬 진입 수락 및 저장 불가 InGame의 Lobby fallback은 기존 경계를 유지한다.
- Request의 controller.Busy 검사와 SyncInputLock 연결은 노드 도착 및 주사위 명령/연출 중의 중복 이동도 차단한다. Busy는 컨트롤러의 actionBusy와 이 Flow의 IsTransitioning을 합친 값이며, Flow가 CampaignMapView나 DiceRollUI를 직접 제어하지 않는다.
- 문서 범위: 실제 소스와 이번 작업의 staged 컨트롤러 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.
