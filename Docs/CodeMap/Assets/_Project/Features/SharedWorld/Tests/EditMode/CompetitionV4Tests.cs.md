# CompetitionV4Tests.cs

- 역할: v4 수치/전투/필수 Owner/타입 누락 거절·콘텐츠/성장/AI 재현 및 v3 정확 호환성 회귀 검사.
- 핵심 동작: 3모험가×9기술 리터럴 기대값, 타입변환·보호막·처치생략, CON성장1회, LUK보물1회, 전체 명령 재현/중간 직렬화를 검사한다.
- 입출력: 별도 v4 JSON fixture와 NUnit24사례, v3 사전 고정 시작/최종 SHA2563쌍.
- 사용하는 대상: 순수 SharedWorld 코어, CompetitionGrowthTests/RulesTests fixture, LoopJson.
- 사용하는 쪽: Unity EditMode 및 .NET Competition.Tests glob 링크.
- 상태/수명: 실제 사용자 저장/백엔드 없음. 각 검사에서 새 정의/상태 복사.
- 관계 근거: csproj와 EditMode asmdef가 같은 파일을 컴파일하며 고정해시는 v4 코드 수정 전 캡처했다.
- 검수 주의: 예상 해시를 새 구현 결과로 갱신하지 않는다. Preview 불변/계산실패 불변·구형 신규필드 생략을 보호한다.

