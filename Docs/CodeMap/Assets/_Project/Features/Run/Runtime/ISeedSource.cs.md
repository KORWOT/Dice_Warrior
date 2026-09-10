# ISeedSource.cs

## RA-A 현재 계약 (2026-09-09)

역할/입출력: NextSeed()로 일반 새 여정에 사용할 nonzero uint 한 개를 공급하는 작은 계약. RunUIController가 새 여정 요청에만 호출하고 GameApplication이 주입한다. FixedSeedSource/SystemSeedSource가 구현한다. 재개·표시·시간 기록은 이 경계를 호출하지 않는다. 코어 추출 전 Runtime에 위치한다.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

# ISeedSource.cs