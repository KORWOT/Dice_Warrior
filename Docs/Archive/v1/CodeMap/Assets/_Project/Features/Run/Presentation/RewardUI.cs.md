# RewardUI.cs
- 역할: 공개 artwork, 보상 설명/진행 안내와 명시적 Claim 입력을 표시한다. RunScreenView<RewardUIData> 파생 화면이다.
- serialized 입력: artworkRoot/artwork/artworkFallback, description/instructions/choices, 상속 layout/group. 전체 RewardUI.prefab은 UiStructureAuthoring가 작성한다.
- 데이터/출력: RunUIController가 만드는 RewardUIData만 읽으며 선택 delegate를 Widgets에 연결한다. RunSession/store/규칙 평가를 직접 호출하지 않는다.
- 상태/수명: 보상 수치·진행 판단은 Controller/RunSession 책임이다. 창을 닫거나 재바인딩해도 보상을 적용하지 않는다. Close는 문구/그림/소유 선택을 정리한다.
- 관계: BaseUI/UIManager가 타입별 한 인스턴스를 캐시한다. RunScreenLayout과 기존 네 반복 View 원본을 사용하는 Widgets를 소비한다.
- 검수: 실제 Scene의 대응 phase, authored 고정 필드/스타일 유지, 현재 버튼 key/가용 상태 및 close 이후 잔류 callback/그림 없음. UI_STRUCTURE_REPORT 실행 증거 참조.
