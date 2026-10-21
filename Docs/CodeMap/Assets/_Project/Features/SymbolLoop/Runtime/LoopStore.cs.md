# LoopStore.cs

- 역할: 새 전용 파일 저장, 체크섬, 백업/복구 세대, 미결 입력 기록 및 JSON 복제.
- 핵심 동작: LoopJson은 타입 자동 생성 없는 Newtonsoft 직렬화/SHA256. LoopStore는 후보 검증·현재 revision/generation 대조 후 임시 파일 flush와 교체. 정상 primary를 backup으로 보존.
- 입출력: 생성자 명시 절대화 경로; Read/Commit/InspectBackup/RestoreBackup 및 Read/Write/ClearPending. 실제 앱 경로는 App에서 주입, 테스트는 임시 고유 경로.
- 사용하는 대상: LoopRules.Validate, System.IO/Cryptography, Newtonsoft.Json. 사용하는 쪽: LoopSession, SymbolLoopApp, 두 검증 assembly.
- 관계 근거: ILoopStore가 Session의 생성자 경계. 전역 persistentDataPath·v1 경로 접근 없음.
- 상태/수명: 파일마다 저장 봉투, backup, generation, pending. 복구는 손상 원본을 .damaged-고유ID로 보존하고 세대 증가를 먼저 기록한다. 세대 기록 이후 중단 시 이전 상태로 쓰지 않는다.
- 검수: 실패 전 공개 상태 불변, 손상 자동 초기화 금지, 백업 읽기만으로 복구 금지, 플랫폼별 File.Replace 동작은 장치에서 별도 확인. pending 삭제 실패는 같은 receipt 조회로 재시도 가능. 명시 DiscardPending은 파싱 없이 원문을 고유 사본으로 이동하므로 손상된 입력도 보존/취소할 수 있다.
