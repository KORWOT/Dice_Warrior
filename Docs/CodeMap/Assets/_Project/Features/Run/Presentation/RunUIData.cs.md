# RunUIData.cs
- 역할: 화면별 런타임 표시 DTO. UIData→RunUIData(context,hud)→Menu/Exploration/Combat/Encounter/Reward/Equipment/ResultUIData를 정의한다. ScriptableObject나 저장 모델이 아니다.
- 입출력: RunHUDData는 표시 문구/복사 dice/메뉴·주사위 요청, UIChoiceData는 키/문구/가용·선택 상태와 요청 delegate. ActionOffer는 originalId/id/실제 효과·태그/grade/art, FateOffer는 id/type/grade만 포함한다.
- 데이터 경계: RunUIContext는 주입 manager, 네 반복 원본, visual catalog, presentation 의존 묶음이다. RunSession/LocalRunStore를 보유하지 않는다. 신규 지도의 campaignNodes는 CampaignMapProjection이 만든 공개 DTO이며, nodes/completedNodes는 구형 지도 전용 복사다.
- 상태/수명: Controller가 렌더마다 새 표시 데이터를 만들고 BaseUI<TData>가 열려 있는 동안만 보관한다. 닫을 때 Data와 소유 callback을 제거한다. delegate 실행은 사용자 선택에만 대응하며 OnClose 보상 없음.
- 직접 관계: RunUIController/UIWorkbenchPreview가 생산하고 CampaignMapProjection이 지도 필드를 채운다. RunScreenView 및 구체 화면/Widgets가 소비한다. 별도 FateChoiceUIData도 RunUIData를 상속해 공통 HUD/context와 FateOfferUIData를 사용한다. UIManager는 공통 UIData 타입 계약만 검사한다.
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
- 구형 모드0의 ExplorationUIData.completedNodes는 resolvedEventIds와 nodeHistory에서 확인한 완료 노드의 id/type/childIds 복사다. Controller와 Preview가 공급하고 ExplorationUI→Widgets→CampaignMapView가 소비한다. 신규 모드1의 전체 지도 공개 계약은 아래 절을 따른다.
- DiceRollUIData는 UIData를 직접 상속하는 modal 전용 DTO다. title/detail/result, 정확히 6개의 values 또는 null, rolling/duration, roll 선택과 ButtonAppearance를 담는다. RunHUD와 세션·저장 참조를 보유하지 않는다.
- RunUIController/UIWorkbenchPreview가 생산하고 DiceRollUI가 소비한다. 굴림 애니메이션 값은 표시 전용이며 실제 확정 값은 세션 스냅샷에서 복사한다.
- 현재 작업의 검증과 범위: Docs/Reports/LOBBY_MAP_DICE_REPORT.md.



## 조합·카드·전투 피드백 (2026-09-08)

- CombatFeedbackData는 확정된 선택 label/grade, 공격량/막힘/획득 수호/실제 HP 감소, 전후 HP/최대 HP, 이전 적 의도/공격량/흡수/수호와 반격 여부를 전달하는 표시 전용 스냅샷이다. View에 RunState/저장소를 넘기지 않으며 저장 모델이 아니다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.


## 주사위 조합 연출 (2026-09-10)

DiceRollUIData에 comboName/hand/comboStrength/holdSeconds 표시 필드를 추가했다. Controller/정지 작업실이 공급하고 DiceRollUI/DiceResultFeedback이 소비한다. comboStrength는 저장된 priority의 순위 0..1이며 게임 규칙이나 카드 Grade가 아니다. 저장 DTO에는 추가하지 않는다.
검증 상태: DICE_PRESENTATION_EFFECTS_REPORT의 실제 결과를 따른다.

## 절차 지도·운명 팝업 현재 계약 (2026-09-10)

- CampaignNodeUIData는 id, nullable type, floor/lane, childIds와 revealed/available/completed/unreachable를 전달한다. 먼 노드의 유형은 null로 제거하며 실제 사건·보상 ID를 포함하지 않는다. 좌표·연결·완료·접근 불가를 서로 구분하므로 미선택 가지가 완료 기록으로 바뀌지 않는다.
- ExplorationUIData.campaignNodes는 신규 모드1의 전체 활성/기록 그래프, currentNodeId는 현재 위치, mapFloors는 일반층 수다. mapProgressLabel/mapGoldLabel/mapHealthLabel/mapWardLabel은 이미 계산한 표시 문구다. CampaignMapProjection.Apply는 신규 모드에서 nodes/completedNodes를 빈 배열로 만들어 원본 유형의 우회 전달도 막는다. 모드0은 campaignNodes=null로 기존 표시 경로를 사용한다.
- chooseNode는 실제 이동 요청이며 ExplorationUI가 노드 미리보기와 하단 이동 버튼을 분리한다. roll은 주사위 또는 운명 선택 창의 재개 요청이다. 런타임 탐험 fates는 빈 배열이고 실제 후보는 별도 FateChoiceUIData.offers에만 전달한다.
- FateOfferUIData의 id/type/grade와 일반 유형 설명만 선택 전에 공개한다. 콜백 필드는 표시층 요청일 뿐 규칙 실행을 소유하지 않는다. FateChoiceUIData의 confirm/close는 해당 팝업의 별도 1:1 요약에서 다룬다.
- 검수 주의: 저장용 NodeState/RunState의 신규 필드를 View에 직접 넘기지 않는다. 공개 깊이는 저장된 설정을 사용하며 기본2층, 보스 표식은 항상 공개한다. 렌더별 새 배열·콜백의 바인딩 수명을 유지한다.
- 구현 계약과 실제 검증 상태: Docs/Plans/PROCEDURAL_CAMPAIGN_PLAN.md, Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md. 이 요약은 실행 PASS의 증거가 아니다.
