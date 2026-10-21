# SaveTests.cs

M5 / FateDice.EditMode.Tests / 독립 위임 C. 현재 저장 단위 테스트32/32 PASS. 제품 디스크 GUI도 별도 FateDiceGuiTests8/8 PASS로 연결을 확인했다.

- 역할: GUID 임시 디렉터리의 실제 파일로 저장·복원·원본 보존 계약을 검사한다.
- 경계: 5사건 각각 새 런/노드 선택/탐험 굴림/카드 선택/전투 굴림·행동/보상/장비·주사위/상점 복귀/진척/보스/결과를 실제 RunSession 명령으로 통과한다.
- 실행 비교: Step은 같은 사전 상태에 명령을 연속 실행한 결과와 저장에서 복원한 결과의 전체 JSON을 비교한다. RoundTrip은 전체 상태, config 스냅샷, RNG, 확정 선택과 처리 ID를 실제 Save/Load로 보존하는지 확인한다. Checkpoint는 실제 Store.Save이다.
- 개별 경계: 장비·주사위 포기, 패배 시 추가 적 행동·보상 방지, 원본 설정 변경 후 저장 스냅샷 독립, 반복 로드 무료 굴림 금지, 스키마·체크섬·설정·참조·단계 손상, 잘못된 후보/손상 기존 파일의 덮어쓰기 거절, 임시 파일 정리, 명시 아카이브, 파일 부재.
- 직접 회귀: Unity inline 부재 DTO의 node type/children/reward health 오염3개를 거절한다. Windows 목적지 FileShare.Read 잠금은 기존 슬롯 읽기를 허용하고 교체를 차단하여 실제 IO 실패를 만들고 원본 바이트/이전 Load/임시 파일 정리를 확인한다.
- 재굴림2개: 실제 사건으로 권한과 비용2회분을 획득한 뒤 탐험/전투 카드 단계에서 Reroll(2)를 실행한다. 손상 슬롯의 Checkpoint 거절 시 원자적 롤백, 다른5개 주사위 유지, 새 확정 카드 ID, RNG/비용/진척 일관성, 반복 Load 및 자원 소진 뒤 추가 사용 거절을 확인한다.
- 직접 호출: PrototypeAuthoring.CreateDefaults, RunSession 명령과 Reroll, CombatRules.Evaluate, LocalRunStore API, JsonUtility, File/Directory, 독립 SHA256 envelope 생성.
- 상태/수명: SetUp은 OS 임시 경로 아래 고유 디렉터리를 만들며 TearDown은 그 정확 디렉터리만 정리한다. 실제 사용자 persistentDataPath에 접근하지 않는다.
- 검수 주의: 고정 시드88과 시험 설정(단일 노드 유형, 임계값1, 위력100, 보유금100, 보물 주사위)을 쓴다. 일반 기본 플레이 밸런스, 실제 SO 조정, UI/Android 검증을 이 단위 테스트로 대체하지 않는다.

## 실행 증거와 제품 관계
- m3-training-m5-save-red.json: 최초26개 전부 의도한 스텁 실패.
- m3-fix-m5-save-check.json: 최초 구현15 PASS/11 FAIL, F-M5-01(null inline DTO 복원) 확인.
- m4-red-m5-fix1-check.json: 수정1 뒤 기존26개+오염3개+실제IO실패1개=30/30 PASS.
- m4-save-regression.json: 신규 탐험/전투 재굴림2개 포함 SaveTests32/32 PASS, 전체 EditMode98/98 PASS, 실패/건너뜀0.
- 제품 연결은 실제 FateDiceScreen 소스에서 확인했다: UseStore/ReadSavedPreview/StartNewJourney/ContinueJourney/ArchiveDamagedSave, Checkpoint=Store.Save, 메뉴·Pause·Quit의 SaveElapsedTime.
- 별도 m5-disk-gui-check.json5/5, m5-gui-acceptance-check.json8/8 PASS는 FateDiceGuiTests의 증거다. 새 Scene 복귀 뒤 고정 카드/유료 재굴림 복원, 손상 파일 원본 보존·명시 아카이브, 5사건/패배/완주/재시작 및 lastResult 보존, 두 세로비 포인터 레이캐스트/스크롤을 확인했다. 이 파일32개의 테스트 수와 합쳐 보고하지 않는다.
- RunState.lastResult는 전체 payload에 직렬화된다. RunSession이 결과 진입 때 기록을 생성하고 Screen이 새 런에 이어받는 관계와 GUI의 Store.Load().lastResult 검증을 확인했다.
- M5 최초검토1/1·수정1/2 유지. 선행 준비 중 CS1628 ref 매개변수 람다 캡처 오류는 의미있는 RED 전에 지역 state 변수로 수정했다. Android 결과는 메인 REPORT의 별도 판정을 따른다.
