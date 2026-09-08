# RunState.cs
- 역할: RunState, NodeState, OfferedCard, RunRecord, LocalSaveEnvelope의 순수 직렬화 DTO. Sprite/Prefab/View 참조가 없다.
- 상태: schema 1, 런/프로필 ID, 고정 GameConfigData 스냅샷, RNG/seed, 전투·성장·보상·장비·주사위·선택 등급·시간·최근 결과를 보관한다.
- 그래프: nodes는 현재 활성 평탄 노드 목록, availableNodeIds는 지금 선택 가능한 루트, selectedNode는 선택 복제다. 연결은 childIds로 유지하여 재귀 직렬화 깊이 문제를 피한다.
- nodeHistory: PruneTo에서 활성 그래프를 떠난 NodeState를 ID/유형/자식 ID 그대로 보존한다. 보스 전환의 기존 활성 루트 변환은 유지한다. 새 이미지 저장 스키마를 만들지 않았으며 기존 schema 1에서 이 필드 부재는 허용한다.
- OfferedCard: id는 개별 제시 카드 선택, contentId는 원본 정의, type/grade는 확정 결과다. 표시 외형은 별도 VisualCatalog가 담당한다.
- 직접 사용: RunSession, Dice/Fate/Combat/Exploration/Growth 규칙, LocalRunStore, Screen/Widgets 및 테스트. DTO 자체는 명령·RNG·파일 IO를 실행하지 않는다.
- 검증: RunTests의 합류/이력/레거시 저장 5개, 기존 Run/Save 회귀, GUI의 실제 합류·fade·이어하기.
