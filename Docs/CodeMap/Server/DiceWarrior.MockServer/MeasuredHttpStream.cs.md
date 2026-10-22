# MeasuredHttpStream.cs
- 역할: ASP.NET 요청/응답 Stream의 실제 읽기·쓰기 엔터티 바이트 측정.
- 핵심 동작: sync/async read/write 성공 후 카운터를 증가시키고 나머지 Stream 동작을 원본에 위임한다.
- 입출력: 원본 Stream → ReadBytes/WrittenBytes. 바이트 내용은 저장하지 않는다.
- 사용하는 대상: System.IO.Stream/Memory/Task.
- 사용하는 쪽/근거: AuthorityEndpoints.Handle이 body를 일시 감싸고 finally에 원본으로 복원한다.
- 상태/수명: 요청별 wrapper. 원본 소유권은 ASP.NET에 남는다.
- 검수: 헤더·압축·TLS·클라이언트 수신 확인이 아니다. 실패한 write의 부분 전송은 확정 측정하지 못한다.
