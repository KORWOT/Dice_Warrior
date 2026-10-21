# ProceduralCampaignAuthoring.cs

- 원본: Assets/_Project/Features/Run/Editor/ProceduralCampaignAuthoring.cs
- 역할: 승인된 절차 지도·운명 popup 원본 작성과 신규 런의 지도 설정 전환을 순서대로 호출하는 Editor 전용 진입점이다.
- 입력/API: 메뉴 Fate Dice/절차 지도와 운명 선택 창 적용의 Apply():string. 별도 인수 없이 FateDiceConfig.DefaultAssetPath의 기존 설정 원본을 사용하고 작업 결과 문구를 반환한다.
- 사전 조건: Play/전환·컴파일·import 중, Main Stage 외부, 저장되지 않은 열린 씬, 기본 설정/world 누락은 예외로 중단한다. config.Snapshot을 복사해 후보를 만들고 Validate가 성공한 뒤 원본 작성을 시작한다.
- 핵심 동작: 후보가 모드0이면 mapGenerationVersion=1, mapColumns=5, mapPathCount=5, previewDepth=2를 지정한다. CampaignMapAuthoring.ApplyProcedural → FateChoiceAuthoring.Apply를 순서대로 실행하고, 실제 설정이 아직0일 때만 이 네 필드를 SetDirty/SaveAssetIfDirty로 저장한다. 이미1이면 기존 사용자 값과 원본 작성기의 버전별 재적용 정책을 유지한다.
- 설정 의미: 기본 eventsToBoss=10인 신규 런은 일반10층+단일보스의 전체 분기/합류 지도다. 일반층 수나 다른 전투·보상·주사위 규칙은 이 도구가 변경하지 않는다. PrototypeAuthoring의 CreateDefaults는 기존 모드0을 유지하며, 기존 저장은 저장된 생성 버전/규칙으로 재개한다.
- 직접 쓰기: 기본 FateDiceConfig의 위 네 필드만 저장한다. 의존 작성기가 수정하는 승인 원본은 ExplorationUI/ExplorationNodeView/FateCardView, 신규 FateChoiceUI, UIRoot의 popup 등록이다. 파일별 범위는 PROCEDURAL_CAMPAIGN_PLAN의 정확한 자산 목록을 따른다. Scene·사용자 저장·Packages·공급사·전역 설정은 쓰지 않는다.
- 관계 근거: Apply → FateDiceConfig.Snapshot/Validate → CampaignMapAuthoring.ApplyProcedural/FateChoiceAuthoring.Apply → AssetDatabase.SaveAssetIfDirty(config). 생성된 등록 원본은 RunUIController와 UIWorkbenchPreview가 사용한다.
- 상태/수명: static 일회성 Editor 작업이고 런타임 component/캐시/업데이트가 없다. 전체 과정을 트랜잭션으로 롤백하는 API는 아니므로 의존 작성기의 실패 문구·작성 결과를 확인한다. 광범위 SaveAssets나 숨은 런타임 대체 생성은 하지 않는다.
- 검수 주의: 재적용 시 설정과 사용자 편집값 보존, 지정 프리팹 GUID/직렬화 보존, 비대상 hash, 실제 작성 후 registry/Workbench 표시를 확인한다. 소스가 존재하는 것과 메뉴 실행·원본 검증 성공을 구분한다.
- 계약·실제 검증: Docs/Plans/PROCEDURAL_CAMPAIGN_PLAN.md, Docs/Reports/PROCEDURAL_CAMPAIGN_REPORT.md. 이 요약은 실행 PASS를 주장하지 않는다.
