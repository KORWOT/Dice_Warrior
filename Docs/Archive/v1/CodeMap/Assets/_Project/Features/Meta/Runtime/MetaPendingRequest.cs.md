# MetaPendingRequest.cs

- 역할: 발송할 메타 요청의 종류·프로필·정확한 입력/ID/revision/복구 세대를 직렬화하여 전체 fingerprint와 함께 보관한다.
- 동작/입출력: Create/Copy/Request가 독립 사본을 제공하며 Validate는 소유 프로필과 해시·기본 계약을 확인한다. DispatchAsync는 지정한 네 가지 서비스 명령만 호출한다.
- 관계: LocalMetaRequestJournal이 파일로 저장하고 MetaRequestCoordinator가 재시도한다. IMetaProgressionService.QueryRequestAsync가 기존 명령 영수증과 비교한다.
- 상태/수명: 요청 원문은 발송 시 고정하며 재시도에서 최신 프로필 revision으로 바꾸지 않는다. 공개 DTO를 전달할 때 별도 사본을 사용한다.
- 검수: 해시는 보안/권위 증명이 아니다. MetaRequestResolution은 같은 문서의 확정 결과/미확정/거절/복원 이전을 구분한다. 실행 증거는 RECOVERY_STABILITY_REPORT를 따른다.
