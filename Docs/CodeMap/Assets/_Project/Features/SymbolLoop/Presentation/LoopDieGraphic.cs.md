# LoopDieGraphic.cs

- 역할: uGUI에 6면 입체 주사위를 투영하고 실제 회전시키는 표시 컴포넌트.
- 핵심 동작: Configure가 숫자 1..6 또는 설정의 여섯 기호 면을 바인딩하고 결과 면을 정면으로 돌리는 정지 자세 계산. Pose는 회전 감속, OnPopulateMesh는 보이는 면/명암/테두리/눈을 투영한다.
- 입출력: 저장된 숫자/기호 결과, 원본 면 배열, 0..1 진행도 → Canvas 메시. Result/RollProgress는 현재 표시 값.
- 사용하는 대상: LoopGlyphGraphic/LoopMesh, Quaternion/uGUI. 사용하는 쪽: SymbolLoopPlayback, Authoring, FlowTests.
- 관계 근거: Playback의 Configure/Pose 직접 호출과 authored numericDie/symbolDice 참조.
- 상태/수명: 게임 RNG를 소비하지 않고 규칙을 실행하지 않는다. Configure는 결과가 6면에 없으면 거절한다.
- 검수: 기호 면의 방향과 최종 결과, 1~3개 배치, 감속 중·정지 후 캡처. 물리 판정은 구현 범위가 아니다.

- 직접 표시 회귀: CanvasRenderer를 RequireComponent로 작성 시 보장한다. lazy 생성에 의존하지 않는다.
