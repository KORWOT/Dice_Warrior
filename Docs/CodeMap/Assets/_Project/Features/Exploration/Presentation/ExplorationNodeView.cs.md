# ExplorationNodeView.cs

추가 UI f91fba13 / FateDice.Runtime / C 담당.
- 역할: 공개 노드 ID/유형/레이블/VisualArtwork를 공통 프레임에 연결하고 선택/페이드를 표현한다. 노드 도달 가능성/합류/진척/보상을 계산하거나 변경하지 않는다.
- API: Bind(nodeId,type,label,visual,style,selectable,selected,callback), Unbind(), FadeOut(seconds), NodeId/Type/IsFading.
- 직접 관계: CommonButtonView.Bind/Unbind, NodeType, VisualArtwork/ButtonAppearance, 코루틴/Time.unscaledDeltaTime. 선택은 주입한 개별 nodeId 콜백으로만 전달한다. 그림 우선순위는 icon → artwork → glyph, tint는 그림/기호에만 적용한다.
- 수명: Bind/Unbind/OnDisable은 이전 코루틴을 취소하고 바인딩 세대를 증가시킨다. 페이드 시작 즉시 Button과 CanvasGroup 입력을 차단한다. 유한 비음수 시간만 허용하고, 0초는 즉시 숨긴다. 각 프레임/완료의 세대 검사로 늦은 작업이 새 바인딩을 숨기지 못한다.
- 완료: alpha0 및 GameObject 숨김만 수행하며 NodeId/Type은 유지한다. 실제 런 노드 기록은 이 View에 전달되지 않는다. Unbind는 ID/유형을 비우고 재Bind는 active/alpha1/새 입력 허용을 복원한다.
- 관계 근거: frame 필드와 호출, 제공된 ID/유형/그림만 사용한다. 실제 ExplorationNodeView.prefab 및 Widgets의 사라지는 노드 결정은 메인 통합 범위다.
- 검증: initial-play-red.json의 두 페이드 테스트를 포함한 View7개 RED 후 구현. 즉시 입력 차단/늦은 완료 취소/완료 ID 보존/재사용 GREEN은 메인 실제 실행에서 PASS 확인다.


- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.
