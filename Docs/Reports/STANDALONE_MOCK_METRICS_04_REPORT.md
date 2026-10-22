# STANDALONE_MOCK_METRICS_04 REPORT

2026-10-22 · IMPLEMENT 완료 · 기준 main/f51a4782d8aefd984f80243fa0c194b02ba85102. 승인된 M4만 구현했다. 실제 Firebase 연결·M5 규모 부하 시험·게임 규칙 변경·Git 커밋은 수행하지 않았다.

## 결과와 확정된 결정

사용자 결정은 서울 `asia-northeast3`, Firestore Standard, USD, 무료 할당량 미차감 기본값이다. `Server/DiceWarrior.Metrics`에 로컬 계측/집계·Firestore 논리 모델·가격 JSON/비용 계산을 추가하고 기존 독립 HTTP/SQLite 서버에 연결했다. `/dev/dashboard`에서 개요, 일별/API/런 비교, 비용 시나리오, 내보내기/보관 범위를 확인한다. [실행 안내](../../Server/README.md#m4-계측과-비용-대시보드)를 따른다.

실제 HTTP 엔터티 바이트·완료/실패/중복/동시 요청·지연 백분위·SQL provider 실행/행/transaction/CAS/busy·재현 벽시계·프로세스 CPU/메모리·파일 크기를 측정한다. SQL과 Firestore 문서 수는 분리한다. 권한 승인 뒤 응답 유실은 승인과 전송 실패를 모두 남긴다. 결과를 확인하지 못한 권한 실패는 미산정으로 보존하고 CSV에도 지원 여부/분류와 빈 숫자를 쓴다.

일별/API/런별 표는 SQL·로그 bytes·재현 ms와 가상 문서 R/W/D·문서 작업 USD를 나란히 표시한다. 이 USD는 문서 작업만의 부분 비용이며 저장/전송/Cloud Run/무료 구간/세금은 제외한다. 전체 비용 시나리오는 입력한 DAU/빈도·문서 크기·보관·활성 시간·vCPU/메모리로 별도 계산한다.

계측은 DB와 분리된 journal에 HMAC 가명 메타데이터만 기록한다. 원본 ID·본문·명령 내용·토큰은 보관하지 않는다. 7일/16×4MiB/최대10,000표본의 먼저 도달한 한도를 적용하고 누락/손상을 알린다. observer/파일 오류가 정산을 실패시키거나 rollback하지 않는다. 같은 디렉터리의 복수 collector는 파일 잠금·별도 journal·공통 가명 키를 사용한다.

## 최신 실제 검증

| 검증 | 결과 | 증거 |
|---|---|---|
| 서버 권한/SQLite/골든 | 13/13 PASS | [authority-final.trx](Evidence/StandaloneMetrics/authority-final.trx) |
| 실제 HTTP 전체 | 25/25 PASS | [http-final.trx](Evidence/StandaloneMetrics/http-final.trx) |
| 계측/가격/논리 모델 | 32/32 PASS | [metrics-final.trx](Evidence/StandaloneMetrics/metrics-final.trx) |
| Unity Editor 회귀 | 83/83 PASS, 실패/생략0 | [unity-editmode.json](Evidence/StandaloneMetrics/unity-editmode.json) |
| DLL 경계·규칙 동치 | Editor 전용·명시 참조, Player 참조 없음 | [unity-boundary.json](Evidence/StandaloneMetrics/unity-boundary.json) |
| 실제 브라우저 | 개요/세 종류 집계/시나리오 변경/내보내기 링크 확인 | [browser-verification.json](Evidence/StandaloneMetrics/browser-verification.json), [집계 표시](Evidence/StandaloneMetrics/browser-groups.txt) |
| 보호/문서 검사 | 보호536 중 의도한 DLL1개만 변경, CodeMap 누락0, staged0 | [protected-audit.json](Evidence/StandaloneMetrics/protected-audit.json) |

HTTP25개에는 기존18개와 M4의7개가 포함된다. 실제 정산 후 동일ID·새ID 재제출, 불법 재현, 형식 오류, before-abort/after-abort/after-delay, 계측 디렉터리 장애, 권한 DB 초기화 실패, 로컬 Host 제한, 자기 조회 제외, 개인정보 없는 JSON/CSV를 확인했다. 중복 지급은 없고 골든 승리 보상50/잔고70이 유지된다. 별도 표본에서는 성장20 차감 후 잔고50을 확인했다.

Metrics32개는 저장/SQLite13개와 비용19개다. HMAC·보관/손상·여러 collector·observer 예외, 손계산·무료 경계·한국 전송 구간·요금 누락/잘못된 카탈로그·입력 상한을 확인했다. 실제 observer가 예외를 계속 던져도 골든 지급은 한 번이다.

새 비용/계측의 최초 RED와 최종 GREEN은 `artifacts/standalone-metrics-04/`에 보존했다. HTTP 시험 작성 중 raw transport가 예외를 던지는 계약을 Coordinator의 결과 불명 반환으로 잘못 기대한3건을 수정했다. Unity 첫 호출은 잘못된 asmdef 필터로0건이었으므로 PASS 근거로 쓰지 않고 실제 `SymbolLoop.EditMode.Tests`의83건 결과를 사용했다. `unity status`가 빈 목록을 보여 열기 요청을1회 했으나, Pipeline/프로세스 재확인에서 기존 PID9780/6000.6.0f1과 그 자식 worker만 확인했다. 이후 기존 Editor만 사용했으며 중복 프로젝트 Editor를 유지하거나 강제 종료/패키지 갱신을 하지 않았다. 임시 HTTP 시험 환경 변수는 해제했다.

## 실제 표본·비용 근거

[sample.json](Evidence/StandaloneMetrics/sample.json) / [sample.csv](Evidence/StandaloneMetrics/sample.csv)는 사용자 저장과 분리된 고유 DB에서 만든8요청 표본이다. 실패2(형식/응답 유실), 중복3, SQL 명령73, 실제 읽기/변경 행44와 별개로 가상 Firestore 읽기14/쓰기10/삭제0을 기록했다. 지연·CPU·파일 크기는 해당 개발 PC/실행 시점의 관측치이며 처리량 기준선이 아니다.

[cost-default.json](Evidence/StandaloneMetrics/cost-default.json)은 DAU1,000·사용자당3런/일·30일·기타 기본 가정·무료 미차감에서 모델 포함 항목 기준 USD2.468477495…이다. 브라우저에서 DAU10,000+무료 적용으로 바꾸어 기준 USD17.62 표시를 확인했다. 실제 청구액/보장 상한이 아니며 제외 비용은 결과에 명시한다. 최소/최악은 명시한 입력을 조정한 계획용 구간이지 신뢰구간이 아니다.

2026-10-22 공식 공개 [Firestore 요금](https://cloud.google.com/firestore/pricing), [과금 규칙](https://firebase.google.com/docs/firestore/pricing), [Cloud Run](https://cloud.google.com/run/pricing), [네트워크](https://cloud.google.com/vpc/network-pricing)의 서울 선택 표를 확인했다. 기본 표시 지역 요율을 복사하지 않았다. [확인 요약](Evidence/StandaloneMetrics/pricing-verification.json), [버전별 카탈로그](../../Server/DiceWarrior.Metrics/Pricing/seoul-standard-usd.json)에 단위/출처를 남겼다. 공용 문서 조회 외 Firebase SDK/프로젝트/API/배포는 사용하지 않았다.

Cloud Run의 서울 무료 연산 혜택은 Tier1 금액 크레딧을 사용한다. Firestore 동일 리전 서버 전송과 Cloud Run→한국 Premium 전송을 분리하고 Firestore 무료10GiB를 후자에서 차감하지 않는다. 무료 옵션은 계정의 미사용 할당량이 전부 남았다고 가정한다. 가격 파일 누락/미지원은 무료0으로 표시하지 않는다.

## 검토·보호·문서

최초 독립 검토1묶음, 통합 수정·재검증2/2를 사용했다. M4-R1(권한 진입 후 실패를0작업으로 표시)·M4-R2(CSV 미산정 소실)를 수정하고 소스/실제 실패 fixture로 닫았다. 활성 시간을 CPU 시간으로 부르던 UI 라벨도 고쳤다. 마지막 원안 대조에서 일별/API/런별 비교 표의 문서 작업 부분 비용·로그/재현 표시를 연결하고 HTTP25개 및 브라우저만 직접 재확인했다. 남은 필수 Finding0, 무관한 재탐색 없음.

사용자 RP2·폰트·ProjectSettings·Packages2·원본 제안7개를 보호했다. 536개 기준 해시의 유일한 변경은 허용된 Editor 전용 Authority DLL이며 meta/GUID는 같다. Assets 게임 소스·Scene/Prefab·Packages·기존 완료 PLAN/REPORT/Archive는 보존했다. DLL은 Editor API로 import했다. 의도된 서버 프로젝트 참조/lock 파일2개 신규+호스트 lock 변경만 포함하며 Unity package lock은 같다.

GDD/Docs README/Server README, CodeMap 색인과 모든 추가·수정 C# 요약(서버 전체28개)을 동기화했다. Git diff whitespace 검사 PASS, stage/commit/push/branch 변경 없음. 원본 제안은 사용자 파일로 유지한다.

## 한계·후속

실제 Firebase/운영 과금·클라우드 CPU/동접·M5 규모별 부하·두 기기·Android는 NOT_RUN. 게임 UI 변경이 없어 이번 PlayMode 재실행은 비적용이다. 재현은 실제 벽시계만 분리 측정하고 CPU는 현재 서버 프로세스 누적값이다. provider 내부 busy 재시도·HTTP 헤더/TLS/압축/최종 수신 보장은 측정하지 않는다. 계측은 제한된 보관 표본이며 HMAC 가명은 익명화를 보장하지 않는다.

추가 필수 사용자 결정은 없다. 다음 승인 범위는 M5 규모별 부하 시험이며 실제 Firebase 연결은 별도 요청 전까지 진행하지 않는다.

![M4 실제 로컬 대시보드](Evidence/StandaloneMetrics/dashboard-overview.png)

## 후속 로컬 커밋 (2026-10-22)

완료 보고 후 사용자의 커밋 승인을 받았다. 위 구현·문서·증거67파일만 포함하고, 기존 사용자7개 변경과 실제 저장/임시 서버 DB/계측 키는 제외한다. 이번 단계는 코드 변경 없이 보호 해시·기존 검사 결과·커밋 파일 목록과 whitespace를 대조한다. 검사를 불필요하게 재실행하지 않으며 새로운 실행 결과로 재표기하지 않는다. 원격 push는 수행하지 않는다. 정확한 커밋 해시와 커밋 후 제외파일 확인은 로컬 `artifacts/standalone-metrics-04/commit-result.json`에 남긴다.
