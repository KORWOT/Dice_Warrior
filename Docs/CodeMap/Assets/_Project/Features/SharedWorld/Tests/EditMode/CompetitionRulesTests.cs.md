# CompetitionRulesTests.cs

- 역할: 새 공동 경기의 의미 있는 도메인 경계 검사. 동일 파일을 Unity EditMode와 .NET에 링크한다.
- 핵심 동작: 4인 공정 시작/개인 컬렉션, 한 인간 사건 이후 AI3, 고정 지리·완주/주사위 선택, 제안 복원·비용 거절, 종료/관전, 개인 난도와 RNG 분리, 64턴 실패, 공동순위, 전투84배열 동치를 확인한다.
- 입출력: Content JSON fixture와 고정 시드 → 순수 경기 상태 및 NUnit assertions.
- 사용하는 대상: 새 도메인 전체, 기존 SymbolCombat/LoopJson, NUnit/System.IO.
- 사용하는 쪽: SharedWorld.EditMode.Tests, Server/DiceWarrior.Competition.Tests.csproj의 링크 Compile.
- 관계 근거: csproj/asmdef와 직접 호출. Definition은 .NET 출력 폴더 또는 Unity 상대 Assets 콘텐츠를 읽는다.
- 상태/수명: 각 검사에서 별도 상태를 생성한다. 사용자 저장/DB/네트워크/Unity Editor 조작 없음.
- 검수: Step/NextCommand는 검사 인간 정책이며 제품 AI가 아니다. 첫 Start red와 Command red 후 구현 확인. C7 대량 밸런스 시뮬레이션이나 서버 권한 정산 검사가 아니다.
