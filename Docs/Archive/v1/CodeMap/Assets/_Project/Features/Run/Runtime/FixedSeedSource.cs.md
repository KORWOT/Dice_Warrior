# FixedSeedSource.cs

## RA-A 현재 계약 (2026-09-09)

역할/입출력: 생성자의 nonzero uint를 Value와 NextSeed()로 반복 제공하는 불변 테스트/작업실 공급자. 0은 ArgumentOutOfRangeException. GameApplication/PlayWorkbenchSession의 명시 주입과 RunUIController.Seed 호환 setter에서 사용한다. RNG·파일·Unity 객체 수명 없음.

검증 상태/실제 증거: Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md. 아래 과거 기록은 이번 PASS를 대신하지 않는다.

# FixedSeedSource.cs