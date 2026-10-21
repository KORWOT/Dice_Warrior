# TierPreviewCapture.cs

- 역할: 실제 DiceRollUI/Workbench 원본을 PreviewScene에서 두 세로 비율·네 연출 단계로 렌더링하는 시각 fixture.
- 입력/출력: Capture(sample,height), sample0 투 페어(Simple),1 트리플 페어(Runic),2 포 카드(Grand),3 식스 카드(Legendary); CaptureAll()은 네 조합 × 두 비율을 직렬 생성한다. 720×1280/1600 PNG와 표시 값/메시 txt를 artifacts/presentation-lifecycle 아래 생성한다.
- 관계: UIWorkbenchPreview.OpenDice/PlayWorkbenchSession.Build가 확정 표시용 설정을 제공하고 UIManager.ShowPopup을 사용한다. 일반 게임 RNG/명령/저장소를 사용하지 않는다.
- 상태/수명: 임시 카메라/RenderTexture/Texture2D/PreviewStage를 finally에서 정리하고 이전 RenderTexture를 복원한다. Scene/Prefab/설정 원본을 쓰지 않는다.
- 검수: static phase1 이미지이며 실제 시간 모션 증거는 PresentationFlowTests의 단계·실제 FEEL 완료 검사와 분리한다. 최종 PNG를 육안 확인한다.
