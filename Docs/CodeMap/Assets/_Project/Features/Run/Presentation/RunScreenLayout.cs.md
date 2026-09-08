# RunScreenLayout.cs
- 역할: 전체 화면 prefab 안의 common HUD Text(header/stats/situation/fate/notice/gear), diceRow/body/footer, ScrollRect의 serialized 직접 참조와 Validate를 제공한다.
- 입출력/관계: UiStructureAuthoring가 실제 객체를 Editor에서 연결한다. RunScreenView/Widgets가 이를 사용하며 경로 검색이나 런타임 scaffold 생성으로 누락을 대체하지 않는다.
- 상태/수명: layout은 UIManager가 캐시한 화면 객체 수명을 따른다. 게임 상태 없음. 필수 참조가 없으면 바인딩 전에 명확한 오류를 낸다.
- 검수: Inspector 편집 가능한 고정 배치, 맞는 scroll.content/viewport, 화면별 필요한 fixed field와 item container 관계.

## 전투 배치 변경 (2026-09-08)
- CombatLayoutAuthoring이 전투만 별도 배치한다. header=battleLabel, stats=플레이어HP, situation=예고, fate=족보, gear=비활성, body=actionGrid content, scroll=가로 actionScroll, footer=authored menu parent다. 다른 화면 의미는 그대로다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 현재 authored Text와 InputField는 Pretendard를 참조한다. 기존 layout 구조/높이/생성 코드의 동작은 변경하지 않았으며 KoreanUiTests에서 보이는 한국어의 높이 초과를 검사한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
