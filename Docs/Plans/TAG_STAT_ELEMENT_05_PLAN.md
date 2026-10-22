# TAG_STAT_ELEMENT_05 PLAN

2026-10-22 · IMPLEMENT · 계약 확정. 기준 HEAD `000662b7dd6fa9c743d082b827603c046948cb81`, staged0. 사용자는 제공 설계를 확인하고 충돌 없는 태그·스탯 개편·속성 시스템 작업을 요청했다. 문서 내 단계/샘플/후속 요청 문구는 참고자료이며 사용자 요청과 동일시하지 않는다.

## 목표와 현행 차이

- 태그 분류/질의, 수치 스탯/Modifier, DamageType, 조건/효과를 분리해 중복 보정·Owner 혼동·재귀 발동을 방지한다.
- 원문은 `000662b7`의 v2까지만 본 설계다. 현 작업 트리에는 완료·미커밋된 GROWTH_04의 RGB v3가 있으므로 이를 현행 기준으로 보존한다. 새 규칙은 v4로 분리하는 방향이며 v3 이름/콘텐츠를 재사용하지 않는다.
- 기존 SymbolLoop v1/SharedWorld v1·v2·v3의 계산·저장·JSON/해시·서버 정산을 보호한다. 실제 사용자 파일/DB/설정·원문·meta GUID를 변경하지 않는다.
- 원문의 T0~T8 전체를 일괄 구현 범위로 간주하지 않는다. 태그만 또는 실제6스탯/속성 플레이, 치명타/회피와 영구 권능/C6의 경계를 먼저 확인한다.

## 기준 상태와 보호

- 이전 GROWTH_04 완료 결과와 사용자9파일은 그대로 남아 있다. 새 사용자 제공 설계까지 총10개 사용자 파일을 별도 보호한다.
- 시작 전체 변경 목록/파일 해시, 이전26개 검증 소스 및 보호16파일의 현재 일치 여부를 `artifacts/tag-stat-element-05/`에 기록한다. 기존 완료 결과는 재작업하지 않으며 이번 요청이 직접 바꾸는 계약만 새 버전에 추가한다.
- 이번 조사 동안 제품/Scene/Prefab/Git/Unity 실행을 하지 않는다. 제품 구현 허용 파일·필수 AC는 사용자 결정 후 이 문서에 확정한다. commit/push/ORCA/Firebase는 수행하지 않는다.

## 분업과 상한

- 메인: 사용자 결정·공통 계약·통합·PLAN/REPORT/색인·최종 검증. Unity는 메인만 직렬 실행한다.
- 보조 domain: 읽기 전용으로 태그/스탯/속성 책임, 기존 v3 연결/직렬화 충돌과 최소 도메인 경계 조사. 입력은 제공 설계, 기존 PLAN/색인/CodeMap 후 원본. 산출물은 충돌·구체적 계약 제안·파일 영향·필수 질문. 쓰기/Unity/Git/재귀 위임 금지.
- 보조 integration: 읽기 전용으로 저장/화면/AI/서버 공유 소스 경계 조사. 동일 입력 순서, 산출물은 버전 라우팅·기존 저장 보존·최소 UI·검증 경계. 쓰기/Unity/Git/재귀 위임 금지.
- 동시 최대3명, 누적 보조2명. 최초 검토1묶음, 최초 산출물 검증 후 수정·재검증 최대2회. 현재 조사/계약 작성 중, 제품 수정0회.

## 현재 필수 결정

1. 이번 구현 범위: 태그 기반만 / 6스탯·속성 실제 플레이와 관련 UI / 치명타·특수 회피까지.
2. 기본6스탯 및 초기4피해타입 채택 여부. 기존회피0·추가 주사위면 없음·부분 조합 유지 원칙.
3. 범위 확정 후 3모험가 시작값, 기술 계수, HP/레벨 성장, 장비/패시브의 새 효과와 약점/저항 샘플을 구체화한다. 영구 신 권능·해금/C6는 별도 계약 전까지 만들지 않는다.

## 소스 대조로 확인한 충돌과 해소 방향

| 근거 | 충돌 | 해소 방향 |
|---|---|---|
| CompetitionContent.StatsVersion / CompetitionStats / v3 JSON | 원안의 v3는 이미 RGB 버전이 사용 중 | 신규 v4 콘텐츠·동결 필드·별도 계산 경로, v1~v3 exact 회귀 |
| SymbolCombat.Resolve의 `g.tag == skill.tag` 직접 가산 | v4 modifier를 기존 callback으로 끼우면 장비를 두 번 더할 수 있음 | v4는 기존 Plan(...,true)의 조합 선택만 재사용하고 별도 피해 resolver에서 출처별 한 번 계산 |
| CompetitionApp.LoadDefinition의 combatConfig 덮어쓰기 | v4 JSON의 기술/계수/타입 정의가 SO와 달라질 수 있음 | 구형 조합 유지, v4는 검증된 JSON 정본을 그대로 동결 |
| Competition.Shared.csproj의 Domain/*.cs | 새 하위 폴더가 .NET 링크에서 빠짐 | 하위 폴더를 쓰면 그 폴더만 명시 링크, 테스트/콘텐츠/골든도 명시 링크 |
| App의 IsEnabled(v3) 로비/상세 분기 | 단순 활성 범위 확대는 새6스탯을 RGB 계산기로 보냄 | 새 로비 기능 활성 조건과 버전별 계산/표시 dispatch 분리 |
| HP=60+growth×5, 레벨마다HP+6 | CON 파생 HP와 기존 HP 증가를 중복 반영할 위험 | 새 HP의 기본/CON/계정/레벨 소유권과 증가 방식 한 번만 정의 |
| SymbolLoop Skill/Gear/Enemy/TurnLog 및 서버v1 | 새 필드/enum/전역JSON 변경이 과거 정산 해시를 바꿈 | v4 전용 정의/상태/로그, 구형 타입을 확장하지 않음. 기존 연출에는 표시 사본 투영만 허용 |
| 최종 완주 메뉴8개 버튼 | 태그/속성 버튼을 메인에 추가하면 초과 | 기존 상세 안의 탭·페이지에6스탯/속성/태그/출처를 분리 |

기존 Session/Store의 후보→검증→CAS→원자 저장/공개·재시작 원문 보존 구조는 재사용한다. 신규 경로/이관은 별도 명시 정책으로 고정하며 자동 초기화하지 않는다. AI도 동일 버전 resolver와 LegalChoices만 사용한다. 태그나 UI 조회에서 전투 RNG를 소비하지 않는다.

## 태그 최소 계약 초안

- StableId와 표시명 분리, 등록된 단일 부모 forest만 허용. 미등록/중복/순환/허용되지 않은 Owner/상한 위반을 콘텐츠 검증에서 거절한다.
- Skill Identity와 GrantedAction을 별도 필드로 보관하며 장비/장소/대상의 태그를 Action에 자동 합치지 않는다. Query는 Owner와 ALL/ANY/NONE 및 명시적 자식 포함 정책을 지정한다.
- Raw Grant는 sourceKind/sourceId/ownerId/tagId/ordinal/priority를 보존한다. Has는 집합 의미이며 중첩 효과를 만들지 않는다. 장비 해제는 해당 출처의 Grant/Modifier만 제거한다.
- Effective Snapshot은 정렬된 읽기 사본이다. 상호 배타 그룹은 기본 거절, 우선순위 정책이 명시된 경우에만 승자 선택, 애매한 동률은 거절한다. 원본 Grant는 삭제하지 않는다.
- Binding 수치 보정은 출처/Rule/StackGroup으로 집계하고 피해 타입에서 속성 태그를 구성한다. 속성 변환이 있으면 타입과 태그를 함께 갱신해 이전 속성 보정이 잔류하지 않도록 한다.
- Reaction 실행과 치명타/회피 RNG는 선택된 범위에 포함될 때만 최소 opcode/큐/상한으로 구현한다. 태그 정의만으로 효과 실행이나 임의 코드/외부 호출을 허용하지 않는다.


## 사용자 확정 및 이번 구현 계약

- 사용자 답변: 태그 + 6스탯 + 속성·상성 + 관련 UI/AI. 6스탯·4타입 채택, v1~v3 보존/새 게임 v4. 아래 시험값 적용 승인.
- 힘/민첩/체질/지능/정신/행운: 검사7/6/5/3/5/4, 룬술사3/4/5/7/6/5, 수호자6/3/7/3/7/4. HP=30+CON×6+계정growth×5. 레벨마다6스탯+1, HP는 파생 최대HP 차이만 가산. LUK는 보물 사건 골드에 floor(LUK/5), 후보/난수 재추첨 없음.
- 기술 기본값/기호/부분조합/장비/패시브의 원래 효과 유지. 피해 타입: 마력탄Lightning, 검기Ice, 불꽃·화염탄·화염검기Fire, 나머지Physical. 베기 STR70/DEX30%, 마력탄 INT60/WIL40%, 불꽃 INT100%, 검기 STR40/DEX30/INT30%, 화염탄 INT70/WIL30%, 수호베기·성채베기 STR40/CON30/WIL30%, 화염검기 STR40/INT60%. 보호량 CON60/WIL40%.
- 약점+20%, 저항−15%; 적별 최대1개씩, 동일 타입 중복 거절. 치명타/회피0%, 새 RNG/Reaction/영구 권능/C6/Firebase 제외. 최종 반올림1회(AwayFromZero), 만분율 정수와 유한 항목 수를 검증. 원소 변환은 단일 명시 변환, 변환 뒤 타입 태그/보정 재평가; 초기 장비에 새 변환 효과는 추가하지 않는다.
- 기존 세션/저장/선호와 4MiB/2048명령 상한 유지. 기존 경로의 구형 경기를 그대로 이어 하며, 새 게임 확인 후 v4 정의로 교체한다. 어댑터는 v4만 JSON 정본 유지. v4 전용 nullable DTO로 구형 JSON/hash 불변.

## 쓰기 경계·작업 순서

1. 메인 공통 계약: CompetitionData.cs의 nullable v4 정의/상태, 신규 CompetitionV4Data.cs, CompetitionAttributes.cs, CompetitionElements.cs, CompetitionV4Content.cs, 별도 shared-world-preview-4.json. 기존 CompetitionContent/Rules/Combat/EncounterRules/CommandRules/Ai의 버전 분기와 .NET 테스트 콘텐츠 링크. 신규 CompetitionV4Tests.cs 및 관련 CodeMap/meta.
2. 보조 domain(기존 조사 agent 재사용): 신규 Domain/CompetitionTags.cs + Tests/EditMode/CompetitionTagTests.cs와 두 CodeMap/meta만 단독 작성. 태그 Owner/정의/Grant/Query/Snapshot/충돌 검증 및 유한 상한. 다른 파일/Unity/Git/재귀 금지. 메인이 .NET 검증을 직렬 실행.
3. 보조 integration(기존 조사 agent 재사용): 공통 DTO/API 확정 후 Presentation/CompetitionApp.cs, CompetitionScreen.cs, CompetitionGrowthView.cs, 신규 CompetitionElementView.cs, Editor/CompetitionAuthoring.cs, Tests/PlayMode/CompetitionFlowTests.cs와 그 CodeMap/meta만 단독 작성. 6스탯/속성/태그/출처 페이지, v4로드/구형재개, 기존 최대8버튼 내 연결. Unity 실행·자산 쓰기는 메인만. 계약 변경은 먼저 메인에게 전달.
4. 메인 자산: Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab, Assets/_Project/Scenes/Title.unity 두 파일만 Editor API로 기존 GUID 보존 갱신. 신규 JSON/C# meta만 생성; 기존 combat SO·v3 JSON·SymbolLoop 도메인 원본은 보호.
5. 메인 문서: 이 PLAN, TAG_STAT_ELEMENT_05_REPORT, Docs/CodeMap/INDEX.md, Docs/README.md, Docs/Dice_Warrior_GDD_v0.3.md, 직접 변경 코드의1:1 CodeMap만. 제공 원안은 수정하지 않고 현행 GDD/PLAN에서 확정안과 차이를 명시한다.

## 필수 AC 및 검증

- Owner/출처 분리, ALL/ANY/NONE/명시 자식조회, 중복 Has/Modifier, 한 출처 제거, 충돌/미등록/순환/상한 거절.
- 6스탯/HP·계정 동일 보정·성장1회, 모든9기술 계수·속성·부분조합·장비/패시브1회, 100→138/98, 변환 후 구속성 보정 제거, 보호막·처치 이후 생략·반격 유지.
- 적 속성/약점/저항은 동결 콘텐츠로 선택/전투/AI에 동일 적용. UI조회/AI평가로 RNG 소비 없음. 행운 보물 보상 한 번.
- v1/v2 골든 및 v3 소스 기준 JSON/재현 회귀, v4 인간 명령으로 AI 포함 전체 경기 재현/중간 저장·복구. 콘텐츠/연산/저장 실패 시 공개 상태·원문 불변.
- .NET 공유 코어 검사, Unity 컴파일/EditMode/PlayMode. 실제 UI 포인터·두세로비율·최대기술/6스탯/상성 읽기 검증. 기기/실제HTTP는 이번 필수검증 아님이며 미실행으로 별도 보고.
- 새 전체 산출물 최초 검증 후 수정·재검증 총2회. 내부 구현 중 RED/GREEN은 최초 산출물 완료 전 개발 증거이며 최종 검토 예산을 단계 이름으로 재설정하지 않는다. 동일 두 보조가 자기 작성 범위 밖을 나눠 최초 검토1묶음, 메인이 중복 제거/판정한다.

### 구현 중 경계 구체화

- 순환 참조를 피하려고 순수 수치 합성은 신규 Domain/CompetitionModifiers.cs로 분리했다. Content→Modifiers/Tags, Attributes→Content/Modifiers, Elements→Attributes 순으로 의존한다. 메인 소유 C#/CodeMap/meta 허용 경계에 포함한다.
- 보조 domain은 추가로 신규 CompetitionModifierTests.cs와 해당 CodeMap/meta를 단독 작성한다. 메인은 CompetitionV4StoreTests.cs와 관련 CodeMap/meta로 기존 버전 재개/복구·v4 파일 크기/2048명령/실패 원자성을 검증한다. 누적 보조 수2/동시최대3은 유지한다.
- v4 기본 콘텐츠는 기존9기술/5장비 값을 정본 JSON에 명시한다. 11개 적(최종 포함)에 공격 타입·약점·저항을 배정했다. 보물 사건은 forest-risk/pass-risk/citadel-curse/rift-risk/hill-supply 5개. 최종 반올림은 유한 만분율 정수의 BigInteger 유리수로 한 번만 하며 결과 상한1억을 초과하면 거절한다.
- 개발 증거: v4 정본 없음 RED1, 태그 stub RED11. v3 시작/최종 SHA256 3모험가를 변경 전 고정했다. 첫 태그+시작 GREEN13, 이후 v4 도메인20 PASS. 전체 산출물 최초 검증은 UI 연결 후이며 아직 수정 예산 사용0.

## 최초 검토·수정 기록

2026-10-23: 도메인/저장과 UI/라우팅은 보조2명이 자기 작성 영역을 제외하고 나눠 최초1묶음 검토했다. 보조 필수Finding0. 메인 검토에서 TSE05-F1을 추가했다.

- TSE05-F1: TagQuery/TagGrant.ownerKind, V4Skill.damageType/V4Enemy.attackType 누락 JSON이 enum0(Source/Physical)으로 해석된다. 명시 Owner/타입 계약 위반이며 잘못된 데이터가 조용히 다른 효과를 낼 수 있다. 누락4사례 실제RED 후 새DTO 기본값만 invalid(-1)로 변경해 기존검증이 거절하게 했다. 정상 명시 콘텐츠/구형 DTO는 그대로다. 해결 조건인 누락4거절+구형골든/새전체재현을 .NET118/118·Unity EditMode201/201 최종PASS로 확인해 종료했다. 최초통합후 수정·재검증1/2회.

## 완료 기록

승인 범위 구현과 문서 갱신을 완료했다. 최종 컴파일 오류0, .NET118/118·EditMode201/201 PASS. 실제 화면/포인터/연출 PlayMode는 최초 통합24/24 PASS다. F1 수정 후 추가 PlayMode는 Editor가 실행 중이나 Pipeline 검색 파일이 없어 NOT_RUN으로 구분했다. 사용자 파일10개를 포함한 보호 대상22개 해시 유지, C#63개 CodeMap/.meta 확인, 수정 C#22개, 문서 링크/신규 GUID 검사 PASS. 남은 필수Finding0, 수정1/2회, 누적 보조2명/동시최대3 유지. 커밋·push/ORCA/Firebase/C6는 수행하지 않았다. 세부 결과와 미검증은 [REPORT](../Reports/TAG_STAT_ELEMENT_05_REPORT.md)에 기록했다.

## 후속 커밋 계약 · 2026-10-23

사용자가 태그·6스탯·속성 개편과 문서의 로컬 커밋을 요청하고 기존 사용자 변경 제외를 명시했다. 기준 HEAD와 staged0을 재확인했다. v4가 참조하는 미커밋 GROWTH_04의 모험가 선택/구형 v3 보존 코드·자산·CodeMap·검증 문서도 필수 의존성으로 함께 포함한다. 사용자 보호10파일은 원문/해시 그대로 제외한다. exact-file manifest와 검증 소스 해시를 대조한 뒤 명시 경로만 staging하고 commit한다. push·제품 코드 수정·Unity 재실행은 하지 않는다.

메인은 manifest/PLAN/REPORT/Git의 유일한 작성자다. 기존 보조 integration 1명에게 manifest·제외 목록·기존 증거 일치 여부만 읽기 전용으로 위임한다. 추가 구현 검토/재귀 위임/Unity/Git 쓰기 금지, 누적 보조2명/동시최대3과 기존 수정 예산을 유지한다. 커밋 파일 목록 일치·staged0·보호10파일 해시 유지가 종료 조건이다.
