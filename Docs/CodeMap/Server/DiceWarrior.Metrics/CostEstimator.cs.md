# CostEstimator.cs

- 역할: 명시적 시나리오와 요금 카탈로그로 최소/기준/최악 계획 비용을 오프라인 산출한다. Firebase 연결·로컬 SQLite→Firestore 비용 환산은 없다.
- 입출력: CostScenario + PricingCatalog → CostEstimate(minimum/basis/worst, 모델/카탈로그 버전, 시나리오 복사본, 가정/미포함 항목). 각 CostVariant는 CostUsage와 항목별 quantity/billableQuantity/freeCreditUsd/costUsd를 반환한다.
- 핵심 동작: DAU·일수·런/성장/조회량, 같은 요청·새 제출 ID 중복, 권한 전/후 오류 비율과 가정한 트랜잭션 추가 시도로 문서/요청량을 계산한다. 재시도는 읽기와 실행 시간을 늘리고 커밋 쓰기를 반복하지 않는다. 일수1..31, DAU0..100만 등 모든 입력을 유한 범위로 제한하고 decimal로 계산한다.
- 저장량: profile은 고정 사용자 집합, 런/receipt/operation/로그는 매일 누적 후 retentionDays로 제한한다. 인덱스/메타데이터 추가 비율을 적용한 일별 평균과 GiB-month를 구분한다. 명시적 보관 기한 삭제를 가정하며 Firestore TTL은 아니다. newly ingested bytes와 평균 저장량을 분리한다.
- 무료: 명시 선택 시 문서 읽기/쓰기/삭제는 일별, 저장량은 매일 차감한다. Cloud Run은 서울 요율의 초 단위 차감 대신 Tier1 기준 월 USD 크레딧을 차감한다. Firestore10GiB 인터넷 전송 할당량은 Cloud Run→한국 전송에 사용하지 않는다.
- Cloud Run/전송: 단일 요청 동시성·최소 인스턴스0·request-based·입력 실행시간100ms 올림을 가정한다. CPU는 vCPU-second, 메모리는 GiB-second이며 로컬 재현 벽시계/프로세스CPU와 별개다. Firestore→같은 리전 서버와 Cloud Run→한국 Premium 인터넷 전송을 별도 표시한다.
- 범위: 최소는 중복/오류/재시도0과 크기·실행시간0.5배, 기준은 입력, 최악은 비율 상한을 지키는2배와 크기·실행시간2배다. 통계적 신뢰구간이 아니다. 일반 로그 서비스·Auth·배포/백업/세금 등은 excludedCosts에 명시한다.
- 사용하는 대상/쪽: PricingCatalog의 검증된 불변 요금, FirebaseOperationModel의 버전, Newtonsoft로 입력 복사. `/dev/costs`와 CostEstimatorTests가 호출한다. 관계 근거는 Estimate 서명과 고정 rate key다.
- 상태/실패: 순수 동기 계산, 원본 시나리오 미변경. 지역/에디션/통화 불일치는 예외, 누락 요율은 해당 항목 unestimated 및 totalUsd=null이며 알려진 항목만 knownSubtotalUsd에 합산한다.
- 검수 주의: totalUsd는 명시 모델 항목 합계이며 실제 전체 청구서가 아니다. 무료 잔여량·월 경계·기존 사용자 수·보관정책은 운영에 연결하기 전 다시 결정해야 한다.
