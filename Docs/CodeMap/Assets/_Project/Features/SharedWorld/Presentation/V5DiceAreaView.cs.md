# V5DiceAreaView.cs

- 역할: v5 인간 전투의 Roll/Hold 두 영역, HP/적 의도, 기술 예상, 하단 리롤/실행과 정보 진입을 authored 참조로 표시한다.
- 핵심 동작: 원래 slotIndex/instanceId를 카드와 콜백에 유지한다. 보관 여부에 따라 카드 부모를 독립 영역으로 옮긴다. AwaitRoll은 전투행동 중 중앙 굴리기만 허용하며 정보 조회는 유지한다. Selection은 CanHold/남은 리롤/미보관 여부로 버튼을 제어하고 실행은 꽝/전부 보관에서도 허용한다. 제한/실패 문구는 authored feedbackScroll 안의 notice에 표시한다.
- 입출력: Bind(state,dispatch,showDice,showCombat,message), Snap(state), Play(kind,before,after), Unbind. 출력은 Text/Image/CommonButtonView/V5FaceGraphic의 위치·배율·색·활성 상태이며 IsAnimating/DiceRolling은 검증용 표시 상태다.
- 예상/연출: CompetitionV5Skills.PlanSteps/Preview의 동일 계획으로 최대3개 기술 전체·사용 slot·조합 크기를 연결한다. 카드는 지속 번호/주사위 종류/결과/보관/기술번호를 표시한다. 모두 꽝은 발동 기술 없음과 적 행동 안내다. 최초 굴림/리롤 .65초는 미보관 카드만 ±6도/최대1.03배로 움직인다. 보관/해제 .18초는 목적 영역으로 먼저 재배치한 뒤 fade/작은 확대를 적용하여 안내문을 가로지르지 않는다. 실행은 확정 SkillLog 순서와 사용 주사위를 연결하고 반격을 별도로 표시한다. 규칙/RNG/저장은 실행하지 않는다.
- F02 실행 건강 표시: execute는 before로 Snap하여 최종 HP가 먼저 노출되지 않는다. SetHealth가 매 단계 HP 텍스트·HP바·적 보호·아군 현재 보호를 함께 갱신한다. 기술마다 저장 SkillLog의 HP/보호와 누적 생성 보호를 표시하고, 적 행동 종료 시 저장 log.playerHpAfter/적 방어 결과 및 현재 보호0(턴 초기화)를 표시한다. 마지막 after Snap에서 전투 보상/레벨 회복을 포함한 최종 확정 상태로 복귀한다. 선택 대기 중 현재 보호는 항상0으로 명시한다.
- 사용하는 대상: v5 Content/Dice/Skills DTO, 기존 TurnLog/Intent, CommonButtonView, uGUI. 카드 그래픽은 FaceId를 스킬/피해 타입과 별개로 읽는다.
- 사용하는 쪽·관계 근거: CompetitionScreen.Bind, CompetitionPlayback.Play/Snap, CompetitionAuthoring.BuildV5Area, CompetitionCombatUiAuthoring.Upgrade, CompetitionV5FlowTests/CompetitionCombatUiTests가 직접 호출/참조한다. DieLabel/Marker는 Net이 같은 주사위 식별을 표시할 때 재사용한다.
- 상태/수명: App가 저장 후 코루틴과 전체 입력 잠금을 소유한다. Snap은 회전/배율/부모/얼굴을 확정 상태로 복구한다. footer는 화면 safe area 하단에 독립 고정하며 본문과 같은 배율을 쓴다. Unbind는 콜백을 제거한다.
- 검수 주의: runtime GameObject 생성 없음. 필수 authored 3카드/3그래픽/영역과 feedbackScroll이 필요하다. 중심pivot180×148/slot별 고정X가 회전 범위의 전제다. 20번째 행동·보호 초기화·살아 있는 적만 반격·리롤/보관제한은 실제 규칙을 안내한다. 실제 터치/취소/여러비율과 안전영역·연출 프레임/텍스트 검증은 메인의 Unity 실행 결과를 따른다. 설계단위를 실제기기dp로 주장하지 않는다.
