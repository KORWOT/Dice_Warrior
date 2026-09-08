# COMBAT_FEEDBACK_REPORT

상태: IMPLEMENT / COMPLETE. 2026-09-08, D:/UnityProject/Dice_Warrior.

세팅의 시련 카드 표시를 와일드 카드로 정정하고, 주사위 조합을 읽을 시간과 카드 선택·전투 반응 연출을 추가했다. 기존 시작 카드 ID와 저장 스키마, 조합·등급·피해 계산은 유지한다.

## 최종 동작과 편집

- 준비 로비 및 플레이 작업실에 `와일드 카드`로 표시한다. 기존 `trial-*` 입력 키·필드·enum은 호환성을 위해 유지하며, 사용자 편집 문구는 정확한 기본 문구 마이그레이션 외에 덮어쓰지 않는다.
- 주사위 정착 후 실제 조합명·조합 단계 N/10·운명력을 텍스트로 표시하고 기본 0.9초 유지한다. 단계는 현재 저장 설정의 priority 순서이며, 카드 등급이나 등장 확률과 동일시하지 않는다. 텍스트는 짧게 확대했다 복원한다.
- 운명/행동 카드 선택 시 선택한 카드만 약 0.22초 확대·강조한 뒤 진행한다. 실제 명령과 checkpoint는 클릭 시 한 번 먼저 성공시키므로 중복 입력·중단·재시작이 추가 행동을 만들지 않는다.
- 전투는 선택 강조 → 플레이어 공격/수호 → 생존 적 행동 → 다음 턴/보상/결과 순서다. 기본 단계 시간은 각각 0.38초, 마지막 유지는 0.25초다. 전투 영역을 약하게 흔들고 색상 flash와 떠오르는 피해 텍스트를 표시한다. 버튼은 흔들리지 않는다.
- `공격 10 / 막힘 4 / 적 체력 -6`처럼 계산 공격량과 실제 HP 감소를 구분한다. 획득 수호·흡수량·실제 받은 HP 피해도 따로 표시한다. 과잉 피해를 HP 감소로 부풀리지 않으며 처치한 적은 반격하지 않는다. 추가 치명타·다단 공격 규칙은 만들지 않았다.
- `Fate Dice > 플레이 작업실`의 UI 원본 편집에서 조절한다. `CombatUI.prefab`: actionFeedback/damageFeedback/hitFlash, Action Feedback Seconds, Shake Pixels, Feedback Hold Seconds. `DiceRollUI.prefab`: Result Hold Seconds와 결과 텍스트 영역. 원본에 연결과 예시 문구가 보이고 런타임 바인딩은 예시를 지운다. `CombatFeedbackAuthoring.Apply()`는 최초 버전 적용 이후 원본 편집을 보존한다.

## 실행 증거

증거 루트: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/combat-feedback/`.

| 검사 | 결과 | 증거 |
|---|---|---|
| 초기 authored RED | 실제 1건 예상 실패: actionFeedback 부재 | red-status.json |
| 초기 신규 피드백 | 12건 중 11 PASS / 좌표 정확 동등 1 FAIL | new1.json |
| 초기 전체 PlayMode | 75건 중 74 PASS / 같은 좌표 비교 1 FAIL, 기존 63개 모두 PASS | play1.json |
| 초기 전체 EditMode | 128/128 PASS | edit1.json |
| 최종 전체 PlayMode | 77/77 PASS, 182.88초 | play-final.json |
| 최종 전체 EditMode | 128/128 PASS, 12.67초 | edit-final.json |
| 컴파일 | completed, failed=false, errors=[] | compile-fix1.json |
| 원본 재적용 | 세 프리팹 바이트 동일 | authoring-idempotence.json |
| 보호 범위 | 기준 259개 중 허용된 기존 14개만 변경, 기본 저장 유지 | boundary-final.json |
| 직접 Unity 버튼 | 카드 선택·공격/적 반응·다음 굴림과 조합 표시 | native-feedback-capture.txt, native-player.png, native-enemy.png, native-dice-result.png |

직접 클릭의 실제 표시 값: 강공격(고급) 공격 22 / 막힘 0 / 적 체력 -22, 적 행동 방어 / 적 수호 +8, 다음 여섯 주사위 [2,4,4,2,5,1] → 투 페어 / 조합 단계 2/10 / 운명력 2. Unity Game 캡처에서 텍스트와 주사위 영역의 겹침이 없고 확정 결과 뒤 다음 카드가 표시되는 것을 확인했다.

최종 자동 검사는 실제 UI 포인터와 공유 RunSession oracle 및 수작업으로 계산한 숫자를 비교한다. 두 세로 비율(720×1280, 720×1600), 결과 유지/확정 여섯 값, 조합 우선순위와 사용자 label, checkpoint 1회, 이중 클릭·메뉴 입력 차단, 실제 피해·전체 방어·적 수호·처치·패배, 저장 실패, 화면 닫기와 저장 재개, 굴림 중/결과 유지 중 controller disable→enable을 포함한다. 실기기 터치나 APK/IPA 검사는 실행하지 않았다.

초기 첫 RED 실행은 Pipeline이 running 상태만 반환하여 취소했다. 스크립트 검색을 다시 로드한 실제 재실행에서 예상 RED를 확인했다. completed/0 또는 미완료 호출을 PASS로 사용하지 않는다. 테스트 중 외부 Unity AI 로그는 무시하도록 변경하지 않았다.

## Finding과 횟수

최초 검토 1묶음, 필수 수정·재검증 1묶음. 남은 필수 Finding: 없음.

- CF-A01 / P2: controller가 굴림 coroutine을 중단할 때 결과 popup이 남아 재활성화 후 입력을 가릴 수 있었다. OnDisable에서 소유한 최상위 DiceRollUI를 닫고, OnEnable이 확정 상태를 다시 표시한다. 굴림 중·결과 유지 중 각각 재활성화 후 popup 없음·카드 사용 가능·상태/RNG/저장 bytes 동일·checkpoint 1회를 검사한다.
- CFB-B01 / 검증 결함: popup cache 재부모화 후 RectTransform 위치를 Vector3 bit 단위로 비교해 표시상 같은 좌표에서 실패했다. 결과 표시 중과 닫기 후 여섯 면 모두 거리 0.001 이하를 요구하고 G9 정밀 진단을 제공하도록 수정했다. 여섯 값·hold·저장·회전·scale assertion은 유지했다.

## 범위와 종료

기준 Git은 main/433edddc872433ead6db52a872ea0fcac659d92e다. 작업 전 Git 목록과 원본 259개를 백업했다. 기존 C# 11개 수정, C# 2개 추가, 허용 프리팹 3개만 Editor API로 수정했다. 변경 C# 13개의 CodeMap 및 직접 관계 5개, INDEX 68개 소스, PLAN/REPORT를 동기화했다. 씬·다른 프리팹·규칙/config·패키지·ProjectSettings·폰트/이미지와 기존 저장은 변경하지 않았다. Git 커밋·스테이징·브랜치 변경은 수행하지 않았다.

기본 저장 SHA-256(시작/종료): B67ACC8E22F8C1FC234010983BBE07929D825B8C0CB68B0952FDFF6541D6C005.

Unity의 격리 플레이를 직접 중지해 기존 Title 씬으로 복귀했다. Play는 정지 상태이며 플레이 작업실과 저장되지 않는 전투 상황 미리보기를 Scene 뷰에 열어 두었다. 기본 저장과 원본 씬은 그대로다.
