# PlayWorkbenchTests.cs

- 역할: Editor 작업실의 상태 생성과 정지 preview를 검사하는 EditMode 테스트6개(일반 Test 3개, UnityTest 3개)다. 실제 Play 시작/중지와 포인터 조작은 메인의 별도 Unity 검증 범위다.
- 입력: 실제 GameApplication prefab/config/UIRoot/8화면 원본과 Pretendard-Regular.ttf, 9개 WorkbenchStartPoint, 고정 시드/시련/등급상한을 사용한다. 테스트가 직접 만든 고유 임시 폴더만 LocalRunStore에 주입한다.
- 프리셋 검사: 모든 시작점의 예상 RunPhase, seed/RNG/cap/trial/config 유효성을 확인하고 LocalRunStore.Save/Load로 완전 상태 검증과 JSON 왕복을 실행한다. Build가 source config SO나 반환 상태를 변경하지 않는지 함께 비교한다.
- 결정성/옵션: 동일 옵션 Build 두 번의 전체 JSON에서 GUID runId와 그 ID 접두부만 정규화하여 모든 선택·RNG·설정을 비교한다. 복사본의 중첩 config를 바꿔 다른 결과와 원본 SO의 독립성을 확인한다. null·앱 누락·seed 0·없는/시련 아닌 action·잘못된 cap/point를 정확한 예외 타입으로 검사한다.
- 미리보기: 9시작점을 720×1280/720×1600에서 열어 8개 concrete 화면, SourcePrefabPath, WorldSpace 크기, PreviewScene, 열린 화면/단일 cache를 확인한다. App/Controller/EventSystem 부재와 CanvasGroup/Selectable 입력 차단을 검증한다.
- 폰트/가시성: 활성 Text가 실제 Pretendard를 사용하고 한글 글리프를 포함하는지 검사한다. Canvas와 RectMask2D 안에 완전히 보이는 텍스트의 preferredHeight 허용오차는 0.1px다. CanvasRenderer.GetMesh() 반환 메시의 실제 정점 수가 0이면 실패로 판정하며 Unity 소유 메시를 변경하거나 파괴하지 않는다.
- 독립 표시 기준: CombatRules/GrowthRules의 실제 HP/정규화 bar/IntentAmount/카드 효과와 OriginalId를 비교한다. 탐험 카드는 실제 제시 개수와 공개 노드 유형·등급만 검사한다. 등급/노드 한글 기대값은 테스트의 고정 목록이다.
- 정리/보존: 새 preview가 이전 root/scene을 폐기하는지, GoToMainStage 두 번이 안전한지 검사한다. source dependency prefab/asset와 제작 scene/meta bytes 및 dirty 상태, 기본 save bytes, Library/FateDiceWorkbench 파일목록, pending/LastStorePath/LastError가 그대로인지 확인한다. 기본 save는 보존 기준을 읽을 뿐 저장소나 bootstrap에 전달하지 않는다.
- 관계: PlayWorkbenchSession/UIWorkbenchPreview를 직접 호출하고 AssetDatabase/StageUtility/EditorSceneManager/uGUI/NUnit/UnityTest를 사용한다. 테스트 asmdef에는 기존 Runtime/Editor/SharedUI 외 Unity.ugui 참조가 필요하며 변경은 메인 담당이다.
- 실행 증거: artifacts/play-workbench/red-edit.json은 별도 최소 reflection 테스트 StopModePreviewExposesAnExplicitDisposableStage의 1건 실패(신규 타입 null)를 기록한다. 최종 직접 참조 테스트6개와 기존 회귀 검증은 이 초안 시점 NOT_RUN이다. 최종 테스트 파일에 최초 reflection 테스트를 중복 포함하지 않는다.
