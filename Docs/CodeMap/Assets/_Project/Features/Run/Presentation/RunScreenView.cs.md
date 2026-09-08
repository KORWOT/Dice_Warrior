# RunScreenView.cs
- 역할: RunScreenView<TData>는 BaseUI<TData>와 IRunScreenView를 연결하는 공통 HUD 바인더다. IRunScreenView.Widgets가 현재 화면의 진단 경계를 제공한다.
- 동작: context/layout 검증, 반복 item helper 생성 또는 context 갱신, 소유 item Clear, authored HUD 문구/가시성, dice/footer 입력 바인딩, 구체 BindScreen 호출. 화면 구조/고정 text는 생성하지 않는다.
- 수명: 같은 ExplorationUI만 PreserveMapOnRebind를 사용한다. 닫기 OnUnbind는 구체 seed/art/content 정리와 반복 item/map 정리를 finally로 보장한다. BaseUI가 typed Data를 비운다.
- SetText/SetArtwork: 내용·가시성·sprite/tint만 갱신한다. authored Text font/style/geometry를 덮지 않고, 빈 artwork/description은 기존 slot을 숨긴다.
- 직접 관계: 7개 구체 화면이 상속, RunUIController가 IRunScreenView.Widgets를 노출. RunScreenLayout/Widgets/UIData를 사용하며 게임 계산 없음.
- 검수: fixed field 재사용과 prefab 편집 전파, same-view 노드 fade 유지, close cleanup 및 비공개 그림이 남지 않는지 확인한다.

## 전투 배치 변경 (2026-09-08)
- OnBind의 HUD를 protected virtual BindHUD(TData)로 추출했다. 기본 동작/context 검증/Clear/BindScreen/Close를 유지하고 CombatUI가 override한다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.


## 로비·캠페인 맵·주사위 창 관계 (2026-09-08)
- MenuUI, ExplorationUI, CombatUI가 BindHUD를 재정의한다. MenuUI는 준비 화면의 header/notice를 유지하고 기존 런 HUD를 숨기며, ExplorationUI는 맵과 운명 카드 단계에 맞춰 dice/fate/footer를 표시한다. OnBind의 context/layout 검증 → Widgets.Clear → BindHUD → BindScreen 순서는 공통으로 유지된다.
- 같은 ExplorationUI의 재Bind는 Widgets.Clear(true)로 CampaignMapView를 보존해 선택 후 가지 fade와 도착 표시가 이어진다. 화면을 닫으면 OnUnbind의 finally에서 Widgets.Clear()가 지도와 반복 항목을 정리한다. 도착 명령·입력 잠금·코루틴의 호출자는 RunUIController이며 이 공통 바인더가 규칙 명령을 실행하지 않는다.
- ExplorationUIData.completedNodes는 RunUIController가 nodeHistory 중 resolvedEventIds와 일치하는 공개 완료 노드를 복사해 공급한 DTO다. ExplorationUI → FateDiceWidgets → CampaignMapView로 전달되며 RunScreenView와 맵은 저장 이력을 직접 읽지 않는다.
- DiceRollUI는 BaseUI<DiceRollUIData>를 직접 상속하는 별도 popup이다. RunScreenView의 HUD나 화면 상속 개수를 늘리지 않고 UIManager의 popup 수명에 참여한다.
- 문서 범위: 실제 소스와 이번 작업의 staged DTO/화면/컨트롤러 직접 관계를 확인한 초안이다. 이 문서 갱신에서는 Unity를 실행하지 않았으며 최종 통합 실행 증거는 메인 기록을 따른다.


## 전투 피드백 직접 관계

- CombatUI는 BindHUD/UnbindScreen에서 비저장 피드백 표시를 초기화하며, 공통 Widgets.Clear는 선택 카드의 남은 scale/color를 복원한다.
- 실행 증거: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
