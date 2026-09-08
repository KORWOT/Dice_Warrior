# CombatRules.cs
- 책임: 공유 피해/보호 계산, 적 예고 추첨, 플레이어 행동→승패→생존 적 행동→승패 순서.
- API: Evaluate(state,offer)→ActionEffect, Amount(stat,coefficient,minimum)→최종정수, IntentAmount→실제와UI가 공유하는예고량, Begin/Resolve→전투상태 갱신.
- 직접 사용: GrowthRules.Stats/Multiplier, config 행동/등급배율/적예고/최소효과, DiceRules.WeightedIndex(다음예고), RunState와OfferedCard.
- 호출자: RunSession의ChooseNode/ChooseFate/ChooseAction; Screen.RenderCombat와GUI선택정책은 Evaluate/IntentAmount를 읽는다. RunTests/SaveTests가순서·저장경계 검사.
- 계산: 위력/수호×카드계수×제시/원본등급비×명시태그배율. double 중간값, AwayFromZero 최종반올림, 양수효과최소값. 수호는보호량만 늘린다.
- 순서: 적보호막은플레이어행동후 만료. 적이죽으면 보상대기로즉시이동하며 반격/다음난수없음. 생존적예고실행후 플레이어보호막만료. 사망→Result, 생존→CombatRoll+새예고. 보상/진척지급은RunSession의별도명령이다.
- 상태/수명: 명령복제된 RunState만변경, SO불변. RNG는생존후새예고에만소비. 모든 제시행동을 비용/쿨다운 없이 실행한다.
- 증거: Edit98/98(전투·성장·저장), 실제GUI8/8(승리/패배/장비보정표시/중복입력), Console 최종 확인은REPORT참조.


- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.


## 전투 피드백 직접 관계

- RunUIController의 표시 스냅샷은 Evaluate(before,card), IntentAmount(before)와 확정 전후 HP 차이를 사용한다. CombatUI는 계산된 값만 받으며 적 사망 후 반격 억제·수호 만료를 재계산하지 않는다.
- 실행 증거: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
