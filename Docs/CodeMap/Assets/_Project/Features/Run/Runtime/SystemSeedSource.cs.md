# SystemSeedSource.cs

## RA-A 현재 계약 (2026-09-09)

역할/입출력: OS RandomNumberGenerator로 nonzero uint 시드를 공급한다. 0만 재요청하고 연속 같은 값은 허용한다. 호출마다 난수 공급 자원을 Dispose하며 게임 DiceRules/Unity Random을 소비하지 않는다. RunUIController.Initialize의 일반 기본 공급자이고 이어하기에는 호출되지 않는다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

# SystemSeedSource.cs