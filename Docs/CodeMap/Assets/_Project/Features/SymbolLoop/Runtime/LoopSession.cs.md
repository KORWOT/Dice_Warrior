# LoopSession.cs

- P3 검토 보완: RestoreActiveRun은 다른 티켓을 복원할 때도 디스크의 최신 authority revision보다 과거인 응답을 거절한다. 늦은 첫 런 응답이 두 번째 완료 런을 덮는 회귀를 BackendSettlementSafetyTests로 검증한다.

- 역할: 로컬 런 명령과 체크포인트 공개 경계. 영구 재화의 직접 지급/차감은 제거했다.
- 핵심 동작: 최신 파일/영수증/토큰 → 후보 복제 → 로컬 규칙 → 명령 로그 → 검증/Commit → 공개. Prepare는 백엔드 요청을 고정하며 ApplyBackend는 확정 응답만 반영한다.
- 입출력: ILoopStore/Rules, Prepare/Execute/ValidateBackendCommand/RefreshProfile/RestoreActiveRun/ApplyBackend/Reload/Snapshot.
- 사용하는 대상: RunCommandRules/LoopRules/LoopJson/BackendContracts/ILoopStore. 사용하는 쪽: App과 Coordinator, 두 검사 assembly.
- 관계 근거: App은 Coordinator.ExecuteAsync를 호출하고 Coordinator가 Session의 로컬 실행/승인 적용을 구분한다.
- 상태/수명: start는 서버 티켓의 시드/규칙/성장/출전으로 생성한다. settle은 서버 영수증의 지급액/상태와 프로필을 반영한다. 구형 pending은 지급 0으로 유지한다. grow는 서버 프로필을 반영한다. 과거 응답으로 더 최신 캐시 revision을 낮추지 않는다.
- 검수: 로컬 Execute(start/grow/settle)는 거절. 승인 후 저장 실패는 같은 ID 응답 재적용. 기존 성장은 시작 시 동결되고 진행 런을 소급 변경하지 않는다.

- RestoreActiveRun은 백업에 없는 승인 티켓을 시작 지점으로 복구한다. 같은 ID의 진행 런은 초기화하지 않고 프로필만 갱신한다. 서로 다른 미완료 런 충돌은 보존 후 거절한다.
- P2: 후보의 런 명령을 RunCommandRules로 실행하고 저장 성공 때만 로그를 공개한다. Target은 검증기와 같은 불변 문화권 형식을 사용한다. 승인 결과 요약은 App이 영수증에서 표시하며 로컬 최종 상태를 지급 근거로 보내지 않는다.
- P3: ReconcileSubmission은 조회한 서버 정산과 같은 로컬 runId를 대조하고, Approved의 티켓/로그로 종료 상태를 다시 만들어 원 영수증을 연결한다. 완료 전 Map/Combat 백업도 복구할 수 있다. P1 pending은 재현하지 않고 0지급 종료를 유지한다. ApplyBackend도 같은 정산 적용 경계를 사용한다. 일반 P2 영수증만 있는 응답은 로컬 Result일 때 계속 읽는다.
- 관계/주의: 교체 전 원문 보존과 최신 권한 재조회는 Coordinator가 담당한다. Session은 지급을 다시 실행하지 않는다. Save는 기존 revision/generation CAS를 유지하며 경합 후 실패한 후보를 공개하지 않는다.
