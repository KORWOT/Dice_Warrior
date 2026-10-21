# PlayerSaveDocument.cs

- 역할: 프로필·런·시작 기록·정산을 공동 확정하는 로컬 저장 경계.
- 핵심 동작·입출력: PlayerSaveDocument는 논리적으로 분리된 데이터를 파일 하나에 묶고 명시적으로 복제/검증한다. LocalPlayerDataStore는 프로필별 해시 경로, 필드 전용 JSON, 체크섬, expected document revision, OS 파일 잠금, flush 후 원자 교체를 사용한다. MetaJson은 CLR 타입 선택을 허용하지 않는다.
- 사용하는 대상·사용하는 쪽·관계 근거: IPlayerDataStore를 LocalMetaProgressionService가 사용한다. IRunStore 어댑터는 서비스 내부이며 저장 구현에는 보상 결정 책임이 없다.
- 상태·수명·검수 주의: 체크섬은 손상 감지이며 위변조 방지가 아니다. 원본 손상/지원하지 않는 버전/다른 플레이어는 거부하며 자동 삭제하지 않는다. 프로필 revision과 문서 revision을 구분한다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.

## RECOVERY_STABILITY RV-01 직접 검증 통과

파일 envelope/문서/프로필 schema와 타 프로필을 MetaFailure typed cause로 구분한다. 기존 InvalidDataException 타입과 일반 I/O 예외는 유지한다. 문서 revision 불일치에 Stale 코드를 지정한다. 자동 초기화와 읽기 시 파일 쓰기는 없다. 검증 상태: RECOVERY_STABILITY_REPORT 참조.

## RV-02 직접 검증 통과

프로필과 런의 복구 세대 일치를 검증한다. ReadBytes는 실제 bytes 하나를 파싱/검증한다. Write는 현재 bytes 검증·revision/세대 CAS 후 이전 정상 bytes를 .backup에 원자 보관하고 새 후보를 확정한다. 공통 파일 작업은 LocalPlayerDataRecovery partial에 있다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.


## RV-03 구현 진행

IPlayerDataStore가 명시적 RequestJournal을 제공한다. 로컬 구현은 동일 프로필 경로의 .pending sidecar를 제공하며 본문/백업은 기존 계약을 유지한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.

## RV-04 관측

ReadPrimaryBytes 및 decode/Validate 범위는 같은 저장소의 Editor 한정 LocalSaveDiagnostics에 호출/시간을 기록한다. Write의 잠금 안에서도 실제 primary read를 별도 집계하며 CAS/세대/후보 검증 순서는 유지한다. 수집하지 않는 제품 빌드의 저장 계약은 동일하다.

## RV-04 측정 후 최소 개선

MetaJson은 Fields resolver의 타입 계약 캐시만 재사용한다. serializer settings와 데이터는 호출별이며 저장 필드·JSON bytes·TypeNameHandling.None 계약을 유지한다.
