# DiceRollAuthoring.cs
- 역할: Editor API로 누락된 DiceRollUI.prefab을 만들고 기존 UIRoot UIManager 목록에 추가한다. 메뉴는 Fate Dice/주사위 창 원본 만들기.
- 입력/출력: 기존 CommonButtonView의 한글 폰트를 사용한다. 작성 대상은 Run/Prefabs/DiceRollUI.prefab 및 Shared/UI/Prefabs/UIRoot.prefab 등록 목록뿐이다.
- 동작: 620×730 panel, 3×2 여섯 주사위, 안내/확정결과/명시적 굴리기 버튼과 직접 참조를 작성한다. 기존 popup의 사용자 배치는 유지한다. 기존 원본도 LoadPrefabContents/SaveAsPrefabAsset으로 저장해 import에서 자동 보완된 CanvasRenderer가 실제 prefab 바이트에도 남도록 한다. 새 면에는 renderer를 명시적으로 추가한다. 재실행 시 지정 원본 5개의 바이트 동일성을 확인했다. 등록된 popup을 중복 추가하지 않는다.
- 보호: Play/컴파일/갱신/Prefab·Preview Stage/dirty scene 중에는 실행을 거절한다. LoadPrefabContents/NewPreviewScene의 소유 객체는 finally에서 정리한다. 기존 원본/GUID와 다른 화면 등록을 유지한다.
- 관계: DiceRollUI/DiceFaceView/CommonButtonView/UIManager를 사용한다. PlayWorkbenchWindow는 생성된 원본을 편집/미리보기한다. Runtime에서는 호출하지 않는다.
- 검수: 실제 여섯 renderer, popup 등록 1개, 재실행 idempotence, 참조 누락/글자 경계. 증거는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md.
