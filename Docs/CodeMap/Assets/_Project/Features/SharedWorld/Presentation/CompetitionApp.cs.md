# CompetitionApp.cs

- 역할: 별도 공동 세계 시험 모드의 진입·명령·저장·화면 수명을 연결한다. 영구 재화 정산을 수행하지 않는다.
- 핵심 동작: v1 App의 타입 독립 이벤트를 구독하고 기존 화면을 일시중지한다. HTTP 권한 프로필 재확인 성공 후 성장/출전값을 복제한다. 새 경기 시작 때도 다시 확인하며 이미 시작된 경기의 스냅샷을 소급 변경하지 않는다. Session.Execute 성공 이후 확정 전후 사본만 Playback에 전달한다.
- 입출력: authored legacy/UIManager/TextAsset/SymbolLoopConfig, Open(path,definition,profile,seedSource) 시험 주입, Render/Close. Session의 Snapshot/Start/Prepare/Execute/Reload/RestoreBackup을 사용한다.
- 사용하는 대상: CompetitionContent/Session/Store/Screen/Playback, LoopJson, SymbolLoopApp/Backend, Shared UI.
- 사용하는 쪽·관계 근거: Title의 CompetitionApp 직렬화 참조, CompetitionAuthoring.Build의 기존 App 연결, CompetitionFlowTests의 격리 주입.
- 상태/수명: 명령·조회·연출 중 입력 차단. 화면 닫기·비활성·pause는 연출을 중단하며 도메인 명령을 재실행하지 않는다. 실제 checkpoint 디렉토리의 shared-world-preview-v1 하위 경로를 사용하므로 endpoint/개발 계정 분리를 상속하고 v1 파일과 격리한다. 완료 경기의 기록 보존은 Store 책임이다.
- SW-R1: 지연된 프로필 응답이 Open을 거쳐 비활성 App 화면을 재바인딩하던 실제 실패를 확인했다(기대 BindingRevision4, 실제5). Render 진입에서 isActiveAndEnabled를 검사해 비활성 동안 저장 상태만 보존하고 OnEnable에서 표시한다. 통합검사의 기대값은 유지한다.
- 검수 주의: 시험 결과는 성장 재화 0으로 표시한다. 손상 저장은 자동 초기화하지 않고 명시 백업 검사/복원을 제공한다. UI에서 v1 phase/정산 함수를 새 규칙에 적용하지 않는다. 스크립트의 Open은 테스트 주입 경계이며 실제 Title은 권한 프로필 확인을 거친다.
