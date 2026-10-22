# CompetitionPartialComboTests.cs

- 역할: shared-world-preview-2의 부분 기호 조합과 구형 전투/저장 호환 검증. Unity/.NET에서 동일 소스 실행.
- 핵심 동작: 실제 CompetitionCommandRules combat 경로의 지팡이·지팡이·검→마력탄·검기를 재현한다. 화염탄/수호베기·반복눈·3눈정확우선·후보경합·각눈1회·기본기순서를 명시 기대값으로 검사한다.
- 입출력: 고정 주사위면/전투 능력/경기 버전 → 기술ID·피해·보호·HP·반격·전체 저장 상태 비교.
- 사용하는 대상: CompetitionRulesTests.Definition/NextCommand, CompetitionRules/CommandRules/Content, SymbolCombat, LoopJson, NUnit.
- 사용하는 쪽: SharedWorld.EditMode.Tests 및 .NET Competition.Tests의 기존 wildcard 소스 링크.
- 관계 근거: 실제 게임 명령→EncounterRules→버전 어댑터 경로 및 직접 전투 API 호출.
- 상태/수명: 격리 후보만 사용한다. 사용자 저장/DB/서버/Unity 객체를 변경하지 않는다. v2 인간 명령을 그대로 두 상태에 재생하며 중간 직렬화복원 후 AI 포함 전체 상태 동치를 확인한다.
- 검수: 최초3검사에서 사용자예시·신규버전2건 의도 실패와 구형1건 통과를 확인했다. 기존2인자 SymbolCombat API 존재/정확전체, v1 저장 재현, 알 수 없는 버전 거절을 유지한다. 장비 태그/보호막/반격1회/기본기 처치 뒤 조합생략을 수치로 검사한다. 새 규칙 기대를 기존 골든 결과에 덮어쓰지 않는다.
