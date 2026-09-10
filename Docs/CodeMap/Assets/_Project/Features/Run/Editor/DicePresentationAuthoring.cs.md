# DicePresentationAuthoring.cs
- 역할: 정확한 주사위/행동/운명 카드 원본 3개에 조합 표시/한 줄/FEEL 선택 효과를 작성하는 Editor 전용 migration.
- 입력/출력: Fate Dice/주사위 조합 연출 원본 적용 메뉴와 Apply() 결과. 플레이·컴파일·미저장 씬·다른 stage 중에는 거절한다.
- 자산: DiceRollUI.prefab presentationVersion 1, ActionCardView/FateCardView SelectionFeedback 유무로 재적용을 건너뛴다. 기존 GUID 보존. 신규 DiceFeedbackCatalog.asset, ComboRibbon.mat, Pretendard-Effects SDF.asset/atlas/material subassets를 작성한다.
- 관계 근거: 실제 DiceRollUI/DiceResultFeedback/SelectionFeedback 필드를 Editor API로 연결한다. 기존 Pretendard-Regular.ttf와 기본 규칙 label을 글꼴 작성 시 읽는다. 게임 규칙/씬/패키지를 수정하지 않는다.
- 상태/수명: PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset/Unload finally. 새 자산만 CreateAsset, 기존 사용자 설정은 유지한다. FEEL scale/player는 unscaled .32초 기본, 실행 DTO로 시간 조정.
- 검수: 정확한 원본 연결, 한 행/화면 경계, 한글 TMP mesh/All In 1 재질, 재실행 0개 변경. 실행 결과는 DICE_PRESENTATION_EFFECTS_REPORT.

- DPE-R02 보완: SaveAssetIfDirty는 이 도구의 catalog/font/material만 대상으로 한다. 다른 dirty 자산을 전역 저장하지 않는다.
- ApplyAuras / Fate Dice/주사위 오라 연출 원본 적용: 기존 catalog와 DiceRollUI prefab만 대상으로 version2 레이아웃/별도 Crest/여섯 Halo를 작성한다. 기존 Apply와 분리해 카드·font·material을 저장하지 않는다. 편집된 대표색은 이전 기본색 비교로 보존하며, 적용된 버전의 프리팹은 다시 쓰지 않는다. 새 참조/그룹 효과는 DiceResultFeedback와 DiceAuraGraphic으로 연결한다.
- 최종 version4: v2/v3는 오라를 중복 생성하지 않고 FinishAuraAppearance에서 투명 패널·밝은 면·결과 요약 간격·dim을 적용 후 한 번 저장한다. RequireComponent로 메모리에 자동 생성된 CanvasRenderer도 실제 prefab에 직렬화한다. v4 이후에는 누락 renderer 수리 외 배치/색을 다시 쓰지 않는다. AuraDefaultColor는 RGBA 전체가 이전 기본일 때만 교체해 alpha만 수정한 색도 보존한다.


## 등급별 연출 수명 (2026-09-10)

ApplyTimelines 메뉴가 catalog 및 PLAN의 정확한 원본5개만 Editor API로 저장한다. tiers가 비어 있을 때 기본4단계를 작성하고 기존 색/레이아웃/FEEL 트랙을 보존한다. timelineVersion1 이후 반복은 무변경이다. dirty scene/Play/Preview/컴파일 중은 거절한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
