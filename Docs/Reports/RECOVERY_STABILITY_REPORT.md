# RECOVERY_STABILITY 결과

상태: IMPLEMENT / 진행 중. 기준 main/b18f3b78fe960e725194bb10143c52c0c947a8d0. [PLAN](../Plans/RECOVERY_STABILITY_PLAN.md). 이전283/283·200/200은 이번 변경의 PASS가 아니다.

사전 확인: 기존 Task 문서/구현 없음. 저장 충돌의 거부는 있으나 SaveElapsedTime이 실패하면 메뉴도 열리지 않고 RefreshView는 같은 Session을 표시한다. PlayerSaveDocument는 정상 백업/복원 API가 없고 메타 UI는 finally가 바인딩 소유 확인 전에 metaBusy를 해제하며 catch도 오래된 오류를 기록한다. 실제 지연 서비스/지속 미결 기록은 아직 없다. Unity6000.6.0f1 연결 Editor는 ready/stopped, Pipeline7801. 참고 검토 파일은 프로젝트 및 명시 Downloads 경로에 없었으며 승인 Goal로 진행한다.

RV-01/02/03 직접 검사 및 RV-04 전후 측정은 아래 실행 기록을 확보했다. 최초 전체 EditMode는303 PASS/14 FAIL이며 요청 창 재개 결함1건도 확인했다. 전체 PlayMode/Android와 통합 판정은 미완료다. 실제 사용자 데이터 손상 시험/쓰기는 없으며, 후속 사용자 요청으로 RV-04 진행분을 로컬 커밋한다. 전체 Task 완료가 아니다.

횟수: 최초 통합 검토1/1 진행 중, 이후 수정·재검증0/2. 신규 필수 Finding과 실행 증거는 이 문서에 누적하며 아래 이전 단계의 횟수는 당시 이력이다.

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

## RV-03 최초 개발 재개

현재 기준 c30d6a8. 직전 사용자 승인 진행분 커밋 이후 Git 쓰기는 다시 하지 않는다. 이전 턴은 진행분 정리와 신규 검증 증거를 남긴 progress다. 본 단계는 승인된 Goal/PLAN과 기존 RED를 이어 구현한다.

영속 요청 원문/전체 fingerprint, 동일 프로필 .pending journal, 화면과 독립된 요청 조정 객체, 설정 가능한30초 응답 대기, 권위 영수증 조회 계약을 추가했다. 요청은 발송 전 보관하며 시간 초과는 Unknown이다. 같은 ID/입력/revision/복구 세대로 재시도하고 결과 확인 후 동일 journal만 정리한다. BaseUI.BindingRevision과 controller 세대·활성 화면을 모두 확인하여 이전 완료/오류/정리가 새 UI를 변경하지 못하게 한다. 별도 prefab 작성이나 서비스 전체 Task.Run/메인 스레드 네트워크 Wait/Result는 없다.

첫 구현 컴파일 completed/failed=false. 기존 실패 재현의 동일 UI 검사 **1/1 PASS**, 0.71초, skipped/inconclusive0 (`rv03-ui-initial.json`). 실제 코드 재컴파일 이후에는 PlayMode 실행이1건 선택되어 동작했다. 이전0건 기록은 그대로 남기며 이1건으로 전체 UI 또는 RV-03 완료를 주장하지 않는다. 확정/응답을 독립 통제하는 TCS 시험 서비스를 네 가지 명령으로 확대하고 직접 AC를 추가하는 중이다. 최초 통합0/1·이후 보완0/2 유지.

확대 UI **11/11 PASS**, 5.38초, skipped/inconclusive0 (`rv03-ui-expanded-initial.json`). 정상 지연 출발/확정 후 유실 조회/확정 전 실패 동일 재시도/무응답 대기 만료/이전 시도의 늦은 실패와 새 대기/앱 재시작/같은 뷰 외부 바인딩의 늦은 성공·실패/성장 비용1회/저널 발송 전 쓰기 및 확정 후 정리 실패를 실제 버튼으로 확인했다. 이후 정산과 복구 이전 요청의 직접 UI, journal 원문/파일 경계 테스트를 추가한다.

기존 MetaProgressionFlowTests의 유실 응답 시나리오는 승인된 RV-03 계약에 따라 먼저 관련 설정 차단을 확인하고 결과를 조회한 뒤 설정을 바꾸어 저장된 출발을 이어가는 동선으로 갱신했다. 원래 시작의 와일드/동일 runId/중복 정산 금지 기대값은 유지한다. 미확정 요청을 둔 채 설정을 바꾸는 기존 동선을 유지하는 것이 이번 요청과 충돌하므로 이 직접 영향만 변경한다.

- 추가 정산 및 복구 이전 미결 요청 UI 포함 **13/13 PASS**, 20.01초 (`rv03-ui-all-direct.json`). journal 원문·복사·원자 기록·손상/미지원/타 프로필 보존·복구 세대·복원 영수증 조회 EditMode **10/10 PASS**, 3.93초 (`rv03-journal-direct.json`). 이 기록은 다음 커밋 직전 검사보다 앞선 실행이다.
- 새 요청 대기 도중 옛 응답 및 확정 전 앱 재시작 사례를 추가한 UI15건은 **14 PASS / 1 FAIL**, 22.41초 (`rv03-ui-15.json`). 실패는 씬 전환 직후 Layout 갱신 전 menu 버튼의 raycast가 null인 fixture였다. 기존 Layout 대기를 해당 클릭 전과 직접 영향받는 MetaProgressionFlowTests의 같은 동선에 추가했다. 제품 코드나 assertion 기대값은 변경하지 않았다.
- 후속 MetaProgressionFlowTests 실행은 completed/0건 (`rv03-meta-ui-initial.json`)으로 **WAIT_VALIDATION**이다. 발견과 실행 결과의 불일치 원인은 아직 확정하지 않았다. 성공으로 간주하거나 패키지/설정을 수정하지 않았다.

## RV-03 진행분 커밋 / 2026-09-14

후속 사용자 요청에 따라 c30d6a8 이후 메타 요청 journal/조정 객체/결과 조회/현재 UI 바인딩 소유권과 관련 직접 테스트·신규 .meta·1:1 요약·색인·PLAN/REPORT를 로컬 커밋한다. 기존 사용자 RP2개/동적 폰트, 미추적 설계 원문/도구 요약/로컬 패키지 설정은 제외한다. 전체 RV-03 완료나 Task 완료 커밋으로 주장하지 않으며 RV-04·최신 전체 suite·Android·최초 통합 검토는 계속 미완료다. 최초 통합0/1 및 이후 보완0/2 유지.

커밋 직전 최신 재컴파일은 completed/failed=false, 오류0이다. 아래에 실제 재실행 결과를 추가한다.

- 실제 지연/유실/늦은 응답 UI **15/15 PASS**, 24.72초, skipped/inconclusive0 (`commit-rv03-async-ui.json`). 앞선 Layout 대기 누락 fixture 실패가 이 실행에서 해소됐다.
- 요청 journal EditMode **10/10 PASS**, 3.32초, skipped/inconclusive0 (`commit-rv03-journal-edit.json`). 신규 동작 직접25건 통과이며 전체 검사 통과를 뜻하지 않는다.
- 기존 MetaProgressionFlowTests 커밋 직전 실행도 completed/0건/0초 (`commit-rv03-meta-flow.json`)으로 **WAIT_VALIDATION**이다. 이를 PASS로 보고하지 않으며, Unity 실행 상태 문제의 추가 조사 및 전체 suite는 후속 Goal에 남긴다.
- 커밋 목록은 소스17개·신규 .meta6개·1:1 CodeMap17개·INDEX/PLAN/REPORT3개로 **43개**다. 모든 소스의 .meta/요약 존재와 범위 내 diff whitespace 검사를 확인했다. `commit-rv03-files.txt`, `commit-rv03-source-hashes.json`에 로컬 목록/소스 해시를 보관한다. 실제 test evidence는 기존 ignore된 artifacts/recovery-stability에 있고 Git에는 결과 요약을 기록한다.
- 제외 대상 RP2개/폰트의 이번 커밋 준비 전후 SHA256 동일 여부는 `commit-rv03-excluded-preservation.json`으로 확인한다. RV-01 시작 baseline 대비 동적 폰트 차이는 앞선 보존 관찰대로 여전히 남아 있다. 이번 진행분은 렌더링/폰트 자산을 stage하지 않는다.

## eaa0114 이후 검증 재개

직전 턴은 사용자 요청 커밋 및 최신 직접25건 증거를 추가한 progress다. Git 쓰기는 다시 하지 않는다. Unity ready/stopped, IsRunActive=false, active scene dirty=false 확인 후 EditorUtility.RequestScriptReload를 실행했다. 동일 MetaProgressionFlowTests **3/3 PASS**, 22.15초, skipped/inconclusive0 (`rv03-meta-flow-domain-reload.json`). 코드·테스트·패키지·설정 변경 없이0건 문제가 해소된 실행이다. 도메인에 남는 검사 상태의 영향은 확인했지만 어느 내부 캐시가 원인인지는 확정하지 않았다. 이후 필요 시 검사 종료와 미저장 씬 없음을 확인한 뒤 같은 재로드 경로를 사용한다. 기존0건은 과거 WAIT_VALIDATION 이력으로 보존한다.

RV-03 뒤 RV-01/02 기존 복구 UI **9/9 PASS**, 132.55초, skipped/inconclusive0 (`rv03-recovery-ui-regression.json`). 전용 도메인 재로드 후 실행했다. 실제 지연 UI15/15+journal10/10+기존 메타3/3 및 복구9/9의 직접 범위가 통과하여 RV-04 측정으로 진행한다. 전체 최신 검사와 Android는 별도 미완료다.

## RV-04 최초 측정 준비

계획대로 LocalSaveDiagnostics를 Editor에만 추가해 실제 primary 읽기, decode/무결성, store 문서 Validate, encode, primary/backup 원자 쓰기를 계측한다. 서비스/Core 검증과 복사 등은 전체 동기 시간에는 포함되나 이 구간 합계에 포함되지 않는다고 명시한다. 진단 객체를 연결하지 않으면 수집하지 않고 Player에는 진단 데이터/타이머가 없다. I/O/CAS/백업 순서는 유지했다. 성능 fixture는 실제 명령으로 누적 이력을 만들고 각 조건5회 데이터를 저장한 뒤 로비 갱신의 단일 문서 읽기 계약을 검사한다. 첫 컴파일 completed/failed=false; 측정 결과와 최적화는 아직 없다.

최초 측정은 **1개 실행/1개 FAIL(RED)**, 45.36초 (`rv04-baseline-test.json`)다. 세 조건의 실제5회 표본 저장과 유효성/영수증/체크포인트 검사는 모두 마친 뒤 로비 단일 읽기 assertion에서 실패했다. 원자료 `performance-20260914-043237-071.json`:0/100/1000 과거 런의 파일22,960/55,053/345,760bytes, 로비 읽기 각각 모든 표본2회. 로비 중앙값278.93/538.06/591.39ms, 체크포인트1073.01/1466.00/888.24ms. 각 조건의 개별 변동이 커 이력 증가에 따른 단조 비용이나 실기기 지연으로 일반화하지 않는다.

store 내부 checkpoint decode 중앙값447.49/550.32/404.44ms, encode167.77/262.00/144.51ms에 비해 실제 primary read는0.22/0.22/0.34ms였다. MetaJson이 각 변환마다 Fields resolver를 생성해 타입 계약을 반복 만드는 직접 원인을 확인했다. [Json.NET 공식 성능 지침](https://www.newtonsoft.com/json/help/html/Performance.htm)에 따라 resolver의 타입 메타데이터만 공유한다. 로비 표시에는 ReadDisplaySnapshot1회로 profile/run/settlement를 함께 사용한다. 명령과 잠금 안 CAS는 여전히 실제 파일을 다시 읽는다.

메모리 첫 수치0은 무할당이 아니다. 64KiB 배열을 생성한 별도 calibration도 GC.GetAllocatedBytesForCurrentThread 증가0이어서 현재 Mono 카운터 미지원으로 판정했다. 후속 표본은 지원 여부를 기록하고 미지원이면 allocatedBytes=-1, 별도 monoHeapDeltaBytes(살아 있는 힙 증감, GC 시 음수 가능)를 보조 지표로 쓴다. baseline0을 절감 비교에 사용하지 않는다.

개선 후 동일 시험 **1/1 PASS**, 6.04초, skipped/inconclusive0 (`rv04-improved-test.json`), 원자료 `performance-20260914-043844-914.json`이다. 문서 크기/이력 수는 동일하고 모든5회 로비 읽기1회, checkpoint read2회(후보+잠금 안 CAS)/primary write1/backup write1·영수증 불변을 확인했다. 아래는5회 중앙값이며 Editor/Mono/warm OS cache 측정이다.

| 과거 런 | bytes | 로비 ms 전→후 | checkpoint ms 전→후 | 후 checkpoint Mono 힙 증감 중앙값 |
|---|---:|---:|---:|---:|
| 0 | 22,960 | 278.93→5.10 | 1073.01→19.73 | 2,015,232 bytes |
| 100 | 55,053 | 538.06→6.00 | 1466.00→22.01 | 3,313,664 bytes |
| 1000 | 345,760 | 591.39→20.10 | 888.24→53.03 | 6,291,456 bytes |

직렬화 metadata cache는 데이터 권위를 캐시하지 않는다. 각 명령/저장 시 revision·sequence·복구 세대를 다시 검증한다. 실제 변경이 없는 설정의 requestId 기록/정산 receipt 삭제/압축/DB 전환은 하지 않았다. 소규모 최적화 범위에서 종료하며1000런에서 남는53ms 동기 비용, 힙 증감과 실제 기기 혼합 이력 측정/보관 정책은 FOLLOW_UP으로 둔다. 단일 값/순수 할당 또는 모바일 FPS 개선으로 주장하지 않는다.

RV-01~04 직접 검증 후 최초 통합 검토1/1을 시작한다. 읽기 전용 보조 검토1명(재위임0), 메인 최신 전체 검사/Android/보존 확인을 한 묶음으로 진행하며 이후 보완은0/2다.

## 최초 통합 중간 결과 및 RV-04 진행분 커밋 / 2026-09-14

후속 사용자 커밋 요청을 반영한다. eaa0114 이후 로비 표시 문서1회 읽기, JSON 타입 계약 재사용, Editor 저장 단계 계측, 유효한 누적 런 성능 시험과 관련 회귀/요약 문서를 포함한다. 요청 창 결함 수정이나 새로운 테스트 실행을 이 커밋 준비에 추가하지 않는다. 직전 실제 검사 원자료를 다시 읽어 아래 상태를 확인했으며 과거 직접 PASS를 전체 통과로 대체하지 않는다.

- 최신 소스의 최초 전체 EditMode **317개 실행:303 PASS/14 FAIL**,46.49초, skipped/inconclusive0 (`artifacts/recovery-stability/integrated-edit-initial.json`). 실패14건은 모두 CoreReplayTests.PreExtractionCommandsKeepEveryStateFieldAndChoiceId의7개 시나리오×재개 여부2개에서 sequence1 전체 상태 해시 불일치다. 이를 FAIL로 유지한다.
- **RS-IR-01 / P2 / OPEN:** RunUIController.MetaRequests.cs의 OpenMetaRequest는 표시 세대를 바꾸고 대기 화면을 다시 그리지만 진행 중 요청의 완료 관찰을 새 바인딩에 연결하지 않는다. `새 여정→요청 창 닫기→완료 전에 재개→성공/실패/시간 초과` 때 이전 관찰자는 바인딩 소유권 검사로 종료하고 새 화면의 확인/재시도는 비활성으로 남는다. 읽기 전용 검토 후 메인이 원본의 OpenMetaRequest/ObserveMetaRequest/RenderMetaRequest 흐름을 대조했다. 해결 조건은 현재 대기 요청 관찰을 새 화면에 연결하고 완료 전 닫기/재개 TCS 성공·실패·시간 초과를 실제 UI로 검증하는 것이다. 이전 응답의 새 Session/이동/잠금 변경 금지는 유지한다. 해당 재개 순서의 실행 재현은 아직 미실행이다.
- **RS-IR-02 / OPEN:** 위 전체 상태 재현14건의 해시 실패로 기존 게임 상태 보존을 아직 입증하지 못했다. 신규 저장용 recoveryGeneration 필드의 역사적 비교 투영 누락 가능성을 조사 중이며 원인은 확정하지 않았다. 해결 조건은 실제 필드 차이를 확인하고 기존 게임 상태/선택 ID/난수 기대값을 유지한 회귀 통과다. 근거 없이 기준 fixture를 재생성하거나 assertion을 삭제하지 않는다.
- 전체 PlayMode와 Android ARM64 IL2CPP 개발 빌드는 이번 소스로 아직 미실행이다. 최신 통합 검증 **WAIT_VALIDATION**, 실행한 전체 EditMode **FAIL**을 구분한다. 승인된 격리 기기가 없어 기기 중단/복귀 검사는 **NOT_RUN**이다.
- 기존 RP2개/동적 폰트 및 미추적 설계/도구 요약/로컬 패키지 설정은 커밋에서 제외한다. RV-01 시작 대비 폰트 차이는 기존 보존 관찰대로 미해결이며 임의로 HEAD 내용으로 되돌리지 않는다. 진행분 보관은 해당 차이의 해소를 뜻하지 않는다.
- 최초 통합 검토1/1 진행 중, 이후 수정·재검증0/2 유지. 이 커밋은 진행분이며 모든 필수 검증 PASS 또는 전체 Task 완료를 주장하지 않는다.
- 커밋 준비 검사: 정확한23개 파일(소스9/신규 .meta2/1:1 CodeMap9/INDEX·PLAN·REPORT3)을 확정하고 모든 소스의 .meta·요약 존재 및 범위 내 whitespace 검사를 확인했다. 요약 문서 끝의 불필요한 빈 줄만 정리했으며 제품 소스는 검사 실행 이후 변경하지 않았다. `commit-rv04-files.txt`, `commit-rv04-source-hashes.json`, `commit-rv04-excluded-before.json`에 파일 목록·소스 해시·제외13개 파일의 준비 시점 해시를 보관한다.
