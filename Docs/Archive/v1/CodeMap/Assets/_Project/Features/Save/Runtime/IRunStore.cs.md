# IRunStore.cs

## RA-B 현재 계약 (2026-09-09)

역할/입출력: Exists/Load/Save/Archive의 단일 로컬 체크포인트 슬롯 계약. Load는 독립 상태를 제공하고 Save는 성공 뒤 반환하거나 실패 예외를 전달한다. LocalRunStore와 실패 주입 테스트 저장소가 구현하며 GameApplication/RunUIController/RunSession이 주입한다. 계정·서버·전역 저장 매니저가 아니다.

실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다. 기존 기록은 이번 실행 증거를 대신하지 않는다.
