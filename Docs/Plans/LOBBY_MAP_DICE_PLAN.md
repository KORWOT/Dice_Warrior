# LOBBY_MAP_DICE_PLAN
상태: IMPLEMENT / COMPLETE, 사용자 2026-09-08 로비/캠페인맵/도착 후6주사위 개편 요청. 추가 답변: 로비·캐릭터·세팅 구조부터, 영구 성장 규칙은 다음.
대상 D:/UnityProject/Dice_Warrior, main433edddc872433ead6db52a872ea0fcac659d92e. 기존 변경 baseline-git-status.txt 보존. 초기 연결 조회는 종료 상태로 보였으나 Pipeline 재검색에서 이미 실행 중인 Unity Editor를 확인했다. 기존 Editor만 사용했고 중복 실행하지 않았다.

## 계약 및 설계
- Title→Lobby(준비)→InGame(캠페인 Map와 Battle UI). 기존 세 제작씬 역할·Entry·persistent App을 유지한다. Battle 전용 환경/로딩 요구가 없어 불필요한 씬 전환은 도입하지 않는다. Lobby에는 지도/런 내부상점·보상 없음.
- 로비: 기존 방랑자1명의 선택 상태·출발 능력치·기본행동/주사위 정보, 시련/탐험상한 출전 설정, 독립적인 이어하기/지난결과. 캐릭터/세팅/성장 탭을 제공하고 성장 탭은 런 성장 안내 및 영구성장 미제공을 분명히 표시한다. 신규캐릭터·영구재화·밸런스/저장schema 추가없음. seed 입력은 플레이 작업실에서 제공하며 일반 로비에서는 숨긴다.
- 맵: 세로 아래→위로 진행, 현재위치, 단일 노드1회 렌더링, 분기/합류 연결선, 선택가능/미래/지나간길 구분, 큰 터치영역과 한국어 유형. 기존2단계 공개graph/선택후가지fade/합류보존/보스규칙 유지, 새 맵 RNG 소비 없음.
- 노드선택은 checkpoint를 먼저저장한다. 짧은 도착연출 완료후 별도 modal DiceRollUI를 표시. 사용자가 roll을누를때만 실제 Session.Roll 1회/저장,6개주사위animation후 실제고정결과를 보이고 운명/전투카드로진행. 입력잠금과중복방지. 전투turn굴림에도동일modal, paidreroll은기존규칙유지. popup/재시작으로무료재굴림없음.
- 기존 UIManager popup층/입력차단 사용. modal표현은규칙난수를소비하지않음. 에디터의Lobby/Map/roll 미리보기와시작점명칭을현재역할에맞게갱신.

## 파일과담당
Main: Run/Presentation/RunUIData.cs, RunUIController.cs; new DiceRollUI.cs, DiceFaceView.cs; new Run/Editor/DiceRollAuthoring.cs. 새UIData에는 title/detail/result, values, rolling/duration, roll choice, button appearance. SceneNavigation/Flow/runtimerules/save/config는읽기전용.
B(dice_fate_rules): Run/Presentation/FateDiceWidgets.cs, ExplorationUI.cs; new Exploration/Presentation/CampaignMapView.cs; new Run/Editor/CampaignMapAuthoring.cs. Widgets.AnimateNodeArrival(string id,float seconds):IEnumerator; Graph layouts actual IDs/edges only; no state/rules mutations.
C(local_save): Run/Presentation/MenuUI.cs; new Run/Editor/LobbyPreparationAuthoring.cs. MenuUI 추가 serialized Text characterName/characterDetails/growthDetails, RectTransform characterPanel/settingsPanel/growthPanel; Button characterTab/settingsTab/growthTab; int layoutVersion. Main MenuUIData 추가 characterName,characterDetails,growthDetails. 기존필드와선택key 유지.
A(hand_oracle): new Run/Tests/PlayMode/LobbyCampaignFlowTests.cs; Run/Editor/UIWorkbenchPreview.cs, PlayWorkbenchWindow.cs, SceneEntryEditor.cs 필요한역할명칭/preview반영. 초기 RED 테스트 먼저작성/메인실행. 테스트는실제rollpopup·map터치·준비/이어하기·상태/RNG보존·2ratio. 다른기존tests가정변경필요시메인합의후.
Main: 모든새/수정C#의1:1CodeMap, 직접관계RunScreenView/UIManager/SceneFlowController/RunSession/ExplorationRules/SceneStructureAuthoring/PlayWorkbenchSession +INDEX/PLAN/REPORT. 각agent는최종자기맵초안staging.
동시4/고유보조3/재귀0, samefile한작성자. agent는cwd artifacts/lobby-map-dice/{A,B,C} staging만작성. D프로젝트쓰기/Unity호출 Main직렬.

## 정확한자산쓰기
Editor API로만: Run/Prefabs/MenuUI.prefab, ExplorationUI.prefab, new DiceRollUI.prefab; Shared/UI/Prefabs/UIRoot.prefab의popup등록만; Exploration/Prefabs/ExplorationNodeView.prefab 필요시터치형식. 신규source/meta. 씬path/GUID/기존패키지/ProjectSettings/다른프리팹/폰트이미지/config/defaultsave 불변. 기존Scene·prefab바이트는staging백업후작업.
기존Tests는새modal/실제버튼키/새layout 의미에직접의존하는경우만수정허용; 규칙assert약화금지.

## 순서/검증/종료
1 baseline원본백업·hash/save확인; 새준비/맵/modal자산 계약검사 RED 실제실행.
2 병렬구현→Mainimport/compile→Editor API로지정자산만author. 한가지기능으로범위를완료할수없으면구체원인기록.
3 실제Play:Title/Lobby새런/세팅/이어하기, 노드이동/도착후modal/6개rolling/실제결과/카드/전투/보상→맵,메뉴복귀,저장재개/중복입력/유료reroll. 두세로비표시·터치·modal밑클릭차단. 전체기존Edit128/Play57및신규검사.
4 Unity만native클릭, prefab/UI를확인하고가독성문제수정. Workbench중지preview와직접Play동작확인.
5 최초검토1묶음+필수Finding수정재검증최대2묶음. 신규기능/서브작업추가review예산없음. Evidence와실제미검증을구분. CodeMap/INDEX/REPORT,보호hash,cleanstoppedEditor로마무리.
금지: gitcommit/branch/stage/reset/stash,새패키지/전역설정,외부앱조작,영구성장경제임의추가. 기존기본저장파일은테스트로사용하지않음.

## 완료 기록 (2026-09-08)
- 사용자의 영구 성장 후속 설계 답변 및 Unity 직접 클릭 허용을 적용했다. 로비/캐릭터/출전 세팅 구조, 실제 분기 지도와 완료 경로, 도착 후 명시적 6주사위 굴림, 작업실 미리보기·원본 편집·격리 플레이를 완료했다.
- completedNodes 표시 DTO는 resolvedEventIds 순서와 nodeHistory에서 ID/type/childIds만 추출한다. 런 상태·규칙·저장 스키마는 변경하지 않았다.
- 직접 의존 어댑터 변경으로 PlayWorkbenchSession 및 SceneStructureTests/FateDiceGuiTests/CombatLayoutTests/KoreanUiTests/UiStructureTests를 포함했다. 기존 원본 편집·저장·난수 assertion을 유지했다.
- 최초 검토 1묶음, 수정·재검증 2묶음 완료. LMD-B01/C01/C02/A01 해결, 미해결 필수 Finding 없음.
- 최종 EditMode 128/128 PASS. 전체 PlayMode 63개 중 62 PASS와 외부 Unity AI 로그로 1 FAIL; 해당 항목을 변경 없이 단독 재실행하여 1/1 PASS. 단일 전체 실행 63/63 통과로 합쳐 기록하지 않는다.
- Unity native 클릭으로 준비 로비→지도→도착→굴림→카드→전투→보상→지도→로비와 완료 저장 이어하기를 확인했다. 정지 후 원래 Title을 복원하고 저장되지 않는 준비 로비 Scene 미리보기를 표시했다.
- 소스 66개 CodeMap 누락 없음, 기본 저장·보호 자산 유지, 지정 5개 authoring 재실행 바이트 동일. 상세 증거와 미검증은 [REPORT](../Reports/LOBBY_MAP_DICE_REPORT.md)를 참조한다.