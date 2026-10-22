# CompetitionSession.cs

- 역할: 공동 세계 시험 모드의 로컬 경기 명령·후보 공개 경계와 schema1 문서/명령 DTO. 영구 재화 권한을 갖지 않는다.
- 핵심 동작: 시작 및 입력을 디스크 최신 revision/generation과 대조하고 후보를 복제한다. 인간 명령과 도메인이 산출한 AI 차례를 후보에 적용, 순번 로그·ID/fingerprint 영수증을 추가한 뒤 저장 성공 때만 공개한다. 동일 ID/본문은 이미 저장된 결과를 읽고 다른 본문·경기·복구 세대·대상은 거절한다.
- 입출력: ICompetitionStore/CompetitionDefinition 주입, Start/Restart(Profile, seed, matchId, adventurerId), Prepare/Execute, Snapshot, Reload, InspectBackup/RestoreBackup. Snapshot과 시작 입력은 깊은 사본으로 격리한다. Start/Restart는 같은 StartCore에서 최신 revision/generation과 새 경기 ID를 검사한다.
- 사용하는 대상: CompetitionRules/CompetitionCommandRules, CompetitionStore.ValidateDocument, LoopJson, 기존 Profile/Receipt. 사용하는 쪽: CompetitionApp, CompetitionStoreTests 및 화면 검사.
- 관계 근거: App이 명령 실행 전후 Snapshot을 연출에 전달하고, 실제 저장은 ICompetitionStore.Commit으로 위임한다. 새 서버 시작·정산 API를 호출하지 않는다.
- GROWTH_04 직접 관계: App은 별도 모험가 선호에서 읽은 adventurerId를 새 Start/Restart에 전달한다. 세션은 선택 파일을 직접 읽지 않고 기존 입력 계약으로 동결 정의/프로필/모험가 ID를 생성 규칙에 전달한다. 기존 진행의 ID/성장을 바꾸는 명령은 추가하지 않았다.
- 상태/수명: 활성 경기는 Start로 교체할 수 없다. 사용자 확인 후 Restart만 replaceActive=true로 저장을 요청한다. 새 경기는 명령/영수증을 비우고 새 규칙·프로필·시드로 생성하며 revision은 계속 증가한다. 이전 완료/활성 원문 보존은 Store 책임이다. Save 성공 후에만 Snapshot을 교체하고 실패 시 공개 상태를 유지한다. 복구 세대 또는 경기 ID가 바뀌면 기존 입력은 무효다.
- 검수 주의점: 로컬 미서명 시험 기록이며 서버 권한 티켓/검증 영수증이 아니다. state와 commands를 새 경기 C6에서 신뢰하면 안 된다. 저장 실패·경합·스냅샷 외부 수정에서 공개 상태 불변을 검사한다.

## TAG_STAT_ELEMENT_05 직접 변경/관계

세션 코드는 변경하지 않는다. 새 앱 정의가v4여도 Reload는 저장된 v1~v3 state.definition을 그대로 사용한다. 명시Restart만 새v4를 설치하며 2048명령/후보저장/세대/CAS/복구 계약을 v4 저장 검사로 확인한다.
