# ContentExtensionFlowTests.cs

- 역할: RA-D 실제 프리팹의 보상/장비/지도/주사위/카드/상점 버튼을 Raycast + pointer 이벤트로 검증한다.
- 입출력: 기존 GameApplication과 InGame 씬을 격리 LocalRunStore로 시작하며 새 카드 획득·실제 추첨·공유 피해30·한국어 fallback 이름, 현재 Rare 할인9/11/14/16 및 이전 저장가격13/15/20/23의 실제 결제·재개·소모품 획득을 검사한다.
- 직접 관계: RunUIController/RunSession/ShopRules/CombatRules와 기존 UI view를 사용한다. NUnit PlayMode runner가 실행한다.
- 상태/수명: TEMP 전용 저장을 사용하고 TearDown에서 app을 해제, 사용자 저장 bytes를 대조한다. Unity 실행과 결과 판정은 메인 담당이다.
- 검수: 실제 authored raycast의 도달 여부/활성 상태를 먼저 검사하며 button.onClick 직접 호출로 우회하지 않는다. 파일 삭제 전 테스트 TEMP 하위 절대경로를 확인한다.
- 스크롤 내용 밖의 버튼은 기존 ScrollRect로 완전히 보이게 이동한 뒤 viewport 포함·raycast를 검사한다. leave가 footer 메뉴에 가려진 개발 FAIL을 확인하고 기존 GUI 검사의 가시성 준비와 동일하게 반영했다.

## COMMIT_REVIEW_8C76732_FOLLOWUP 현재 할인·기존 저장 UI 계약 (2026-09-11)

- Prepare는 기본적으로 실제 GameApplication prefab이 참조하는 SO snapshot을 사용하며 기존 mode0 가격 fixture를 유지한다. 이전 저장 전용 사례만 [1,1.1,1.25,1.5,1.75]를 그 snapshot에 명시하고 격리 저장에 기록한다. SO 자산은 수정하지 않는다.
- 현재 기본 사례는 Rare9/11/14/16을, 이전 저장 사례는13/15/20/23을 동일 VerifyShopButtonsAndResume 입력 oracle로 전달한다. 표시 문자열·실제 raycast·포인터 클릭, potion 차감9 또는13, 재시작 후 소유·가격 유지, reroll까지 누적차감20 또는28, 보상/재구매 차단/퇴장 시 offer 정리를 모두 검사한다. 예상 가격은 실제 런에서 계산해 만들지 않고 각 테스트에 명시한 값이다.
- 이전 저장 사례는 현재 prefab 설정이 할인배율임을 먼저 확인하고, 이어하기가 저장 속 이전배율·가격으로 표시·결제함을 확인한다. Open의 무저장 assertion, TEMP 디스크 snapshot 비교, 사용자 저장 bytes 보존 및 기존 카드 효과 검사는 유지한다.
- 상태·직접 관계: 두 UnityTest → 공통 검사 coroutine → 실제 RunUIController/UI 및 LocalRunStore. 기존 app 해제·다시 Bootstrap·InGame 진입으로 재개하며 새로운 런타임 API나 검사 전용 생산코드를 추가하지 않는다.
- 검증 상태: stage 작성 시 Unity 컴파일·RED·GREEN은 NOT_RUN이다. Main만 현재 PlayMode 종료 후 적용·직렬 실행하며 실제 증거는 COMMIT_REVIEW_8C76732_FOLLOWUP_REPORT를 따른다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

콘텐츠 가격·취득·피해30·저장 oracle는 유지하고 snapshot의 mapGenerationVersion=0으로 기존 경로를 고정한다. Press는 노드 preview/운명 카드 강조 후 state 불변을 확인한 뒤 별도 move/confirm 포인터를 누른다. 운명 카드와 confirm은 top FateChoiceUI의 실제 참조로 조회한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

Press의 무저장 선택 분기는 fate 카드에만 적용한다. 노드 포인터 한 번에서 실제 이동 명령이 확정되고 기존 Until(!Controller.Busy)를 거친다. fate confirm 후 전투 진입도 같은 실제 Busy 종료 대기에 포함된다. 기존 표시가격·정확 피해30·단일 취득·저장/RNG/bytes oracle를 유지한다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
