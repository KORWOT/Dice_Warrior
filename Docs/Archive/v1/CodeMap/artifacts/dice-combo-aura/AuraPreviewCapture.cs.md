# AuraPreviewCapture.cs

- 역할: run_script용 정지 UI 시각 fixture. 실제 DiceRollUI prefab/Canvas를 PreviewScene에서 렌더링한다.
- 입력: Capture(sample,height), sample0 투 페어/1 트리플 페어/2 식스카인드의 명시 fixture. height1280/1600. 실제 GameApplication의 규칙 label/priority/fatePower를 읽는다.
- 출력: artifacts/dice-combo-aura/<Hand>-<height>.png와 txt(값/실제 요약/오라별 표시·정점 수). 저장/플레이 결과로 가장하지 않는다.
- 관계: UIWorkbenchPreview/PlayWorkbenchSession의 폐기 가능한 복사본, UIManager.ShowPopup, Camera/RenderTexture/Texture2D. RunSession 명령/실제 저장소는 호출하지 않는다.
- 수명: 현재 stage의 Canvas 정상 활성화 뒤 재구성하며, 임시 카메라는 preview scene culling mask만 렌더링한다. finally에서 RenderTexture.active 복원·카메라/텍스처/preview 정리. 프로젝트 자산 저장 없음.
- 검수: 지정 씬/제작 자산/기본 저장 불변과 실제 캡처를 확인한다. 이미지 비교는 육안 검사이며 런타임 검사는 별도 DiceAuraTests/presentation 회귀가 담당한다.
