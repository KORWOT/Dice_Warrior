# CompetitionFlowTests.cs

- 역할: 새 authored 화면과 실제 포인터/프레임/저장 복귀를 격리된 PlayMode에서 검증한다.
- 핵심 동작: 새 프리팹의4개 말 참조, 동일 칸 분리, 성장 동기화, 인간 이동/기호/적용 스킬 연출, AI 라운드 진행, 저장 재표시, 이동 연출 중 닫기/재열기 상태 동치,720×1280/720×1600 및 inset safe area를 검사한다.
- 실제 진입 통합: CompetitionScreen 하나만 등록하고 직접 공동 로비를 연다. 출전 선택→성장 구매→최신 프로필 재확인→경기 시작→로비→재개와 App 재생성 후 선호 설정/경기 보존을 실제 포인터로 검증한다. 메모리 IGameBackend만 주입하고 지연 프로필 응답 중 비활성화해 BindingRevision이 변하지 않는지 확인한다.
- FLOW_COMBO_03: `모험 종료`→동일 진행 이어하기, `새 게임` 경고/취소의 파일 불변과 확정 후 초기화·원문 보존·재로드를 실제 포인터로 검사한다. 새 게임 직전 프로필 조회 실패에도 기존 모험을 이어갈 수 있어야 한다. 별도 격리 경기의 3개 주사위를 지팡이·지팡이·검으로 고정해 실제 전투 명령/회전 후 마력탄 SKILL→검기 COMBO의 표시와 로그 순서를 확인한다. 경고/혼합 발동 PNG를 저장한다.
- 계정 실패 경계: 서버 불통 후 명시 재조회, 승인 응답 유실 후 재진입/같은 ID와 body 재시도/단일 성장 적용, 승인 직후 임시 계정 파일 독점 잠금으로 로컬 적용 실패와 pending 보존, old active run 원문을 바꾸지 않는 출전 선택/새 경기/성장 보류, 계정·경기 손상 원문 보존과 복구 진입, 기존 start pending 자동 제출 금지를 확인한다.
- 입출력: 실제 authored prefab/content/공용 UI → NUnit 결과 및 Temp/shared-world-competition PNG. 고유 임시 저장과 명시 seed33을 사용한다.
- MAIN_02 화면 증거: main-lobby/loadout/legacy-growth-held PNG는 계정 로비·출전 선택·이전 기록으로 성장 보류된 화면을 저장한다. 최초 로컬 로비 검사는 같은 프레임에 새로 활성화한 Action4가 depth/absoluteDepth=-1인 상태라 실제 RaycastAll에서 제외되어 실패했다. 해당 검사만 메뉴가 표시되는 다음 프레임 후 선택하고, 포인터 hit·선택·저장 동치 assertion은 유지한다. 실패 진단은 hit를 먼저 고정한 뒤 좌표/Graphic 깊이/CanvasGroup 상태와 캡처를 남긴다.
- 사용하는 대상: CompetitionApp/Session/Screen/Playback, PointerEventData/Raycast, RenderTexture, 기존 LoopJson/LoopGlyphGraphic.
- 사용하는 쪽·관계 근거: SharedWorld.PlayMode.Tests assembly를 Unity Test Runner/Pipeline이 실행한다. 실제 서버/사용자 저장을 열지 않는다.
- 상태/수명: 각 검사별 고유 fixture를 만들고 자신의 오브젝트·RenderTexture·허용 임시 하위 폴더만 정리한다. 파일 잠금은 finally에서 반드시 해제한다. 실제 서버/사용자 DB·계정은 접근하지 않는다.
- 검수 주의: MAIN_02의 실제 최초 RED는 새 로비 출전 선택 누락, FLOW_COMBO_03의 실제 최초 RED는 모험 종료 버튼 누락이었다. 구형 화면 진입 검사는 제거한 제품 흐름에 맞춰 위 직접 계정 검사로 교체했다. 현재 총 15개 검사의 실제 실행 결과는 SHARED_WORLD_FLOW_COMBO_03_REPORT가 기준이며 코드 존재만 PASS로 간주하지 않는다. Android 장치 검증을 대신하지 않는다.
