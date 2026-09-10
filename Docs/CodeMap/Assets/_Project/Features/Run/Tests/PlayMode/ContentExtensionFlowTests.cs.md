# ContentExtensionFlowTests.cs

- 역할: RA-D 실제 프리팹의 보상/장비/지도/주사위/카드/상점 버튼을 Raycast + pointer 이벤트로 검증한다.
- 입출력: 기존 GameApplication과 InGame 씬을 격리 LocalRunStore로 시작하며 새 카드 획득·실제 추첨·공유 피해30·한국어 fallback 이름, Rare 가격13/15/20/23과 실제 결제·재개·소모품 획득을 검사한다.
- 직접 관계: RunUIController/RunSession/ShopRules/CombatRules와 기존 UI view를 사용한다. NUnit PlayMode runner가 실행한다.
- 상태/수명: TEMP 전용 저장을 사용하고 TearDown에서 app을 해제, 사용자 저장 bytes를 대조한다. Unity 실행과 결과 판정은 메인 담당이다.
- 검수: 실제 authored raycast의 도달 여부/활성 상태를 먼저 검사하며 button.onClick 직접 호출로 우회하지 않는다. 파일 삭제 전 테스트 TEMP 하위 절대경로를 확인한다.
- 스크롤 내용 밖의 버튼은 기존 ScrollRect로 완전히 보이게 이동한 뒤 viewport 포함·raycast를 검사한다. leave가 footer 메뉴에 가려진 개발 FAIL을 확인하고 기존 GUI 검사의 가시성 준비와 동일하게 반영했다.


## 절차형 지도·운명 팝업 직접 회귀 (2026-09-10)

콘텐츠 가격·취득·피해30·저장 oracle는 유지하고 snapshot의 mapGenerationVersion=0으로 기존 경로를 고정한다. Press는 노드 preview/운명 카드 강조 후 state 불변을 확인한 뒤 별도 move/confirm 포인터를 누른다. 운명 카드와 confirm은 top FateChoiceUI의 실제 참조로 조회한다.

이번 갱신은 Unity 미실행(NOT_RUN)이며 과거 PASS를 새 계약의 실행 증거로 재사용하지 않는다. 실제 통합 실행/판정은 Main의 PROCEDURAL_CAMPAIGN_REPORT에 기록한다.


## CAMPAIGN_FLOW_POLISH 현재 입력·진입 계약 (2026-09-10)

Press의 무저장 선택 분기는 fate 카드에만 적용한다. 노드 포인터 한 번에서 실제 이동 명령이 확정되고 기존 Until(!Controller.Busy)를 거친다. fate confirm 후 전투 진입도 같은 실제 Busy 종료 대기에 포함된다. 기존 표시가격·정확 피해30·단일 취득·저장/RNG/bytes oracle를 유지한다.

이 새 사용자 계약이 위 절차형 지도 작업의 노드 preview→move 입력 기록을 대체한다. 검사 추가/삭제는 없고 기존 EventSystem 6실패를 완화하지 않는다. Main의 신규 RED2 실패 확인 후 작성했으며 담당 Unity/컴파일 실행은 NOT_RUN이다. 현재 실행/최종 판정은 CAMPAIGN_FLOW_POLISH_REPORT의 실제 증거를 따른다.
