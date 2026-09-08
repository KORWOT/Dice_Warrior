# DiceRollUI.cs
- 역할: BaseUI<DiceRollUIData>를 상속하는 탐험/전투 공통 6주사위 modal. 배경 전체 화면과 별도 popup 수명을 가진다.
- 입력/출력: serialized title/detail/result, DiceFaceView[6], CommonButtonView rollButton. DTO의 6개 확정 결과 또는 굴림 전 null을 표시하며 잘못된 길이/누락 참조는 예외로 거절한다.
- 동작: OnBind는 기존 listener를 제거하고 명시적 roll 선택을 연결한다. rolling 중 버튼을 숨기고 unscaledTime 기반 눈금 순환·회전·상하 이동을 표시한다. duration 종료 시 실제 values로 정착하고 IsRolling=false.
- 관계: RunUIController.ShowDiceWindow와 UIWorkbenchPreview.OpenDice가 UIManager를 통해 바인딩한다. DiceRollAuthoring이 원본을 만든다. DiceFaceView.Render와 CommonButtonView.Bind/Unbind를 사용한다.
- 상태/수명: 열려 있는 동안 DTO와 최초 로컬 위치를 보관한다. OnUnbind는 위치/회전/눈금 복원 및 callback/text 정리. 화면 표시 자체는 RunSession·저장·난수를 사용하지 않는다.
- 검수: 6개의 CanvasRenderer/mesh, 굴림 중 잠금, 정확한 확정 값, 닫기·재개로 중복 굴림 없음, 두 세로비의 panel 경계. 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md.



## 조합·카드·전투 피드백 (2026-09-08)

- resultHoldSeconds 기본 .9초 및 feedbackVersion을 추가했다. Controller가 결과 읽기 시간을 기다리며, View는 정확한 여섯 면 정착 후 조합 결과 텍스트를 .3초 동안 작게 확대했다 복원한다. 텍스트의 원래 scale을 바인딩에서 보관하고 Unbind에서 복구한다. 추가 난수·굴림·체크포인트는 없다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
