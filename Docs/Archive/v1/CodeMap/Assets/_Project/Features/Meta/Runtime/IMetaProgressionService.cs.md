# IMetaProgressionService.cs

- 역할: 미래 서버 어댑터를 위한 비동기 메타 명령 계약.
- 핵심 동작·입출력: LoadoutRequest/StartRunRequest/SettlementRequest/GrowthRequest는 requestId·expectedRevision과 선택 의도만 전달한다. 정산량·비용·잔액·인증 UID를 요청에서 지정하지 않는다. Profile은 이미 로드된 읽기용 복사, RunStore는 런 체크포인트다.
- 사용하는 대상·사용하는 쪽·관계 근거: GameApplication이 구현을 주입하고 RunUIController.Meta가 명령을 소비한다. 현재 구현은 LocalMetaProgressionService.
- 상태·수명·검수 주의: Firebase 어댑터는 Auth UID와 서버 승인 상태를 공급해야 한다. 로컬 저장소 전체 JSON 업로드를 영구 잔액 확정 API로 쓰지 않는다. 실제 네트워크/Auth/Rules는 이번 범위 밖이다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

ReadRecoverySnapshot은 같은 시점의 프로필·런·정산을 복사 반환한다. LocalMetaProgressionService 구현을 RunUIController.Recovery가 후보 준비에 사용한다. 새 프로필 생성이나 재정산을 하지 않으며 읽기 실패를 전달한다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

MetaRequest.expectedRecoveryGeneration을 불변 재시도 입력에 추가한다. InspectBackup/RestoreBackup은 검증된 미리보기와 확인 이후 복원을 연결하며 실제로 LocalPlayerDataStore의 복구 기능에 위임한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.


## RV-03 구현 진행

RequestJournal/ResponseWaitSeconds/QueryRequestAsync를 추가한다. 결과 조회는 같은 원문 요청과 기존 영수증 fingerprint를 비교한 확정/미확정/거절/복구 이전 결과를 반환하는 권위 경계다. 실제 서버 구현은 범위 밖이다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.

## RV-04 측정 후 최소 개선

ReadDisplaySnapshot은 신규 프로필도 파일을 만들지 않는 읽기 전용 표시 사본이다. 로비는 한 문서의 profile/run/settlement를 같이 표시하며 명령과 ReadRecoverySnapshot의 권위 확인을 생략하지 않는다.
