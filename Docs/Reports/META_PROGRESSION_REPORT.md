# 메타 원본·출전·정산·성장 구현

상태: **IMPLEMENT / COMPLETE — 전체 EditMode283/283, 최종 PlayMode200/200 PASS. 필수 Finding 모두 CLOSED.** [PLAN](../Plans/META_PROGRESSION_PLAN.md). 기준 main/8c767329eddfd8bf4c74c07c7bb4c757112681f6. 기존 변경은 보존하며 commit/staging/branch 변경 없음. 최초2회 및 BLOCKED 이력을 보존하고 2026-09-12 사용자 승인 추가1회로 누적3/3 안에서 완료했다. EditMode는 제품 코드가 고정된 이전 통과 증거이며 최종 추가 실행은 PlayMode 전체다.

## 구현 계약과 사용

기본 제작 씬 GameApplication은 LocalDevelopment 프로필을 사용한다. 캐릭터/와일드 카드/장비 개체/주사위 개체의 영구 보유 데이터와 로비 출전 구성을 저장한다. 시작 시 소유/슬롯을 검증하고 영구 성장 효과와 정산 정책을 값으로 고정한 독립 RunSession을 만든다. 기존 런 내부 골드·장비·주사위는 프로필로 반출하지 않는다.

로비의 세팅에서 보유한 장비 또는 여분 주사위가 있으면 해당 슬롯 버튼을 눌러 순환 선택한다. 없는 장비는 장착 없음, 기본 주사위만 있는 슬롯은 장착 중으로 표시한다. 현재 캐릭터 콘텐츠는 모험가 한 명이며 보유 목록/선택 구조를 갖췄다. 개별 장비·주사위 강화와 신규 캐릭터 콘텐츠는 미구현이다.

결과 화면의 ‘정산 확인 후 로비로’에서 성장 재화를 확정한다. 결과 표시/새로고침만으로 지급하지 않는다. 결과가 미정산이면 새 출발은 막고 로비의 ‘완료 여정 정산하기’에서 결과로 돌아갈 수 있다. 진행 중 ‘현재 여정 포기 후 새 여정’은 영구 보상 없이 기존 런을 포기한다. 로비 성장 버튼에서 재화를 소비하고, 적용은 다음 새 런부터다.

Editor 설정: `Assets/_Project/Features/Meta/Configs/DefaultMetaProgression.asset`의 초기 보유 장비/여분 주사위/성장 재화 및 정책을 편집한다. 초기 보유품은 신규 프로필에만 적용된다. 정책 기본값은 사건당 1, 승리 추가 10, 비용 10×다음 단계, 최대20단계, 단계당 HP+2/위력+1/방어+0의 시험값이다. 현재 진행 중 런의 정산 정책은 시작 시 복사되어 바뀌지 않는다.

메타 저장은 `persistentDataPath/FateDiceMeta/<profileId의 SHA256>/player.json`이다. 프로필·현재 런·시작 기록·정산 receipt·명령 receipt를 한 파일의 원자 교체로 확정한다. 문서 revision과 OS 파일 잠금으로 충돌을 감지하고 프로필 revision으로 오래된 명령을 거부한다. 같은 runId는 한 번만 지급하고 요청 종류/입력 fingerprint가 다른 requestId 재사용은 거부한다. 읽기는 파일을 만들지 않으며 첫 실제 변경 때 저장한다.

구형 `FateDiceLocal/run.json`은 원본을 수정하지 않고 이어하기용 독립 사본으로 이관한다. 구형 런은 영구 시작 기록이 없어 정산액0이며 결과에 이유를 표시한다. 손상 프로필은 자동 초기화/삭제하지 않고 UI를 중단한다. 파일 보관/클라우드 백업 복구 UI는 후속 범위다.

## Firebase 후속 연결 계약

현재 Firebase SDK·Auth·Firestore·Cloud Functions·App Check를 설치하거나 배포하지 않았다. **로컬 개발 구현은 핵/위변조 방지의 완성이 아니다.** 로컬 체크섬은 손상 감지용이며 사용자가 파일과 체크섬을 함께 바꿀 수 있다. LocalDevelopment 데이터의 잔액/소유권을 실제 계정에 자동 승인·승격하는 경로는 없다.

`IMetaProgressionService`가 교체 경계다. 요청은 선택 ID/requestId/expectedRevision으로 제한하고 클라이언트가 보상량·비용·잔액·신뢰할 UID를 지정하지 않는다. 실제 서버 단계에서 다음을 구현해야 한다.

1. 인증된 Auth UID로 프로필 경로와 소유권을 결정하고 Callable Function에서 인증/App Check를 검증한다. SDK가 제공하는 토큰 전달은 검증된 게임 결과와 같은 뜻이 아니다. [Firebase Callable Functions](https://firebase.google.com/docs/functions/callable)
2. 새 출발은 서버가 콘텐츠 버전·소유 구성·성장 상태·runId/시작 티켓을 확정한다. 검증 가능한 명령/체크포인트와 난수 정책을 정하고, 위조한 완료 플래그/이벤트 수만으로 영구 보상을 승인하지 않는다. 오프라인 런 결과의 신뢰/반출 정책도 이때 별도로 확정한다.
3. 정산은 서버가 검증한 결과와 서버 정책으로 계산하고 profileId/runId 영수증과 잔액을 같은 Firestore transaction으로 확정한다. 성장도 서버가 소유·잔액·가격을 확인한다. [Firestore transactions](https://firebase.google.com/docs/firestore/manage-data/transactions)
4. 클라이언트의 영구 잔액·소유 목록·정산 문서 직접 쓰기를 Rules로 차단한다. 서버 SDK는 Rules를 우회하므로 서버 코드 검증/IAM도 필요하다. [Firestore security guidance](https://firebase.google.com/docs/firestore/security/insecure-rules)
5. App Check는 비정상 클라이언트 요청을 줄이는 추가 수단이며 결과 검증·권한 확인·재시도 중복 방지의 대체물이 아니다. [App Check](https://firebase.google.com/docs/app-check)

## 실행 증거

- 신규 경계 테스트 RED: 전체 EditMode265개 중 기존264 PASS, 신규1개만 `Missing meta boundary: IMetaProgressionService`로 실패. `artifacts/meta-progression/red-result.json`.
- 구현/테스트 컴파일 완료, authored Apply 성공. 마지막 컴파일 결과 failed=false.
- 최초 전체 EditMode 281/281 PASS, 166.68초. 신규 메타 17개를 포함한다. `artifacts/meta-progression/edit-initial.json`.
- 최초 전체 PlayMode 199개 중197 PASS/2 FAIL, 369.57초. 신규 보유품→출전 버튼 PASS. 신규 결과→성장 테스트는 실제 런 레벨업 후 HP106을 초기 HP90과 비교한 오라클 오류이며 완료 당시 HP로 수정한다. 기존 CFU-R01 지도 드래그 회귀도 virtual mouse enabled=false/position0 상태로 실패했다. `artifacts/meta-progression/play-initial.json`.
- META-R1은 수정 전 실제 NUnit fixture의 SetUp/충돌 메서드/TearDown을 Editor run_script로 호출해 `Expected MetaCommandException but was null` RED를 재현했다. TestRunner 전체 실행과 별도인 지정 메서드 실행이다. `artifacts/meta-progression/conflict-red.json`.
- 1차 수정 후 전체 EditMode **283/283 PASS**, 222.18초. 메타19개(소유 검증 매개변수 사례 포함)와 META-R1 충돌 회귀를 포함한다. `artifacts/meta-progression/edit-round1.json`.
- 1차 수정 후 전체 PlayMode **199/200 PASS**, 383.54초. 신규 메타3개(720×1280 보유 장비/주사위/와일드 선택→출발, 1080×2400 결과 정산→성장→다음 출발, 720×1280 출발 응답 유실→설정 변경→원래 런 이어하기)는 모두 PASS. 실제 레이캐스트/포인터 버튼 경로를 사용하며 메서드 직접 호출만으로 UI PASS를 주장하지 않는다. 유일한 실패는 기존 CFU-R01이었다. `artifacts/meta-progression/play-round1.json`.
- CFU-R01 원인: virtual Mouse enabled=false/position0, Application focus=false에서 이벤트가 전달되지 않았다. GameView.Focus()만으로 해결되지 않았고 설치 InputSystem 원문에서 AddDevice의 background disable과 Editor pointer 라우팅을 확인했다. 임시 Mouse 수명에만 비저장 InputSettings 사본을 사용하는 fixture를 시험했다. 기존 입력 이벤트/yield와 .005 스크롤 변화·이동/저장 불변 검사는 유지했다.
- 2차 전체 PlayMode **199/200 PASS**, 367.74초. 메타3개는 모두 재통과했으나 CFU-R01은 TemporaryMouse.Dispose에서 원본 설정 복원 시 ArgumentNullException으로 실패했다. 설치 InputManager.cs:190~195가 기존 HideAndDontSave 설정을 교체 시 파기한다는 수명 조건을 놓쳤다. 이 보완만 원복하여 새로운 테스트 결함을 남기지 않았으며 CFU-R01은 원래 비활성 입력 문제로 OPEN이다. `artifacts/meta-progression/play-round2.json`. 실패한 시험의 결과를 원복된 코드의 새 전체 PASS로 주장하지 않는다.
- 원복 후 컴파일 completed/failed=false. 제품 코드 및 해당 입력 fixture는 1차 검증 상태와 같고 기존 읽기 전용 진단도 보존했다. 원복 뒤 전체 suite를 추가 실행하지 않았으며 2회 상한을 새 회차로 초기화하지 않는다.
- 최종 Editor 읽기 확인: Title 씬, playing=false/compiling=false/dirty=false; GameApplication의 metaConfig, MenuUI version1과 컨테이너 참조 모두 유효. 실제 사용자 메타 파일은 생성하지 않았다. InputSettings는 유효한 기본 메모리 설정으로 돌아왔고 ResetAndDisableNonBackgroundDevices / PointersAndKeyboardsRespectGameViewFocus이다. `artifacts/meta-progression/final-inspect.json`.
- 최종 실제 사용자 run.json 및 기존 사용자 RP 자산2개의 SHA256이 기준과 동일하다. `artifacts/meta-progression/{baseline-hashes,final-hashes}.json`. Packages/기존 ProjectSettings tracked diff와 staged 파일 없음. 기존 미추적 ProjectSettings/Packages는 보존한다.
- C#/Markdown `git diff --check` PASS, 새 Meta C# 후행 공백 없음. 대상 C#15개(실제 Assets 밖 Editor 도구 포함)의 CodeMap 및 필요한 .meta 누락 없음. `artifacts/meta-progression/source-map-check.json`. Unity가 작성한 prefab의 빈 `m_Name: ` 직렬화 줄은 수동 YAML 편집하지 않았다.
- Android 기기·Firebase 서버 통합·공격 방어: NOT_RUN/후속 연결 범위. Editor 테스트를 실제 기기/서버 보안 검증으로 주장하지 않는다.

## 문서·변경 범위·검토

신규 Meta6개 C#, RunUIController.Meta, EditMode/PlayMode 테스트, MetaProbe의 CodeMap을 작성했다. GameApplication/RunUIController/MenuUI/RunUIData 및 CampaignFlowPolishTests 요약과 기능 INDEX의 직접 관계를 갱신했다. Scene 쓰기 없음; Editor API로 MenuUI/GameApplication prefab과 신규 Meta SO만 작성했다. 입력 설정 교체 시험은 원복했고 원본 자산 저장은 하지 않았다.

최초 검토1묶음(메인 검증 + 스킬 지침의 읽기 전용 보조 검토1명). 보조 검토는 완료, R1/R2 수정의 구조적 해소를 재확인했고 실행 PASS는 메인의 실제 NUnit/PlayMode 결과로 구분한다. 수정·재검증1/2에서 R1/R2 및 신규 UI 테스트의 런 레벨업 오라클을 수정했다. 수정·재검증2/2는 기존 필수 회귀 차단 CFU-R01의 입력 fixture 보완과 PlayMode 재검증이다. 제품 지도 코드의 직접 회귀로 확인되지 않았으며 기존 보고의 역사적 FAIL을 덮어쓰지 않는다.

| ID | 근거·영향 | 해결 조건 | 상태 |
|---|---|---|---|
| META-R1 | Checkpoints.Save가 같은 sequence의 다른 후보를 허용하여 동일 상태에서 연 두 세션이 같은 n+1 명령으로 서로 덮어쓸 수 있음. 최신 문서를 다시 읽은 CAS만으로는 이를 막지 못함 | 같은 sequence는 시간 외 상태 동일성, 시간 비감소를 요구하고 새 명령은 +1만 허용. 상충 세션·시간 저장·응답 유실 재시도 회귀 | CLOSED: EditMode283/283 내 회귀 PASS |
| META-R2 | 출발 확정 후 응답 오류에서 savedPreview가 갱신되지 않아, 설정 변경 후 pendingStart가 사라지면 최초 런의 이어하기/포기 상태가 UI에 없어 복구가 막힘 | 메타 명령 결과/오류 후 저장된 런 preview를 동기화하고 오류 안내 보존. 실제 버튼으로 응답 유실→설정 변경→이어하기 검증 | CLOSED: 신규 PlayMode 응답 유실 버튼 흐름 PASS |
| CFU-R01 | 기존 지도 drag 검사에서 비활성 Editor가 가상 Mouse를 차단해 실제 스크롤 변화0. 2차 fixture의 설정 수명 보완 실패 후 사용자 승인1회로 원본 객체 보호/복원을 수정함 | 설치 InputSystem의 입력 라우팅과 임시 설정 파기/소유권에 맞는 격리 fixture, 설정 복원, 동일 입력·스크롤 및 상태/저장 불변 oracle로 전체 PlayMode 통과 | CLOSED: 승인된3차 전체 PlayMode200/200 내 실제 drag 및 복원 PASS |

2차 종료 시 남은 작업은 CFU-R01 한 건이었다. 프로젝트 AGENTS.md 5절의 “최초 산출물 검증 이후 수정·재검증은 Task/문서당 기본 총 2회다”에 따라 중단했으며, 아래에서 사용자 승인 추가1회로 재개해 해소했다. 새 일반 개선을 범위에 추가하지 않았다.

## 2026-09-12 승인된 추가 1회

사용자가 추가1회 요청에 “그래”로 승인하여 위 차단에서 재개했다. 기존 InputManager가 outgoing HideAndDontSave 객체를 파기하는 계약을 확인하고, 설정 교체 순간에만 원본을 DontSave로 보호한 뒤 finally에서 정확한 원래 플래그를 복원하도록 TemporaryMouse를 수정했다. 임시 설정은 원본 사본이고 생성 실패/Dispose에서 장치 제거·원본 설정/current Mouse 복원·사본 파기를 수행한다. 원본 설정 참조/직렬화 내용/hideFlags 불변 assertion을 포함한다. 실제 마우스 이벤트/yield와 스크롤·이동/저장 oracle는 유지했다.

이번 수정은 테스트 한 파일 및 직접 문서/검증 증거에 한정된다. 메타/지도 제품 코드·씬/prefab·프로젝트 입력 자산·패키지는 수정하지 않는다. 재개 때 이미 변경된 Pretendard-Effects SDF.asset을 포함한 보호 대상과 저장 파일을 실행 전 해시로 기록했다. `round3-baseline-hashes.json`, `round3-preservation-before.json`.

컴파일 completed/failed=false. 실행 전 Title stopped/clean, 입력 설정 유효 및 기본 모드, 실제 메타 저장 미생성 확인. `round3-inspect-before.json`. 제품 및 EditMode 코드는 고정되어 기존 EditMode283/283 증거를 유지한다. 누적3/3, 추가 검토 위임 없음.

- 최종 전체 PlayMode **200/200 PASS, 실패/제외/불확정0, 361.2초**. 실제 지도 drag 및 원본 입력 설정 복원, 메타3개, 기존 연출/타임아웃을 포함한다. `artifacts/meta-progression/play-round3.json`.
- 종료 후 Title playing=false/compiling=false/dirty=false, metaConfig/Menu 컨테이너 정상. InputSettings 유효, ResetAndDisableNonBackgroundDevices/PointersAndKeyboardsRespectGameViewFocus로 복귀했다. 실제 사용자 메타 파일 미생성. `round3-inspect-after.json`.
- 기존 run.json, Title, RP자산2개, 기존 변경 폰트의 총5개 SHA256 불변. 테스트한 C# 소스도 실행 중/후 동일하다. `round3-preservation-{before,after}.json`, `round3-tested-source.json`. 실제 drag 본문의 이벤트/yield/assertion을 바꾸지 않았고 diff는 TemporaryMouse에 한정됨을 `round3-test.diff`로 확인했다.
- 예기치 않은 자동 기록은 ProjectSettings/ProjectSettings.asset의 Android 정의2개 추가 한 건: SENTIS_ANALYTICS_ENABLED/APP_UI_EDITOR_ONLY. 설치 Inference Engine `Editor/Analytics/AnalyticsDefineManager.cs`의 InitializeOnLoadMethod 및 App UI `Runtime/Core/AppUIManager.cs`의 ApplySettings/delayCall이 선택 빌드 타깃에 자동 기록하는 코드를 확인했다. 기존 Standalone에 있던 같은 정의이며 프로젝트 소유 C#에서 이 정의를 조건부 참조하지 않는다. 설정 파일 기록 시각은20:41:45, 테스트 시작20:41:43/완료약20:47:44이며 이번200 PASS 실행 중 기록됐다. 패키지/lock 변경 없음. Android 기기 빌드 검증으로 확대 해석하지 않는다. 수동 프로젝트 설정 변경은 하지 않았고 diff를 `round3-package-defines.diff`에 보존했다.
- 대응 CodeMap/INDEX/PLAN/REPORT 동기화, C#/Markdown 공백 검사 PASS. 현재 범위의 남은 필수 Finding 없음. Firebase 서버 연동과 Android 기기 검증은 위 비목표/NOT_RUN 계약을 유지한다.

## 2026-09-12 커밋 정리

사용자의 명시적 commit 승인으로 완료된 메타/선행 안정화 변경을 local Git 스냅샷으로 기록한다. 포함: 영구 소유·출전·독립 런·성장 재화 정산/성장, 저장 충돌·중복·응답 유실 복구, 로비 프리팹 연결, Title 중복 입력 제거, 상점 등급 할인 시험값/회귀, 지도 합성 입력 수명, 직접 CodeMap/검토·데이터 audit/PLAN·REPORT, 확인된 Android 패키지 자동 정의. 정확한 staging 목록은 artifacts/meta-progression/commit-paths.txt에 기록하며 수정 대상 C#의 CodeMap과 Unity 자산 meta를 함께 확인한다.

기존 사용자 변경으로 구분했던 RP자산2개와 Pretendard-Effects SDF는 제외하고 작업 트리에 보존한다. 기존 별도 설계 문서2개, 이전 로컬 도구 CodeMap5개, ProjectSettings/Packages의 로컬 설정2개도 포함하지 않는다. 임시 실행 로그/저장/원본 유료 패키지는 Git에 추가하지 않는다. 이번 단계는 코드 변경이나 테스트 재실행이 아니며, 검증 증거는 직전 전체 PlayMode200/200 및 제품 코드가 고정된 EditMode283/283이다. 이전 보고의 “Git 수행 없음”은 당시 실행 이력이며 이후 사용자 승인에 따른 이번 commit과 구분한다.

커밋 준비: 정확한73개 파일 목록과 staged 집합 일치, 신규 C#/CodeMap 및 Unity meta 대응/충돌 마커 검사 완료. staging 후 새 문서5개의 불필요한 EOF 빈 줄을 제거했다(본문/코드 계약 변경 없음). C#/Markdown staged 공백 검사는 PASS다. Unity가 생성한 MenuUI의 빈 m_Name 공백3줄과 신규 .meta의 빈 userData/assetBundleName/assetBundleVariant 24줄은 직접 확인 후 생성 형식 그대로 보존하며 수동 YAML 수정하지 않는다. 커밋의 실행 상태는 Git 기록을 기준으로 하며, 실제 commit ID와 잔여 파일은 완료 응답에 표시한다.
