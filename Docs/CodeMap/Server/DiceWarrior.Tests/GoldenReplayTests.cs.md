# GoldenReplayTests.cs

- 역할: 서버 공유 Domain과 기존 Unity 골든의 전 단계 동일성 및 JSON 요청 지문 계약 검사.
- 핵심 동작: 읽기 전용으로 링크된 ReplayGoldenVectors.json의 승리/패배/지역 후 포기/상점·장비·재개의 4벡터를 읽는다. 초기 및 각 명령 뒤 기대 필드 전체를 비교하고 독립 검증기의 최종 결과/지급액을 비교한다. 숫자 enum·null·필드 순서를 포함하는 고정 JSON은 en-US/ko-KR/tr-TR/fr-FR 문화권에서 같아야 한다.
- 입출력: 기존 고정 121명령/125상태, 명시적 기대 JSON. 골든을 생성하거나 쓰지 않는다.
- 사용하는 대상: LoopJson, LoopRules, RunCommandRules, MockRunVerifier, NUnit, Newtonsoft JToken. 사용하는 쪽: dotnet test 및 SqliteAuthorityTests의 고정 완주 명령 입력.
- 관계 근거: Shared 프로젝트는 Assets/Domain의 동일 C#을 링크 컴파일한다. 본 검사는 공유 소스가 같은다는 선언 대신 실제 명령/상태와 직렬화 결과를 검사한다.
- 상태/수명: 호출마다 파일을 새 DTO로 읽고 복사한 Rules/Profile을 사용한다. 문화권 변경은 finally에서 복원한다.
- 검수 주의: 기존 골든의 검토된 기대값을 유지하며 서버 변경에 맞춰 자동 재생성하지 않는다. 장치 IL2CPP와 Firebase 언어 포팅은 검증 범위 밖이다.
