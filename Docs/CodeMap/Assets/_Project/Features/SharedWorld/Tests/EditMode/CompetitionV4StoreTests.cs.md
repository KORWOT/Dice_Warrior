# CompetitionV4StoreTests.cs

- 역할: v4 앱에서 구버전 저장 재개·명시교체·백업복원, 저장 실패/상한 검사.
- 핵심 동작: v1/v2/v3原文 보존·v4 명시Restart, 실제 전체경기 파일저장/재개/UTF8크기, 파일잠금/4MiB초과/중복재시도/2048명령차단을 검사한다.
- 입출력: 고유 임시 checkpoint 파일, 실제 CompetitionStore/Session, NUnit6사례.
- 사용하는 대상: CompetitionV4Tests/RulesTests fixture, System.IO, 최소 SnapshotStore 용량 경계 주입.
- 사용하는 쪽: Unity EditMode 및 .NET Competition.Tests.
- 상태/수명: 임시 루트 아래 고유GUID 디렉터리만 생성·범위확인 후 정리. 사용자 데이터 미사용.
- 관계 근거: 각 테스트가 Store/Session 공개 메서드를 사용하며 경계 fake는 쓰기 도달 여부만 관찰한다.
- 검수 주의: 2048fixture는 형식상 유효한 용량 시험이며 C6 명령 검증 성공을 주장하지 않는다. 크기는 실제 envelope 바이트를 측정한다.
