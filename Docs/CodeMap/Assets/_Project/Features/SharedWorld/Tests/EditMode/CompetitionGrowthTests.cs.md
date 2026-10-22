# CompetitionGrowthTests.cs

- 역할: 승인된 v3 RGB·모험가·런 성장의 의미 있는 숫자와 구형 보존/재현을 Unity와 .NET 동일 소스로 검사한다.
- 핵심 동작: 별도 v3 JSON을 로드하고3종 시작값/AI/계정HP/사본 격리,9기술×3종 수동 계산값, 합산 후 내림, 태그 장비/패시브, 보호막·처치 이후 생략·효과0·반격을 확인한다.
- 입출력: NUnit18개 사례. 공유 fixture는 새 정의/프로필/경기이며 실제 저장·사용자 DB·네트워크를 사용하지 않는다.
- 사용하는 대상: CompetitionRules/Stats/Combat/CommandRules/EncounterRules, LoopJson, 기존 CompetitionRulesTests.NextCommand/Definition.
- 사용하는 쪽: SharedWorld.EditMode.Tests와 Server/DiceWarrior.Competition.Tests가 같은 파일을 컴파일한다.
- 관계 근거: 도메인 직접 호출과 csproj 링크. 테스트에서만 가상 적/관찰용 경기 상태를 준비한다.
- 상태/수명: 경기마다 독립 상태를 만든다. XP 사건 뒤 HP/RGB/계정 불변, 새 경기의 레벨 초기화를 확인한다. 인간 명령 kind/arg/target을 기록해 AI 포함 전체 상태를 다시 실행하며 중간 직렬화 복원을 끼운다.
- 검수: 예상 피해/보호는 리터럴이며 구현 계산식으로 기대값을 만들지 않는다. v1/v2의 새 중첩 필드 생략과 균일 능력값·왕복 직렬화를 확인하며 기존 골든은 변경하지 않는다. 잘못된 분모/누락스탯/버전혼합/accountHp 불일치를 거절하는지도 검사한다. 실행 결과는 Task REPORT/TRX가 기준이다.
