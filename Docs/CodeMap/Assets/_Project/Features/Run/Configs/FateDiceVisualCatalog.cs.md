# FateDiceVisualCatalog.cs

- 원본: `Assets/_Project/Features/Run/Configs/FateDiceVisualCatalog.cs`
- 기능/어셈블리: Run Configs / FateDice.Runtime
- Task: FATE_DICE_PROTOTYPE 추가 UI 계약 f91fba13 / 기존 B
- SHA256: `707EAD26BF59E6A76429D41AE13E39645B608492C0A2C231FB9D5528CB702114`

표시 전용 SO와 작은 직렬화 매핑 DTO다. 게임의 안정 ID·공개 유형·제시 등급으로 Sprite 직접 참조와 fallback을 조회한다. 게임 효과·확률·저장 ID를 결정하지 않는다.

## 데이터와 공개 API
- VisualArtwork: 선택적인 icon/artwork, tint, fallbackGlyph. 그림을 등록하지 않아도 tint와 글자로 표시한다.
- actions/events: 원본 콘텐츠 stable ID별 선택적 외형. defaultAction/defaultEvent는 유효 콘텐츠에 매핑 행 또는 visual이 없을 때만 사용한다.
- nodes/fates: NodeType 6종의 필수 공개 외형 행. fates는 선택 전 공개 정보용이다.
- grades: Grade 5종의 border/badge. 색상은 기존 GameConfigData.presentation.gradeColors를 재사용하며 이 SO에 중복 보관하지 않는다.
- buttons: ButtonPurpose 7종의 공통 스타일 및 선택적 icon. nodeFadeSeconds는 유한·비음수의 초 단위 표현값이다.
- Validate(GameConfigData): 콘텐츠 원본/매핑 ID·중복·null행·필수 enum 키·fallback·유한 색상/페이드 시간을 검사한다. 실패는 FateDiceVisualCatalog.<필드 경로>를 담은 InvalidOperationException이다.
- ResolveAction/ResolveEvent: 콘텐츠 존재 여부를 먼저 확인한다. 잘못된 ID를 기본 그림으로 숨기지 않고, ID가 유효할 때만 선택적 기본 외형을 허용한다.
- ResolveNode/ResolveFate: NodeType만 받아 해당 공개 외형을 반환한다. 특히 ResolveFate는 GameConfigData/event ID/events/defaultEvent를 읽지 않으므로 비공개 사건 정체를 조회 경로로 넘길 수 없다.
- ResolveGrade/ResolveButton: 명시한 enum의 필수 행을 반환한다. 배열 순서나 이름/파일명은 키가 아니다.

## 실제 직접 관계
- 설정 입력: `Fate/Configs/FateDiceConfig.cs`의 GameConfigData.combat.actions/world.events 및 NodeType/Grade.
- 공통 UI 의존: `Assets/_Project/Shared/UI/Presentation/CommonButtonView.cs`의 ButtonPurpose/ButtonAppearance.
- DTO를 받는 View: `Exploration/Presentation/ExplorationNodeView.cs`, `Combat/Presentation/ActionCardView.cs`, `Fate/Presentation/FateCardView.cs`의 Bind 인자가 VisualArtwork와 필요 시 GradeVisualEntry를 받는다.
- 확인된 조회/검증 호출자: `Run/Tests/EditMode/UiVisualCatalogTests.cs`. `Run/Tests/PlayMode/FateDiceGuiTests.cs`도 화면의 Catalog 참조를 검사한다.
- 현재 소스 확인 시 실제 Screen/Widgets의 Catalog 조회 및 .asset/Prefab 연결은 메인 통합 중이다. 예정된 자산 연결을 완료된 것으로 기록하지 않는다.

## 상태·수명·검수
원본 그림/스타일 참조를 읽기 용도로 반환하고 캐시하지 않는다. 다음 화면 재생성 때 Inspector 변경을 다시 조회할 수 있다. Catalog는 RNG·런 상태·씬 검색·Resources 경로·저장 스키마를 사용하지 않는다. 매핑은 표시 계층에서만 게임 데이터를 읽으며 규칙으로 Sprite 의존성이 역전되지 않는다.
선택적 Sprite null과 잘못된 콘텐츠 연결을 구분한다. 같은 Sprite를 여러 콘텐츠/아이콘/배지에 연결할 수 있다. ResolveEvent를 호출할 시점의 공개 정책과 실제 제시카드 ID의 클릭 전달은 Screen/View 책임이다.
초기 RED: Codex workspace artifacts/fate-dice-ui/initial-edit-red.json, 전체122 중 이 파일의 계약 테스트19/19가 의도한 NotImplemented로 실패했다. 구현 후 GREEN은 메인 실제 실행에서 PASS 확인이며 Prefab·실제 이미지 교체 완료를 이 문서로 주장하지 않는다.

- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.

- UI 구조 변경 관계(2026-09-08): FateDiceScreen은 RunUIController 파생 Scene 진입점이다. 설정/표시 평가/저장 명령 호출은 RunUIController가 담당하고, typed 화면 View와 UIManager는 게임 규칙·저장을 직접 호출하지 않는다. 기존 규칙과 저장 C# bytes는 변경하지 않았다. 현재 관련 회귀는 UI_STRUCTURE_REPORT의 Edit122/Play37 실행 결과를 따른다.


## 전투 피드백 직접 관계

- 카드·전투 결과 강조는 기존 gradeColors를 사용한다. 결과 유지와 전투 영역 반응 시간은 DiceRollUI/CombatUI 원본에서 편집하며 이 catalog·게임 설정·저장 schema는 변경하지 않는다.
- 실행 증거: Docs/Reports/COMBAT_FEEDBACK_REPORT.md.
