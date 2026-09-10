# ShopRules.cs

- 역할/입출력/직접 관계/상태 수명: 아래 RA-D 계약을 따른다.

- ShopOffer(productId,price) 및 ShopRules가 저장된 기본가격×등급배율을 AwayFromZero로 정수화해 입장 snapshot을 만든다. RunApplication이 입장/구매/퇴장 수명을 소유하고 UI는 Offer의 가격으로 표시·가능 여부를 판정한다. LocalRunStore/RunApplication의 RestoreLegacy는 유효한 구형 상점만 당시 기본가격으로 복원한다. 최신 SO를 참조하지 않고 RNG를 쓰지 않는다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
