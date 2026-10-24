# CompetitionV5Dice.cs

- 역할: v5 roll/hold/release/reroll/execute reducer와 명령 공간 예약·공정한 AI 선택.
- 핵심 동작: 최초 굴림은 모든 슬롯, 리롤은 보관하지 않은 슬롯만 슬롯 순서로 combatRng를 소비한다. 보관 변경은 RNG를 소비하지 않으며 턴당12회·필수 남은 진행 공간을 검사한다. 실행 때만 Skills와 기존 사건 종료 처리를 호출한다.
- 입출력: RemainingRequiredCommands(state), HoldUnavailableReason(state,p), CanHold(state,p). 내부 Begin/Apply/AiCommand는 CommandRules/EncounterRules/Ai에서 호출한다. hold/release arg는 현재 dieInstanceId, 나머지 명령은 빈 문자열이다.
- 사용하는 대상: V5Content의 실제6면/참가자 검증, V5Skills.Execute, EncounterRules.FinishCombat, CompetitionRandom, BackendProtocol.MaxCommands.
- 사용하는 쪽: CompetitionCommandRules의 5명령 분기, EncounterRules.BeginCombat, CompetitionAi.PlayTurn, Session의 저장 명령 수 예약, V5 UI의 비활성 사유.
- 관계 근거: 해당 클래스들의 직접 메서드 호출. 인간과 AI 모두 ApplyParticipant를 거쳐 같은 reducer를 사용한다.
- 상태/수명: 각 전투 턴 AwaitRoll→Selection→다음 AwaitRoll/종료. 실행 전에는 HP·보상·반격·라운드가 진행되지 않는다. 다음 턴은 리롤1·보관0으로 초기화한다.
- 검수 주의: 예산은 현재 단계와 남은 라운드마다 이동/선택+최대20×(굴림/리롤/실행)을 보수적으로 예약한다. 인간은 actionNumber 기준2048, AI는 로그 예산을 소비하지 않으며 동일12회 제한을 쓴다. AI는 현재 보이는 꽝만 다시 굴리고 슬롯 순 비꽝을 보관하며 미래 RNG를 읽지 않는다. 실행/모두보관 리롤/중복 보관의 무효 입력은 변이 전에 거절한다.
