# CombatStageGraphic.cs
- 역할: 새 이미지/아이콘 없이 중앙 전투 연출 공간을 표시하는 장식용 uGUI Graphic.
- 입출력/동작: RectTransform/Graphic.color로 OnPopulateMesh에서 배경 gradient, 바닥/그림자와 익명 적·플레이어 실루엣을 그린다. 외부 Sprite/Texture/게임 데이터/RNG가 없다.
- 상태/수명: public showFigures=false이면 바닥/음영만 남긴다. 값 변경 시 mesh를 갱신한다. CanvasRenderer를 필수 컴포넌트로 요구하고 OnEnable/OnValidate에서 raycastTarget=false를 유지한다.
- 직접 관계: CombatLayoutAuthoring이 CombatUI.prefab의 Arena 첫 자식에 작성한다. CombatUI는 공개된 artwork Sprite가 있을 때 실루엣을 숨긴다. Graphic/VertexHelper/UIVertex/CanvasRenderer를 사용한다.
- 한계/검수: 몬스터별 실제 그림·애니메이션·VFX가 아닌 자리 표시 도형이다. render component 존재/장식 raycast 비차단/두 비율 실제 캡처를 검수한다. 그림 등록은 후속 작업이다.
