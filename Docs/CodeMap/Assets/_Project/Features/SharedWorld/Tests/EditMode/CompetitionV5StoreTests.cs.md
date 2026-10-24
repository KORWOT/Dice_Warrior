# CompetitionV5StoreTests.cs

- 역할: v5 선택 도중 실제 파일 저장·재개·원자성·중복 영수증·구형 경기 보존과 용량/명령 상한 검사.
- 핵심 동작: 굴림/보관/해제/리롤 후 파일 재개, 저장 전 실패, 실제 커밋 후 응답유실, 동일 ID 재시도와 다른 본문 충돌, 복구 세대 이전 입력 거절을 검사한다. v4 전투의 신규 앱 재개·기존 combat 및 명시 v5 Restart의 원문 사본/백업복원을 확인한다.
- 입출력: TempPath/DiceWarriorV5Tests/고유GUID/checkpoint.json, 실제 CompetitionStore/Session. FaultStore는 실제 파일 저장 직전·직후에만 IOException을 주입한다. 완주 경기와 2048명령 fixture의 실제 UTF-8 envelope 크기를 출력한다.
- 사용하는 대상: CompetitionV5Tests의 정본/전투/명령 fixture, CompetitionV4Tests.Definition, CompetitionSession/Store, LoopJson, System.IO. 보존 대상은 검사 자체가 만든 임시 파일뿐이다.
- 사용하는 쪽: Unity SharedWorld.EditMode.Tests 및 .NET DiceWarrior.Competition.Tests glob.
- 관계 근거: 공개 Prepare/Execute/Reload/Restart/RestoreBackup과 실제 primary/backup 원문 비교를 사용한다. 전체 경기는 저장된 인간 명령과 동일 입력을 독립 상태에 재적용하여 AI 포함 상태 해시를 매 단계 대조한다.
- 상태/수명: 매 검사 고유 경로를 생성하고 TearDown에서 절대 경로가 지정 임시 루트 아래인지 확인한 뒤 해당 디렉터리만 정리한다. 사용자 저장/실 DB를 읽거나 수정하지 않는다.
- 검수 주의점: 2045개 placeholder 명령은 형식·용량 경계용이며 그 과거 이력의 의미상 replay 권위를 주장하지 않는다. 실제 마지막 굴림/리롤/실행은 Session과 파일을 통해 처리한다. state.actionNumber와 기록 수 불일치·4MiB 초과는 공개 상태 및 원문 보존과 함께 거절되어야 한다. 플랫폼/Android 검증은 이 검사만으로 PASS가 아니다.
