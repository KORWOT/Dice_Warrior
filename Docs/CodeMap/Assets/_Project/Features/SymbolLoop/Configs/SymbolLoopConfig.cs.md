# SymbolLoopConfig.cs

- 역할: 새 게임 규칙과 MVP 콘텐츠의 ScriptableObject 작성 경계.
- 핵심 동작: CreateRules에서 규칙 검증 후 깊은 복제 반환.
- 입출력: Inspector Rules → 독립 Rules. 자산 SymbolLoopConfig.asset.
- 사용하는 대상: LoopRules, LoopJson, UnityEngine. 사용하는 쪽: SymbolLoopApp, SymbolLoopAuthoring, PlayMode 검증.
- 관계 근거: Title의 App.config 직렬화 참조, Authoring의 AssetDatabase 작성.
- 상태/수명: P1에서 자산은 로비 콘텐츠 표시/로컬 선택 검증용이다. 새 런의 실제 규칙은 백엔드 티켓이 결정하며; 저장된 진행 런은 자체 rules 스냅샷 사용.
- 검수: 기존 런을 SO 편집으로 소급 변경하지 않음. 실제 장치/밸런스 확정과 기능 검증은 별개.
