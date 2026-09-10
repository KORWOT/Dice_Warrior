# CampaignFlowPolishTests.cs

- 역할: 현재 노드 복귀·한 번 탭 이동·전투 진입 완료 후 주사위 표시의 새 사용자 계약을 실제 원본과 플레이 흐름으로 검증한다.
- 초기 RED: ExplorationUI 원본의 별도 이동 버튼 비표시, CombatUI 실제 진입 연출/바인딩 소유 API 검사 2개를 그대로 유지한다. Main이 실제 RED 2실패를 확인한 뒤 통합 UnityTest 9개를 추가해 총11개다.
- 입출력/사용 대상: 실제 GameApplication/ExplorationUI/CombatUI/DiceRollUI/FateChoiceUI 원본, 제작 InGame 씬, seed33·절차 지도1/5열/5경로/10층의 config snapshot, 격리 LocalRunStore를 감싼 CountingStore다. 원본 SO와 게임 규칙 소스는 변경하지 않는다.
- 단일 이동: 실제 Raycaster 포인터로 첫 탭의 저장1회·Busy·도착 전 popup 없음, 이전 버튼 callback 및 오래된 command token의 무효를 검사한다. 저장 IOException은 예상 Warning 한 개로 요구하며 phase/marker/RNG/bytes 불변과 재시도를 확인한다.
- 지도 복귀: Combat/Rest/Treasure/Event/Shop 각 유형을 두 세로 비율에서 실제 UI 명령으로 처리한다. 세 사건을 실제 규칙으로 완료한 저장을 재개해 현재 노드와 다음 available 행의 viewport 포함을 검사하고, 상단으로 스크롤한 캐시 지도에 복귀해 초점 회복과 모든 저장 좌표 고정을 확인한다. 수동 스크롤 이후 RefreshView가 위치·저장/RNG를 바꾸지 않아야 한다.
- 전투 진입: Fate 선택·confirm 저장1회 후 선택 퇴장→실제 IsEntryPlaying 중 popup 없음→완료 후 DiceRollUI 순서를 두 비율에서 요구한다. CombatRoll의 timeScale0 재개는 실제 .7초 진입 뒤 미굴림 창을 열며 새 저장/난수를 소비하지 않는다. 다음 턴/유료 재굴림에는 entry가 반복되지 않고 CombatCards 재개는 자동 진입/roll 없이 동일 offered ID를 보존한다.
- 수명/소유권: controller cancel/disable, CombatUI component/gameObject disable 시 원래 scale/rotation/position·색·임시 문구 복원을 검사한다. 비활성 View에 새 dice 창을 만들면 실패한다. 같은 캐시 CombatUI에 새 Data/entry와 새 DiceRollUI를 바인딩한 뒤 옛 controller cancel/disable이 새 두 뷰와 문구를 보존해야 한다. 수동으로 구동한 새 entry iterator도 finally에서 dispose한다.
- watchdog: 실제 timeScale0에서 9.4초까지 Busy/entry와 popup 부재, 10~10.8초에 TimedOut/종료1회/정확한 오류 로그1개 및 고정된 CombatRoll 저장·RNG와 미굴림6면 복구를 검사한다. Playback.Started의 공개 이벤트에서 cold entry 시작 전 runtime entrySeconds=20을 설정한다.
- 실제 drag: 설치된 Unity.InputSystem의 AddDevice/MouseState/QueueStateEvent/RemoveDevice를 reflection으로 호출해 임시 Mouse의 down→threshold 이상 이동→다음 프레임 추가 이동→up을 실제 input module에 전달한다. 첫 이동에서 ScrollRect가 드래그 시작점을 저장하므로 두 번째 이동까지 보내 실제 스크롤을 검사한다. 스크롤이 실제 움직여야 하며 이동 명령·저장은 없어야 한다. 새 device만 제거하고 이전 current Mouse를 복원하며 asmdef/input 설정은 바꾸지 않는다.
- 캡처 산출: 실제 실행 시 artifacts/campaign-flow-polish/entry-1280.png, entry-1600.png와 returned-map-1280.png, returned-map-1600.png를 ScreenCapture.CaptureScreenshotAsTexture로 저장한다. WaitForEndOfFrame 뒤 캡처하며 생성 Texture는 finally에서 즉시 Destroy한다. 캡처는 시간 oracle를 완화하지 않으며 timeout 검사에는 포함하지 않는다.
- 사용하는 쪽: Main의 CAMPAIGN_FLOW_POLISH 통합 검증. 원본 변경·신규 C# meta 작성은 Main Unity Editor 작업만 수행한다.
- 상태/수명/검수 주의: 자기 app을 제거하고 사용자 기본 저장은 존재/bytes 관찰만 한다. 자기 Temp 정규화 경계 안의 디렉터리만 정리하고 timeScale을 복원한다. 기존 EventSystem 6실패를 기대 로그로 바꾸거나 전역 객체를 제거하지 않는다. 담당은 Unity/컴파일을 실행하지 않았고 확대11검사의 실제 PASS/FAIL과 캡처 생성 여부는 Main의 CAMPAIGN_FLOW_POLISH_REPORT를 따른다.
