# RunTests.cs
- 책임: 순수전투·명령원자성·탐험/보상·성장/태그·재굴림경계의EditMode NUnit34건.
- fixture: PrototypeAuthoring.CreateDefaults독립복제, Battle명시상태, Journey특정사건가중치/상한. 실제SO원본/파일변경없음.
- 직접사용: RunSession,CombatRules,GrowthRules,GameConfigData/RunState/OfferedCard,JsonUtility,NUnit. Checkpoint의IOException만실패주입.
- 검증: 생존적만행동/보호막만료/Guard비상시감쇠/빈등급수치/모든제시행동/중복과실패RNG보존;가지2단계/5사건/+1/임계Boss/보상결정재진입;훈련상한표시;레벨/HP/교체·포기/ANY ALL NONE부분교집합/태그소스·대상분리;권한·비용·단일die·저장실패·교체분포.
- 호출자: UnityTestRunner/CLI만,제품에서호출하지않음. 실제파일은SaveTests,실제포인터는FateDiceGuiTests담당.
- 증거: M2 9RED→PASS,M3 8RED→PASS,훈련12vs16RED→PASS,M4 14미구현RED→PASS. Edit98/98에서본파일34건PASS. 모든예정규칙이실제연결되었음.


## 추가 UI 직접 회귀
- RunTests 34→39개. A→C,D / B→C,E 공유 자식 oracle에서 선택·보상 후 활성/이력, RNG/진척, 초기/선택후/보상후 디스크 왕복을 확인한다.
- cycle, active/history ID 겹침, 누락 이력 자식은 거부하고 nodeHistory 필드가 없는 checksum schema 1 파일은 이어서 저장한다.
- 실제 초기 구현 EditMode 전체122/122 PASS(기존98 + 신규24). 테스트 fixture는 별도 임시 디스크 경로를 사용한다.
