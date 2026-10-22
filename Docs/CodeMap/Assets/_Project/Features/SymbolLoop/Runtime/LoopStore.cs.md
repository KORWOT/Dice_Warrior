# LoopStore.cs

- 역할: 로컬 체크포인트의 체크섬·revision/generation 비교 저장, 백업 복원, 미결 입력과 복구/오류 원문 보존. 직렬화·복제·SHA256은 Domain/LoopJson.cs의 공유 계약을 사용한다.
- 핵심 동작: 같은 절대 경로의 프로세스 내 Gate로 Read/Commit/InspectBackup/RestoreBackup, Read/Write/Clear/DiscardPending, PreserveCheckpoint/RecordBackendFailure를 직렬화한다. Commit은 후보 검증 후 현재 revision/generation을 대조하고 고유 임시 파일 Flush(true)→원자 교체한다. 경합 패자는 예외를 받고 다시 읽은 상태로 새 입력을 만든다.
- 입출력: 생성자에 명시 파일 경로를 주입한다. ILoopStore는 Session의 Read/Commit 경계다. 실제 계정/출전 선택 경로는 CompetitionApp/CompetitionAccount가 분리하여 주입하고 검사는 고유 임시 경로를 쓴다. 전역 persistentDataPath·v1 저장에 직접 접근하지 않는다.
- 미결 계약: WritePending은 기존 본문과 같은 입력만 멱등 허용한다. ClearPending(id)와 DiscardPending(expectedId)는 ID 비교부터 삭제/보존 이동까지 한 잠금에서 수행하여 늦은 응답이 새로운 입력을 지우지 못한다. 명시 DiscardPending(null)은 파싱 없이 raw를 `.pending-discarded-<id>`로 옮기므로 손상 입력도 보존 취소한다. 자동 정리는 반드시 예상 ID를 전달한다.
- 보존 계약: PreserveCheckpoint는 primary가 없으면 null, 있으면 검증/재직렬화 없이 동일 byte의 `.reconciled-<id>.json` 사본 경로를 반환한다. 사본은 고유 임시 파일 flush 후 rename하므로 성공 반환 전 보존을 끝낸다. RecordBackendFailure(command,reply,diagnostic)는 요청·응답·예외 진단 원문을 고유 `.backend-error-<id>.json`에 flush하여 기록하며 pending을 지우지 않는다.
- 상태/수명: 복원은 기존 primary의 `.damaged-<id>` 원문 사본을 남기고 최대 복구 세대+1을 먼저 고유 임시 generation 파일에 flush/교체한 뒤 복원 primary를 쓴다. revision이 우연히 같아도 이전 generation 입력을 거절한다. marker 교체 이후 중단되면 과거 primary의 읽기/쓰기를 차단한다. backup 읽기만으로는 복원을 실행하지 않는다.
- 사용하는 대상: LoopRules.Validate, LoopJson, System.IO, ConcurrentDictionary. 사용하는 쪽: LoopSession, GameBackendCoordinator, CompetitionAccount, EditMode/PlayMode 검증과 서버 HTTP 복구 시험의 링크 컴파일. Coordinator가 서버 영수증·프로필로 대체하기 전 PreserveCheckpoint를 호출하고 실제 오류 정보를 RecordBackendFailure에 넘긴다.
- 관계 근거: ILoopStore 생성자 주입과 Coordinator의 복구/미결 요청 호출이 저장 경계다. schema2 파일은 권한 데이터가 아니며 서버 최신 상태로 다시 확인한다.
- 검수: 같은 프로세스의 동일 절대 경로만 보호한다. 다중 프로세스가 직접 같은 파일을 공유하는 파일 DB는 지원하지 않으며 Firebase 트랜잭션을 대신하지 않는다. 실패 시 공개 상태를 바꾸지 않고 손상을 자동 초기화하지 않는다. 플랫폼별 File.Replace·flush의 장치 동작은 별도 검증 대상이다. LoopStoreConcurrencyTests가 실제 Task/Barrier 경합과 원문 보존을 검사한다.
