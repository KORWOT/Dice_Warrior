# 현재 코드 색인

기준: [GDD v0.3](../Dice_Warrior_GDD_v0.3.md), [교체 PLAN](../Plans/SYMBOL_LOOP_REPLACEMENT_PLAN.md).
구형 기능 코드 요약은 [v1 이력](../Archive/v1/CodeMap/INDEX.md)으로 이동했다.
프로젝트 소유 C# 18개와 일회성 정리 도구 1개의 요약을 유지한다.

| 기능 | 스크립트 / 요약 | 현재 역할 |
|---|---|---|
| 데이터 | [LoopData](Assets/_Project/Features/SymbolLoop/Domain/LoopData.cs.md) | 규칙·런·프로필·로그·명령·영수증 |
| 전투 | [SymbolCombat](Assets/_Project/Features/SymbolLoop/Domain/SymbolCombat.cs.md) | 정확한 전체 조합, 순서대로 기본기, 턴 보호막/반격 |
| 캠페인 | [LoopRules](Assets/_Project/Features/SymbolLoop/Domain/LoopRules.cs.md) | 12칸/3지역/완주/성장/선택/보스와 검증 |
| 명령 | [LoopSession](Assets/_Project/Features/SymbolLoop/Runtime/LoopSession.cs.md) | 후보 저장 후 공개, 토큰·재시도·정산 |
| 저장 | [LoopStore](Assets/_Project/Features/SymbolLoop/Runtime/LoopStore.cs.md) | 체크섬·원자 교체·백업·세대·미결 입력 |
| 설정 | [SymbolLoopConfig](Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.cs.md) | MVP 콘텐츠 SO와 새 런 복사 |
| 화면 흐름 | [SymbolLoopApp](Assets/_Project/Features/SymbolLoop/Presentation/SymbolLoopApp.cs.md) | 로비/여정/결과/복구와 세션 연결 |
| 화면 표시 | [SymbolLoopScreen](Assets/_Project/Features/SymbolLoop/Presentation/SymbolLoopScreen.cs.md) | 고정 타일·주사위·버튼 바인딩 |
| 연출 재생 | [SymbolLoopPlayback](Assets/_Project/Features/SymbolLoop/Presentation/SymbolLoopPlayback.cs.md) | 칸별 이동·기호 굴림·스킬/반격 순차 표시 |
| 입체 주사위 | [LoopDieGraphic](Assets/_Project/Features/SymbolLoop/Presentation/LoopDieGraphic.cs.md) | 6면 회전·감속·결과 면 투영 |
| 벡터 아트 | [LoopGlyphGraphic](Assets/_Project/Features/SymbolLoop/Presentation/LoopGlyphGraphic.cs.md) | 기호·말·수호자·칸·효과 그림 |
| 자산 작성 | [SymbolLoopAuthoring](Assets/_Project/Features/SymbolLoop/Editor/SymbolLoopAuthoring.cs.md) | Editor API로 프리팹/SO/Title 연결 |
| 규칙/저장 검증 | [SymbolLoopTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/SymbolLoopTests.cs.md) | 조합·완주·원자성·복구 |
| 실제 UI 검증 | [SymbolLoopFlowTests](Assets/_Project/Features/SymbolLoop/Tests/PlayMode/SymbolLoopFlowTests.cs.md) | 포인터 완주/재표시/두 세로 화면 |
| 공통 UI | [BaseUI](Assets/_Project/Shared/UI/Presentation/BaseUI.cs.md) | typed 화면 바인딩·닫기 수명 |
| 공통 UI | [UIManager](Assets/_Project/Shared/UI/Presentation/UIManager.cs.md) | 명시 등록·캐시·레이어·입력 |
| 공통 UI | [UIRoot](Assets/_Project/Shared/UI/Presentation/UIRoot.cs.md) | Canvas·safe area·레이어 |
| 공통 UI | [CommonButtonView](Assets/_Project/Shared/UI/Presentation/CommonButtonView.cs.md) | 로컬 버튼 표시·콜백 |
| 정리 증거 | [RemoveLegacy](artifacts/symbol-loop-replacement/RemoveLegacy.cs.md) | 승인된 구형 자산 삭제와 직접 참조 검사 |

직접 흐름: Title → SymbolLoopApp → LoopSession → LoopRules/SymbolCombat → LoopStore.
화면은 App → UIManager → SymbolLoopScreen → CommonButtonView로 연결한다.
App → SymbolLoopPlayback → LoopDieGraphic/LoopGlyphGraphic으로 확정 결과를 재생한다. Screen은 Playback.Snap으로 저장 상태를 바로 표시한다.
Core assembly는 Unity 의존이 없고, Runtime은 Core/SharedUI/uGUI/InputSystem을 참조한다.
구형 게임 모드나 호환 어댑터는 없다.
