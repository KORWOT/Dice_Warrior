# 절차적 캠페인 지도·운명 선택 팝업

상태: **신규 기능 구현·필수 검증 완료 / 전체 프로젝트 회귀는 기존 Title 결함 6건으로 미통과**. 사용자 ‘추천방안으로’ 승인에 따라 [PLAN](../Plans/PROCEDURAL_CAMPAIGN_PLAN.md)의 신규 지도·공개 범위·팝업 계약을 적용했다. 기준 HEAD d8df4999174ad63bb73e0727ff0149cb55624db2, 기존 main 작업 폴더를 유지했다. Git stage/commit/branch 변경은 없다.

## 적용한 동작
- 새 여정은 전체 층별 경로를 한 번 생성한다. 기본 5열·5경로·일반10층+마지막 단일 보스이며 분기/합류·가변 선택지·교차선 방지·모든 경로의 보스 도달성을 검사한다. mapGenerationVersion1/설정/initialSeed가 같으면 같은 좌표·유형·연결이다. 지도 지역 난수는 게임 rngState를 추가 소비하지 않는다.
- 전체 그래프와 좌표는 저장되고, 가까운 다음2층까지만 유형을 공개한다. 보스 표식은 항상 공개한다. DTO에서 먼 type은 null이며 legacy nodes 필드로 비공개 정보가 우회 전달되지 않는다. 완료/버린 경로는 지도에 구분하여 유지한다.
- 노드를 누르면 설명과 선택 테두리만 갱신한다. 하단 이동 버튼에서 ChooseNode를 한 번 확정하고 도착 연출 후 주사위 창이 열린다. 유형 아이콘·점선·현재 위치·접는 범례·실제 진척/골드/체력/수호를 표시한다.
- 주사위 결과 뒤 별도 FateChoiceUI가 지도를 덮는다. 3장의 카드(4~5장은 가로 스크롤)를 살펴보고 선택한 뒤 하단 버튼으로 확정한다. 구체적인 적/보상은 미리 노출하지 않는다. 닫기/다시 열기는 후보와 RNG를 유지하고, 유료 재굴림은 기존 규칙으로 후보를 새로 만들어 선택을 해제한다.
- 선택 대기는 연출 타이머 밖이다. 확정 성공 후 실제 FEEL 종료→팝업 퇴장 종료→사건 표시 순서로 진행한다. 기존 PresentationPlayback의 시작/종료·10초 watchdog·강제 종료 로그와 확정 상태 복구를 사용한다. 이전 연출이 재바인딩한 팝업을 닫지 않게 소유 버전을 확인한다.
- 구형 저장은 누락 필드0을 legacy로 해석하고 기존 생성·RNG·보스 전환 방식 그대로 이어간다. 기존 파일에 새 그래프를 강제로 덧씌우지 않는다. 새 모드는 새 여정/새 작업실 미리보기부터 적용된다.

## 편집 위치
- `Fate Dice > 플레이 작업실`에서 캠페인 지도/운명 카드 시작점을 선택하여 실제 제작 원본을 정지 상태로 볼 수 있다. ‘UI 원본 편집’은 각각 ExplorationUI/FateChoiceUI를 연다.
- `Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset`: world.mapGenerationVersion=1, mapColumns=5, mapPathCount=5, previewDepth=2. 일반층 수는 기존 eventsToBoss이다.
- `ExplorationUI.prefab`: HUD/범례/설명/이동·footer 배치. `ExplorationNodeView.prefab`: 노드 링/도형·텍스트·터치 영역. CampaignMapView.rowSpacing/arrivalSeconds로 세로 간격/도착 연출을 편집한다.
- `FateChoiceUI.prefab`: 팝업·카드 목록·버튼·6재굴림 행 및 exitSeconds. `FateCardView.prefab`: 유형/등급/공개 설명·유형 도형/선택 테두리·기존 FEEL. 새 이미지가 있으면 기존 visual catalog의 artwork/icon이 도형보다 우선한다.
- `Fate Dice > 절차 지도와 운명 선택 창 적용`은 승인된 원본만 초기 작성한다. 적용된 버전과 사용자 편집값을 유지하며 원본을 매 실행마다 재생성하지 않는다.

## 실제 검증
| 항목 | 결과 | 증거 |
|---|---|---|
| 신규 계약 RED | map1, popup1, Core26의 실제 실패 확인 | artifacts/procedural-campaign/*-red.json |
| 절차 Core | 26/26 PASS | core-green.json |
| 팝업 입력/두 비율/스크롤/FEEL | 7/7 PASS | popup-initial.json |
| 신규 통합 | 최초7/10 → 보완10/10 PASS | flow-initial.json, flow-fix1.json |
| 기존 EditMode | 최초241/255 + 실패 재연14/14 재실행 PASS. 최종255 일괄 재실행은 하지 않음 | edit-initial.json, replay-final.json |
| 전체 PlayMode | 최초171/185 → 최종179/185 PASS, 6 FAIL, 0 skipped, 301.12초 | play-initial.json, play-final.json |
| 최종 신규 팝업/통합 | 전체 PlayMode 안에서7/7 및10/10 재확인 PASS | play-final.json |
| 실제 작업실 캡처 | 720×1280/720×1600에서 지도 시작/보스/선택팝업6장 최종 렌더·열람, 마스크/한글/하단 버튼 확인 | map-current-*, map-boss-*, fate-selected-* |
| 원본 재적용 | 대상6자산 재적용 전후 SHA256 모두 동일 | idempotence-before.json, idempotence-final.json |
| 문서/메타/공백 | 프로젝트 C#113개 CodeMap·meta 누락0, 기존meta190개 동일. 수정 C#/MD61개 diff check PASS | final-protection-audit.json |

모든 결과 파일은 `artifacts/procedural-campaign/`에 있다. 신규 통합10은 실제 포인터 raycast로 지도 미리보기→이동→굴림→선택/확정→사건→보상→지도, 저장1회/상태·RNG oracle, 실패 재시도, 닫기/재열기, 재굴림, cancel/disable/rebind를 검사한다. 선택 생각 시간10.1초 무타임아웃 및 실제 연출10초 timeout/단일로그/복구를 확인했다.

## Finding과 수정 상한
- PC-R01: JsonUtility가 Map의 null selectedNode를 빈 객체로 복원. ID 유효성으로 마지막 완료 노드와 eventsResolved에 복귀하도록 수정했고 3층 실제 디스크 왕복 통과.
- PC-R02: disable/재굴림 종료가 새 Fate 바인딩을 닫거나 Render로 덮음. 종료 전 소유 바인딩을 보존하고 새 소유자는 건드리지 않게 수정, 취소/disable/재굴림 통과.
- PC-V01: custom Graphic이 마스크 밖으로 렌더. 두 Graphic을 MaskableGraphic으로 수정, 재캡처에서 HUD/하단 영역 넘침 해소.
- PC-V02: 선택 테두리의 center fill이 카드 텍스트를 가림. 중앙을 비우고 공개 유형 도형 fallback 추가, 재캡처에서 내용/등급이 보임.
- PC-R03: 기존 canonical hash에 신규 직렬화0필드가 추가되어 구형 재연14개 실패. 새 필드의 존재·정수0을 먼저 검증한 뒤 해당 필드만 역사적 투영에서 제외했다. 기존 golden fixture/기대 hash/기존 전체 상태/RNG 비교는 유지했고 Unity14/14 PASS.
- PC-V03: 하단 footer와 rollChoices의56 높이가 공통 버튼 최소102보다 작아 작은 세로 화면에서 버튼이 잘림. 두 행을108로 작성했고 미래 legacy 노드 높이는92로 조정했다. 관련 실제 UI/한글 높이 검사 및 최종 캡처에서 직접 회귀 해소.
- 최초 통합 검토1묶음; 보완2/2 사용. 신규 필수 Finding은 해결했다. 이전 완료 연출 작업의 예산이나 판정을 재설정하지 않는다. 실행 전에 reload가 겹쳐 pending으로 남은 테스트는 cancel 후 재개했으며 PASS로 세지 않았다. 최초 캡처 eval 호출은 클래스 선언 carrier 오류였고, run_script로 실행하여6장 생성했다.

## 기존 Title 결함 — ASSET_HANDOFF / 전체 PASS 아님
- PC-B01 근거: Title.unity:214–292에 별도 활성 EventSystem이 있고 UIRoot.prefab:184–230의 Input/EventSystem과 중복된다. SceneStructureTests:83의 단일 EventSystem 검사가 실제2개를 확인했다. 직전 앱/Canvas 단일 검사는 통과했다. Title SHA는 작업 시작과 동일하고 UIRoot 변경은 FateChoiceUI 등록1건뿐이다. GameApplication/SceneEntry/SceneFlow는 수정하지 않았다.
- 최종 실패6개: KoreanUiTests의 AuthoredApplicationUsesPretendardFont, CompletedWinAndLossSummariesStayKoreanAndLeaveTheSavedResultsIntact, TitleLobbyExplorationAndRecoverableErrorsUseKoreanWithStableCommands; SceneStructureTests의 InvalidDestinationLeavesCurrentViewUsableAndStoreUntouched, ProductionJourneyUsesRealViewsThroughResultAndReturnsToLobby, TitleLobbyNewRunAndRepeatedContinueKeepOneOwner.
- 최초5개가 이 중복으로 실패했으며, 한글 흐름1개는 노드 높이 회귀를 해결한 뒤 동일 중복 경고까지 진행해 최종6개다. 모두 play-final.json에 원문 메시지/stack trace가 있다. 테스트 순서별 경고 발생 원인은 일부 추론이며, 보호 Title의 중복 자체와 단일 EventSystem 실패는 실제 확인이다.
- 영향/해결 조건: Title 진입·장면 왕복에서 UI 입력 시스템이 하나여야 한다. 별도 자산 작업에서 Title의 중복 소유를 정리하고 해당6개 및 장면 왕복 회귀를 재실행해야 한다. 이번 PLAN은 씬 쓰기를 허용하지 않아 Title을 수정하지 않았다. 경고 무시나 테스트 검증 완화로 PASS 처리하지 않았다.

## 보존·문서·남은 범위
최종 보호 검사는 final-protection-audit.json에 기록했다. Title/manifest/package lock/기본 사용자 저장 SHA256이 시작 시점과 동일하고 기존meta190개도 그대로다. 기존 자산 변화는 허용5개에 한정하며 신규 FateChoiceUI가 추가됐다. DefaultFateDice 원본 diff는 mapGenerationVersion/mapColumns/mapPathCount 세 줄만 추가됐고, previewDepth는 기존2를 유지한다. UIRoot는 popup 등록만 바뀌었다. 새 stage/commit은 없고 HEAD도 동일하다.

검증 중 ProjectSettings.asset에 생긴 runInBackground:0→1은 Unity PlayerSettings 값을0으로 복원하고 해당 직렬화 속성 한 항목만 파일에 반영하여 Git diff가 없어졌다(settings-restored.json). 다른 dirty 설정/자산을 함께 저장하지 않았다. CommonButton 원본의 일시 hash 차이는 기존 스타일 전파 검사의 Bold 적용/finally 복원이며, 종료 후 시작 SHA와 같다. 최종 Editor는 정지·컴파일 아님·Title 씬 dirty=false(editor-final.json). Unity가 작성한 prefab의 빈 YAML 문자열 뒤 공백은 직렬화 결과 그대로 유지했으며 수동 공백 정리는 하지 않았다.

C#113개에 대응 CodeMap을 유지하고 기능 INDEX와 GDD/Architecture의 승인된 직접 문단을 동기화했다. 주요 역할은 ProceduralMapGenerator→ExplorationRules→RunSession/Validator→CampaignMapProjection→ExplorationUI/CampaignMapView, RunUIController→FateChoiceUI→기존 PresentationPlayback이다.

이번 검증은 Unity Editor의 실제 제작 프리팹·포인터·파일 저장과 정지 렌더이다. Android 실기기 터치/성능/빌드, 새 일러스트 제작, 엘리트·추가 재화·다구간·영구 성장은 실행하거나 구현했다고 주장하지 않는다.
