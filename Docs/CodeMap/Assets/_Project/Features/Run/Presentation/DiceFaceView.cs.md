# DiceFaceView.cs
- 역할: uGUI MaskableGraphic으로 해상도에 맞는 주사위 면과 1~6 눈금을 직접 그린다. 외부 이미지·새 폰트를 요구하지 않는다.
- 입력/출력: Render(int)는 0~6으로 제한해 Value를 보관하고 mesh를 갱신한다. 0은 굴림 전 대기 상태의 흐린 점이다. faceColor/edgeColor/pipColor는 Inspector에서 편집 가능하다.
- 동작: OnPopulateMesh는 둥근 테두리/면, 원형 눈금을 VertexHelper로 만든다. Rect 크기에 맞춰 정사각형을 유지한다. 난수를 생성하거나 실제 주사위 규칙을 계산하지 않는다.
- 관계: DiceRollUI가 여섯 면을 렌더하고, DiceRollAuthoring이 원본과 CanvasRenderer를 작성한다. RequireComponent(CanvasRenderer)로 신규 컴포넌트의 그리기 의존성을 선언한다.
- 상태/수명: GameObject/Canvas 수명을 따르는 표시 값만 보유한다. MaskableGraphic의 clip/dirty mesh 동작을 사용한다.
- 검수: CanvasRenderer 누락 시 논리 값만 존재하고 화면이 안 보이므로 실제 component와 native 화면을 함께 검사한다. 검증은 Docs/Reports/LOBBY_MAP_DICE_REPORT.md.
