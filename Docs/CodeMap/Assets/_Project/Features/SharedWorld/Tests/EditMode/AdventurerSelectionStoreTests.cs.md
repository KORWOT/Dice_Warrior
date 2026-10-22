# AdventurerSelectionStoreTests.cs

- 역할: 별도 출전 모험가 선호 저장의 내구성과 원문 보존 검증이다.
- 핵심 동작: 기본값/쓰기 없는 읽기, 새 인스턴스 재개, revision 경합·잘못된 ID 거절, 교체 백업, 파일 잠금 실패, 손상 원문·주 파일 유실·크기 상한을 검사한다.
- 입출력: 고유 임시 폴더의 격리 파일 → NUnit 단언. 사용자 저장·계정/DB에 접근하지 않는다.
- 사용하는 대상: AdventurerSelectionStore, System.IO, NUnit. 실제 파일 bytes와 revision을 비교한다.
- 사용하는 쪽·관계 근거: SharedWorld.EditModeTests 및 DiceWarrior.Competition.Tests의 동일 소스 링크.
- 상태/수명: 테스트마다 고유 임시 디렉터리를 만들고 허용 루트 확인 후 자기 폴더만 정리한다.
- 검수 주의: 실패 저장이 원문을 보존하는지와 백업이 이전 유효 선택인지 확인한다. UI 손상 선택 후 경기 재개는 CompetitionFlowTests에서 별도로 검증한다.
