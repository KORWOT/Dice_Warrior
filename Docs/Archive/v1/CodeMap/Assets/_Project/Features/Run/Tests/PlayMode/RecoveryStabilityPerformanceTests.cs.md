# RecoveryStabilityPerformanceTests.cs

- 역할: 과거 런0/100/1000개의 유효한 저장에서 authored Lobby와 실제 체크포인트 비용을 측정한다.
- 입력: 실제 출발/명시 포기 명령을 메모리 시험 저장소에서 실행한 문서. 과거 이력은 abandoned이며 현재 정상 활성 런1개를 포함한다. 전체 문서 검증 및 생산 코드의 envelope 직렬화로 GUID TEMP에 보존한다.
- 출력: artifacts/recovery-stability/performance-TIMESTAMP.json의 환경/소스해시/파일 bytes/실제 primary read 횟수/구간 및 전체 시간/할당/다음 프레임 간격. 워밍업1회 후 조건별5회.
- 관계: LocalMetaProgressionService/LocalPlayerDataStore/LocalSaveDiagnostics/RunSession, GameApplication/기존 Lobby/RunUIController.RefreshView. 기존 콘텐츠·SO를 읽으며 수정하지 않는다.
- 상태/수명: 별도 시험 앱/메타 설정과 GUID TEMP만 정리한다. 실제 사용자 런·프로필·backup·pending은 읽기 전후 bytes 동일을 확인한다.
- 검수: read-only 로비, 저장 직전 CAS를 포함한 checkpoint 읽기2회/쓰기1회/backup1회, 영수증 무변경과 시간6초 증가를 검사한다. 한 로비 갱신의 문서 읽기1회 계약은 초기 측정에서 RED가 될 수 있으며 실제 실행 결과는 REPORT에 기록한다. Editor/Mono/warm cache 수치를 실기기·cold cache·승리/상점 최악 이력 비용으로 일반화하지 않는다.

## RV-04 측정 후 최소 개선

할당 카운터 calibration이 증가하지 않으면 allocatedBytes=-1로 기록한다. monoHeapDeltaBytes는 살아 있는 Mono 힙 증감으로 총 할당과 구분한다. 원자료의 sourceHash로 최적화 전후를 구분한다.
