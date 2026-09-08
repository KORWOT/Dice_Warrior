# FateDiceConfig.cs
- 책임: 게임 설정 SO와 직렬화 데이터/열거형, 유효성 검증, 검증된 깊은 스냅샷.
- 진입: GameConfigData.Validate→오류경로 배열, DeepCopy→JsonUtility 복제, FateDiceConfig.Snapshot→검증 후 복제. Action/Enemy/Equipment/Die/Event는 안정 ID로 정확히 조회한다.
- 입력: DefaultFateDice.asset의 dice/fate/combat/growth/world/presentation. 확률 표/스탯/보상/가격/연출/태그 조건이 편집 대상이며 HP/보호막/진척은 포함하지 않는다.
- 검증: 중복/누락 ID,10패 고유우선순위,유효 주사위6면,가중치 음수·NaN·0합,등급표·범위,행동소유/시험Rare이하,장비/태그/적/사건25풀/보상참조 등을 확인한다. 숨은 기본값으로 복구하지 않는다.
- 사용하는 쪽: PrototypeAuthoring.CreateDefaults/CreateAssets/VerifyConfigEdits, Screen.Awake/새런/메뉴, RunSession.New, 모든 순수 규칙, LocalRunStore.ValidateState, 세 EditMode 테스트 클래스.
- 상태/수명: SO는 원본이고 RunState.config는 JSON 복제. 파일명/배열 인덱스 대신 id를 저장. 진행 중/디스크 복원은 실제 저장 config를 사용한다.
- 증거: RuleTests32 포함 Edit98/98, m5-so-edit-evidence.json에서 실제적HP/계수·가중치·임계값변경 새런반영 및 기존저장불변·원래SO복원 PASS.


- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.
