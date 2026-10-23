# CompetitionHttpFixture.cs

- 역할: NEXT_PHASE_06 Q1의 실제 HTTP/격리 저장/authored UI 포인터 검사를 지원한다. 제품 코드나 메모리 대역을 추가하지 않는다.
- 실행: CompetitionHttpFlowTests는 Explicit이므로 메인은 Q1 exact test name을 선택할 때 Pipeline에 `--include_explicit true`를 함께 전달한다. 일반 Q0 전체 호출에는 이 옵션을 추가하지 않으며, 이 helper가 독립 검사나 서버를 실행하지 않는다.
- 입력: `DICE_WARRIOR_Q1_URL`, `DICE_WARRIOR_Q1_ROOT`, `DICE_WARRIOR_Q1_USER`를 필수로 읽는다. 주소는 숫자 루프백 HTTP이며 5087을 거절한다. root는 현재 프로젝트 `artifacts/next-phase-06/q1-*/client` 하위만 허용하고, HttpBackendSettings로 주소·계정·제한시간을 추가 검증한다. 설정 누락은 실패이며 기본값으로 대체하지 않는다.
- 저장 경계: 사례 폴더는 j(journey)/p(profile-fault)/l(legacy)/r(restart)로 짧게 유지한다. 일반 사례의 계정 ID는 GUID 파생값이며 기존 사례 폴더 재사용을 거절한다. restart 두 검사는 같은 r와 prefix_restart 계정을 쓴다. 실제 경로는 Settings.CheckpointPath의 endpoint/계정 해시 폴더와 그 안의 shared-world-preview-v1/checkpoint.json, adventurer-selection.json, lobby-loadout.json이다. 사용자 영구 저장 경로를 참조하지 않는다. 증거는 사례 폴더에 보존하고 최종 정리는 메인이 맡는다.
- 핵심 동작: HttpGameBackend에 Q1WireHandler를 주입한다. handler는 실제 HttpClientHandler를 호출하며 서버가 제공하는 before-abort/after-abort/after-delay 헤더를 일치하는 다음 요청 하나에만 붙인다. 응답을 생성하거나 승인 상태를 변경하지 않는다. 메서드·경로·본문 해시·요청 ID·오류를 메모리에서 관찰하고 증거 요약에는 본문/개발 계정 원문을 출력하지 않는다.
- UI: 실제 UIRoot/CompetitionScreen prefab과 v4 JSON/SymbolLoopConfig를 AssetDatabase로 읽는다. OpenHttp의 authored 진입 또는 OpenAccount의 실제 HTTP transport 주입을 사용한다. UI 이벤트는 RaycastAll로 목표 버튼 hit와 interactable을 확인한 뒤 pointerClickHandler로 전달한다. 메뉴가 렌더된 다음 프레임에 누른다.
- 자연 진행: 변경하지 않은 정의와 seed33으로 travel/합법 사건 선택/combat/lap/advance를 실행한다. 저체력 시 회복을 우선하며 그 외 전투, XP 선택 순이다. 추가 주사위는 비전, 마지막 장비는 aegis를 선택한다. 첫 이동/전투는 전체 연출을 기다리고 나머지는 저장 성공 후 App 비활성/재활성으로 연출만 중단한다. actionNumber가 정확히 하나 늘고 중단 전후 전체 저장 스냅샷이 같아야 한다. RNG·HP·주사위 눈을 조작하지 않는다.
- 진단: 별도 실제 HttpClient로 healthz/M4 metrics/costs를 읽는다. healthz의 개발 모드·프로토콜·v1 권한 규칙 버전을 확인한다. 진단 요청은 게임 요청 카운터에 섞지 않는다. FileHash는 실제 파일 SHA256, Capture는720×1280 RenderTexture PNG, Write는 사례 폴더의 단일 파일명만 허용한다.
- 사용하는 대상: CompetitionApp/Content/CommandRules/Screen, HttpGameBackend/HttpBackendSettings/LoopJson, Unity UI/EventSystem/AssetDatabase, NUnit, System.Net.Http, Newtonsoft. 사용하는 쪽: CompetitionHttpFlowTests의 5개 명시 실행 검사. 관계 근거는 생성자·Open·Press·Step·Await·Capture·Write 직접 호출이다.
- 상태/수명: HTTP/연출 대기는 30초 상한, 진단 전송은10초 상한이다. UI·카메라·EventSystem·RenderTexture·HTTP transport만 해제한다. 서버 실행/종료/DB/환경변수 주입은 메인 소유이며 이 helper는 프로세스를 조작하지 않는다.
- 검수 주의: Q1은 격리 Development Mock의 실제 HTTP 증거이며 Firebase/실서비스/Android 검증이 아니다. 테스트 코드 작성과 실행 성공은 구분하고 최신 결과는 NEXT_PHASE_06 REPORT를 따른다. metrics의 추정 Firestore 사용량과 비용을 실제 사용량/청구로 표시하지 않는다.
- Q1-03 해결: 긴 시험 경로287자 실패를 재현한 후 사용자 승인 범위에서 root/사례 폴더를 단축했다. 생성자는 실제 원자 저장·generation·보존 접미사까지 계산해260자 미만을 파일 접근 전 요구하고 path-budget.json을 남긴다. 최종 최장259자, HTTP5여정 PASS. 제품의 저장 경로나 긴 경로 처리 정책은 변경하지 않았다.
