# GrowthRules.cs
- 책임: 장비를합친실제스탯,XP레벨업/최대HP변경,명시태그피해·보호보정.
- API: Stats→PlayerStats, Grant(state,reward), Equip(state,id), Multiplier(state,action,value)→double.
- 직접 사용: RunState, config.growth.levels/equipment, GameConfigData.Equipment, TagModifier/ActionDefinition; LINQ/HashSet.
- 호출자: RunSession.ClaimReward→Grant, Equip→Equip; CombatRules.Evaluate→Stats/Multiplier; Screen.Render/장비스탯표시→Stats; RunTests/GUI/SaveTests.
- 성장: XP를현재레벨행비용만큼차례로소비,마지막행뒤레벨상한. 기본스탯성장만base필드에저장,장비3슬롯은매번합산. 최대HP증가분만현재HP회복,감소는새상한으로제한,보상HP는성장뒤적용.
- 태그: Card와장착Equipment태그집합을분리,ANY/ALL/NONE와Damage/Block대상명시,일치bonus합+1을0이상으로제한. 태그전파/화상/추첨편향없음.
- 상태/수명: SO불변,규칙RNG소비없음,실제게임변경은RunSession의복제/저장경계안. 정수overflow는int상한으로제한.
- 증거: Edit98/98에부분교집합/조건대상분리/교체·포기/레벨·HP와재굴림경계포함,GUI8/8에훈련16XP/장비실제스탯/주사위교체표시검사. A독립M4검토 필수결함없음.


- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.
