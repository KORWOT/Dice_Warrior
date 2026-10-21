# LoopRules.cs

- 역할: 런 생성·순환 보드·착지·선택·완주·성장·보스·상태 검증.
- 핵심 동작: 시드/지역 전용 보드 생성, 저장된 이동/전투 RNG 분리. 12 이상 이동은 초과 소멸 및 LapReward. 지역1/2 슬롯 증가, 지역3 장비 보상 뒤 보스.
- 입출력: Rules/Profile/seed → Run; Travel/ClaimLap/Choose/RollCombat은 전달받은 후보만 변경. Validate는 부적합 후보/저장을 거절.
- 사용하는 대상: LoopData, SymbolCombat, BackendProtocol. 사용하는 쪽: RunCommandRules, LoopSession, MockRunVerifier, MockAuthorityStore, LoopStore, SymbolLoopConfig, Authoring, 테스트.
- 관계 근거: 위 소스의 직접 호출. v1 카드/족보/분기 지도 참조 없음.
- 상태/수명: 보드/규칙/주사위/장비/완주 ID는 런 소유, 영구 재화는 IGameBackend의 권한이며 규칙 엔진은 지급하지 않는다.
- 검수: phase 제한, 3지역 종료, 주사위 개수/6면, 등록 조합 유일성, 비용 검증, 값 범위 및 중복 완주.

- 저장 검증: schema2, 연속 명령 순번, 승인 ticket/run ID, 캐시 구조를 검사한다. ValidSubmission은 구형 PendingVerification의 0지급과 Approved의 검증 버전/결과 범위를 구분한다. 정산된 Run은 동일 ID의 영수증 지급액과 일치해야 한다. 이 로컬 구조 검사가 권한 지급 근거는 아니다.
- RNG: xorshift32는 0을 0x9e3779b9로 바꾼 뒤 XOR/좌13·우17·좌5를 순서대로 실행한다. Board의 지역 상수 곱셈은 명시적 unchecked uint이며 기존 결과는 그대로다. 보드 셔플은 i=11..2, j=1+Next%i, 시작칸 제외. 이동과 전투 RNG는 별개이며 Board RNG는 버린다.
