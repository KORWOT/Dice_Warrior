# LocalMetaRequestJournal.cs

- 역할: 프로필마다 미결 요청1개를 player.json.pending에 보존한다.
- 입력/출력: 검증한 MetaPendingRequest → envelope/checksum 원자 생성. Read는 파일이 없으면 생성하지 않는다.
- 동작: 기존 profile.write-lock을 공유하며 Begin은 같은 fingerprint 재시도만 허용한다. Clear는 동일 요청인지 확인한 뒤 해당 기록만 제거한다. 실패 시 현재 프로필/백업을 건드리지 않는다.
- 관계: LocalPlayerDataStore.RequestJournal이 제공하고 MetaRequestCoordinator가 요청 전에 쓰고 확인 후 정리한다. 기존 MetaJson을 사용한다.
- 수명/검수: 앱 종료·응답 유실·프로필 백업 복원에도 미결 원문을 남긴다. 테스트는 TEMP 전용이며 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.
