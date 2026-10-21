# SymbolCombat.cs

- 역할: 전체 기호 멀티셋 판정과 단일 적 턴 처리.
- 핵심 동작: 전체 signature가 등록 조합과 같으면 한 기술, 아니면 눈 인덱스마다 기본기. 기술 태그에 맞는 장비만 피해/보호 보정.
- 입출력: Plan(Rules, Symbol[]) → Skill[]; Resolve(Run 후보, Symbol[]) → 확정 TurnLog와 후보 변경.
- 사용하는 대상: LoopData, LINQ. 사용하는 쪽: LoopRules.RollCombat, SymbolLoopTests.
- 관계 근거: 직접 호출. 저장·UI·RNG를 소유하지 않는다.
- 상태/수명: 적 보호막은 기술 묶음 전체에 차감 후 만료. 플레이어 보호막은 로컬 턴 변수. 적 사망 뒤 계획은 enemy-defeated로 기록.
- 검수: 부분 조합 금지, 반복 기본기 누락 금지, 반격 최대 한 번, 현재 HP/보호막과 로그 전후 값.
