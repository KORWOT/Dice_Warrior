# RecoveryStabilityAsyncTests.cs

- 역할: 실제 완료를 보류하는 메타 서비스와 authored UI를 사용한 요청 수명 회귀.
- 입력/출력: TaskCompletionSource로 아직 확정되지 않은 출발을 유지하고 명시 완료 시 실제 LocalMetaProgressionService로 확정한다. Raycast/Pointer 버튼, Session/뷰/입력 잠금/저장 assertion을 사용한다.
- 관계: IMetaProgressionService 시험 어댑터 → 실제 로컬 서비스/격리 저장, GameApplication/RunUIController/기존 MenuUI.
- 수명: 현재 fixture의 보류 Task만 취소하고 앱/시험 SO 및 검증된 TEMP 경로를 정리한다. 실제 사용자 저장 존재와 bytes를 전후 비교한다.
- 검수: category RecoveryAsync. 실제 실행 수와 완료/검증 판정은 RECOVERY_STABILITY_REPORT에 기록한다.


## RV-03 구현 진행

TCS 호출 목록에서 로컬 권위의 실제 확정과 응답 성공/실패를 따로 제어한다. 지연/유실/만료/동일 재시도/컨트롤러 재개/같은 UI의 외부 재바인딩/앱 재생성/성장 중복/저널 쓰기·정리 실패를 격리 실제 버튼으로 검사한다. 완료 수를 기록하기 전 PASS로 간주하지 않는다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT를 따른다.

정산 재시도의 재화·영수증1회, 복원 전 미결 요청의 세대 거부/명시 조회, 새 성장 요청 대기 중 옛 출발 응답, 확정 전 재시작 후 Title→Lobby→동일 요청 재시도를 포함한다. 씬 이동 후 버튼 검사는 canvas Layout 완료를 기다린 후 raycast한다.

## RV-04 측정 후 최소 개선

ControlledMetaService는 ReadDisplaySnapshot도 실제 로컬 서비스에 위임해 단일 표시 스냅샷 계약의 직접 영향을 반영한다.

## RS-IR-01 보완1

RecoveryReopen category의3개 검사는 완료 전 창 닫기/재개 후 성공·실패·timeout을 통제한다. 새 발송 없음, 미결 원문 bytes 유지, 동일 재시도1회 출발, timeout 뒤 늦은 실패의 바인딩 무변경을 실제 버튼으로 검사한다. 원래 완료 후 재개/외부 재바인딩 검사도 유지하며 실행 상태는 REPORT를 따른다.
