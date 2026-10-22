# LoopJson.cs

- SharedWorld 직접 관계: 신규 DTO의 복제/체크섬/로그 fingerprint와 Unity/.NET 고정 골든에도 동일 직렬화기를 사용한다. 기존 JSON 설정과 v1 fingerprint는 변경하지 않는다.

- 역할: Unity/독립 서버 공통 Newtonsoft JSON 직렬화·깊은 복사·UTF-8 SHA256.
- 핵심 동작: 기존 LoopStore 내부 클래스를 Domain으로 이동했으며 필드 순서/enum 정수/null/MaxDepth64/알 수 없는 필드 거절/TypeNameHandling.None 의미를 유지한다.
- 입출력: DTO↔JSON, 문자열→대문자 SHA256. mutable DTO는 Copy로 분리한다.
- 사용하는 대상: Newtonsoft.Json, System.Security.Cryptography. Domain Core가 자동 참조되는 기존 Newtonsoft DLL을 사용하며 서버 Shared는13.0.3을 고정한다.
- 사용하는 쪽: Session/Store/Coordinator/HttpBackend, 서버 서비스/검증기/SQLite/호스트 및 검사.
- 관계 근거: 서버 Shared.csproj가 같은 Domain 소스를 링크 컴파일한다. 권한 fingerprint는 kind+":"+Encode(request)이며 체크섬/복사도 이 구현을 공유한다.
- 상태/수명: static 불변 설정, 호출별 SHA/DTO 사본. 사용자 데이터 저장 책임은 없다.
- 검수: 기존 프로토콜 직렬화 변경 금지. 서버/Unity 골든125상태와 문화권·필드 순서 재전송을 비교한다.
