# ExplorationUI.cs

- 역할: Map/ExplorationRoll/ExplorationCards를 같은 cached typed 화면으로 표시한다. 입력은 ExplorationUIData뿐이며 게임 명령을 직접 계산하거나 저장하지 않는다.
- authored 참조: 기존 instructions/mapContainer/rollChoices/fateChoices/layout 외 campaignMap, layoutVersion 추가. campaignMap은 mapContainer와 같은 GameObject여야 한다.
- 지도/도착 전: compact HUD와 지도, 안내, 메뉴/등급 설정 표시. Map과 미굴림 단계의 상시 ? 주사위 줄은 숨긴다. rollChoices는 Controller가 준 '주사위 창 다시 열기' 행동을 그대로 바인딩한다.
- 카드 단계: 지도를 접고 안내/운명 카드 영역을 강조한다. 기존 6개 실제 결과/족보 및 유료 재굴림 클릭을 보존한다. FateOfferUIData에는 선택 전 비공개 이벤트 ID가 없다.
- 바인딩/수명: PreserveMapOnRebind로 도착 후 가지 fade를 보존한다. 닫을 때 RunScreenView.OnUnbind → Widgets.Clear가 동적 항목을 정리한다. viewport는 하나를 유지하고 지도는 현재위치 쪽, 카드는 위쪽에서 시작한다.
- 관계: RunUIController와 UIWorkbenchPreview가 DTO를 제공한다. UIManager가 캐시/모달 입력 차단, CampaignMapView가 그래프 배치를 소유한다. 별도 DiceRollUI의 굴림/닫기는 Controller 책임이다.
- 검증: 두 세로 비율과 기존 원본 편집 검사를 포함한 실제 통합 결과는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
- LMD-C02: ExplorationUIData.completedNodes를 지도에 전달하고, Map 단계의 강제 bottom scroll을 제거해 CampaignMapView가 현재 진행 위치를 표시하도록 한다. Cards의 top scroll은 유지한다.
