# 커밋 검토 후속 안정화

상태: IMPLEMENT / CR-01~03 적용, 최신 전체 검사 및 Android 빌드 PASS. CFU-R01은 원인 미확정 OPEN으로 남기며 무결함 완료를 주장하지 않는다. 2026-09-11 사용자의 “그래 일단 권장 작업 진행해줘”를 COMMIT_REVIEW_8C76732_REPORT의 후속 실행 승인으로 기록한다. 기존 CR-02/CFP-T02 추가 보완 1회 승인도 이 요청에 포함한다. 이전 구현의 보완 2/2 이력은 유지한다.

## 계약과 범위
- CR-01: Title의 별도 EventSystem 오브젝트만 Editor API로 제거하고 공용 UIRoot 입력 소유자를 유지한다. 입력 관련 런타임 탐색/삭제나 구조 재설계 없음.
- CR-02: LobbyCampaignFlowTests의 ExplorationUI.layoutVersion 기대값을 2→3으로 갱신하고 1:1 CodeMap을 동기화한다. 다른 assertion을 완화하지 않는다.
- CR-03: 고등급 가격 할인 방향에 대한 선택 질문 후 독립 안정화 작업 동안 응답 기회를 두었다. 답변이 없어 사용자에게 추천한 0/5/10/15/20%를 조정 가능한 초기값으로 적용한다고 알렸다. 배율 [1,.95,.9,.85,.8], 기존 float→double/AwayFromZero 계산 유지(회복약10/9/9/9/8). 새 여정용 기본 배율만 변경하며 기존 런의 규칙·확정가격을 보존한다. 상품·보상·수량 추가 없음. 사용자 후속 선택 시 그 값이 우선한다.
- 허용 자산 쓰기: Assets/_Project/Scenes/Title.unity. CR-03 수치 확정 시에만 Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset의 shopPriceMultipliers. .meta/GUID와 다른 Scene/Prefab·SO·패키지·ProjectSettings·실제 저장 파일 보호. 자산은 Editor API/CLI만 사용.
- 허용 코드/문서: LobbyCampaignFlowTests.cs와 해당 CodeMap, ContentExtensionTests.cs/ContentExtensionFlowTests.cs와 각각 CodeMap, PrototypeAuthoring.cs:77의 기본 배율과 해당 CodeMap, 직접 INDEX/GDD 계약 문단, 이 PLAN/대응 REPORT. 테스트/저작 보조는 artifacts/commit-review-followup/에 기록하고 신규 C#은 CodeMap 동반. 과거 REPORT는 소급 수정하지 않는다.
- 기준 main/8c767329eddfd8bf4c74c07c7bb4c757112681f6. 시작 tracked/staged 변경 없음. 기존 untracked CodeMap5개·사용자 설계2개·직전 검토 PLAN/REPORT·설정2개 보존. 실제 기준 hash·Editor 상태·결과는 artifacts/commit-review-followup/에 기록한다. Git stage/commit/branch/push 없음.

## 검증·완료
- 기존 실제 196건 중7실패를 RED 근거로 사용한다. 변경 후 해당 7건을 포함한 직접 검사, 최신 전체 EditMode/PlayMode 실행과 Console 오류 확인. 새 통합 실패는 원인을 확인하고 기존 assertion을 우회하지 않는다.
- Title→Lobby→InGame·메뉴·재개 입력 소유자, 포인터/드래그/모달 동작은 기존 실제 UI 시험을 우선 사용한다. CR-03 적용 시 등급별 가격 비증가, 구매/저장 재개 및 변경 전 저장가격 유지 검사.
- 가능하면 설치된 기존 Editor에서 최신 Android ARM64 IL2CPP 빌드. 물리 기기 연결 여부를 읽기 전용 확인하고 기기가 없으면 실기기 검사는 NOT_RUN으로 남긴다. 같은 프로젝트 중복 batchmode/잠금 삭제/전역 설정 변경 없음. Editor 상태를 확인하고 사용자 미저장 변경을 보존한다.
- 최초 검토1묶음. CR-02는 이전 작업의 추가1회 보완이며 횟수 초기화 없음. CR-01 및 확정될 가격 변경의 최초 산출물 이후 보완 최대2회; 공통 후속 검토에서 집계하고 별도 검토 예산을 늘리지 않는다.
- Main만 Unity 실행·자산·공통 문서·최종 판정. 기존 effect_tests에 CR-02 test/CodeMap만 단독 쓰기 및 후속 지정 diff 검토를 위임한다. 기존 graph_contract_audit에는 CR-03의 수치 독립적인 테스트/호환성 변경 지점과 빌드 경로를 읽기 전용 확인하도록 위임한다. 최대 동시3, 고유 보조2, 재귀0. 동시 파일 작성 금지.

인계: CR-02 수정 후 effect_tests 쓰기는 종료. graph_contract_audit의 직접 경로 조사 후 할인 계약이 위와 같이 확정되어 ContentExtensionTests.cs/ContentExtensionFlowTests.cs 및 각 CodeMap 4개를 단독 작성하도록 인계한다. 현재 진행 중인 Unity PlayMode를 방해하지 않도록 먼저 artifacts/commit-review-followup/price-stage/에 작성하고 Main의 적용 지시 후에만 Assets/Docs 대상에 반영한다. Main은 해당4파일 동시 작성 금지, SO/PrototypeAuthoring/공통 문서만 담당한다.

최초 후속 실행: PlayMode196중195PASS/1FAIL. 기존CR-01 6건·CR-02 1건은 PASS. CFU-R01: RealMouseDragFromNodeScrollsWithoutTravellingOrSaving의 실제 scroll delta가0(기대>.005). Title을 직접 거치지 않는 검사이며 원인 미확정이다. 새 직접 회귀 근거에 한해 CampaignFlowPolishTests.cs와 해당 CodeMap의 비침습 진단 및 확인된 테스트 입력 원인 최소 보완을 추가 허용한다. effect_tests가 이2파일을 단독 작성하고 Main은 Unity 실행/계측 결과 판정만 담당한다. 단일/전체 메서드명/fixture CLI필터는 실제0건으로 종료되어 PASS로 취급하지 않으며 기존 전체 실행 경로를 사용한다. 공급사 Pipeline 패키지 수정 없음. 이 보완은 공통 후속 보완 상한2회 안에서 누적하며 기존2/2 이력을 지우지 않는다.

빌드 보존 범위: 설치 TMP_PreBuildProcessor가 clearDynamicDataOnBuild=true인 Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Effects SDF.asset의 문자/글리프/아틀라스 캐시를 제거하는 직접 부작용을 확인했다. 빌드 종료 후에도 남으면 Main이 작업 시작 hash와 일치하는 원본 바이트를 UnityEditor.FileUtil.ReplaceFile/AssetDatabase.ImportAsset으로 해당 파일에만 복원한다. 신규 글꼴 설정/내용 변경은 하지 않으며 .meta는 쓰지 않는다. 소스 hash·교체 전 대상 hash·stopped/compiled/no-build/clean 상태를 확인하고 다른 변경이 있으면 중단한다. ProjectSettings의 사전 로드 항목은 InputSystem 빌드 후처리의 자동 복원을 먼저 확인한다. 이 절은 승인된 검증으로 생긴 부작용 보존 처리이며 기능 범위나 기존 수정 예산을 새로 만들지 않는다.

빌드 종료 후 PlayerSettings.GetPreloadedAssets()는 원래 빈 배열로 복원됐지만 ProjectSettings/ProjectSettings.asset 디스크에는 빌드의 추가 참조 한 줄만 남았다. 해당 정확한 diff와 시작 hash가 같은 원본을 확인했다. 메모리 배열이 비어 있고 디스크 hash가 확인값과 같을 때만 Main의 Editor FileUtil로 원본 바이트를 복원하는 것을 허용한다. 다른 PlayerSettings 값 쓰기나 전체 SaveAssets 호출은 하지 않는다.

최종 기록: EditMode264/264(47.72초), PlayMode197/197(344.28초), Android ARM64 IL2CPP 빌드 성공(168.098초/오류0/경고1000). 빌드 부작용2파일은 시작 hash로 복원했고 실제 사용자 저장·GUID·패키지·설정을 보존했다. CFU-R01은 진단만 추가한 재실행에서 미재현이며 입력 원인 해결로 판정하지 않는다. 공통 후속 보완1/2 사용(진단 추가와 전체 재검증), 원인 수정0회. CR-02의 승인된 추가1회 사용 및 이전2/2 이력 유지. 실기기 NOT_RUN, Git stage/commit/branch/push 없음. 상세 증거와 OPEN/FOLLOW_UP은 대응 REPORT를 따른다.
