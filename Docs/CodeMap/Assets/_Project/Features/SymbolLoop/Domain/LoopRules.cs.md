# LoopRules.cs

- 역할: 런 생성·순환 보드·착지·선택·완주·성장·보스·상태 검증.
- 핵심 동작: 시드/지역 전용 보드 생성, 저장된 이동/전투 RNG 분리. 12 이상 이동은 초과 소멸 및 LapReward. 지역1/2 슬롯 증가, 지역3 장비 보상 뒤 보스.
- 입출력: Rules/Profile/seed → Run; Travel/ClaimLap/Choose/RollCombat은 세션 후보만 변경. Validate는 부적합 후보/저장을 거절.
- 사용하는 대상: LoopData, SymbolCombat. 사용하는 쪽: LoopSession, LoopStore, SymbolLoopConfig, Authoring, 테스트.
- 관계 근거: 위 소스의 직접 호출. v1 카드/족보/분기 지도 참조 없음.
- 상태/수명: 보드/규칙/주사위/장비/완주 ID는 런 소유, 영구 재화는 세션 정산 책임.
- 검수: phase 제한, 3지역 종료, 주사위 개수/6면, 등록 조합 유일성, 비용 검증, 값 범위 및 중복 완주.
