# RunUIController.Meta.cs

- 역할: 로비 영구 설정·성장·결과 정산 화면 조정.
- 핵심 동작·입출력: Profile 복사에서 보유 캐릭터/장비3/주사위6/와일드/상한 버튼을 만들고 서비스 명령으로 변경한다. RunMeta가 비동기 Busy와 뷰 수명 버전을 관리한다. 새 출발/성장 요청은 실패 재시도 ID를 유지하며 결과 정산은 runId 기반으로 실행한다.
- 사용하는 대상·사용하는 쪽·관계 근거: RunUIController partial의 Initialize/RenderMenu/StartNewJourney/Result와 직접 연결된다. MenuUIData/IMetaProgressionService/RunSession을 사용한다.
- 상태·수명·검수 주의: UI 표시만으로 지급하지 않는다. 결과 화면의 버튼에서 정산한다. 비활성화 후 늦은 응답은 새 화면을 이동시키지 않는다. 캐릭터 콘텐츠는 현재 1명, 없는 보유품 버튼은 비활성이다.
- META-R2 수정: 메타 명령의 정상/오류 반환 뒤 동일 뷰 수명에서 저장된 런 preview를 다시 읽고 현재 오류를 보존한다. 출발이 확정된 뒤 응답을 잃어도 이어하기/포기 상태가 갱신되어 설정 변경 뒤에도 저장된 출발을 재개할 수 있다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

메타 명령의 일반 규칙 거절과 Stale 등의 복구 오류를 구분한다. 프로필 읽기 실패는 같은 Recovery partial 화면을 사용한다. 실제 비동기 미결 요청 수명 개선(RV-03)은 아직 미구현이며 기존 async finally의 한계를 완료로 주장하지 않는다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

출발/성장/세팅/정산 요청에 표시 프로필의 recoveryGeneration을 전달한다. 새로운 세대는 명시 재로드한 프로필에서만 얻으며 기존 pending 요청 입력을 조용히 바꾸지 않는다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.
