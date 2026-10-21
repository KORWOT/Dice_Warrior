# LoopSession.cs

- 역할: 유일한 게임 명령/체크포인트/정산 경계.
- 핵심 동작: 최신 파일 읽기 → 같은 요청 영수증 조회 → 입력 토큰 검사 → 깊은 후보 복제 → 규칙 → 후보 검증 → Commit → state 공개.
- 입출력: ILoopStore/Rules 주입, Prepare/Execute/Reload/Snapshot. start/loadout/grow/travel/combat/lap/choose/abandon/settle 명령.
- 사용하는 대상: LoopData, LoopRules, LoopJson, ILoopStore. 사용하는 쪽: SymbolLoopApp, 테스트.
- 관계 근거: App의 화면 버튼은 렌더링 시 Prepare한 명령을 캡처하며 Execute만 호출한다.
- 상태/수명: 세션 state 비공개, Snapshot 복제. 새 런 규칙 복사, 진행 런 규칙 동결. 런당 settled로 정산 1회, 명령 ID fingerprint로 같은 요청의 재시도 1회.
- 검수: 오래된 revision/generation/runId 거절, 기존 ID의 다른 입력 거절, 완료 미정산 런 교체 금지, 영구 성장 새 런부터 반영.
