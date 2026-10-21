# ReplayGenerationProbe.cs

- 역할: RS-IR-02의 상태 해시 차이를 실제 Unity 메모리 실행으로 진단하는 일회성 도구.
- 입출력: 변경하지 않은 ReplayBaseline.json의7개 첫 명령 → 실제 RunSession snapshot과 기존 투영 후 해시, 빈 recoveryGeneration만 제거한 해시, 기존 golden 비교 JSON.
- 동작: 기존 RA-D/지도 신규 필드의 역사적 기본값을 검사하고 투영한다. 새 저장 세대 필드는 존재·문자열 타입·빈 값을 확인한 뒤 그 필드만 제거한다. 다른 차이가 남으면 실패한다.
- 관계: RunSession/RunState/JsonUtility/Newtonsoft, run_script의 ReplayGenerationProbe.Run. Editor 정지 때만 실행하며 SaveStore·Scene·Prefab을 수정하지 않는다.
- 수명/검수: artifacts/recovery-stability/replay-generation-diagnosis.json만 기록한다. 첫 명령 진단이며 전체273명령/14개 재연의 PASS를 대신하지 않는다.
