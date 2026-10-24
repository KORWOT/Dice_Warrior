# V5DiceAreaView.cs

- 역할: v5 인간 전투의 Roll/Hold 두 영역, HP/적 의도, 기술 예상, 하단 리롤/실행과 정보 진입을 authored 참조로 표시한다.
- 핵심 동작: 원래 slotIndex/instanceId를 카드와 콜백에 유지한다. 보관 여부에 따라 카드 부모를 독립 영역으로 옮긴다. AwaitRoll은 중앙 굴리기만 허용한다. Selection은 CanHold/남은 리롤/미보관 여부로 버튼을 제어하고 실행은 꽝/전부 보관에서도 허용한다. 제한/실패 문구는 notice에 표시한다.
- 입출력: Bind(state,dispatch,showDice,showCombat,message), Snap(state), Play(kind,before,after), Unbind. 출력은 Text/Image/CommonButtonView/V5FaceGraphic의 위치·배율·색·활성 상태이며 IsAnimating/DiceRolling은 검증용 표시 상태다.
- 예상/연출: CompetitionV5Skills.Preview를 순수 조회해 실제 HP 피해/흡수/보호/처치후생략을 처음 2개+외 N개로 표시한다. 모두 꽝은 NO_OP와 적 행동 안내다. 최초 굴림/리롤 .65초는 저장된 결과의 미보관 카드만 회전, 보관/해제 .18초는 위치 이동, 실행은 확정 SkillLog 순서와 반격을 별도로 표시한다. 규칙/RNG/저장은 실행하지 않는다.
- F02 실행 건강 표시: execute는 before로 Snap하여 최종 HP가 먼저 노출되지 않는다. SetHealth가 매 단계 HP 텍스트·HP바·적 보호·아군 현재 보호를 함께 갱신한다. 기술마다 저장 SkillLog의 HP/보호와 누적 생성 보호를 표시하고, 적 행동 종료 시 저장 log.playerHpAfter/적 방어 결과 및 현재 보호0(턴 초기화)를 표시한다. 마지막 after Snap에서 전투 보상/레벨 회복을 포함한 최종 확정 상태로 복귀한다. 선택 대기 중 현재 보호는 항상0으로 명시한다.
- 사용하는 대상: v5 Content/Dice/Skills DTO, 기존 TurnLog/Intent, CommonButtonView, uGUI. 카드 그래픽은 FaceId를 스킬/피해 타입과 별개로 읽는다.
- 사용하는 쪽·관계 근거: CompetitionScreen.Bind, CompetitionPlayback.Play/Snap, CompetitionAuthoring.BuildV5Area, CompetitionV5FlowTests가 직접 호출/참조한다.
- 상태/수명: App가 저장 후 코루틴과 전체 입력 잠금을 소유한다. Snap은 회전/배율/부모/얼굴을 확정 상태로 복구한다. footer는 화면 safe area 하단에 독립 고정하며 본문과 같은 배율을 쓴다. Unbind는 콜백을 제거한다.
- 검수 주의: runtime GameObject 생성 없음. 필수 authored 3카드/3그래픽/영역 참조가 필요하다. 실제 터치/취소/720×1280·1600 캡처 및 텍스트 검증은 메인의 Unity 실행 결과를 따른다.
