# PLAY_WORKBENCH_PLAN
상태: IMPLEMENT / COMPLETE · 사용자2026-09-08 요청: Unity 에디터에서 씬·UI 편집 가시성과 플레이 루프 변경 편의 개선, 직접 버튼 클릭 테스트. 컴퓨터 UI 조작은 Unity Editor 창으로 한정한다.

## 계약
대상 D:/UnityProject/Dice_Warrior, main433edddc872433ead6db52a872ea0fcac659d92e, Unity6000.6.0f1/7801. 시작 시 실제 Git/hash/기본save를 보존한다. 직전 한글화 완료 작업 및 이전 완료 계약은 재개방하지 않는다.
기존 제작씬에는 SceneEntry/Camera만 있고 Play에 앱/UI가 생성된다. 이를 유지하면서 Editor-only ‘플레이 작업실’을 제공한다. 원본 UI 프리팹 편집, 제작씬 열기, 앱/설정/시각 카탈로그 찾기, 원하는 시작 상황/시드/시련/등급상한으로 격리 Play를 시작한다.
정지 중 미리보기는 폐기 가능한 PreviewSceneStage에서 원본 UIRoot/8화면 프리팹과 실제 규칙으로 만든 예시 데이터를 표시한다. 미리보기에는 GameApplication/RunUIController/저장소/입력 EventSystem/실제 플레이 콜백을 만들지 않는다. 한 번만 바인딩하고 전체 Stage를 닫아 정리한다. 프리팹 편집은 별도 버튼으로 원본 Prefab Stage에 진입하며 미리보기 편집이 저장되지 않음을 명확히 표시한다.
격리 Play는 Library/FateDiceWorkbench의 새 전용 저장 경로를 사용한다. BeforeSceneLoad에서 1회 요청을 소비하여 기본 SceneEntry.Start보다 앞서 Bootstrap(prefab,store)을 실행한다. 초기화 실패 시 기본 저장소로 진행하지 않고 Play를 중단한다. 요청 없는 일반 Play는 그대로다. domain/scene reload 옵션을 변경하지 않고 현재 옵션3에서 반복 시작/종료를 검사한다.
시작점 enum: Title,Lobby,Map,ExplorationCards,Combat,Shop,Reward,Equipment,Result. 시드/시련/등급상한을 제공한다. 특정 상황으로 바로 가기 위한 사건확률 고정/결과 생성용 스탯 조정은 테스트용 config 복사본에만 적용하고 작업창에 명시한다. 실제 룰·checkpoint 형식과 config SO/기존 저장은 변경하지 않는다.

## 파일/API/담당
기존4에이전트 이내(Main+A+B+C), 재귀0, 같은파일단일작성, 모든D쓰기/Unity/문서는Main직렬처리.
A: 신규 Features/Run/Editor/PlayWorkbenchSession.cs stagedA. enum WorkbenchStartPoint, [Serializable]WorkbenchOptions {GameApplication application; uint seed=33; string trialId; Grade cap=Legendary; WorkbenchStartPoint startPoint=Combat;}. static PlayWorkbenchSession: Build(WorkbenchOptions):RunState (Title/Lobby도Map상태생성), Start(WorkbenchOptions):void, public IsPending/LastError/LastStorePath 읽기. actualRunSession commands로preset 생성. scene dirty/PrefabStage/Play/compile guard, 1회 BeforeSceneLoadbootstrap, 시작시 GameView해상도720x1280, 원래SceneManagerSetup 저장/종료복원. SessionState로 pending복원, 여러번시작거절/일반Play무효과, 실패시Play중단. User save불접근.
B: 신규 Features/Run/Editor/UIWorkbenchPreview.cs stagedB. sealed PreviewSceneStage UIWorkbenchPreview; static Open(WorkbenchOptions,int height=1280); cloneCanvas WorldSpace720xheight, 실제 UIManager/typedUIData 1회바인딩, input비연결, actualconfig/state 데이터. 현재 stage 닫고 생성갱신, SourcePrefabPath/PreviewRoot 읽기, SceneView2Dframe. PreviewStage수명내정리, GameApplication/Controller/LocalRunStore없음. Title/Lobby/Map/Combat/Shop/Reward/Equipment/Result표시. 실제원본Prefab편집과미리보기는명확히구분.
Main: 신규 Features/Run/Editor/PlayWorkbenchWindow.cs 및 SceneEntryEditor.cs. Window메뉴 ‘Fate Dice/플레이 작업실’, 한국어 컨트롤/연결/시작/미리보기/원본열기/닫기·중지. Inspector SceneEntry 기존SerializedProperty참조/role표시유지와작업창/scene/프리팹편집shortcut. gameplay UX변경은 직접 관찰된 문제에 한정하고 새 exactfile 필요시여기수정.
C: 신규 Features/Run/Tests/EditMode/PlayWorkbenchTests.cs stagedC. 옵션검증·actualpreset합법성/원본config불변·전체화면미리보기stage수명·소스asset/기본save불변·normalplaypending없음 검사. 필요시 기존 EditMode Tests.asmdef에 Unity.ugui참조추가만 허용. 추가Play기능검증은 Main실제 Editor 경로로 진행.
Main: 모든4신규C#+tests1 및 직접관계 GameApplication,SceneEntry,SceneStructureAuthoring,UIManager,RunUIData의 CodeMap/INDEX, 본PLAN/REPORT. 문서 색인의 구식 LegacyRuntime폰트참조를 실제 Pretendard로 보정한다.

## 정확한 쓰기 범위
Assets/_Project/Features/Run/Editor/{PlayWorkbenchSession,UIWorkbenchPreview,PlayWorkbenchWindow,SceneEntryEditor}.cs 및 신규Unitymeta.
Assets/_Project/Features/Run/Tests/EditMode/PlayWorkbenchTests.cs 및 신규meta; 같은folder/FateDice.EditMode.Tests.asmdef의 uGUI참조만.
Docs/Plans/PLAY_WORKBENCH_PLAN.md, Docs/Reports/PLAY_WORKBENCH_REPORT.md, Docs/CodeMap의 위신규5·직접관계5, INDEX.
Library/FateDiceWorkbench의격리저장·임시요청, SessionState/Editor현재창·stage만.
제작scene/prefab/config/catalog asset에 영구쓰기0. Packages/ProjectSettings/기존save/이미지·font/기존규칙수정0. Scene/Prefab 수정이필요해지면구체파일/목적을추가한후 Editor API만.

## 실행 및 완료
1 baseline/Git/userSavehash + 실제Editorhierarchy/화면·직접클릭 사전확인, RED 의미있는미리보기/옵션검증부터.
2 신규Editor도구구현 및 실제CLI import/컴파일. 다양한preset RunState유효성/원본불변 확인.
3 새Edit검사+기존Edit122/Play57실제PASS. 정지미리보기두비율·stage닫기누수0, 제작scene/원본prefab/config/GUID기본save불변. domain/scene reload 옵션3에서 격리시작/중지2회, 요청없는일반Play, 초기화실패failclosed.
4 Computer Use승인후 UnityEditor창에서실제 시작/미리보기/프리팹편집/주사위·카드·메뉴·이어하기·상점/보상 버튼클릭, 관찰결과/불편·개선을기록. 직접클릭은CLI eval명령콜백실행과구분. 앱접근타임아웃이면미실행을명시하고허용후재개.
5 최초검토1묶음 + 필수Finding수정/재검증최대2묶음. 문서/서브작업별새예산없음. 신규5포함C#59/CodeMap59/INDEX동기화, boundaryhash 및실제증거보고. 외부앱UI조작/커밋/브랜치/패키지/전역설정변경없음.

## 실제 실행 보완과 경계 기록
Window의 선택 비율은 미리보기와 Play 모두 적용한다. 현재 화면 표시는 OnInspectorUpdate로 갱신한다. Preview는 격자를 임시로 숨기고 닫으면 원래 값을 복원한다.
PWB-01: 오래된 SceneSetup만 남은 일반 Play에서 과거 씬을 복원하던 동작을 실제 RED→GREEN으로 검증했다. 자동 종료 복원은 이번 Pending/Active 소유권으로 제한한다. 비상 중단에서도 Active를 먼저 보존하여 Entry·원래 씬 복원을 유지한다. 최초 검토1+필수 수정 재검증2 묶음으로 종료했다.
원본 편집 버튼 직접 검증 중 Unity Prefab Stage가 CombatUI.prefab을 15:14:55에 재저장했다. 의도한 화면 배치 편집 입력은 없었으나 baseline 해시는 다르다. 원본 바이트 백업이 없어 정확한 직렬화 필드 차이는 확인하지 못했으며, 임의 재구성으로 덮어쓰지 않았다. 따라서 이 프리팹은 변경 경계 예외로 기록하고 전체 전투·한글 UI 회귀 결과와 함께 전달한다. Scene/config/catalog/font/다른 prefab/기본 save는 baseline과 비교한다.