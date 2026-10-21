# Dice Warrior — 다음 개발 작업 계획서

**문서 ID:** `SERVER_BACKEND_FOUNDATION`  
**버전:** v0.1 · 2026-10-21  
**대상:** Unity 6.6 / Android 세로형 싱글플레이 로그라이크 `KORWOT/Dice_Warrior`  
**GitHub 기준:** `main` / `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`  
**문서 성격:** **향후 작업 제안·실행 로드맵**. 아래에 적힌 신규 백엔드 기능이 이미 구현됐다는 뜻이 아니다.

> **한 줄 목표:** 지금은 목(Mock) 백엔드로 서버 권한·검증·정산을 실제처럼 동작시키고, 이후 같은 계약을 사용해 Firebase Authentication / Cloud Functions / Firestore / App Check로 이전한다. 기존 게임 루프는 유지한다.

---

## 0. 결정 사항과 경계

### 0.1 현재 확정된 개발 방향

| 구분 | 결정 |
|---|---|
| 출시 목표 | 치트·위변조 방지를 의식한 **Android 싱글플레이 시범 출시** |
| 게임 실행 | 이동 주사위, 기호 주사위, 전투·UI·연출은 **Unity에서 즉시 처리** |
| 서버의 역할 | **계정·런 시작 승인·합법성 검증·영구 프로필·최종 보상 정산** |
| 지금 사용할 서버 | `MockGameBackend` 등 **개발용 목 구현**. 로컬 검증은 가능하되 실제 보안 서버는 아님 |
| 향후 정식 서버 | Firebase Auth + Cloud Functions + Cloud Firestore + App Check/Play Integrity |
| 서버 호출 횟수 | 원칙적으로 **런 시작·종료·영구 성장 등 주요 시점**. 턴마다 서버 요청하지 않음 |
| 영구 데이터 | **서버 권한**. Unity 로컬 값은 표시용/캐시이며 결코 정산의 진실 원천이 아님 |
| 런 중 게임 상태 | Unity에 저장·재개 가능. 서버는 시작 시드와 행동 기록으로 결과를 재현·검증 |
| 3눈·4눈 | **조건부 상위 기술**이라는 미래 방향만 유지. 개방 조건·소모 방식·배율은 미정 |
| 이번 변경의 범위 | 서버 경계와 목 검증 구축. **3·4눈 발동 조건, 경제 밸런스, 아트는 변경하지 않음** |

### 0.2 기존 프로젝트에서 확인된 기반

- `Assets/_Project/Features/SymbolLoop/Domain/` 아래 순수 C# 규칙 (`LoopData.cs`, `LoopRules.cs`, `SymbolCombat.cs`).
- `LoopSession.cs`가 명령 후보 → 검증 → 저장 성공 → 상태 공개 흐름을 제공한다.
- `LoopStore.cs`가 로컬 파일·체크섬·백업·미결 명령 처리를 제공한다.
- 현재 `LoopSession.Execute("settle")`가 로컬 `Profile.currency`까지 직접 변경한다. **이 부분을 서버 권한으로 분리해야 한다.**
- 현재 `LoopRules.Start`에 외부에서 지정한 시드·시작 프로필이 전달되며, `travelRng`/`combatRng`가 구분되어 있다.
- `SymbolLoopApp`에서 런 시드 선택과 정산 버튼을 연결한다.
- 기존 커밋의 검사 근거: 새 EditMode 21/21, 새 PlayMode 4/4, 후속 직접 화면 검사 각 1/1. **현재 코드의 Android 실기기 검사는 미실행**이다.

**주의:** 본 문서는 2026-10-21에 조회한 GitHub 원격 커밋을 근거로 작성했다. 이후 로컬에서 추가 수정한 파일이나 미커밋 변경은 실제 착수 전에 별도로 확인해야 한다.

### 0.3 이번에 하지 않을 것

- 아직 정하지 않은 **3·4눈 각성 게이지·발동 조건·스킬 계수**를 임의 구현하지 않는다.
- 싱글플레이 전체 전투를 온라인 서버에서 실시간으로 처리하지 않는다.
- 기존 `symbol-loop-v1/player.json`을 삭제하거나 변조하거나, 그 안의 로컬 성장 재화를 **검증 없이 실제 Firebase 자산으로 이전하지 않는다**.
- 기존 v1 아카이브, 사용자 변경 자산, Scene/Prefab, 패키지를 승인 없이 일괄 삭제·교체하지 않는다.
- **목 백엔드 구현만으로 치트가 방지됐다고 주장하지 않는다.**

---

## 1. 목표 아키텍처

```text
[Unity / Android 클라이언트]
  화면·전투·캠페인·연출
        |
        +--> LocalRunEngine / LoopRules
        |      └─ 임시 런 상태, 이동/전투/선택 결정, 행동 로그
        |
        +--> LocalCheckpointStore
        |      └─ 안전한 저장·재개, 재시도할 요청/제출 보존
        |
        +--> GameBackendCoordinator
                   |
                   v
              IGameBackend
                   |
            +------+-------+
            |              |
      MockGameBackend   FirebaseGameBackend
      [지금 개발]         [후속 구현]
            |              |
      MockAuthorityStore   Firebase Auth / Callable Functions
      MockRunVerifier      Firestore / App Check
            |              |
            +------+-------+
                   |
       서버가 승인한 프로필·런 결과·정산 영수증
```

핵심은 **화면과 전투 코드가 목 서버 또는 Firebase의 구체적 API를 직접 알지 않도록** 하는 것이다. 단순한 인터페이스 치환만으로 서버 검증 구현이 자동 완성되는 것은 아니므로, 아래의 공통 도메인 계약과 검증기를 먼저 만들어야 한다.

### 1.1 필수 책임 분리

| 영역 | 책임 | 금지되는 결합 |
|---|---|---|
| `LocalRunEngine` | 전투, 이동, 상태 계산, 행동 기록 | 영구 재화 권한 확정 |
| `LocalCheckpointStore` | 기기 내 진행·로그 보관 | 자체 값만으로 서버 보상 지급 |
| `GameBackendCoordinator` | 서버 시작 승인, 제출·재시도·복구 상태 | 전투 효과 계산 중복 |
| `IGameBackend` | 목/실서버 동일 API·에러 계약 | UI 타입·Unity 컴포넌트 직접 의존 |
| `MockGameBackend` | 별도 권한 저장소로 서버 의미론 재현 | 클라이언트가 보내는 승리/재화 값 무검증 수용 |
| `RunVerifier` | 시작 시드+규칙 버전+입력 로그로 독립 재실행 | 제출된 최종 HP/골드/승리를 정답으로 사용 |
| `FirebaseGameBackend` | 인증 토큰 포함 서버 API 호출, 오류 변환 | Firestore 영구 재화 직접 수정 |
| Cloud Functions | 인증·검증·트랜잭션 정산 | 클라이언트 제출 값 신뢰 |

### 1.2 API 경계 제안 (설계용 가안)

아래는 **정식 구현 계약을 확정하기 전의 API 형태**다. Unity 종속 타입을 넣지 말고 DTO와 취소/실패 정보를 정의한다.

```csharp
public interface IGameBackend
{
    Task<BackendProfile> GetProfileAsync(CancellationToken ct);
    Task<StartRunResponse> StartRunAsync(StartRunRequest request, CancellationToken ct);
    Task<SubmitRunResponse> SubmitRunAsync(SubmitRunRequest request, CancellationToken ct);
    Task<SettlementStatus> GetSubmissionStatusAsync(string requestId, CancellationToken ct);
    Task<UpgradeResponse> UpgradeAsync(UpgradeRequest request, CancellationToken ct);
}
```

각 응답의 상태는 예를 들어 `Approved`, `Rejected`, `AlreadyApplied`, `Conflict`, `UnknownAfterTimeout`, `TemporaryFailure`를 구분한다. **네트워크 시간 초과 = 실패 확정**으로 취급하면 안 된다.

**요청 공통 필드 제안:** `protocolVersion`, `requestId`, `runId?`, `rulesVersion`, `expectedRevision?`. 사용자 UID는 클라이언트가 보낸 문자열을 신뢰하지 않고 **인증된 요청 컨텍스트**에서 결정한다(목 서버는 고정 테스트 사용자).

**런 시작 응답:** `serverRunId`, 서버 확정 `seed`, `rulesVersion`, `startingLoadout`, `authoritativeProfileRevision`, 서버 시작 기록(만료/진행 제약은 추후 명세).

**런 종료 제출:** `requestId`, `serverRunId`, `rulesVersion`, `commandLog` 등. `claimedCurrency`, `won`, `hp`, `finalGold`는 참고 진단 자료로 받을 수 있으나 **승인의 근거로 삼지 않는다**.

**영구 성장 요청:** `requestId`, 업그레이드 종류·대상, 예상 프로필 버전. 가격과 가능 여부는 목 서버/Functions가 계산한다.

### 1.3 권한 데이터와 로컬 데이터

```text
[권한 저장소 - Mock → Firebase]
  uid
  permanentProfile: currency, growth, loadout/보유품(추후), revision
  serverRuns: runId, seed, rulesVersion, starterSnapshot, status
  settlements: runId, appliedAmount, receiptId, serverRevision
  operations: requestId, fingerprint, response/receipt

[로컬 체크포인트 - Unity]
  serverRunId, rulesVersion
  currentRunState (빠른 재개/표시용)
  sequence, orderedCommandLog
  pendingStart / pendingSubmit / pendingUpgrade
  lastConfirmedServerProfile (캐시)
```

권한 저장소에는 **전체 플레이어 계정의 영구 상태를 한 문서에 계속 무한 누적하지 않도록** 런/영수증/요청 보관 구조와 정리 정책을 별도로 설계한다. 초기 목 구현은 간단해도 되지만, 나중에 대량의 런 기록을 한 단일 Firestore 문서에 누적하는 계약으로 고정하면 안 된다.

---

## 2. 실행 순서: 필수 개발 작업

### P0 — 기존 게임 보존·계약 확정 (최우선 / DOCUMENT + 최소 테스트)

**목적:** 잘 돌아가는 `SymbolLoop`를 깨뜨리지 않고 백엔드 개편을 시작한다.

- [ ] 최신 HEAD, 기존 staged/unstaged/untracked, 파일 해시, 보호 대상 확인.
- [ ] 현행 `LoopData`, `LoopRules`, `SymbolCombat`, `LoopSession`, `LoopStore`, `SymbolLoopApp`, GDD/CodeMap 직접 참조 확인.
- [ ] 변경 전 기준 **로비 → 3지역 완주 → 보스 → 정산 → 다음 런** 회귀 테스트 확보.
- [ ] `SERVER_BACKEND_FOUNDATION_PLAN.md`에 보호 경로·허용 변경 파일·AC·검증 횟수 기록.
- [ ] `protocolVersion=1` 및 `rulesVersion`이 무엇을 고정하는지 문서화. 런의 규칙은 진행 도중 바뀌지 않아야 한다.
- [ ] 이번 단계에서는 `Firebase SDK` 패키지 설치, Scene/Prefab 재작성, 기존 저장 삭제를 하지 않는다.

**완료 기준:** 기존 게임이 동일하게 플레이되고 서버 연동을 위한 명세·테스트 범위가 고정된다.

### P1 — 목 백엔드 인터페이스와 영구 프로필 권한 분리 (첫 구현 작업)

- [ ] `IGameBackend`, DTO, 공통 오류/취소·타임아웃 계약을 순수 C# 또는 Unity 의존성이 없는 계층에 추가.
- [ ] `MockGameBackend` 구현: 별도 `MockAuthorityStore`에 프로필·승인된 런·정산/요청 영수증 저장.
- [ ] 목 테스트 사용자 ID 도입. 개발 모드 계정은 실제 Firebase UID·권한 데이터와 혼용하지 않는다.
- [ ] `LoopSession`의 **영구 재화 지급·영구 성장**을 외부 권한 백엔드 경계로 이동. 런 내 HP/골드/XP/장비/주사위는 계속 로컬 엔진 소유.
- [ ] 서버 시작 응답의 시드·출전 구성·서버 규칙 버전만으로 새 런 시작. `SymbolLoopApp`의 임의 시드 선택을 출시 경로에서 권한으로 사용하지 않음(테스트용 고정 시드 주입은 유지 가능).
- [ ] 로컬 저장의 임시 상태와 서버가 확정한 영구 프로필 표시를 분리.
- [ ] 백엔드 생성 지점 한 곳에서 Mock/Firebase 구현을 선택하도록 만들되, **실제 Firebase 미구현 시 명시적 미지원 상태**를 반환하고 자동으로 Mock 권한을 승격하지 않는다.
- [ ] 기존 로컬 프로필과 새 권한 프로필의 스키마/경로 분리. 저장은 백업·복구·현재 진행 보호를 우선한다.

**완료 기준:** 로컬 `Run` 또는 UI 프로필의 재화를 마음대로 수정해도 목 서버의 권한 성장 재화가 바뀌지 않는다. 기존 게임 루프는 여전히 완주할 수 있다.

**주의:** Unity 프로세스 내부 목 서버는 누구나 변조할 수 있다. **보안 효력을 시연하는 서버가 아니라 프로토콜·권한 분리·테스트를 수행하는 개발 대역**이다. 필요할 경우 후속 단계에서 별도 프로세스 목 HTTP 서버를 추가한다.

### P2 — 결정적 런 재현 검증기와 행동 로그 (핵심 보안 작업)

- [ ] `RunCommandLog`의 **기록 형식** 확정: `sequence`, `kind`, `arg`, 이벤트/선택 대상 ID, `clientCommandId` 등; 기록은 사용자 선택·실행 명령이며 **주사위 눈/피해 결과를 권한 입력으로 받지 않는다**.
- [ ] 런 시작 스냅샷(서버 `seed`, `rulesVersion`, 초기 영구 프로필/출전 구성)을 권한 측에 보존.
- [ ] 로컬 실행 시 확정된 명령을 순서대로 추가하고 체크포인트와 함께 보존. 실패한 명령·연출만 수행한 장면은 기록하지 않는다.
- [ ] `MockRunVerifier`가 **별도의 신뢰된 시작 상태**에서 기록을 처음부터 순차 재실행.
- [ ] 순서 변경·누락·중복·위조된 아이템·비정상 단계 전환·잘못된 이동/전투 실행 등을 거절.
- [ ] 규칙과 난수 동결: `Board(seed,region)`, `travelRng`, `combatRng`, `Next`(32-bit unsigned, 연산 순서), 스킬·장비·XP·정산을 동일하게 재현.
- [ ] 성공/패배/중도 종료에 대해 재현된 `completedRegions`와 `won`으로 **서버가 정산액 산출**.
- [ ] 최대 행동 로그 길이/크기·반복 횟수, 규칙 미지원 버전, 활성 런 수, 런 시작·중단·제출 빈도 제한을 둔다.
- [ ] 골든 벡터(고정 시드 + 명령 목록 + 각 단계 핵심 상태/최종 보상) 생성. 향후 TypeScript 등 다른 언어로 서버를 작성할 경우 두 구현이 이 벡터에 **완전히 합치**해야 한다.

**완료 기준:** 정상 런은 검증을 통과하고, 최종 결과만 조작하거나 로그를 건너뛰거나 주사위 결과를 바꿔 제출하는 시도는 거부된다. 검증 실패가 클라이언트의 영구 자산에 영향을 주지 않는다.

> **보안 한계:** 시작 시드와 전체 결정 규칙을 아는 조작된 클라이언트는 미래 결과 예측·최적 경로 자동 탐색·합법적인 명령 로그 구성 등을 시도할 수 있다. **런 전체 리플레이는 영구 재화·결과 위조를 줄이지만, 모든 형태의 자동화·미래 예측·세이브 스컴을 증명 불가능하게 막지는 못한다.** 시범 출시는 이 범위를 위협 모델에 명시한다.

### P3 — 중복 정산·비동기 복구·동시 실행 안전성

- [ ] 런당 **정산 1회**: `serverRunId`를 고유 키로 지급 영수증 생성, 프로필 잔액 증가와 지급 기록을 **하나의 원자적 작업**으로 적용.
- [ ] 동일 `requestId` + 동일 요청 내용이면 같은 결과 재전송, 내용이 다르면 거절.
- [ ] 응답 유실 또는 타임아웃을 **결과 불명**으로 기록; 새 ID로 중복 요청하지 않고 같은 요청 조회/재시도.
- [ ] 서버 승인은 완료됐는데 기기 저장에 실패한 경우에도 재시작 후 서버에서 확정 지급 영수증을 복구.
- [ ] 동시 정산·동시 성장 구매·두 기기/두 세션의 동일 `expectedRevision` 경합 처리.
- [ ] 로컬 `LoopStore.Commit()`의 읽기→비교→교체 사이 동시 접근 방어(동일 프로세스 잠금/프로세스 간 접근 범위 명시) 및 충돌 재로드 시험.
- [ ] 손상 파일 백업 복원 시 이미 승인된 서버 재화가 과거 값으로 롤백되지 않도록 **서버 프로필 우선**으로 다시 합친다.
- [ ] 앱 종료/백그라운드/네트워크 끊김 시 로컬 런은 보존하고, 승인되지 않은 보상은 '검증 대기'로 표시.

**완료 기준:** 동일 런의 중복 제출 10회, 타임아웃 재시도, 복구, 동시 구매를 포함해도 권한 재화가 정확히 한 번만 변한다. 에러 원문과 로컬 진행은 손실 없이 재확인 가능하다.

### P4 — Firebase 어댑터 및 Emulator Suite (Mock 완료 후)

**정식 기술 후보:** Firebase Authentication(로그인), Cloud Functions의 callable HTTPS, Cloud Firestore(영구 상태·런·영수증), App Check(Play Integrity 기반).

- [ ] Firebase 프로젝트를 **개발용/출시용으로 분리**할지 결정하고 각 환경 구성·접근 권한 분리.
- [ ] Unity Google 로그인 또는 Google Play Games 로그인 → Firebase Auth UID 획득 경로 결정. **사용자 ID를 요청 본문만으로 신뢰하지 않음**.
- [ ] Firestore 문서 구조 초안: `players/{uid}` 영구 프로필, `runs/{runId}` 권한 런 시작, `settlements/{runId}` 영수증, `operations/{requestId}` 멱등 기록(실제 UID 범위/키는 구현 때 검증).
- [ ] `FirebaseGameBackend`가 P1에서 정의한 **동일 DTO/오류 모델**을 사용하도록 구현. 호출 설정과 인증은 한곳에서 처리.
- [ ] Functions가 **서버 시작 시드 발급 및 시작 기록**, 검증기 실행, 정산 트랜잭션, 성장 비용 계산을 담당하도록 작성.
- [ ] Firestore Security Rules: 클라이언트의 영구 재화·정산 결과 직접 쓰기 차단. 서버 Admin SDK만 승인 쓰기 수행.
- [ ] App Check / Play Integrity 도입 및 유효 요청 검사. **App Check는 클라이언트 변조/치트 자체의 완전 방지는 아님**.
- [ ] Firebase Emulator Suite의 Auth/Functions/Firestore로 인증, 정상·악성 로그, 결제 없는 정산, 규칙 위반, 재시도, 동시성, 접근 규칙을 검증.
- [ ] Firebase Functions 런타임 선택(예: TypeScript/Node.js) 시 **C# 게임 규칙을 그대로 실행할 수 없으므로** 서버 검증기를 별도 이식하거나 검증 실행 환경을 결정. P2 골든 벡터 전체 일치가 필수.
- [ ] 호출 횟수·로그 크기·정산 실행 시간 및 Firebase 과금/모니터링/레이트 제한을 측정하여 시범 출시용 한도를 정한다.

**완료 기준:** Unity 게임 로직/화면 수정 없이 백엔드 생성 설정을 Mock→Firebase 어댑터로 바꾸고, Emulator에서 같은 시나리오가 동일한 결과와 오류 의미를 갖는다. 운영 배포는 별도 승인 후 수행.

### P5 — Android 시범 출시 전 점검 (출시 게이트)

- [ ] Unity Editor 새 EditMode/PlayMode 전체, 현행 로비~정산 회귀, 앱 재시작·복구/백그라운드/네트워크 상태 테스트.
- [ ] Android ARM64 IL2CPP 개발 빌드 → 실제 기기 실행, 타일 터치·주사위 애니메이션·화면 safe area·앱 중단·재개 검증.
- [ ] Firebase 인증·App Check를 포함한 **서명된 테스트 빌드** 검증(출시 환경에서는 개발용 허용 정책 제거).
- [ ] 네트워크 단절 중 런 플레이는 가능하되, **서버 승인 전 영구 성장·정산 사용 차단**.
- [ ] 직접 Firestore 영구 재화 쓰기, 임의 고액 보상 제출, 다른 UID/runId 요청, 구형 규칙 로그, 재사용 영수증, 동시 정산 등 공격 테스트.
- [ ] 오류 모니터링, 함수/DB 사용량과 예산 경보, 악성 호출 비율, 로그 개인정보 최소화 및 보관 정책 검토.
- [ ] 시범 배포 정책(테스터 계정, 진행 데이터 초기화 안내, 운영 전환 시 목 데이터를 승격하지 않는 규칙) 문서화.

**완료 기준:** 실제 기기에서 로그인 → 새 런 → 저장·재개 → 최종 정산 → 재접속 프로필 유지가 성공하며, 주요 무단 지급 경로의 실패가 확인된다.

---

## 3. 첫 스프린트 권장 구현 범위 (`SERVER_BACKEND_FOUNDATION_01`)

**이번에 Codex에 맡길 최소 변경 단위는 P0 + P1이다.** P2~P5를 한 번에 시키면 코드·UI·서버 규칙·저장 마이그레이션 문제가 겹쳐 검증하기 어렵다.

### 우선 구현해야 할 결과물

1. 서버/클라이언트 **권한 표와 API 계약** (버전 포함).
2. `IGameBackend`와 순수 DTO/오류 타입.
3. 프로필·런 시작·승인/거절·영구 성장에 대한 **독립 목 저장소**.
4. `LoopSession`/`SymbolLoopApp`의 서버 경계 적용과 기존 루프 회귀.
5. 저장/복구·미결 요청 처리 유지, 임시 프로필을 운영 데이터처럼 보이지 않게 표시.
6. 대응 `Docs/Plans`, `Docs/Reports`, 1:1 CodeMap/INDEX 갱신, 실제 실행 증거.

### 첫 작업에 포함하지 않을 것

- Unity Firebase SDK·실제 계정 설정·클라우드 배포.
- 임시 성공 응답만 반환하는 불완전한 '서버 검증 완료' 주장.
- 최종 독립 리플레이 엔진(P2는 후속 스프린트). 단, 그 엔진이 사용할 DTO와 시드/명령 로그를 P1에서 예상해 인터페이스를 확정한다.
- 기존 프로토타입 3눈 발동 조건 변경, 4눈 스킬/애니메이션 추가.
- 임의의 Scene/Prefab/패키지 교체·기존 저장 삭제·자동 Git 쓰기.

### 영향을 받는 현행 파일(확인 후 최소 수정)

| 기존 파일 | 변경 방향 |
|---|---|
| `SymbolLoop/Domain/LoopData.cs` | 런/프로필 상태와 서버 계약 DTO 분리; 저장 버전 명시 |
| `SymbolLoop/Domain/LoopRules.cs` | 전투·이동은 유지. 시작 입력의 권한 경계를 정리 |
| `SymbolLoop/Runtime/LoopSession.cs` | 로컬 `start/settle/grow` 책임 분리, 요청 중복 방지 유지 |
| `SymbolLoop/Runtime/LoopStore.cs` | 임시 런 체크포인트와 서버 캐시/미결 제출 분리; 원본 보존 |
| `SymbolLoop/Presentation/SymbolLoopApp.cs` | Mock backend를 통해 시작/프로필/정산 호출. 처리 중·복구 UI 연결 |
| `SymbolLoop/Tests/...` | Mock 검증, 정상 게임 완주, 리비전/재시도/저장 실패 회귀 |
| **신규** `SymbolLoop/Backend/` (경로 가안) | `IGameBackend`, DTO, `MockGameBackend`, `MockAuthorityStore`, Coordinator 등 최소 필요 범위 |

`FateDice.SharedUI`와 기존 작성된 `SymbolLoopScreen.prefab`은 가능하면 변경하지 않는다. Scene/Prefab 변경이 꼭 필요하면 `AGENTS.md`에 따라 PLAN에서 대상 파일을 명시하고 별도 승인 범위를 맞춘다.

### 첫 작업의 인수 조건(AC)

| ID | 직접 확인할 것 |
|---|---|
| AC-01 | Mock 경로로 기존 로비 → 순환 보드 → 전투 → 완주 → 최종 보스 → 결과 화면 진행 가능 |
| AC-02 | 서버 권한 프로필과 로컬 런 상태가 구분되며 로컬 프로필/메모리 임의 변조로 권한 재화 증가 불가 |
| AC-03 | 서버가 정한 시작 시드·규칙 버전·초기 성장 상태만 새 런 입력으로 사용 |
| AC-04 | 프로필 업그레이드 비용·가능 여부는 Mock backend가 검증하고 결과 반환 |
| AC-05 | 오류·타임아웃·중복 명령 시도·기기 재개 이후 이전에 승인된 프로필/진행을 보존 |
| AC-06 | 기존 `symbol-loop-v1` 원본 저장·백업·v1 아카이브/사용자 자산 무손실 |
| AC-07 | 실행한 검사만 PASS 기록, 빠진 테스트는 NOT_RUN, 대응 CodeMap/계획/보고서 동기화 |

> P1은 **권한 분리 검증 단계**다. 부정 로그를 진짜 서버처럼 검증/차단하는 `RunVerifier`는 반드시 **P2에서 완료한 후** 보호 수준을 평가한다. P1의 Mock 결과를 운영 출시 기준으로 착각하지 않는다.

---

## 4. P2 이후 필수 보안 테스트 매트릭스

| 시나리오 | 기대 결과 |
|---|---|
| 정상 시드와 정상 명령 로그 | 정산 승인, 서버 계산 금액 지급 |
| 최종 `won=true`/`currency=999999`만 수정 | 제출 값 무시·권한 보상 증가 없음 |
| 이동 눈 또는 전투 스킬 결과 조작 | 서버 재현 결과와 불일치 시 거절 |
| 사건 선택 전투 단계 건너뛰기 | 상태 전이 위반으로 거절 |
| 획득하지 않은 주사위·장비 사용 | 보유 상태 불일치로 거절 |
| 같은 `runId` 여러 정산 요청 | 1회만 지급, 기존 영수증 반환 |
| 같은 `requestId`에 다른 본문 | fingerprint 불일치로 거절 |
| 지급 직후 응답 유실 | 요청 상태 조회/재시도로 기존 영수증 회수 |
| 서로 다른 세션 동시 지급/업그레이드 | 트랜잭션·revision에 따라 중복 쓰기 방지 |
| 백업 복원 후 과거 제출 재실행 | 이전 서버 결과와 충돌/중복 지급 없음 |
| 서버 접속 없이 새 영구 성장 구매 | 승인 전에는 성공 표시/적용 불가 |
| 구버전 규칙·초과 길이 로그 | 명시적 거절·재시도 정책 적용 |
| 한 사용자가 다른 사용자 runId 제출 | 인증 컨텍스트·소유권 검증으로 거절 |
| Firebase 직접 Firestore 재화 수정 | 보안 규칙으로 거절 |

### 결정적 재현 검증의 골든 데이터

고정 시드 3~5개로 최소 `정상 승리`, `중도 패배`, `중도 포기`, `2·3눈 조합`, `상점·교체·성장`, `재개 후 제출` 기록을 만들고, 각 기록에 `stateHash` 또는 비교 가능한 주요 상태 목록을 보존한다. 향후 Functions 검증기에서 **모든 사건 결과와 난수 소비 순서**가 일치해야 한다. JSON 문자열 해시만을 권한의 진실 근거로 사용하지 않는다.

---

## 5. 3눈·4눈 상위 기술: 지금은 설계 보류

### 유지할 방향

- 기본 눈 조건을 충족하면 별도의 스킬 습득 없이 기술 자동 발동.
- **전체 눈 정확 일치 → 등록된 조합 1개**, 불일치 → 활성 눈의 기본기 각각 실행 원칙을 우선 유지.
- 미래에는 **특정 조건을 충족했을 때 활성 기호 눈이 일시적으로 3개·4개로 늘어나고 매우 강력한 상위 기술**이 나가도록 확장할 계획.
- 현재 3눈 구현은 **MVP 검증용 상태**. '지역 진행으로 3개가 되면 항상 3눈 판정'이라는 규칙을 출시 최종 규칙으로 고정하지 않는다.

### 나중에 정할 사항(이번 스프린트 제외)

- 3·4눈을 열어 주는 조건(게이지/장비/특수 사건/전투 상황 등).
- 추가 주사위를 **소유**하는 것과 해당 턴에 실제로 **활성**하는 것의 관계.
- 조건 충족 이후 몇 턴 유지하는지, 발동/실패 시 조건 소모 여부.
- 3·4눈 상위 기술의 피해·방어·보조 효과와 기본기 여러 번 대비 기대값.
- 전투 UI의 추가 주사위 배치와 모바일 세로 화면 가독성.
- 새 조건도 서버 재현 대상이므로 버전·입력 로그에 반영하는 방식.

**당장 필요한 최소 조치:** 이번 백엔드 작업에서 `1~3개`라는 MVP 판정 규칙/시드를 **rulesVersion과 함께 동결**하여 서버 재현이 가능하게만 한다. 현재 동작하는 3눈을 변경하거나, 4개 지원을 위해 전투 코어를 미리 과도하게 추상화할 필요는 없다.

---

## 6. 운영·안전성 원칙

1. **신뢰하지 않는 입력:** Unity 메모리, 로컬 저장, 결과 화면, 전송된 승리 플래그/골드/HP, 클라이언트 지정 UID·시간은 서버 권위가 없다.
2. **신뢰 경계:** 서버 시작 시드·규칙 버전·서버 프로필·거래 영수증은 권한 저장소에서 읽는다. Firebase Functions 호출 시 인증 및 UID/소유권을 재확인한다.
3. **공격 모델:** 위조 재화·임의 보상·중복 정산을 우선 차단. 봇·미래 결과 예측·클라이언트 메모리 관찰까지 완벽히 막는다고 약속하지 않는다.
4. **규칙 업데이트:** 진행 중인 런은 시작 당시 `rulesVersion`으로 재현한다. 버전 미지원 시 임의로 최신 규칙을 적용하지 말고 안전하게 거절/명시 안내.
5. **오프라인:** 이미 시작한 런은 로컬 진행 가능하도록 계획하되, 새 런 승인 및 영구 지급/성장은 서버 연결을 필요로 한다.
6. **진행 보호:** 새 저장 스키마·서버 캐시를 명시적으로 분리하고 이전 파일을 자동 삭제하지 않는다. 개발용 Mock 재화는 운영용 서버에 신뢰 이관하지 않는다.
7. **개발 규율:** 기존 `AGENTS.md`에 맞게 PLAN → 최소 구현 → 실제 검사 → REPORT + C# 1:1 CodeMap → 승인 후 Git 처리. 패키지·Scene·Git 쓰기는 범위 밖에서 자동 수행하지 않는다.

---

## 7. Firebase 공식 참고 자료 (2026-10-21 확인)

- Authentication / Unity Google 로그인: https://firebase.google.com/docs/auth/unity/google-signin
- Authentication / Unity Play Games 로그인: https://firebase.google.com/docs/auth/unity/play-games
- Unity App Check / Android Play Integrity: https://firebase.google.com/docs/app-check/unity/default-providers
- App Check / Cloud Functions enforcement: https://firebase.google.com/docs/app-check/cloud-functions
- Callable Cloud Functions: https://firebase.google.com/docs/functions/callable
- Firestore 트랜잭션: https://firebase.google.com/docs/firestore/manage-data/transactions
- Firestore Security Rules: https://firebase.google.com/docs/rules/rules-and-auth
- Firebase Local Emulator Suite: https://firebase.google.com/docs/emulator-suite/connect_and_prototype

---

## 8. 지금 바로 착수할 작업 / 정리

**바로 다음에 실행할 작업은 `SERVER_BACKEND_FOUNDATION_01 (P0+P1)`이다.**

> 현행 `44e6c31` 기반 SymbolLoop의 전투·캠페인·연출을 그대로 유지하고, 영구 재화와 런 시작 권한을 `IGameBackend` 뒤로 옮긴다. 우선 독립 목 권한 저장소를 사용하여 런을 끝까지 플레이한다. 수정 범위를 작게 유지하고 저장·재개·중복 요청 회귀와 CodeMap/REPORT를 함께 작성한다. 서버가 명령을 완전 재현해 보상을 승인하는 P2는 후속 작업으로 진행한다.

그다음 `SERVER_RUN_REPLAY_VALIDATION (P2)`, `SERVER_SETTLEMENT_RECOVERY (P3)`, `FIREBASE_ADAPTER_EMULATOR (P4)`, `ANDROID_PILOT_RELEASE_GATE (P5)` 순서로 진행한다.

**종료 목표:** 목 서버 모드에서는 검증 가능한 한 런을 시작·완료·정산할 수 있고, Firebase로 전환할 때 게임플레이 코드를 다시 뜯어고치지 않고 백엔드·인증·서버 검증기를 교체/연결할 수 있는 구조를 갖춘다.
