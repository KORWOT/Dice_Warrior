# FateDiceConfig.cs

RA-C 현재 계약 · 2026-09-09

- 역할: 제작 SO와 표시 설정, 구형 저장 호환 DTO.
- 입출력/핵심 동작: GameConfigData : RunRulesCatalog가 presentation만 더해 기존 config JSON 필드명을 유지한다. Snapshot은 규칙+표시 검증 후 명시 복사한다. PresentationSettings.DeepCopy는 값형 Color/Vector2, 배열 및 두 timing 그룹을 독립 복사하고 null 그룹의 기존 실효 legacy 시간을 보존한다.
- 직접 사용하는 대상: RunRulesCatalog/RulesCopy, Unity Color/Vector2/ScriptableObject.
- 직접 사용하는 쪽: PrototypeAuthoring/UIWorkbenchPreview, RunSession, LocalRunStore, RunUIController/표시 계층.
- 상태/수명·검수 주의: SO는 원본 정의다. Core에는 GameConfigData와 Unity 표시 타입을 전달하지 않으며 런이 저장된 정의를 최신 SO로 대체하지 않는다.
- 관계 근거: 위 공개 API 및 실제 소스의 직접 호출/필드 타입을 확인했다. 실행 상태는 Docs/Reports/ROGUELIKE_ARCHITECTURE_REPORT.md를 따른다.
