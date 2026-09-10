# RunStateCopy.cs

- 역할/소속: FateDice.Core의 상태 명시 복제/Runtime 어댑터 공통 경계. JSON·Reflection·엔진·파일 의존이 없다.
- 입출력: CopyFields(source,target)는 config/Rules를 제외한 기존 RunStateData의 모든 필드를 복제한다. ToCore(source)는 Rules.DeepCopy()와 CopyFields로 CoreRunState를 만든다. Node/Card/Record는 각각 독립 DTO를 반환한다. source/target 필수 인자 null은 ArgumentNullException이다.
- 직접 관계: RunApplication과 CoreRunState의 후보/표시 복제, Runtime RunState/RunSession의 checkpoint/export 조합이 사용한다. pendingReward는 RulesCopy.Reward를 사용한다. NodeState/OfferedCard/RunRecord는 RunStateData.cs에 있다.
- 상태/수명: 문자열만 공유하며 배열·List·목록 원소·자식 ID 목록·선택 노드·보상·결과 기록은 새로 복제한다. null/빈 컬렉션과 null 원소를 보존한다. 선택 노드와 활성 노드의 같은 ID는 유지하지만 mutable 참조를 공유하지 않는다.
- 검수: 원본/대상의 config는 건드리지 않는다. 새 상태 필드는 CopyFields에 반드시 추가해야 한다. schema/seed/RNG/sequence/처리 ID/playedSeconds도 누락 없이 복제하며 시간이나 ID를 보정하지 않는다. 필드 전수 비교와 구형 저장·전체 명령 재연 실행은 메인 REPORT에서 확인한다.

- RA-D: ShopOffer 원소와 목록을 독립 복제한다. 규칙 effects/addActionId/가격배율도 RulesCopy를 통해 복제한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.


## 절차 지도 좌표 복사 (2026-09-10)
- Node 복사는 floor/lane도 보존한다. active/history/selectedNode 모두 같은 복사 경로로 전달하므로 Core 후보와 Runtime 디스크 snapshot 간 좌표가 사라지지 않는다.
- 자식 ID 목록과 원소의 독립 복사를 유지한다. 규칙 모드·열/경로 개수는 RulesCopy.World의 saved rule 복사를 따른다. ProceduralMapTests가 좌표/설정/child list 격리와 저장 왕복을 검사한다.
