# MetaProgressionConfig.cs

- 역할: Editor에서 조절하는 로컬 초기 보유품과 성장 정책 SO.
- 핵심 동작·입출력: CreateProfile은 기본 주사위 6개와 선택적 추가 주사위/장비에 초기 instance ID를 부여하고 현재 와일드 카드/캐릭터를 보유 처리한다. startingCurrency/policy는 시험 경제 값이다.
- 사용하는 대상·사용하는 쪽·관계 근거: MetaProgressionAuthoring이 DefaultMetaProgression.asset을 만들고 GameApplication이 LocalMetaProgressionService에 주입한다.
- 상태·수명·검수 주의: 기존 프로필에 초기 지급을 반복하지 않는다. 런 시작 시 정책을 복제하며 현재 인스턴스에는 정책 변경을 소급하지 않는다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
