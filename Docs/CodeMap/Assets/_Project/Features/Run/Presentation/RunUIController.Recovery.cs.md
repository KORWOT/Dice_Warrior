# RunUIController.Recovery.cs

- 역할: 저장 장애의 명시 복구 화면과 후보 읽기→세션 교체를 조정한다.
- 입력/출력: MetaFailureKind, 복구 버튼 → IMetaProgressionService.ReadRecoverySnapshot → 검증된 RunSession 또는 로비.
- 핵심 동작: 동일 문서의 런/정산 확인 후에만 교체한다. 재로드 전에 자동 저장하지 않으며 읽기 실패 시 기존 Session/파일을 보존한다. 이미 정산된 런은 로비, 새/기존 미정산 런은 저장 phase로 복귀한다.
- 관계: Controller의 PlayerError/Render/Choice와 MenuUIData/MenuUI를 사용한다. SceneFlow에는 RequestLobby/RequestInGame으로 기존 계약대로 연결한다.
- 수명: 복구 진입/완료 및 disable 시 입력 세대를 무효화한다. 준비 중에는 기존 세션을 유지한다. 새 세션 적용 전에 기존 연출/뷰를 정리한다.
- 검수: 복구 버튼은 게임 명령 차단과 별개로 사용 가능하다. 백업 복원은 RV-02에 포함되며 미결 요청 수명은 RV-03에서 아직 구현할 범위다. 현재 상태는 REPORT를 따른다.

## RV-02 직접 검증 통과

백업 확인과 최종 확인/취소를 분리한다. 보관 시각/해결 사건/진행 손실을 표시하며 확인 때만 RestoreBackup을 호출한다. 취소/새 확인에서 입력 세대를 바꾸고 복원 성공 후 최신 상태를 명시 재로드한다. 백업 없음/검증 실패는 재읽기 동선을 유지한다. 실제 검증 상태는 RECOVERY_STABILITY_REPORT 참조.
