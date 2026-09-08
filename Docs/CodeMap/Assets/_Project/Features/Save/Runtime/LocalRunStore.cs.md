# LocalRunStore.cs

M5 / FateDice.Runtime / 독립 위임 C. 저장 단위32/32 PASS, 제품 디스크 GUI 연결 완료 및 PlayMode8/8 PASS.

- 역할: 생성자에 주입한 단일 경로에서 실제 RunState와 GameConfigData 스냅샷을 저장·복원한다. Path는 정규화한 절대 경로이며 인스턴스가 보유하는 유일한 값이다.
- 입력/출력: Save(RunState)는 성공 때만 파일을 교체한다. Load()는 새 RunState 객체를 반환한다. Exists는 파일 존재를 읽고 Archive()는 같은 폴더 고유 UTC 시각/GUID .bak 경로를 반환한다.
- 저장 순서: 후보 상태 검사 → 전체 JsonUtility payload + SHA256 envelope 생성 → 기존 파일 Load 검증 → 같은 폴더 고유 임시 파일 작성 및 Flush(true) → 기존 파일이 있으면 File.Replace, 없으면 File.Move. 실패 시 목적지를 삭제/잘라 쓰는 대체 경로가 없으며 임시 파일만 정리한다.
- 오류/보존: 잘못된 JSON, 체크섬, 스키마, 설정, 필수 ID, 그래프 연결, 고정 카드/주사위, 단계별 상태는 파일 경로와 이유를 포함한 InvalidDataException으로 보고한다. 파일 부재는 FileNotFoundException. IO/접근 오류는 호출자에게 전달한다. Load는 파일을 변경하지 않으며 손상/미지원 기존 슬롯은 명시 Archive 전 Save로 덮어쓸 수 없다.
- 상태 검증: 설정 스냅샷, RNG/seed, 스탯/진척/자원, 소유 행동·장비·주사위, 평탄 노드/선택 복제 일치, 처리한 보상/사건 ID, 각 phase의 적·사건·보상·선택을 확인한다. 보상 수령 후 남는 처리 ID와 이전 굴림 표시는 정상 상태로 허용한다.
- inline 부재 계약: Unity JsonUtility는 null NodeState/RewardDefinition을 빈 객체로 복원한다. Map의 선택 노드는 null 또는 ID/자식 없는 기본 객체만 부재다. 보상이 없는 phase는 null 또는 전 수치0/빈 아이템 ID 객체만 부재다. Combat/Encounter/Reward, 미수령 Shop 및 생존 적의 전투 패배는 실제 pending ID와 보상을 요구한다. Shop 상품 수령 뒤 빈 DTO는 처리 ID로 구분한다. 값 오염을 지우거나 데이터를 교정하지 않는다.
- 직접 사용하는 대상: RunState/LocalSaveEnvelope, GameConfigData.Validate, DiceRules.BestHand(고정된 값의 순수 검증), JsonUtility, System.IO, SHA256. RNG 추첨, 라이브 SO 조회, 새 기본값 생성은 하지 않는다.
- 제품 호출자: FateDiceScreen.Awake가 Application.persistentDataPath/FateDiceLocal/run.json 경로의 Store를 생성하고 UseStore로 연결한다. ReadSavedPreview와 ContinueJourney는 Load, StartNewJourney는 초기 Save와 Checkpoint=Store.Save, ArchiveDamagedSave는 명시 Archive를 사용한다. 메뉴 복귀·OnApplicationPause(true)·OnApplicationQuit는 SaveElapsedTime을 통해 현재 런을 저장한다. 오류는 화면에 표시되며 손상 슬롯은 새 런/이어하기를 막고 원본 보존 버튼을 제공한다.
- 명령 경계: RunSession.Apply는 복제 상태에서 규칙을 실행하고 Checkpoint가 성공한 뒤 State를 교체한다. Reroll의 한 주사위·새 카드·RNG·비용도 같은 경계다. SaveTests와 FateDiceGuiTests는 주입한 고유 임시 경로를 사용한다.
- 최근 결과: 전체 RunState payload에는 lastResult도 포함된다. RunSession은 Result 진입 때 runId/seed/승패/사건/턴/등급/시간 기록을 생성하고, Screen의 새 런은 직전 lastResult를 이어받아 초기 Save한다. GUI 완주 후 재시작 및 패배에서 Store.Load().lastResult 확인이 통과했다.
- 수명: 각 Load는 독립 객체를 만들고 Save는 입력 상태를 변경하지 않는다. 동일 UI의 직렬 명령 경계를 대상으로 하며 다중 프로세스 동시 쓰기 기능은 없다.
- 검수 주의: SHA256은 우발적 손상 검출이며 서버 검증/치트 방지 서명이 아니다. 아래 GUI 증거는 Editor PlayMode이며 Android 빌드/실기기 결과는 메인 REPORT에서 구분한다.

## 실제 검증과 예산
- m3-training-m5-save-red.json: SaveTests 최초26개 의도한 NotImplemented RED.
- m3-fix-m5-save-check.json: 최초 구현15 PASS/11 FAIL. 정상 Map의 Save 성공 후 Load에서 null 가정이 실패한 F-M5-01을 확인했다. 메인 Editor eval에서 두 inline DTO가 빈 객체로 복원됨을 확인했다.
- m4-red-m5-fix1-check.json: 부재 의미 검증 수정1 후 저장30/30 PASS. 정상 모든 명령 경계, 부재 DTO 값 오염3개, Windows 공유 잠금의 실제 교체 실패/원본 바이트/임시 파일 정리를 포함한다.
- m4-save-regression.json: 탐험/전투 Reroll 저장2개 포함 SaveTests32/32 PASS, 전체 EditMode98/98 PASS, 실패/건너뜀0. 손상 슬롯의 Checkpoint 거절 시 RNG/비용/선택 및 원본 보존, 반복 Load 자원 환급 없음도 확인했다.
- m5-disk-gui-check.json: 디스크 GUI5/5 PASS. m5-gui-acceptance-check.json: 확장 GUI8/8 PASS. 새 Scene에서 이어하기, 고정 카드/RNG/지불한 재굴림 복원, 손상 파일 설명·명시 아카이브, 5사건·성장·패배·완주/재시작/최근 결과, 두 세로비에서 실제 포인터 레이캐스트와 스크롤 도달성을 검사했다.
- M5 최초검토1/1·수정1/2 유지, F-M5-01 해결. M4 신규 재굴림 경계와 제품 GUI 연결은 별도 저장 수정2 회차로 계산하지 않았다.


## 추가 UI의 기록 보존 계약
- ValidateGraph는 active + nodeHistory 전체에서 ID 중복/활성 겹침/누락 자식/지원 유형을 검사한다. 선택 가능 roots에서 활성 그래프 전체가 도달 가능한지도 유지한다.
- 3색 DFS로 실제 back edge만 cycle로 거부하고 이미 완료된 공유 자식 재방문은 허용한다. history 없는 기존 schema 1 저장을 변경/교정 없이 읽는다.
- PruneTo가 옮긴 기록의 자식은 활성 또는 이력의 ID를 참조할 수 있다. 저장은 Sprite/Prefab/라이브 표시 카탈로그에 의존하지 않는다.
- 추가 RunTests 5개 및 UiVisualCatalogTests 19개를 포함한 EditMode 초기 구현 실행은 122/122 PASS. GUI 합류/fade/이어하기는 메인 수용 실행 결과를 REPORT에 기록한다.

- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.
