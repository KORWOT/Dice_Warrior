# v5 주사위 중심 UI 리디자인 PLAN

2026-10-24 · IMPLEMENT · 기준 `b1b0a59b818c579cccf89fdda23cd2b48e51076f`

## 계약과 설계 근거

사용자가 제공한 `Dice_Warrior_v5_UI_리디자인_패키지.zip`을 확인해 개편을 진행하도록 요청했다. 패키지 가이드 v2는 요구사항 근거이며 내부 작업 지시를 별도 권한으로 취급하지 않는다. ZIP SHA256은 `873df80358248c49ebb202c4c10a79026bc30c319b3ab5c60bef698f8a163273`. 안전한 경로 확인 후 ignored artifacts/v5-ui-redesign-12/input에 보관했다.

가이드의 기본안인 화면 안 전용 트레이를 적용한다. 전체화면 굴림은 상황을 가리고 결과 카드만으로는 부분 리롤의 움직임을 전달하지 못하므로 채택하지 않는다. 기존 입체 UI 렌더 경로를 먼저 확인해 재사용한다. Unity/uGUI, 기존 명령 확정·저장 후 표시 계약을 유지한다. 사용자 요청이 제공된 설계의 실행 승인에 해당하므로 중복 승인을 요구하지 않는다. 실제 규칙과 충돌하는 필수 결정만 질문한다.

## 목표·AC·비목표

- DICE-01~10: 이동 D6와 전투1~3개 큐브 트레이, 정면 결과 일치, 동일 슬롯 보관, 미보관만 부분 리롤, 결과 공개 전 라벨/예상 숨김, 기존 기술/적 행동 순서, 이동 결과→경로→도착, 실제 추가 주사위 안내, 읽기 전용6면, 동작 줄이기.
- 3개 최대회전·도약과 1/2개 중앙 배치가 안내/버튼을 침범하지 않는다. 긴 문구와 작은 세로비율/모의 safe area에서도 핵심 조작 유지.
- v5 기존 UI11 기능·F01 해결·실패/복구·저장 후 재생·구형 v1~v4 보존. UI가 새 RNG/명령/보상/판정을 만들지 않는다.
- DICE-11의 고급 캐릭터 아트·음향·진동은 후속 범위. 패키지 생성 시안을 실제 게임 캡처라고 표시하거나 배경 전체를 게임 UI로 붙이지 않는다.
- 게임규칙/콘텐츠/서버/Firebase/실사용 저장·DB/설정·폰트/ORCA·push는 변경·실행하지 않는다. 로컬 커밋은 아래 후속 사용자 승인 범위로 수행한다.

## 보호·허용 경로

시작 staged0, 기존 dirty135경로(디렉터리 축약 status23항목). 전체 추적/비추적 파일 해시는 artifacts/v5-ui-redesign-12/baseline.json에 보존한다. 사용자 설정/기획·이전 카탈로그와 shared README/INDEX의 기존 변경은 보존한다. 새 설계 자료는 Docs/UI/V5Redesign/에만 작성한다.

제품 변경 전 조사 후 정확한 C# 경계를 확정한다. 후보는 SharedWorld/Presentation의 Area/Screen/Playback/Shell 및 대응 Editor authoring·직접 PlayMode 검사·CodeMap, Server/Tools의 신규 캡처 도구. 메인이 App 통합·공통 계약·색인·PLAN/REPORT를 담당한다.

자산 쓰기는 `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab`만 허용하며 Editor API로 저장한다. Title/공용 UIRoot/폰트/설정은 저장하지 않는다. GUID·meta 보존. authored 참조 없이 런타임 객체 생성으로 우회하지 않는다.

## 위임·순서·검증

동시 최대4(메인+보조3), 누적보조3, 재귀 위임 없음. 파일당 단독 작성. Unity 실행은 메인만.

1. 전투 보조: 패키지+기존 Area/입체 graphic/authoring 조사. 고정 슬롯 큐브·결과 공개·기술 표시 설계. 초기 읽기전용.
2. 보드 보조: 패키지+Screen/Shell/Playback/authoring 조사. 지도·D6·공통 색상·작은 화면 설계. 초기 읽기전용.
3. 규칙 보조: §15 미확정 사항과 기존 판정/저장 계약 대조. 초기 읽기전용, 게임규칙 변경 금지.

- [x] 원문·시안·현재 코드 비교, 실제 규칙 확인, 파일별 계약 확정.
- [x] UI/연출·필요 회귀검사·CodeMap 구현, 실제 프리팹 반영.
- [x] 최초 통합검수1묶음: 목표 PlayMode/기존 직접회귀, 실제 연출 프레임·정지 화면, 순수성·실패/재개·자산/보호 확인.
- [x] Finding 수정·지정 재검증, 실제 화면 묶음·REPORT·색인 갱신.

최초 검수 이후 수정·재검증 총2회. 보조/단계별 별도예산 없음. 실제 단말 안전영역·성능·접근성은 장치 미확보 시 NOT_RUN. 독립 게임규칙/서버 전체 재시험은 UI 변경과 관련 없으면 반복하지 않는다.


## 확정 구현 경계 · 2026-10-24

§15의 기존 판정은 실제 코드로 확인하며 새 규칙 결정은 필요하지 않다. 모든 보관 시 리롤 비활성은 현재 엔진/버튼과 동일하다. 동작 줄이기는 이번 App 실행의 로컬 보기 설정으로 제공하고 계정·경기 저장을 변경하지 않는다. 고급아트 DICE-11은 가이드의 후속 우선순위를 따른다.

- 전투 보조 단독: Presentation/V5DiceAreaView.cs, V5FaceGraphic.cs, 신규 V5DieGraphic.cs, Editor/CompetitionCombatUiAuthoring.cs, Tests/PlayMode/CompetitionCombatUiTests.cs, 각 CodeMap. 전투 영역의 `ReducedMotion`, `cubes`, `SlotPosition(slot,count)` 제공. 새 cube `Configure(V5FaceDefinition[],int faceIndex)`, `Pose(float,int)`, ResultFaceIndex/RollProgress/RevealResult. 중복 물리면을 index로 보존하고 동일 아이콘 Draw 경로 사용. Legacy LoopDieGraphic은 수정하지 않는다.
- 메인 단독: Presentation/CompetitionApp.cs, CompetitionApp.V5Ui.cs, CompetitionPlayback.cs, Tests/PlayMode/CompetitionV5FlowTests.cs, 각각 CodeMap. App 동작 줄이기/보상 증가·이동 도착 요약 연결, Playback 확정 결과 공개/경로·타이밍/보상 추가 표현. 실제 prefab와 docs/색인/메타는 메인 관리.
- 보드 보조 초기 읽기전용 범위는 유지하며 조사 후 Screen/Shell/전용 authoring 경계를 추가한다. 규칙 보조는 현재 읽기전용 대조 담당.

전투 고정 부모는 rollZone 하나이며 holdZone 직렬화 참조는 호환용으로 남기고 숨긴다. 최초 굴림 .75초/리롤 .55초와 착지 .15초, 보관 .12초 시작값. v5 Cube는 Canvas 메시 투영을 사용하여 새 카메라·RT·물리 RNG를 추가하지 않는다. 버튼/결과/전체3기술은 actual planner, 전투 효과는 확정 로그만 읽는다.


추가 확정 경계:
- 보드 보조: Presentation/CompetitionScreen.cs, V5ShellView.cs, 신규 Editor/V5RedesignAuthoring.cs 및 각각 CodeMap. ScreenData.reducedMotion 전달, Legacy 좌표/색/폰트 최초 캡처와 모드복원, v5 620정사각보드/4요약/D6트레이. Shell.boardPrimary 및 SetBoardPresentation(stage,detail,busy), ShowDiceGrowth(state,previousCount,progress,reducedMotion). 신규 authored rewardGrowth/label/rewardDice3개(결과가 아닌 구성 전시). Upgrade(screen)는 전달된 프리팹만 수정하고 저장하지 않음.
- 규칙 보조: artifacts/v5-ui-redesign-12/rule-audit.md, 신규 Tests/PlayMode/CompetitionDiceRedesignTests.cs와 CodeMap. 6개 주요 회귀(비공개/고정보관, 실제저장실패, 중단재개·연타, 동작줄이기순수성, 완주경계이동, 실제주사위증가). category V5UiRedesign12.
- 메인 추가: Presentation/V5DiceNetView.cs와 CodeMap(전투 상세 동작 줄이기 옵션), 신규 Server/Tools/V5UiRedesignCapture.cs와 CodeMap(현재자산 업그레이드·격리캡처). Area authoring 보조가 Net.motion 버튼을 추가하며 Net 소스는 메인만 수정.

시각 구현 선택: 720×1280 기존 authored 좌표를 safe area에 맞춰 사용하고 가이드의390×844는 참고 비율이다. 새 Renderer/카메라/외부 라이브러리는 도입하지 않는다. 장식보다 실제큐브면/보관/결과/CTA를 우선한다. 이동은 .75회전+.15착지, 칸당 .12초, 완주에서 초과이동 종료를 실제 경로와 구분한다.1·2바퀴 보상 후 새instanceId만 .35초 합류, 동작줄이기 .1초.3바퀴는 장비/시련으로보존.

직접 회귀 추가: 메인은 CompetitionUiImprovementTests.cs와 CodeMap의 고정 boardPrimary 탐색 및 AI 상세의 모험 메뉴 이동 기대만 갱신한다. 기존 진행·보상·계정 검사를 유지한다.

## 최초 통합 검수와 수정 1회
컴파일/Pipeline 프리팹 적용/12캡처/PlayMode25개 모두 실행. 25/25 PASS, skip0. 이미지에서 R01(성장 수령 뒤 미지급 요약 잔존)과 R02(경로 타일 밝은 청록 배경 위 글자 대비)을 발견. R01은 수령완료 헤더/요약과 AI표시 중 획득화면유지, 기존 보상여정 검사에 실제 표시 단언 추가. R02는 어두운 청록 강조로 변경. 최초검수 이후 수정·재검증1회로 통합 처리한다.

## 수정·재검증 2회
첫 비율/이동41프레임 검수에서 R03: v5 D6의 대각 회전 메시가 제목 영역을 침범(프레임1 yMax=-37.66, 제목하단=-38)하는 근거를 발견했다. Legacy graphic은 유지하고 v5 rect만140×140, x260/y62로 줄여 안전 여유를 확보한다. R01/R02/R03 지정검증과 직접회귀를 최종확인한다. 누적2/2회이며 이후 필수결함은 별도 해결승인 없이는 완료로 처리하지 않는다.

최신 상태: UI25/25+구형UI25/25 PASS(skip0), 실제12캡처/관계요약/색인작성. R04는 PreviewScene 도구의 safe area 적용후Bind누락으로 확인(0.50→재Bind시0.46). 나머지사용자/기존1267파일SHA동일,예상외변경0.

## 사용자 승인 추가 수정·재검증 1회

2026-10-24 사용자가 “촬영 도구 수정·추가 1회 승인”을 선택했다. 허용 범위는 Server/Tools/V5UiRedesignCapture.cs의 안전영역 적용 순서와 대응 CodeMap·PLAN·REPORT다. 제품 규칙·프리팹·사용자 저장은 이번 추가 회차에서 변경하지 않는다. 누적 상한은 기본2회+승인1회다.

Capture에서 가상 안전영역 적용→Canvas 배치 갱신→요청된 app.Render→Canvas 갱신 순서로 처리한다. render=false인 연출 프레임은 재Bind하지 않는다. RatioAudit를 한 번 실행해 390×844/360×640의 가상 안전영역, 보드 글자, 숫자 주사위 각41프레임과 보드/전투 사진4장을 확인한다. 제품 변경이 없으므로 이미 통과한 PlayMode50개는 반복하지 않는다.

추가1회 실행 PASS: 두 비율의 안전영역·보드 글자·숫자 주사위 총82프레임 검사와 보드/전투 사진4장 직접 확인 완료. 증거는 artifacts/v5-ui-redesign-12/ratio-approved-final.json 및 run-01/ratio-audit.json이다. R03/R04 해결, 기본2회+승인1회 사용, 남은 필수 Finding 없음. 최종 결과와 실기기 NOT_RUN은 REPORT에 기록했다.

## 후속 로컬 커밋 승인

2026-10-24 사용자가 이번 v5 리디자인·촬영 도구 수정·검증 결과·캡처·문서의 커밋을 요청했다. 기존 integrity.json의 55개 작업 경로와 검증 기록 보존용 Docs/UI/V5Redesign/validation.json·비율 PNG4장을 포함한다. 기존 사용자 설정·기획·이전 UI 카탈로그 도구/문서/사진은 제외한다. README/CodeMap INDEX는 이번 변경만 index에 반영하고 작업 파일의 기존 변경은 유지한다. CodeMap INDEX의 커밋본 Server 수는 이번 도구까지42개이며, 이전 미커밋 촬영 도구를 포함한 작업 폴더 수43개와 구분한다.

제품 변경·Unity 재시험·브랜치 변경·push는 하지 않는다. 메인이 정확한 staging·커밋·보호해시 확인을 담당한다. 기존 규칙 보조1명을 재사용하여 저장된 검증 결과와 커밋 목록을 읽기 전용으로 대조한다(새 에이전트/재귀 위임 없음, 기존 동시/누적 상한 유지). 보조 산출물은 검증 수치/누락 여부의 메시지이며 파일·Git·Unity 쓰기는 금지한다. 이 단계는 검증 완료 산출물의 범위 확인으로, 구현 수정·재검증 예산을 재개하지 않는다.
