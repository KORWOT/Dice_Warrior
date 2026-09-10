# RunStateData.cs

- 역할/소속: FateDice.Core Domain의 가변 런 상태 공통 필드 및 NodeState/OfferedCard/RunRecord DTO. 기존 schema1의 config 이외 필드명·기본값을 그대로 보존한다.
- 입출력: abstract Rules getter로 순수 RunRulesCatalog를 제공한다. HP/자원/주사위·RNG/안정 ID/노드·이력/확정 카드·보상/시간·최근 결과 필드를 보유하며 스스로 명령·저장·난수를 실행하지 않는다.
- 직접 관계: CoreRunState가 순수 규칙 config를 연결하고 Runtime RunState가 저장/표시 호환 config를 연결한다. RunStateCopy가 전체 필드를 명시 복제한다. Dice/Fate/Combat/Growth/Exploration 규칙과 RunStateValidator는 이 공통 상태만 참조한다.
- 상태/수명: 외부 표시/저장 DTO와 Core 세션의 후보를 같은 mutable 인스턴스로 공유하지 않는다. Core 후보는 RunApplication 경계에서만 확정한다. config는 이 기반 클래스에 두지 않아 Runtime 표시 설정이 Core 상태에 섞이지 않는다.
- 검수: 노드/보상 부재는 기존 phase+ID 의미로 검증하고 null/빈 DTO를 임의로 지우지 않는다. 새 필드를 추가하면 RunStateCopy.CopyFields와 저장/재연 검사를 함께 갱신한다. 실제 Unity 상속 필드 직렬화 및 schema1 실행은 메인 REPORT를 따른다.

- RA-D: shopOffers 목록은 상점 입장 때 고정한 productId/price를 저장하며 보상·장비선택 중에도 동일 상점 수명을 유지한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
