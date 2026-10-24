# CompetitionApp.cs

- UI13: 정보→모험 메뉴는 같은 V5DiceNetData를 보관·복원하는 OpenV5Management 콜백을 사용한다. 로컬 Open/새 경기/Close/포기 확정/경기 백업 복원 성공에서 UI 조회 참조를 해제한다. 복원 실패·확인 취소는 원조회 복귀를 보존한다. 경기 복원 후 이전 전투 정보가 로비 위에 다시 열리지 않도록 메뉴도 함께 닫는다.

- V5_UI_IMPROVEMENT_11: partial App.V5Ui에서 전용 Shell을 구성하고 기존 Render는 보존 경기 경로로 남긴다. HandleBack은 InputSystem Escape(모바일 뒤로 포함)를 받아 조회 닫기/확인 취소/모험 메뉴 열기만 수행한다. 출전·모험가 선택은 즉시 저장하고 같은 선택 화면에 남으며, 성장 구매 성공은 실제 전후 프로필 차이를 표시한다. v5 포기 확정 후 개인 종료/AI 진행 화면으로 돌아간다. 검증/자산 상태는 V5_UI_IMPROVEMENT_11_REPORT를 따른다.

- V5_DICE_COMBAT_09: 새 v5 정의의 JSON combat을 SO로 덮지 않는다. v5도 모험가 선택/6스탯 공용 표시를 사용하되 기존 경기의 전투/상세는 항상 state.definition으로 분기한다. v4 저장을 v5 로비에서 이어도 기존 UI/단일 combat 명령이 유지된다.
- V5 입력/정보: combat_roll/hold/release/reroll/execute를 같은 Dispatch의 원자 저장 후 재생 경계에 연결한다. v5 전투에는 전용 HUD 콜백을 넘기며 정보 모달에서 능력치 조회와 모험 관리로 이동한다. 종료/포기 확정은 별도 모험 관리에 둔다. 로비 출전 주사위, 경기 보유 슬롯, 완주 보상 후보에서 V5DiceNetData를 만든다. 정보 조회는 Session 명령을 실행하지 않는다.
- V5 상세: CompetitionElementView.Details가 버전에 따라 6스탯/주사위·스킬/속성·상성/태그/효과 출처 5탭을 표시한다. v5 전용 기술은 별도 v5 Plan/SkillPreview(effect)/Tags(effect) 경로를 사용하며 v4 Symbol/SkillId 조회를 거치지 않는다. 실제 6면 모달은 로비·보상·전투 진입에서 계속 사용한다. v1~v4는 탭5+앞뒤2+복귀1로 최대8 버튼을 지킨다. v5는 별도 Shell을 사용한다.
- 취소: disable/pause/Close/화면 해제는 코루틴을 중단하고 입력 잠금을 해제한 뒤 Session의 확정 사본으로 Playback.Snap한다. 재추첨/재명령은 없다. 저장 실패 notice는 전용 HUD에 전달한다.

- 역할: Title에서 공동 모험 로비를 직접 열고 계정·출전 선택·경기 저장·표시 수명을 연결한다. 구형 1인 화면/App에 의존하지 않는다.
- TAG_STAT_ELEMENT_05: v4 로더는 검증된 JSON combat을 기존 SO로 덮지 않는다. 모험가 선호는 v3/v4에서 열지만 계산 API는 버전별로 분리한다. v4의 Roster/상세/전투 상성 문구는 CompetitionElementView에 위임하고, 구형 저장 상세는 그 state.definition으로 기존 GrowthView를 호출한다. 상세5탭+이전/다음+뒤로는 최대8버튼이며 detailPage는 탭 전환/상세 진입 때0으로 초기화한다.
- GROWTH_04: v3 로비는 모험가 선택/출전 능력치를 추가하고 성장 구매/프로필 재확인을 계정 관리로 모아 최대8버튼을 유지한다. 별도 AdventurerSelectionStore를 경기 폴더에 열고 새 게임 직전 다시 읽은 ID를 Session.Start/Restart에 전달한다. 구형 현재 경기 상세는 state.definition을 사용하며 새 v3 선택을 소급 적용하지 않는다.
- 상세 관계: Render의 adventurers/stats/skills → CompetitionGrowthView → Screen의 authored 패널이다. 다음 출전 Preview와 경기 Describe를 구분한다. 메뉴는 명령/RNG를 실행하지 않고 뒤로는 원래 로비/경기로 복귀한다.
- 선택 실패: 선택 저장·revision·손상 오류는 읽기 재시도만 제공하고 원문을 자동 복구하지 않는다. 새 게임/선택을 제한하되 경기 이어하기는 유지한다. 선택 ID는 신격/소유권이 아니다.
- 핵심 동작: 기본 HTTP 설정에서 기존 endpoint/user 계정 경로를 구하고 CompetitionAccount를 연다. 로비에서 출전 주사위/무기 선택, 영구 성장 구매, 프로필 재확인, 새 게임/경기 이어하기, 계정/경기 백업 확인·명시 복원을 제공한다. 기존 경기가 있으면 새 게임은 확인/취소 경고를 먼저 표시하고, 확인 뒤 최신 프로필 조회와 Session.Restart 저장에 성공해야 새 경기로 전환한다. 시작 직전 최신 프로필과 별도 출전 선택을 네 참가자에게 동결한다.
- 입출력: authored UIManager/TextAsset/SymbolLoopConfig, OpenHttp(settings,root), OpenAccount(accountPath,competitionPath,definition,backend,seedSource), Open(path,definition,profile,seedSource) 격리 주입. Session/Account/IsPresenting/IsOpen/InLobby를 공개한다. Close는 공동 로비로 돌아가며 경기 상태를 보존한다.
- 사용하는 대상: CompetitionContent/Session/Store/Account/Screen/Playback, HttpBackendSettings/HttpGameBackend, LoopJson, Shared UI. 명령 저장 성공 이후 확정 전후 사본만 Playback에 전달한다.
- 사용하는 쪽·관계 근거: Title의 CompetitionApp 직렬화 연결과 CompetitionAuthoring.Build, CompetitionFlowTests의 로컬/계정 주입이 실제 호출자다. 별도 경쟁 저장은 기존 계정 디렉토리의 shared-world-preview-v1/checkpoint.json이며 출전 설정은 동일 하위의 lobby-loadout.json이다.
- 상태/수명: `모험 종료`는 Close로 진행을 보존하고 로비로 돌아간다. 별도 `모험 포기`는 참가자 탈락 명령이므로 구분한다. confirmNewGame은 UI 상태이며 취소/Close는 저장을 변경하지 않는다. 계정 조회·요청·연출 중 입력을 차단한다. 비활성/pause는 연출만 중단하고 Render는 isActiveAndEnabled/paused를 확인한다. 지연 응답은 비활성 화면을 재바인딩하지 않으며 OnEnable에서 표시한다. OnDestroy는 계정 요청 토큰을 취소하고 자신이 만든 HTTP transport만 해제한다. Android에서 세로 방향을 적용한다.
- 오류·복구: 설정 오류는 설정 다시 불러오기, 계정/미결/출전 오류는 계정 관리, 경쟁 저장 오류는 다시 읽기/백업 검사로 안내한다. 원문을 자동 초기화하지 않는다. 이전 active run은 그대로 보존하고 성장 구매를 비활성화하지만 출전 선택과 공동 경기 시작/재개를 허용한다. 이전 미결 서버 요청은 동일 ID 명시 재확인만 제공하고 새 v1 start/settle을 만들지 않는다.
- 검수 주의: 구형 메뉴별 버튼은 8개 이내다. v5는 V5Shell의 후보/도구/탭을 분리한다. 이미 시작된 경기는 새 프로필 조회로 소급 변경하지 않으며 서버가 불통이어도 저장 경기 재개가 가능하다. 새 게임 조회/저장 실패 시 기존 진행을 유지하고, 초기화 범위는 공동 경기뿐이다. 경기 성장 재화 지급은 0이며 C6 이후 과제다. Open은 테스트 주입 경계이고 실제 Title은 HTTP 권한 확인을 사용한다. 종료/새 게임의 실제 검증 결과는 SHARED_WORLD_FLOW_COMBO_03_REPORT를 따른다.

- UI12: ReducedMotion/SetReducedMotion은 App 수명 내 보기 설정이며 busy 동안 변경하지 않는다. 현재 Net의 표시값을 동기화하고 Render하며 경기/계정 저장·명령은 실행하지 않는다. 로비·모험 메뉴·전투 상세에서 연결한다.
