# FirebaseOperationModel.cs

- 역할: 보관된 HTTP 측정 한 건을 Firestore Standard 문서 작업의 **가상 계획**으로 설명한다. 실제 Firebase 호출·계측·요금 청구가 아니다.
- 핵심 동작: `Describe(RequestMeasurement)`는 authorityOutcome/domainCode와 중복 진단만 분기한다. SQL 횟수와 SQLite CAS 재시도 횟수를 문서 읽기로 바꾸지 않는다. 모델 버전은 `firestore-logical-plan-1`이다.
- 계획: start는 읽기2/쓰기3, settle은 읽기4/쓰기4, grow는 읽기2/쓰기2. 같은 요청 ID는 읽기1/쓰기0, 새 요청 ID의 이미 제출된 런은 읽기3/쓰기1이다. 검증 거절은 해당 읽기와 거절 영수증 쓰기1이며 지급 쓰기는 없다. profile/operation/run 조회는 각각1/1/2읽기다.
- 실패: 권한 진입 전 HTTP 오류 및 권한 입력 한도 거절은 Firestore0이다. 권한 결과 불명/저장소 실패는 `supported=false`로 미산정하며0으로 단정하지 않는다. 응답 유실 후 authorityOutcome approved는 정상 커밋 작업을 유지한다.
- 입출력: RequestMeasurement → OperationEstimate(읽기/쓰기/삭제/인덱스·최소조회 읽기/트랜잭션 시도/새 보관 바이트/HTTP 출력 바이트/가정). storageBytes는 기본 문서 크기와 로그를 사용한 **새 바이트 증가량**이며 egressBytes는 관측 HTTP body 출력 바이트다. 둘 다 실제 Firestore 저장/청구 바이트가 아니다.
- 사용하는 대상: MeasurementContracts의 DTO/DiagnosticTotal. 사용하는 쪽: 로컬 `/dev/metrics`·내보내기·비용 화면의 논리 사용량 표시 및 CostEstimatorTests. 관계 근거: 고정 endpoint와 Authority 진단 kind 중 duplicate-operation/duplicate-submission, domainCode에만 직접 의존한다.
- 상태/수명: 정적 순수 계산, 외부 I/O와 공유 변경 상태 없음. SQL/CAS 수치가 가상 읽기를 증폭시키지 않는다.
- 검수 주의: 직접 문서 키 조회·기존 프로필·run 안에 로그·보안 규칙/인덱스 스캔 없음은 가정이다. 실제 Firebase 설계가 정해지면 모델 버전을 바꿔야 한다. unknown 결과는 따로 집계한다.
