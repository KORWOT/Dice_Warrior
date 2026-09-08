# UiVisualCatalogTests.cs

- 원본: `Assets/_Project/Features/Run/Tests/EditMode/UiVisualCatalogTests.cs`
- 기능/어셈블리: Run Tests / FateDice.EditMode.Tests
- Task: FATE_DICE_PROTOTYPE 추가 UI 계약 f91fba13 / 기존 B
- SHA256: `C753D639FB118FD26840CF3EDB63106F6B6739FDF75BAE76EB6C574BD0668A38`

NUnit 19개 사례로 표시 매핑의 ID·유효성·fallback·공개 정보 분리를 검증한다. 규칙 테스트나 실제 게임 화면 검증을 대체하지 않는다.

## 흐름·입력·기대값
SetUp은 PrototypeAuthoring.CreateDefaults와 임시 FateDiceVisualCatalog를 만들고 6 node/fate, 5 grade, 7 button 기본행을 준비한다. TearDown은 테스트 SO를 해제한다.
- 이미지와 콘텐츠 매핑 행이 없어도 기본 glyph와 tint로 조회 가능하고 원본 규칙 설정이 동일해야 한다.
- 유효하지 않은 콘텐츠 ID는 Resolve부터 오류가 나야 하며, 중복/불법 매핑은 actions/events[index].contentId 경로를 표시해야 한다.
- node/fate/grade/button의 null·중복·불법 enum·누락 행을 거부한다. 선택적 콘텐츠의 null행도 거부하지만 visual 자체 누락은 기본 외형을 사용한다.
- 같은 표시명과 배열 순서 변경에도 stable ID별 그림이 맞아야 한다.
- 임시 Sprite 하나를 여러 원본/그림/아이콘/테두리/버튼에 재사용해도 허용하고, 조회 전후 규칙·RNG·진척·sequence를 포함한 런 상태 JSON이 동일해야 한다.
- ResolveFate는 일부러 잘못된 비공개 사건 매핑과 SECRET 기본 이벤트 그림을 넣어도 공개 NodeType 그림만 반환해야 한다.
- nodeFadeSeconds는 유한·비음수이고 잘못된 enum 조회는 명확한 오류여야 한다.

## 직접 관계·수명
직접 호출은 `Run/Configs/FateDiceVisualCatalog.cs`의 Validate 및 6개 Resolve API, `Run/Editor/PrototypeAuthoring.cs`의 CreateDefaults, JsonUtility/NUnit이다. ButtonPurpose/ButtonAppearance는 Shared UI 계약이다.
공유 이미지 테스트는 Texture2D/Sprite를 try/finally로 DestroyImmediate한다. 실제 프로젝트 .asset·Prefab·사용자 저장 파일은 쓰지 않는다. 모델 입력과 임시 SO/이미지를 테스트가 소유하며 시간/전역 RNG에 의존하지 않는다.

## 검증
초기 RED 파일: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-ui/initial-edit-red.json. 전체122/99PASS/23FAIL 중 이 클래스19건 모두 의도한 NotImplemented 예외로 실패했음을 결과 목록에서 확인했다.
생산 Catalog 구현 후 테스트 내용/기대 기준은 변경하지 않았다. 현재 GREEN은 메인 실제 실행에서 PASS 확인다. 실제 Prefab 사용·이미지 교체·재Bind·fade·GUI 정보 누출은 해당 PlayMode/Editor 수용 검증에서 별도로 확인해야 한다.

- 메인 최종 실행: EditMode122/122(매핑19 포함), PlayMode21/21(View7+실제 GUI14 포함) PASS. 실제 원본/이미지 편집·복원과 합류/저장 결과는 REPORT 추가 UI 절 참조.
