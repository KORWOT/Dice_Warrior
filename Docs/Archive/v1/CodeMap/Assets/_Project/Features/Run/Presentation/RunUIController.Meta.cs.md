# RunUIController.Meta.cs

- 역할: 로비 영구 설정·성장·결과 정산 화면 조정.
- 핵심 동작·입출력: Profile 복사에서 보유 캐릭터/장비3/주사위6/와일드/상한 버튼을 만든다. 고정 ID·버전·복구 세대와 의도를 MetaRequests partial에 전달하고, 요청 기록·대기·재시도·바인딩 수명은 해당 partial과 MetaRequestCoordinator가 관리한다. 정산 ID는 runId 기반이다.
- 사용하는 대상·사용하는 쪽·관계 근거: RunUIController partial의 Initialize/RenderMenu/StartNewJourney/Result와 직접 연결된다. MenuUIData/IMetaProgressionService/RunSession을 사용한다.
- 상태·수명·검수 주의: UI 표시만으로 지급하지 않는다. 결과 화면의 버튼에서 정산한다. 미결 요청 중 관련 변경은 비활성이고 요청 결과 확인 버튼은 사용 가능하다. 캐릭터 콘텐츠는 현재 1명, 없는 보유품 버튼은 비활성이다.
- 결과 조정: 출발 응답 유실 시 관련 변경을 차단하고 미결 요청 조회로 확정 상태를 확인한다. 이후 설정을 바꿔도 저장된 출발의 원래 설정과 runId로 재개한다. 자동 preview 재조회 방식은 RV-03에서 명시 결과 확인으로 교체했다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md 및 RECOVERY_STABILITY_PLAN.md. 최신 검증 상태는 Docs/Reports/RECOVERY_STABILITY_REPORT.md의 실제 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

메타 명령의 일반 규칙 거절과 Stale 등의 복구 오류를 구분한다. 프로필 읽기 실패는 같은 Recovery partial 화면을 사용한다. 당시 남겨 둔 비동기 수명은 아래 RV-03에서 MetaRequests partial로 교체했다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

출발/성장/세팅/정산 요청에 표시 프로필의 recoveryGeneration을 전달한다. 새 요청은 현재 프로필 세대를 사용하며 기존 pending 요청의 세대나 입력은 바꾸지 않는다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.


## RV-03 구현 진행

기존 async RunMeta와 화면 소유 pendingStart/Growth를 제거하고 MetaRequests partial에 고정 의도를 전달한다. 미결 요청 중에는 새 변경 대신 처리 결과 확인 버튼을 표시한다. 로비 표시/경제 계산 수치는 유지한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.

## RV-04 측정 후 최소 개선

RenderMetaMenu는 ReadDisplaySnapshot1회로 프로필/정산 여부/런 preview를 함께 표시한다. 표시 사본을 명령 권위로 쓰지 않으며 명령은 expectedRevision과 새 권위 읽기로 검증한다.
