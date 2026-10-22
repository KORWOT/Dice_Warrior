# AdventurerSelectionStore.cs

- 역할: 경기·서버 프로필과 별개인 다음 모험가 출전 선호를 저장한다. 기본 제공 목록 중 하나의 선택일 뿐 해금 권한이 아니다.
- 핵심 동작: 기본 wanderer/revision0은 파일을 만들지 않는다. 읽기는 16KiB 상한, format/schema/체크섬/ID/revision을 검사한다. Select는 경로별 잠금에서 현재 revision을 비교하고 후보를 임시 파일에 flush한 뒤 원자 교체한다. 기존 파일은 .backup에 보존한다.
- 입출력: 생성자(path,allowedIds), Read(), Select(id,expectedRevision) → AdventurerSelection 사본. 실패하면 예외이며 공개·저장 상태를 부분 갱신하지 않는다.
- 사용하는 대상: LoopJson, Newtonsoft JSON, System.IO, 경로별 ConcurrentDictionary 잠금. Unity/서버 API에 의존하지 않는다.
- 사용하는 쪽·관계 근거: CompetitionApp.OpenAdventurers/ReadAdventurer/SelectAdventurer와 AdventurerSelectionStoreTests. 서버 Competition.Tests는 동일 소스를 링크한다.
- 상태/수명: 계정 경기 디렉터리의 adventurer-selection.json. 앱 재개 시 읽고 새 게임 직전에 재확인한다. 진행 중 참가자 상태에는 소급 적용하지 않는다.
- 검수 주의: 손상·파일 잠금·stale revision·기본 파일 유실과 남은 백업을 자동 초기화하지 않는다. 실패한 선택은 App에서 새 게임을 막지만 기존 경기 재개는 유지한다. 현재 잠금 계약은 단일 Unity 프로세스 내 여러 인스턴스다.
