# NEXT_PHASE_06 · Q0/Q1 REPORT

2026-10-23 · IMPLEMENT(검증 보강) · **Q0 PASS / Q1 PASS · 완료** · 기준 HEAD `859ac227e809db7263f7670d34539f7ff1bab8c9` / main.

## 범위

[제공 계획](../Dice_Warrior_Next_Phase_06_Validation_Balance_God_Power_Plan_v0.1.md)을 자료로 확인하고 [실행 PLAN](../Plans/NEXT_PHASE_06_VALIDATION_BALANCE_GOD_POWER_PLAN.md)에 Q0→Q1만 채택했다. 현재 규칙 v1~v4와 서버 v1 정산 의미/공동 경기 영구 지급0은 변경하지 않는다. B0/C7 밸런스·G0 신격/권능·E0 확률/상태·C6 권위 정산·D0 Android/Firebase는 DEFERRED다. 검증 완료 후 사용자가 관련 코드·문서만의 로컬 커밋을 별도로 승인했다.

## 환경과 보호

- 시작 HEAD 일치, staged0, 실제 사용자 변경11파일(기존10+이번 제공 원문1)을 SHA256으로 기록했다. 제품/콘텐츠·패키지·구형 골든·자산의 별도 보호 목록도 기록했다.
- 기존 Editor44732의 Pipeline 검색 파일이 없어 공식 CLI 연결이 실패했다. 사용자는 저장 후 직접 재시작을 선택했고, 새 Editor60424/port7800에서 ready/컴파일 중 아님/Play 정지/Title 씬 dirty=false를 확인했다. 에이전트가 Editor를 강제 종료하거나 검색 파일/인증 상태를 만들지 않았다.
- Editor 재시작 직후 기존 사용자 소유 `Pretendard-Effects SDF.asset` 해시 변화1건을 발견했다. 그 외 사용자 파일은 그대로였다. 해당 폰트를 수정·복원하지 않고 전후 해시와 발견 시점을 기록한다. 원인과 사용자 편집 여부를 단정하지 않는다.
- 이전 .NET NuGet 캐시가 Unity Temp 아래에 있어 재시작 후 사라졌다. HTTP 검사는 이 때문에 처음에 테스트 발견 전 빌드 실패했다. 기존 pinned 패키지 버전을 `artifacts/next-phase-06/nuget`에 복원해 재실행했다. 제품/package lock/전역 설정 변경 없이 검증 환경 경로만 바꿨다.
- 최초 긴 경로 서버(port62008)는 종료했다. 최종 Q1은 짧은 `artifacts/next-phase-06/q1-a390/authority.db`, 고유 loopback port55619를 사용했다. 기존5087/실제DB/사용자 persistentDataPath를 사용하지 않았다. 소유 PID/시작시각을 확인해17652→20704로 같은 endpoint/DB를 재시작하고 최종 종료했다.

## 실제 실행 기록

| 대상 | 현재 결과 | 설명 |
|---|---|---|
| 공유 .NET 최초 | 118/118 PASS | 현재 HEAD의 기존 검사 새 실행 |
| 공유 .NET 보강 후 | 119/119 PASS | 체크섬·손상 백업 거절 추가1 |
| 기존 서버 권한/SQLite/골든 | 13/13 PASS | 사용자DB 없이 격리 fixture |
| 실제 HTTP/복구/M4 통합 | 25/25 PASS | 새 프로세스·임시DB,503·fault·영수증·계측 경계 |
| 계측·비용·보관 계산 | 32/32 PASS | 실제/가상/미산정 구분, 기존 가격 사용 |
| Unity 컴파일 | PASS | 최종 경로 수정 후 오류0, R3/R3-compile-final.json |
| Unity EditMode | 202/202 PASS | 보강 저장검사1개 포함, 14.32초 |
| Unity PlayMode | 25/25 PASS | 최종 기본 집합 24.07초, 실패/생략0. 기존24+구형 v1/v2 재개1 |
| Q1 Unity 실제 HTTP 여정 | 5/5 PASS | 아래5개 개별 실행, 실패/생략0. 최초 경로 실패 증거도 보존 |
| Android/실제 Firebase/C6 | NOT_RUN/미구현 | 이번 범위 밖 |

## 최소 검증 보강

기존 규칙을 다시 구현하는 검사를 만들지 않고 원문의 실제 공백만 보강한다. `CompetitionV4StoreTests`는 유효JSON의 checksum 불일치와 깨진 backup 복원 거절 후 파일/공개 상태/generation 보존을 확인한다. `CompetitionFlowTests`는 새 v4 앱에서 구형 v1/v2 그대로 재개·취소 불변·확정 후 원문 보존을 검사하며, 기존 상세 페이지와3모험가 카드를720×1600 inset에서도 검사한다. 신규 Q1 fixture는 실제 HttpGameBackend/프로필·로컬 경기·UI를 연결하고 인프로세스 권한 대역으로 우회하지 않는다.

고정 골든은 SymbolLoop v1 4벡터/125상태, SharedWorld v1 2벡터/50해시, v3 시작·종료3쌍이다. v2는 리터럴 부분조합·동일명령 재현 검사이며 독립 전체 골든이 있다고 주장하지 않는다.

직접 검증 대응: 태그 Owner/피해 타입 누락과 출처·중첩·변환은 CompetitionTag/Modifier/Element/V4 검사, 조회 RNG 불변·구형 보존은 Golden/Growth/PartialCombo/V4 검사, 잠금·CAS·손상/중복은 Store/V4Store/AdventurerSelectionStore 검사로 확인한다. UI의 포인터·연출·상세 페이지·safe area와 v1~v3 재개는 CompetitionFlowTests, 실제 HTTP 프로필·응답 유실·재시작은 신규 CompetitionHttpFlowTests가 맡는다. 구형 서버 정산 검사는 v1 fixture만 보내며 v4 로그를 서버에 제출하지 않는다.

## Q1 실제 여정 결과

| 검사 | 결과 | 실제 확인 |
|---|---|---|
| HttpJourneyPreservesFrozenMatchAndPaysNothing | PASS · 8.23초 | 3모험가의 다음 경기 적용·취소/확정 보존, seed33의45명령 후13라운드 전원 완료, 전투 재개 스냅샷 동치. 물리/화염/냉기/번개가 실제 로그에 관찰됨. 서버 프로필 전체 불변·v4 start/settle/grow0 |
| ProfileFaultsPreserveCheckpointAndRetry | PASS · 1.49초 | 실제 before-abort/after-delay2건, 프로필 조회5회 중 transport오류2회. 기존 경기/원문 보존과 명시 재조회 회복 |
| LegacyActiveRunBlocksGrowthWithoutSubmitting | PASS · 0.56초 | fixture setup의 v1 start1회만 발생. 기존 ticket/active run·미결 정산 원문 보존, 영구 성장 구매 보류, pending 재진입 자동 요청0 |
| PrepareLostGrowthResponseForServerRestart | PASS · 0.61초 | 성장 POST 승인 후 after-abort: 서버 성장1·재화0, 로컬 성장0·pending 유지. 네 파일/명령본문·ID·revision 증거 저장 |
| ResumeLostGrowthResponseAfterServerRestart | PASS · 0.68초 | 동일 endpoint/DB 서버 재시작·App 재생성 후 같은 ID/body 재전송. 서버 revision 중복 증가 없음, 성장1·재화0, 경기/선호/동결 성장0 불변 |

일반 경기의 profile 조회는 authored OpenHttp1회+관찰 handler6회로 총7회이며 영구 쓰기가 없다. 별도 legacy/setup·성장 복구 시나리오의 POST를 일반 경기 보상과 섞지 않는다. 실제503은 기존 HTTP25개 검사에서 확인했으며 Q1 Unity의 두 프로필 fault와 구분한다. Unity 재진입은 같은 Editor 내 App 재생성·도메인 재로드로 확인했으며 Android 실기기/강제 OS 종료 검증을 뜻하지 않는다.

## Finding·실행 장애와 판정

- Q1-01 CLOSED: 신규 검사 meta의33자리 GUID로 Unity가 스크립트를 무시했다. 신규 미연결 meta만 유효32자리 고유 값으로 수정했다. 기존25+신규5=30개 발견을 실제 확인했다. 최초0개 실행은 PASS가 아니다.
- Q1-02 CLOSED: Unity NUnit이 NonParallelizable 속성을 제공하지 않아 발생한 CS0246을 해당 속성 제거로 해결했다. Q1은 Explicit+Category와 메인의 개별 직렬 실행으로 분리한다. 일반 전체 DiceWarrior PlayMode 실행25/25도 재확인했다.
- Q1-03 CLOSED: 최초 checkpoint250자+임시 접미사=287자로 Unity DirectoryNotFoundException을 재현했다. 사용자 추가 승인 후 서버 root를 충돌 검사한 q1-4자리, 사례 폴더를 j/p/l/r로 줄였다. 계정 GUID/endpoint 해시 격리는 유지하며 원자 저장·복구·보존 접미사를 파일 접근 전 검사한다. 최종 account183자/경기207자/최장 임시259자(<260), 실제5여정 PASS로 해결했다. 제품의 긴 경로 지원 변경으로 범위를 확대하지 않았다.
- 최초 읽기 전용 검토1묶음 후 지정된 경로 Finding만 확인했다. 사용자 승인으로 상한은4회(기본2+추가2), 총3회 사용(기본2+추가1) 후 완료했다. 남은 필수 Finding0. 처음 제기된 PowerShell 백슬래시 지적은 실제 문자 코드·환경변수 readback으로 반증해 철회했다.
- 독립 검사 사이 Unity의 테스트 검색이0개를 반환한 경우 도메인 재로드 후 실제 개수로 재확인했다.0개/skip/컴파일 성공만으로 게임 검증 PASS를 주장하지 않는다.

## 증거·보호·정리

- [증거 디렉터리](Evidence/NextPhase06/): Q0 TRX·컴파일/Edit/Play JSON과 캡처7개, Q1의 최초 실패/진단 및 최종 [R3 증거](Evidence/NextPhase06/Q1/R3/), 실행 스크립트 사본/서버 수명을 보관한다.720×1600 모험가 카드/스킬과 Q1 자연 종료 화면을 직접 열어 확인했다.
- M4 최종 실측은 전체4시험 계정의 HTTP25회(profile20/start1/grow2/operation2), 중복1, 오류·미완료4다. 오류4는 주입한 프로필2/성장응답유실1 및 구매 전 operation-not-found1이며 검사 실패 수가 아니다. 제출 명령/로그0, Firestore 가상 모델·운영 실측 없음·실제 청구액 아님을 구분한다. 초기2회 진단 화면은 이력이며 최종 R3 화면과 혼동하지 않는다.
- 제품/콘텐츠·패키지·구형 골든·Title/CompetitionScreen을 포함한 보호57파일 해시 변화0. 사용자11파일 중10개 그대로이며 폰트1개는 앞서 기록한 Editor 재시작 후 해시와 최종 해시가 같았다. 임의 복원/staging을 하지 않았다.
- C#4개와 각각 CodeMap4개, 신규 meta2개, README/INDEX 및 PLAN/REPORT를 갱신했다. Unity 소유 C#65개이며 신규 GUID 두 개는32자리·중복 없음이다. 제품 C#·규칙·밸런스·씬/프리팹 수정0이다.
- Unity의 Q1 환경변수3개를 해제했고 최종 Title dirty=false/Play 정지/컴파일 정지를 확인했다. 마지막 소유 PID20704 종료를 확인했다. 기존5087 서버와 사용자DB는 건드리지 않았으며 진단용 임시 DB/시험 폴더는 ignored artifacts에 보존했다.
- 검증 종료 시 Git staged0, 기준 HEAD/branch 유지, commit/push 없음을 확인했다. 이후 로컬 커밋 승인은 아래에 구분했다. **이번 Q0/Q1 묶음은 완료**다. B0/G0/E0/C6/D0는 별도 단계다.

## 커밋 범위

2026-10-23 사용자 승인: Q0/Q1 검사4개·신규 meta2개·CodeMap4개·README/INDEX·PLAN/REPORT와 증거76개, 총90파일만 로컬 커밋한다. 설정/폰트/사용자 원문5개/로컬 패키지 설정을 포함한 기존 사용자11파일은 제외한다. 커밋 준비 시 검증 당시 C#4개 해시와 현재 소스가 일치했고 기존 staged 변경은0이었다. 실제 파일 목록과 제외 해시는 ignored artifacts의 commit-files.txt/commit-excluded-before.json에 기록하고, 최종 Git commit tree와 사용자 파일 해시를 대조한다. 소스 변경이 없어 완료 검사를 반복하지 않았으며 push/branch 변경은 수행하지 않는다.

스테이징 후 공백 검사에서는 원본 접근성 캡처 `Evidence/NextPhase06/Q1/dashboard-cost.txt`의 `AXListMarker` 22행에 끝 공백이 관찰됐다. 캡처 원문은 그대로 보존하며 이 파일을 제외한89파일의 staged diff 공백 검사를 별도로 확인한다. 코드·기획·검증 결과 변경은 없다.

## 후속

현재 범위에서 추가 사용자 결정이나 미해결 필수 결함은 없다. 다음 B0/C7은 시뮬레이션 표본/AI 평가 정책/밸런스 판정 기준을 먼저 확정하는 별도 구현 단계다. 신격 이관·권능 포인트·AI 예산·재굴림 규칙과 C6 보상은 해당 단계에서 결정한다. 기존 완료 문서와 규칙 기대값을 새로 열어 수정하지 않았다.
