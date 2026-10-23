# ReportWriter.cs

- 역할: 고유 실험 디렉터리의 배치 결과를 기계 판독 원자료와 사람이 검토하는 Markdown 요약으로 쓴다.
- 핵심 동작: `Statistics.BuildSummary` 이후 summary.json/summary.md/matches.csv/participants.csv/failures.jsonl 다섯 파일만 생성한다. 기존 동명 파일·디렉터리가 있으면 거절하고 각 쓰기도 CreateNew를 사용한다. 성공 경기의 참가자 원자료만 participant CSV에 넣고 실패는 match CSV/전용 JSONL에 남긴다. seed/hero/slot 정렬, invariant 숫자·CSV escape·UTF-8 without BOM을 적용한다.
- 입출력: `Write(outputDirectory, BatchResult)` → 생성한 `SimulationSummary`; 입력 디렉터리는 메인이 사전 manifest와 함께 준비한다. JSON 필드명은 공통 DTO의 PascalCase다.
- 사용하는 대상: `Statistics` 집계/DTO, `SimulationData` 배치/참가자/실패/성능 DTO, System.Text.Json, System.IO, CultureInfo.InvariantCulture.
- 사용하는 쪽: BalanceSim CLI `Program`, `StatisticsTests` 실제 파일 round-trip 검사.
- 관계 근거: 메인과 합의한 CLI `ReportWriter.Write` 계약, StatisticsTests 직접 호출. manifest.json 및 matches.jsonl, 최종 performance.json은 메인 소유이며 이 파일은 수정하지 않는다.
- 상태/수명: 호출마다 문자열을 만들고 닫힌 파일로 저장한다. 기본 계측 DTO·전역 문화권·콘텐츠·상태를 변경하지 않는다. 파일 시스템 실패는 호출자에 전파한다.
- 검수 주의: 모든 결과 표의 표본·결측·실패·미실행, AI 고정 조합 교란, 자동 인간 정책, 현재 HP/비율 구분, 다중 비교/paired 방향을 함께 노출한다. 값이 없으면 0 대신 null/NOT_MEASURED다. EventHealOffered는 실제 HP 회복량이 아니며 기술 실제 HP 피해·적 보호막 흡수·플레이어 block·처치 후 생략을 구분한다. Performance.RawOutputBytesBeforeReports는 검증·보고서 작성 전 원자료 bytes이며 performance.json 직전 범위와 최종 디렉터리 전체 크기는 메인이 구분해 별도 기록한다. 기술적 PASS를 밸런스 적합 판정으로 표현하지 않는다.
