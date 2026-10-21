# MockServerSettings.cs

- 역할: 루프백 URL/서버 DB/고정 콘텐츠 및 명시적 시험 옵션 검증.
- 핵심 동작: 숫자 HTTP loopback 주소 하나만 허용, 고정 rules JSON을 공유 LoopJson으로 정규화해 SHA256 확인, rules.Validate, optional 양수 seed/fault 설정.
- 입출력: IConfiguration와 배포 Content JSON→검증된 설정. DB 기본은 서버 출력 폴더 data이며 실행 스크립트는 Server/data를 지정한다.
- 사용하는 대상/쪽: LoopJson/LoopRules/IPAddress/File, Program과 AuthorityEndpoints.
- 관계 근거: 호스트 콘텐츠는 골든/Unity Config와 동일성을 별도 검사하며 서버가 클라이언트의 Rules 입력을 받지 않는다.
- 상태/수명: 시작 시 한 번, 서비스 생성 시 Rules가 깊은 복사된다.
- 검수: 콘텐츠 변경은 새 버전·기대값 갱신 필요. loopback/고정 seed는 실제 기기·운영 설정이 아니다.
