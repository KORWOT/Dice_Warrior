# 커밋 검토 후속 안정화 결과

상태: IMPLEMENT / CR-01~03 적용 및 최신 검사·빌드 PASS, CFU-R01 원인 미확정 OPEN. 기준 main/8c767329eddfd8bf4c74c07c7bb4c757112681f6. 사용자 2026-09-11 권장 작업 실행 승인에 따른 [PLAN](../Plans/COMMIT_REVIEW_8C76732_FOLLOWUP_PLAN.md)을 적용한다. 이전 읽기 전용 [검토](COMMIT_REVIEW_8C76732_REPORT.md)의 사실·과거 검증 결과는 소급 수정하지 않는다.

## 적용

- CR-01: 이미 열린 clean Title 씬을 확인하고 Editor API/Undo로 별도 EventSystem 오브젝트와 그 루트 참조만 제거·저장했다. GameApplication→공용 UIRoot 입력 소유권과 기존 GUID를 유지했다. 런타임 탐색/삭제 보정은 추가하지 않았다.
- CR-02/CFP-T02: LobbyCampaignFlowTests의 ExplorationUI.layoutVersion 기대값만 2→3으로 수정하고 대응 CodeMap을 동기화했다. 기존 6개 검사·다른 assertion은 유지한다. 이전 보완2/2 이력에 사용자 후속 실행 요청으로 허용된 추가1회이며 횟수를 초기화하지 않았다.
- CR-03: 할인율 질문에 응답이 없어 추천한 일반→전설 0/5/10/15/20%를 조정 가능한 초기값으로 적용한다고 알린 뒤 기본 SO와 PrototypeAuthoring의 배율만 [1,.95,.9,.85,.8]로 변경했다. 상품·보상·가격 계산식·저장 구조는 유지했다. 사용자 후속 선택이 있으면 그 값이 우선한다.

| 새 여정 등급 | 회복약 | 재굴림 | 주사위 | 무기 |
| --- | ---: | ---: | ---: | ---: |
| 일반 | 10 | 12 | 16 | 18 |
| 고급 | 9 | 11 | 15 | 17 |
| 희귀 | 9 | 11 | 14 | 16 |
| 영웅 | 9 | 10 | 14 | 15 |
| 전설 | 8 | 10 | 13 | 14 |

Editor에서 실제 ShopRules.Price로 위 표를 확인했다(`discount-apply.json`). 기존 float→double/AwayFromZero 반올림을 그대로 사용하므로 10×.95f는9이며 인접 등급 가격이 같을 수 있다. 이전 저장의 증가 배율과 입장 시 확정한 가격은 보존한다.

## 실제 검증 기록

산출물 기준 폴더는 `artifacts/commit-review-followup/`이다. Unity 6000.6.0f1의 기존 연결 Editor를 사용했으며 중복 batchmode를 실행하지 않았다. PATH에 없는 기존 CLI를 로컬 설치 경로에서 호출했다. 초기 status 검색은0인스턴스를 반환했지만 명시적 프로젝트 editor_status와 실제 Editor API 명령은 ready/성공으로 확인했다.

- `editor-before.json`: stopped, compiled, MainStage, clean Title, 별도 EventSystem1개, runInBackground=false. `title-fix.json`: 제거 후 Title 입력0개.
- CR-01/02 변경 후 `compile.json`: completed/failed=false/errors없음, `console-before-tests.json`: 오류0.
- `play-initial.json`: 전체 PlayMode196건 중195PASS/1FAIL,344.89초. 이전 CR-01의6건과 CR-02의1건은 모두 PASS. 새 실패 CFU-R01은 CampaignFlowPolishTests.RealMouseDragFromNodeScrollsWithoutTravellingOrSaving의 스크롤 변화량0(기대>.005)이며 이 단계에서 해결로 판정하지 않는다.
- 단독 메서드명·전체 이름·fixture 이름 필터와 기존 Pipeline 직접 호출은 실제0건으로 종료됐다. list_tests는196건과 해당 이름을 반환하고 InspectFilter도 대상1개를 찾았다. 이 0건 호출들은 실행 PASS 근거로 사용하지 않는다. 공급사 패키지나 전역 설정은 수정하지 않고 확인된 전체 검사 경로를 사용한다.
- CFU-R01 원인 분리를 위해 해당 검사에만 기존 Queue/yield/방향/기대값을 유지한 진단을 넣었다. 포커스·스크롤 위치·임시 Mouse 값·입력 모듈의 포인터/드래그 상태를 실패 메시지에 기록한다. 게임 코드 변경이나 입력/저장 assertion 우회는 없다.
- 가격 변경 전 `price-red.json`: EditMode264건 중255PASS/9FAIL,48.73초. 실패는 새 할인 배율/정확가격/구저장 대조에서 기대한 증가 가격 차이이며 나머지 기존 검사는 통과했다. 이 실패를 확인한 뒤 SO/저작기 배율을 적용했다.
- `compile-discounts.json`: 가격 변경 후 completed/failed=false/errors없음. `edit-final.json`: EditMode264/264 PASS,47.72초. 실제 SO/저작 기본값, 구버전 가격 보존, 반올림·0·overflow, 구매·재개·RNG 검사를 포함한다.
- `play-diagnostic.json`: 최신 전체 PlayMode197/197 PASS,344.28초. 기존7실패, 새 할인 및 구가격 UI 구매/재개, 노드 드래그 검사도 이번 실행에서는 PASS. 입력 로직을 고치지 않았으므로 이전 CFU-R01 원인 해결이나 간헐적 실패 제거를 의미하지 않는다. 진단은 실패 메시지 전용이어서 통과 실행의 단계별 trace가 저장되지는 않는다.
- `android-status.json`: build_764100ff4c93, Android Succeeded,168.098초,오류0/경고1000. `Temp/commit-review-followup/DiceWarrior-Review.apk` 실제 파일48,662,971바이트. APK 안의 lib/arm64-v8a/libil2cpp.so 및 libunity.so를 확인했다. build report의 totalSizeBytes(모든 빌드 파일 합계)는 APK 파일 크기로 사용하지 않았다.
- 빌드 경고 분류: AI Inference/Sentis/ConvGeneric 셰이더978, Unity 컴파일 분석기15, TMP 셰이더/IL2CPP4, 미사용 필드1, 진단 심볼 설정1, Player용 RuntimePipelineConfig 부재1. 이 APK의 Player Pipeline은 비활성이다. 공급사 코드·런타임 연결·심볼 정책 변경은 하지 않았다.
- `editor-final.json`: stopped/compiled/MainStage/clean Title, background=false. `console-final.json`: 최종 Console 오류0. 의도적10초 타임아웃 검사의 과거 error 로그를 숨기거나 검사 성공으로 대체하지 않았으며 각 결과는 실제 테스트 판정으로 기록한다.

## 검토·보존·남은 항목

첫 검토1묶음에서 Main은 가격·자산·통합, effect_tests는 CR-01/02 직접 구조 및 CFU-R01 지정 진단, graph_contract_audit는 가격 계약·스테이징 검사를 담당했다. Main만 Unity와 자산을 수정했다. 서로 다른 파일을 작성했고 재귀 위임은 없다.

CR-01/02 정적 검토에서 새 필수 Finding은 없었다. Title diff는 중복 입력 블록79줄과 SceneRoots 참조1줄 삭제뿐이며 참조가 남지 않았다. UIRoot는 기본 입력 액션을 활성화하는 기존 InputSystemUIInputModule을 유지한다. SceneStructureTests의 직접 onClick 호출을 물리 키보드 검사로 확대 해석하지 않는다.

사용자 저장은 검사 전 hash를 기록했고 테스트들은 격리 TEMP 저장만 사용한다. `save-final.json`에서 실제 run.json의 SHA256이 작업 전과 같음을 확인했다. 이전 사용자 설계2개·도구 CodeMap5개·설정2개·직전 검토 문서는 편집하지 않았다. 프로젝트 자산/패키지/설정의 기준은 baseline.json이며, 이 파일은 CR-02 테스트 한 줄 적용 후 기록된 작업 중 기준이다. 실제 작업 시작 staged/tracked 변경은 없었다.

빌드가 동적 글꼴 캐시와 ProjectSettings 사전 로드 참조를 디스크에 남겼다. TMP_PreBuildProcessor의 clearDynamicDataOnBuild 경로와 InputSystem의 메모리 후처리를 원문·실제 상태로 확인했다. 시작 SHA256과 같은 원본을 확보해 승인 범위의 보존 처리로 Editor FileUtil/AssetDatabase API만 사용하여 정확한 두 파일 바이트를 복원했다. `font-cache-restored.json`: 원본 hash,문자/글리프98개,아틀라스1024,dirty=false. `settings-restored.json`: 원본 hash,preloaded0개. .meta 쓰기나 전체 SaveAssets는 하지 않았다.

`preservation-final.json`: 기준392파일 중 변경은 허용한6개뿐이며(CR-02 파일은 기준 채집 시 이미 변경), 글꼴·다른 자산·모든 기준 .meta·패키지·ProjectSettings는 기준 hash와 일치한다. HEAD는 동일하고 staged0개다. 프로젝트 소유 변경의 git diff --check는 오류없음이다.

연결 Android 기기는 `adb devices -l`에서0개였다. 실기기 터치·한손 조작·발열·메모리·OS 중단/복귀는 NOT_RUN이다. 최신 ARM64 IL2CPP 빌드 성공은 기기에서의 실행 성공을 의미하지 않는다. Git stage/commit/push는 수행하지 않았다.

| ID | 상태·근거 | 영향·해결 조건 |
| --- | --- | --- |
| CR-01 / CR-02 / CR-03 | 적용 및 최신 관련 검사 PASS | 원래7실패와 가격 방향을 보완했다. 할인율은 조정 가능한 초기값이다. |
| CFU-R01 | OPEN: 첫 전체 실행 드래그 delta0, 진단 추가 후 전체 재실행에서는 PASS. 원인과 제품 입력 회귀 여부 미확정 | 자동화 입력의 간헐적 실패 위험. 다음 실패의 focus/scroll/pointer trace로 원인을 특정하고 해당 조건에서 기존 입력·저장 불변 assertion을 유지한 수정/검증이 필요하다. 현재 private 입력 모듈 필드 진단은 설치 버전 전제다. |
| CFU-T01 | FOLLOW_UP: 필터 실행 결과0개, 전체 실행은 정상 | 필터 호출의 실제 선택 콜백과 TestRunner 전달/결과 경계를 계측해야 한다. 현재0건은 PASS 근거가 아니다. 공급사 수정 없음. |
| CFU-B01 | FOLLOW_UP: 빌드 성공/경고1000 | 대부분 AI Inference 셰이더 경고다. 패키지 포함 범위·경고 감소는 별도 작업이며 이번 변경으로 경고가 새로 발생했는지 비교 검증하지 않았다. |
| CFU-D01 | NOT_RUN: 연결 실기기0개 | 기기 연결 후 터치·성능·중단/복귀와 필요 시 개발 Player 연결을 확인해야 한다. |

횟수: 최초 검토1묶음 유지. CR-02는 이전2/2 뒤 사용자 승인 추가1회 사용. 공통 후속 보완1/2는 CFU-R01 진단 추가·전체 재검증에 사용했고 입력 원인 수정은0회다. 가격 RED→최초 적용→GREEN은 최초 구현 검증이며 별도 보완 예산을 만들지 않았다. 원인 불명 실패를 재실행 PASS만으로 닫지 않는다.

CodeMap: LobbyCampaignFlowTests, ContentExtensionTests, ContentExtensionFlowTests, PrototypeAuthoring, CampaignFlowPolishTests, artifacts의 ReviewFollowup을 직접 동기화했다. INDEX와 GDD에는 입력 소유자 및 새 여정 상점의 시험 배율만 반영한다. 기존 완료 REPORT를 다시 쓰거나 Core/저장 구조를 확대하지 않는다.
