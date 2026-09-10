# KoreanUiTests.cs

## RA-A 현재 계약 (2026-09-09)

한국어 Title/Lobby 흐름의 새 여정에 Seed=33을 명시하여 기존 Combat 첫 노드/첫 운명카드 기대를 보존한다. 로컬 저장·한국어 glyph·레이캐스트·상태 불변 검사는 유지한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

- 역할: 실제 제작 GameApplication/Title/Lobby/InGame과 기존 영어 저장 파일에서 한국어 표시·폰트·입력·상태 불변을 검증하는 PlayMode 통합 테스트6개다.
- 입력/출력: 실제 GameApplication prefab/config snapshot, 임시 LocalRunStore, 720x1280/720x1600 Game View. NUnit/UnityTest assertions로 결과를 보고한다.
- 핵심 검사: 모든 화면/반복 원본 Text의 Pretendard 참조와 includeFontData/dynamic; 실제 한글 glyph 생성/advance; 활성 기본 문구의 영문 잔존과 보이는 Text 높이; Title/Lobby/탐험 포인터·손상 저장·이동 오류; legacy Combat의 카드 ID/실제 효과·재바인딩; 사건/상점/휴식/보물/보상/장비; 승리·패배/lastResult; 사용자 영문·혼합 문구 보존.
- 상태 불변: playedSeconds만 정규화한 전체 RunState와 config/RNG/원본·제시 ID를 비교하고 표시 전후 저장 bytes도 비교한다. 결과 생성은 실제 RunSession 규칙을 사용한다.
- 사용하는 대상/관계: GameApplication/RunUIController/SceneFlowController, UIManager/각 View, Rule/LocalRunStore, AssetDatabase/TrueTypeFontImporter, Raycaster/EventSystem. 기본 명칭/등급의 독립 기대값으로 번역 구현만 되풀이하는 검사를 피한다.
- 상태/수명: 매 테스트 고유 임시 저장 경로를 bootstrap 전에 주입하고 종료 시 소유 GameApplication/임시 폴더를 정리한다. 실제 사용자 저장은 쓰지 않는다. 의도된 Warning만 명시적으로 기대하고 NoUnexpectedReceived를 유지한다.
- 검수 주의: Editor 테스트 전용 AssetDatabase와 Game View 해상도를 사용한다. Android 실기기/배포 검증은 별도다.
- 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
- KUI-01: 높이 허용오차를 0.1px로 강화하고 CanvasRenderer.GetMesh()의 실제 정점 수가 0인 비어 있지 않은 표시 텍스트를 실패로 판정한다. Unity 소유 반환 메시를 수정·파괴하지 않는다.

## 준비 로비·주사위 팝업 조회 (LOBBY_MAP_DICE)
- trial/cap 선택 전에 공개 settingsTab을 실제 pointer로 클릭한다. roll은 top DiceRollUI.rollButton을 같은 실제 pointer helper로 누른다. 메뉴 복귀 fixture는 popup을 명시적으로 닫는다.
- 한국어 텍스트/폰트/보이는 mesh·높이/legacy content/custom passthrough/전체 상태·디스크 bytes 검증은 유지한다.

- 이번 변경 검수 상태: 최종 실행 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
- RA-D-01 수정1: 사용자명 원문보존 fixture의 고정 ID dictionary에 신규 ember_slash의 Ember Cut / 사용자 잔불을 추가한다. 기존 5개 이름·모든 표시/상태/디스크 assertion을 유지하며 신규 실제 정의 6개를 순회해도 누락 ID 예외가 없어야 한다. 실제 검증은 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
