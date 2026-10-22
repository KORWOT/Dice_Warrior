# CompetitionEncounterRules.cs

- 역할: 개인 단계별 착지 제안·조건·전투·사건 보상을 처리한다.
- 핵심 동작: 고정 장소의 단계별 후보에서 가중 중복 없는 최대3개를 뽑고 합법 선택을 최소1개 보장한다. 비용/보상/적 능력을 제안에 동결하며 인간/AI가 같은 비용과 전투를 적용한다.
- 입출력: CreateOffer(state,participant) → EncounterOffer와 방문/사건 RNG 증가. LegalChoices는 합법 후보, 내부 Choose/Combat은 사건 완료 여부를 반환한다.
- 사용하는 대상: CompetitionData/Rules/Random/Combat, 기존 Enemy/Intent/DieSlot.
- 사용하는 쪽: CompetitionCommandRules/CompetitionAi 및 도메인 검사.
- 관계 근거: 명령/AI의 직접 호출. Combat은 state.definition을 어댑터에 전달하고 사건·최종 시련 보상도 같은 definition으로 AddXp를 호출한다.
- 상태/수명: 제안은 착지할 때 생성하며 화면 재표시/복원은 재추첨하지 않는다. 확정된 사건/최종 시련 ID로 보상을 한 번 지급한다. RNG 소비와 보상 순서는 유지한다.
- 검수: 제안에 없는 선택·조건 미충족을 비용 차감 전 거절한다. HP/골드 비용, 개인 완주 난도, 공통64턴 실패, 처치 보상·명예 중복을 검사한다. v1 정확전체/v2 부분조합/v3 RGB 계수는 동결 버전으로 실행한다. 런 XP가 영구 계정/권능을 변경하지 않는다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

v4 BeginCombat 뒤 선택사건ID를 전용 enemyId로 동결하고 typed 로그를 초기화한다. 장비교체는6스탯/파생HP를 갱신한다. 명시된5보물에 현재LUK/5 정수 보너스를 XP보다 먼저 한 번 지급한다. 사건 후보/고정 비용/난수 추첨은 바꾸지 않는다.
