# SymbolLoopFlowTests.cs

- 역할: 실제 authored UI 포인터 흐름과 이동·주사위·스킬 재생/중단을 검증.
- 핵심 동작: 실제 기본 Config/격리 저장/고정 시드로 캠페인 완주. 별도 유효 합성 저장으로 1·2·3눈, 조합/기본기/처치 생략 검증. 픽셀 캡처는 실행된 실제 Canvas/Camera에서 얻는다.
- 입출력: 720×1280, 720×1600, 축소 safe area → 포인터 raycast, 칸 방문, 정지 면, 로그 순서/반격, 중복 입력·중단·화면 닫기 후 저장 불변 단언 및 Temp PNG.
- 사용하는 대상: App/Screen/Playback/Session/Store, Shared UI, InputSystem/EventSystem/Camera. SymbolLoop.PlayMode.Tests assembly에서 실행.
- 상태/수명: 생성 객체/Config 사본/RenderTexture/임시 저장만 정리. 실제 사용자 저장에 접근하지 않는다. 재생 대기는 각 12초 제한.
- 검수: 기본값 완주와 합성 연출 시나리오를 구분. 캡처/포인터 검증은 Android 장치 터치·OS 백그라운드 재개 검증의 대체물이 아니다.

- CanvasRenderer와 실제 메시 정점 수를 검사하고, 새 활성 Graphic의 raycast depth가 등록된 다음 프레임에 다음 포인터 입력을 보낸다.
