# LoopData.cs

- SharedWorld 직접 관계: 신규 모드는 Rules/Profile/DieSlot/Enemy/TurnLog/Receipt 자료형을 재사용하지만 기존 Run/PlayerDocument/BackendRequest 저장·정산 계약을 바꾸지 않는다. CompetitionState/ParticipantState/CompetitionDocument는 별도 형식이다.

- 역할: 기호·타일·단계·규칙·런·표시 프로필·로컬 영수증과 명령 DTO.
- 핵심 동작: schema=2 체크포인트에 authority 캐시, 승인 ticket, 확정 명령 commands, 제출 submission을 분리한다. profile은 UI/출전 선택용 캐시다.
- 입출력: Rules/Run/Profile/PlayerDocument/Command. Command.backend에 같은 요청 재시도용 전체 BackendRequest를 보존한다.
- 사용하는 대상: System/List, BackendContracts. 사용하는 쪽: 도메인/Session/Store/Coordinator/화면/검사.
- 관계 근거: PlayerDocument와 Command의 직접 필드 참조. Core assembly는 Unity/IO에 의존하지 않는다.
- 상태/수명: 후보·Snapshot·응답 복제로 소유권 분리. 런 중 값과 권한 프로필 캐시는 별도이며 캐시는 서버의 진실 원천이 아니다.
- 검수: 기존 schema1 파일 경로는 앱이 열지 않는다. commands는 저장 성공한 실행만 포함하며 피해/주사위 결과는 권한 입력이 아니다.
