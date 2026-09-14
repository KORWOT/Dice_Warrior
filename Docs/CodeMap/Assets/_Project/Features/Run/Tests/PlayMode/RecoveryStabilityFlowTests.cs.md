# RecoveryStabilityFlowTests.cs

- 역할: 격리 프로필과 실제 authored UI의 복구 회귀를 검증한다.
- 입력/출력: 복제한 시험 설정·TEMP 파일·실제 Raycast/Pointer 버튼 → 저장 bytes/Session/sequence/명령 불변 assertion.
- 핵심 동작: A(n, 미저장 시간 양수)/B(n+2)에서 충돌을 재현하고 오래된 명령/메뉴/RefreshView가 복구를 우회하지 못하며 명시 버튼으로 최신 상태를 읽는지 확인한다.
- 관계: GameApplication prefab/SceneFlow/RunUIController, LocalMetaProgressionService/LocalPlayerDataStore/RunSession을 실제로 사용한다. ProbeStore는 격리 파일의 I/O 실패만 주입한다.
- 수명: 앱/시험 SO를 해제하고 TEMP 하위 검증 경로만 정리한다. 실제 사용자 run/meta 존재·bytes를 전후 비교한다. 경고는 LogAssert로 지정하며 예기치 않은 로그를 허용하지 않는다.
- 검수: category RecoveryStability의 실제 실행 수와 판정은 RECOVERY_STABILITY_REPORT와 아티팩트에서 확인한다.

## RV-02 직접 검증 통과

확인 전 무변경/취소/확인 후 복원/구세대 버튼 거부/복원 후 실제 새 명령, 백업 없음/손상 표시를 실제 레이캐스트 버튼으로 추가 검사한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.

정산된 백업 이후 성장 비용 소비·손상·전체 복원·결과 화면 재정산 사례로, 재화/성장/시작 기록/명령 및 정산 영수증이 같은 시점이고 다시 정산해도 재화가 늘지 않는지 확인한다.
