# CompetitionSession.cs

- 역할: 공동 세계 시험 모드의 로컬 경기 명령·후보 공개 경계와 schema1 문서/명령 DTO. 영구 재화 권한을 갖지 않는다.
- 핵심 동작: 시작 및 입력을 디스크 최신 revision/generation과 대조하고 후보를 복제한다. 인간 명령과 도메인이 산출한 AI 차례를 후보에 적용, 순번 로그·ID/fingerprint 영수증을 추가한 뒤 저장 성공 때만 공개한다. 동일 ID/본문은 이미 저장된 결과를 읽고 다른 본문·경기·복구 세대·대상은 거절한다.
- 입출력: ICompetitionStore/CompetitionDefinition 주입, Start(Profile, seed, matchId, adventurerId), Prepare/Execute, Snapshot, Reload, InspectBackup/RestoreBackup. Snapshot과 시작 입력은 깊은 사본으로 격리한다.
- 사용하는 대상: CompetitionRules/CompetitionCommandRules, CompetitionStore.ValidateDocument, LoopJson, 기존 Profile/Receipt. 사용하는 쪽: CompetitionApp, CompetitionStoreTests 및 화면 검사.
- 관계 근거: App이 명령 실행 전후 Snapshot을 연출에 전달하고, 실제 저장은 ICompetitionStore.Commit으로 위임한다. 새 서버 시작·정산 API를 호출하지 않는다.
- 상태/수명: 활성 경기는 Start로 교체할 수 없다. 종료 경기 다음 Start는 로그/명령 영수증을 초기화하고 revision을 계속 증가한다. 이전 종료 파일 보존은 Store 책임이다. 복구 세대가 바뀌면 기존 입력은 무효다.
- 검수 주의점: 로컬 미서명 시험 기록이며 서버 권한 티켓/검증 영수증이 아니다. state와 commands를 새 경기 C6에서 신뢰하면 안 된다. 저장 실패·경합·스냅샷 외부 수정에서 공개 상태 불변을 검사한다.
