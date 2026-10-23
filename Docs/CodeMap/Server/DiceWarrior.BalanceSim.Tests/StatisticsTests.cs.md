# StatisticsTests.cs

- 역할: B0/C7 통계의 분모·결측·짝비교·동률·피해 기여와 실제 출력 보존을 인공 결과로 검사한다.
- 핵심 동작: 손계산 Wilson 경계, 표본0/null, NaN 거절, nearest-rank 분위수, 공동1위와 분할점수/시련 구분, 고정 AI 조합별 분리, 실패·NOT_RUN 분모, complete seed block, 입력 순서와 무관한 결정 bootstrap 및 원자료 비변경을 확인한다. 불일치 share/HP 비율·중복 경기 거절, 인간 실제 피해/흡수/생략 및 미관측 분리, HP 타이브레이크/최종시련 상태 빈도를 확인한다. 모든 경기 완료와 별도로 배치 재현 검증 BLOCKED를 요약에서 보존하는 회귀도 검사한다.
- 입출력: NUnit 검사; 자체 생성 BatchResult와 고유 임시 출력 디렉터리를 사용한다. 실제 게임/사용자 저장·콘텐츠를 테스트 입력으로 수정하지 않는다.
- 사용하는 대상: `Statistics.BuildSummary/Wilson95/Describe`, `ReportWriter.Write`, 공통 SimulationData와 OutcomeCollector 관찰 DTO, System.Text.Json, NUnit.
- 사용하는 쪽: 메인이 직렬 실행하는 `DiceWarrior.BalanceSim.Tests` .NET 검사 프로젝트.
- 관계 근거: 공개 API 직접 호출 및 실제 summary.json/participants.csv/summary.md 파일 readback. 다른 하네스·제품 테스트를 대체하지 않는다.
- 상태/수명: 각 fixture는 메모리에서 생성한다. 출력 검사는 GUID 이름의 임시 폴더만 만들고 생성한 파일을 개별 삭제한 뒤 비재귀 폴더 삭제한다. de-DE 문화권 검사는 finally에서 원상 복구한다.
- 검수 주의: 결정성은 반복 결과뿐 아니라 짝차이 방향과 수작업 평균/상수구간도 확인한다. 실패와 미계측을 0으로 보충하는 회귀를 막는다. manifest 원문 보존과 overwrite 거절을 실제 파일로 확인한다. 실행 여부와 PASS 수는 메인 REPORT의 신선한 실행 증거를 따른다.
