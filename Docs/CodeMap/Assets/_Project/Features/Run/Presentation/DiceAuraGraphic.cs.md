# DiceAuraGraphic.cs

- 역할: DiceRollUI의 주사위 Halo/중앙 Crest를 그리는 MaskableGraphic. 외부 텍스처/ParticleSystem 없이 등급마다 다른 링·문양·궤도·파동·광선을 UI 메시로 만든다.
- 입출력: 작성 필드 shape/variation, SetVisual(primary,secondary,strength,phase,complexity,opacity), Clear. 기존 4인자 오버로드는 Grand/opacity1을 전달하여 기존 코드와 reflection 검증을 보존한다. Visible/Primary/Secondary/Strength/Phase/Complexity/Opacity는 현재 표시만 읽는다. strength/phase/opacity는 0..1로 제한된다.
- 핵심 동작: Simple Halo는 링 1개+작은 빛점 2개, Simple Crest는 0정점이다. Runic은 Halo 두 원호+4표식과 최소 Crest 원/8표식을 순차 점등한다. Grand는 조각 문양·교차 궤도·반짝임을 회전/펄스한다. Legendary는 Grand에 확산 링 3개·추가 방사선과 Crest 왕관형 5꼭짓점을 더한다. 동일 strength에서도 complexity에 따라 구성/정점 수/운동이 다르다.
- 모션/색: phase 0..1을 전체 모션 진행으로 받아 회전·다중 펄스·확산을 계산하고 phase1에서 정착한다. 실제 시간·등급 판정·전체 연출 종료는 여기서 결정하지 않는다. 모든 vertex alpha는 원본 색 alpha × Graphic.color.a × 외부 opacity를 유지하며 white core도 alpha를 올리지 않는다. 기본 UI 재질/마스크/Canvas 수명을 그대로 사용한다.
- 사용하는 대상/사용하는 쪽: DiceFeedbackCatalog.cs의 DiceEffectComplexity enum을 사용한다. DiceResultFeedback가 확정 hand 참여 그룹·catalog 색·등급 모션 진행/퇴장 opacity를 전달한다. DicePresentationAuthoring.ApplyAuras가 여섯 Halo를 row의 면 뒤 sibling, Crest를 banner와 별도 sibling으로 작성한다. 관계 근거는 해당 필드 및 SetVisual 호출이다. DiceAuraTests/PresentationFlowTests가 실제 메시·정착·초기화·등급 차이를 검사한다.
- 수명: 자체 Update/난수/Coroutine/재질 인스턴스 없음. 외부 SetVisual 시 SetVerticesDirty, OnPopulateMesh는 전달 helper를 Clear 후 사용한다. phase1 이후 자체 재구성을 반복하지 않는다. Clear는 다음 재구성에서 geometry를 비운다. Simple Crest는 Visible 요청이 true여도 그릴 geometry가 없다.
- 범위/검수: 길이는 rect 반경에 비례하고 기본 authored 크기에서 외곽 여유를 둔다. 비정상적으로 작거나 좁은 편집 rect도 최종 vertex를 rect 안으로 제한한다. 투명 중심/눈금 가독성, phase0/중간/1, strength/opacity 양끝, 동일 값 재전달 시 정착, 두 세로 비율을 확인한다. 종료/watchdog는 별도 PresentationPlayback/소유자 책임이다. 원래 오라 검증은 DICE_COMBO_AURA_REPORT, 신규 계약 검증은 PRESENTATION_LIFECYCLE_REPORT에 메인이 기록한다.
