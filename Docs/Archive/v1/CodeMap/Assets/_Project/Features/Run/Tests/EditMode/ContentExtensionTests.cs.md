# ContentExtensionTests.cs

## COMMIT_REVIEW_8C76732_FOLLOWUP 현재 할인·저장 계약 (2026-09-11)

- 역할·입출력: 실제 DefaultFateDice SO와 PrototypeAuthoring.CreateDefaults 각각에서 등급이 높아질수록 기존 네 상품의 가격이 증가하지 않고 Legendary가 Common보다 저렴함을 검사한다. 현재 조정 가능한 기본 배율은 [1,.95,.9,.85,.8]이고 두 기본값 경로 모두 실제 ShopRules.Price 결과를 독립 가격표와 비교한다.
- 가격 oracle: Common→Legendary의 potion/reroll/die/blade는 [10,12,16,18], [9,11,15,17], [9,11,14,16], [9,10,14,15], [8,10,13,14]. 기존 float→double 및 AwayFromZero 계산을 유지하므로 10×.95f 결과는9다. 실제 상점 입장·구매·재개 검사는 이 신규5행과 기존 증가배율의 정확가격5행을 함께 검증한다.
- 호환·수명: 이전 [1,1.1,1.25,1.5,1.75] 규칙으로 만든 Rare 저장을 실제 LocalRunStore에 기록하고 새 기본값으로 시작한 별도 런의 할인과 비교한다. 기존 저장의 전체 상태·파일 bytes, 규칙배율·확정가격, 구매13/15골드·중복구매 차단·sequence·RNG·보상 후 재개를 유지한다. 실제 SO를 사용하는 새 상점 fixture의 지도만 mode0으로 고정해 가격 검증을 지도 생성과 분리한다.
- 기존 검사 보존: 반올림·0·overflow 및 복사 독립성 시험은 구배율을 명시하여 기존 기대값을 유지한다. 기존 저장실패의 비공개 상태, 카드 획득/효과, 필드 없는 구형JSON 시험과 golden fixture는 변경하지 않는다.
- 직접 관계: NUnit EditMode runner → 이 클래스 → PrototypeAuthoring/FateDiceConfig/ShopRules/RunSession/LocalRunStore. 자산은 읽기만 하고 모든 디스크 검사는 기존 격리 TEMP 수명과 경로 보호를 사용한다. 새 기본값과 과거 저장 규칙을 같은 객체로 덮어쓰지 않는다.
- 검증 상태: 먼저 artifacts/commit-review-followup/price-stage에 준비했다. Unity 컴파일·RED·GREEN은 이 작성 단계에서 NOT_RUN이며 실제 적용/실행/판정은 Main의 COMMIT_REVIEW_8C76732_FOLLOWUP_REPORT를 따른다. 과거 PASS를 새 실행 증거로 사용하지 않는다.

- 역할/입출력/직접 관계/상태 수명: 아래 RA-D 계약을 따른다.

- RA-D 콘텐츠/효과/가격 계약 EditMode 검사. 독립 30피해·23수호, 명시효과 검증, 실제 보물획득→장비→Roll추첨→사용→디스크재개, 중복 소유/RNG, 등급별 정확가격/실패저장/구형JSON/복사 독립성을 검증한다. 격리 TEMP 저장만 사용하고 사용자 저장은 접근하지 않는다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
- 획득한 actionId가 원본 정의나 같은 정의에서 시작한 다른 새 런을 오염시키지 않음을 별도 실행 검증한다.
