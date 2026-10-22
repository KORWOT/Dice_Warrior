# TAG_STAT_ELEMENT_05 REPORT

2026-10-23 · IMPLEMENT · 승인 범위 구현 완료. 기준 HEAD `000662b7dd6fa9c743d082b827603c046948cb81`. 구현 완료 시점에는 커밋하지 않았으며, 이후 사용자 요청으로 아래 범위의 로컬 커밋을 진행한다. push는 요청 범위가 아니다.

## 확정 범위와 결과

사용자는 [제공 원안](../Dice_Warrior_Tag_Stat_Element_Integration_Design_v0.1.md) 검토 후 태그·6스탯·4피해 타입·상성·UI/AI 연결과 시험 수치 묶음을 승인했다. 문서의 단계/예시는 직접 명령으로 간주하지 않고 [PLAN](../Plans/TAG_STAT_ELEMENT_05_PLAN.md)에 채택 범위를 동결했다. 이미 RGB가 v3를 사용하므로 신규 규칙은 **shared-world-preview-4**다.

- 태그: StableId/이름,9종 Owner, 단일 부모 forest, Identity/GrantedAction 분리, ALL/ANY/NONE/명시 자식 포함, 단계별 깊은 읽기 스냅샷. Raw 출처를 보존하면서 Has는 집합으로 평가한다. 한 출처 제거는 그 출처만 제거하며 우선순위에서 밀렸던 출처도 복원한다. 기본 충돌 거절, 명시 Priority·동률 거절, 미등록/순환/상한/Owner 검증을 제공한다.
- 스탯: 힘·민첩·체질·지능·정신·행운. 검사7/6/5/3/5/4, 룬술사3/4/5/7/6/5, 수호자6/3/7/3/7/4. 최대HP=30+CON×6+기존 계정growth×5. 레벨마다6스탯+1, 파생HP 증가분만 현재HP에 가산한다. 인간과 AI의 계정 보정/시작 주사위/무기는 동일하다. 보물5사건은 floor(LUK/5) 추가골드를 한 번 지급한다.
- 보정: 출처/규칙/대상 중복1회, 충돌한 중복 거절, Stackable/UniqueBySource/HighestWins. Flat→PercentAdd 합산→FinalMultiply 그룹→상성, 만분율 정수 유리수와 최종1회 반올림. 항목 수/ID/연산 입력/최종 결과 상한을 검사한다. 태그가 자체로 효과나 코드를 실행하지 않는다.
- 전투: 9개 기술에 명시된 계수와 물리·화염·냉기·번개 타입. 번개 마력탄·냉기 검기·화염 기술이 실제 전투에서 실행된다. 검/지팡이/불/방패4기호와 3눈 우선·2눈 부분조합을 유지한다. 장비/패시브는 새 Binding 한 경로에서만 가산한다. 적 보호막·처치 뒤 생략·살아 있는 적 반격1회는 유지한다.
- 속성: 일반 적10종+최종 시련의 공격 타입, 약점+20%/저항−15%를 동결했다. 같은 타입 약점/저항은 거절한다. 명시된 무기 변환은 타입과 피해 태그를 함께 바꾼 뒤 보정을 평가하므로 이전 화염 보정이 남지 않는다. 초기5장비에 새 변환 효과는 추가하지 않았고 기본 모험가 저항은0이다.
- AI/화면: AI는 합법 후보 안에서 보유면의 상성·행운 보물을 읽고 사람과 같은 전투/보상 함수를 사용한다. 로비/경기6스탯·HP 출처, 가능한 기술 타입/계수, 속성·상성, 태그/효과 출처5탭을 연결했다. 최대8버튼 내 페이지를 사용한다. 선택 전 적 상성, 전투 후 저장된 typed 피해 결과를 표시한다. 조회/예측으로 RNG를 소비하지 않는다.
- 저장: v4 전용 nullable 정의/상태/로그를 추가했다. v1~v3 JSON/해시/계산과 기존 SymbolLoop·서버v1 타입은 유지한다. 기존 세션/CAS/체크섬/원자 저장/4MiB/2048명령을 재사용한다. 새 앱에서 구형 경기 재개는 원문 그대로, 확인 후 Restart만 새v4를 사용하며 `.restarted-*`와 백업으로 구형 진행을 보존한다.
- 자산: 허용된 `CompetitionScreen.prefab`, `Title.unity`만 기존 Editor API로 갱신했다. 6행 패널과 v4 JSON 참조를 작성했고 기존 .meta GUID를 유지했다. 기존 RGB 패널/계산은 구형 재개용으로 남긴다.

치명타/회피는 이번0%다. Reaction/상태이상·DOT/영구 신격·권능·업적해금·C6 서버 권한 정산·Firebase는 범위 밖이다. 새로운 RNG 흐름이나 영구 보상을 만들지 않는다. 순위 보상0, 구형1인 active 계정의 성장 구매 보류 정책은 유지한다.

## 실제 검증

| 대상 | 결과 | 증거 |
|---|---|---|
| 태그/기반 RED | 태그11 FAIL, 새 정본 시작1 FAIL | [태그](Evidence/TagStatElement/tags-red.trx), [v4](Evidence/TagStatElement/v4-red.trx) |
| ID 경계 RED | 보정 ID5 FAIL 후 전체 합격 | [RED](Evidence/TagStatElement/modifier-ids-red.trx) |
| 자산 연결 RED | 6행 reference 누락1 FAIL, Editor API build 후 합격 | [RED](Evidence/TagStatElement/authoring-red.json), [Build](Evidence/TagStatElement/authoring-build.json) |
| 필수 타입 누락 수정 | 누락4 FAIL→4 PASS | [RED](Evidence/TagStatElement/required-type-red.trx) |
| .NET 공유 코어 최종 | 118/118 PASS | [TRX](Evidence/TagStatElement/competition-final.trx) |
| Unity 컴파일 최종 | completed, failed=false, errors0 | [결과](Evidence/TagStatElement/compile-final.json) |
| Unity EditMode 최초 | 197/197 PASS | [결과](Evidence/TagStatElement/editmode-initial.json) |
| Unity EditMode 최종 | 201/201 PASS, 14.69초 | [결과](Evidence/TagStatElement/editmode-final.json) |
| Unity PlayMode 최초 | 24/24 PASS | [결과](Evidence/TagStatElement/playmode-initial.json) |

v3 도입 완료 시점의3모험가 시작/최종 해시를 v4 구현 전에 고정했고 모두 일치한다. v1/v2 골든,9기술×3모험가 수치,138/98 상성, 변환 후 구속성 보정 제거, HP 성장1회, 보물 중복 지급 방지, 화면/AI 읽기 불변, 인간 명령으로 AI 전체 재현·중간 저장 복원을 확인했다. 전체 v4 실제 저장 시험은 최대 **58,075 UTF-8 바이트/39명령**이었다. 4MiB 초과·2048명령·잠긴 파일·실패 후 재시도도 원문/공개 상태 보존을 확인했다. 경계 fixture는 C6 권한 검증을 주장하지 않는다.

PlayMode는 실제 Raycast/포인터로 모험가 선택→6스탯→새 게임, RGB 경기 그대로 재개→취소/확인 후v4 교체, 최대3주사위/9기술/태그·효과 모든 페이지,720×1280 및720×1600 safe area, 선택 전/전투 중 적 상성, 번개 기본기→냉기 조합 연출, 연출 중단 뒤 저장 재개를 검사했다. 기존 로비/계정 실패/오프라인/보존/주사위·보드 연출 검사도 포함한다.

[6스탯](Evidence/TagStatElement/element-six-stats.png), [모험가](Evidence/TagStatElement/element-adventurers.png), [기술](Evidence/TagStatElement/element-skills.png), [태그](Evidence/TagStatElement/element-tags.png), [출처](Evidence/TagStatElement/element-sources.png), [전투 상성](Evidence/TagStatElement/element-combat-result.png), [safe area](Evidence/TagStatElement/element-safe-area.png)를 육안으로 확인했다. 화면 검사에서 실제 사용자 계정/저장은 사용하지 않았다.

## 검토·보존·미검증

기존 Unity6000.6.0f1 Editor PID44732/port7801 한 개를 사용했다. CLI status의 검색0과 달리 pipeline list/editor_status로 실제 연결을 확인했다. 중복 Editor/ORCA/서버 재시작/사용자 DB 작업은 하지 않았다. 세부 .NET/UI 검사는 격리된 메모리 백엔드·고유 임시 파일만 사용했다. Android 기기·실제HTTP에서 신규 화면 전체 여정·장기 밸런스/부하 검증은 **NOT_RUN**이다.

최종 EditMode 후 추가 PlayMode 재실행은 **NOT_RUN**이다. Editor PID44732는 응답하고 port7801도 수신 중이나 `Library/Pipeline/.unity-pipeline-port` 검색 파일이 없어 CLI가 연결을 찾지 못했다. 공식 상태 API는 인증 없는 요청을401로 거절했다. 종료/강제 재시작/검색 파일 수동 재생성/설정 변경은 하지 않았다. 마지막 Editor 로그에는 Title 씬 재로드가 남지만 종료 시 dirty/playing 상태는 API로 재확인하지 못했다. PlayMode24 PASS는 최초 통합 증거이며 수정 후 실행 결과로 간주하지 않는다. 이후 변경은 누락된 Owner/피해 타입을 거절하는4필드 기본값과4회귀 검사에 한정된다. 정상 명시 콘텐츠의 화면 경로는 변경하지 않았고 해당 수정과 전체 도메인/저장 회귀는 최종 .NET118·EditMode201 PASS로 확인했다.

최초 검토는 도메인/저장과 UI/라우팅을 작성자와 교차 분담한1묶음이다. 보조 필수Finding0, 메인 TSE05-F1은 JSON의 Owner/피해 타입 누락이 Source/Physical로 묵인되는 문제였다. 신규 필드 기본값을 invalid(-1)로 두고 기존 검증에서 거절해 해결했다. 정상 명시 콘텐츠/과거 DTO에는 영향이 없다. 최초 통합 이후 수정·재검증 **1/2회**, 남은 필수Finding0. 후속 일반 개선을 위해 검토를 재개방하지 않는다.

현행 GDD/README/PLAN과 변경 C#1:1 CodeMap·직접 관계/색인을 갱신했다. [최종 감사](Evidence/TagStatElement/final-audit.json)에서 사용자 변경10개를 포함한 보호 대상22개 해시가 모두 유지됐다. 프로젝트 소유 C#63개에 CodeMap/.meta가 존재하며 이번 변경 C#은22개다. 신규 .meta GUID 중복0, 변경 문서 깨진 상대 링크0, 코드/문서 diff-check PASS, 기존 서버v1 변경0, staged0이다. Unity가 작성한 Scene/Prefab YAML은 일반 공백 검사에서 제외했다. [소스 해시38개](Evidence/TagStatElement/source-hashes.json)와 감사의 taskFiles가 이전 작업 및 사용자 변경과 이번 범위를 구분한다.

## 후속 로컬 커밋 범위 · 2026-10-23

사용자 요청에 따라 태그·6스탯·속성 개편과 직접 문서/검증 증거를 커밋한다. v4에서 참조하는 선행 GROWTH_04의 모험가 선택·구형 v3 보존 코드/콘텐츠/화면/검사와 해당 문서도 아직 미커밋이므로 함께 포함한다. exact-file manifest는127파일이며 기존 staged0에서 시작했다. 두 작업의 검증 소스 해시를 최신 v4 우선으로 합친51개가 현재 파일과 모두 일치한다. 제품/검증 코드 변경 없이 커밋 범위와 문서만 확인했으므로 기존 테스트 결과를 새 실행으로 표기하지 않는다.

사용자 소유 설정4파일(렌더링2·폰트1·ProjectSettings1), 패키지 로컬 설정2파일, 사용자가 제공한 원문 문서4파일은 총10개 모두 제외하고 시작 해시를 유지한다. 구현 검토를 다시 열지 않고 기존 보조1명이 manifest의 사용자 제외/필수 의존성만 읽기 전용 대조한다. 명시된 파일만 staging하고 커밋 후 실제 파일 목록·staged0·사용자10파일 보존을 확인한다. 이번 요청은 로컬 커밋이며 push·Unity/ORCA/Firebase 실행을 추가하지 않는다.

읽기 전용 범위 검토는 manifest127+제외10=전체 변경137, 소스51개 해시 일치, C#28개 대응 CodeMap/.meta 누락0으로 종료했다. 새 파일까지 포함한 staged 기본 공백 검사는 Unity 생성 JSON .meta2개의 빈 값 뒤 공백과 C#3개/CodeMap1개의 마지막 빈 줄을 보고했다. 기존 검증 소스와 Unity 자산을 보존하기 위해 이 형식은 수정하지 않았다. Scene/Prefab/.meta를 제외하고 EOF 빈 줄만 허용한 코드/문서 검사로 다른 공백 오류가 없음을 확인한다. Git의 전역/프로젝트 설정은 변경하지 않는다.
