# 플레이어 원본·준비·런·정산 데이터 구조 검토 결과

상태: DOCUMENT / 검토 완료. 구현·Unity 실행·테스트·Git 변경 없음. 사용자 목표를 구현한 완료 판정이 아니라 현재 코드의 적합성 판단이다. 기준 main/8c767329eddfd8bf4c74c07c7bb4c757112681f6와 기존 working tree 변경을 읽었다. [PLAN](../Plans/PLAYER_DATA_FLOW_AUDIT_PLAN.md).

## 판정

사용자가 제시한 플레이어 영구 원본 → 로비 출전 설정 → 독립 런 인스턴스 → 결과 정산 → 영구 재화/성장 흐름은 현재 런 코어를 유지하며 확장 가능한 방향이다. 현재 구현은 공용 설정 기반 로비와 독립 런/단일 체크포인트까지이며, 플레이어 영구 원본·소유 기반 출전 설정·영구 정산은 아직 없다. 따라서 현 상태 그대로 전체 요구가 동작하거나 영구 정산까지 안전하다고 판정할 수 없다.

영구 성장은 기존 사용자 승인에서 후속으로 미뤄졌다([LOBBY_MAP_DICE_PLAN](../Plans/LOBBY_MAP_DICE_PLAN.md):2,7). 미구현 부분은 그 승인 범위를 어긴 기존 버그가 아니라 이번 목표의 확장 격차다. 이번 질문으로 영구 재화/성장의 희망 방향은 확인했지만 획득량·성장 비용·반출 대상 등 경제 규칙을 확정한 것으로 간주하지 않는다.

## 실제 연결과 근거

| 경계 | 현재 상태 | 원문 근거 |
| --- | --- | --- |
| 공용 콘텐츠 정의 | FateDiceConfig SO에 규칙과 기본 능력치/장비·주사위 정의를 보관하고 Snapshot은 깊은 복사. 플레이어 소유 원본이 아님 | [FateDiceConfig.cs:78](../../Assets/_Project/Features/Fate/Configs/FateDiceConfig.cs#L78), Snapshot:82–87 |
| 플레이어 영구 원본 | 프로필·영구 지갑·보유 목록·영구 성장 상태/저장소 없음. profileId 필드는 local-test 기본값이며 현재 선언·복사·공백 검증에만 연결 | [RunStateData.cs:34](../../Assets/_Project/Features/Run/Domain/RunStateData.cs#L34), RunStateCopy:53, RunStateValidator:32 |
| 로비 준비 | PreviewConfig를 SO에서 읽고 와일드 카드/탐험 등급 상한을 controller 필드로 선택. 캐릭터는 공용 설정의 한 명, 기본 주사위6개 안내. 보유 캐릭터·장비·주사위 선택을 전달하는 독립 RunSetup 없음 | [RunUIController.cs:71](../../Assets/_Project/Features/Run/Presentation/RunUIController.cs#L71), :38–39,95–97,486–522 |
| 새 런 | New(config.Snapshot(),seed,trialId,cap,store,previousResult). 기본 HP/스탯/골드·level1·기본 행동+와일드·기본 주사위6개로 시작. 이전 런의 성장/장비를 출전 설정으로 사용하지 않음 | [RunUIController.cs:130](../../Assets/_Project/Features/Run/Presentation/RunUIController.cs#L130), [RunApplication.cs:76](../../Assets/_Project/Features/Run/Application/RunApplication.cs#L76)–96 |
| 런 독립성 | 내부 상태 소유·규칙/상태 깊은 복사·읽기용 복사·후보 검증→저장→확정 경계가 구현됨. 이어하기는 저장된 규칙과 상태를 복원 | [RunSession.cs:25](../../Assets/_Project/Features/Run/Runtime/RunSession.cs#L25)–46, RunApplication:32–47,334–350 |
| 런 내부 성장·보상 | gold/xp/level/baseStats/장착/주사위 슬롯은 RunStateData에만 반영. processedRewardIds와 명령 토큰이 런 보상 중복을 막음 | [GrowthRules.cs:16](../../Assets/_Project/Features/Growth/Runtime/GrowthRules.cs#L16)–45, RunApplication:222–240,259–268 |
| 완료·영구 정산 | Result와 lastResult 통계는 있음. RunRecord는 runId/seed/승패/사건/턴/등급/시간만 기록하고 영구 보상/정산 상태 없음. 결과 화면은 표시 후 로비 이동 | [RunStateData.cs:21](../../Assets/_Project/Features/Run/Domain/RunStateData.cs#L21)–29, RunApplication:344–357, RunUIController:397–402,525–530 |
| 저장 | 기본은 FateDiceLocal/run.json 단일 슬롯. 유효성·체크섬·임시 파일 flush·원자 교체·손상 보관 구현. 플레이어별 경로 라우팅·프로필과 런 정산의 공동 저장은 없음 | [GameApplication.cs:38](../../Assets/_Project/Features/Run/Flow/GameApplication.cs#L38), [LocalRunStore.cs:21](../../Assets/_Project/Features/Save/Runtime/LocalRunStore.cs#L21)–73 |

## 필요한 분리 — 후속 설계 제안

아래 이름은 제안이며 현재 존재하는 타입으로 주장하지 않는다.

1. **PlayerProfile**: 플레이어 식별자, 영구 재화, 캐릭터 성장, 보유 장비·주사위·와일드 카드. 공용 정의 SO와 별도로 저장한다. 같은 종류의 장비/주사위 여러 개를 각기 강화한다면 definitionId와 소유 개체 instanceId/강화 상태를 구분한다.
2. **RunSetup / LoadoutDraft**: 로비에서 선택한 캐릭터·장비·주사위6개·와일드 카드·런 옵션. 출발 시 해당 플레이어 소유 여부·슬롯·장착 조건을 검사한다. 전체 프로필을 복사하거나 SO를 편집해 플레이어 상태로 사용하지 않는다.
3. **RunStartSnapshot + 기존 RunState**: 검증된 출전 구성과 영구 성장의 출발 효과를 값으로 확정하고, 현재 HP·런 XP·임시 장비·노드·RNG는 독립 런 상태에서 변화시킨다. 이어하기에서는 최신 프로필로 기존 런을 재생성하지 않고 저장된 시작 기준/진행 상태를 복원한다.
4. **RunResult + Settlement**: 확정 결과에서 허용된 영구 보상만 계산하여 프로필에 반영한다. 런 전체 상태로 프로필을 덮어쓰지 않는다. profileId/runId에 따른 지급 기록과 재화 증가를 같은 확정 단위로 저장하고, 종료·저장 실패·재시도에 복구 가능해야 한다. 결과 UI 표시 횟수에 지급이 종속되지 않게 한다.

공용 정의 → 소유 프로필/선택 검증 → 출전 스냅샷 → 가변 런 → 미정산 결과 → 한 번만 정산 → 갱신 프로필의 책임을 유지하는 것이 최소 확장 방향이다. 로비 화면과 기존 RunApplication/RunSession/RunStore의 런 책임은 재사용할 수 있지만 새 런 생성 인자는 실제 출전 구성으로 확장해야 한다.

기존 GDD:244는 영구 해금과 런 상태를 구분하고, :357,385–397은 실제 계정의 영구 재화·소유권을 별도 서버 승인 경로로 확정하는 방향을 제시한다. 로컬 프로토타입부터 분리 구현할 수 있으나 로컬 체크섬/파일 교체를 계정 인증이나 서버 보상 검증으로 간주하지 않는다. 본 검토에서는 외부 서버 SDK/API를 검증하거나 서버를 구현하지 않았다.

## 격차·위험·해결 조건

| ID | 요구 격차와 영향 | 해결 조건 |
| --- | --- | --- |
| PD-01 | profileId 자리만 있고 플레이어별 영구 원본/보유 데이터 연결 없음 | 식별자→프로필 저장/소유 조회·계정별 런 연결을 정의하고 검증 |
| PD-02 | 로비 선택이 trial/cap뿐이며 New가 고정 시작 구성 생성 | 캐릭터·장비·주사위·와일드 설정과 소유/슬롯 검증, 출전 스냅샷 입력을 추가 |
| PD-03 | 런 안의 콘텐츠 정의 ID만으로 개별 보유품 강화 상태를 보존할 수 없음 | 개별 성장 대상에 한해 소유 instanceId와 정의 ID/영구 상태를 분리 |
| PD-04 | 현재 Result는 통계이며 영구 정산 없음. 단순 UI 콜백 지급을 붙이면 재표시/이어하기로 중복 지급 가능 | runId 단위 정산 재시도 안전성, 재화+처리 기록의 원자 확정과 실패 복구를 설계/검증 |
| PD-05 | 새 여정이 같은 run.json을 덮어쓰며 이전 결과는 통계만 계승 | 미정산 결과를 보존하거나 정산 전 새 여정을 차단하는 명시적 정책. 플레이어 원본과 런 교체 범위 분리 |

PD-04/05는 현재 이미 중복 영구 지급이나 원본 유실이 발생한다는 주장이 아니다. 영구 지급 자체가 없는 상태에서 후속 기능을 잘못 연결할 경우의 구체적 위험이다. 기존 단일 런 파일의 원자 교체만으로 프로필/런 두 상태의 정산 원자성을 보장할 수 없다.

후속 구현 전에 정할 항목: 영구 반출 대상(재화만/장비·주사위 포함), 승리·패배·중도 포기의 보상 차이, 영구 성장/런 성장 효과의 결합, 소유품의 개별 강화 여부, 미정산 상태의 새 여정 허용 정책. 이 질문에 대한 구조 판정은 해당 경제 수치 없이 가능하다.

## 검증 한계와 종료

기능 색인/관련 CodeMap→원문→직접 테스트 순으로 확인했다. CoreBoundaryTests:49–68,84–123은 깊은 복사·저장 실패·저장 규칙 복원을 검사한다. SaveTests:196–249와 RunBoundaryTests:86–141에는 파일 교체 실패/초기 저장/후보 소유권 검사가 있다. 이번 작업에서는 실행하지 않았으며 모두 테스트 원문 확인이다(NOT_RUN). 직전 전체 검사 PASS는 새 영구 프로필·정산이 존재하거나 검증됐다는 근거로 사용하지 않는다.

Main은 로비/생성/통합과 문서, 보조2명은 저장/소유 및 성장/정산을 분담했다. 검토1묶음, 수정·재확인0/2. 이번 쓰기는 PLAN/REPORT2개뿐이며 기존 변경·실제 저장·Editor를 조작하지 않았다. 데이터 구조 개선 구현은 별도 요청 범위다.
