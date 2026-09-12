# PlayerSaveDocument.cs

- 역할: 프로필·런·시작 기록·정산을 공동 확정하는 로컬 저장 경계.
- 핵심 동작·입출력: PlayerSaveDocument는 논리적으로 분리된 데이터를 파일 하나에 묶고 명시적으로 복제/검증한다. LocalPlayerDataStore는 프로필별 해시 경로, 필드 전용 JSON, 체크섬, expected document revision, OS 파일 잠금, flush 후 원자 교체를 사용한다. MetaJson은 CLR 타입 선택을 허용하지 않는다.
- 사용하는 대상·사용하는 쪽·관계 근거: IPlayerDataStore를 LocalMetaProgressionService가 사용한다. IRunStore 어댑터는 서비스 내부이며 저장 구현에는 보상 결정 책임이 없다.
- 상태·수명·검수 주의: 체크섬은 손상 감지이며 위변조 방지가 아니다. 원본 손상/지원하지 않는 버전/다른 플레이어는 거부하며 자동 삭제하지 않는다. 프로필 revision과 문서 revision을 구분한다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
