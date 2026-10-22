# CompetitionPlayback.cs

- 역할: 공동 세계의 인간 이동과 AI 위치 결과를 재생하고 기존 기호/스킬 전투 연출을 재사용한다.
- 핵심 동작: 숫자 결과 면 회전→인간의 칸별 이동→확정 AI 위치를 짧게 표시한다. 전투는 ParticipantState를 표시 전용 Run으로 변환해 SymbolLoopPlayback.Play에 전달한다. 기존 Run은 새 경기 저장/판정에 사용되지 않는다.
- 입출력: Play(kind,before,after), Snap(state) → authored 네 말/주사위/전투 무대/문구. MovementRolling/DiceRolling/VisiblePosition은 검증용 표시 상태다.
- 사용하는 대상: CompetitionScreen의 좌표, SymbolLoopPlayback의 확정 TurnLog/SkillLog 재생, LoopDieGraphic.Pose, Unity unscaled time.
- 사용하는 쪽·관계 근거: App가 코루틴과 입력 잠금을 소유하고 Screen.OnBind가 Snap 호출, Authoring이 로컬 참조를 연결한다.
- 상태/수명: 실제 규칙/난수/저장과 독립적이다. 중단 후 Snap은 확정 상태로 복귀하며 AI 차례를 다시 실행하지 않는다. AI 전투 전체 애니메이션을 재생하지 않는다.
- 검수 주의: 표시 어댑터는 HP/장비/주사위/적/마지막 턴만 복사한다. 새 이동은2×2 좌표를 사용하며 출발점 도착 시 초과 칸을 버리는 확정 규칙을 반영한다. 기존 전투 연출은 applied 스킬 및 실제 반격 로그만 재생한다.
