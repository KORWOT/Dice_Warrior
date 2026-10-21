# MetaRequestCoordinator.cs

- 역할: 화면에 의존하지 않는 메타 요청 상태와 시도 수명을 소유한다.
- 입력/출력: 서비스/영속 journal, 신규 의도 또는 같은 요청 재시도/조회 → Waiting/Unknown/Resolved 및 결과.
- 동작: 발송 전에 journal을 확정하고 사본으로 전달한다. 설정 대기 시간이 지나면 Unknown으로 남기며 취소 성공을 주장하지 않는다. 오래된 시도는 새 시도의 상태를 변경하지 못하고 늦은 fault는 관찰만 한다.
- 관계: IMetaProgressionService의 명령/QueryRequestAsync와 IMetaRequestJournal을 사용한다. RunUIController.MetaRequests가 현재 바인딩 소유권을 따로 검사해 표시/적용한다.
- 수명/검수: UI 참조·전역 이벤트·메인 스레드 Wait/Result·서비스 전체 Task.Run이 없다. Acknowledge는 확인된 동일 요청의 journal 정리가 성공한 뒤만 상태를 비운다. 실제 검증은 RECOVERY_STABILITY_REPORT 참조.

- RS-IR-01: ActiveAttempt는 마지막 발송/조회 시도의 실제 Task를 읽기 전용으로 제공한다. 재개 UI는 동일 Task만 관찰하며 새 발송·ID 변경·대기 시간 재시작을 하지 않는다. 확인 후 완료 때 Task 참조를 비운다.
