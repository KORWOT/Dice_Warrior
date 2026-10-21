# LocalSaveDiagnostics.cs

- 역할: RV-04에서 LocalPlayerDataStore의 실제 읽기·직렬화·파일 쓰기를 관측하는 Editor 전용 진단.
- 입력/출력: 해당 저장소 인스턴스에 연결된 Diagnostics → 구간별 호출 수와 Stopwatch 경과 milliseconds 배열. 순서는 primary read/decode 및 무결성/store document Validate/encode/primary atomic write/backup atomic write.
- 동작/수명: 각 using 범위 종료 시 성공/실패 모두 관측한다. 인스턴스별 수집이며 연결하지 않으면 타이머/배열 접근이 없다. Player 빌드는 진단 데이터와 타이머를 제외한다. 결과는 저장 문서에 직렬화하지 않는다.
- 관계: PlayerSaveDocument 및 LocalPlayerDataRecovery partial의 파일 경계, RecoveryStabilityPerformanceTests의 표본 수집. 전역 상태/게임 규칙/UI 의존성 없음.
- 검수: 구간 시간은 store 내부만이며 전체 checkpoint의 Core 검증·복사 비용은 포함하지 않는다. 전체 시간과 합계가 같다고 주장하지 않는다. 실제 상태는 RECOVERY_STABILITY_REPORT 참조.
