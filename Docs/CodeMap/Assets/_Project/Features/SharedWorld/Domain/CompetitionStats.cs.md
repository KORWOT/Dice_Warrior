# CompetitionStats.cs

- 역할: v3 모험가 기본/계정 HP/런 레벨과 기술 계수를 계산하고 UI용 읽기 사본을 제공한다.
- 핵심 동작: IsEnabled는 StatsVersion만 활성화한다. Preview는 선택한 모험가의 레벨1과 profile.growth×5, Describe는 동결 정의와 participant.growth.accountHp/level로 HP·STR/GRD/ARC를 계산한다. 기본/계정/레벨/합계 및 이름·패시브 설명을 분리한다.
- 입출력: Preview(definition,profile,id), Describe(definition,participant) → CompetitionStatBreakdown 사본; 구형은 null. ScalingText(definition,skillId)는 실제 기본값·계수·내림/수호를 문자열로 표시하고 구형은 빈 문자열이다.
- 사용하는 대상: CompetitionData/Content, SymbolLoop Profile/Skill, LINQ.
- 사용하는 쪽: CompetitionRules 시작/검증, CompetitionCombat의 내부 DamageBonus/BlockBonus, CompetitionGrowthView 표시, CompetitionApp의 버전 분기, CompetitionGrowthTests.
- 관계 근거: 직접 API 호출. SharedWorld 저장 모델과 기존 Profile을 읽으며 Profile/권능/서버 상태를 쓰지 않는다.
- 상태/수명: 계산할 때마다 새 사본을 반환한다. 장비는 RGB 합계에 넣지 않고 공통 전투 엔진의 태그 기술 보너스로 남긴다. 계수의 STR/ARC 항을 합산하고 분모로 한 번 나누며 비음수 값이므로 정수 나눗셈이 내림이다.
- 검수: 기본HP+accountHp+levelHp의 합계, levelBonus=level−1, 패시브 태그와 보호막 보너스를 구분한다. 실제 효과0의 가산 차단·장비·흡수·반격은 SymbolCombat 책임이다. 데이터 정의에 없는 선택/기술과 누락된 참가자 growth를 거절한다.
