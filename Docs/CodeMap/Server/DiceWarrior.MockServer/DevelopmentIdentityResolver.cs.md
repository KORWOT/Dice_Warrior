# DevelopmentIdentityResolver.cs

- 역할: 로컬 개발 신원 공급자 인터페이스와 헤더 기반 구현.
- 핵심 동작: 실제 연결 remote IP가 loopback이고 헤더가 하나이며 계정이 영문/숫자/_- 1~64자일 때만 userId 반환.
- 입출력: HttpContext→개발 계정 문자열 또는 null. 요청 body UID/forwarded IP를 신뢰하지 않는다.
- 사용하는 대상/쪽: Regex/IPAddress/HTTP headers, AuthorityEndpoints.
- 관계 근거: 이 반환값으로만 SqliteAuthorityRepository(path,userId)를 생성한다.
- 상태/수명: 불변 resolver, 요청별 값. 계정은 로그에 출력하지 않는다.
- 검수: 개발 PC의 다른 프로그램은 임의 계정을 선택할 수 있다. 운영 전 Firebase ID token/App Check 검증 구현과 교체해야 한다.
