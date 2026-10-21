# META_PROGRESSION — IMPLEMENT

사용자 승인: 플레이어 영구 원본 → 로비 출전 설정 → 독립 런 → 결과 정산 → 영구 성장 구현. 추후 Firebase를 통한 부정행위/위변조 검증을 고려한다. 2026-09-11 추가 답변으로 영구 반출은 성장 재화만 확정했다.

## 기준과 범위

기준 main / 8c767329eddfd8bf4c74c07c7bb4c757112681f6. 기존 staged 없음. Fate config/PrototypeAuthoring/관련 테스트/Title 및 CodeMap/GDD의 이전 후속 변경, Assets/Settings 두 RP 자산, 기존 미추적 문서/ProjectSettings를 보존한다. 기존 실제 FateDiceLocal/run.json은 읽기 전용 이관 원본이며 테스트에 사용하지 않는다. 한 작성자가 현 Unity 프로젝트에서 순차 구현·검증한다. Git commit/branch/staging, 패키지·Firebase SDK 설치/배포, 기존 경제 밸런스 재조정, 다른 씬 수정은 비목표다.

## 고정 계약

- 공용 FateDiceConfig와 별개 PlayerProfile: schema/profileId/revision/LocalDevelopment authority, 성장 재화, 보유 캐릭터와 성장 단계, 장비·주사위 instanceId/definitionId, 보유 와일드 카드, 저장된 출전 설정.
- 출전 설정은 보유 캐릭터, 슬롯별 장비 3칸, 중복 개체 없는 주사위 6개, 와일드 카드와 탐험 상한이다. 장착하지 않은 장비 슬롯은 허용한다. 현재 콘텐츠는 모험가 한 명이며 새 캐릭터/개별 장비 강화는 콘텐츠 확장 시 추가한다.
- 런 시작은 소유/슬롯 검증 후 profile revision/출전 설정/성장 단계/정산 정책을 독립 값으로 고정한다. 런 내부 HP/골드/장비·주사위 변경은 프로필을 변경하지 않는다. 이어하기는 저장된 런 규칙을 사용한다.
- 결과 정산 요청은 runId/requestId/expectedRevision만 전달한다. 호출자가 지급량을 전달하지 않는다. 성장 요청도 캐릭터/요청 ID/revision만 전달하며 비용/효과는 서비스가 결정한다.
- 정산 receipt와 프로필 잔액을 런 체크포인트와 같은 로컬 파일 원자 교체로 확정한다. runId별 중복 정산은 재지급하지 않고, 요청 ID 재사용은 종류/입력이 같을 때만 허용한다. 실패 시 확정 전 상태를 유지한다. 미정산 결과가 있으면 새 여정은 차단한다. 진행 중 여정의 명시적 새 시작은 포기 처리(성장 재화 0)와 새 시작을 같은 확정 단위로 저장한다.
- Firebase 교체 경계는 비동기 명령 서비스이며 영구 잔액/소유권/보상량 전체 덮어쓰기 API를 제공하지 않는다. 실제 Auth UID와 서버 권한은 미래 어댑터가 제공한다. 로컬 저장/체크섬/LocalDevelopment 정산은 보안 증명이 아니며 서버 계정으로 자동 승격/동기화하지 않는다. 온라인 서버의 시작 티켓, 체크포인트 검증, 정산 트랜잭션/중복 키, Auth/App Check/Rules는 후속 연결 계약이다.
- 로컬 저장은 profile별 경로, 버전·유효성·체크섬·revision 비교와 파일 쓰기 잠금으로 충돌/손상을 감지한다. 구형 런은 원본을 수정하지 않고 독립 사본으로 이어할 수 있으나 영구 정산 보상은 0이다. 손상 원본을 자동 삭제하지 않는다.

## 첫 설정값 (시험값, SO에서 변경)

초기 성장 재화 0, 기본 모험가, 기본 주사위 6개, 기존 와일드 카드 보유, 초기 장비 없음. 해결 사건당 성장 재화 1, 승리 추가 10, 패배는 해결 사건분만, 포기/구형 런은 0. 성장 단계 최대 20, 다음 단계 비용 10×(현재 단계+1), 단계당 체력 +2/위력 +1/방어력 +0. 모든 영구 효과는 다음 새 런부터 적용한다. 런 골드·획득 장비·주사위는 반출하지 않는다.

## 허용 경로

- 신규 Assets/_Project/Features/Meta/{Domain,Runtime,Configs,Editor}/ 및 필요한 asmref/.meta.
- 기존 Run/Flow/GameApplication.cs, Run/Presentation/{RunUIController.cs,RunUIController.Meta.cs,MenuUI.cs,RunUIData.cs}.
- 테스트 Run/Tests/{EditMode/MetaProgressionTests.cs,PlayMode/MetaProgressionFlowTests.cs}.
- 필수 전체 PlayMode 회귀의 기존 CFU-R01 차단 원인이 확인되어 2차 수정에 `Run/Tests/PlayMode/CampaignFlowPolishTests.cs`와 대응 CodeMap/색인을 포함한다. 임시 마우스 수명 동안만 비저장 InputSettings 사본으로 합성 입력을 GameView에 전달하고 반드시 원본 설정 참조/내용을 복원한다. 저장된 입력 설정/asmdef/런타임 지도 코드는 수정하지 않으며 기존 이벤트·yield·스크롤/저장 불변 오라클을 유지한다.
- Editor API 쓰기 자산: Assets/_Project/Features/Meta/Configs/DefaultMetaProgression.asset (신규), Assets/_Project/Features/Run/Prefabs/MenuUI.prefab, Assets/_Project/Features/Run/Prefabs/GameApplication.prefab. 그 외 Scene/Prefab 쓰기 금지.
- 대응 Docs/CodeMap 1:1 요약/INDEX, 본 PLAN, Docs/Reports/META_PROGRESSION_REPORT.md, artifacts/meta-progression/의 도구·검증 증거.

## 필수 AC와 검증

1. 프로필·출전 설정 저장/재시작·소유/슬롯 검증·깊은 복사.
2. 실제 선택 장비/주사위·성장 효과로 런 시작, 런과 영구 데이터 격리, 기존 런 복원 고정.
3. 완료/패배/포기/구형 런 정산, 중복·응답 유실 재시도·저장 실패·stale revision·손상/다른 profile 거부.
4. 로비의 캐릭터/설정/성장 UI 및 결과 정산 버튼을 실제 포인터 이벤트/레이캐스트로 검증. 편집 가능한 prefab/SO 참조 확인.
5. Unity 컴파일, 전체 EditMode/PlayMode 회귀와 프로젝트 소유 변경 공백 검사. 실제 저장/기존 사용자 설정 해시 보존. 새 자산 meta 및 CodeMap/색인/REPORT 동기화.

테스트 우선: 신규 경계 존재/동작 테스트 RED → 구현 GREEN. 최초 검토 1묶음, 이후 필수 수정·재검증 공통 상한 2회. 필수 미검증은 PASS로 표기하지 않는다. Android 기기/Firebase 검증은 이번 범위에서 NOT_RUN이며 Editor 검증과 구분한다.

## 진행

검토 위임: requesting-code-review 스킬의 major feature 검토 지침에 따라 보조 검토 1명, 동시/누적 1명만 사용한다. 입력은 본 PLAN과 신규 Meta 및 직접 연결 파일/CodeMap/테스트. 산출물은 근거·영향·해결 조건을 갖춘 필수 Finding만 메인에 보고한다. 읽기 전용, 파일 쓰기·Unity 실행·재위임 금지. 메인은 문서/검증/수정을 단독 소유한다.

- [x] 선행 데이터 흐름 audit 및 실제 저장/화면 경계 확인
- [x] 영구 반출 정책 사용자 확정, Firebase 공식 계약 확인
- [x] 신규 테스트 RED / 최소 프로필·저장·명령 구현
- [x] 로비·런 시작·결과 연결 및 자산 작성
- [x] 통합/회귀 검증, CodeMap/REPORT

수정·재검증 1/2: META-R1/R2와 신규 UI 검사 오라클 수정 후 EditMode283/283, PlayMode199/200 PASS. 메타 PlayMode3개 모두 PASS. 남은 CFU-R01은 비활성 GameView의 virtual Mouse enabled=false/position0으로 재현됐으며 창 Focus만으로 해결되지 않았다. 설치 InputSystem의 AddDevice/background disable 및 Editor 입력 라우팅 원문을 확인했다.

수정·재검증 2/2: 입력 fixture 보완 후 PlayMode199/200 PASS(367.74초), 메타3개는 다시 모두 PASS. CFU-R01은 Dispose의 입력 설정 복원에서 실패했다. InputManager.settings가 기존 HideAndDontSave 설정을 교체할 때 파기하는 동작을 뒤늦게 확인했다. 불완전한 보완만 원복했으며 제품 코드/기존 입력 검사/읽기 전용 진단은 1차 검증 상태로 유지한다. 원복 후 컴파일 failed=false, Title stopped/clean, 기본 입력 설정 유효성/모드, 실제 저장 및 RP 해시 불변을 확인했다. 새 해결안을 만들어 반복 상한을 우회하지 않는다.

2차 종료 상태: **IMPLEMENT / BLOCKED(필수 전체 회귀 CFU-R01 미해결)**. 당시 메타 구현·자산·CodeMap/관계/REPORT 완료, 메타 EditMode19/PlayMode3 및 전체 EditMode283 PASS. 당시 전체 PlayMode는 PASS로 판정하지 않았다. 최초 검토1묶음·수정2/2 사용 후 아래 사용자 승인 추가1회로 재개했다.

## 2026-09-12 사용자 승인 추가 1회 (누적 3회 상한)

직전의 CFU-R01에 한정한 추가 수정·재검증1회 요청에 사용자가 “그래”로 승인했다. 같은 Task/기준 commit을 유지하며 기존2회 기록을 초기화하지 않는다. 현재 staged 없음. 기존 메타 구현 및 이전 변경을 보존하고 이번 재개 때 관찰된 Pretendard-Effects SDF.asset 변경도 수정하지 않는다. 재개 기준 테스트/폰트/Title 해시는 artifacts/meta-progression/round3-baseline-hashes.json에 기록했다.

변경은 CampaignFlowPolishTests의 TemporaryMouse 수명 및 대응 CodeMap/INDEX/PLAN/REPORT/검증 증거로 제한한다. 설치 InputManager.settings는 기존 HideAndDontSave 객체를 자동 파기하므로, 설정 교체 동안에만 원본 hideFlags를 DontSave로 보호하고 finally에서 즉시 원래 플래그를 복원한다. 임시 입력 설정은 IgnoreFocus/AllDeviceInputAlwaysGoesToGameView를 사용한다. Dispose와 생성 실패 경로에서 임시 장치를 제거하고 원본 설정/current Mouse를 복원하며 설정 참조·내용·플래그 불변을 검증한다. 저장된 자산/게임 코드/기존 스크롤·이동/저장 oracle는 유지한다.

필수 검증은 Unity 컴파일, 전체 PlayMode200개(실제 drag/메타3개/연출 타임아웃 포함), 종료 후 Editor 상태 및 기존 저장·자산 해시 보존이다. EditMode283개는 제품/해당 테스트 코드에 추가 변경이 없으므로 이전 PASS 증거를 유지한다. 최초 검토를 재개방하거나 추가 위임하지 않는다.

추가1회 결과: 전체 PlayMode **200/200 PASS**, 361.2초, CFU-R01 및 메타3개 포함. 원본 입력 설정 참조·내용·플래그 복원 assertion도 PASS. 테스트 후 소스 해시 동일, Title stopped/clean, 기본 입력 설정 복귀 및 실제 메타 파일 미생성 확인. 기존 run/Title/RP2개/폰트 총5파일 해시 불변. CodeMap/INDEX/REPORT 갱신 완료.

예기치 않은 Unity 자동 기록: ProjectSettings/ProjectSettings.asset의 Android scriptingDefineSymbols에 기존 Standalone과 같은 SENTIS_ANALYTICS_ENABLED/APP_UI_EDITOR_ONLY가 추가됐다. 설치된 Inference AnalyticsDefineManager.Initialize와 AppUIManager.ApplySettings가 선택된 빌드 타깃에 자동 기록하는 원문을 확인했다. 프로젝트 소유 코드에 해당 조건 분기는 없고 패키지 manifest/lock 변경도 없다. 이 설정이 기록된 실행에서 PlayMode200 PASS이며 Android 기기 빌드는 NOT_RUN이다. 수동 설정 쓰기/삭제로 되돌리지 않고 diff와 출처를 REPORT에 보존한다.

최종 상태: **IMPLEMENT / COMPLETE**. 필수 Finding META-R1/META-R2/CFU-R01 CLOSED. 최초 검토1묶음, 기본2회+사용자 승인1회로 누적3/3 사용. 추가 기능·Git 작업 없음. Firebase 서버 연결/기기 검증은 명시된 후속 범위다.

## 2026-09-12 커밋 정리 승인

사용자의 “정리해서 커밋진행” 요청으로 완료된 변경의 local commit을 승인받았다. 기존 Git 비목표는 이 단계의 명시적 commit에 한해 갱신하며 push/branch 전환은 범위에 추가하지 않는다. main/8c76732, 기존 staged0개를 확인했다. 메타 구현 및 직접 문서, 선행 커밋 검토/안정화(Title 입력·상점 할인·회귀), 데이터 구조 audit, 확인한 Android 자동 정의를 한 스냅샷으로 정확한 파일만 staging한다. 기존 RP자산2개/폰트1개, 별도 설계초안2개/이전 도구요약5개/로컬 패키지 설정2개는 보존한다. 테스트된 제품 코드를 수정하거나 완료 검토를 재개방하지 않는다. 필수 확인은 staged 목록/문서·meta 대응/공백·충돌 마커/커밋 결과 및 잔여 변경 확인이며, 직전 PlayMode200/200 및 고정 제품 코드의 EditMode283/283 기록을 재검증 근거로 보존한다.
