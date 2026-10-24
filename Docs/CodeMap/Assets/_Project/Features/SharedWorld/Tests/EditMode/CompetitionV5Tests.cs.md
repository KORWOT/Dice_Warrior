# CompetitionV5Tests.cs

- 역할: v5 정본·물리 주사위 선택·스킬 예상·명령 예약·AI 재현의 EditMode 회귀 검사.
- 핵심 동작: 시작 주사위의 공용4/전용1/꽝1, 인스턴스·슬롯·중복 물리면, 굴림/보관/해제/리롤/실행의 RNG·턴 경계, 꽝 무효과, 3눈→2눈→남은 단일 순서와 동률, 전용 권한·필수 필드·위조 결과 거절을 검사한다. 프리뷰의 HP/RNG/명령 불변과 실제 피해·흡수·보호·처치 뒤 생략을 대조한다.
- 입출력: 신규 shared-world-preview-5.json을 읽는 Definition, 테스트 전용 Combat/Selected/NextCommand fixture, NUnit 단언. 12회 보관 변경 뒤 실행과 다음 턴 초기화, 2045명령 뒤 마지막 전투 턴의 굴림·리롤·실행3공간을 검사한다.
- 사용하는 대상: CompetitionContent/Rules/CommandRules, CompetitionV5Content/Dice/Skills와 v5 DTO, LoopJson, 기존 CompetitionRulesTests의 비전투 선택 fixture. AI 비공개 정책 함수는 reflection으로 호출하여 같은 현재 눈에서 미래 RNG와 무관한 선택·슬롯순 보관을 확인한다.
- 사용하는 쪽: Unity SharedWorld.EditMode.Tests 및 .NET DiceWarrior.Competition.Tests glob. CompetitionV5StoreTests가 정본과 공개 명령 fixture를 재사용한다.
- 관계 근거: 직접 public API 호출과 실제 인간 명령 이력을 새 초기 상태에 다시 적용하여 전체 참가자/RNG/대상/상태 해시를 비교한다. 중간 JSON encode/decode 뒤에도 같은 이력을 검증한다.
- 상태/수명: 각 검사는 신규 정의·상태를 만든다. 전투 경계 fixture만 격리 메모리에서 명시적으로 구성하며 사용자 파일/DB/프로필을 사용하지 않는다.
- 검수 주의점: RNG 자체 알고리즘을 다시 검증하는 검사가 아니라, 지정된 슬롯 수와 순서로 정확히 추첨했는지를 보호한다. 전체 경기 해시는 당회 독립 재생 대조용이며 기존 골든 기대값을 갱신하지 않는다. 실제 실행 결과는 V5_DICE_COMBAT_09 REPORT에 기록한다.
