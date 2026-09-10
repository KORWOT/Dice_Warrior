# 주사위 조합 오라 연출 결과

상태: IMPLEMENT 완료 / 2026-09-10. [PLAN](../Plans/DICE_COMBO_AURA_PLAN.md)의 필수 AC 충족. 보완 2/2 사용, 남은 필수 Finding 없음.

## 결과와 편집 위치
- 6개 주사위의 한 줄 배치를 유지하고, 실제 조합에 기여한 주사위에만 색상 링과 반짝임을 표시한다. 투 페어는 4개에 파랑, 트리플 페어는 금색과 보라, 식스 카드는 6개 모두 적색과 금색이다. 다른 조합도 설정된 고유 색을 사용한다.
- 조합 이름 위아래의 중앙 문양과 빛을 조합 강도에 맞춰 표시한다. 기존 등장 구간 동안 확장·빛 이동 후 정착하며, 기존 결과 유지 시간이 끝날 때까지 읽을 수 있다. 눈금 위를 가리지 않도록 링은 주사위 뒤에, 문양은 글자 뒤에 놓았다.
- 설정: `Assets/_Project/Features/Run/Configs/DiceFeedbackCatalog.asset`의 color/accentColor/alternateGroups와 기존 punchScale/textEffect. 사용자 색상 편집을 보존하기 위해 기본색 migration은 RGBA 전체가 이전 기본값과 같은 항목에만 적용한다.
- 배치: `Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab`의 `조합 빛 문양`, `주사위 N 오라`, `조합 연출`. `Fate Dice > 플레이 작업실`에서 6주사위 창 미리보기와 원본 편집을 사용할 수 있다.
- `Fate Dice > 주사위 오라 연출 원본 적용`은 적용 완료 상태다. presentationVersion4/auraVersion1이며 재실행 시 프리팹·설정 자산 hash 불변을 확인했다. 전체 자산 저장을 사용하지 않는다.
- 참고 그림의 장식 방향을 UI 메시로 구현했다. 신규 그림·텍스처·셰이더·패키지는 추가하지 않았으며 기존 FEEL/Text Animator/All In 1 배너 연결을 유지했다.

## 범위와 규칙 보존
- 변경 C#: DiceResultFeedback, DiceFeedbackCatalog, DicePresentationAuthoring. 신규 C#: DiceAuraGraphic, DiceComboHighlights, DiceAuraTests. 위 파일은 모두 `Assets/_Project/Features/Run/` 아래에 있다.
- 신규 Graphic은 CanvasRenderer를 요구하며 링·광선·별의 메시만 생성한다. 자체 Update나 게임 RNG 사용이 없다. 기존 feedback 수명에서 진입/정착/닫기와 재바인딩 시 정리한다. 알파와 Rect 범위를 실제 메시로 검사했다.
- 참여 표시 helper는 확정된 hand와 6개 값을 받아 최소 참여 index를 결정한다. 입력 배열·값·순서·최종 hand·RNG를 변경하지 않는다.
- 조합 단계는 프로젝트 실제 규칙 값을 표시한다. 그림의 임의 등급 숫자나 확인/보상 버튼을 게임 기능으로 추가하지 않았다. roll/hold 타이밍, 0초 계약, 저장 및 결과 확정 흐름을 유지했다.
- 새 작업의 자산 쓰기는 위 DiceRollUI.prefab과 DiceFeedbackCatalog.asset에 한정했고 live Editor API를 사용했다. 기존 미커밋 작업 및 사용자 Plugins/Samples/Packages/Title 변경은 보존했다.

## 실제 검증
Unity 6000.6.0f1 / Pipeline 0.6.0-exp.1의 연결된 에디터에서 직렬 실행했다. 최종 컴파일 오류 0. 아래 결과 합계 **62/62 PASS**, 실패·건너뜀·미결정 0이다. 근거 파일은 `artifacts/dice-combo-aura/`에 있다.

| 결과 파일 | 실행/통과 | 확인 내용 |
|---|---:|---|
| aura-tests-final.json | 32/32 | 기여 주사위 기대값, 잘못된 입력, 입력/RNG 불변, 실제 프리팹 연결·메시·알파·영역·정리, 사용자 색 보존 |
| presentation-tests.json | 9/9 | 실제 UI 입력 경로, 중복 입력, 저장 1회, 타이밍·닫기·재바인딩·0초·긴 결과 유지 |
| workbench-tests.json | 7/7 | 정지 미리보기, 한글/메시/배치, 원본·dirty 상태·저장 보호 |
| core-replay-tests.json | 14/14 | 고정 7시나리오 273명령 및 저장/재생 상태 회귀 |

- Workbench 첫 실행의 결과 파일 회수에 실패해 같은 fixture를 한 번 재실행하고 최종 7 PASS 원문을 저장했다. 0개 실행이나 회수 실패를 PASS로 세지 않았다.
- `final-verification.json`: manifest, packages-lock, Title, DefaultFateDice, 사용자 run.json, HEAD가 baseline 대비 불변이다. 기존 staged 변경 없음, 신규 .meta 존재 확인. HEAD는 `d8df4999174ad63bb73e0727ff0149cb55624db2`다.
- `authoring-repeat.json`: 반복 적용 후 prefab_unchanged 및 catalog_unchanged가 모두 true다.
- 프로젝트 소유 C# 99개에 대응하는 CodeMap 누락 0. 변경·신규 소스 6개와 캡처 helper의 1:1 요약, 직접 관계, INDEX를 동기화했다. 프로젝트 소유 소스/문서 diff 공백 검사 통과.
- 프리팹까지 포함한 공백 검사는 Unity가 직렬화한 빈 문자열의 `m_Name: ` 등에서 trailing whitespace를 보고한다. 자산을 수동 정리하지 않고 Editor 출력 그대로 보존했다. 위 소스/문서 검사는 해당 직렬화 파일을 제외한 결과다.

## 최종 시각 증거
실제 DiceRollUI 원본과 Workbench 미리보기를 PreviewScene에서 렌더링했다. 고정 조합 값을 넣은 시각 fixture이며 무작위로 굴린 실플레이 화면이 아니다. 각 캡처 후 임시 카메라/RenderTexture/미리보기를 정리한다. 두 화면 비율에서 이름·눈금·링의 구분, 참여 주사위와 색상, 하단 요약 간격을 육안으로 확인했다.

| 조합 | 720×1280 | 720×1600 |
|---|---|---|
| 투 페어 | [캡처](../../artifacts/dice-combo-aura/TwoPairs-1280.png) | [캡처](../../artifacts/dice-combo-aura/TwoPairs-1600.png) |
| 트리플 페어 | [캡처](../../artifacts/dice-combo-aura/ThreePairs-1280.png) | [캡처](../../artifacts/dice-combo-aura/ThreePairs-1600.png) |
| 식스 카드 | [캡처](../../artifacts/dice-combo-aura/SixKind-1280.png) | [캡처](../../artifacts/dice-combo-aura/SixKind-1600.png) |

각 PNG와 같은 이름의 txt에 fixture 값, 해상도, 표시 이름·규칙 수치와 메시 상태를 기록했다. 캡처 도구는 `artifacts/dice-combo-aura/AuraPreviewCapture.cs`다.

## Finding과 한계
- DCA-R01: 최초 UI 검사 2건이 CanvasRenderer 누락으로 실패했다. RequireComponent와 최초 한 번의 실제 프리팹 직렬화로 해결했다. 초기 29 PASS/2 FAIL 증거도 보존했다.
- DCA-R02: 별 중심색의 흰색 보간이 alpha를 올리는 문제를 RGB 보간 후 원래 alpha 유지로 해결하고 실제 메시 검사로 확인했다.
- DCA-R03: 기본색 migration의 RGB 전용 비교를 RGBA 전체 비교로 보정해 사용자 alpha를 보존한다. 독립 fixture 통과.
- DCA-R04: 아래 별빛과 요약 간격 및 뒤쪽 화면의 시각 간섭을 별빛 축소·요약 위치 조정·dim 강화로 해결했다. 최종 6캡처 확인 완료.
- 최초 통합 검토 이후 보완은 총 2/2 사용했다. 새 필수 결함 없음. 이전 DICE_PRESENTATION_EFFECTS의 별도 보완 이력은 재설정하지 않았다.
- 이번 작업에서 native Computer Use 수동 클릭은 실행하지 않았다. UI 동작은 실제 입력 경로를 사용하는 자동 PlayMode 검사로 확인했다. 모바일 실기기 빌드·프레임 성능 측정 및 전체 프로젝트 테스트 일괄 실행은 NOT_RUN이다.
- Git staging/commit/branch 변경 없음.
