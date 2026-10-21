# SymbolLoopFlowTests.cs

- 역할: 실제 authored UI 포인터 흐름과 이동·주사위·스킬 재생/중단을 검증.
- 핵심 동작: 실제 기본 Config/격리 저장/고정 시드로 캠페인 완주. 별도 유효 합성 저장으로 1·2·3눈, 조합/기본기/처치 생략 검증. 픽셀 캡처는 실행된 실제 Canvas/Camera에서 얻는다.
- 입출력: 720×1280, 720×1600, 축소 safe area → 포인터 raycast, 칸 방문, 정지 면, 로그 순서/반격, 중복 입력·중단·화면 닫기 후 저장 불변 단언 및 Temp PNG.
- 사용하는 대상: App/Screen/Playback/Session/Store, Shared UI, InputSystem/EventSystem/Camera. SymbolLoop.PlayMode.Tests assembly에서 실행.
- 상태/수명: 생성 객체/Config 사본/RenderTexture/HTTP 전송/임시 저장만 정리. 실제 사용자 저장에 접근하지 않는다. 네트워크 포함 재생 대기는 각 20초 제한.
- 검수: 기본값 완주와 합성 연출 시나리오를 구분. 캡처/포인터 검증은 Android 장치 터치·OS 백그라운드 재개 검증의 대체물이 아니다.

- CanvasRenderer와 실제 메시 정점 수를 검사하고, 새 활성 Graphic의 raycast depth가 등록된 다음 프레임에 다음 포인터 입력을 보낸다.

- M0~M3: 실제 포인터 완주는 독립 HTTP Mock의 시작→완주→기록 검증/50지급(잔액70)→20재화 성장 구매(잔액50)→HP65 다음 런을 확인한다. `DICE_WARRIOR_HTTP_TEST_URL`에 seed33 격리 서버를 명시해야 하며 없으면 실패한다. 사용자 ID는 테스트마다 GUID이고 체크포인트는 endpoint+계정으로 격리한다. 나머지 합성 연출·손상 회귀는 Editor 전용 Authority DLL을 명시적으로 주입한다.

- 추가: primary 손상+승인 여부 불명 start의 원 요청을 백업 복원 후 재사용한다. 지연된 start 응답이 pause/disable 뒤 완료되어도 중복 발행/진행 소실 없이 재개되는지 검사한다.
- 정산 응답 유실: 명시적 테스트 서버의 `after-abort` 헤더로 실제 HTTP 제출 후 연결을 끊는다. pause/disable→검증 대기→화면 재생성→동일 ID 재시도→50지급/잔액70을 실제 버튼으로 확인한다. 준비는 seed33 명령 직접 실행이며 별도 포인터 완주가 전 과정을 확인한다. `settlement-pending`/`settlement-recovered` 캡처를 생성한다. fault 기능은 서버의 명시적 Testing 설정에서만 허용된다.

- 테스트 분류: 기존 PlayMode asmdef의 플랫폼 제한 없는 분류를 유지한다. 소스 전체는 UNITY_EDITOR 조건이며 Editor 전용 Authority DLL을 명시 참조한다. includePlatforms=Editor를 넣으면 EditMode로 오분류되므로 넣지 않는다. 일반 플레이어 그래프에 권한 DLL이 없는지는 VerifyUnityBoundary가 검사한다.
