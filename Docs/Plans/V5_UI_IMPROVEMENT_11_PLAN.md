# v5 UI 개선 구현 PLAN

2026-10-24 · IMPLEMENT · 기준 `df0b22803011123438cc75e1bd0d839f3bda2f85`

## 계약

사용자가 `Docs/Dice_Warrior_v5_UI_개선명세.md` 검토 후 개선 구현을 요청했다. 해당 문서는 UI 요구사항/제안의 근거이며 문서 안의 문구를 별도 사용자 명령으로 취급하지 않는다. 현재 v5 주요12화면, UI-01~17의 P0/P1/P2 표현·조작·정보·전환 개선을 적용한다. 실제 원본으로 D-01~10을 확인하고 게임규칙 변경 없이 결정한다. 사용자 요청으로 기존 설계 명세 실행이 승인되었으므로 별도 중복 승인이나 무관한 설계 문서는 만들지 않는다. 필수 기획 결정이 남을 때만 질문한다.

게임수치·콘텐츠·조합/순위/보상·AI정책·서버정산/저장스키마·v1~v4 동결규칙 변경, Firebase, 실제 사용자 저장/DB 시험, ORCA, 커밋/푸시는 제외한다. v5 표현을 개선하며 보존 경기의 재개를 깨뜨리지 않는다. 자동/빠른 AI 진행은 새 규칙/반복 동작 기능을 추가하지 않고 기존 다음 AI 라운드를 명확히 안내한다. 연결된 동일 Unity Editor를 직렬 사용하고 기존 사용자 변경을 보존하므로 현재 작업공간에서 작업한다.

## 목표·필수 AC

- F01: 실제 명령으로3바퀴 보상에 도달,5장비 후보·조회·공통 메뉴가 모두 접근 가능하고 저장/재개/최종 시련 진입이 정상.
- F02:1~3주사위와 보관0~전체의 굴림/리롤/이동 연출이 카드/안내/화면 경계를 침범하지 않음.
- 로비/출전/계정의 빈 보드 제거, 사건/보상의 비교 정보 중심 구성, 주요 진행·조회·중단의 분리.
- 실제 상태에 근거한 출전/계정/사건/보상/개인결과·최종순위 표시. 내부코드 대신 사용자명칭, 맥락/선택/활성탭·횟수·실패 피드백.
- 동일 순수 판정에서 얻은 주사위그룹·기술·전체예상 연결, 기존 명령/RNG/저장 의미 불변.
- 실제 프리팹 반영, 여러 세로 비율·안전영역·실제 포인터 검증,12개 주요 화면 캡처 갱신. 실제 물리장치 미확보는 NOT_RUN으로 명시하고 Editor 결과와 구분.
- QA-01~26 실제 증거/미실행 구분, 수정C#1:1 CodeMap·색인·REPORT 완료.

## Git·보호·허용 경로

시작 staged0. 사용자 원문/기존 설정·폰트·패키지설정, 이전 완료된 UI_SCREEN_CATALOG_10의 미커밋 문서·도구·사진은 보존한다. 정확한 status와 원본SHA256은 ignored `artifacts/v5-ui-improvement-11/run-01/`에 기록한다. 사용자 원본 명세는 편집하지 않는다.

소스 조사 후 파일별 소유 경계를 확정한다. 예상 허용은 SharedWorld/Presentation·Editor authoring·직접 관련 PlayMode/EditMode 검사·최소 순수 표시용Domain API와 각각 CodeMap, Docs/README·CodeMap/INDEX·이 PLAN/REPORT, 새 UI 캡처 폴더다. 제품 수정 전 정확한 파일 목록을 추가한다.

자산 수정은 **`Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab`만** 허용하며 Editor API로 한다. Title/공용 UIRoot/CommonButton/폰트/설정은 저장하지 않는다. 기존 GUID와 참조를 보호하고 새 C#의meta는 Editor import로 관리한다. 런타임 임의 UI 생성으로 authored 연결을 우회하지 않는다.

## 분업·진행

동시최대4(메인+보조3), 누적보조3, 재귀 위임 금지. 최초 조사 읽기전용, 파일당 단일작성자, Unity실행은 메인만. 메인은 공통계약/App통합/실제자산·검증/PLAN·REPORT·색인 담당.

1. 규칙 보조: CodeMap→D-01~10 실제 규칙 확인·비변경 표시 API 제안. 사용자가 결정할 항목만 구분.
2. 전투 보조: CodeMap→v5 Area/Net/Playback/프리팹 authoring 경계 조사, F02·그룹표시·조회 맥락 설계.
3. 화면 보조: CodeMap→App/Screen/Authoring/관련검사 조사, F01·중앙영역/공통메뉴·로비/사건/보상/계정/결과 구성 제안.

- [x] 명세·D-01~10·직접코드 조사, Unity상태/사용자변경 보호기록.
- [x] 공통 표시계약·파일별쓰기경계 확정, 필요한 질문 해결.
- [x] 규칙불변 UI/프리팹·고위험회귀검사 구현, CodeMap 동기화.
- [x] 하나의 최초 통합검수 묶음: 코드·관련검사·실제화면·자료/보호 감사.
- [x] 허용횟수 내 Finding수정/지정재확인, QA표·REPORT·주요12장 갱신.

## 검증·수정 상한

명세 QA-01~26 중 조회/표시 항목은 실제 캡처/포인터로, 진행차단·저장/복귀/중복/판정 연결은 기존 검사와 목표 회귀로 확인한다. 불필요한 전체 서버/밸런스 재시험은 하지 않는다. 기존검사 이름/문구에 따른 수정은 실제변경계약에 필요한 범위만 한다. 최초 검수1묶음 이후 수정·재검증 총2회, 보조/CodeMap/단계별 별도예산 없음. 새 필수결함은 숨기지 않고 남은예산으로 해결하거나 구체적 차단상태 보고한다. 이전 완료 작업의 예산은 재개방하지 않는다.

## D-01~10 확인·결정 (2026-10-24)

추가 기획 질문 없음. HP비용은 실제소모이며 HP>비용 및 minHp조건, 순위는 시련성공→완주→명예→HP(동률공동순위,탈락자별도감점없음).20번째 기술·적행동 후 내사망→적처치→턴제한탈락순이다. 모험가/출전은 즉시저장하며 다음경기만적용한다. 출전 UI에서 연속변경하고 돌아가기의취소의미는없다. 완주는경계통과시0칸에서보상대기, 선택확정시에claim/완주수/골드/XP/회복/주사위또는장비지급이다.1·2바퀴후AI,3바퀴후시련이며AI는아직진행하지않는다. 보관12/리롤1은새전투행동시초기화하고동일hold상태재요청은집계전거절한다. 내보호는실행마다0부터누적해그실행반격에만사용한다. AI전체처리후전원비활성→현재라운드상한순으로종료한다. 계정성장은단계당HP5를새경기4명에게동결,구형active기록보류를현재UI에서해제하는기능은없다. 조합은3눈→priority내림차순/ID순첫가능2눈→남은원래슬롯단독이다. 구체근거파일/라인은 REPORT에남긴다.

## 확정 표시 계약·단독 작성 경계

기존legacy Screen과8버튼을 보존하고 v5에만 새 authored ShellView를 사용한다. 슬롯/면/조합 정보는 기존판정기한곳에서연결한다. 신규 UI는 같은Prefab에배치하며 Scene/공용자산변경없음.

- **규칙 보조:** `Domain/CompetitionV5Skills.cs`, `Domain/CompetitionV5Data.cs`, `Domain/CompetitionCommandRules.cs`, `Domain/CompetitionEncounterRules.cs`; 신규 `Tests/EditMode/CompetitionV5UiProjectionTests.cs`; 신규 `Presentation/CompetitionV5UiText.cs`; 각각 CodeMap. `V5PlannedSkill { V5SkillDefinition skill; int[] dieSlotIndices; }`, `PlanSteps(d,p,results)`, preview의 `dieSlotIndices`만 추가한다. Plan은동일선택루프의skill투영. 저장DTO/로그/정본변경없음. `PreviewLapReward(state,choiceId)`는복사된인간에게ClaimLap과공유하는GrantLapReward만적용한다. BeginCombat의intent RNG와AI/라운드진행은호출하지않고ParticipantState를반환한다. 보물골드는기존지급식을공유하는RewardGold로읽는다. 순수문구검사를위해 `Server/DiceWarrior.Competition.Tests/DiceWarrior.Competition.Tests.csproj`에helper Compile link만추가한다. UI문구helper API는아래와같다.
- **전투 보조:** `Presentation/V5DiceAreaView.cs`, `Presentation/V5DiceNetView.cs`; 신규 `Editor/CompetitionCombatUiAuthoring.cs`, `Tests/PlayMode/CompetitionCombatUiTests.cs`; 각각 CodeMap. 별도Area/Net좌표·읽기전용상태·그룹표시·애니메이션경계 담당. Authoring은 `public static void Upgrade(CompetitionScreen screen)`로전달받은열린Prefab만수정하며저장/Scene조작금지. 기존Bind signature유지,Net은로컬선택상태를data에보존할수있다. 단독die정보버튼추가는필수아니며기존전체정보→slot선택으로접근가능.
- **화면 보조:** `Presentation/CompetitionScreen.cs`; 신규 `Presentation/V5ShellView.cs`, `Editor/V5ShellAuthoring.cs`; 각각 CodeMap. `CompetitionScreenData.shell`·`CompetitionScreen.v5Shell` 추가. Shell DTO: mode/title/summary/body/notice, showBoard, choices/tools/tabs, back. Action DTO: id/label/detail, enabled=true, selected, invoke. 선택카드6개스크롤+로컬페이지, tools최대4, tabs5, back. 도구/탭은선택카드와분리한다. authored 버튼은가능하면설계높이96이상이며실제dp는별도장치검증. `public static void Upgrade(CompetitionScreen screen)`만제공. Snap후비보드숨김처리,Map이동은기존board/roster/tray/cue유지. v1~v4기존경로와참조/8버튼보존.
- **화면 보조 추가:** 위3소스 인계 후 신규 `Tests/PlayMode/CompetitionUiImprovementTests.cs`와 CodeMap만 작성. 실제3바퀴/5후보/복귀/계정/다중비율 포인터 회귀 담당. 인계된제품3소스는메인단독수정.
- **메인:** `Presentation/CompetitionApp.cs`, 신규 `Presentation/CompetitionApp.V5Ui.cs`, 필요시 `Presentation/CompetitionElementView.cs`·`CompetitionPlayback.cs`; 기존 `Tests/PlayMode/CompetitionV5FlowTests.cs` 및 직접영향검사,  필요한 `Server/Tools/V5UiImprovementCapture.cs`·대응CodeMap; 실제Prefab저장/meta·문서/색인·검증통합. 기존UI카탈로그/사용자원문은보존하고새결과자료는 `Docs/UI/V5Improved/`에만쓴다.

UI문구helper `CompetitionV5UiText`: `Die(d,hero,dieId)`→실제6면요약, `Gear(d,p,gearId)`→후보효과/현재슬롯비교, `Encounter(d,p,choice)`→실제비용/조건·보상/적정보, `Lap(state,choiceId)`→기존순수보상경로에의한선택전후값, `Results(state)`→실제Rankings/개인상태·동률설명, `Friendly(d,text)`→등록태그/이름표시(숫자·계산변경금지). public static string API이며UI게임상태를수정하지않는다. domain보조만작성하고메인/전투는호출한다.

baseline:시작1247파일SHA256보존. Unityready/Playstopped확인. 기존 F01실행예외와F02실제프레임증거를회귀대상으로고정하고,변경된계약검사만추가한다. 도메인투영은기존전수검사+새사용슬롯/순수보상검사,화면은최초통합실행으로판정한다.

## 종료 기록

2026-10-24 구현/지정검증/주요12사진/요약·색인 완료. 최초통합검수1묶음,수정·재검증1회(상한2),필수Finding0. .NET207·UI18·기존UI25·경계23항목PASS. 실제장치는 REPORT에서 NOT_RUN 구분.3바퀴+30라운드 복합경계는 격리fixture 실제UI 콜백으로 추가확인.1247기준 중25허용변경·1222SHA동일,staged0. 사용자기획/설정/Title/기존사진보존.

## 사용자 승인 커밋 체크포인트 · 2026-10-24

사용자가 이번 개선 작업의 로컬 커밋을 요청했다. 제품 코드는 추가 수정하지 않고 UI11의 코드·meta·프리팹·검사·도구·CodeMap·PLAN/REPORT·주요12사진을67파일로 고정한다. 사용자 설정/폰트/기획 원문과 이전 UI_SCREEN_CATALOG_10 자료133파일은 제외한다. README/INDEX는 이번 변경만 index에 부분 반영하며 작업폴더의 이전 변경은 그대로 유지한다. 규칙 보조1명에게 경계만 읽기전용 대조를 맡기고 메인이 staging/commit/해시 검증을 수행한다(누적 인원 증가 없음). 정확한 목록과 제외파일 SHA256은 ignored artifacts/v5-ui-improvement-11/commit/manifest-before.json에 보존한다. 기존 완료 검증을 재개방하거나 Unity를 실행하지 않는다. 커밋 전후 파일목록·staged 상태·제외파일 해시를 확인하고 push/branch 변경은 하지 않는다.
