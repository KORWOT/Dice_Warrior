# 등급별 연출·시작/종료 동기화 결과

상태: **IMPLEMENT 완료**. [PLAN](../Plans/PRESENTATION_LIFECYCLE_PLAN.md)의 사용자 신규 계약과 필수 검증을 충족했다. 보완 2/2 사용, 남은 필수 Finding 없음.

## 구현과 편집
`Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset`의 **Tiers**에서 낮은 조합부터 네 단계의 경계와 Entrance/Flourish/Read/Exit Seconds를 편집한다. 저장 priority 순위로 단계를 선택하고, 조합 색·보조색·Text Animator 태그는 기존 Styles를 유지한다.

| 단계 | 기본 구성 | 등장 | 강조 | 읽기 기본 | 퇴장 | 기본 결과 총길이 |
|---|---|---:|---:|---:|---:|---:|
| 일반 | 링 한 개와 작은 빛점, 중앙 문양 없음 | 0.16초 | 0초 | 0.35초 | 0.12초 | 1.18초 |
| 중급 | 두 원호·순차 점등·간단한 룬 | 0.32초 | 0.28초 | 0.55초 | 0.18초 | 1.68초 |
| 상급 | 회전 궤도·교차 광선·파동 | 0.5초 | 0.65초 | 0.8초 | 0.25초 | 2.3초 |
| 최상급 | 세 겹 파동·방사 광선·왕관 모양 강조 | 0.7초 | 1.3초 | 1.1초 | 0.4초 | 3.5초 |

- 표의 총길이는 기존 결과 유지 설정 0.9초를 최소 읽기 시간으로 반영한 값이다. 굴림 시간은 별도이며 FEEL에 더 긴 트랙이 있으면 실제 종료를 기다리므로 총길이가 늘어난다. 기존 hold=0은 연출 생략 계약을 유지한다.
- 카드 선택은 ActionCardView/FateCardView의 SelectionFeedback.playbackSpeed와 연결된 FEEL 트랙을 편집한다. 기존 0.22초 추측 대기를 제거하고 실제 player.IsPlaying 완료를 기다린다. playbackSpeed0은 생략한다.
- 전투는 CombatUI의 actionFeedbackSeconds(타격/반응)와 feedbackHoldSeconds(읽기)를 편집한다. 0 허용, 비유한/음수 거절. 노드 도착은 ExplorationUI 안 CampaignMapView.arrivalSeconds다.
- `Fate Dice > 등급별 연출 시간 원본 적용`은 지정 catalog 및 원본5개만 Editor API로 저장하는 migration이다. timelineVersion1 이후 기존 값/배치/색/FEEL 트랙을 다시 덮지 않는다.

## 실제 종료와 오류 복구 계약
- Controller는 시간을 추측해 다음 화면을 열지 않고 DiceRollUI.PlayPresentation → DiceResultFeedback의 전체 완료, 카드 FEEL의 완료, CombatUI.PlayFeedback의 완료를 순서대로 기다린다. 등장→강조→읽기→퇴장 구간이 끝나야 결과 popup을 닫는다.
- `RunUIController.Playback`의 Started/Ended 사건과 Label/State/StartedAt/EndedAt/Error로 연출 수명을 확인할 수 있다. 정상 Completed, 취소 Cancelled, 예외 Faulted, 제한 초과 TimedOut을 구분한다. 시각은 Unity 실행 이후의 실제 시간(초)이다.
- 저장 성공 뒤 시작한 **한 시퀀스 전체에 10초 제한**을 적용한다. 굴림+결과 또는 카드 선택+공격+반격+읽기가 한 시퀀스다. timeScale0에서도 적용하며 중첩 IEnumerator/WaitForSecondsRealtime/AsyncOperation을 매 프레임 감독한다.
- timeout 시 중첩 iterator의 finally를 정리하고 외부 FEEL/오라/흔들림/popup/입력 잠금을 해제한 뒤 확정 snapshot을 다시 표시한다. `Run presentation timeout: ...; elapsed=...s; limit=10.0s` 오류 로그에 연출 이름·phase·sequence와 경과 시간이 들어간다. 게임 명령·보상·저장·RNG를 재실행하지 않는다.
- Cancel도 같은 복구 경계를 사용하며 비활성 중 새 화면을 렌더하지 않는다. Dice popup BindingVersion과 Combat feedbackGeneration이 이전 coroutine의 반격/닫기/finally가 새 바인딩을 덮는 것을 막는다.
- 정상적인 게임 메인 스레드가 프레임을 실행하는 상황의 watchdog이다. Unity 자체가 완전히 멈추거나 프로세스가 중단되면 그동안 콜백·강제 정리를 실행할 수 없고, 다음 실행 프레임에서 초과를 판정한다.

## 검증 기록
연결된 Unity 6000.6.0f1 / Pipeline 0.6.0-exp.1에서 직렬 실행했다. 원문 증거는 `artifacts/presentation-lifecycle/`에 있다. 결과 발견0건·취소된 테스트는 PASS에 포함하지 않는다.

| 결과 파일 | 통과 | 확인 범위 |
|---|---:|---|
| presentation-final.json | 56/56 | 신규 lifecycle16/통합9, 기존 연출9·시드18 및 직접 표시/저장 회귀 |
| aura-tests.json | 32/32 | 참여 주사위/메시·색·alpha·범위·정착·정리 |
| combat-tests.json | 14/14 | 실제 버튼·피해·수호·반격·중복·중단·두 세로 비율 |
| boundary-tests.json | 5/5 | 표현 실패 후 저장 상태 보존·복구·소수점 transform 복원 |
| workbench-tests.json | 7/7 | 정지 미리보기/한글·메시/원본·저장 보호 |

총 114회 실행, FullName 중복 제거 기준 **113개 지정 검사 모두 PASS**. 컴파일 완료·오류0. 최종 집계와 보호 검사는 [final-verification.json](../../artifacts/presentation-lifecycle/final-verification.json)에 기록했다.

- 새로운 주사위/전투 timeout 검사는 실제 생산 원본 UI에서 버튼을 눌러 명령을 한 번 저장한 뒤 표시 기간을 60초로 설정한다. timeScale0에서 9.4초에는 Busy, 실제10초 뒤 TimedOut·오류 로그1회·종료1회·저장1회·기존 bytes를 검증한다. 이는 의도한 오류 주입 검사 로그이며 정상 연출에서 timeout이 발생했다는 뜻이 아니다.
- 긴 FEEL 검사에서는 0.04초 등장 설정에 실제 약0.5초 트랙을 넣고 0.2초 시점에도 Entrance/FEEL 재생 중임을 검사한다. 이후 읽기·퇴장·종료 시각까지 확인했다.
- 이번 작업에서 native Computer Use 수동 클릭은 하지 않았다. 실제 포인터/GraphicRaycaster 경로의 자동 PlayMode 검사다. 모바일 실기기 빌드·성능/전체 테스트 일괄 실행은 NOT_RUN이다.

## 검토·보완·보호
- 최초 통합 검토1묶음. PL-R01(옛 전투 반격), PL-R02(공개 취소 정리 누락), PL-R03(새 Dice binding 닫힘)은 독립 회귀에서 실제 FAIL을 확인한 뒤 보완1로 해결했다. 56/56 PASS 및 세 Finding 소스 해결 조건을 검토자가 재확인했다.
- 보완2는 두 기존 CombatFeedbackTests가 legacy summary Text의 고정 pulse만 관찰하던 계약을 새 전용 배너/오라 실제 모션으로 이전했다. 읽기·6면·저장1회·복원 assertions 유지, 14/14 PASS. 총 보완2/2 사용. 이전 DICE_COMBO_AURA의 예산/완료 기록은 재설정하지 않았다.
- 초기 테스트 코드의 공급사 어셈블리 직접 참조 오류는 기존 reflection 경계로 해결했다. 대기 중인 테스트는 cancel_tests로 종료하고 컴파일 성공 후 실제 검사를 실행했다. 발견0건 결과를 실행 증거로 사용하지 않았다.
- 변경·신규 C#과 캡처 helper의 1:1 CodeMap, 직접 관계, INDEX를 동기화했다. Assets/_Project C#102개, CodeMap 누락0. 소스/문서 공백 검사 통과(줄바꿈 정규화 경고만). Unity가 직렬화한 빈 문자열의 trailing space는 수동 정리하지 않았다.
- baseline 대비 manifest/lock/Title/DefaultFateDice/사용자 run.json hash 불변 확인. 기존 미커밋 작업/사용자·공급사 자산 보존, Git staging/commit/branch 변경 없음. HEAD 불변, staged0. 최종 보호 원문은 final-verification.json에 기록했다.
- 설정 재적용은 이미 적용된 상태를 반환했으며 지정 catalog/원본5개의 전후 hash가 모두 같다(authoring-repeat.json). Unity는 MainStage의 Title 씬으로 돌아왔고 Play 중 아님·dirty false다(final-editor-state.json).

## 시각 증거
최종 네 단계 × 720×1280/1600의 **8개 PNG를 모두 열어 확인**했다. 6개 주사위가 한 줄이며 눈·조합 이름이 가려지지 않는다. 일반의 단일 링부터 중급의 간단한 룬, 상급의 궤도·파동, 최상급의 다중 파동·광선까지 구성이 구분된다.

| 단계 fixture | 720×1280 | 720×1600 |
|---|---|---|
| 일반 / 투 페어 | [PNG](../../artifacts/presentation-lifecycle/TwoPairs-1280.png) | [PNG](../../artifacts/presentation-lifecycle/TwoPairs-1600.png) |
| 중급 / 트리플 페어 | [PNG](../../artifacts/presentation-lifecycle/ThreePairs-1280.png) | [PNG](../../artifacts/presentation-lifecycle/ThreePairs-1600.png) |
| 상급 / 포 카드 | [PNG](../../artifacts/presentation-lifecycle/FourKind-1280.png) | [PNG](../../artifacts/presentation-lifecycle/FourKind-1600.png) |
| 최상급 / 식스 카드 | [PNG](../../artifacts/presentation-lifecycle/SixKind-1280.png) | [PNG](../../artifacts/presentation-lifecycle/SixKind-1600.png) |

TierPreviewCapture는 실제 DiceRollUI/Workbench 원본을 고정 조합 DTO로 정지 렌더링하며 게임 명령/저장소를 실행하지 않는다. 시간 모션 증거는 위 실제 PlayMode 검사와 구분한다. 참고 이미지의 세밀한 일러스트를 그대로 재현한 결과나 모바일 성능 검증으로 주장하지 않는다.
