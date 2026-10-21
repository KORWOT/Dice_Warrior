# LoopGlyphGraphic.cs

- 역할: 숫자 해상도와 무관한 기호·말·수호자·칸 아이콘의 Canvas 벡터 그림.
- 핵심 동작: LoopGlyph별 폴리곤/선/원과 색을 생성. LoopMesh는 전달받은 2D 변환으로 같은 그림을 주사위 면에도 투영한다.
- 입출력: glyph/color/Rect → VertexHelper 메시. 게임 상태와 난수 없음.
- 사용하는 대상: uGUI MaskableGraphic/VertexHelper, Unity 벡터.
- 사용하는 쪽/관계 근거: Authoring이 타일/말/전투 아이콘을 직렬화하고 Screen/Playback이 갱신. LoopDieGraphic이 Draw/Tint/LoopMesh를 직접 호출.
- 상태/수명: authored Graphic 수명. 변경 시 SetVerticesDirty. 텍스처·전역 검색·런타임 오브젝트 생성 없음.
- 검수: 아이콘은 Symbol enum의 첫 4개 값과 일치. Canvas bounds와 투영 면 회전 확인.

- 직접 표시 회귀: CanvasRenderer를 RequireComponent로 작성 시 보장한다. lazy 생성에 의존하지 않는다.
