# RECOVERY_STABILITY 결과

상태: IMPLEMENT / 진행 중. 기준 main/b18f3b78fe960e725194bb10143c52c0c947a8d0. [PLAN](../Plans/RECOVERY_STABILITY_PLAN.md). 이전283/283·200/200은 이번 변경의 PASS가 아니다.

사전 확인: 기존 Task 문서/구현 없음. 저장 충돌의 거부는 있으나 SaveElapsedTime이 실패하면 메뉴도 열리지 않고 RefreshView는 같은 Session을 표시한다. PlayerSaveDocument는 정상 백업/복원 API가 없고 메타 UI는 finally가 바인딩 소유 확인 전에 metaBusy를 해제하며 catch도 오래된 오류를 기록한다. 실제 지연 서비스/지속 미결 기록은 아직 없다. Unity6000.6.0f1 연결 Editor는 ready/stopped, Pipeline7801. 참고 검토 파일은 프로젝트 및 명시 Downloads 경로에 없었으며 승인 Goal로 진행한다.

RV-01/02: 직접 검사 PASS. RV-03: 실제 지연 서비스의 실패 재현 단계이며 제품 요청 수명 구현은 미완료. RV-04: 미착수. 전체 최신 suite/Android는 통합 후 필수 미완료 검사다. 실제 사용자 데이터 손상 시험/쓰기는 없으며, 후속 사용자 요청으로 아래 범위의 로컬 커밋을 진행한다.

횟수: 최초 개발/재현, 최초 통합 검토0/1, 이후 수정·재검증0/2. 신규 필수 Finding과 실행 증거는 해당 단계에서 이 문서에 누적한다.

## RV-01 최초 개발 / 2026-09-14

- 앞선 커밋 상태 확인 턴은 구현 진척 없음. 본 턴은 동일 PLAN을 이어 구현한다. HEAD b18f3b7/staged0 재확인, Git 쓰기 없음.
- 재현 fixture 준비에서 사건 해결 다음이 지도라는 잘못된 가정으로 실패했다. 실제 규칙은 사건 해결→보상→보상 수령이며 이를 확인하여 유효한 두 명령 ResolveEncounter/ClaimReward로 수정했다. 게임 규칙이나 기대 sequence n+2는 변경하지 않았다.
- 제품 수정 전 실제 PlayMode UI RED **1개 실행/1개 실패**(4.18초). 양수 미저장 시간의 A와 n+2 B를 준비하고 레이캐스트 resolve, 이전 명령 재시도, 메뉴 자동 저장, RefreshView 뒤 A의 동일 세션/sequence와 디스크 bytes 불변 확인까지 통과했다. 실패점은 `recovery-reload` 버튼 부재다. `artifacts/recovery-stability/rv01-red.json`. 재현의 예상 오류 기록 무시 설정은 RED에서만 사용했고 최종 fixture는 정확한 경고 1개와 NoUnexpectedReceived로 교체했다.
- 기존 전체 suite로 우회하지 않고 명시 category=RecoveryStability로 실제 1개가 선택됐다. 최초 컴파일 중 테스트 요청 연결 오류는 미실행이며, 완료 상태/이전 결과 확인 후 새 요청을 보냈다.
- 오류는 MetaFailureKind 코드로 분류하고 기존 MetaCommandException/InvalidDataException 소비자 타입을 유지한다. 체크포인트/문서/프로필 충돌에만 Stale을 지정하고 정상 규칙 거절은 Rejected다. 미지원 형식과 타 프로필은 InvalidDataException의 typed cause로 구분한다.
- 기존 authored MenuUI 컨테이너를 recoveryMode로 바인딩하며 자산 쓰기 없음. ReadRecoverySnapshot의 같은 문서 run/receipt로 후보를 준비한 뒤만 교체하고, 저장 전 사전 autosave 없음. 복구 에피소드의 입력 세대와 완료 후 세대 증가로 이전 delegate를 무효화한다. 현재 컴파일 completed/failed=false; 첫 GREEN 실행 중이며 직접 AC 전체 통과는 아직 주장하지 않는다.

- 첫 GREEN 1/1 PASS(3.39초). 이후 파일/schema/profile/권한/영수증/I/O/규칙 거절/stale/독립 복사 검사 **EditMode12/12 PASS**(4.92초), 실제 authored 버튼의 A(n)/B(n+2)·읽기 실패 보존/재시도·다른 출발·이미 정산·일반 쓰기 실패 **PlayMode5/5 PASS**(48.67초). skipped/inconclusive0. `rv01-green-initial.json`, `rv01-edit.json`, `rv01-play.json`.
- RV-01 직접 AC 통과. 최신 전체 suite/Android는 전체 RV 통합 후 실행할 필수 미완료 검사다. 이에 의존하지 않는 승인된 RV-02 개발로 이어간다. 최초 통합 검토0/1·이후 보완0/2 유지.

## 보존 관찰

`rv01-preservation.json`: 보호 대상13개 중12개 SHA256 동일(AGENTS/Goal/GDD/RP2/Title/manifest/lock/프로젝트 설정/기존 저장 등). 기존 변경이 있던 Pretendard-Effects SDF만 다르다. PlayMode 과정에서 Dynamic atlas의 glyph/character/texture 데이터가 갱신됐으며 baseline hash와 같다고 주장하지 않는다. 현재 사본과 줄 길이를 제한한 diff를 `font-after-rv01.asset.snapshot`, `font-after-rv01.diff.txt`로 보존했다. 원본은 임의 rollback/재작성하지 않았다. 이 관찰은 최종 보존 감사에서 해결/제한을 명시할 대상이다.

## RV-02 최초 개발

정상본 보관 RED 1개 실행/1개 실패(0.89초): 기존 기록 후 다음 체크포인트가 정상 백업을 남기지 않는 점 재현. `rv02-backup-red.json`. ReadBytes가 실제 읽은 bytes를 검증하고 Write가 그 bytes를 잠금 아래 .backup에 원자 보관한 후 새 현재본을 쓰도록 구현했다. 이전본 검증 실패/백업 쓰기 실패는 새 현재본 확정 전 중단한다. 복원 UI와 세대 차단은 아직 미완료다.

- 정상본 구현 후 category EditMode13/13 PASS(6.5초), `rv02-backup-green.json`.
- 복원/세대 구현 후 category EditMode23/23 PASS(54초), `rv02-edit-initial.json`: 정상/없음/손상/타 프로필/미지원/백업 교체/백업 쓰기 실패/원본 보관 실패/현재본 원자 교체 실패, 동일 문서와 원본/백업 보존, revision ABA와 옛 세션·요청·후보 거절, 새 세대 정상 쓰기를 포함한다.
- UI category8/8 PASS(95.24초), `rv02-play-initial.json`: 기존RV-01과 복원 전 시각/손실 안내·확인/취소·원본 무변경·이전 callback 차단·복원 후 새 실제 명령·없거나 손상된 백업 동선. 최종 정산 receipt가 있는 백업 복원 후 재지급 방지 UI 사례를 추가하여 아직 검사할 예정이다.
- recompile 관찰 요청1개는30초 타임아웃이었으나 재요청/재시작하지 않고 editor_status/recompile_status로 동일 프로젝트 ready/stopped/completed/failed=false를 확인했다. Pipeline 연결 포트가7801→7800으로 바뀌었으며 모든 호출은 project-path로 식별했다. 현재 실제 실행된 category 결과를 위에 기록했다.
- RV-02 UI 실행 전후 동적 글꼴 사본/실제 파일 SHA256은 동일(0688B5CE…). 최초 baseline과의 RV-01 관찰은 여전히 별도이며 최종 보존 확인 전에 숨기지 않는다. 설치 TMP_EditorResourceManager의 dynamic/clearDynamicDataOnBuild 경로도 읽기 확인했으나 임의 자산 쓰기는 하지 않았다.
- 정산된 문서 백업→그 이후 성장 비용 소비→손상→시각/손실 확인→전체 복원→결과 화면 재정산의 추가 실제 UI **1/1 PASS**(32.14초), `rv02-paid-restore-ui.json`. 복원된 재화/성장과 같은 시점의 시작/명령/정산 기록 일치 및 다시 정산해도 재화 불변을 확인했다. RV-02 직접 요구 통과, RV-03의 실제 지연 미결 요청은 별도 다음 단계다. 최초 통합0/1, 이후 보완0/2 유지.

## 진행분 커밋 / 2026-09-14

후속 사용자 요청으로 RV-01/02 코드·직접 테스트·1:1 요약·색인·PLAN/REPORT만 로컬 커밋한다. 기존 메타 구현 b18f3b7은 재커밋하지 않는다. 기존 사용자 RP2개/동적 폰트, 미추적 설계 원문3개/과거 도구 요약/로컬 패키지 설정은 보존하고 제외한다. RV-03 테스트와 그 요약은 작업 사본에 남기며 staged 색인에서 해당 행만 제외한다. Git push는 하지 않는다.

- 직전 RV-02 관련 기존 MetaProgressionTests 검사: **19/19 PASS**, 141.49초, `rv02-meta-regression.json`. 과거 실행 기록이며 아래 커밋 직전 재실행과 구분한다.
- RV-03 실제 지연 UI RED: **1개 실행/1개 실패**, `rv03-ui-red.json`. 미완료 Task에서 대기창 닫기 버튼이 없다는 assertion으로 실패했다. 제품 수정은 아직 없으며 테스트를 삭제하거나 성공으로 간주하지 않는다. 이번 커밋 대상에서 제외한다.
- 커밋 직전 Unity 재컴파일 요청: `up_to_date`, 후속 상태 `failed=false`, 오류0. 이번 요청에서는 제품 코드/Scene/Prefab/SO를 수정하지 않았다.
- 커밋 직전 RV-01/02 category EditMode **23/23 PASS**, 81.22초, skipped/inconclusive0 (`commit-rv0102-edit.json`).
- 커밋 직전 PlayMode는 **WAIT_VALIDATION**. category 실행이 completed/0건으로 끝나 `commit-play-zero-not-run.json`에 보존했다. `list_tests`는 대상 9개 및 RecoveryStability category를 정상 발견했으나, 정확한 클래스 이름으로 지정한 후속 실행 역시 completed/0건이었다(`commit-rv0102-play.json`). Editor 로그도 RunStarted0/RunFinished0이므로 UI 동작 성공/실패의 근거가 아니다. 목록 조회와 실행 단계의 불일치이며 원인은 확정하지 않았다. 이전 8/8+1/1 통과는 위 과거 기록으로만 남긴다. 테스트/조건을 삭제하거나 패키지·설정을 수정하지 않았다.
- 제외 대상 RP2개/폰트는 이번 커밋 준비 시작 시점과 SHA256이 동일하다(`commit-excluded-preservation.json`). RV-01 최초 작업 baseline과의 폰트 차이는 위 보존 관찰로 여전히 남아 있으며 이 커밋에는 폰트를 넣지 않는다. 소스15개는 모두 `.meta`와 1:1 CodeMap이 존재한다. 소스 SHA256과 정확한 커밋38개 목록을 로컬 아티팩트에 기록했다.
- 전체 suite/Android/최초 통합 검토는 아직 미완료이며 이 진행분 커밋을 전체 Task PASS로 간주하지 않는다. 최초 통합0/1, 이후 보완0/2를 유지한다. 후속 구현은 RV-03부터 이어간다.
