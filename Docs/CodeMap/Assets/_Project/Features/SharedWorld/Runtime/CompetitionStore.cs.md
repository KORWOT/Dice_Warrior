# CompetitionStore.cs

- 역할: 공동 세계 시험 모드의 별도 체크포인트와 백업·복구 세대를 보관하는 파일 저장소. 기존 schema2/LoopStore 파일을 읽거나 이관하지 않는다.
- 핵심 동작: 절대 경로별 프로세스 내 잠금, revision/generation CAS, 체크섬·4MiB 상한·문서/명령 검증, 고유 임시 파일 Flush(true) 후 원자 교체. 정상 이전 파일은 backup으로 남긴다.
- 입출력: 명시 path 생성자, ICompetitionStore.Read/Commit/InspectBackup/RestoreBackup. UI 경로는 CompetitionApp이 endpoint+계정에 맞춰 주입하고 검사는 고유 임시 경로를 사용한다.
- 복구: 원문을 .damaged-*로 보관하고 최신 세대+1 marker부터 저장한 뒤 backup 상태를 복원한다. marker 이후 중단되면 과거 primary 사용을 차단한다. 주 파일이 없고 백업/marker가 있으면 자동 새 경기 초기화를 하지 않는다.
- 사용하는 대상: CompetitionRules.Validate, LoopJson, System.IO/ConcurrentDictionary. 사용하는 쪽: CompetitionSession/CompetitionApp 및 CompetitionStoreTests.
- 관계 근거: Session의 후보 공개 전 Commit과 백업 복구 호출. 완료 경기 교체 시 .completed-*로 원본을 보존한다.
- 상태/수명: 프로세스 내 동일 절대 경로의 모든 인스턴스가 잠금을 공유한다. 파일 백업과 완료 기록은 사용자 정리 전 유지한다. 원본 실제 사용자 저장은 시험 데이터로 쓰지 않는다.
- 검수 주의점: 다중 프로세스 공유 DB나 서버 권한 저장소가 아니다. 체크섬은 실수·손상 탐지이며 위변조 인증이 아니다. 플랫폼 File.Replace/flush 및 Android는 별도 실제 검증이 필요하다.
