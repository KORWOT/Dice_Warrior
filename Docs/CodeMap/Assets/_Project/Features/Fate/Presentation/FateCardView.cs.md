# FateCardView.cs

추가 UI f91fba13 / FateDice.Runtime / C 담당.
- 역할: 선택 전 공개 NodeType/Grade/그림과 제시 ID만 표시한다. EventDefinition이나 비공개 사건 ID를 받는 API가 없다.
- API: Bind(offeredId,type,grade,publicVisual,gradeVisual,gradeColor,style,selected), Unbind(), OfferedId. 레이블은 type + "  /  " + grade, 선택 대상은 offeredId다.
- 직접 관계: CommonButtonView.Bind/Unbind/SetBorder, 공개용 VisualArtwork, GradeVisualEntry, NodeType/Grade, uGUI Image/Text. 큰 그림은 artwork → icon → 공개 fallbackGlyph, preserveAspect로 표시한다. badge/border/레이블은 전달된 현재 등급 색을 사용한다.
- 공개 경계: 이 View는 사건 정의/콘텐츠 데이터/저장 상태를 조회하지 않는다. 실제 공개 이미지 매핑 선택은 Screen/Catalog가 맡으며, GameObject 이름을 바꾸거나 그림에서 명령을 추론하지 않는다.
- 상태/수명: Unbind/OnDestroy에서 ID/본인 콜백/그림/배지/fallback을 비운다. Bind는 공통 프레임 alpha/입력과 새 이미지/텍스트를 다시 연결한다. 실제 FateCardView.prefab/Widgets 사용은 메인 통합 범위다.
- 검증: initial-play-red.json의 공개 파라미터/재Bind 그림·콜백 검사 RED 후 구현했다. GREEN은 메인 실제 실행에서 PASS 확인다.


- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.

## 한글 UI 적용 (2026-09-08)
- 공개 유형과 등급을 KoreanText.Node/Grade로 표시한다. 비공개 사건 ID를 받지 않는 경계와 OfferedId/입력 콜백을 유지한다. 폰트는 공통 원본 상속을 유지한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
