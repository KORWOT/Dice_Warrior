# CommonButtonView.cs

추가 UI f91fba13 / FateDice.SharedUI / C 담당.
- 역할: ButtonPurpose(Primary/Secondary/Navigation/Purchase/Die/Trial/Grade), 직렬화 참조형 ButtonAppearance, 공통 버튼의 표시/입력 연결 경계. SharedUI는 uGUI만 참조하며 GameConfig/Grade/NodeType 의존이 없다.
- API: Bind(id,text,icon,fallbackGlyph,style,interactable,selected,clicked), Unbind(), SetBorder(sprite,color), BoundId. 특정 Scene/스킬 실행 대상을 고정하지 않는다.
- 바인딩: 이전 본인 UnityAction만 RemoveListener한 뒤 새 안정 ID 콜백을 등록한다. onClick을 직접 호출해도 active/enabled/interactable/CanvasGroup 허용이 아니면 본인 콜백은 실행하지 않는다. 외부 구독에 RemoveAllListeners를 적용하지 않는다.
- 상태: Unbind는 ID/콜백/동적 icon/border/fallback/텍스트를 비우고 alpha1/입력불가로 만든다. Button 비활성화는 uGUI Selectable의 이전 pressed/hover/selection 상태를 초기화한다. Bind는 active/입력을 새 인자로 복원한다.
- 스타일: explicit icon → ButtonAppearance.icon → glyph/텍스트. 이미지는 preserveAspect, Sprite가 없으면 이미지 영역을 끄고 glyph가 있으면 표시한다. ColorBlock의 normal/selected/pressed/disabled를 적용한다. 최초 연결 때 Prefab의 색/ColorBlock을 기억하여 재사용 시 기준 스타일로 돌아가며 background Sprite/레이아웃은 바꾸지 않는다. 최초 Prefab border Sprite도 기억하여 SetBorder(null,color)는 원본의 얇은 테두리에 색을 적용한다. 바인딩 이미지가 있으면 그 Sprite를 사용하며 Unbind는 동적 border를 비운다. 원본도 없으면 border 영역을 숨긴다.
- 직접 참조: uGUI Button/Image/Text/CanvasGroup, Sprite/Color, UnityAction, Action<string>. 기능 View3개가 Bind/Unbind/SetBorder를 호출한다. Widgets/실제 CommonButtonView.prefab 연결은 메인 통합 범위다.
- 수명: OnDestroy는 본인 클릭 구독만 해제한다. 전역 pooling/게임 규칙/지연 명령 없음.
- 검증: ReusableViewTests7개가 initial-play-red.json에서 전부 NotImplemented RED였음을 직접 확인한 뒤 구현했다. 외부 구독 보존/재Bind 잔여값/입력/스타일 검증 GREEN은 메인 실제 실행에서 PASS 확인다.



- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.
