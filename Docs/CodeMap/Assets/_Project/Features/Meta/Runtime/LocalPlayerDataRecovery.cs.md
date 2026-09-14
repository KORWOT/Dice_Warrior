# LocalPlayerDataRecovery.cs

- 역할: LocalPlayerDataStore의 정상 백업·원자 파일 쓰기 부분을 분리한다.
- 입력/출력: 검증 완료된 이전 문서 bytes와 새 후보 → 마지막 정상본 player.json.backup 및 현재본의 원자 교체.
- 핵심 동작: 주 저장 잠금 아래 이전 bytes를 검증한 뒤 백업에 그대로 기록한다. 각 파일은 고유 임시 파일 flush 후 replace/move하며 자체 임시 파일만 정리한다. 백업 쓰기 실패는 현재본 쓰기를 막는다.
- 관계: PlayerSaveDocument.cs의 ReadBytes/Write가 호출한다. MetaJson/LocalSaveEnvelope를 사용하며 UI나 지급 규칙에 의존하지 않는다.
- 수명/검수: 정상 백업은 하나만 보관한다. 현재 손상 때문에 마지막 정상본을 덮어쓰지 않는다. 확인 후 복원/세대 차단의 RV-02 직접 검증은 통과했으며, 전체 안정화 Task는 진행 중이다. REPORT의 실제 증거를 따른다.

- 복원 API: ProfileBackupPreview에 검증된 bytes fingerprint/보관 시각/프로필 revision/해결 사건을 읽기 전용으로 제공한다. 확인 이후 RestoreBackup은 동일 .write-lock 안에서 백업을 재검증하고 fingerprint를 비교한다. 현재본이 정상으로 바뀌었으면 거부하며 손상 원본을 고유 파일에 CreateNew/flush한 뒤에만 현재본을 원자 교체한다. backup bytes는 보존한다.
- 전체 문서/소유권: 프로필·런·시작 기록·명령/정산 영수증을 같은 시점으로 복원하고 프로필/런 recoveryGeneration만 새 값으로 바꾼다. 기존 revision은 복원 시점 그대로여도 Write/서비스 요청/RunSession의 세대 검사가 ABA를 차단한다. 치트 방지나 백업 롤백 보안으로 주장하지 않는다.
