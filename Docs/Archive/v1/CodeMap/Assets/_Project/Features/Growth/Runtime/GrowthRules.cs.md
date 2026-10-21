# GrowthRules.cs

## RA-C 현재 경계 (2026-09-09)

상태 인자는 RunStateData, 설정 접근은 Rules로 변경되고 Core asmref에 소속된다. HP/위력/수호·레벨·장비·태그 계산식, overflow 처리와 회복/상한 순서는 그대로다. CombatRules/RunApplication 및 기존 Runtime 표시/테스트가 호출한다. 입력 상태 변경 함수는 세션 후보 안에서 실행한다.

기존 수식/행동 보존 및 Core 컴파일/전체 패·명령 재연은 ROGUELIKE_ARCHITECTURE_REPORT의 새 실행 증거를 따른다. 아래 기존 단계 증거를 RA-C PASS로 재사용하지 않는다.

- 책임: 장비를합친실제스탯,XP레벨업/최대HP변경,명시태그피해·보호보정.
- API: Stats→PlayerStats, Grant(state,reward), Equip(state,id), Multiplier(state,action,value)→double.
- 직접 사용: RunState, config.growth.levels/equipment, GameConfigData.Equipment, TagModifier/ActionDefinition; LINQ/HashSet.
- 호출자: RunSession.ClaimReward→Grant, Equip→Equip; CombatRules.Evaluate→Stats/Multiplier; Screen.Render/장비스탯표시→Stats; RunTests/GUI/SaveTests.
- 성장: XP를현재레벨행비용만큼차례로소비,마지막행뒤레벨상한. 기본스탯성장만base필드에저장,장비3슬롯은매번합산. 최대HP증가분만현재HP회복,감소는새상한으로제한,보상HP는성장뒤적용.
- 태그: Card와장착Equipment태그집합을분리,ANY/ALL/NONE와Damage/Block대상명시,일치bonus합+1을0이상으로제한. 태그전파/화상/추첨편향없음.
- 상태/수명: SO불변,규칙RNG소비없음,실제게임변경은RunSession의복제/저장경계안. 정수overflow는int상한으로제한.
- 증거: Edit98/98에부분교집합/조건대상분리/교체·포기/레벨·HP와재굴림경계포함,GUI8/8에훈련16XP/장비실제스탯/주사위교체표시검사. A독립M4검토 필수결함없음.


- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.

- RA-D: Grant가 addActionId를 소유 집합에 한 번만 추가하고 나머지 보상은 기존대로 처리한다. 중복 획득은 추첨 가중치나 RNG를 늘리지 않는다.
- 검수 근거: 직접 호출 소스와 RA-D 계약 시험. 실제 실행 상태는 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
