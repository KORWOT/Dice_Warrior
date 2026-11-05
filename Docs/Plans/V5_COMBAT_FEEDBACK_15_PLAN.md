# v5 전투 판단·결과 피드백 1차 PLAN

2026-10-25 · IMPLEMENT · 기준 `f7ba0e183ee8ecd5e4d6c851cdacb9d92f477fd2`

## 승인 계약

사용자는 UI14의 1차(UI14-01·02·03): 실행 전 피해/HP 예상, 승리 보상/성장/AI 결과 요약, 전투 모험 메뉴 직접 진입을 구현하고 주요 실제 캡처/문서를 갱신하도록 승인했다. [점검 REPORT](../Reports/V5_UI_PLAYFLOW_AUDIT_14_REPORT.md)의 해당 세 항목과 AC를 구현 기준으로 삼는다. UI14 자체는 완료된 조사 이력으로 유지한다.

설계: 동일 결정적 resolver가 복제 참가자에서 이미 계산한 값을 표시 전용 Preview로 투영한다. App은 저장 성공 전후 상태의 확정 변화만 읽고 최근 요약을 UI 수명 안에서 유지한다. Board/Combat의 authored 표시와 조회 콜백을 추가하며 규칙·명령·저장 schema는 변경하지 않는다. Unity 6000.6.0f1/uGUI/C#·기존 .NET/Unity 검사 채널을 사용한다.

## 필수 AC와 경계

- 전투 선택 시 적 공격 타입/약점·저항 및 총 HP 피해·보호·받을 HP 피해·실행 후 HP를 표시한다. HP 기준은 승리 보상/레벨 회복 전. 정상·강공격·방어·상성·보호·처치 후 생략·꽝·사망·20턴 제한과 일치한다. 굴림 전/리롤 중에는 미공개 결과가 새지 않는다.
- 전투 종료 후 승패·HP/골드/XP/레벨/명예 변화와 AI 결과의 조회 진입을 보드에 비차단형으로 표시한다. 관전 종료 사유/남은 인원은 최근 알림에 가리지 않는다. 결과/시련 완료까지 이어진다. 자동 모달/새 계속 명령/새 영구 이력은 추가하지 않는다.
- 재개·새 경기·복원·실패 시 다른 경기/이전 revision의 요약을 보여주지 않는다. 재개에서 전후 변화가 없으면 저장된 상태/AI 기록만 표시하며 차이를 추정하지 않는다. 읽기 전용 상세와 메뉴 조회로 보상/명령/RNG/저장값을 변경하지 않는다.
- 전투에서 바로 모험 메뉴를 열고 되돌아오거나 진행 보존 후 로비/이어하기가 가능하다. 실제 포기와 진행 보존, 새 모험 경고는 유지한다. 연출 중 입력 잠금 유지.
- 실제 프리팹/포인터·확정 명령 검증과 주요 캡처, 720×1280 및 390×844/360×640 모의 안전영역을 확인한다. 미실행 실기기 검사는 명시한다.

비목표: UI14-04~08, 아트·음향·진동, AI 자동 진행, 밸런스/조합/성장/재화/정산 변경, Firebase, 새 저장 DTO, 사용자 저장 사용, ORCA, 커밋/push/브랜치 변경. 현재 checkout에서 단독 Editor를 사용하며 사용자 변경은 해시로 보존한다.

## 허용 경로·쓰기 담당

SharedWorld 이하를 기준으로 작성하며 각 수정 C#의 `Docs/CodeMap/<상대 경로>.md`도 같은 담당이 갱신한다. 다른 파일은 발견 시 먼저 PLAN에 필요성을 기록한다.

- 보조 A(순수 예상): `Domain/CompetitionV5Data.cs`, `Domain/CompetitionV5Skills.cs`, `Tests/EditMode/CompetitionV5Tests.cs` 및 대응 CodeMap. 허용된 Preview만 확장하고 저장 상태 자료는 변경하지 않는다.
- 보조 B(확정 요약): `Presentation/CompetitionApp.cs`, `Presentation/CompetitionApp.V5Ui.cs`, `Presentation/CompetitionV5UiText.cs`, `Tests/EditMode/CompetitionV5UiProjectionTests.cs` 및 대응 CodeMap. 별도 클래스를 만들 필요가 있으면 기존 파일 안의 UI 전용 자료로 제한한다.
- 메인(화면/검증/통합): `Presentation/V5DiceAreaView.cs`, `Presentation/V5ShellView.cs`, `Presentation/CompetitionScreen.cs`, `Editor/CompetitionCombatUiAuthoring.cs`, `Editor/V5ShellAuthoring.cs`, 필요 시 직접 관계 `Editor/V5RedesignAuthoring.cs` 및 대응 CodeMap. `Tests/PlayMode/CompetitionCombatUiTests.cs`, `CompetitionUiImprovementTests.cs`, `CompetitionDiceRedesignTests.cs`, `CompetitionV5FlowTests.cs`와 대응 요약 중 직접 영향받는 검사만 수정한다.
- 메인 촬영: 기존 `Server/Tools/V5UiRedesignCapture.cs`와 그 CodeMap에 새 UI15 촬영/비율 메서드 추가. 과거 UI12/UI13 출력은 덮어쓰지 않는다.
- **자산 쓰기 허용은 `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab` 하나**이며 Editor API로만 갱신한다. 기존 GUID/meta 유지. Title/폰트/설정/SO/다른 프리팹은 읽기 전용.
- 메인 문서: 이 PLAN, `Docs/Reports/V5_COMBAT_FEEDBACK_15_REPORT.md`, Docs/README.md와 CodeMap/INDEX.md의 이번 항목, `Docs/UI/V5Feedback/`의 주요 사진·갤러리·설명·manifest/validation·Validation 사진, `artifacts/v5-combat-feedback-15/`의 진단/원본 근거.

시작 staged0·기존 변경137경로. 전체 경로/해시는 artifacts의 baseline.json에 기록했다. README/INDEX의 기존 사용자·UI14 항목을 유지한다. Docs/AI 운영 보조 파일은 현재 checkout에 없으며 AGENTS.md 직접 규칙을 따른다.

## 공통 인터페이스

- A가 `V5CombatPreview`에 공개 표시 필드 추가: `int playerHpBefore, playerHpAfter, enemyHpBefore, enemyHpAfter, enemyShieldAfter, damageApplied, shieldAbsorbed, block, retaliationDamage`; `bool retaliation, enemyDefeated, playerDefeated, turnLimitDefeat`. 기존 skills/noOp/status 유지. 실제 TurnLog/복제 상태에서만 읽으며 명령/보상을 실행하지 않는다. turnLimitDefeat는 적 생존·마지막 행동 기준으로 계산한다.
- 메인이 `CompetitionScreenData.v5Menu` Action과 Area의 optional 마지막 `Action showMenu=null` Bind 인자, authored `menu` 버튼을 추가한다. B는 v5 전투 렌더에서 `data.v5Menu=()=>V5Menu("adventure")`를 전달한다.
- 메인이 `V5ShellData`에 `string recentTitle,recentBody,boardStatus` 및 `V5ShellAction recentAction`을 추가한다. B는 보드/관전에 최근 결과(최대 제목1줄·본문3줄)와 `AI 결과 보기` 조회 Action을 제공한다. 긴 상세는 별도 읽기 전용 메뉴이며 조회 후 같은 보드로 복귀한다. 제목/본문은 보관 데이터가 아닌 UI 전용 문자열이다.
- 원본 명령/저장 타입·callback ID 계약은 유지한다. 새로운 결과 표시의 클릭은 명령을 추가하지 않는다.

## 작업·검증 순서

- [x] 기준 보존·Editor 상태 확인·계약/허용 프리팹 확정.
- [x] A: 기존 Preview 회귀에 결과 예상을 추가해 먼저 실패 확인 → 복제 resolver 투영 구현 → 동일 실행 결과/순수성 확인.
- [x] B: 확정 변화/복귀 수명과 텍스트 검사를 먼저 추가 → App 요약/AI 상세/메뉴 콜백 구현 → 해당 순수 투영 검사 확인.
- [x] 메인: authored 추가 참조·전투/보드 배치·결과 표시/포인터 검사 연결. 전투 회전·리롤/연출 중 예상 공개 경계를 유지한다.
- [x] 메인만 Unity 컴파일→Editor API 프리팹 갱신→직접 Unity UI/순수 Preview 검증. .NET은 기존 linked EditMode 프로젝트로 공유 규칙 회귀를 확인한다. 관련 구형 UI도 보존 검사한다.
- [x] 주요 화면 촬영/직접 시각 확인·작은 비율 확인·CodeMap/색인/REPORT와 최종 보호 해시 기록.

검토 초점: 모두 꽝에서도 적 행동, 적 선처치 후 받는 피해0, 승리 후 회복과 사전 예상의 기준 차이, 리롤 중 비공개, 오래된 요약의 재개/복원 오염, 관전→전체 종료 전환, 작은 화면 입력/읽기 경계. 과거 PASS 수치를 이번 PASS로 사용하지 않는다.

## 위임·검토 상한

보조 최대2명 동시, 누적2명, 메인 포함 최대3명. A/B의 파일 경계는 위와 같고 재귀·Unity/Git/자산/공통색인 쓰기 금지. 메인은 UI/통합을 진행한다. 보조는 자기 범위 검사를 작성하고 .NET 실행은 메인과 순서를 맞춘다. 최초 통합 검토1묶음에 두 보조의 담당 외 경계 검토를 포함한다. 별도 검토/수정 예산을 만들지 않는다.

최초 통합 산출물 검증 이후 수정·재검증 총2회. 새 필수 결함은 ID·근거·영향·해결 조건과 회차를 기록한다. 완료 조건 충족 시 종료하고 필수 미검증/예산 소진이면 완료 처리하지 않는다. 진행 기록은 이 PLAN과 artifacts의 progress.md에 남긴다.

## 완료 기록

.NET235/235, Unity EditMode235/235, v5 UI29/29, 구형 UI25/25 통과. 주요8장과 작은 비율8장을 최종 확인했다. 수정·재검증2/2회, 필수 잔여 Finding0. 제품 규칙·저장 schema 변경 없음. 상세 판정과 사용자 변경 보존은 [REPORT](../Reports/V5_COMBAT_FEEDBACK_15_REPORT.md) 및 artifacts/v5-combat-feedback-15/integrity.json에 기록한다. 실기기 검사는 NOT_RUN이며 커밋은 하지 않았다.

## 2026-11-05 로컬 커밋 승인

사용자가 이번 작업 정리·커밋을 승인했다. 구현 완료 당시의 커밋 비목표를 이번 로컬 커밋에 한해 갱신한다. 제품 구현과 수정·재검증2/2회 판정은 다시 열지 않는다. 최종 integrity의55경로만 포함하고 기존 변경137경로는 제외한다. README/CodeMap INDEX는 UI15 추가분만 index에 반영하며 작업 파일은 보존한다. 현재 HEAD는 동일 f7ba0e1, 시작 staged0이고 마지막 검증 이후 대상55파일의 해시가 모두 같다.

메인이 PLAN/REPORT·스테이징·커밋을 단독 작성한다. 보조 최대1명 동시/누적1명은 기존 검증 JSON/TRX·사진 해시의 읽기 전용 확인만 맡으며 파일·Unity·Git 쓰기와 재귀 위임은 금지한다. 이는 제품 재리뷰나 추가 구현 회차가 아니다. commit-before.json/commit-files.txt에 범위·보존 해시를 기록하고 스테이징 내용, 로컬 커밋 경로, 제외 파일 해시를 확인한다. 원격 push는 하지 않는다.
