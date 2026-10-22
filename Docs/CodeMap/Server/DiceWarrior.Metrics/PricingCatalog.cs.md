# PricingCatalog.cs

- 역할: 버전이 있는 로컬 JSON 가격표를 읽어 검증된 불변 카탈로그로 제공한다. 요금 숫자는 C# 상수가 아니다.
- 핵심 동작: Load(path)/LoadDefault()가 최대64KiB·깊이16의 JSON을 읽는다. 중복 키/추가 루트/NaN·음수·과도한 수치·없는 단위·잘못된 구간/출처를 거절한다. 서울 asia-northeast3·Firestore Standard·USD·Cloud Run request-based·한국 목적지만 지원한다.
- 입출력: `Pricing/seoul-standard-usd.json` → 읽기 전용 version/verifiedOn/region/edition/currency/sources/notes/rates/freeQuota. DefaultPath는 실행 출력 폴더이며 프로젝트 파일이 JSON을 복사한다. PriceRate의 단일요율 또는 증가하는 계층 구간은 읽기 전용이다.
- 가격 출처: 공식 Firestore/Cloud Run/Google Cloud 네트워크 공개 페이지의 **서울 선택값**을2026-10-22 확인했다. 원문 기본 표시인 Iowa 요율을 서울에 사용하지 않는다. 카탈로그에 단위·확인일·출처·가정을 함께 보관한다.
- 사용하는 대상: Newtonsoft JObject/JsonTextReader, 파일 I/O와 읽기 전용 collection. 사용하는 쪽: CostEstimator와 로컬 개발 API 및 CostEstimatorTests. 관계 근거: Estimate가 카탈로그의 rate key/units/freeQuota를 사용한다.
- 상태/수명: 호출마다 별도 인스턴스, 배열·사전은 읽기 전용이다. 외부 URL은 근거 문자열이며 HTTP 연결을 실행하지 않는다.
- 실패/검수: 지원하지 않는 선택을 기본값으로 대체하지 않는다. 누락 rate key 자체는 허용하여 계산기가 unestimated/null 합계로 표시하며, 존재하는 rate의 잘못된 숫자나 단위는 오류다. 실제 청구·할인/무료잔액·환율은 확인하지 않는다.
