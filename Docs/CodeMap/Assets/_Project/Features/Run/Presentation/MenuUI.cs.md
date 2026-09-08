# MenuUI.cs

- 역할: Lobby의 캐릭터·세팅·성장 안내 탭과 출전/이어하기를 표시하는 RunScreenView<MenuUIData>다. 기존 방랑자와 런 설정만 표시하며 계정·영구 성장·게임 규칙을 추가하지 않는다.
- serialized 입력: 기존 trialHeading/capHeading/seedHeading, trialChoices/capChoices/mainChoices, seedInput/error/lastResult 및 상속 layout/group을 유지한다. characterName/characterDetails/growthDetails, characterPanel/settingsPanel/growthPanel, characterTab/settingsTab/growthTab, layoutVersion이 추가된다. LobbyPreparationAuthoring가 MenuUI.prefab 한 개에 이 연결을 작성한다.
- 데이터: RunUIController 또는 Editor UIWorkbenchPreview가 만든 MenuUIData의 세 캐릭터/성장 문자열과 기존 선택 DTO를 바인딩한다. 시련·등급·새 여정·이어하기·손상 보관의 기존 command key/가용 상태/delegate는 Widgets.Choice를 통해 유지한다. 로비는 RunState/LocalRunStore/SceneManager를 직접 조회하지 않는다.
- 로비 HUD: BindHUD가 header/notice를 표시하고 기존 런용 stats/situation/fate/gear/diceRow를 숨긴다. 일반 로비의 seedHeading/InputField는 참조와 SetTextWithoutNotify 바인딩을 유지하되 비활성이다. 실제 출전 설정은 시련·탐험 상한이며 시드 편집은 작업실에서 제공한다.
- 탭/입력: 한 번에 하나의 panel만 활성화하고 선택 색상을 적용한다. 탭 이동은 root와 Button의 활성 상태/IsInteractable을 확인하므로 modal/global lock 중 직접 Invoke로도 탭을 바꾸지 못한다. 탭만 바뀔 때 게임 명령/RNG/저장 변화 없이 scroll을 위로 맞춘다.
- 재바인딩/수명: 열린 로비에서 시련·등급 변경으로 재Bind해도 선택 탭을 유지한다. Close는 자기 seed/tab listener와 동적 문자열을 정리하고 다음 진입의 탭을 캐릭터로 초기화한다. RemoveAllListeners를 사용하지 않으며 OnDestroy도 자기 listener만 분리한다. 공통 버튼의 비활성 입력 guard를 완화하지 않는다.
- 표시: 검정/남색과 절제한 금색으로 선택·주요 행동을 구분한다. accent/buttonNormal/buttonSelected/buttonPressed/buttonDisabled/primaryButton/secondaryBorder/unselectedTabText를 serialized Color로 노출하여 MenuUI 원본 Inspector에서 선택 색상을 편집한다. 선택 영역은 기존 CommonButtonView 원본을 복제하므로 font/style 연결을 보존한다. 하단 mainChoices와 오류/최근 결과/notice는 탭 scroll 밖의 authored footer에 놓인다.
- 관계: UIManager/BaseUI가 화면 캐시·열기/닫기·입력잠금을 담당하고 RunScreenLayout/FateDiceWidgets가 기존 참조와 반복 선택을 제공한다. PlayWorkbenchSession의 초기 trial/cap 설정은 숨겨진 버튼을 우회하지 않고 세팅 탭을 먼저 여는 경로로 연결해야 한다.
- 검수 상태: 실제 통합 실행 결과는 Docs/Reports/LOBBY_MAP_DICE_REPORT.md를 참조한다.
