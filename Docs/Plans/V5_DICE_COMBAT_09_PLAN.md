# V5 주사위 선택 전투 PLAN

2026-10-24 · IMPLEMENT · **완료**. v5 자산 연결·실제 UI·구형 회귀·보호 감사 PASS. 커밋/푸시는 하지 않았다.

## 요청과 기준

사용자가 `Dice_Warrior_V5_Dice_Roll_Hold_Reroll_Skill_UI_Implementation_Plan_v0.1.md` 검토 후 구현을 요청했다. 문서는 제품 요구사항 자료로 사용하며 문서 안의 대화 이력·복사용 지시문을 별도 사용자 승인으로 간주하지 않는다. 목표는 v5 신규 경기의 굴림/보관/해제/리롤/실행, 공용/전용/꽝 면, 스킬 예상, 6면 전개도, 동등 AI, 모바일 전투 화면과 저장/구형 회귀다. 세계관·영구 권능·C6/Firebase·v4 밸런스 변경·Git 쓰기는 제외한다.

기준 main `bd9f32a671e7b7b6934aad8902ff7a5a08f151e5`, staged0. 기존 사용자12파일, 이전 완료·미커밋 실행 도우미5파일, 이번 사용자 제공 원문1파일 총18변경을 보존한다. Docs/README와 Server/README의 기존 실행 도우미 변경은 같은 대화에서 작성한 것으로 유지하며 필요한 v5 안내만 추가한다. 원문·폰트·설정·실사용 DB/저장·5087 서버는 시험하지 않는다. Docs/AI 운영 파일은 현재 부재, AGENTS와 기존 PLAN/CodeMap을 따른다.

## 조사 분업과 상한

- 메인: 공통 계약/버전·Session/Store 통합, Unity 직렬 실행·검증, PLAN/REPORT·색인·GDD·README.
- domain 보조: 기존 CodeMap→Domain 원본 및 v5 제안 대조. DTO·콘텐츠·조합/수치 재사용·AI/명령 경계, 필수 미결정·파일별 계약을 읽기 전용 조사한다.
- presentation 보조: 기존 CodeMap→App/Screen/Playback/Editor authoring/PlayMode 원본 대조. 실제 프리팹/Scene 경로·연결·safe area/정보창/중단 재개·검증 경계를 읽기 전용 조사한다.
- 필요 시 세 번째 보조는 공통 계약 확정 후 독립 검사만 맡는다. 동시 최대4명(메인+보조3), 누적 보조3명, 재귀 위임 금지. 조사 후 쓰기 경계는 본 PLAN에 추가 동결한다. 공통 계약/DTO/색인은 메인 소유이며 파일당 한 작성자다.
- 최초 통합 검토1묶음, 최초 산출물 검증 이후 기본2회에 사용자 추가 승인1회+1회를 합쳐 총4회. 최종4/4회 사용, 필수 Finding 모두 해결. 완료된 이전 Task 검토 예산을 재개방하지 않는다.

## 최초 확인과 다음 단계

- [x] 원문 및 기존 TAG_STAT_ELEMENT_05/독립 서버/실행 도우미 계약 확인.
- [x] 사용자/레거시/골든/기존 자산 해시·변경 목록 보존, Unity 상태 확인(사용자 직접 재시작 후 Pipeline 연결 복구).
- [x] 스킬 시험값·AI·명령 한도 및 공통 API 동결, 파일 단위 허용 경로 확정.
- [x] v5 Domain/Session/콘텐츠 및 관련 CodeMap·검사 구현(.NET184/184 PASS).
- [x] v5 전투 UI/전개도/연출과 Editor API authoring 소스·CodeMap 구현(외부 컴파일3/3 PASS).
- [x] Editor API 자산 반영·신규11meta import·GUID/원래 object ID·Title v5 참조 확인.
- [x] .NET184/184·Unity EditMode184/184·v5 PlayMode9/9·기존 PlayMode25/25 및30개 요구사항 증거 통합, 문서/보호 검증.

Scene/Prefab 쓰기는 실제 대상 경로 확인 후 이 PLAN의 파일 단위 목록에 등록된 자산만 Editor API로 수행한다. 원문 V5-T01~30은 필수 검증 행으로 REPORT에 실제 PASS/FAIL/BLOCKED/NOT_RUN과 근거를 남긴다. 증거/시험 저장은 고유 ignored `artifacts/v5-dice-combat-09/` 아래에 둔다. 필수 질문이 없으면 사용자 요청에 포함된 플레이 개편 범위 안에서 구현을 계속한다.

## 확정 계약과 구현 경계

- 사용자 답변: **v5 전투20행동턴 + 진행·매턴 리롤1회 공간 보장**. 구형64턴/전체2048명령/4MiB 유지. v5 보관·해제는 턴12회, 남은 필수 진행·리롤 명령을 예약하고 선택 토글만 예약을 침범하면 거절한다. 최악기본예산30×(2+20×3)=1860, 초기선택여유188이며 조기전투종료 등에 따라 실제여유는 늘어난다.
- 시작 balanced/arcane/steel의 6면은 원문 공용4+직업1+blank1. `$class` 템플릿은 동결 모험가의 실제 FaceId로 조회·추첨 때 해석하며 결과에는 실제ID/물리면인덱스/instance/slot을 저장한다. 미등록/누락/다른모험가 face·skill은 거절한다.
- 구현자가 채택한 초기 시험값: 공용9스킬은 v4 기본값/계수/태그·속성 유지. 전용 단일/2눈은 검사4/14물리(힘70%+민첩30%), 룬술사3번개(지능60%+정신40%)/15번개(지능70%+정신30%), 수호자보호5/12(체질60%+정신40%). 새 상태효과 없음. 3눈→우선순위 큰2눈1개→남은단일 원래슬롯순, 동률SkillId Ordinal, 같은recipe 중복은 로딩거절. 원문이 미제시한 수치의 시험 구현 선택이며 최종 밸런스 판정이 아니다.
- AI `v5-keep-nonblank-1`: 최초굴림→꽝이 있으면 비꽝을슬롯순보관→꽝만1회리롤→실행. 꽝없으면 즉시실행. 미래난수조회없음, 내부최대123전이. 구형 AI버전의 동작/JSON 그대로.
- DTO명: `V5Definition`, `V5FaceDefinition`, `V5DieLayout`, `V5SkillDefinition`, `V5ParticipantState`, `V5SelectionState`, `V5FaceResult`, `V5CombatLog`, `V5CombatPreview`. definition.v5와 participant.v5는 null생략, 구형payload에 새필드 없음. v5 required필드 누락은 fail-closed. 프리뷰는 피해/흡수/보호와 처치후생략을 포함한 순수조회다.
- v5 경기 ID는 최대100자로 제한하여 여기서 파생되는 주사위instance/명령target과 저장 크기에 상한을 둔다. 기존 v1~v4 입력 계약은 유지한다. v5 Store는 state.actionNumber와 commands.Count의 일치를 검사해 도메인의 예약 예산과 실제 저장 로그가 일치하도록 한다.
- 공통 공개 API는 Domain 담당이 위 계약에 따라 파일에 먼저 선언하고 두 담당에게 전달한다. `CompetitionV5Content.IsEnabled/Face/Faces`, `CompetitionV5Skills.Preview`, 선택상태/로그, 명령예산조회가 UI/검사의 공통읽기경계다. 변경 시 메인 승인후 다른작성자에게 전달한다.

### 단독 작성 경계

- domain 보조: 신규 Domain `CompetitionV5Data.cs`, `CompetitionV5Content.cs`, `CompetitionV5Dice.cs`, `CompetitionV5Skills.cs`, 필요시 `CompetitionV5CommandBudget.cs`; 기존 Domain `CompetitionData.cs`, `CompetitionContent.cs`, `CompetitionV4Content.cs`, `CompetitionRules.cs`, `CompetitionCommandRules.cs`, `CompetitionEncounterRules.cs`, `CompetitionAi.cs`, `CompetitionElements.cs` 및 직접필요한 `CompetitionAttributes.cs`; 신규 `Content/shared-world-preview-5.json`. 각각의 CodeMap. 기존 정본JSON/골든/기존SymbolLoop 코드 쓰기 금지. 신규v5콘텐츠만 v4의동결 공용값을 재사용하며 원본을 변경하지 않는다.
- presentation 보조: 기존 Presentation `CompetitionApp.cs`, `CompetitionScreen.cs`, `CompetitionPlayback.cs`, Editor `CompetitionAuthoring.cs`; 신규 Presentation `V5DiceAreaView.cs`, `V5DiceNetView.cs`, `V5FaceGraphic.cs`; 신규 Tests/PlayMode `CompetitionV5FlowTests.cs` 및 각각 CodeMap. 최초 Finding F01 해결에 직접 필요한 `CompetitionElementView.cs`와 CodeMap의 v5 상세 분기를 추가 허용한다(구형 표시 유지). 전용뷰는 runtime에서임의UI생성없이 authored 직렬화참조사용. 기존LegacyUI·공용Prefab·폰트 쓰기 금지.
- 검사 보조: 신규 Tests/EditMode `CompetitionV5Tests.cs`, `CompetitionV5StoreTests.cs` 및 CodeMap. 실제저장고유fixture·중간복원·RNG·중복/실패/기존v4재개·2,048한도예약·전체경기/4MiB증거. 제품소스·기존골든기대값 수정 금지.
- 메인: Runtime `CompetitionSession.cs`, `CompetitionStore.cs`의 필요v5검증연결(필요없으면무변경), Server/DiceWarrior.Competition.Tests csproj의v5콘텐츠링크, 신규검증도구 필요시 Server/Tools/V5DiceCombatValidation.cs와CodeMap, 문서·색인·GDD/README, 모든실제검사실행. 신규Unity파일meta는Editor import로생성하고기존meta/GUID보호.
- 자산 파일단위 허용: `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab`에v5 subtree/직렬화참조추가, `Assets/_Project/Scenes/Title.unity`의App 콘텐츠참조만v5연결. EditorAPI만사용, 기존 GUID/local참조보존, Play/dirty상태확인후 실행. 공용UIRoot/CommonButton/Config/폰트는읽기참조만. 신규v5JSON/meta는정본추가허용.

baseline 증거: 기존18변경·추적154파일 SHA256을 `artifacts/v5-dice-combat-09/run-01`에보존. 최초 sandbox Get-Process는Unity0개로보였으나 외부읽기에서는프로젝트Editor가실행중임을확인해중복기동하지않았다. 이후 사용자가 직접 재시작하여 연결을 복구했다.

## 최초 통합 검토와 수정1회차

Domain/Store 검토 필수 Finding0, .NET184/184와 동결 v4 9경기의27해시 비교 PASS. UI Runtime/Editor/PlayMode 소스는 설치된 Unity DLL 참조를 사용한 외부 Roslyn 컴파일 PASS다. 이는 Unity import·실제 런타임 검사와 구분한다. 보조 스크립트/로그는 ignored artifacts에 보관한다.

- **F01**: v5 상세가 stats로 고정되어 효과 출처/장비/태그/속성 조회가 막히고, 본문은 접근 불가능한 효과 출처를 안내한다. 전용 SkillId를 지원하는 상세 조회와 적 정보, 실제 포인터 회귀 검사로 해결한다.
- **F02**: 실행 연출은 최종 HP바와 중간 기술의 HP 숫자를 동시에 보여준다. 아군 현재 보호량도 HUD에 없다. 저장 로그 단계에 맞는 바/숫자/보호 표시 및 최종 Snap으로 해결한다.

1회차 당시에는1/2회 사용 상태였다. Domain 제품 코드를 동결하고 위 UI Finding만 수정했으며 외부 Runtime/Editor/PlayMode 소스3어셈블리 재컴파일이 통과했다. 당시 실제 PlayMode9개와 프리팹/Title·신규5개 meta는 Pipeline 재연결을 기다리는 미검증/미반영 상태였고 완료 판정을 보류했다. 아래 재연결 이력과 최종 판정에서 후속 결과를 확인한다.

## Unity 재연결과 수정2회차

사용자는 직접 저장 후 재시작을 선택했고 새 Editor 연결을 확인했다. Play 중지·Title dirty=false 상태에서 UpgradeV5를 실행했다. 기존 prefab550개·Title25개 object ID 및 두 GUID는 보존했고 Title은 content 참조1개만 변경됐다. 신규11자산 meta가 모두 import됐다. Unity EditMode184/184 PASS, 최초 신규 PlayMode9개는6PASS/3FAIL이다.

- **F03**: 새로 활성화한 버튼/모달/상세 페이지의 같은 프레임 입력에서 아직 그려지지 않은 Graphic이 raycast에서 제외된다. 통과하는 modal 검사는1프레임을 기다리며 실패하는3경로는 즉시 입력한다. 전용 fixture는 실제 프레임 경계를 기다리되 최초 raycast가 기대 Button이라는 단언을 유지한다.
- **F04**: 새 V5FaceGraphic9개의 prefab에 CanvasRenderer가 없다. 기존 공용 glyph와 같이 RequireComponent를 명시하고 UpgradeV5에서 **새 v5 subtree의 해당9개만** 누락 Renderer를 Editor API로 추가한다. 해당 C#·CodeMap과 검사 외 쓰기는 하지 않는다. 실제 mesh/표시 및 모달 배경 중첩 여부를 두비율 재캡처로 확인한다.

위 지정 Finding은 당시 기본 한도의2회차로 수정했다. 실제 Unity는 메인만 실행했고 공용 UI/구형 정본/게임 규칙은 수정하지 않았다.

수정2회차 컴파일에서 **F05**(검사 코드의 `CanvasRenderer.GetMesh(mesh)` API 불일치)를 확인했다. 설치된 Unity 문서의 인자 없는 `GetMesh()`로 변경하고 Renderer 소유 메시를 삭제하지 않는 최소 수정안을 준비했다. 사용자가 **최대1회 추가 진행**을 승인하여 총3회 중3회차로 검사 코드/CodeMap만 수정·재검증한다. 해당 파일 쓰기는 메인이 인수하며 보조는 동결한다.

3회차에서 신규 PlayMode9/9 PASS 후 캡처의 **V5-NET-VIS-01**(98% 불투명 정보창 뒤 HUD 글자 비침)이 남았다. 사용자가 해당 배경의100% 차폐·검사·문서에 **추가1회 승인**했다. 총4회 중4회차로 메인이 Authoring/CodeMap을 인수해 새 v5Net 배경만 변경하고 두비율 캡처를 확인한다. 다른 UI·게임 규칙은 변경하지 않는다.

## 최종 판정

사용자 승인된4회차에서 v5 상세창 alpha1, 실제 authored CanvasRenderer9개, 두비율720×1280/720×1600 캡처와 PlayMode9/9 PASS를 확인했다. 이전 F01~F05 및 V5-NET-VIS-01은 모두 CLOSED다. 최종 수정·재검증4/4회 사용이며 새 필수 Finding은 없다.

기존 PlayMode는 처음 연속 실행에서0건이었다. 현재 Test Framework의 PlayerTestAssemblyProvider가 도메인 리로드 없는 다음 Play 진입 시 static목록을 Clear한 뒤 null이 아니어서 재로드를 생략하는 코드와 실제 로그가 일치했다. 제품·패키지·설정 변경 없이 EditorUtility.RequestScriptReload 후 기존 fixture를 첫 실행하여 **25/25 PASS**를 확인했다. 0건 시도는 PASS가 아니다. 채널 복구는 제품 수정 회차에 더하지 않는다.

최종 보호16파일 해시 동일, 추적154 중 허용18파일만 변경, 수정/신규 C#26개와 CodeMap26개, 신규자산11개와 meta11개를 확인했다. 원래 prefab550개/Title25개 object ID와 GUID를 보존했다. git diff --check의90건은 Editor가 직렬화한 prefab의 빈 m_Name/m_Text/iconId 뒤 공백만이며 수작업으로 자산 YAML을 고치지 않았다. 그 외 공백 경고·staged 변경은 없다. [REPORT](../Reports/V5_DICE_COMBAT_09_REPORT.md)에 최종 증거를 모았다.

## 사용자 승인 커밋 범위 (2026-10-24)

사용자가 v5 구현·검증 결과·문서의 로컬 커밋을 승인했다. 위 구현 당시 Git 제외 조건에 대한 후속 승인이다. 기존 사용자 변경13파일과 별도 실행 도우미5파일의 변경은 포함하지 않는다. Docs/README와 Server/README는 v5 부분만 index에 반영하고 실행 도우미 부분은 작업 트리에 보존한다. 제품/자산 소스는 최종 검증 해시와 대조하며 기존 통과 검사를 불필요하게 다시 실행하지 않는다. 정확한 파일 목록·제외 파일 해시·staged 내용·커밋 후 diff-tree를 확인한다. 푸시는 하지 않는다.

메인이 Git·PLAN/REPORT를 맡는다. 기존 v5_tests 보조1명에게 최종 증거·범위의 읽기 전용 대조만 맡길 수 있으며 새 코드 검토·테스트·쓰기·재귀 위임은 하지 않는다. 동시 최대2명, 기존 누적 보조3명 상한을 유지한다.
