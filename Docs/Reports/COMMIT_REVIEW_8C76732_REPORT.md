# 외부 커밋 검토 보고서 대조

상태: DOCUMENT / 검토 완료. 검토일 2026-09-11. 대상은 사용자가 제공한 `C:/Users/coli4/Downloads/Dice_Warrior_Commit_Review_8c76732.md`이며, [PLAN](../Plans/COMMIT_REVIEW_8C76732_PLAN.md)의 읽기 전용 검토 범위를 따른다. 게임의 실패 해결이나 구현 완료를 뜻하지 않는다.

## 판정

보고서의 핵심 분류와 권장 우선순위는 타당하다. CR-01은 입력 소유자 중복, CR-02는 변경된 작성 계약을 반영하지 않은 테스트 기대값, CR-03은 의도적으로 설정한 가격의 밸런스 문제다. 세 항목을 모두 코어 설계 결함으로 묶거나 전면 재작성할 근거는 없다.

이번 판정은 로컬 고정 커밋 `8c767329eddfd8bf4c74c07c7bb4c757112681f6`의 실제 코드·자산, 해당 코드 요약, 이전 계획·보고서, 남아 있는 실제 테스트 JSON을 대조한 결과다. Unity·테스트·빌드·기기 검사를 새로 실행하지 않았다. 원격 push 상태는 이번 검토 대상이 아니며 확인하지 않았다.

## 지적사항별 근거와 최소 후속 범위

### CR-01 — 확인: 활성 EventSystem 두 개

- `Assets/_Project/Scenes/Title.unity:226`의 EventSystem 오브젝트는 활성 상태이며 InputSystemUIInputModule과 EventSystem도 활성이다(`:231,239,270`).
- `Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab:195`의 Input 오브젝트도 활성이고 두 입력 컴포넌트가 활성이다(`:200,223,238`).
- `RunUIController.cs:75`는 Initialize에서 공용 UIRoot를 인스턴스화한다. GameApplication은 이 계층을 초기화하고 씬 간 유지한다. 따라서 Title의 별도 입력 소유자와 함께 시작하는 경로에서 중복이 발생한다.
- 기존 실제 `artifacts/campaign-flow-polish/play-final.json`의 6건은 KoreanUiTests 3건과 SceneStructureTests 3건이다. 4건에는 두 EventSystem 경고가 기록되고, 2건은 하나를 기대한 활성 EventSystem 수가 2여서 실패했다. `SceneStructureTests.cs:83`의 수량 검사는 Canvas 검사가 아니라 EventSystem 검사다.

영향: 단일 UI 입력 소유권 계약 위반과 통합 검사 실패다. 정적 중복 및 검사 실패만으로 모든 버튼이 두 번 실행된다고 단정하지 않은 외부 보고서의 표현이 적절하다.

해결 조건: 후속 구현에서 Title 자산의 중복 소유자를 제거하거나 비활성화하고 공용 UIRoot 소유자를 유지하는 최소 변경이 적절하다. Scene 쓰기 범위는 파일 단위로 명시해야 한다. 전역 탐색 후 삭제하는 런타임 보정은 필요하지 않다. 실제 Title 시작·씬 전환·재개·모달·포인터/키보드 입력과 해당 6개 검사를 확인해야 하며, 원인 제거만으로 나머지 assertion까지 자동 통과한다고 볼 수 없다.

### CR-02 — 확인: 작성 버전 2 기대값이 남아 있음

- `Assets/_Project/Features/Run/Tests/PlayMode/LobbyCampaignFlowTests.cs:72`는 여전히 `layoutVersion == 2`를 요구한다.
- 실제 `Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab:821`은 버전 3이다. `CampaignMapAuthoring.cs:20–41`의 ApplyDirectTravel은 이동 버튼 숨김과 안내 갱신 후 버전을 3으로 저장한다.
- 실제 JSON의 해당 검사 실패도 Expected 2 / Actual 3이다. 기존 [CAMPAIGN_FLOW_POLISH_REPORT](CAMPAIGN_FLOW_POLISH_REPORT.md)의 미해결 CFP-T02와 같은 항목이다.

영향: 승인된 즉시 노드 이동 동작과 오래된 테스트 계약의 불일치다. 이 실패 자체가 버전 3 런타임의 오작동 증거는 아니다.

해결 조건: 기대값을 정확히 3으로 갱신하고 대응 `LobbyCampaignFlowTests.cs.md`의 현재 계약도 동기화한다. assertion 삭제나 범위 완화는 부적절하다. 해당 검사와 직접 입력·중복·잠금·재개 검사를 유지한 채 재검증한다. 이전 작업은 보완 2/2와 추가 허용 대기 상태로 기록되어 있으므로, 이 읽기 전용 대조를 추가 보완 승인이나 새 예산으로 취급하지 않는다.

### CR-03 — 확인: 같은 상품의 고등급 가격만 증가

- `DefaultFateDice.asset:775–785`의 작은 회복약은 기본 가격 10, 보상 health 25다. `:819–824`의 배율은 `[1, 1.1, 1.25, 1.5, 1.75]`다.
- `ShopRules.cs:15–23`의 곱셈과 MidpointRounding.AwayFromZero를 적용하면 가격은 아래 표와 같다. 이번 검토에서도 별도 산술 계산으로 대조했다. 이는 Unity 실행 결과가 아니다.

| 등급 | 가격 | 설정상 회복량 |
| --- | ---: | ---: |
| 일반 | 10 | 25 |
| 고급 | 11 | 25 |
| 희귀 | 13 | 25 |
| 영웅 | 15 | 25 |
| 전설 | 18 | 25 |

- `RunApplication.cs:278–286`은 확정된 offer.price를 차감하고 공통 상품 보상을 복제한다. 기본 shop_0~4 사건 보상도 0이며, 고등급의 추가 상품·수량·회복 보상으로 가격 증가를 상쇄하지 않는다.
- **외부 표의 회복 25는 명목값이다.** `GrowthRules.cs:32–34`에서 최대 체력으로 제한되므로 실제 회복량은 부족한 체력에 따라 더 작을 수 있다. 이 보충은 가격 역전 지적을 바꾸지 않는다.
- `ContentExtensionTests.cs:162–175`는 현재 가격·차감·구매 제한·저장 재개·RNG 계약을 검사한다. 고등급의 효용이 더 좋아야 한다는 검사는 아니다. `ROGUELIKE_ARCHITECTURE_PLAN.md:158–160`에는 증가 배율이 시험값으로 명시되어 있다. 따라서 수식 구현 오류나 확정 수치 위반으로 분류하지 않은 보고서가 맞다.

영향: 같은 조건에서 동일 상품을 사는 데 고등급이 불리하다. 상점 등급을 좋은 결과로 보여주는 플레이 의도와 충돌할 수 있는 밸런스 문제다.

해결 조건: 가격만 바꾸는 기존 범위에서는 고등급일수록 가격이 유지되거나 할인되도록 정하는 방향이 가장 작다. 정확한 배율은 별도 설계 결정이며 이번 검토에서 확정하지 않는다. 높은 가격을 유지하려면 상품·보상 이익을 새로 설계해야 하므로 안정화 작업에 자동 포함하지 않는다.

저장 호환성: RulesCopy는 상점 규칙·보상·배율을 복제하고 RunStateCopy는 확정 offer와 구매 상태를 복제한다. LocalRunStore의 구형 상점 복원도 저장된 규칙을 사용한다. 새 기본 배율을 적용하더라도 기존 런의 저장 규칙과 확정 가격을 소급 갱신하지 말라는 외부 권고가 현재 구조에 맞다.

## 구조·호환성·의존성 주장 대조

외부 보고서 4–6절의 지정 주장에 필수 정정 사항은 발견하지 못했다. Main과 읽기 전용 보조 검토를 한 묶음으로 통합했다.

| 외부 주장 | 원본 근거 및 판정 |
| --- | --- |
| 새 여정의 일반 시드와 고정 주입 분리 | RunUIController.cs:79,125–137과 SystemSeedSource.cs:9–20에 부합한다. |
| Core 상태 소유·깊은 복사·명령 경계 | RunApplication.cs:32–75,335–352가 입력/조회 복사와 후보 처리→검증→독립 저장 복사→상태 교환 순서를 갖는다. 저장 예외 전에 상태를 공개하지 않는다. 시간은 누적 경로이며 매 프레임 저장하지 않는다. |
| Unity와 Core 참조 분리, Runtime facade | FateDice.Core.asmdef:4–8의 참조 설정, CoreBoundaryTests.cs:15–27의 실제 컴파일 참조 검사, RunSession.cs:27–62의 위임과 일치한다. |
| 효과 확장·카드 획득·저장 호환 | EffectResolver의 명시 효과/구형 계수 fallback, GrowthRules.cs:16–19의 중복 없는 addActionId 처리, ContentExtensionTests.cs:115–136의 획득·추첨·사용·복원 검사, LocalRunStore.cs:54–103의 저장 JSON 기준 복원 경로에 부합한다. |
| 새 지도 생성과 구형 경로 보존 | ProceduralMapGenerator.cs:20–22,62–78은 지도용 RNG와 전층/단일 보스 생성 경로다. ExplorationRules.cs:8–18,33–54는 모드 0/1 분기를 유지한다. 구형 재연 결과만으로 새 생성기를 승인할 수 없다는 지적이 적절하다. |
| 명령 확정 후 연출 및 복구 | RunUIController.cs:549–553,600–624는 명령 확정 후 연출하고 실패 복구에서 명령을 다시 호출하지 않는다. PresentationPlayback.cs:90–188은 중첩 진행·취소·시간 초과·Dispose 정리를 감독한다. |
| 외부 에셋 별도 설치 필요 | Runtime asmdef:10–12의 Febucci/MoreMountains 직접 참조와 DICE_PRESENTATION_DEPENDENCIES.md:3–17이 일치한다. 명시된 FEEL 6.1, Text Animator 3.14.2/기본 콘텐츠, All In 1 Sprite Shader 4.68의 버전·GUID·참조 텍스처 재현이 필요하다. 최신 clone 빌드 성공은 확인하지 않았다. |

보충할 연출 한계: 10초는 실시간 시계로 측정하지만 판정은 Unity가 감독 iterator를 실행할 때 발생한다. PresentationPlayback.cs:96–107은 사용자 MoveNext 전후에 시간을 검사한다. 따라서 앱이 OS에 의해 정지하거나 사용자 MoveNext가 반환하지 않는 동안 정확히 10초에 선점 종료하는 장치는 아니다. 외부 보고서가 백그라운드 복귀를 미검증으로 분리한 것은 맞다. fake clock/timeScale 검사나 Editor의 10초 timeout PASS를 실제 모바일 suspend/resume 결과로 대체하지 않는다.

이는 지정 구조의 정적 대조 결과이며 프로젝트 전체에 결함이 없다는 판정은 아니다. 이번 범위에서 인터페이스·상속 계층을 추가하거나 상태/저장 구조를 다시 만드는 후속 작업을 요구할 근거도 없다.

## 검증 기록의 해석

외부 보고서는 자체 실행이 없다는 점을 명시했다. 이번에는 문서 숫자뿐 아니라 기존 실제 `artifacts/campaign-flow-polish/play-final.json`도 읽었다.

| 기존 실행 산출물 | 확인 결과 | 해석 |
| --- | --- | --- |
| 최신 기록의 전체 PlayMode | 196건, 189 PASS, 7 FAIL, 344.79초 | 전체 통과 아님 |
| CampaignFlowPolishTests | 11/11 PASS | 새 흐름의 지정 검사 통과 |
| KoreanUiTests + SceneStructureTests | 6 FAIL | CR-01 관련 경고/수량 실패 |
| PreparationLobbyAndSixDicePopupHaveAuthoredContracts | 1 FAIL | CR-02 / 기존 CFP-T02 |

새 11건에는 지도 복귀·수동 스크롤, 노드 탭과 저장 실패 재시도, 실제 Mouse drag, 전투 진입과 주사위 순서, 재개·재굴림, 취소·disable·새 바인딩 보존, 실제 10초 watchdog 검사가 포함된다. 이는 과거 실행 증거이며 이번 재실행이 아니다. 모바일 실제 터치, OS 백그라운드 복귀, 프레임·발열·메모리 결과로 확대 해석할 수 없다.

`ROGUELIKE_ARCHITECTURE_REPORT.md`의 이전 EditMode 228/228, PlayMode 102/102, Android 빌드 성공 기록은 이전 단계의 증거다. 외부 보고서처럼 최신 8c76732의 빌드·실기기 성공과 구분해야 한다. 실패를 남긴 체크포인트 커밋과 모든 완료 조건 충족도 구분해야 한다.

## 후속 순서

1. CR-01/02의 최소 안정화 범위와 기존 보완 허용 상태를 정리한 뒤 해당 검사 및 최신 전체 EditMode/PlayMode를 실행한다.
2. CR-03은 별도 밸런스 결정으로 배율 방향과 값을 정하고 기존 저장 규칙·가격을 보존한다.
3. 의존성이 준비된 환경에서 최신 ARM64 IL2CPP 빌드와 실제 기기 입력·팝업·전환·중단/복귀를 확인한다.

이번 요청에서 수정·실행 권고를 실제로 수행하지 않았다. 외부 원문, 코드·Scene/Prefab·SO·패키지·설정·사용자 저장 파일은 편집하지 않았다. Git staging·commit·push도 수행하지 않았다. 쓰기 대상은 이 REPORT와 대응 PLAN뿐이며 코드 요약/색인 갱신은 비적용이다.

검토 묶음 1회. 이번 문서 대조의 근거 오독 보완 0/2회. CR-01/02는 해결되지 않은 구현 후속 항목, CR-03은 미결정 밸런스 항목으로 유지한다. 이전 구현의 완료 판정이나 보완 횟수는 변경하지 않는다.
