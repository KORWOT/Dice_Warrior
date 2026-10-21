# FlowPreviewCapture.cs

- 역할: 정지 Unity에서 즉시 이동 원본이 보이는 지도 미리보기를720×1280/1600으로 렌더하는 검증 도우미. 기존 CampaignPreviewCapture의 격리 Stage 캡처를 재사용한다.
- 입력/출력: run_script의 FlowPreviewCapture.CaptureAll. artifacts/campaign-flow-polish/map-current-{height}.png/txt 두 쌍을 만든다. 게임 App/사용자 저장을 부팅하거나 변경하지 않는다.
- 사용하는 대상: 실제 UIWorkbenchPreview·GameApplication prefab 참조·Camera/RenderTexture/Texture2D. 생성 카메라/Stage/픽셀을 finally에서 정리하고 원래 Stage/선택/SceneView/RenderTexture를 복원한다.
- 사용하는 쪽: Main CLI run_script 전용. Assets 밖 파일이므로 프로젝트 런타임/빌드에 포함하지 않는다.
- 검수 주의: 정지 렌더는 실제 입력·복귀 초점·연출 통과를 대신하지 않는다. 해당 동작은 CampaignFlowPolishTests에서 별도로 실행한다.
