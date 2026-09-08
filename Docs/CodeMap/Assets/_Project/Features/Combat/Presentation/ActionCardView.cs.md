# ActionCardView.cs

추가 UI f91fba13 / FateDice.Runtime / C 담당.
- 역할: 원본 카드의 시각 데이터와 현재 제시 카드의 ID/등급/계산된 효과/태그를 분리해서 표시한다.
- API: Bind(offeredId,originalId,name,grade,effects,tags,visual,gradeVisual,gradeColor,style,selected), Unbind(), OfferedId/OriginalId.
- 관계: CommonButtonView.Bind/Unbind/SetBorder, VisualArtwork, GradeVisualEntry, Grade, uGUI Image/Text. frame.label은 이름, 독립 gradeLabel/effectLabel/tagsLabel에 전달 문자열을 배치한다. 효과를 계산하거나 원본/Sprite/오브젝트 이름으로 선택 대상을 찾지 않는다. 콜백 대상은 offeredId이며 originalId는 표시 연결의 식별 기록이다.
- 그림/등급: 큰 그림은 artwork → icon → fallbackGlyph. icon/그림에는 VisualArtwork.tint, border/badge/gradeLabel에는 현재 전달 gradeColor를 사용한다. 모든 이미지 preserveAspect. 실제 매핑 조회와 현재 효과 문자열 계산은 Screen/Catalog의 책임이다.
- 재사용: Unbind는 원본/제시 ID, 콜백, 큰 그림/배지/기호/등급/효과/태그를 비운다. Bind는 프레임 alpha/입력 및 텍스트/이미지 가시성을 복원한다. OnDestroy는 해제 경계를 실행한다.
- 실제 자산: ActionCardView.prefab/Widgets 연결은 메인 통합 범위이며 콘텐츠별 Prefab 복사나 범용 카드 클래스를 만들지 않는다.
- 검증: initial-play-red.json에서 재Bind/동일 원본별 독립 ID·등급·효과 테스트가 의도한 RED였음을 확인한 뒤 구현했다. GREEN은 메인 실제 실행에서 PASS 확인다.


- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.

## 전투 배치 변경 (2026-09-08)
- C# Bind 계약은 그대로다. CombatLayoutAuthoring이 같은 prefab을 artwork/이름→등급→실제 효과→태그의 portrait로 배치한다. CombatUI가 공격/보호별 프레임색을 적용하고 gradeLabel은 실제 등급색을 유지한다. 기본3장은 함께,4~5장은 가로 스크롤한다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- gradeLabel만 KoreanText.Grade로 표시한다. 직접 입력한 이름/효과/태그 문구는 View에서 재해석하지 않는다. 폰트는 CommonButtonView 상속과 KoreanUiAuthoring을 통해 Pretendard를 참조한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
