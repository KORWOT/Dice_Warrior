# FateChoiceAuthoring.cs

- 역할: public static Apply()로 승인된 운명 카드 세로 원본과 새 선택 popup 및 UIRoot 등록을 작성하는 Editor migration이다. 메뉴 Fate Dice/운명 선택 팝업 원본 적용에서도 호출한다.
- 정확한 쓰기: Fate/Prefabs/FateCardView.prefab, 새 Fate/Prefabs/FateChoiceUI.prefab, Shared/UI/Prefabs/UIRoot.prefab의 등록만 변경한다. CommonButtonView와 그 한글 폰트는 읽기/인스턴스 참조만 사용한다. Scene/설정/패키지/공급사 원본은 쓰지 않는다.
- 동작: 카드 choicePresentationVersion<1에서 기존 CommonButton frame/폰트/SelectionFeedback/FEEL을 유지하며 큰 그림/유형/등급/일반설명/금색 선택 테두리를 작성한다. 새 popup은 664×1040 panel, 가로 scroll/3개 세로 카드, 명시 confirm/close, 재굴림6개 행을 작성한다. 카드원본 직접 참조와 renderer를 연결한다.
- 멱등/실패: 적용된 카드 버전과 이미 존재하는 popup은 재작성하지 않는다. 등록도 같은 타입이 없을 때만 추가한다. Play/컴파일/가져오기/preview stage/dirty scene 및 누락된 기존 폰트·FEEL·등록을 거절한다. 정확한 PrefabUtility.SaveAsPrefabAsset만 사용하며 GUID를 재생성하지 않는다. 새 popup은 임시 PreviewScene에서 만들고 finally로 정리한다.
- 관계: FateChoiceUI/FateCardView/CommonButtonView/DiceFaceView/SelectionFeedback/UIRoot/UIManager, UnityEditor PrefabUtility/AssetDatabase/EditorSceneManager. 메인이 정지 Editor API로 Apply를 호출하는 유일한 실행 담당이다.
- 검수 상태: 소스 작성만 수행했고 자산 작성/멱등 실측은 NOT_RUN이다. 실제 Apply와 원본 GUID/FEEL/폰트·비대상 보호 검증은 PROCEDURAL_CAMPAIGN_REPORT를 따른다.

- 버전2 보완: 기존 버전1 카드에는 선택 테두리의 center fill 해제와 공개 유형 도형 참조만 추가한다. 버전2 이상은 이 보완도 재실행하지 않는다. 신규 카드도 동일 순서로 최종 버전2가 된다.
