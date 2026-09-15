# RunUIController.MetaRequests.cs

- 역할: 메타 요청의 대기/결과 확인/동일 재시도/안전한 닫기와 현재 UI의 재조정을 담당한다.
- 입력/출력: 로비·결과의 요청 의도 → MetaRequestCoordinator → 현재 바인딩의 MenuUI 또는 저장된 출발/준비 화면.
- 동작: 요청 수명은 조정 객체에 보관하고 UI 완료·오류·정리는 controller 세대/활성 화면/BaseUI.BindingRevision이 모두 일치할 때만 한다. 관련 변경 명령은 차단하면서 닫기/조회 동선은 입력 잠금에서 분리한다.
- 관계: RunUIController.Meta/Recovery와 기존 MenuUIData.recoveryMode, IMetaProgressionService의 같은 시점 결과, RunSession/SceneFlow를 사용한다. 새 prefab이 없다.
- 수명/검수: 화면 닫기나 disable은 요청 취소가 아니다. 결과 후보를 검증하고 동일 journal 정리에 성공한 뒤 UI에 반영한다. 미지원/손상 journal을 삭제하거나 새 요청으로 대체하지 않는다. 실제 검증은 RECOVERY_STABILITY_REPORT 참조.

- RS-IR-01: 대기 중 창을 다시 열면 조정 객체의 ActiveAttempt에 새 바인딩의 ObserveMetaRequest를 연결한다. 예전 관찰자는 기존 소유권 검사로 중단되고 현재 화면만 결과/오류/timeout을 갱신한다. 이미 완료된 요청을 다시 열었을 때의 명시 결과 확인 경로는 유지한다.
