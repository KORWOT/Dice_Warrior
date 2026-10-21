# RunApplication.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 순수 코어의 상태 소유 및 직렬 명령 처리.
- 입출력/핵심 동작: RA-B의 모든 bool 명령과 시간 누적을 그대로 소유한다. 입력/반환/callback은 명시 깊은 복사. token/phase/안정 ID/자원 검사→후보 생성→기존 규칙 처리→순수 검증→checkpoint 성공→상태 교체. 실패 시 RNG/카드/HP/자원/처리 ID와 pending 시간 유지.
- 직접 사용하는 대상: CoreRunState/RunStateCopy/RulesCopy, RunStateValidator/RunCommandToken, Dice/Fate/Combat/Growth/Exploration 규칙.
- 직접 사용하는 쪽: Runtime RunSession facade와 순수 코어 테스트.
- 상태/수명·검수 주의: FateDice.Core Application. 저장은 Action<CoreRunState> callback이며 파일/Unity/표시/Runtime 참조 없음. GUID 생성은 runId에만 쓰며 게임 RNG에 쓰지 않는다. 새 런/Result 기록·dirty 시간 flush·재진입 거절은 B와 동일.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.

- RA-D: ChooseFate의 상점 입장에 ShopRules.Enter, Buy는 고정 offer 가격으로 결제, LeaveShop은 목록을 해제한다. 생성자는 검증 후 private 복사에만 구형 상점 snapshot을 복원한다. 후보 저장 성공 후 확정 경계는 동일하다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
