# MetaFailure.cs

- 역할: 저장/메타 오류를 복구 종류와 한국어 안내로 변환한다.
- 입력/출력: Exception → MetaFailureKind → 안내 문자열. InvalidFile은 기존 InvalidDataException 안에 형식/프로필 구분 코드를 보존한다.
- 핵심 동작: 문구를 비교하지 않는다. 일반 규칙 거절, stale, 임시 I/O, 손상, 미지원 버전, 타 프로필을 분리한다.
- 관계: PlayerSaveDocument가 파일 오류를 만들고 RunUIController/Recovery가 분류와 표시를 사용한다. Core의 MetaFailureKind/MetaCommandException에 의존한다.
- 수명: 무상태 정적 변환이며 파일·세션을 수정하지 않는다.
- 검수: 체크섬은 보안 증명이 아니다. 분류/실제 UI 검증은 RECOVERY_STABILITY_REPORT에 기록한다.
