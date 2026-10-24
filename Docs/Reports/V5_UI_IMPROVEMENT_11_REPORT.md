# v5 UI 개선 구현 REPORT

2026-10-24 · IMPLEMENT 완료 · 물리 모바일 검증은 후속

[PLAN](../Plans/V5_UI_IMPROVEMENT_11_PLAN.md) · [주요 12화면](../UI/V5Improved/index.html)

사용자 제공 로컬 원문 `Docs/Dice_Warrior_v5_UI_개선명세.md`를 기준으로 구현했다. 해당 원문은 사용자 파일로 보존하며 이번 커밋에는 포함하지 않는다.

## 결과와 적용 범위

명세 UI-01~17의 v5 화면·조작·표시를 실제 CompetitionScreen 프리팹에 반영했다. 3바퀴 장비 보상 표시 예외와 주사위 연출 겹침을 해결했다. 게임 수치·조합 우선순위·AI 정책·순위·보상·저장 스키마·서버 정산은 변경하지 않았다. 기존 v1~v4 경기는 저장된 정의와 기존 화면으로 재개한다. 사용자에게 새로 결정받아야 할 게임 규칙은 없었다.

- 로비·모험가·출전·사건·보상·계정·결과는 전용 Shell로 구성했다. 빈 보드를 없애고 후보 카드와 조회 도구·모험 메뉴·상세5탭을 분리했다. 긴 목록은 스크롤하며 6개를 넘는 후보는 화면 안의 페이지로 나눈다.
- 출전은 선택 즉시 저장하고 같은 화면에서 연속 변경한다. 다음 새 모험과 현재 저장 경기를 구분한다. 계정은 실제 승인 전후 재화/성장, 대기·부족·실패·구형 기록 보류를 표시한다.
- 사건은 실제 비용·최소 HP·장비 조건·행운 포함 골드·XP·명예·회복 상한·적 상성을 보여 준다. 완주 보상은 선택 전후 값을 비교하고 선택 즉시 지급 확정임을 명시한다.
- 주사위 슬롯 ①②③과 물리면 1~6을 구분하고 실제 자동 계획의 사용 슬롯을 기술과 연결한다. 전체3기술 예상, 저장된 실행 결과의 HP·보호, 이번 굴림/조회 중을 표시한다. 카드의 중심축·크기·회전 폭·보관 전환을 조정해 영역을 침범하지 않는다.
- 조회 닫기·뒤로는 원래 메뉴/전투 정보로 복귀한다. 진행 유지와 포기·새 모험 교체를 분리한다. 사건 이후 AI 확정 결과 재생 안내와 직전 AI 행동 요약을 보강했다.

## 실제 검증

| 채널 | 최종 결과 | 원문 증거: artifacts/v5-ui-improvement-11/run-01/ |
|---|---|---|
| .NET 순수 규칙·저장·표시 회귀 | 207/207 PASS, skip0 | domain-correction-1.trx |
| Unity 컴파일 | 오류0 | Pipeline recompile_status completed/failed=false |
| Unity 개선 UI | 18/18 PASS, skip0 | correction-1-ui.json |
| Unity 기존 화면·계정·재개 | 25/25 PASS, skip0 | legacy-flow.json |
| 별도 경계 확인 | 23항목 PASS(18+5) | boundary-audit.json / lap-round-boundary.json |
| 실제 프리팹 주요 화면 | PNG12장 확인, manifest BLOCKED0 | correction-1-capture.json / Docs/UI/V5Improved/manifest.json |
| 파일/관계 보호 감사 | 예기치 않은 변경0, CodeMap/meta 누락0, staged0 | final-audit.json |
| Editor 종료 상태 | Title, dirty=false, Play 정지 | editor-final.json |

Unity 6000.6.0f1의 기존 연결 Editor에서 직렬 실행했다. 주요 사진은 실제 프리팹을 격리 PreviewScene에 표시한 720×1280 PNG다. 검사에는 720×1600·360×640, 가상 안전영역, 긴 한국어, 실제 EventSystem 첫 raycast와 버튼 클릭, 스크롤·페이지 접근을 포함했다. 연출 검사는 1~3개 주사위·모든 보관 마스크·굴림/리롤/보관/해제의 실제 애니메이션 콜백을 여러 시점에서 검사했다.

3바퀴 회귀는 합법 초기 프로필 growth20을 모든 참가자에게 동일 적용하고 **실제 start→travel→choose→roll/reroll/execute→lap 명령**으로 도달했다. completedLaps를 주입하지 않았다. 장비5개 접근·상세조회·로비/이어하기의 원문 불변, 마지막 장비 실제 포인터 선택, claim1회·최종 시련 진입을 확인했다. 해당 첫 보상/5번째/최종 시련 PNG는 `artifacts/v5-ui-improvement-11/tests/`의 각 검사 폴더에 보존한다.

경계 확인은 실제19회 꽝 실행 뒤20번째 행동의 UI 카운터/경고, 적 생존 시 탈락과 마지막 행동 처치 우선, 중첩 정보창 왕복 불변,29→30라운드와 AI 후 종료,백업 조회 취소, 새 모험 교체·이전 원문 보존을 검사했다. 고유 격리 계정/경기만 사용했다. 추가5항목은 합법30라운드3바퀴 보상 fixture에서 실제 카드 콜백→시련 승리→AI3명→전체결과를 확인했다. 해당 경계의 초기 상태·적HP1은 명시적 fixture이며 자연진행 도달 검사는 앞의 별도3바퀴 회귀다.

## 최초 Finding과 수정 횟수

최초 통합 검토는 코드 계약·순수 검사·Unity17개·11장 캡처를 한 묶음으로 취합했다. UI11-R01~05를 **수정·재검증 1회**에 해결했다(상한2회, 잔여1회). 보조 작업에 별도 예산을 만들지 않았다. 이후 남은 필수 경계 확인에는 제품 수정이 없었다.

| ID | 최초 근거·영향 | 해결 및 확인 |
|---|---|---|
| R01 | 출전 Gear(d,null,id)의 null 접근으로 화면 진입 예외 | null은 다음 모험 후보 조회로 처리. 시작 무기2종 순수 회귀와 실제 출전 연속선택 PASS |
| R02 | 연출 검사에서 held만 주입해 hold history가 불일치 | 실제 combat_hold로 마스크 구성. 전체 연출 경계 PASS |
| R03 | 계정 요청 첫 Render가 Account.Busy보다 빨라 일반 대기문구 표시 | account 메뉴의 busy도 서버 대기로 표시. 지연/승인/부족/실패 PASS |
| R04 | 사건 화면은 기존 보드 cue가 숨겨져 AI 진행 안내가 없음 | Shell의 AI 결과 재생 안내와 확정 lastRound 표시. 실제 포인터 회귀 PASS |
| R05 | v5 보드에 구형 TRAVEL D6 표기 잔존 | v5만 이동 주사위·1~6칸으로 표시. 새 캡처 확인 |

남은 필수 Finding0. 최초 Unity9개는9 PASS, 신규전투3개는2 PASS/1 FAIL, 신규화면5개는3 PASS/2 FAIL이었다. 위 최종 결과로 해결을 확인했으며 최초 실패를 숨기지 않는다. Pipeline OR 이름 필터가0개를 반환한 호출은 유효 검증으로 계산하지 않았다. 프리팹의 `m_Name: ` 등 Unity 직렬화 공백은 텍스트 수동 편집하지 않았다. C#/문서 diff 검사는 별도로 통과했다. 동적 캡처 fixture의 미사용 선택적 실패 주입 필드 CS0649 경고3개는 제품 컴파일 오류가 아니다.

## D-01~10 확인 기록

모두2026-10-24 실제 소스와 검증으로 확인했으며 새 게임 규칙을 정하지 않았다. 아래 파일은 `Assets/_Project/Features/SharedWorld/` 기준이다.

| ID | 확정 내용 | 근거 / 영향 |
|---|---|---|
| D01 | HP비용은 소모. HP>비용 및 HP≥minHp, 골드≥비용, 장비 조건을 모두 확인 | Domain/CompetitionEncounterRules.cs Allowed/Choose/RewardGold → 사건 카드 |
| D02 | 시련 성공→완주→명예→HP 내림차순. 모두 같으면 공동순위, 탈락 자체의 추가 감점 없음 | Domain/CompetitionRules.cs Rankings/Rank, 동률 검사 → 결과/보드 |
| D03 | 굴림·보관·리롤은 행동턴 미진행. 실행·적 행동 뒤 내사망→적처치→턴제한 순.20번째 처치 우선 | Domain/CompetitionV5Dice.cs Apply, CompetitionEncounterRules.cs FinishCombat, 경계23항목 → 전투/정보 |
| D04 | 모험가/출전은 선택 즉시 선호 저장, 다음 새 경기만 적용 | Runtime/AdventurerSelectionStore.cs, CompetitionAccount.cs SelectLoadout, App 선택 경로 → 출전 |
| D05 | 출발 경계에서 멈추고 남은 이동 폐기. 보상 선택 시 claim/완주/골드/XP·레벨/회복/주사위·장비 지급.1·2회 후AI,3회 후시련 | CompetitionCommandRules.cs ClaimLap/GrantLapReward/PreviewLapReward → 보상 |
| D06 | 새 전투 행동 Begin에서 보관12/리롤1 초기화. 같은 hold 상태는 집계 전 거절, 모두 보관 시 리롤 거절 | CompetitionV5Dice.cs Begin/Apply/HoldUnavailableReason → 영역/정보 |
| D07 | 내 보호는 실행마다0부터 누적하고 해당 반격에만 적용. 적 방어/강공격은 확정 의도에 따라 판정 | CompetitionElements.cs ResolvePlanned → HP/보호/정보 |
| D08 | AI 전원 처리 후 전원 비활성→현재 라운드상한 검사.30라운드 인간·AI를 처리. 개인 종료 뒤 수동 다음AI라운드 유지 | CompetitionCommandRules.cs CompleteRound, 경계23항목 → 개인 종료/결과 |
| D09 | 계정 단계당HP5를 새경기 네명에게 동결. 구형active구매보류 해제 기능은 현재 공동 UI에 없음. 백업은 계정/출전/경기 구분 | Runtime/CompetitionAccount.cs, CompetitionStore.cs, CompetitionRules.Start → 계정/로비 |
| D10 | 꽝 제외,3눈 우선; 없으면 priority내림차순·ID순 첫2눈, 남은 슬롯순 단독. 동일 단독 반복 가능, 처치 후 후속 생략 | CompetitionV5Skills.cs PlanSteps/Preview,207검사 → 그룹/기술 |

## QA-01~26 매핑

기호: U=개선 UI18, L=기존 UI25, D=.NET207, B=경계23항목, C=최종 캡처12. PASS는 아래 명시된 실행/읽기 범위를 뜻하며 모든 장치·모든 복합 상태를 전수 검증했다는 뜻은 아니다.

| QA | 결과·근거 |
|---|---|
| 01 | PASS · C01, 출전 구성/새 모험, U 실제 시작 |
| 02 | PASS · U 출전 변경/이어하기, 현재/다음경기 분리 |
| 03 | PASS · U 실제 보드·사건·전투·보상·시련·개인종료의 로비/이어하기, 원문 불변 |
| 04 | PASS · U 주사위/무기/모험가 연속선택·재오픈, 현재 경기 불변 |
| 05 | PASS · D HP경계/골드·행운/정찰/표시, C05 비용·조건 |
| 06 | PASS · U 실제 사건 선택→AI안내→확정 요약·입력 재개 |
| 07 | PASS · U 1~3개/전 보관마스크/애니메이션 시점·안전영역 |
| 08 | PASS · D 12/13경계·중복거절, U 한도 안내/실행 유지 |
| 09 | PASS · D/U 전부보관/리롤 소진·보관 면 정지/불변 |
| 10 | PASS · D 648개 물리조합·사용슬롯/순서, U 카드그룹/포인터 |
| 11 | PASS · U 단독3개 전체 예상·그룹, 텍스트 높이 |
| 12 | PASS · U/D 전부꽝→기술없음/적행동·턴 증가 |
| 13 | PASS · D 처치후 생략/예상일치, B 실제 마지막 행동 처치 후 반격 없음 |
| 14 | PASS · B 실제19행동→20번째 경고/카운터/탈락·처치우선 |
| 15 | PASS · U 면/탭/읽기전용, B 전투정보→능력치→전투정보 복귀·정확한 상태 불변 |
| 16 | PASS · U 실제1·2바퀴·조회/복귀·3회lap기록, D 전후/상한/RNG 불변 |
| 17 | PASS · U 실제3바퀴·5후보 접근·단일claim·시련 진입 |
| 18 | PASS · U Back취소·원문불변, 포기확정→개인종료 |
| 19 | PASS · U 개인종료·재개·다음AI, C/B 실제 AI진행→전체결과 |
| 20 | PASS · C 실제 전원종료, B29→30·AI후제한, D동률/순위.30라운드3바퀴 보상 fixture에서 실제 보상 선택→최종 시련 승리→AI3명→라운드제한 결과 UI 추가5항목 PASS |
| 21 | PASS · U 승인대기/성공/부족/실패/구형보류; 실제 HTTP는 이번 검사 제외 |
| 22 | PASS · B 백업조회/뒤로 취소 원문불변, 실제 복원 대상은 기존 L과 소스 대조 |
| 23 | PASS · U 파일 잠금/연출 중단·pause/resume·재명령 없음, L 계정쓰기복구 |
| 24 | Editor PASS · U 세 크기·긴 한국어/스크롤/포인터/가상safeArea; 물리기기 NOT_RUN |
| 25 | Editor PASS · U/B 닫기/HandleBack 계층·임의확정 없음. InputSystem Escape 연결 소스 확인, 물리OS 제스처 NOT_RUN |
| 26 | PASS · U 취소/기존진행, B 실제 교체확정·계정값/이전원문 보존 |

## 문서·자산·사용자 변경 보호

- 새/수정 C# 모두 1:1 CodeMap을 동기화했다. 현재 프로젝트 C#83개와 Server C#42개, 기존 정리 도구의 요약을 유지한다. INDEX와 Docs/README는 현행 UI와 개편 전 자료를 구분한다.
- 자산은 승인된 `CompetitionScreen.prefab`만 Editor API로 저장했다. GUID `ae62a590a196a5944815a56b969507cf` 유지. 신규8개 프로젝트 C#의 meta는 Editor import로 생성했다.
- 기준1247파일 중 의도한25파일만 달라졌고 나머지1222파일은 SHA256 동일했다. 사용자 설정/폰트/패키지/기획 원문, Title 씬, 공용UI 프리팹, 이전89장 카탈로그·12장 묶음 모두 보존했다.
- 새 자료는 `Docs/UI/V5Improved/`의 PNG12장·manifest·README·index에 모았다. raw검증/추가사진/격리저장은 ignored artifacts에 남긴다. Git staged0, 커밋/푸시 없음.

## 남은 검증과 후속

실제 Android 장치의 터치48dp·OS 뒤로/제스처·장치 safeArea는 NOT_RUN이다. Editor 픽셀·설계단위를 dp로 간주하지 않는다.후속 장치 QA 범위이며 이번에 새 규칙·자동AI·Firebase·서버정산을 추가하지 않았다. [Android 접근성](https://developer.android.com/guide/topics/ui/accessibility/apps), [Unity 큰 화면 안내](https://developer.android.com/games/engines/unity/unity-large-screen)를 검토 근거로 사용했다.

## 사용자 승인 커밋 범위 · 2026-10-24

사용자의 후속 커밋 요청으로 UI11 산출물67파일을 지정했다. 위 테스트 결과는 구현 검증 당시의 실제 결과이며 이번 Git 작업에서 재실행하지 않았다. 사용자 설정/폰트/원문과 이전 UI_SCREEN_CATALOG_10의 자료133파일은 제외하고 바이트 해시를 보존한다. README/INDEX의 이전 카탈로그 행은 unstaged로 유지한다. 따라서 이 커밋의 코드 색인은 프로젝트 C#83개·Server C#41개이며, 작업폴더에는 이전 미커밋 촬영 도구1개가 더 있다. commit manifest/제외파일 해시/커밋 전후 대조는 ignored artifacts/v5-ui-improvement-11/commit/에 남긴다. 로컬 커밋만 승인되었으며 push/branch 변경은 범위 밖이다.
