# RunUIData.cs
- 역할: 화면별 런타임 표시 DTO. UIData→RunUIData(context,hud)→Menu/Exploration/Combat/Encounter/Reward/Equipment/ResultUIData를 정의한다. ScriptableObject나 저장 모델이 아니다.
- 입출력: RunHUDData는 표시 문구/복사 dice/메뉴·주사위 요청, UIChoiceData는 키/문구/가용·선택 상태와 요청 delegate. ActionOffer는 originalId/id/실제 효과·태그/grade/art, FateOffer는 id/type/grade만 포함한다.
- 데이터 경계: RunUIContext는 주입 manager, 네 반복 원본, visual catalog, presentation 의존 묶음이다. RunSession/LocalRunStore를 보유하지 않는다. Exploration nodes는 Controller가 만든 공개 id/type/childIds 복사다.
- 상태/수명: Controller가 렌더마다 새 표시 데이터를 만들고 BaseUI<TData>가 열려 있는 동안만 보관한다. 닫을 때 Data와 소유 callback을 제거한다. delegate 실행은 사용자 선택에만 대응하며 OnClose 보상 없음.
- 직접 관계: RunUIController가 생산, RunScreenView 및 7개 구체 화면/Widgets가 소비. UIManager는 공통 UIData 타입 계약만 검사한다.
- 검수: ID 분리, private event 정보 부재, rules/save 직접 참조 없음, 콜백 중복/잔류 없는 재바인딩.
- Scene 구조 이후: 기존 7개 Run UI DTO는 그대로이고 TitleUIData는 별도 TitleUI.cs에 있다. 씬 경로나 persistent app/session을 DTO에 추가하지 않는다.

## 전투 배치 변경 (2026-09-08)
- CombatUIData에 표시 전용 battleLabel/turnLabel, enemy/player 이름·HP문구·HP01·shield, intentLabel/value, playerAttributes, handLabel/fatePowerLabel/rerollLabel을 추가했다. Controller가 계산하고 CombatUI가 소비하며 저장·규칙·ActionOffer 계약은 그대로다.
- 현재 실행 증거: Docs/Reports/COMBAT_LAYOUT_REPORT.md.

## 한글 UI 적용 (2026-09-08)
- 문구 필드는 Controller가 한국어 표시 스냅샷으로 채운다. raw tags/command key/원본·제시 ID의 계약은 그대로며 tag 번역은 Widgets의 최종 출력에서 수행한다. DTO 코드 변경은 없다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.

## 플레이 작업실 (2026-09-08)
- UIWorkbenchPreview는 실제 RunSession 상태와 규칙 계산으로 기존 DTO를 채운다. 미리보기에서는 게임 입력 콜백을 연결하지 않으며 컨트롤러·저장소를 만들지 않는다.
- 사용·검증: Docs/Reports/PLAY_WORKBENCH_REPORT.md.

## 준비 로비·캠페인 지도·주사위 창 (2026-09-08)
- MenuUIData는 characterName/characterDetails/growthDetails로 기존 방랑자와 출전 정보·현재 런 성장 안내를 전달한다. 영구 성장 모델/저장을 추가하지 않는다.
- ExplorationUIData.completedNodes는 resolvedEventIds와 nodeHistory에서 확인한 완료 노드의 id/type/childIds 복사다. 미선택 과거 가지·비공개 사건 내용은 전달하지 않는다. Controller와 Preview가 공급하고 ExplorationUI→Widgets→CampaignMapView가 소비한다.
- DiceRollUIData는 UIData를 직접 상속하는 modal 전용 DTO다. title/detail/result, 정확히 6개의 values 또는 null, rolling/duration, roll 선택과 ButtonAppearance를 담는다. RunHUD와 세션·저장 참조를 보유하지 않는다.
- RunUIController/UIWorkbenchPreview가 생산하고 DiceRollUI가 소비한다. 굴림 애니메이션 값은 표시 전용이며 실제 확정 값은 세션 스냅샷에서 복사한다.
- 현재 작업의 검증과 범위: Docs/Reports/LOBBY_MAP_DICE_REPORT.md.



## 조합·카드·전투 피드백 (2026-09-08)

- CombatFeedbackData는 확정된 선택 label/grade, 공격량/막힘/획득 수호/실제 HP 감소, 전후 HP/최대 HP, 이전 적 의도/공격량/흡수/수호와 반격 여부를 전달하는 표시 전용 스냅샷이다. View에 RunState/저장소를 넘기지 않으며 저장 모델이 아니다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
