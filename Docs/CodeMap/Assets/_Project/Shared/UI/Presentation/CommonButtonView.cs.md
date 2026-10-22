# CommonButtonView.cs

- 역할: 공통 authored Button/Image/Text/CanvasGroup의 표시·입력 바인딩.
- 동작/입출력: Bind(id,text,icon,fallback,style,interactable,selected,clicked), Unbind/SetBorder. 자신의 UnityAction만 교체하고 외부 구독은 보존한다. style과 모든 로컬 참조는 필수다.
- 사용하는 대상: uGUI/UnityEngine/System.Action. 사용하는 쪽: CompetitionScreen 8개 고정 슬롯, CompetitionAuthoring, CompetitionFlowTests.
- 관계 근거: 기존 CommonButtonView.prefab을 새 화면 안에 인스턴스로 작성한다. 구형 카드/탐험 View는 삭제되었다.
- 상태/수명: 바인딩된 ID/콜백, 최초 스타일 캐시를 소유. Unbind/OnDestroy가 자체 구독을 해제한다. 활성/상호작용 가능한 상태에서만 콜백을 실행한다.
- 검수: Sprite 없는 텍스트 버튼, 재바인딩 잔류, CanvasGroup 입력 차단, 타입별 색. 새 흐름은 실제 raycast와 pointerClick으로 검사한다.
