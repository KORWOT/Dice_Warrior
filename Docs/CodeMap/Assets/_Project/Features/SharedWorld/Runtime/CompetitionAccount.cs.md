# CompetitionAccount.cs

- 역할: 기존 계정 checkpoint/권한 요청과 공동 모험의 독립 출전 선호 설정을 연결한다. 기존 1인 모험의 시작·포기·정산을 새로 발행하지 않는다.
- 핵심 동작: 기존 LoopSession/LoopStore/GameBackendCoordinator로 계정 조회·성장·기존 미결 요청 재확인을 수행한다. 별도 LoopStore의 lobby-loadout.json에는 출전 주사위/무기만 적용하고 revision/generation CAS로 단일 commit 후 재로드한다. 기존 active run의 RequireLobby 제한과 계정 저장에 영향을 주지 않는다.
- 입출력: 생성자(account checkpoint 경로, preference 경로, Rules, IGameBackend). Session/Coordinator/Pending/StorageError/LoadoutError/PendingError/Busy/HasLegacyRun/Ready/HasProfile/LastReply/Profile, Reload/RefreshAsync/SelectLoadout/GrowAsync/RetryPendingAsync/DiscardLocalPendingAsync/InspectBackup/RestoreBackupAsync를 제공한다. Profile은 계정 사본에 출전 사본을 덮어쓴 값이다.
- 사용하는 대상: SymbolLoop의 LoopSession/LoopStore/GameBackendCoordinator/Rules/Backend 계약 및 LoopJson. 계정 원본·백업·pending·receipt 의미는 기존 저장 계층에 맡긴다.
- 사용하는 쪽·관계 근거: CompetitionApp.OpenAccount가 생성하고 로비/계정 관리에서 호출한다. CompetitionFlowTests는 메모리 IGameBackend와 고유 임시 파일로 같은 경계를 검증한다. .NET 순수 경쟁 검사는 이 Unity 계정 연동 파일을 포함하지 않는다.
- 상태/수명: account/pending/loadout 읽기 오류를 구분하고 자동 초기화하지 않는다. 기존 미결이 있으면 자동 재전송/프로필 갱신을 하지 않는다. 조회/성장/재확인 finally에서 pending을 다시 읽으므로 서버 승인 뒤 로컬 저장 실패에도 같은 요청을 다시 확인할 수 있다. CancellationToken과 backend 수명은 App 소유다.
- 보호·실패: active run이면 성장 구매를 거절한다. 별도 출전 파일의 run/authority/ticket은 오류로 보존한다. 새 출전값이 없어도 기존 계정 출전값을 사용하며 최초 선택 시 나머지 값을 함께 보존한다. 기존 backend pending은 원 ID/body로만 제출하고 로컬 pending 보존 취소는 명시 요청과 expected ID로만 수행한다. 백업 복원도 기존 원문/pending 보존 정책을 유지한다.
- 검수 주의: Ready는 저장/출전/미결 오류 없음, pending 없음, 서버 프로필 확인 성공을 모두 요구한다. 기존 active run 보존·응답 유실·계정 파일 잠금·손상 fixture의 실제 Unity 실행 결과는 REPORT로 확인하며 코드 존재를 PASS로 간주하지 않는다.
