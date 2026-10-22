# CompetitionTagTests.cs

- 역할: 태그가 Owner를 넘나들거나 출처를 잃지 않도록 하는 동일 소스 NUnit 검사.
- 핵심 동작: Skill/Source/Action/Target/Place 격리, ALL/ANY/NONE·명시 자식 조회, 같은 태그 여러 출처/한 출처 제거, Owner별 Reject/Priority 충돌·동률·억제 복원, stage 동결, 깊은 복사, Ordinal 순서, 검증 거절과 유한 순회를 검사한다.
- 입출력: 11개 테스트와 메모리 내 소형 카탈로그/Grant fixture. 실제 저장/사용자 상태·Unity UI·네트워크를 사용하지 않는다.
- 사용하는 대상: CompetitionTags, TagSnapshot, 태그 DTO, System/LINQ, NUnit.
- 사용하는 쪽: SharedWorld.EditMode.Tests와 Server/DiceWarrior.Competition.Tests가 같은 Tests/EditMode/*.cs 원본을 컴파일한다.
- 관계 근거: Validate/Build/Has/Matches/WithoutSource/Raw/Effective의 직접 호출. 무한 열거 위험을 피하기 위해 넘치는 fixture의 소비 횟수를 측정한다.
- 상태/수명: 테스트마다 정의/그룹/Grant를 새로 만든다. 반환 요소와 입력을 변경해도 기존 스냅샷 판정이 유지되는지 확인한다. 예외 테스트는 원본 Grant가 삭제되지 않았는지도 확인한다.
- 검수: 예상 Owner/태그/순서/소비 횟수는 리터럴이며 자체 구현으로 기대값을 계산하지 않는다. 정의만으로 게임 효과를 실행하는 API가 없다. tags-red.trx에서11개 미구현 실패가 확인됐으며 이후 실제 GREEN/회귀 증거는 Task REPORT/TRX를 따른다. 테스트/Unity 실행은 메인에서 직렬로 수행한다.
