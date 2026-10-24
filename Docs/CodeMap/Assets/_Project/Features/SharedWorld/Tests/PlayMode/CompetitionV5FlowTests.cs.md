# CompetitionV5FlowTests.cs

- UI13: 로비 6면 도구는 화면 줄바꿈 문구 대신 활성 Shell 버튼의 고정 BoundId `net`으로 찾는다. 실제 첫 raycast·포인터 클릭과 모달의 저장 불변/뒤 입력 차단 단언은 그대로 유지한다.

- v5 정보창의 authored 배경 alpha=1을 검증하고 두비율 캡처로 뒤 HUD 글자 차폐 여부를 확인한다.

- Unity 6000.6의 `CanvasRenderer.GetMesh()` 반환 메시를 읽어 정점 수를 확인한다. 반환값은 Renderer 소유이므로 검사에서 생성하거나 삭제하지 않는다.

- 역할: 실제 authored CompetitionScreen/UIRoot를 인스턴스화해 V5 UI 계약을 PlayMode에서 검사한다. 기존 CompetitionFlowTests는 수정하지 않는다.
- 입력/출력: 실제 v5/v4 콘텐츠, 격리 CompetitionStore, 카메라 RenderTexture 720×1280/1600, EventSystem 포인터 → NUnit 결과와 고유 artifacts/v5-dice-combat-09/playmode/GUID의 체크포인트/PNG 증거.
- 검사: authored 두 영역/3카드/6면/8 legacy 버튼; 1·2·3 주사위의 실제 roll/hold/release/reroll/execute; 보관 카드 정지와 횟수/턴; 모달 6인덱스·중복·현재 눈·뒤 입력 차단·상태 불변; roll/hold/reroll/execute 취소/일시정지 Snap 및 연타 무효; all-blank NO_OP/적 행동; 12회 제한/파일 잠금 실패; safe area 하단/44px 이상/텍스트 높이; 로비·보상 진입 및 v4 저장 재개 게이트.
- 수정1회차 F01/F02 검사2개 추가(총9개): 로비/현재 전투의 V5 상세5탭 전체 페이지를 실제 포인터로 확인하고 전용 기술 계산 근거·장비/효과/태그·적 HP/보호/공격/상성/태그 및 상태 불변을 검사한다. 실행 단계 검사는 보호막이 있는 적에게 피해+보호 기술을 사용하고 시작→각 저장 SkillLog→적 행동 종료→확정 Snap의 HP바/숫자/현재 보호 일치를 프레임마다 확인한다. 기존 legacy 검사도 유지한다.
- 수정2회차 F03: Tap/Click은 coroutine으로 실제1프레임을 진행한 후 첫 raycast를 검증하고 클릭한다. 클릭 이후 즉시 IsPresenting/저장 상태를 확인하는 검사는 유지한다. 상세 페이지 읽기와 Capture도 프레임 경계를 갖는다. 실패 시 첫 hit 및 전체 hit 경로/깊이, 대상 Graphic 렌더러/depth/cull, 부모 CanvasGroup, 전역 입력 잠금 상태를 기록한다. 실패 hit를 건너뛰거나 성공까지 재시도하지 않는다.
- 수정2회차 F04: authored prefab 및 실제 인스턴스9개 face의 CanvasRenderer 존재를 GetComponent로 검사한다. Capture는 실제 프레임 진행 후 PNG를 남기고 보이는 face와 modal blocker의 렌더러/depth/cull/material/mesh 정점을 검사한다. Graphic.canvasRenderer getter로 구성 누락을 자동 추가하지 않는다. 모달 배경 중첩은 새 프레임 경계의 PNG와 렌더 준비 단언으로 메인이 재판정한다.
- 사용하는 대상: CompetitionApp.Open 격리 주입, Session/Store, UIManager/CommonButtonView, 새 Area/Net, Unity InputSystemUIInputModule/ExecuteEvents. Tap은 첫 raycast가 실제 Button인지 검사한 뒤 포인터 이벤트를 보낸다.
- 상태/수명: 각 검사 고유 artifact 경로만 사용하고 실제 사용자 저장/HTTP/백엔드를 호출하지 않는다. 테스트 fixture는 유효한 복사본을 별도 Store에 Commit/Reload해 생성한다. 생성 오브젝트/RenderTexture는 teardown에서 제거하고 증거 파일은 남긴다.
- 검수 주의: 소스 작성 완료와 실제 Unity PASS를 구분한다. 첫 authored RED는 메인 실행 채널 미연결로 NOT_RUN이며 원본의 참조 부재만 확인했다. 실제 실행/자산 업그레이드/최종 판정은 메인이 담당한다.

- V5_UI_IMPROVEMENT_11: v5 Shell의 실제 활성 버튼을 찾고 후보를 viewport에 스크롤한 뒤 첫 raycast를 검사한다. 새 라벨·친숙한 태그·기술 그룹 표시로 기대 문구를 변경하되 상태/RNG/저장/HP단계 단언은 유지한다. 상세 본문은 실제 Shell text에서 읽고 구형 경기는 기존 패널을 검사한다.

- UI12: authored 큐브3개/비활성 호환 holdZone을 확인한다. 실제포인터 hold/release·중단재개는 동일 rollZone 고정 부모를 기대한다. 기존 저장·조회·구형재개·확정HP로그 검증은 유지한다. 결과는 UI12 REPORT의 이번실행 근거를 따른다.

- UI12 기술 타격 시점: 새 기술 이동 연출 중에는 이전 HP를 유지하며 impact 문구가 공개된 시점의 HP/보호막 수치를 해당 저장 단계와 대조한다. 기술별 로그 순서·적 반격 후 수치는 계속 검사한다.
