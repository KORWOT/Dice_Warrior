# TitleUI.cs
- 역할: BaseUI<TitleUIData> 파생 타이틀 화면. 같은 파일의 TitleUIData는 title/subtitle/status, ButtonAppearance, enterLobby Action을 전달한다.
- 입출력: authored Text 3개와 CommonButtonView enterButton. enter-lobby ID/ENTER LOBBY 문구로 소유 콜백 하나를 바인딩한다.
- 동작: OnBind는 전달 텍스트/표시 여부와 버튼 상태만 설정, OnUnbind는 소유 listener/텍스트를 정리한다. 사용자 지정 폰트/배치를 재생성하지 않는다.
- 관계: RunUIController.EnterTitle/Render에서 UIManager.Show<TitleUI>, SceneStructureAuthoring이 전체 TitleUI.prefab 및 UIRoot registry를 작성. 씬/세션/store에 직접 의존하지 않는다.
- 상태/수명: UIManager의 타입별 캐시와 BaseUI 수명을 사용한다. Title을 떠나도 앱 루트/다른 게임 화면은 유지된다.
- 검수: 실제 prefab refs, 포인터 도달과 세로비, 이중 클릭 시 한 번의 이동, 실패 상태 문구와 다시 진입 가능 여부.

## 한글 UI 적용 (2026-09-08)
- 진입 버튼 표시를 ‘로비로 이동’으로 바꿨다. TitleUIData 바인딩과 기존 enterLobby 콜백/입력 수명은 그대로 유지한다. 고정 프리팹 문구와 모든 Text는 KoreanUiAuthoring이 적용한다.
- 현재 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.
