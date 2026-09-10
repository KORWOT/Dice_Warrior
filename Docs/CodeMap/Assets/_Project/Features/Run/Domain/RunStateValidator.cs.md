# RunStateValidator.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 저장 및 명령 후보의 순수 불변조건 검증.
- 입출력/핵심 동작: Validate(RunStateData)는 schema/규칙/ID/graph/phase/자원/패/확정 카드/보상/기록 의미를 검사하고 RunStateValidationException을 던진다. 상태/RNG/시간을 변경하지 않는다. null과 비어 있는 inline DTO는 phase/ID에 따라 같은 부재 의미로 허용한다.
- 직접 사용하는 대상: RunStateData.Rules 및 순수 규칙.
- 직접 사용하는 쪽: RunApplication, Runtime RunSession/LocalRunStore.
- 상태/수명·검수 주의: 기존 Runtime 파일을 GUID 유지 이동했다. LocalRunStore만 파일 경로를 붙인 InvalidDataException으로 감싼다. Runtime 카탈로그 Validate override는 표시 설정까지 확인한다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.

- RA-D: pendingReward.addActionId와 absent reward의 공백 여부, ShopRules.Validate의 고정 상품/가격/상점 수명을 명령·저장 경계에서 검사한다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
