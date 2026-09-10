# ContentExtensionFlowTests.cs

- 역할: RA-D 실제 프리팹의 보상/장비/지도/주사위/카드/상점 버튼을 Raycast + pointer 이벤트로 검증한다.
- 입출력: 기존 GameApplication과 InGame 씬을 격리 LocalRunStore로 시작하며 새 카드 획득·실제 추첨·공유 피해30·한국어 fallback 이름, Rare 가격13/15/20/23과 실제 결제·재개·소모품 획득을 검사한다.
- 직접 관계: RunUIController/RunSession/ShopRules/CombatRules와 기존 UI view를 사용한다. NUnit PlayMode runner가 실행한다.
- 상태/수명: TEMP 전용 저장을 사용하고 TearDown에서 app을 해제, 사용자 저장 bytes를 대조한다. Unity 실행과 결과 판정은 메인 담당이다.
- 검수: 실제 authored raycast의 도달 여부/활성 상태를 먼저 검사하며 button.onClick 직접 호출로 우회하지 않는다. 파일 삭제 전 테스트 TEMP 하위 절대경로를 확인한다.
- 스크롤 내용 밖의 버튼은 기존 ScrollRect로 완전히 보이게 이동한 뒤 viewport 포함·raycast를 검사한다. leave가 footer 메뉴에 가려진 개발 FAIL을 확인하고 기존 GUI 검사의 가시성 준비와 동일하게 반영했다.
