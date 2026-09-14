# RECOVERY_STABILITY — IMPLEMENT

2026-09-14 시작. 기준 main/b18f3b78fe960e725194bb10143c52c0c947a8d0, staged0. 기존 RP2개·Pretendard 폰트 변경, 설계 원문3개/이전 도구요약/로컬 패키지 설정 미추적을 보존한다. 적용 원문은 사용자가 승인한 Recovery Stability Goal v0.1이며 원문/AGENTS/GDD는 수정하지 않는다. 참고 b18f3b7 검토는 프로젝트/명시 Downloads 경로에서 찾지 못했으므로 Goal의 명시 요구와 현재 코드를 기준으로 진행한다. 이전 META 완료 및 CR/CFU는 재개방하지 않는다.

## 고정 계약 및 순서

1. RV-01: 오류 종류를 타입/코드로 구분한다. 같은 런 A(n,미저장 양수 시간)/B(n+2) 재현 후 저장을 먼저 하지 않는 명시적 재로드를 제공한다. 읽기/검증 후보 완료 후 세션·화면 교체, 이전 UI 명령 토큰과 연출 무효화, 실패 시 기존 메모리/파일 보존. 규칙·난수·보상·정산 재실행 없음.
2. RV-02: 로컬 프로필마다 검증한 마지막 정상 문서 백업1개. 새 커밋 전 정상 이전본을 원자적으로 백업하며 실패하면 주 저장 확정도 중단한다. 손상 현재본은 백업으로 쓰지 않는다. 복구 후보의 시각/해시를 확인 UI에 제공하고 확인 때 동일 후보를 다시 검증한다. 손상 원본 보관에 실패하면 복원 금지. 복구 시 문서 전체를 원자 교체하고 별도 복구 세대로 이전 세션/요청의 쓰기를 거부한다. 보관 원본은 명시적 복원 시에만 생성하며 자동 삭제하지 않는다. 무제한 정상 백업·매프레임 백업·초기화/임의 가져오기 없음.
3. RV-03: 화면과 독립적인 명령 상태/불변 요청 기록, 응답 대기 기준(기본30초, 연출 watchdog과 별개), 결과 불명 상태의 조회/같은 ID·입력·revision 재시도. 이전 continuation/finally는 새로운 바인딩의 Session/오류/잠금을 변경하지 못한다. 격리 TaskCompletionSource 서비스로 확정 전 실패/확정 후 유실/늦은 응답/무응답/재시작을 제어한다. 복구 세대가 다른 미결 요청은 자동 재실행하지 않는다.
4. RV-04: 검증된 신규/100/1000런 문서, 표본5회 이상(워밍업 별도), 파일 bytes/실제 읽기 횟수/체크포인트 단계 시간/가능 할당 및 프레임 지연 측정. 중복 읽기 등 확인된 비용만 최소 개선하고 같은 조건 전후 비교한다. receipt 보존 및 저장 직전 CAS는 유지한다.

## 변경 경계

Meta/{Domain,Runtime,Configs,Editor}의 필요한 복구/요청 타입, 기존 LocalMetaProgressionService/PlayerSaveDocument/IMetaProgressionService/MetaProgressionConfig. Run/Presentation의 RunUIController partials/MenuUI/RunUIData, Run/Flow/GameApplication, 필요한 Run/Runtime 저장 어댑터. 관련 EditMode/PlayMode 테스트를 기존 Run/Tests에 추가한다. C# 대응 CodeMap/INDEX와 본 PLAN/REPORT만 작업 문서로 만든다. artifacts/recovery-stability는 로컬 검증 증거만 보관한다.

UI는 기존 MenuUI의 작성된 정보/버튼 컨테이너를 복구 상태로 바인딩한다. RV-01에서 Scene/Prefab 쓰기 없음. 설정/연결 변경이 실제 필요하면 허용 자산은 정확히 Assets/_Project/Features/Meta/Configs/DefaultMetaProgression.asset, Assets/_Project/Features/Run/Prefabs/MenuUI.prefab, Assets/_Project/Features/Run/Prefabs/GameApplication.prefab이며 기존 Editor API 도구로 필드만 변경한다. 기존 화면 전체 재생성/직접 YAML·GUID 수정은 금지한다.

보호: 전투/주사위/카드/성장/상점/반올림/정산 수치, 성장 재화만 반출, 기존 영수증/후보→저장→확정/CAS. 실제 사용자 저장·백업은 해시/존재 확인만, 시험은 격리 TEMP. Git 모든 쓰기/설정 변경/SDK/패키지/외부서비스/기존 앱 덮어쓰기 없음.

2026-09-14 후속 사용자 요청 «좋아 이제 정리해서 커밋진행해줘»에 따라 이번 RV-01/02 진행분의 정확한 파일 staging·로컬 commit만 허용한다. 미완성 RV-03 재현 테스트/그 요약, 기존 사용자 렌더링·폰트 변경, 미추적 설계/도구 요약/로컬 패키지 설정은 제외한다. INDEX는 커밋에 포함되는 요약만 반영한 사본을 stage하며 작업 사본의 RV-03 행은 유지한다. push·브랜치 변경·reset·stash는 하지 않는다. 이번 커밋은 전체 Task 완료 판정이 아니며 RV-03/04와 최종 통합 검증 및 기존 예산은 이어간다.

## 검증·진행

RV별 실패 재현→근거 있는 최소 구현→직접 실행 통과 후 다음 단계. 필터0건은 성공 아님; 현재 정상 채널인 전체 suite 또는 대상이 실제 선택된 실행을 확인한다. 최종 소스로 컴파일/전체 EditMode/PlayMode 및 Android 모듈이 있으면 ARM64 IL2CPP 개발 APK/해시/경고 기록. 승인 기기·격리조건 부재는 기기만 NOT_RUN. 필요한 Unity 실행 불가 WAIT_VALIDATION, 실제 실패 FAIL, 상한 내 미해결 필수 결함 BLOCKED.

최초 통합 검토1묶음, 이후 전체 Task 공통 수정·재검증 최대2묶음. 현재 최초 개발/재현 단계, 통합 검토0, 이후 보완0/2. 단계별 예산 초기화 없음. 메인 단독 코드/자산/실행 소유. 리뷰 스킬의 보조 검토가 필요할 경우 최초 통합 시 읽기 전용1명/누적1명 상한과 경계를 이 문서에 구체화하고 실행한다.

- [x] RV-01 직접 재현/복구/UI 검증 (EditMode12/12, PlayMode5/5; 전체 통합 검증은 별도 미완료)
- [x] RV-02 백업·확인·복구 세대 직접 검증 (EditMode23/23, UI8/8 + 정산 백업 추가UI1/1)
- [ ] RV-03 실제 지연/유실/불명 요청 UI 검증
- [ ] RV-04 누적 측정/근거 기반 최소 개선
- [ ] 최초 통합 검토/최신 전체 검사/Android/보존/CodeMap

### RV-01 구현 선택 / 최초 개발 진행

MetaFailureKind를 Core 예외 코드로 두고 runtime MetaFailure가 파일 오류/표시를 분류한다. 기존 소비자의 InvalidDataException 계약은 typed inner cause로 보존한다. IMetaProgressionService.ReadRecoverySnapshot은 같은 시점의 런과 정산을 읽는 작은 복구 계약이며 사라진 파일을 신규 프로필로 만들지 않는다. RunUIController.Recovery partial과 MenuUIData.recoveryMode/기존 mainChoices로 명시 재로드를 표시한다. UI 명령 세대는 복구 진입/후 교체에 증가해 예전 화면 delegate를 거부한다. 기존 Scene/Prefab 변경은 필요하지 않다. 단일 category 직접 실행으로 RED 확보; 최초 통합 검토0/1 및 이후 보완0/2는 유지한다.

### RV-02 기술 선택

정상본 1개는 player.json.backup에 보관한다. 기존 파일을 검증한 bytes 그대로 원자 복제한 뒤 새 현재본을 확정한다. 백업 쓰기 실패는 현재본 확정을 중단한다. 손상 원본은 명시 복원 때만 player.json.recovery/의 고유 파일에 flush 후 보존하며 보존 실패 시 현재본을 덮어쓰지 않는다. 백업 조회의 fingerprint와 보관 시각/진행을 확인 화면에 고정하고 확인 시 잠금 아래 재검증한다. 조회 뒤 현재본이 정상으로 바뀌었다면 복원을 거부한다.

복구 세대는 프로필과 Runtime RunState의 recoveryGeneration, MetaRequest의 expectedRecoveryGeneration으로 전달한다(기존 누락 값은 빈 초기 세대). 복원 때만 새로운 세대를 만들며 기존 profile revision/sequence로 돌아가도 저장 직전 세대 불일치로 이전 후보/세션/요청을 차단한다. RunSession은 이 저장용 값을 수명 동안 따로 보관해 Core 계산·게임 수치에 넣지 않는다. 이를 위해 기존 Run/Runtime/RunState.cs와 RunSession.cs 및 대응 CodeMap을 허용 경계에 명시한다. 단순 복원으로 영수증을 삭제하거나 타 시점 자료를 병합하지 않는다.

### RV-03 구현 선택

UI와 독립된 메타 요청 조정 객체가 요청 사본/종류/전체 입력 fingerprint/expectedRevision/복구 세대를 보유한다. 요청은 발송 전 profile별 .pending 파일에 원자 기록하고 결과가 불명확하면 유지한다. 한 개 미결 요청만 허용하며 다른 요청의 journal을 이전 응답이 삭제하지 못하도록 동일 ID/fingerprint 확인 후에만 정리한다. 앱 재시작 때는 이 기록을 결과 확인 필요 상태로 읽는다. 백업 복원은 이 기록을 새 세대 요청으로 바꾸지 않으며 조회 후 복구 이전 요청의 처분을 확인한다.

TaskCompletionSource 시험 서비스는 실제 확정과 응답 반환을 따로 통제한다. 설정 가능한30초 응답 대기는 연출 watchdog과 분리한다. 대기 만료는 실패 확정이 아니며 조회/동일 입력 재시도만 제공한다. 관련 게임/메타 변경을 차단하되 상태 창 닫기/재개/결과 조회 UI는 사용 가능하게 한다. 정상/늦은 응답과 catch/finally는 요청 및 화면 바인딩 소유권으로 제한한다.

같은 UI 인스턴스의 외부 재바인딩도 구분하기 위해 직접 관계인 Assets/_Project/Shared/UI/Presentation/BaseUI.cs에 읽기 전용 바인딩 세대만 추가할 수 있다(기존 열기/닫기·소유·팝업 동작 변경 없음). 이 파일과 대응 CodeMap/기존 UI 회귀도 허용 경계에 포함한다. journal 계약의 직접 영향 테스트 어댑터와 MetaProgressionConfig/대응 요약도 함께 갱신한다.
