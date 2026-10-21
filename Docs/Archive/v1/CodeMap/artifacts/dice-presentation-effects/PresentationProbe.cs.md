# PresentationProbe.cs

- 역할: Unity CLI run_script로 실행하는 일회 검증 캡처 도구. Assets 밖에 있어 게임 어셈블리/빌드에는 포함되지 않는다.
- 입력/출력: Arm(label)이 실제 격리 Play 상태를 확인하고 artifacts/dice-presentation-effects/<label>에 PNG와 phase/busy/조합/피해 텍스트를 기록한다.
- 동작: 물리 버튼 입력 뒤 DiceRollUI 또는 CombatUI 연출이 시작되면 0.1초 간격, 최대 16장, 90초 이내로 ScreenCapture를 요청한다. 게임 명령이나 RNG는 호출하지 않는다.
- 관계: GameApplication.Current.controller의 UI/Session 읽기, EditorApplication.update 및 ScreenCapture. Unity CLI에서만 호출하며 런타임 코드에서 참조하지 않는다.
- 수명: Arm 재호출 시 같은 클래스의 이전 callback을 해제하고, Play 종료/앱 소멸/기한 종료 때 해제한다. run_script의 개별 로딩 인스턴스도 각 기한이 적용된다.
- 검수: 사용자 저장을 건드리지 않는 PlayWorkbenchSession에서 사용한다. 캡처 시각과 실제 상태는 같은 번호의 txt로 확인한다. 실제 사용자 클릭 증거이며 특정 타이밍을 보장하는 자동 테스트는 아니다.
