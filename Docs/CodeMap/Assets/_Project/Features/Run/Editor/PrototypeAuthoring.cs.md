# PrototypeAuthoring.cs

## RA-A 현재 계약 (2026-09-09)

CreateDefaults에서 탐험/전투 RollPresentationSettings를 각각 생성하여 .65초 굴림/.9초 결과 유지 기본을 명시한다. 기존 legacy .35 값과 모든 게임 규칙 수치는 유지한다. 기존 SO 자동 재생성 없음; 이번 DefaultFateDice.asset 변경은 PLAN에 지정된 Editor API 도구로만 수행한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 책임: 최소기본콘텐츠와전용SO/Scene생성,실제데이터편집수용검증. FateDice.Editor전용정적도구.
- CreateDefaults는초기값한곳에서GameConfigData를구성. 실제DefaultFateDice.asset이런타임원본이며기존에셋을자동덮어쓰지않음.
- CreateAssets→없는SO만CreateAsset,없는Scene만NewScene/Camera/Screen생성/직접config+내장Font연결/SaveScene. Play중/dirtyScene은보호한다. EnsureFolder는AssetDatabase로신규폴더생성.
- InspectAssets는실제SO로드/Validate및콘텐츠수보고. VerifyConfigEdits(savePath)는실제SO를임시편집하고새런/저장복원행동비교,finally원래값복원. SaveConfigAsset은해당SO만SaveAssetIfDirty/ImportAsset한다.
- 직접사용: UnityEditor AssetDatabase/EditorSceneManager/EditorUtility, FateDiceConfig,RunSession,LocalRunStore,CombatRules,구체설정DTO,Camera/Screen/Font.
- 호출자: MenuItem와CLI eval,RuleTests/RunTests/SaveTests의독립설정fixture. 생성자산은Assets/_Project/Scenes/FateDicePrototype.unity와Fate/Configs/DefaultFateDice.asset.
- 검증: 실제25사건/5행동SO와Scene직접연결PASS. m5-so-edit-evidence.json:HP26→52,Strike13→26,등급U/C/U→Legendary3,임계값10→1;새런Boss/기존런정상길/동일저장RNG·전체스냅샷/원래SO복원 모두PASS.
- 주의: VerifyConfigEdits는명시적검증용변경진입점이며일반플레이/자동기동에서호출하지않는다. UI/GameRuntime에초기숫자를재주입하지않음.

## RA-C 직접 관계
GameConfigData는 Runtime의 제작/표시 DTO이며 규칙 필드는 Core RunRulesCatalog에서 상속한다. 기존 SO 값/기본 규칙 제작/직접 편집 흐름은 유지된다. C에서 자산을 재생성하지 않는다.

- RA-D: CreateDefaults에 잔불 베기1종/treasure_0의 addActionId/등급별 상점 배율만 추가한다. 시작 행동/와일드 카드 목록·기존 보상·품목을 유지한다. 기존 SO 자동 덮어쓰기는 하지 않는다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
