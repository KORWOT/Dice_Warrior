# CompetitionRandom.cs

- 역할: 참가자별 이동/전투/사건/AI 선택 난수를 결정적으로 분리한다.
- 핵심 동작: seed XOR FNV offset에서 participantId/stream의 UTF-16 코드 단위를 낮은 바이트→높은 바이트 순서로 FNV-1a 처리하며 uint 오버플로를 명시한다. 0은 고정 상수로 치환한다. Next는 기존 xorshift32를 재사용한다.
- 입출력: Split(uint seed,string participantId,string stream) → uint; Next(ref uint state,int count) → [0,count).
- 사용하는 대상: LoopRules.Next.
- 사용하는 쪽: CompetitionRules.Start, CompetitionCommandRules 이동, CompetitionEncounterRules 사건/전투, CompetitionAi 동점 선택.
- 관계 근거: 각 호출자의 직접 ref RNG 사용.
- 상태/수명: 전역 RNG 없음. 상태는 ParticipantState의 네 uint에만 있다.
- 검수: 이동은 D6 1회, 착지는 사건 스트림1회 후 장소/단계/방문 기반 지역 스트림, 전투 시작 의도1회·턴마다 주사위수만큼·계속 싸울 때 다음의도1회, AI는 동점후보가 복수일 때1회 소비한다. 표시명·시간·GetHashCode는 시드에 사용하지 않는다.
