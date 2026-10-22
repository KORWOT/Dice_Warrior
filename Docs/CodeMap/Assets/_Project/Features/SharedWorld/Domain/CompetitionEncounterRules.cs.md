# CompetitionEncounterRules.cs

- 역할: 개인 단계별 착지 제안·조건·전투·사건 보상을 처리한다.
- 핵심 동작: 고정 장소의 단계별 후보에서 가중 중복 없는 최대3개를 뽑고 합법 선택을 최소1개 보장한다. 비용/보상/적 능력을 제안에 동결한다. 선택 후 인간/AI가 같은 비용과 전투를 적용한다.
- 입출력: CreateOffer(state,participant) → 고정 EncounterOffer 및 방문/사건 RNG 증가. LegalChoices → 현재 합법 후보. 내부 Choose/Combat은 사건 완료 여부를 반환한다.
- 사용하는 대상: CompetitionData/Rules/Random/Combat, 기존 Enemy/Intent/DieSlot.
- 사용하는 쪽: CompetitionCommandRules, CompetitionAi 및 도메인 검사.
- 관계 근거: 명령 실행과 AI 정책의 직접 함수 호출.
- 상태/수명: 사건 제안은 착지할 때만 생성하고 화면 재표시/복원은 읽기만 한다. 보상은 선택 ID가 확정된 사건 또는 최종 시련 ID로 한 번 지급한다.
- 검수: 제안에 없는 선택·조건 미충족은 비용 차감 전 거절한다. HP/골드 비용, 장비/주사위 정의, 개인 완주 난도, 64턴 공통 실패, 처치 보상과 명예 중복을 확인한다. 최종 시련은 세 번째 완주 보상 후 별도 전투로 시작한다.
- 버전 연결: Combat은 Rules만 넘기는 구형 어댑터 대신 state.definition을 CompetitionCombat.Resolve에 전달한다. 인간·AI가 같은 경로로 동결된v1 정확전체 또는v2 부분조합을 사용한다. RNG 소비와 피해/보상 순서는 변경하지 않는다.
