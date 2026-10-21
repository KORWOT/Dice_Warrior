# CombatLayoutAuthoring.cs
- 역할: 기존 CombatUI.prefab/ActionCardView.prefab을 전투 전용 배치로 이전하고 CombatDie.prefab variant를 만드는 Editor 도구.
- API/입력: Apply(), CombatPath/ActionPath/DiePath. 기존 CombatUI root/layout/group, CommonButtonView 원본과 직접 font, ActionCardView의 기존 slot을 요구한다.
- 동작/출력: LoadPrefabContents/SaveAsPrefabAsset/UnloadPrefabContents로 기존 GUID를 보존한다. CombatUI 자식만 header/HP/arena/dice/actions/notice로 배치하고 typed 필드를 연결한다. ActionCardView는 같은 variant/frame을 portrait로 배치하며 fontStyle 상속을 유지한다. CombatDie는 CommonButtonView variant다.
- 상태/실패: Play/dirty scene/Prefab Mode 거절. 마지막 CombatUI 저장에 layoutVersion=1을 기록하며 재실행은 no-op이다. 임시 contents/객체는 finally 정리한다.
- 직접 관계: Unity CLI eval/MenuItem → Apply; UiPrototypeAuthoring.CommonPath, CombatUI/RunScreenLayout/CombatStageGraphic/ActionCardView/CommonButtonView/uGUI. Scene/UIRoot/catalog/config/다른 화면을 쓰지 않는다.
- 검수: PLAN 허용 자산3개, 원본 meta/GUID·variant 상속·고정 참조 보존, 반복 실행 hash 불변, 실제 화면을 확인한다. 기존 장식 Graphic의 CanvasRenderer 누락은 같은 허용 CombatUI에 Editor API로 보완했다.
