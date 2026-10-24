# CompetitionV5UiProjectionTests.cs

- 역할: v5 UI의 물리 주사위 그룹, 완주/사건 보상 투영과 사용자 설명이 실제 도메인 결과와 일치하는지 확인하는 순수 EditMode 회귀 검사.
- 핵심 동작: 중복 기호의 2눈+남은 단독 배정과 꽝 뒤 물리 인덱스를 검사한다. 3모험가 × 6³ 결과 전체에서 비꽝 슬롯이 정확히 한 번 사용되고 recipe/preview/실제 실행 순서와 연결되는지 확인한다. 첫/둘째 완주 및 최종 5장비를 실제 지급과 비교하며 레벨업·회복 상한·원본/사본 RNG 불변을 검사한다. 실제 6면, 등록명, HP 비용/최소 경계, 정찰 보상, 장비 교체 비교, 공동 순위/탈락 이유를 확인한다. 5보물의 표시 합계를 실제 선택 지급과 대조한다.
- 입출력: NUnit 23개 검사 사례. CompetitionV5Tests.Definition/Combat/Selected의 격리 fixture와 신규 Lap/Encounter fixture. Unity 화면/외부 HTTP/계정 저장은 실행하지 않는다. 수정 회차1은 시작 무기 2종을 p=null로 조회해 후보 효과/다음 모험 문구를 확인하고 현재 장착·교체를 임의로 표시하지 않는지 검사한다.
- 사용하는 대상: CompetitionV5Skills.PlanSteps/Preview, CommandRules.PreviewLapReward/Apply, EncounterRules.RewardGold, V5UiText, V5Content/Attributes/Rules, LoopJson.
- 사용하는 쪽: SharedWorld.EditMode.Tests asmdef와 DiceWarrior.Competition.Tests glob. 문구 helper는 후자의 Compile Link로 포함한다.
- 관계 근거: 예상 숫자만 고정한 스냅샷 대신 실제 Apply 후 참가자의 보상·RNG와 비교하고, 계획의 물리 face 다중집합을 레시피에 대응시킨다. 전체 원본 JSON 비교는 조회의 무변이를 보호한다.
- 상태/수명: 각 검사는 메모리상의 새 정의/상태를 만들며 실제 사용자 파일이나 DB를 열지 않는다. 결과 근거는 V5_UI_IMPROVEMENT_11 REPORT 및 해당 artifacts TRX에 기록한다.
- 검수 주의점: 최초 RED는 미구현 슬롯/보상 API 12건 및 문구 API 4건을 검출했고 최초 .NET 묶음은 기존 184+신규21=205/205, skip0이었다. UI11-R01의 null 참가자 조회 2건도 수정 전 실제 NullReferenceException을 검출했다. 수정 회차1의 최종 결과는 domain-correction-1.trx와 REPORT를 따른다. 실제 Unity 컴파일/화면은 별도 검증이며 .NET PASS로 대신하지 않는다.
