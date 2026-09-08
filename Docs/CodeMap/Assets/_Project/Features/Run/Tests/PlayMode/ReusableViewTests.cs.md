# ReusableViewTests.cs

추가 UI f91fba13 / FateDice.PlayMode.Tests / C 담당.
- 역할: 재사용 View의 연결/해제/선택/fade 계약을 검사하는 PlayMode 테스트7개. 실제 Prefab 생성과 화면 통합은 메인이 별도로 검증한다.
- 직접 참조: CommonButtonView, ExplorationNodeView, ActionCardView, FateCardView, ButtonAppearance/ButtonPurpose, VisualArtwork/GradeVisualEntry, Unity UI, NUnit/UnityTest.
- 검사: 외부 버튼 구독 보존, 이전 콜백/ID/이미지/태그/alpha/input 제거, 선택/눌림/비활성 색, null 그림의 glyph 대체, 같은 원본의 제시 ID별 독립 선택, Fate의 공개 계약, 페이드 즉시 입력 차단/재Bind 뒤 늦은 완료 무효/숨김 후 ID 보존.
- 상태/수명: 테스트 전용 GameObject 컴포넌트를 수동 구성하고 임시 Texture2D/Sprite를 사용한다. 사용자 Scene/Prefab/이미지 자산을 쓰거나 외부 아트를 생성하지 않는다. UnityTearDown이 소유 객체만 Destroy한다.
- 입력 경계: 단위 테스트는 Button.onClick.Invoke로 본인 콜백 보호를 확인한다. 실제 포인터/스크롤/Prefab 원본 연결 검증을 대신하지 않는다.
- 현재 상태: initial-play-red.json 실측17개 중 기존GUI8 PASS, ReusableViewTests7개 전부 의도한 NotImplemented RED, 통합2개 연결 전 FAIL. RED 확인 후 대응4View 구현 완료, 메인 GREEN 실행 PASS. 테스트 소스는 변경하지 않았고 Unity 실행은 메인만 한다.



- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.

## 한글 UI 적용 (2026-09-08)
- 등급·공개 유형의 한국어 기대만 갱신했다. View에 직접 입력한 custom 영문 문자열/글리프/강조 폰트 스타일·리스너·ID 검증을 보존한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
