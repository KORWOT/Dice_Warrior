# 캠페인 복귀·즉시 이동·전투 진입 개선

상태: IMPLEMENT / 새 기능 검증 PASS, 기존 검사 기대값 갱신1건에 대한 추가 보완 허용 대기. 사용자 요청의3동작을 [PLAN](../Plans/CAMPAIGN_FLOW_POLISH_PLAN.md)에 따라 변경했다. Git stage/commit/branch 변경 없음. 기준 HEAD d8df4999174ad63bb73e0727ff0149cb55624db2와 기존 미커밋 변경은 artifacts/campaign-flow-polish/baseline.json에 기록했다.

## 적용 동작
- 이동 가능한 노드를 누르면 ChooseNode를 한 번 저장하고, 도착 연출이 끝난 뒤 탐험 주사위 창을 연다. 이동 버튼은 원본·런타임에서 숨기며 serialized 참조를 유지한다. 팝업/입력 잠금·중복·stale·불가 노드 검사를 유지하고 저장 실패는 재바인딩으로 재시도 가능하게 한다.
- 지도는 다른 화면에서 돌아오거나 current/topology가 바뀌면 활성화 후 최종 레이아웃에서 현재 노드·플레이어·다음 가능 노드를 한 번 중앙에 맞춘다. 이후 수동 스크롤과 같은 화면 RefreshView는 강제로 초기화하지 않는다. 기존 ScrollRect/InputSystem의 drag-click 억제 구조를 유지한다.
- 전투 첫 진입은 전투 화면과 적 이름 표시→무대/적 확대·페이드와 약한 금빛→실제 종료→전투 주사위 창 순서다. CombatUI 원본의 entrySeconds=.7, entryIntensity=.06으로 기본 작성했으며 Editor에서 편집한다. Controller에 고정 대기 시간을 추가하지 않았다.
- Fate 확정·보스 노드 도착은 기존 PresentationPlayback 안에서 진입까지 기다린다. Cold CombatRoll 저장 재개는 별도 입력 잠금과 Playback을 사용하며, CombatCards 저장/다음 턴/재굴림에는 진입을 반복하지 않는다. 실제10초 watchdog/로그/확정 저장 복구를 유지한다.
- 원본 적용 메뉴는 `Fate Dice > 즉시 이동과 전투 진입 연출 적용`. ExplorationUI version3와 CombatUI entryVersion1을 한 번만 작성하여 사용자 편집을 보존한다. Scene·게임 규칙·저장 schema·패키지는 수정 대상이 아니다.

## 검토·실행 증거
- 초기 RED2/2 실패(red.json): 실제 원본의 이동 버튼이 활성, CombatUI 진입 완료 API 없음.
- 통합 source compile 완료/오류0 확인 후 지정2원본 적용(authoring.json).
- 최초 통합 검토1묶음: Main 지도·원본, 보조 전투/Controller 수명. CFP-R01(이전 Controller disable이 새 Combat binding의 popup을 닫음), CFP-R02(CombatUI만 disable해도 종료 뒤 popup을 엶) 보완1묶음. 새 소유자 정리 제외와 view.isActiveAndEnabled 가드를 적용했다. 실제 검증 대기.
- 최초 확대 실행 `play-new-initial.json`: 11개 중9 PASS/2 FAIL. CFP-R01/R02의 실제 수명·복원·새 바인딩 검사는 PASS. 실패2개는 CFP-R03(하단 앵커 y를 중앙 pivot 좌표로 바꿔 지도 초점이 절반 높이 위로 어긋남), CFP-T01(검사가 BeginDrag 프레임의 이동만 보내 실제 scroll delta가0)이다.
- 보완2/2: FocusCurrent가 nodeLayer.rect.yMin+y를 로컬 좌표로 변환하도록 최소 수정. 설치 InputSystemUIInputModule:758–781/ScrollRect:723–725,796–797 근거에 따라 Mouse 검사만 두 번째 held move를 추가하고 저장·RNG·스크롤 assertion은 유지했다. 생산 입력/설정 변경 없음. 이후 전체 PlayMode를 실행한다.
- 최종 전체 PlayMode `play-final.json`: 196개 /189 PASS /7 FAIL /344.79초. 새 CampaignFlowPolishTests 11개는 모두 PASS. 지도5유형×2비율 복귀, 수동scroll 보존, 첫 탭·중복·저장실패 복구, 실제 Mouse drag, 전투 선택/진입 순서, 재개/다음턴/재굴림, cancel/disable/새 바인딩 보존, 실제10초 timeout·로그·미굴림 저장 보존이 통과했다.
- CFP-T02: LobbyCampaignFlowTests.PreparationLobbyAndSixDicePopupHaveAuthoredContracts의 :72가 이전 layoutVersion2를 요구하지만 승인된 즉시이동 원본은3이다. 남은 조치는 해당 기대값2→3와 직접 CodeMap 동기화 및 해당 검사 재실행이다. 보완2/2를 사용했으므로 AGENTS.md:37–38에 따라 사용자에게 추가1회 허용을 요청했다. 아직 수정·재실행하지 않았으며 전체 PASS로 표기하지 않는다.
- 나머지6실패는 이전 PROCEDURAL_CAMPAIGN의 실제 실패 이름과 일치한다. KoreanUiTests3개와 SceneStructureTests3개에서 Title의 별도 EventSystem과 UIRoot의 InputEventSystem이 동시에 활성화된다. Title/UIRoot 바이트는 이번 baseline과 동일하다. 이 기존 문제는 보호 자산·이번 범위 밖으로 남긴다.
- 실제 `returned-map-1280.png`, `returned-map-1600.png`, `entry-1280.png`, `entry-1600.png`를 생성·열어 확인했다. 현재 노드·다음 가능 행이 보이며 별도 이동 버튼이 없다. 진입 캡처는 시작 페이드의 초기 프레임이고 주사위 팝업이 없다. 정적 원본 미리보기2장도 생성·확인했다.

## 문서·보존
RunUIController/CombatUI/ExplorationUI/CampaignMapView/CampaignMapAuthoring 및 새 CampaignFlowAuthoring과 직접 테스트 CodeMap, INDEX/GDD의 새 입력·진입 문단을 동기화했다. 프로젝트 C#115개는 모두1:1 CodeMap과meta가 있다. 대상 git diff --check PASS.

`preservation.json`: baseline 대비 변경된 기존 자산은 허용2프리팹뿐이다. 기존meta/GUID, Title, UIRoot, DefaultFateDice, Packages두파일, ProjectSettings, 사용자 실제run.json hash가 같다. `idempotence.json`: 재적용 메뉴가 두 프리팹의 기존 편집값과 SHA256을 그대로 유지한다. 실제 Editor는 stopped/clean Title/MainStage/runInBackground=false로 확인했다. Android 실기기 성능·빌드·새 일러스트 제작은 이번 검증 대상이 아니다.
