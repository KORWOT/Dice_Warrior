# PrototypeAuthoring.cs
- 책임: 최소기본콘텐츠와전용SO/Scene생성,실제데이터편집수용검증. FateDice.Editor전용정적도구.
- CreateDefaults는초기값한곳에서GameConfigData를구성. 실제DefaultFateDice.asset이런타임원본이며기존에셋을자동덮어쓰지않음.
- CreateAssets→없는SO만CreateAsset,없는Scene만NewScene/Camera/Screen생성/직접config+내장Font연결/SaveScene. Play중/dirtyScene은보호한다. EnsureFolder는AssetDatabase로신규폴더생성.
- InspectAssets는실제SO로드/Validate및콘텐츠수보고. VerifyConfigEdits(savePath)는실제SO를임시편집하고새런/저장복원행동비교,finally원래값복원. SaveConfigAsset은해당SO만SaveAssetIfDirty/ImportAsset한다.
- 직접사용: UnityEditor AssetDatabase/EditorSceneManager/EditorUtility, FateDiceConfig,RunSession,LocalRunStore,CombatRules,구체설정DTO,Camera/Screen/Font.
- 호출자: MenuItem와CLI eval,RuleTests/RunTests/SaveTests의독립설정fixture. 생성자산은Assets/_Project/Scenes/FateDicePrototype.unity와Fate/Configs/DefaultFateDice.asset.
- 검증: 실제25사건/5행동SO와Scene직접연결PASS. m5-so-edit-evidence.json:HP26→52,Strike13→26,등급U/C/U→Legendary3,임계값10→1;새런Boss/기존런정상길/동일저장RNG·전체스냅샷/원래SO복원 모두PASS.
- 주의: VerifyConfigEdits는명시적검증용변경진입점이며일반플레이/자동기동에서호출하지않는다. UI/GameRuntime에초기숫자를재주입하지않음.
