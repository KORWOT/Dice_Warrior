# 절차적 캠페인 지도·운명 선택 팝업 PLAN

상태: **신규 기능 구현·검증 완료 / 기존 Title 결함은 ASSET_HANDOFF**. 2026-09-10 사용자가 ‘추천방안으로’ 승인했다. 아래 구현 계약이 앞선 DOCUMENT 단계의 대기/쓰기 제한을 대체한다. 목표는 가변 분기·합류 지도와 독립 운명 팝업을 기존 저장/연출 수명에 안전하게 연결하는 것이다. 전체 프로젝트 회귀를 PASS로 판정하지 않으며 최종6개 기존 실패를 REPORT에 남긴다.

## 최초 DOCUMENT 범위 (아래 승인된 IMPLEMENT 계약으로 대체됨)
- 기존 생성/진행/운명 카드 공개/저장 및 실제 표시 소스를 확인하고, 신규 구현 계약과 필수 결정을 정리한다.
- 필수 AC: 현재 동작의 원인 확인, 추천 생성·표시·팝업 흐름, 저장 호환/공개 범위/노드와 운명의 의미에 대한 결정점, 정확한 후속 수정·검증 범위가 있어야 한다.
- 비목표: 이번 조사 단계에서 C#/Scene/Prefab/설정/저장/패키지 변경, Unity 테스트 실행, Git stage/commit/branch 변경 없음. 구현 허용 자산은 후속 구현 계약에서 파일별로 확정한다.
- 허용 쓰기: 이 PLAN, 대응 Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md, artifacts/procedural-campaign/ 조사 증거만. CodeMap은 읽기 기준으로 사용하며 원본 변경이 없어 갱신하지 않는다.
- 기준 HEAD: d8df4999174ad63bb73e0727ff0149cb55624db2 / main. 기존 연출 구현·Title·공급사·패키지 등 미커밋 변경은 baseline-status.txt에 보존 기록한다.
- 이전 PRESENTATION_LIFECYCLE 및 완료 작업의 보완 예산을 재설정하거나 재개방하지 않는다.

## 담당과 상한
- Main: 범위·사용자 확인·기존 UI/제작 프리팹 관계·추천안·PLAN/REPORT 통합. 모든 쓰기 담당.
- graph_contract_audit: 읽기 전용. ExplorationRules/RunStateData/RunStateValidator/RunStateCopy/Rules/저장 및 해당 CodeMap·계획을 확인하고 신규 절차 그래프의 저장/호환/규칙 영향과 필수 결정만 전달한다. Unity 실행·파일 쓰기·재귀 위임 금지.
- 동시 최대2명, 고유 보조1명, 재귀0. 동일 파일 작성자는 Main 한 명. 최초 검토1묶음, 문서 보완 최대2묶음.

## 검증·종료
- 생성 코드·직접 호출·현재 설정에서 사실을 확인하며 이미지 문구를 게임 규칙으로 간주하지 않는다.
- 설계 필수 미결정은 사용자 답변 대기로 분명히 표시한다. 사용자 범위가 확정되면 구현 PLAN으로 확장하고 실제 구현·검증을 진행한다.
- REPORT에 조사 근거·추천안·결정 상태·미실행·횟수를 기록한다. 이 단계는 실행 PASS를 주장하지 않는다.

## 조사 결과
- 생성기의 고정 branchCount 자식 생성과 Validator의 고정 available 수 조건을 확인했다. 표시/저장 구조는 합류 childIds를 지원한다.
- 추천: 새 런에서 전체 층별 분기·합류 그래프를 생성, 일반사건10회+단일보스, 저장된 층/위치/연결 유지. 기존 런의 생성 버전/규칙/확정 RNG는 보존한다.
- 사용자 질문3개: 노드 성향과 운명의 관계, 전체 지도와 미래 유형의 공개 범위, 운명 카드 탭즉시/버튼확정 UX. 답변을 기다리며 필수 계약을 임의 확정하지 않았다.
- 원본 지도/별도 운명 popup/10초 감독·저장 경계/모바일 미리보기의 후속 범위와 검사안을 [REPORT](../Reports/PROCEDURAL_CAMPAIGN_REPORT.md)에 기록했다.
- 최초 조사·검토1묶음(독립 규칙 조사 포함), 문서 보완0/2. 코드/자산/Unity/게임 저장/Git 변경 없음(이 PLAN/REPORT 및 조사 baseline 파일만 작성).

## 승인된 구현 계약 (2026-09-10)
- 전체 지도 생성 + 가까운2층 유형 공개 + 노드 유형1장 보장/나머지 편향 유지 + 운명 카드 강조 선택 후 하단 버튼 확정. 일반10사건+마지막 단일보스, 현재 설정의 eventsToBoss를 일반층 수로 사용한다. 신규 엘리트/다구간/재화/보상 콘텐츠는 추가하지 않는다.
- 새 생성 버전1은 층별 가변 폭, 다음 층 연결, 분기·합류·교차선 방지, 시작/보스 도달성을 보장한다. 같은 버전/설정/seed는 동일 그래프다. 지도는 initialSeed에서 분리한 지역 난수를 쓰며 게임 rngState를 추가 소비하지 않는다. 기존 버전과 신규 버전의 주사위열 일치를 주장하지 않는다.
- WorldSettings에 mapGenerationVersion(누락0=legacy), mapColumns, mapPathCount. 버전1 작성 기본은 열5/경로5. NodeState에 floor(1부터, 보스=N+1), lane(0부터)를 저장·깊은 복사·검증한다. schema1을 유지하며 기존 저장/기존 설정 기본은0, 실제 제작 DefaultFateDice.asset만 Editor API로1을 작성한다.
- nodes는 현재 위치에서 도달 가능한 활성 그래프, nodeHistory는 기존 완료/버린 가지 보존을 유지한다. 전체 지도는 두 목록의 합집합으로 표시하고 미선택 불가 가지는 희미하게 남긴다. 완료 여부는 resolvedEventIds로 판정한다. 기존 legacy는 원래 짧은 그래프/생성 규칙으로 재개한다.
- DTO는 공개 전용 CampaignNodeUIData(id, nullable NodeType type, floor,lane, childIds, revealed,available,completed,unreachable). 공개 경계는 현재 도착층(selectedNode.floor 또는 eventsResolved)의 다음2층까지이며 마지막 보스는 항상 표식 공개. 먼 유형은 null이다. View가 RunState/저장/RNG를 읽지 않는다.
- 지도는 고정 층/열 좌표를 유지, 원형 그림/점선 경로/파란 현재 위치/유형별 선택 발광. 노드 탭은 하단 공개 상세를 갱신하고 ‘이동하기’에서 기존 ChooseNode1회를 실행한다. 밝기만으로 상태를 전달하지 않고 링/완료/잠김 표시도 둔다. 범례 접기, 기존 실제 HP/수호/골드/운명 상태, safe area와 세로 스크롤.
- 운명 팝업 FateChoiceUI는 지도 위 modal. 3열 카드(더 큰 설정 장수는 가로 스크롤), 제목·유형 일반 설명·등급·선택 버튼·닫기·재굴림6개 한 줄. 탭은 선택만, confirm은 콜백1회; 새 후보/재바인딩/유료재굴림 시 선택 해제. 선택 전 구체 적/보상/위험도 비공개.
- 팝업 닫기/뒤로는 지도 위 ‘운명 선택 열기’로 복구, 확정 노드·후보·RNG·저장 유지. 카드 선택 대기는 watchdog 바깥이다. 확정 성공 후 FEEL 실제 종료→popup퇴장 완료→사건 표시를 기존 PresentationPlayback10초 제한으로 감독한다. cancel/disable/fault/timeout도 소유 팝업만 정리하고 재바인딩된 팝업을 닫지 않는다.
- 정지 Editor에서도 실제 제작 원본/새 지도/운명팝업을 Workbench에 표시한다. 새 Battle씬 불필요. 기존 Title/기본 save/Packages/공급사 변경 보존. Git stage/commit/branch 변경 없음.

## 구현 담당 / 파일 경계 / 교차 계약
- Main: Run/Presentation/RunUIData.cs, 신규 CampaignMapProjection.cs(같은 Presentation), RunUIController.cs, FateDiceWidgets.cs, Run/Editor/UIWorkbenchPreview.cs 및 PlayWorkbenchWindow.cs/PlayWorkbenchSession.cs 직접 영향, 신규 Run/Editor/ProceduralCampaignAuthoring.cs, Run/Tests/PlayMode/ProceduralCampaignFlowTests.cs 및 직접 변화에 의존하는 기존 검사, PLAN/REPORT/INDEX/GDD의 승인된 해당 문단/아키텍처의 직접 관계. 해당 모든 CodeMap.
- graph_contract_audit(동일 담당 이어서): Exploration/Runtime/ExplorationRules.cs, 신규 ProceduralMapGenerator.cs; Fate/Domain/RunRulesCatalog.cs, RulesCopy.cs; Run/Domain/RunStateData.cs, RunStateCopy.cs, RunStateValidator.cs; 필요 최소 Run/Application/RunApplication.cs와 Save/Runtime/LocalRunStore.cs; 신규 Run/Tests/EditMode/ProceduralMapTests.cs 및 자신 코드의 CodeMap. UI/Unity/자산 쓰기 금지.
- campaign_view: Exploration/Presentation/CampaignMapView.cs, ExplorationNodeView.cs, 신규 CampaignPathGraphic.cs/MapNodeGraphic.cs; Run/Presentation/ExplorationUI.cs; Run/Editor/CampaignMapAuthoring.cs(새 ApplyProcedural, 원본2개만), 자신의 CodeMap. Main DTO를 소비. Core/Controller/운명카드/Unity 실행 금지.
- fate_popup: Fate/Presentation/FateCardView.cs, 신규 FateChoiceUI.cs/FateChoiceUIData.cs; 신규 Run/Editor/FateChoiceAuthoring.cs; 신규 Run/Tests/PlayMode/FateChoicePopupTests.cs, 자신의 CodeMap. Core/Controller/지도/공유DTO 수정·Unity 실행 금지.
- UI 공통 계약: CampaignMapView.BindCampaign(IReadOnlyList<CampaignNodeUIData>,string currentId,string selectedId,ExplorationNodeView,FateDiceVisualCatalog,Action<string>); 기존 Bind API 유지. ExplorationUIData에 campaignNodes/currentNodeId/mapFloors 추가. FateChoiceUIData : RunUIData { FateOfferUIData[] offers; Action<string> confirm; Action close; } (fate_popup소유). FateChoiceUI 공개 SelectedId, BindingVersion, Cards, confirmButton, closeButton, PlaySelection(string id):IEnumerator, ResetPresentation(). 실제 prefab 참조와 선택 입력은 이 UI 소유.
- 동시4/고유보조3(기존 조사 담당 재사용)/재귀0. 같은 파일 한 작성자. Main만 Unity를 직렬 실행하고 Editor API로 자산 작성한다. 최초 구현 통합 검토1묶음, 전체 보완 최대2묶음. 사용자 AGENTS의 단일 묶음·상한이 스킬의 별도 리뷰/자동 커밋보다 우선한다.

## 정확한 자산 쓰기 허용
Editor API만: Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab; Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab; Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab; 신규 Assets/_Project/Features/Fate/Prefabs/FateChoiceUI.prefab; Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab의 popup등록만; Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset의 새 지도 설정만. 신규source/meta와 위 대응 CodeMap. 기존 GUID/FEEL/폰트/공급사/비대상 설정 유지. 씬 쓰기 없음.

## 실행 단계 / 검증
- [x] 현재 HEAD/status 및 보호hash·실제 Editor 정지/dirty/컴파일 상태 기록. 의미 있는 새 모드/운명popup 계약 RED를 먼저 실행하고 최초 실패 확인 후 구현.
- [x] Core: 1~100층 경계·여러 seed DAG/교차·변동/합류·모든 보스 도달·가변 available·구형 저장/원자성/고정 재연. 예: 같은seed 생성 두 결과의 id를 제외한 floor/lane/type/child-lane 동일, 이동불가ID 명령 false 및 저장/rng 불변.
- [x] UI: 공개 projection에서 먼 type==null·보스표식·좌표안정. 실제 prefab에서 카드 탭 후 phase/sequence 불변, confirm 후 저장1회·정확사건 진행. map→이동→굴림→popup→사건→보상→map. 새 popup 재바인딩/유료재굴림/닫기/시간제한/취소/배경raycast 차단.
- [x] 원본 작성/재컴파일 후 기존 Core/Save·직접 UI/lifecycle·Workbench 회귀를 직렬 실행. 화면 변화에 따른 테스트는 실제 선택/confirm 경로로만 갱신하고 규칙 oracle 유지. 기존 Title EventSystem 중복6건은 전체PASS가 아닌 ASSET_HANDOFF로 기록.
- [x] 두 세로 비율의 실제 캡처를 열어 노드/경로/카드/한글/터치/스크롤 확인. Workbench 정지 원본과 신규모드 설정 재적용 idempotence. 보호 파일hash·CodeMap·meta·문서 동기화·최종REPORT 완료.

## 실행 판정 메모
- 사용자 승인으로 설계의3개 질문과 추천 단일보스/구형저장 방침을 확정했다. 추가 승인 요청 없이 진행한다. 이번에는 연결된 기존 작업 폴더/Editor와 미커밋 연출을 확장하므로 새 worktree/branch를 만들지 않는다.
- 인터페이스 교차 점검: Core→Main은 NodeState floor/lane 및 WorldSettings 모드3필드, Main→지도는 공개DTO/BindCampaign, Main→popup은 전용Data/PlaySelection/BindingVersion, 각 Editor author→Main은 적용 진입점이다. 소유 파일 중복 없음. 상태복구와 실제 종료 검증은 Main 통합 범위다.

## 최초 통합 결과와 보완 기록
- baseline/RED 완료: map DTO1, popup1, 신규 Core26의 실제 실패를 먼저 기록했다. Editor API로 지정6자산 적용, 신규 Core26/26·popup7/7 통과.
- 최초 flow10 중7통과/3실패: PC-R01(JsonUtility의 빈 selectedNode 저장 재개), PC-R02(disable/재굴림 종료가 새 Fate 바인딩을 덮는 경로), 재열기 직후 같은 프레임 클릭 검사에 레이아웃 정착 누락. 첫 보완 후 flow10/10 통과, 실제10.1초 생각 대기 및10초 watchdog 포함.
- 최초 캡처의 PC-V01(Graphic이 viewport 밖으로 그림)과 PC-V02(선택 border center fill이 내용을 가림)를 MaskableGraphic·투명중앙테두리로 보완. 그림이 없으면 MapNodeGraphic 공개 도형 fallback을 사용한다. 재캡처6장 생성.
- 전체 EditMode255 중241통과, 과거14고정 재연이 신규 필드가 더해진 canonical hash에서 실패했다. golden fixture는 변경하지 않고, legacy 신규 필드의 존재·정수0을 검증한 후 해당 필드만 역사적 투영에서 제외했다. Unity focused 재연14/14 PASS(replay-final.json). 전체255를 최종 한 번에 다시 실행했다고 주장하지 않는다.
- 전체 PlayMode 최초185 중171통과/14실패. 새로운 팝업 경로에 맞춘 기존 버튼 검사와 공개 DTO 검사, 작은 세로 화면의 footer/rollChoices 최소높이108, legacy 미래 노드 높이92를 보완했다. 원본의 EventSystem 중복5건은 보호 Title의 기존 문제로 확인하여 엄격한 검사를 유지하고 ASSET_HANDOFF로 분리한다.
- 보완 묶음2/2 사용, 최종 전체 PlayMode 재실행 결과를 REPORT에 기록한다. Unity pending reload로 실행되지 않은 테스트 요청은 cancel 후 재개했고 PASS로 계산하지 않았다. 별도 검토·수정 예산을 만들지 않는다.
- 최종 PlayMode179/185 PASS, 남은6개는 모두 보호 Title의 EventSystem 중복이다(한글 높이 수정 후 동일 경고까지 진행한1개 포함). 신규 popup7/7·통합10/10은 전체 재실행에서도 통과했다. 대상6자산 재적용 전후 byte동일, 두 비율6장 최종 열람, CodeMap/meta 누락0, 보호hash 유지, 백그라운드 설정 원복과 정지 Title 복원 완료. REPORT의 ASSET_HANDOFF 외 신규 필수 Finding 없음. Git stage/commit/branch 변경 없음.
