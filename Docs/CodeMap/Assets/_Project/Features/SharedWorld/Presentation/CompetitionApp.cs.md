# CompetitionApp.cs

- 역할: Title에서 공동 모험 로비를 직접 열고 계정·출전 선택·경기 저장·표시 수명을 연결한다. 구형 1인 화면/App에 의존하지 않는다.
- 핵심 동작: 기본 HTTP 설정에서 기존 endpoint/user 계정 경로를 구하고 CompetitionAccount를 연다. 로비에서 출전 주사위/무기 선택, 영구 성장 구매, 프로필 재확인, 경기 시작/이어하기, 계정/경기 백업 확인·명시 복원을 제공한다. 경기 시작 직전 최신 프로필과 별도 출전 선택을 네 참가자에게 동결한다.
- 입출력: authored UIManager/TextAsset/SymbolLoopConfig, OpenHttp(settings,root), OpenAccount(accountPath,competitionPath,definition,backend,seedSource), Open(path,definition,profile,seedSource) 격리 주입. Session/Account/IsPresenting/IsOpen/InLobby를 공개한다. Close는 공동 로비로 돌아가며 경기 상태를 보존한다.
- 사용하는 대상: CompetitionContent/Session/Store/Account/Screen/Playback, HttpBackendSettings/HttpGameBackend, LoopJson, Shared UI. 명령 저장 성공 이후 확정 전후 사본만 Playback에 전달한다.
- 사용하는 쪽·관계 근거: Title의 CompetitionApp 직렬화 연결과 CompetitionAuthoring.Build, CompetitionFlowTests의 로컬/계정 주입이 실제 호출자다. 별도 경쟁 저장은 기존 계정 디렉토리의 shared-world-preview-v1/checkpoint.json이며 출전 설정은 동일 하위의 lobby-loadout.json이다.
- 상태/수명: 계정 조회·요청·연출 중 입력을 차단한다. 비활성/pause는 연출만 중단하고 Render는 isActiveAndEnabled/paused를 확인한다. 지연 응답은 비활성 화면을 재바인딩하지 않으며 OnEnable에서 표시한다. OnDestroy는 계정 요청 토큰을 취소하고 자신이 만든 HTTP transport만 해제한다. Android에서 세로 방향을 적용한다.
- 오류·복구: 설정 오류는 설정 다시 불러오기, 계정/미결/출전 오류는 계정 관리, 경쟁 저장 오류는 다시 읽기/백업 검사로 안내한다. 원문을 자동 초기화하지 않는다. 이전 active run은 그대로 보존하고 성장 구매를 비활성화하지만 출전 선택과 공동 경기 시작/재개를 허용한다. 이전 미결 서버 요청은 동일 ID 명시 재확인만 제공하고 새 v1 start/settle을 만들지 않는다.
- 검수 주의: 메뉴별 버튼은 8개 이내다. 이미 시작된 경기는 새 프로필 조회로 소급 변경하지 않으며 서버가 불통이어도 저장 경기 재개가 가능하다. 경기 성장 재화 지급은 0이며 C6 이후 과제다. Open은 테스트 주입 경계이고 실제 Title은 HTTP 권한 확인을 사용한다. 실제 검증 결과는 SHARED_WORLD_MAIN_02_REPORT가 기준이다.
