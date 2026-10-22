# CompetitionStoreTests.cs

- 역할: SharedWorld 후보 공개·파일 경합·복구 보존의 독립 회귀 검사. Unity와 .NET에서 같은 소스를 컴파일한다.
- 핵심 동작: 실패 주입 저장소로 시작/명령 실패와 같은 ID 재시도를 검증한다. 실제 고유 임시 파일로 두 세션 경합, 백업/손상 원문, 복구 세대, 주 파일 유실, 잘못된 대상/revision을 확인한다.
- 입출력: shared-world-preview-1 콘텐츠, Profile와 고정 seed; NUnit 결과와 격리 임시 디렉터리.
- 사용하는 대상: CompetitionSession/CompetitionStore, 공용 도메인, LoopJson. 사용하는 쪽: SharedWorld.EditMode.Tests 및 DiceWarrior.Competition.Tests.
- 관계 근거: 새 서버 시험 프로젝트가 Runtime/Tests 소스를 링크하며 Unity 테스트 asmdef가 실제 Runtime을 참조한다.
- 상태/수명: 매 검사 고유 임시 폴더; 정리 전 절대 경로가 지정 시험 루트 내부인지 확인한다. 실제 사용자 저장에 접근하지 않는다.
- 검수 주의점: 기능 시험 범위의 저장 경합이며 C6 서버 동시 정산 또는 Android 저장 보장이 아니다. 현재 실제 실행 결과는 작업 REPORT를 따른다.
