# CombatUI.cs
- 원본 적용 메타데이터: entryVersion은 CampaignFlowAuthoring이 진입 시간/강도를 최초1회 작성했는지 나타낸다. 런타임 전투 판정이나 재생 횟수로 사용하지 않는다.
- 역할: RunScreenView<CombatUIData>를 상속하는 전투 전용 header/HP/arena/dice/portrait 카드 화면.
- 입력/출력: Controller가 계산한 실제 전투명/누적턴/이름/HP01/보호막/예고 수치/족보/운명력/재굴림과 기존 ActionOffer/roll delegate. RunSession/store/규칙 평가를 직접 호출하지 않는다.
- authored 참조: 기존 artworkRoot/artwork/artworkFallback/rollChoices/actionChoices/layout/group와 arena, HP fills/Text, intentValue, player 정보, menuButton/diePrefab, actionScroll/actionGrid, stageGraphic, layoutVersion. CombatLayoutAuthoring이 직접 직렬화한다.
- 동작: BindHUD override가 HUD/HP fill anchor와 authored menu 소유 callback/key를 바인딩한다. 주사위는 CombatDie variant/푸른 appearance. BindScreen은 공개 Sprite를 기존 slot에 연결하며 없을 때 장식 실루엣을 표시한다.
- 카드: 제시 전체를 Widgets.ActionCard에 전달한다. defense 태그는 푸른 프레임, 나머지는 붉은 프레임이며 등급 text는 실제 등급색이다. originalId/offeredId/효과/태그/요청은 그대로다.
- 배치/수명: LateUpdate에서 viewport 폭에 최대3개가 맞도록 grid 셀 폭만 조절한다. 기본3장은 동시 표시하고4~5장은 가로 스크롤한다. Close는 menu listener/그림/추가 문구를 비우며 Base가 반복 item을 정리한다. 고정 골격은 runtime 생성하지 않는다.
- 직접 관계: RunUIController → CombatUIData → UIManager → CombatUI → FateDiceWidgets/ActionCardView/CommonButtonView/CombatStageGraphic. RunScreenLayout.body/scroll은 action grid/가로 ScrollRect다.
- 검수: CombatLayoutTests의 두 세로비/실제 값/입력/카드3·5/재굴림/저장 oracle와 FateDiceGuiTests의 공개 그림/공통 원본 상속 회귀. COMBAT_LAYOUT_REPORT 참조.

## 한글 UI 적용 (2026-09-08)
- Controller가 한국어 전투/누적 턴/다음 행동/체력·수호 데이터를 제공한다. defense raw tag를 보고 파란 카드 색을 선택하는 경계와 기존 전투 배치/실루엣은 유지된다. 모든 Text 폰트는 Pretendard다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.



## 조합·카드·전투 피드백 (2026-09-08)

- CombatFeedbackAuthoring이 추가하는 actionFeedback/damageFeedback/hitFlash, actionFeedbackSeconds/shakePixels/feedbackHoldSeconds/feedbackVersion을 사용한다. PlayFeedback(CombatFeedbackData)는 플레이어 결과와 생존 적 반응을 순서대로 표시하며 HP bar/Text를 단계별 확정치로 갱신한다. 피해량과 막힌 양, 획득 수호와 실제 흡수를 구분한다. arena만 감쇠 흔들림, 텍스트 확대·상승, 약한 색상 flash를 표시하고 IsFeedbackPlaying으로 재생 수명을 노출한다. Bind/Unbind/OnDisable/finally에서 텍스트·색상·위치를 정리한다. 규칙이나 저장은 직접 실행하지 않는다.
- 실제 검증 및 한계: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
- RA-B-02 수정2: arena의 흔들림은 기존 anchored 좌표로 계산하되 PlayFeedback 진입 시 localPosition도 보존한다. Impact 종료와 ResetFeedback/중단 복원은 원래 localPosition을 직접 사용해 RectTransform anchor 변환에서 분수 좌표가 사라지지 않게 한다. 기존 시간/표시/연출 크기/Prefab은 유지한다. RunBoundaryFlowTests의 실제 분수좌표 완전동등 RED→GREEN과 기존 연출/오류복구 검증을 함께 확인한다.

- B-02 추가 수정3 검증: 정상 종료의 기준은 완료된 RectTransform layout 위치이며, 중단은 같은 프레임의 실제 captured localPosition이다. Unity 초기 layout 계산 자체를 연출이 되돌리도록 하지 않는다. 대응 테스트는 안정된 분수좌표 종료 및 초기 tiny fractional 값의 동기 중단을 각각 완전동등 비교한다.

- B-02 최종 원인/수정: anchoredPosition getter 자체가 pending RectTransform layout을 갱신할 수 있으므로 localPosition을 반드시 먼저 보존한 뒤 anchored 흔들림 기준을 읽는다. 종료/중단은 보존 local로 복원한다. getter 독립 probe와 같은 프레임 중단 RED→GREEN을 사용하며 신규 serialized 필드/Prefab 변경은 없다.


## 등급별 연출 수명 (2026-09-10)

actionFeedbackSeconds/feedbackHoldSeconds는 직접 애니메이션과 읽기 기간이며 유한한 비음수만 허용한다. 0은 생략하고 .1초 숨은 최솟값은 없다. feedbackGeneration을 재생/리셋마다 추적해 이전 Impact·반격·finally가 새 binding/새 재생을 덮지 못한다. 실제 coroutine이 끝난 뒤 Controller 진행, 10초 제한은 상위 Playback이 감독한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.

## 전투 진입 연출 (2026-09-10 · CAMPAIGN_FLOW_POLISH)

- 역할/API: PlayEntry():IEnumerator가 이미 바인딩된 적의 등장과 전투 시작 표시를 재생한다. IsEntryPlaying은 실제 재생 중 상태, EntryBindingVersion은 BindHUD마다 증가하는 화면 바인딩 세대다. ResetEntry()는 소유한 진입 연출을 취소하고 원본 표시를 복원한다. 전투 진입·다음 턴·저장 재개의 구분과 주사위 창 열기는 RunUIController의 책임이다.
- 작성 설정: CombatUI 원본의 entrySeconds 기본0.7초, entryIntensity 기본0.06(0..0.25)을 사용한다. 시간은 유한한 비음수만 허용하며 0초는 추가 대기 없이 종료한다. 실행 중 설정을 다시 읽어 재생 길이를 바꾸지 않고 시작값을 캡처한다. 숨은 최소 지연이나 자체10초 타이머는 추가하지 않는다.
- 기존 참조 재사용: arena.localScale이 조금 작은 크기에서 원래 크기로 정착한다. stageGraphic/artwork의 원래 색 알파를 기준으로 등장하고, actionFeedback에 ‘전투 시작’과 Data.enemyName을 표시해 확대·페이드한다. hitFlash는 강도에 비례한 약한 금빛이다. 새 CanvasGroup·동적 참조 탐색·이미지·프리팹 자식은 만들지 않는다.
- 수명: PlayEntry 호출 시 EntryBindingVersion을 private iterator에 캡처하므로 실행되기 전에 다시 바인딩된 호출은 재생하지 않는다. 실행 중에는 별도 entryGeneration과 binding·IsOpen·활성 상태를 확인한다. 완료/finally/ResetEntry/BindHUD/UnbindScreen/OnDisable에서 소유 세대의 크기와 색을 복원하고 임시 시작 문구를 비운다. 이전 iterator의 지연 종료가 새 binding이나 새 연출을 정리하지 않는다.
- 포즈 보존: 진입 연출은 arena의 localScale만 바꾸고 anchoredPosition/localPosition을 읽거나 쓰지 않는다. 기존 타격 연출의 분수 좌표 복원 경계를 유지한다. actionFeedback의 원래 scale/color와 stage/artwork/hitFlash 색을 별도로 캡처·복원한다.
- 공유 효과 소유권: 진입 시작 전에 ResetFeedback, 타격 PlayFeedback 시작 전에 ResetEntry를 호출한다. 두 연출이 actionFeedback/hitFlash를 동시에 소유하지 않는다. ResetEntry가 실제 재생 중이 아니면 이 공유 텍스트·색을 건드리지 않는다.
- 직접 관계 근거: RunUIController의 진입 흐름 → PlayEntry → 실시간 Time.realtimeSinceStartupAsDouble 기반 IEnumerator. 상위 PresentationPlayback이 실제 종료를 기다리고 전체10초 watchdog·입력잠금·주사위 준비/복구를 담당한다. CombatUI는 게임 명령·RNG·저장·다음 턴 판정을 호출하지 않는다.
- 검수: 0초·유한 값·timeScale0, 실제 완료 전/후 상태, 원본 scale/color 복원, disable/rebind와 이전 iterator dispose, 기존 타격 연출 회귀를 확인한다. 소스 구현 완료이며 Unity는 Main이 직렬 검증한다. 실제 결과는 Docs/Reports/CAMPAIGN_FLOW_POLISH_REPORT.md를 따르고 이전 작업의 PASS를 이번 검증으로 대체하지 않는다.
