# PlayerProfileData.cs

- 역할: 플레이어 영구 원본과 출전·시작 스냅샷의 순수 값 계약.
- 핵심 동작·입출력: OwnedItem은 instanceId와 definitionId를 구분한다. PlayerProfileData.Copy/RunLoadout.Copy/RunStartSnapshot.Copy는 중첩 값을 분리한다. ProfileRules는 식별자·보유·중복 개체·장비 슬롯·주사위 6개·콘텐츠 지원을 검증한다. MetaPolicy는 성장 효과/비용과 정산 정책이다.
- 사용하는 대상·사용하는 쪽·관계 근거: MetaProgressionConfig가 초기 원본을 작성하고 LocalMetaProgressionService/PlayerSaveDocument와 UI가 사용한다. Core asmref 아래이므로 Unity/Firebase 의존성 없음.
- 상태·수명·검수 주의: 현재 authority는 LocalDevelopment만 허용한다. 실제 Firebase authority를 이 DTO 값만 바꿔 인증했다고 간주하면 안 된다. 현재 콘텐츠의 지원 캐릭터는 공용 규칙 한 명이다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

MetaFailureKind와 MetaCommandException.Kind를 추가했다. ProfileRules.Require는 기본 Rejected를 유지하고 명시한 저장/프로필 충돌만 Stale이다. 런타임 MetaFailure와 Controller가 코드로 분기하며 게임 수치/소유 규칙은 유지한다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

프로필 recoveryGeneration(기존 빈 초기 세대)을 복사·검증한다. 영구 재화나 성장 수치에 영향을 주지 않으며 명시 복원 시에만 새로운 GUID로 바뀐다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.
