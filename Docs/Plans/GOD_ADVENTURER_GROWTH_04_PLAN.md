# GOD_ADVENTURER_GROWTH_04 PLAN

IMPLEMENT · 2026-10-22 · 1차 플레이 개편 완료. 기준 `000662b7dd6fa9c743d082b827603c046948cb81`, staged0. 원문 방향과 아래 사용자 답변을 구현 계약으로 동결한다.

## 요청과 고정 방향

사용자가 `Dice_Warrior_God_Adventurer_Growth_Direction_GDD_v0.2.md`를 확인하고 기획 변경에 따른 작업을 요청했다. 원문은 방향 확정/추천안/보류를 구분하는 자료로 취급하며 미확정 수치를 구현 승인으로 해석하지 않는다.

- 신은 계정 주체, 모험가는 보드의 출전자다. 신격/권능 포인트/트리 투자/해금은 영구 계정, 모험가 XP/레벨/장비/주사위/사건 성장은 해당 경기 소유다.
- 권능은 로비에서 준비하고 새 경기 시작 때 동결한다. 경기 중 레벨업이 영구 포인트를 지급하거나 트리를 변경하지 않는다.
- 기존 공동 보드/AI/종료·이어하기·새 게임/부분 조합은 유지한다. 기존 SymbolLoop 및 SharedWorld v1/v2 기록은 소급 변경하지 않는다.
- 실제 Firebase·ORCA 실행·기존 사용자 저장/DB 변경·commit/push는 이번 조사에 포함하지 않는다. 원문은 보존한다.

## 현재 조사 범위와 완료 조건

- 원문 전체와 현행 GDD/색인/직접 CodeMap을 확인한 뒤 실제 원본·참조로 차이를 검증한다.
- 필수 결정은 이번 구현 범위, 전투/모험가의 시험 규칙, 영구 계정 권한과 기존 성장 보존, 적용 시점으로 한정한다. 범위 밖 세부 사항은 후속으로 둔다.
- 출력은 이 PLAN의 구체적인 변경 계약/진행과 대응 REPORT의 실제 검증이다. 초기 문서 분석에는 Unity/테스트를 실행하지 않았다. 사용자가 아래 범위와 시험값을 확정한 뒤 제품 구현으로 진행한다.
- 기존 사용자 변경은 이전8파일과 새 제공 GDD1파일 총9개다. 조사 시작 해시와 경로는 `artifacts/god-adventurer-growth-04/user-before.json`에 기록한다.

## 역할·경계·반복 상한

- 메인: 공통 계약/통합, App/Screen/Editor·선택 저장/검사, GDD/README/색인/PLAN/REPORT, Unity 직렬 실행 및 최종 .NET 검증. Scene/Prefab은 메인만 Editor API로 수정한다.
- 보조 domain: 초기 조사 완료. 아래 Domain/새 콘텐츠·도메인 검사와 해당 CodeMap만 작성한다. 순수 .NET RED는 메인과 직렬 조정하여 실행 가능. Unity/Git 변경·다른 파일·재귀 위임 금지.
- 보조 authority/review: 초기 계정 영향 조사 완료. 서버 변경 없음과 기존 성장 정책 보존을 확인했다. 구현 후 최초 한 검토 묶음의 코드/증거 검토를 읽기 전용 수행한다. 파일 쓰기·Unity/Git 변경·재귀 위임 금지.
- 동시 최대3명(메인 포함), 누적 보조2명. 하나의 초기 조사/검토 묶음, 수정·재검증 총2회. 최초 통합 .NET59/EditMode142/PlayMode19 PASS. 육안 확인에서 장비 효과 문구 중복·계정 보정 문구·모험가 카드 강조색을 수정하고 UI19/19 재검증까지1/2회로 완료했다. 이전 완료 작업은 재개방하지 않는다.

## 승인 범위·시험 규칙

사용자 답변: **1차 플레이 개편부터 진행**, **3종과 시험값 적용**. G1 스탯, G2 기본 제공 모험가/고유성, G4 자동 런 성장, G5 선택/상세 화면을 구현한다. 신격·영구 권능·업적 해금·재굴림·C6/C7/M5는 후속이며 서버 제품/DB/과거 영구 자산을 변경하지 않는다.

| 모험가 | ID | HP | 무력 | 수호 | 비전 | 고유 능력 |
|---|---|---:|---:|---:|---:|---|
| 방랑 검사 | wanderer | 60 | 6 | 4 | 4 | 강철 태그 피해 기술 +1 |
| 룬술사 | scholar | 60 | 4 | 4 | 6 | 비전·불 태그 피해 기술 +1 |
| 성채의 수호자 | vanguard | 60 | 4 | 6 | 4 | 보호막 발생 기술의 보호량 +1 |

- 세 종류 모두 기본 제공이다. 영구 해금/신격 보상으로 표시하지 않는다. 모험가 변경은 다음 경기 출전 선호이며 진행 중 경기의 선택과 시작 상태는 바뀌지 않는다.
- 무력 RGB(229,108,100)/#E56C64, 수호(82,185,135)/#52B987, 비전(114,151,237)/#7297ED. 색과 텍스트·아이콘·정확한 수치를 함께 사용한다.
- 레벨 필요 XP=현재 레벨×8. 레벨 상승마다 최대/현재 HP+6, 무력/수호/비전 각+1. 기존 계정 growth당 시작 HP+5를 유지하며 모든 참가자에게 같은 growth/출전 주사위·무기를 적용한다. AI는 전쟁=검사, 지혜=룬술사, 행운=수호자로 고정하고 기존 사건 정책/난수는 유지한다.
- 기술 기본값과 태그 장비 효과는 유지한다. 베기/검기/수호 베기/성채 베기 피해는 기본값+무력, 마력탄/불꽃/화염탄은 기본값+비전, 화염 검기는 기본값+floor((무력+비전)/2). 보호막은 기본값+수호. 계수 합산 후 정수 내림1회, 태그 장비와 모험가 패시브를 더한 뒤 기존 보호막 흡수/HP 차감/반격을 적용한다. 효과가0인 피해/보호에 보너스로 새 효과를 만들지 않는다.
- 기존 3눈 정확 조합 우선→2눈 부분 조합+남은 기본기 순서를 유지한다. 처치 이후 생략·적 반격1회·64턴 제한은 그대로다.

## 구현 계약·파일 경계

1. **버전/도메인 (domain 담당)**: 새 `shared-world-preview-3`과 별도 `Content/shared-world-preview-3.json`/meta. 기존 v1/v2 JSON·골든과 SymbolLoop Rules/Skill/Profile/전역 직렬화는 변경하지 않는다. 새 중첩 필드는 구형에서 null이며 해당 필드만 null 직렬화를 생략한다. SharedWorld/Domain/{CompetitionData,CompetitionContent,CompetitionRules,CompetitionCombat,CompetitionCommandRules,CompetitionEncounterRules}.cs 및 새 CompetitionStats.cs, 필요 최소 SymbolCombat 오버로드, 새 Tests/EditMode/CompetitionGrowthTests.cs/meta와 각 요약을 허용한다.
2. **공통 표시 API**: CompetitionStats.IsEnabled(definition), Preview(definition,profile,adventurerId), Describe(definition,participant), ScalingText(definition,skillId). Preview/Describe는 HP·STR/GRD/ARC의 기본/기존계정/런레벨/최종 수치와 이름/패시브 설명을 제공하는 CompetitionStatBreakdown 사본을 반환한다. 장비는 RGB 직접 가산이 아닌 태그별 기술 효과로 별도 표시한다. 서버 권한/신 포인트 타입은 만들지 않는다.
3. **선택 저장 (메인)**: 새 Runtime/AdventurerSelectionStore.cs/meta와 Tests/EditMode/AdventurerSelectionStoreTests.cs/meta. 계정별 공동 경기 폴더의 별도 adventurer-selection.json에 ID/revision/체크섬을 저장한다. 기존 Profile/선호 파일은 그대로다. 잠금·revision 비교·원자 저장·백업, 실패/손상 원문 보존. 오류는 시작/선택에서 안내하고 진행 중 경기 이어하기는 가능하게 한다.
4. **화면 (메인)**: Presentation/{CompetitionApp,CompetitionScreen}.cs, 새 CompetitionGrowthView.cs/meta는 표시 문구·가능 기술 계산만 담당한다. 로비 모험가 선택/예상 시작 스탯, 경기 내 상세 화면(읽기 전용)을 추가한다. 모험가 기본/기존 HP 보정/레벨 성장/합계와 장비 효과·주사위면·기술계수를 표시한다. 신격/권능 UI를 완성된 기능처럼 표시하지 않는다. 선택 메뉴/상세 표시 중에는 진행 명령을 실행하지 않는다.
5. **자산 허용 파일 (메인 Editor API)**: `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab`에 작성된 선택/상세 패널 참조 추가, `Assets/_Project/Scenes/Title.unity`의 CompetitionApp 콘텐츠 연결을 v3로 변경한다. Editor/CompetitionAuthoring.cs에 해당 작성 절차를 연결한다. 기존 GUID/다른 Scene 오브젝트/Config/폰트/프로젝트 설정은 보존한다. 신규 JSON/C#의 meta를 함께 관리한다.
6. **통합 검사/문서 (메인)**: Tests/PlayMode/CompetitionFlowTests.cs, Server/DiceWarrior.Competition.Tests/DiceWarrior.Competition.Tests.csproj의 정확한 Runtime/콘텐츠 링크만 변경한다. GDD/README/CodeMap 색인·수정 C# 요약/직접 관계·REPORT, 새 Evidence/GodAdventurerGrowth를 갱신한다. 원본 기획서는 변경하지 않는다.

## 필수 AC·실패 조건·검증

- 세 모험가 선택이 실제 시작/재시작·직렬화 재개와 AI에 반영된다. 잘못된 ID/과거 선택 저장 revision·손상/저장 실패는 진행을 덮지 않는다. 기본 제공과 업적 해금을 혼동하지 않는다.
- 표의 시작값·계수·패시브·장비/보호/처치/반격·런 레벨 성장·새 게임 초기화를 수치로 검사한다. 구형 v1/v2 실제 저장과 고정 골든은 불변, v3는 인간 명령/AI/전체 상태 재현과 중간 직렬화 복원이 일치한다.
- 실제 authored UI 포인터로 선택/스탯 상세/뒤로/이어하기/새 게임·오프라인/저장 실패를 검사한다. 720×1280/720×1600 safe area와 텍스트 잘림·버튼 수를 확인하고 캡처한다. 기존 숫자/기호/혼합 스킬 연출 회귀를 포함한다.
- 관련 .NET→Unity compile→EditMode→PlayMode를 직렬 실행한다. 실제 사용자 계정/DB를 시험하지 않는다. 검사 실패·자산 연결 미완료·필수 미검증은 완료로 처리하지 않는다. 최초 검증 이후 수정·재검증은 작업 전체2회 이내다.

## 완료 기록

필수 AC를 실제 .NET59/59·EditMode142/142·PlayMode19/19와 authored 캡처로 확인했다. 원본 사용자9파일을 포함한 보호16파일의 SHA256 동일, C#52개의 요약/meta 누락0, 신규6meta GUID 유일, 기존 서버v1 콘텐츠 변경0, 문서 링크 오류0이다. 소스16개 C#와 자산/콘텐츠/프로젝트 링크 총26파일의 해시를 보존했다. 최종 Editor는 Title 저장 상태·Play 정지다. 자세한 증거/미검증은 [REPORT](../Reports/GOD_ADVENTURER_GROWTH_04_REPORT.md)를 따른다. 추가 필수 질문·미해결 Finding0, commit/push 없음.
