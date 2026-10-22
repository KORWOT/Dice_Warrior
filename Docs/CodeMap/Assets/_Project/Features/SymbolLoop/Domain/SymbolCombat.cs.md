# SymbolCombat.cs

- 역할: 전체/부분 기호 멀티셋을 판정하고 단일 적 전투 턴을 처리하는 공통 엔진.
- 핵심 동작: 등록된 전체 조합 우선. allowPartial=true인3눈에 전체 조합이 없으면 콘텐츠 순서상 첫 합법2눈 조합을 고르고 남은 기본기를 원래 순서로 먼저 실행한다. 각 눈은1회만 소비한다.
- 입출력: 기존 Plan(Rules,faces)/Resolve(Run,faces) 및 bool allowPartial 오버로드는 서명·기존 동작을 유지한다. 새 Resolve(Run,faces,bool,Func<Skill,int> damageBonus,Func<Skill,int> blockBonus)는 기술별 능력/패시브 가산을 받는다. 기존 오버로드는 run.power/guard를 전달한다.
- 사용하는 대상: LoopData, LINQ, System.Func. 사용하는 쪽: LoopRules.RollCombat, CompetitionCombat, SymbolLoop/SharedWorld 검사.
- 관계 근거: 직접 호출. SharedWorld는 개인 전투 필드만 임시 Run으로 전달하며 이 엔진을 이동/보상/정산에 사용하지 않는다.
- 상태/수명: 장비는 기술 태그로 가산한다. 적 보호막은 기술 묶음에 걸쳐 소모 후 만료, 플레이어 보호막은 턴 지역 변수다. 적 사망 뒤 계획은 enemy-defeated로 기록하고 반격은 최대1회다.
- 검수: 기본 피해/보호가 양수일 때만 해당 가산 함수를 호출하여 효과0에 새 효과가 생기지 않는다. v3의 계수·패시브는 SharedWorld가 제공하고 공통 엔진은 흡수/HP 클램프/생략/반격을 그대로 소유한다. 미리 컴파일된 Authority DLL의 기존2인자 메서드를 삭제·대체하지 않는다. v1/v2 기존 공식과 골든을 보호한다.
