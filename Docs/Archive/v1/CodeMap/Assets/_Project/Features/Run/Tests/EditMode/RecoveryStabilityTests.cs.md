# RecoveryStabilityTests.cs

- 역할: 격리 저장의 복구 오류 분류와 읽기 불변 검증.
- 입력/출력: 정상 실제 시작 기록을 만든 뒤 TEMP 파일의 envelope/schema/프로필/권한/영수증/해시를 통제한다. 분류 코드·원본 bytes·복사 독립성 assertion을 출력한다.
- 핵심 동작: 미지원 형식/타 프로필/손상/임시 I/O/규칙 거절/stale의 구분, 파일 소실 때 자동 초기화 금지, 같은 시점 복사 읽기의 무변경을 검사한다.
- 관계: PrototypeAuthoring의 시험 기본값, MetaProgressionConfig, LocalMetaProgressionService, LocalPlayerDataStore, MetaFailure를 사용한다.
- 수명: 시험마다 GUID TEMP 하위 디렉터리와 SO를 만들고 검증된 TEMP 하위만 제거한다. 실제 사용자 저장을 사용하지 않는다.
- 검수: category RecoveryStability의 실제 실행 증거는 RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

정상 백업 bytes, 검증 실패, 미리보기 무변경, 전체 문서 복원, 원본 보관, 같은 revision/sequence ABA에서도 이전 세션/요청/후보 쓰기 거부를 검사한다. 공유 파일 핸들/아카이브 경로 충돌로 실제 파일 I/O 실패를 주입한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.
