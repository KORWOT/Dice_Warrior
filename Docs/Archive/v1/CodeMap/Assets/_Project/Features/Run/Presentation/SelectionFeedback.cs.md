# SelectionFeedback.cs
- 역할: 선택한 카드 한 장의 scale/color를 FEEL로 강조한다.
- 입력/출력: 작성 player/surface, Play(seconds) IEnumerator, ResetFeedback.
- 관계 근거: FateDiceWidgets.AnimateCardSelection이 선택한 실제 버튼에서 조회하고 재생한다. DicePresentationAuthoring이 ActionCardView/FateCardView 원본에 연결한다.
- 동작/상태: 확정 명령 이후 기존 선택 지연만 사용. 시작 시 원래 pose/color를 캡처하고 gradient를 설정한다. finally/OnDisable/Clear에서 정지·복원하며 다른 카드나 화면/세션을 탐색하지 않는다.
- 검수: 해당 카드만 강조, 공격 전 scale 복원, 취소 정리, 추가 저장 없음. 실행 결과는 DICE_PRESENTATION_EFFECTS_REPORT.


## 등급별 연출 수명 (2026-09-10)

Play()는 작성 FEEL 트랙을 재생하고 실제 player.IsPlaying 종료를 기다린다. playbackSpeed는 Inspector 배율(0생략, 비유한/음수 예외)이며 DurationMultiplier=1/speed다. 추측 seconds 대기를 제거했다. finally/Reset/OnDisable이 기존 scale/color를 복원한다. 사용하는 쪽은 FateDiceWidgets.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
