# V5FaceGraphic.cs

- 역할: 공용/전용/꽝 면의 식별 가능한 벡터 프레임과 아이콘을 uGUI로 그린다.
- 핵심 동작: 공용은 모서리를 접은 사각 프레임과 검/지팡이/불/방패, 전용은 육각 프레임과 교차검/룬 원/성벽 방패, 꽝은 회색 원과 X다. 현재 faceIndex 강조는 흰색 굵은 프레임으로 그린다. 색뿐 아니라 프레임·그림·상위 카드 문구로 구분한다.
- 입출력: Configure(V5FaceDefinition,highlighted), kind/iconId/current → MaskableGraphic.OnPopulateMesh 정점. UI12의 static Draw(VertexHelper,Func<Vector2,Vector2>,V5FaceDefinition,bool highlighted=false,bool onLightSurface=false)는 같은 면을 큐브 표면에 투영한다. onLightSurface만 잉크 색을 밝은 큐브용으로 바꾸고 그림/면 의미는 그대로다. FaceId가 SkillId/DamageType을 대신하지 않는다.
- 사용하는 대상: Unity VertexHelper, 기존 LoopGlyphGraphic.Draw의 공용 그림. 내부 선/원 프레임은 이 컴포넌트가 구성한다.
- 사용하는 쪽·관계 근거: V5DiceNetView의6면이 Configure하며 V5DieGraphic은 각 실제 물리면을 Draw로 그린다. V5DiceAreaView의 기존3개2D face 참조는 호환용으로 갱신·숨김 처리한다. CompetitionAuthoring.FaceGraphic이 authored 비레이캐스트 자식을 만든다.
- 상태/수명: 뷰 로컬 표시 상태만 보유한다. 변경 시 SetVerticesDirty하며 규칙/저장/난수/정적 가변 상태가 없다.
- 수정2회차 F04: CanvasRenderer를 RequireComponent로 명시한다. 기존 prefab의 누락 구성은 UpgradeV5가 소유된9개 face에만 Editor API로 추가한다. Graphic.canvasRenderer의 lazy AddComponent에 의존하지 않는다.
- 검수 주의: 파일은 UI 소스이고 신규 미디어/폰트 자산을 생성하지 않는다. 아이콘과 텍스트의 실제 크기·가독성은 Unity 캡처로 확인해야 한다.
