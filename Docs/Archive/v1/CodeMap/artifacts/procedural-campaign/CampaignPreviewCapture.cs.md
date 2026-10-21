# CampaignPreviewCapture.cs

- 역할: Unity Pipeline run_script로 메모리 컴파일하는 검증용 캡처 도구다. Assets 밖에 있어 빌드나 런타임 어셈블리에 포함되지 않는다.
- 입력/출력: CaptureAll()은 720×1280/1600에서 지도 시작 위치, 상단 보스, 운명 카드 강조 화면을 각각 PNG/TXT로 artifacts/procedural-campaign에 기록한다. Map(height,boss), Popup(height,selectedIndex)도 개별 호출할 수 있다.
- 동작: 실제 GameApplication 원본을 읽어 UIWorkbenchPreview.Open의 폐기 가능한 Stage를 만들고, 실제 화면의 ScrollRect/카드 장식만 갱신한다. 카메라로 Canvas를 RenderTexture에 그려 저장한다.
- 관계: UIWorkbenchPreview, PlayWorkbenchSession의 고정 seed 상태, ExplorationUI/CampaignMapView/FateChoiceUI, Editor Stage/SceneView와 Camera/RenderTexture. 실제 GameApplication Bootstrap이나 사용자 Store는 호출하지 않는다.
- 수명: MainStage·정지·컴파일 완료를 요구한다. finally에서 임시 카메라/texture/Stage를 닫고 선택·SceneView·렌더 타깃을 복원한다. 원본 씬/프리팹/설정 저장 없음.
- 주의: 강조는 SetSelected만 호출하는 시각 fixture이므로 실제 confirm/선택 의미는 PlayMode 포인터 검사로 별도 확인한다. eval 안에 클래스 선언을 넣는 방식 대신 run_script --file ... --entry CampaignPreviewCapture.CaptureAll을 사용한다. 실제 결과는 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
