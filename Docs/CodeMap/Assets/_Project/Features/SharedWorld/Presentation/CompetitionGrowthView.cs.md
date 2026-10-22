# CompetitionGrowthView.cs

- 역할: 새 모험가 선택 카드와 시작/경기 중 능력치·장비·주사위·스킬 상세 문구를 만드는 읽기 전용 표시 도우미다.
- 핵심 동작: Roster는 기본 제공 3종과 선택 ID를 표시한다. Details는 새 경기 Preview 또는 동결된 경기 Describe로 기본/계정 HP/런 레벨/최종 RGB를 나누며 구형은 위력·수호를 유지한다. 장비는 태그별 피해/보호 보정으로 별도 표시한다.
- 입출력: CompetitionScreenData, definition, profile, participant/selected ID, skills 탭 여부 → data의 growthMode/title/summary/body/배열. 저장·명령·난수를 변경하지 않는다.
- 사용하는 대상: CompetitionStats의 Preview/Describe/ScalingText, SymbolCombat.Plan, 콘텐츠 주사위면/장비. 가능한 기술은 소유 주사위의 서로 다른 면 조합을 열거해 해당 버전의 조합 규칙으로 구한다.
- 사용하는 쪽·관계 근거: CompetitionApp.Render의 adventurers/stats/skills 분기 → CompetitionScreen.OnBind의 authored 패널. CompetitionFlowTests에서 실제 포인터와 표시값/크기를 확인한다.
- 상태/수명: 정적/무상태. 화면 재표시마다 현재 스냅샷에서 계산하며 경기 RNG를 소비하지 않는다.
- 검수 주의: 구형 경기에는 RGB를 소급 표시하지 않는다. 신격/권능/해금을 구현된 기능으로 표현하지 않는다. 모든 장비 슬롯과 가능한 조합이 상세 패널 안에 들어가야 한다.
- 장비 문구: 기존 콘텐츠의 `이름 · 효과` 형식에서 앞의 이름을 쓰고 실제 tag/damage/block으로 효과를 한 번 표시한다. 기존 이름에 포함된 효과가 중복되지 않게 한다.
