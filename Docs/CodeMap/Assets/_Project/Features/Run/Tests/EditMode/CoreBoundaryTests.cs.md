# CoreBoundaryTests.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 실제 Core 참조 차단과 명시 복제/소유권 검사.
- 입출력/핵심 동작: 어셈블리의 실제 타입 소속/참조 및 noEngineReferences/overrideReferences를 검사한다. 모든 public 필드 값과 중첩 객체 분리를 재귀 비교하고 null/빈 DTO, core 후보 저장 실패, 저장된 규칙/독립 표시/legacy 시간을 검사한다.
- 직접 사용하는 대상: Core Domain/Application, Runtime facade, PrototypeAuthoring 테스트 정의, NUnit.
- 직접 사용하는 쪽: Unity EditMode Test Runner.
- 상태/수명·검수 주의: 별도 초기 Core 부재 RED를 확보한다. 전체 참조 차단은 실제 조립 결과를 확인하며 단순 파일 존재를 PASS로 삼지 않는다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.

## RA-C 시간 경계
추가 실제 파일 회귀는 .01을100번 누적한 현재/최근결과 double의 IEEE754 bits와 전체 JSON을 반복 Load에서도 정확히 비교한다. 임시 저장 경로만 사용한다.
