# EncounterUI.cs
- 역할: 일반 사건·휴식 및 상점의 공개 artwork/설명/outcome과 선택 목록을 표시한다. RunScreenView<EncounterUIData> 파생 화면이다.
- serialized 입력: artworkRoot/artwork/artworkFallback, description/outcome/choices, 상속 layout/group. 전체 EncounterUI.prefab은 UiStructureAuthoring가 작성한다.
- 데이터/출력: RunUIController가 만드는 EncounterUIData만 읽으며 선택 delegate를 Widgets에 연결한다. RunSession/store/규칙 평가를 직접 호출하지 않는다.
- 상태/수명: 상점 가격/구매 가능 상태와 휴식 결과는 Controller가 제공한다. 빈 설명은 authored field를 숨긴다. Close는 그림/문구/입력을 정리하며 Buy/보상을 실행하지 않는다.
- 관계: BaseUI/UIManager가 타입별 한 인스턴스를 캐시한다. RunScreenLayout과 기존 네 반복 View 원본을 사용하는 Widgets를 소비한다.
- 검수: 실제 Scene의 대응 phase, authored 고정 필드/스타일 유지, 현재 버튼 key/가용 상태 및 close 이후 잔류 callback/그림 없음. UI_STRUCTURE_REPORT 실행 증거 참조.
