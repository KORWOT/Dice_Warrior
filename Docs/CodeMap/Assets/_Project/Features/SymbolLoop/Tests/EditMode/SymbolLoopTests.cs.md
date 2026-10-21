# SymbolLoopTests.cs

- 역할: 새 계약에 직접 해당하는 기호/보드/성장/저장 경계 검증.
- 핵심 동작: 84 ordered outcomes를 독립 기대 표로 비교, D1/D2/태그/완주/비용/규칙 동결/정산/저장 실패/복구/미결 입력 검사.
- 입출력: 고정 시드/명시 후보/실제 임시 파일과 고장 주입 ILoopStore → NUnit assertions.
- 사용하는 대상: LoopRules/SymbolCombat/LoopSession/LoopStore 및 Authoring 경로의 실제 프리팹.
- 사용하는 쪽/관계 근거: SymbolLoop.EditMode.Tests assembly의 Unity Test Runner.
- 상태/수명: 테스트별 고유 임시 경로, 종료 시 허용 루트 하위 여부 확인 후 정리. 실제 persistentDataPath 접근 없음.
- 검수: 정산/영구 성장 검사는 높은 HP/위력 fixture를 사용한다. 별도 기본 설정 시드 33 완주 검사도 두지만 어느 쪽도 밸런스 승률 증거가 아니다. 실제 Title preview의 Missing Script/연결도 확인한다. 구형 v1 테스트/fixture를 실행하지 않는다.
