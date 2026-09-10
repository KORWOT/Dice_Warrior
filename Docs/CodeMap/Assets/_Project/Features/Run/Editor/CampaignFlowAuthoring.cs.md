# CampaignFlowAuthoring.cs

- 역할: 승인된 즉시 노드 이동·전투 진입 연출의 실제 원본을 한 번 작성하는 Editor 메뉴다.
- 입력/출력: `Fate Dice/즉시 이동과 전투 진입 연출 적용` 또는 Apply(). 지도 ApplyDirectTravel 결과와 전투 설정 적용/유지 안내 문자열을 반환한다.
- 핵심 동작: 정지/컴파일·가져오기 종료/MainStage/모든 씬 clean을 확인한다. CampaignMapAuthoring.ApplyDirectTravel은 ExplorationUI만 갱신한다. CombatUI.entryVersion<1일 때 기존 arena/stageGraphic/actionFeedback/hitFlash 참조를 확인하고 entrySeconds=.7, entryIntensity=.06, entryVersion=1만 작성한다.
- 사용하는 대상: Unity AssetDatabase/PrefabUtility/SceneManager/StageUtility, CampaignMapAuthoring, CombatUI. 사용하는 쪽: 사용자 Editor 메뉴와 Main의 Unity CLI eval. 런타임 호출 경로가 없다.
- 상태/수명: LoadPrefabContents는 finally에서 Unload. 이미 작성된 버전은 시간/강도 등 사용자 편집값과 원본 bytes를 유지한다. 기존 Scene/SharedUI/폰트/FEEL/게임 설정은 쓰지 않는다.
- 검수 주의: 원본 경로는 Run/Prefabs/ExplorationUI.prefab 및 CombatUI.prefab 두 개. GUID/기존 필드 보존과 재적용 byte동일은 실제 Main 검증이 필요하며 구조만으로 PASS를 주장하지 않는다.
