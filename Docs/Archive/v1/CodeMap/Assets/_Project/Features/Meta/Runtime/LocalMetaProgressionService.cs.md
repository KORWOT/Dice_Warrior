# LocalMetaProgressionService.cs

- 역할: 개발용 메타 권한과 프로필→런→정산 흐름.
- 핵심 동작·입출력: 소유 설정 저장, 검증된 런 생성, 시작 시 영구 성장/정산 정책 복제, 런 ID별 지급 및 요청 fingerprint 중복 방지, 성장 비용 차감을 한 후보에 적용한 뒤 IPlayerDataStore에 확정한다. 진행 중 새 시작은 명시적 포기, 미정산 결과 교체는 거부한다. 구형 저장은 읽기 전용 사본으로 이관하고 지급 0.
- 사용하는 대상·사용하는 쪽·관계 근거: Meta config/content/seed/store를 명시 주입받는다. RunSession/RunUIController가 내부 Checkpoints IRunStore를 사용한다. 값 변경만으로 UI나 scene을 조작하지 않는다.
- 상태·수명·검수 주의: 명령 실패는 Task 실패이며 저장 이전에 원본을 publish하지 않는다. 저장 후 응답 유실은 같은 요청 ID로 재시도한다. LocalDevelopment는 실제 계정 보안 검증이 아니다.
- META-R1 수정: Checkpoints는 새 명령의 sequence가 정확히 +1이고 시간이 감소하지 않는지 검사한다. 같은 sequence이면 시간 및 최종 기록 시간 외 상태가 같아야 한다. 따라서 합법적 autosave/확정 후 동일 명령 재시도는 허용하고 같은 n+1의 상충 세션은 거부한다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

ReadRecoverySnapshot은 존재하는 저장을 한 번 읽고 복사한다. 문서/프로필/체크포인트 충돌을 Stale로 분리하고 다른 프로필은 ForeignProfile, 시작 규칙 불일치는 Rejected로 유지한다. 기존 Checkpoints CAS/동일 sequence 내용 비교/정산 불변은 그대로다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

Replay보다 먼저 요청 세대를 확인한다. 시작 DTO에 현재 프로필 세대를 붙이고 Checkpoints.Save에서 세대를 검사한다. 복원 전 요청이나 런은 같은 revision/sequence로 돌아가도 거부한다. InspectBackup/RestoreBackup은 IPlayerDataRecoveryStore 지원 시만 실행한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.


## RV-03 구현 진행

QueryRequestAsync는 원문의 종류별 business fingerprint를 기존 operations와 대조한 뒤 같은 문서 스냅샷을 반환한다. 복원된 백업에 이미 처리 기록이 있으면 확인하고, 기록이 없고 복구 세대가 다르면 자동 재실행 없이 SupersededByRecovery다. 대기 설정과 journal을 조정 객체에 제공한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.

## RV-04 측정 후 최소 개선

ReadDisplaySnapshot은 Read1회 결과의 독립 사본이다. 기존 Recovery 읽기는 파일 부재를 거부하고, 명령·checkpoint의 최신 읽기/CAS는 그대로 유지한다.
