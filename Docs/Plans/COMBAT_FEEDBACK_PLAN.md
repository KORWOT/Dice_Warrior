# COMBAT_FEEDBACK_PLAN

상태: IMPLEMENT / COMPLETE. 사용자 2026-09-08 명칭 수정 및 조합·카드 선택·공격 피드백 요청.
목표: 와일드 카드 표시, 실제 조합 단계 텍스트와 결과 유지, 선택 카드 강조 및 전투 타격·피해량/수호 표시로 행동을 읽을 시간을 준다.
대상: D:/UnityProject/Dice_Warrior. 기존 완료 LOBBY_MAP_DICE 결과를 기준으로 좁게 확장한다. 기본 저장·기존 사용자 변경은 보존한다.

## 계약
- trial ID/enum/schema/게임 규칙은 유지하고 사용자 표시의 시련 카드를 와일드 카드로 정정한다.
- 조합 label과 현재 저장 config의 priority 정렬로 단계(높을수록 상위)를 표시한다. 운명력은 실제 state 값이며 카드 등급과 동일시하지 않는다. KoreanText.HandSummary(RunState,bool multiline=false)를 공용 표시 경계로 쓴다.
- 주사위 굴림 확정·저장은 즉시 1회. 기존 rolling 뒤 결과를 기본 0.9초 유지하고 결과 text를 짧게 확대한다. DiceRollUI.resultHoldSeconds 필드로 조절한다.
- 실제 제시 카드 클릭만 선택 연출한다. 명령/저장은 기존처럼 먼저 1회 성공시킨 뒤 old view를 유지하며 선택 카드 pulse(기본 .22초), 전투 피드백, 다음 Render 순서다. 실패는 성공 연출 없이 기존 오류 흐름. 연출 내 난수·명령 실행 없음.
- CombatFeedbackData는 선택 카드 label/grade/effect와 before/after HP의 확정 표시 스냅샷이다. 실제 HP 감소와 공격량/막힘·수호 획득을 구분하고, 처치 시 반격을 표시하지 않는다. 전투 계산 재구현·스키마 변경 없음.
- CombatUI의 authored actionFeedback/damageFeedback Text, hitFlash Image, 기존 arena로 공격/적 반응을 순서대로 표시한다. Player/Enemy HP는 연출 시점에 해당 확정치로 업데이트하고 최종 전체 Render는 연출 뒤. actionFeedbackSeconds 기본 .38초/단계, shakePixels 기본 9, feedbackHoldSeconds 기본 .25초. 전투 영역만 흔들고 버튼은 고정한다. 선택 pulse는 FateDiceWidgets.AnimateCardSelection(string key,float seconds). 중복 입력 잠금·종료 정리와 원래 transform/color 복원.
- CombatUI.feedbackVersion 및 DiceRollUI.feedbackVersion으로 원본 작성 1회·사용자 편집 보존. Scene/캐릭터/이미지 추가 없음. 영구 성장 미포함.

## 경로/담당
Main 기존: Run/Presentation/RunUIController.cs, RunUIData.cs, FateDiceWidgets.cs, CombatUI.cs, DiceRollUI.cs.
보조 A 기존: Run/Presentation/KoreanText.cs, Run/Editor/UIWorkbenchPreview.cs, KoreanUiAuthoring.cs, LobbyPreparationAuthoring.cs, PlayWorkbenchWindow.cs, PlayWorkbenchSession.cs. 신규 Run/Editor/CombatFeedbackAuthoring.cs. 명칭·표시helper·원본 authoring 담당.
보조 B 신규: Run/Tests/PlayMode/CombatFeedbackTests.cs. 실제 저장 oracle/버튼/시간·수명 검증. 필요시 기존 직접 영향 Tests의 의미 변경은 Main이 최소 adapter만 수정하며 assertion 약화 없음.
명시적 자산 허용: Run/Prefabs/MenuUI.prefab(정확한 기본 문구만), CombatUI.prefab(피드백 참조/원본 값), DiceRollUI.prefab(결과 공간/hold 값). Editor API로만 수정. 다른 프리팹·씬·규칙/config/패키지/ProjectSettings/폰트/이미지/기본 저장 불변.
Main 문서: 변경 C# 1:1 CodeMap, 직접 관계 RunScreenView/RunSession/CombatRules/DiceRules/FateDiceVisualCatalog, INDEX/PLAN/REPORT. Docs/AI/CODEX_WORKFLOW.md는 프로젝트에 없어 현재 AGENTS 계약을 적용한다.
동시 최대 3, 고유 보조 최대 2, 재귀 0. 보조는 cwd artifacts/combat-feedback 아래 staging만 작성, 같은 파일 단일 작성자. Main만 실제 D 프로젝트/Unity 직렬 변경. 기존 idle 에이전트는 사용하지 않는다.

## 실행/종료
- [x] 기준 Git/파일 hash/기본 저장 백업. 실패 검사는 초기 authored feedback 계약 1건을 실제 RED로 실행.
- [x] Main/controller·뷰와 보조 표시·authoring/검사를 독립 staging한 뒤 순서대로 import→recompile→지정 prefab authoring.
- [x] 신규 검사: 실제 조합 우선순위/사용자 label/무변경, 결과 표시·hold, 선택 후 체크포인트 1회/입력 차단/화면 지연, 실제 피해·수호·반격/처치/패배, 흔들림 및 종료 복원. 기존 전체 Edit/Play도 실제 실행. 720×1280/1600 레이아웃 포함.
- [x] Unity만 직접 버튼 클릭으로 굴림/선택/전투 피드백 관찰. 기존 권한 유지, 입력 충돌 시 사용자 작업 방해 금지.
- [x] 최초 검토 1묶음+필수 Finding 수정·재검증 최대 총 2묶음. 새 subtask로 예산 재시작 금지. Evidence 기록, 소스·CodeMap·보호 경계 확인, 정지 Editor로 마무리.
- [x] REPORT에 실제 PASS/미실행/외부 로그 영향을 구분. Git commit/stage/branch 없음.

최종: 최초 검토 1묶음, 필수 수정·재검증 1묶음(CF-A01/CFB-B01 해결), 남은 필수 Finding 없음. PlayMode 77/77, EditMode 128/128 PASS. Unity 직접 선택·굴림과 표시 캡처 확인, 정지 후 전투 미리보기 표시. 상세 증거 및 미실행 실기기 범위는 [REPORT](../Reports/COMBAT_FEEDBACK_REPORT.md)에 기록했다.
