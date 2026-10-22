# SymbolLoopPlayback.cs

- SharedWorld 직접 관계: CompetitionPlayback이 표시 전용 Run과 확정 TurnLog로 전투 연출을 재사용한다. CompetitionScreen 프리팹의 직렬화된 human 참조를 사용한다. 구형 모험 화면은 제거했고 네 말/숫자 이동은 CompetitionPlayback 책임이다.

- 역할: 확정 전후 Run 사본만 받아 이동과 전투의 시각적 순서를 재생한다.
- 핵심 동작: Travel은 숫자 굴림→칸별 포물선 이동→착지. 초과 칸은 출발점에서 버린다. Combat은 기호 굴림→단일 조합/남은 기본기+조합/기본기 안내→applied 스킬→실제 HP/보호 변화→기록된 적 행동. 각 SkillLog.skillId가 rules.combos에 있는지 따로 판단해 SKILL/COMBO 제목과 효과를 정한다. 혼합 발동의 남은 기본기를 조합으로 오표시하지 않으며 생략 스킬/없는 반격은 재생하지 않는다.
- 입출력: 명령 종류, before/after Run → pawn/dice/stage/effect/text/HP bar. Snap은 확정 상태로 모든 임시 자세를 복원한다. 표시 값 getter는 검증에도 사용한다.
- 사용하는 대상: LoopDieGraphic/LoopGlyphGraphic, TurnLog/SkillLog, authored RectTransform/Text/Image.
- 사용하는 쪽/관계 근거: CompetitionApp이 CompetitionPlayback.Play iterator를 소유·실행하고 해당 어댑터가 본 Play를 호출한다. CompetitionScreen.OnBind가 어댑터 Snap을 호출하며 CompetitionAuthoring이 로컬 참조를 작성하고 CompetitionFlowTests가 실제 프레임과 상태를 확인한다.
- 상태/수명: 코루틴 소유자는 App. unscaled time을 사용하며 난수·세션 명령·저장을 호출하지 않는다. Snap은 재생 재개가 아닌 최종 상태 복귀다.
- 검수: 저장 실패 시 진입 금지, 중단 시 잠금 해제는 App 책임. 스킬별 HP는 로그이고 레벨업/보상 최종 HP는 다음 Screen bind에 반영. 피해0 보호 전용 기술은 전사에게 표시한다.

- 안내 문구는 현재 Phase에 맞춘다. Result에는 주사위 트레이를 숨겨 추가 이동/굴림을 안내하지 않는다.
