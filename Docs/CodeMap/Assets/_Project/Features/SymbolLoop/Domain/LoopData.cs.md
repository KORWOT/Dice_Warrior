# LoopData.cs

- 역할: SymbolLoop.Core의 기호·타일·단계·적 의도·규칙·런·프로필·영수증·명령 DTO.
- 핵심 동작: 규칙 기본값과 직렬화 가능한 상태만 정의한다. Unity·파일·화면 의존 없음.
- 입출력: Rules는 새 런에 복사되는 콘텐츠, PlayerDocument는 프로필/런/receipt의 공동 저장 단위. Command는 요청 ID·입력·runId·revision·generation·seed.
- 사용하는 대상: System, List. 사용하는 쪽: LoopRules, SymbolCombat, LoopSession, LoopStore, Config, 화면, 테스트.
- 관계 근거: 위 파일들의 직접 타입 참조. 외부 서비스 어댑터나 v1 payload 없음.
- 상태/수명: 후보마다 깊게 복제하며 공개 Snapshot도 복제본. Run의 장비/XP/골드와 Profile의 영구 재화/성장을 분리한다.
- 검수: 배열·리스트/중첩 규칙의 누락 복제, die instanceId/definitionId 구분, 로그 적용·생략 상태.
