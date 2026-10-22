# CompetitionFlowTests.cs

- 역할: 새 authored 화면과 실제 포인터/프레임/저장 복귀를 격리된 PlayMode에서 검증한다.
- 핵심 동작: 새 프리팹의4개 말 참조, 동일 칸 분리, 성장 동기화, 인간 이동/기호/적용 스킬 연출, AI 라운드 진행, 저장 재표시, 이동 연출 중 닫기/재열기 상태 동치,720×1280/720×1600 및 inset safe area를 검사한다.
- 실제 진입 통합: 두 화면을 등록하고 기존 SymbolLoopApp의 로비 버튼을 실제 포인터로 클릭한다. 메모리 IGameBackend만 주입해 로비/모드 진입/시작의 프로필3회 확인과 성장 동결을 검증한다. 지연 응답 중 비활성화, legacy Render/disable/pause에도 화면 소유권 유지, 재활성 후 표시 및 기존 화면 복귀를 확인한다.
- 입출력: 실제 authored prefab/content/공용 UI → NUnit 결과 및 Temp/shared-world-competition PNG. 고유 임시 저장과 명시 seed33을 사용한다.
- 사용하는 대상: CompetitionApp/Session/Screen/Playback, PointerEventData/Raycast, RenderTexture, 기존 LoopJson/LoopGlyphGraphic.
- 사용하는 쪽·관계 근거: SharedWorld.PlayMode.Tests assembly를 Unity Test Runner/Pipeline이 실행한다. 실제 서버/사용자 저장을 열지 않는다.
- 상태/수명: 각 검사별 고유 fixture를 만들고 자신의 오브젝트·RenderTexture·허용 임시 하위 폴더만 정리한다.
- 검수 주의: authored 프리팹 미존재 최초1건 실패는 메인에서 확인했다. 실제 이후 실행 결과는 REPORT가 기준이며 코드 존재만 PASS로 간주하지 않는다. Android 장치 검증을 대신하지 않는다.
