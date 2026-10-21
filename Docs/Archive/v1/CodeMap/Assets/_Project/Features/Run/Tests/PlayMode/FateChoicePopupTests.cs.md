# FateChoicePopupTests.cs

- 역할: PROCEDURAL_CAMPAIGN의 별도 운명 선택 popup 계약을 실제 제작 원본과 공개 API에서 검증한다.
- 현재 RED: reflection으로 FateChoiceUI를 조회하므로 신규 타입이 없는 현재 소스에서도 컴파일하고 명시적 missing-popup assertion으로 실패한다. 이후 실제 prefab의 confirm/close CommonButtonView와 RunUIData 기반 전용 offers/confirm/close 계약을 확인한다.
- 사용하는 대상/관계: 기존 FateCardView Runtime assembly, BaseUI/RunUIData/FateOfferUIData/CommonButtonView, UnityEditor AssetDatabase, NUnit. Unity PlayMode Test Runner가 호출하며 제품 코드가 시험을 호출하지 않는다.
- 실제 UI: 총7개 검사다. 카드 탭은 선택만 하고 confirm/close는 1회 요청하는지, 재Bind·재굴림 선택 해제 및 낡은 카드 입력 거절, 실제 FEEL 재생 후 exit/낡은 iterator가 새 alpha/pose를 변경하지 않는지, 720×1280/1600의 세 카드·한글/재굴림6개 행/실제 raycast, 5개 scroll의 마지막 카드 confirm을 검사한다.
- 수명/격리: 실제 GameApplication prefab은 context/원본 참조를 읽기만 한다. UIRoot/popup/card 복제와 필요할 때 자체 EventSystem만 생성·정리한다. GameApplication/RunSession/저장소/Scene 이동을 시작하지 않고 사용자 저장·원본 Scene/Prefab/설정·전역 RNG에 쓰지 않는다. 게임 callback은 계수기로 관찰하며 offer.clicked가 잘못 실행되지 않아야 한다.
- 검증 상태: 메인이 popup-red.json의 missing-popup 실제 RED 1/1을 확인했다. 보조는 구현 후 Unity를 실행하지 않았으며 최신 컴파일/GREEN은 NOT_RUN이다. 메인이 Unity를 직렬 실행하고 PROCEDURAL_CAMPAIGN_REPORT에 기록한다. 같은 Task의 최초 통합 검토/보완 예산을 따른다.
