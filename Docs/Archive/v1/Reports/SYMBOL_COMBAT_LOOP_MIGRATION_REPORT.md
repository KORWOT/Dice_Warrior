# SYMBOL_COMBAT_LOOP_MIGRATION — 기획·구조 전환 검토

작성일: 2026-10-20. 유형: DOCUMENT.
상태: 전환 설계 승인 / P0 상세 실행 계획 작성 완료 / 실행 계획 검토 대기.
작업 계약: [PLAN](../Plans/SYMBOL_COMBAT_LOOP_MIGRATION_PLAN.md).

## 1. 검토 결론과 요청 해석

첨부 GDD가 제시하는 방향은 기존 프로젝트에 적용 가능하다. 다만 숫자 주사위의 표시만 바꾸는 작업이 아니라 전투 턴, 캠페인 상태, 출전, 저장 검증, 정산 정책을 함께 확장하는 개편이다.

추천은 **기존 v1을 보존하고 `SymbolLoop` 새 런 모드를 단계적으로 추가**하는 것이다. 현재의 후보 복제 → 검증 → 저장 → 공개 상태 확정 경계와 프로필·정산·복구는 유지한다. 기존 족보·카드·분기 지도 규칙을 새 모드로 재해석하지 않는다.

이번 사용자 요청은 첨부 확인 후 개편 진행이다. 첨부에 적힌 “확정”은 원문이 주장하는 기획 상태로 구별했으며, 그 안의 구현·Git 지시나 과거 승인 주장을 별도 사용자 승인으로 취급하지 않았다. 이후 사용자가 이 보고서의 추천 방식·MVP 범위·D1/D2에 “좋아”라고 답해 아래 승인 기록의 설계 기준으로 채택했다. 새 실행 계획과 실제 구현의 상태는 분리한다.

원문은 `C:/Users/coli4/Downloads/Dice_Warrior_GDD_Symbol_Combat_Loop_Campaign_v0.2.md`, SHA256은 `0D3BD3AD15830795AFDD8EFC9B1DA61A68B7C2D1E8B5FB50D034C9D595DA99BE`다. 기준 HEAD와 원문 기준 커밋은 모두 `82b4b7befd0a90f2e37a9a24be4612714040a2ce`이며 현재 브랜치는 main, Unity는 `6000.6.0f1`이다.

## 2. 기획 상태 분리

| 구분 | 내용 |
|---|---|
| 원문의 확정 방향 | 숫자 이동과 기호 전투 분리, 전투 주사위 1개 시작, 전체 멀티셋 정확 일치 기술 1개, 미등록이면 각 눈의 기본기, 학습/행동카드 선택 없음, 지역 한 바퀴 확정 보상과 새 맵 |
| 첫 시험안 | 검/지팡이/불/방패, 6면 기호 주사위 최대 3개, 턴마다 굴림 입력, D6 이동 1개, 12칸 보드, 3지역 후 최종 보스 |
| 첫 시험안의 진행 | 착지 사건만 실행, 완주 초과 이동 소멸, 지역 1·2 보상으로 1→2→3개, 최종 지역 보상 후 보스, 전투 재굴림 비활성 |
| 계속 보류 | 최종 주사위 상한, 면 제작, 최종 밸런스, 이동 선택/보정, 장기 캠페인, 상태이상·다중 적, 온라인/과금/서버 |
| 구현 전에 보강할 것 | 처치 시 남은 기본기, 적 보호막 수명, 신규 슬롯에 실제 주사위 부여 방식, 규칙/저장/요청 버전 경계 |

지역 3 보상 후보, 기호 면 분포, 조합별 계수, 적/상점 구성, 영구 지급 수치는 전투 계산기나 호환성 설계의 선행 차단 요소가 아니다. 각 콘텐츠가 실제 실행되는 단계 전에 시험 데이터와 기대값을 고정해야 한다.

## 3. 현재 코드에서 확인한 구조

경로는 프로젝트 루트 기준이다. 아래는 소스 확인 근거이며 실행 PASS를 뜻하지 않는다.

| 영역 | 현재 근거 | 전환 시 의미 |
|---|---|---|
| 숫자 주사위 | `Assets/_Project/Features/Dice/Runtime/DiceRules.cs:51`은 6개 고정, 77행 이후 족보 | 기호 굴림은 별도 규칙. 기존 숫자/족보 경로와 RNG 소비 계약 보존 |
| 전투 턴 | `Assets/_Project/Features/Combat/Runtime/CombatRules.cs:28`은 한 카드와 적 행동까지 한 턴 처리 | 기본기마다 Resolve를 반복하면 적 행동·턴·만료가 중복된다. 여러 기술을 한 턴으로 해결하는 경계 필요 |
| 효과 계산 | `Assets/_Project/Features/Combat/Runtime/ActionEffects.cs:56`은 카드 ID와 등급 비율 사용 | Damage/Block·반올림·태그 계산을 좁은 입력으로 공유. v2에 가짜 카드/소유/등급을 요구하지 않음 |
| 지도 | `Assets/_Project/Features/Exploration/Runtime/ExplorationRules.cs:8`, `Assets/_Project/Features/Exploration/Runtime/ProceduralMapGenerator.cs:20` | 기존 생성 버전 0/1 모두 v1 지도다. `mapGenerationVersion`을 게임 모드로 재사용하지 않음. 별도 순환 보드 생성/진행 |
| 지도 표시 | `Assets/_Project/Features/Run/Presentation/CampaignMapProjection.cs:29`는 floor/lane/연결/공개 깊이 사용 | 순환 보드용 DTO·표시 투영 필요. 표시가 위치/착지/RNG를 결정하지 않음 |
| 주사위 UI | `Assets/_Project/Features/Run/Presentation/DiceRollUI.cs:33`, `Assets/_Project/Features/Run/Presentation/RunUIController.cs:426` | 숫자 6개 검증과 족보 표시가 고정. 기호 1~3개와 이동 숫자 1개용 표시 계약 필요 |
| 연출 | `Assets/_Project/Features/Run/Presentation/RunUIController.cs:556`은 명령 성공 뒤 연출, 619행 이후 watchdog 복구 | 저장된 턴 로그를 재생하고 취소/오류 시 확정 상태만 표시하는 구조 유지 |
| 상태/저장 | `Assets/_Project/Features/Run/Domain/RunStateValidator.cs:62`, `Assets/_Project/Features/Meta/Runtime/PlayerSaveDocument.cs:30` | 기존 6개 검증과 구체 RunState 저장을 모드별로 분리해야 함 |
| 원자 확정 | `Assets/_Project/Features/Run/Application/RunApplication.cs:335` | 새 턴의 눈/RNG/HP/반격/결과 기록을 한 후보에서 처리하고 저장 실패 시 전부 불변 |
| 출발/정산 | `Assets/_Project/Features/Meta/Runtime/LocalMetaProgressionService.cs:114`, 153행 | 기존 소유·미정산 결과 보호·요청 ID·receipt 재사용. 모드별 출전과 시작 시 동결한 지급 정책 필요 |
| 조립 | `Assets/_Project/Features/Run/Flow/GameApplication.cs:42` | 기존 명시적 서비스/설정 주입 확장. 새 singleton·런타임 전역 탐색 도입 불필요 |

`FateDice.Core`는 `Assets/_Project/Features/Run/Domain/FateDice.Core.asmdef`의 `noEngineReferences=true`로 정의된다. Dice/Combat/Exploration의 기존 Runtime 폴더에는 Core asmref가 있다. 따라서 “Runtime 폴더 = Unity 의존 코드”가 아니다. 새 순수 규칙은 기존 Core 소속 폴더에 추가하는 것이 가장 작은 변경이다. GDD의 새 Domain 폴더 이름은 제안이며, 채택한다면 해당 폴더의 asmref/.meta까지 명시해야 한다.

## 4. 접근안 비교

| 안 | 장점 | 비용/제약 | 판단 |
|---|---|---|---|
| A. 기존 저장·명령 경계에 명시적 모드와 전용 payload 추가 | 복구·소유·정산·UI 수명 재사용, v1 이어하기 가능 | 복제/검증/codec/요청에 모드 분기 필요 | 추천 |
| B. 격리된 새 프로토타입만 제작 | 전투 재미 시험을 빠르게 시작 가능 | 프로필/완주/저장 호환 검증을 나중에 다시 연결해야 함 | 전투 실험만 원할 때 선택 |
| C. 기존 타입·규칙을 일괄 대체 | v2 경로 자체는 단순 | 구형 저장·정산·재연 보호 요구와 충돌 | 현재 제약에서는 제외 |

추천 A의 초기 사용자 흐름은 **기존 v1 이어하기를 유지하고, 정산 또는 명시적 포기 후 새 v2 출발**이다. v1/v2 신규 출발을 자유롭게 전환하는 완전한 모드 선택 UI는 첫 목표에 포함하지 않는 안을 제안한다.

## 5. 승인된 전환 설계 기준

### 전투

- `SymbolComboRules`: 실제 기호 배열을 개수 서명으로 정규화하고 전체 정확 일치만 조회한다. 조합 중복/지원하지 않는 기호/기본기 누락은 시작 전에 거절한다.
- `AutoCombatResolver`: 굴림 → 기술 계획 → 효과 적용 → 생존 적 행동 1회 → 사망/승리/다음 턴/기록을 처리한다. v1 `CombatRules.Resolve` 반복 호출은 금지한다.
- 기본기/조합은 같은 효과 평가 입력을 사용한다. v1 래퍼의 등급 비율과 반올림은 보존하며, v2는 카드 등급 비율에 영향을 받지 않는다.
- 승인 D1: 미등록이면 모든 눈의 기본기를 계획하되 인덱스 순으로 적용하고, 적 처치 후 남은 기술은 `enemyDefeated` 사유로 생략한다. 계획 목록·실제 적용 목록·생략 목록을 구분한다. 같은 멀티셋은 같은 조합 판정/기본기 구성을 보장하지만, 사망 예외가 있는 fallback의 실제 적용 횟수까지 순열 독립이라고 주장하지 않는다.
- 승인 D2: 적 보호막은 플레이어 기술 묶음 전체에서 차감하며, 묶음 종료 시 잔량이 만료된다. 첫 기본기가 방어라는 이유만으로 적 보호막을 지우지 않는다. 플레이어 보호막은 기본기들을 합산하고 적 행동 후 만료하며 전투 종료 시 정리한다.
- D1/D2는 원문의 모호한 경계를 사용자가 이번 대화에서 확정한 것이다. P0에 독립 시험 입력을 기록하고 실제 효과 처리는 P1에서 검증한다.

### 이동·지역·성장

- `RollTravel`은 위치와 착지 사건 또는 완주 보상 대기를 한 번에 확정한다. 별도 UI 입력으로 임의 타일에 진입할 수 없다.
- 12칸 시험안에서 `position + roll >= 12`이면 초과 눈을 버리고 완주 대기로 간다. 일반 타일 사건은 실행하지 않는다.
- 지역별 지도는 런 시드·지역 번호·생성 버전으로 파생한 별도 RNG에서 만든 후 타일 목록을 저장한다. 전투와 이동 RNG도 각각 소유하여 연출/다른 시스템의 소비와 분리하는 안을 제안한다.
- 지역/타일/보상 ID에는 runId와 region을 포함한다. 보드 배치는 지역 중 다시 추첨하지 않는다.
- 1·2지역 보상은 빈 슬롯만 늘리지 않고 **검증된 기호 주사위 인스턴스 추가와 장착까지** 같은 후보에서 수행하는 안을 제안한다. 기호 면 분포와 지급 정의 ID는 콘텐츠 단계에서 고정한다.
- 선택형 완주 보상은 후보를 먼저 저장한다. 수령 성공 시 지급 기록과 다음 지역 생성 또는 최종 보스 진입을 함께 확정하면 별도 `AdvanceRegion` 명령을 줄일 수 있다.
- 장비/주사위 교체 UI가 필요하면 그 선택까지 완료하기 전 다음 지역/보스로 넘어가지 않는다. 보상 팝업 닫기를 지급으로 취급하지 않는다.
- 상점/보물/전투 보상은 카드·재굴림·숫자 주사위 보상이 들어오지 않도록 v2 콘텐츠 검증을 둔다.

### 저장·프로필·정산

- 추천 데이터 형태는 공통 런 식별/sequence/복구 메타데이터와 **명시적 mode + 모드 전용 상태/규칙 payload**다. v1 필드 의미와 enum 기존 숫자는 보존한다. 임의 CLR 다형 직렬화는 사용하지 않는다.
- 오래된 저장에 mode가 없으면 LegacyDiceFate로 읽는다. 명시적으로 알 수 없는 mode/버전 또는 payload 불일치는 거절하고 원본을 보존한다. 읽기만으로 이관/초기화/쓰기하지 않는다.
- `MetaJson`은 `TypeNameHandling.None` 및 `MissingMemberHandling.Error`를 사용한다(`PlayerSaveDocument.cs:79`). 기존 저장을 새 DTO로 단순 덮어 읽는 것으로 호환을 가정하지 않는다. schema1 codec과 새 schema codec의 허용 필드/왕복/체크섬 경계를 초기 구현 계획에서 고정한다.
- `RunStateCopy`, `RulesCopy`, Core/Runtime 변환, 저장 왕복 모두 새 배열·보드·로그·정책을 독립 복제한다. SO 수정이나 반환 snapshot 수정이 진행 중 런에 스며들지 않아야 한다.
- 기존 6개 숫자 주사위 출전은 유지한다. v2 출전·기본 기호 주사위 최초 지급은 별도 버전/지급 표식으로 중복 없이 추가하는 안을 제안한다. 실제 프로필 변경은 명시적 새 모드 출발 절차와 원자 백업/저장에 포함한다.
- `legacyRun`은 **옛 단독 저장 이관 여부**이며 LegacyDiceFate 모드와 다르다. 정상 프로필 v1은 시작 때 고정한 사건당 지급 정책으로 정산하고, 기존 imported legacy만 원래의 지급 0 정책을 유지한다.
- 시작 요청의 mode/출전 버전은 business fingerprint에 포함한다. 과거 mode 없는 pending request와 receipt는 기존 fingerprint로 조회/재시도하며 새 모드로 재해석하지 않는다.
- 진행 중 런 교체는 명시적 포기 필요, 미정산 Result는 정산 전 교체 금지라는 현재 계약을 유지한다.
- 지역 보상 ID와 종료 정산 receipt는 별개다. v2 영구 정산은 시작 때 동결한 지역/승리 정책을 사용하고 과거 receipt를 재계산하지 않는다.
- 복구 세대·revision CAS·동일 sequence 허용 범위·정상본 백업은 유지한다. 새 모드/규칙도 기존 비교·복구 범위에 포함되는지 검증한다.

## 6. 단계와 정확한 영향 후보

다음은 구현 계획에 반영할 파일 후보이며 지금의 쓰기 허가는 아니다. `F/`는 `Assets/_Project/Features/`를 뜻한다. 수정/신규 C#마다 대응 CodeMap과 직접 관계/INDEX 갱신을 포함한다.

| 단계 | 대상 파일 후보 | 완료 조건 |
|---|---|---|
| P0 계약·기준 | 본 PLAN/REPORT를 바탕으로 후속 실행 계약 고정. 기존 `F/Run/Tests/EditMode/Fixtures/ReplayBaseline.json`, CoreReplay/Save/Meta/Recovery 테스트를 기준으로 사용 | D1/D2와 모드/codec/요청 호환 확정, 변경 전 실제 테스트, 격리 합성 v1 저장·프로필·백업·pending fixture 확보 |
| P1a 최소 저장 기반 | `F/Run/Domain/{RunStateData,RunStateCopy,RunStateValidator,CoreRunState}.cs`, 신규 `RunMode.cs`/`SymbolRunState.cs`; `F/Run/Runtime/{RunState,RunSession}.cs`; `F/Run/Application/RunApplication.cs`; `F/Fate/Domain/RulesCopy.cs`; `F/Save/Runtime/{IRunStore,LocalRunStore}.cs` | 모드별 상태/규칙 복제·검증·저장·복원, 구형/미지원 입력 분기, 실패 시 전부 불변 |
| P1a 출발·요청 연결 | `F/Meta/Domain/PlayerProfileData.cs`; `F/Meta/Runtime/{PlayerSaveDocument,IMetaProgressionService,LocalMetaProgressionService,MetaPendingRequest,LocalMetaRequestJournal}.cs`; `LocalPlayerDataRecovery.cs`는 새 codec 영향 시에만 수정 | 1개 기호 주사위 출발, mode fingerprint/옛 요청 호환, 기존 출전/active-run/receipt/세대 보호 |
| P1b 기호 전투 | 신규 `F/Dice/Runtime/{SymbolDiceRules,SymbolDiceDefinitions}.cs`; 신규 `F/Combat/Runtime/{SymbolCombatDefinitions,SymbolComboRules,AutoCombatResolver,CombatTurnResult}.cs`; 기존 `F/Combat/Runtime/ActionEffects.cs`, `F/Growth/Runtime/GrowthRules.cs`, Run 명령/상태 연결 | 1~3개 전체 조합·fallback·D1/D2·적 행동 1회, 명령 저장 실패/재개. 순수 계산기만 통과하면 P1 전체 완료가 아님 |
| P2 표시·연출 | `F/Run/Presentation/{RunUIController,RunUIData}.cs`; 신규 `F/Combat/Presentation/{SymbolCombatUI,SymbolCombatUIData}.cs`, `F/Dice/Presentation/SymbolDieView.cs`; 기존 `PresentationPlayback.cs`는 재사용 우선 | 카드 선택 없는 실제 승패, 1~3개 기호, 확정 로그 재생, 닫기/재바인딩/timeout/재개 중복 0 |
| P3 순환 캠페인 | 신규 `F/Exploration/Runtime/{LoopBoardDefinitions,LoopBoardGenerator,LoopBoardRules}.cs`; 신규 `F/Dice/Runtime/TravelDiceRules.cs`; 신규 `F/Exploration/Presentation/{LoopBoardUI,LoopBoardUIData,LoopBoardProjection}.cs`; Run 명령/상태 연결 | 5종 타일·정확한 착지/완주, 지역 지도/저장 결정성, 전투 소비와 지도 배치 독립 |
| P4 보상·완주 | `F/Growth/Runtime/GrowthRules.cs`, `F/Shop/Domain/ShopRules.cs`의 재사용 가능한 입력과 모드별 콘텐츠 연결; Run 명령/상태, Meta 시작정책/정산; 신규 `F/Exploration/Configs/LoopCampaignConfig.cs`, `F/Combat/Configs/SymbolCombatConfig.cs` | 1→2→3 실물 지급, 교체/상점, 지역 3 보상→보스→정산 1회. 필요한 최소 config는 P1/P3에서 먼저 도입 |
| P5 제작 로비·통합 | `F/Run/Flow/GameApplication.cs`; `F/Run/Presentation/{RunUIController.Meta,MenuUI,ResultUI}.cs`; 필요한 전용 Editor authoring/PlayMode 검사 | authored 연결, 기존 런 이어하기와 새 모드 출발, 전체 EditMode/PlayMode·Android 빌드·실기기 별도 보고 |

GDD의 P5에 있는 호환성 전체를 마지막으로 미루지 않는다. P1/P2가 저장 재개를, P4가 프로필 정산을 요구하므로 **최소 호환·출발·저장 기반을 P1a로 선행**한다. P5에는 제작 로비와 최종 통합을 남긴다. P0는 계약/기준 확보이며 현재 조사만으로 P0 구현 완료라고 판정하지 않는다.

Scene 쓰기는 현재 필요하다고 확정하지 않았다. UI 단계 자산 후보는 새 `F/Combat/Prefabs/SymbolCombatUI.prefab`, `F/Exploration/Prefabs/LoopBoardUI.prefab`, 그리고 연결용 기존 `F/Run/Prefabs/GameApplication.prefab`, `MenuUI.prefab`, `ResultUI.prefab`다. 실제 등록 참조를 확인하여 실행 PLAN에 파일별 허용을 고정한 뒤 Editor API로만 작성한다. v1 CombatUI/DiceRollUI와 GUID/.meta는 보존한다. 허용되지 않은 연결은 ASSET_HANDOFF로 남기며 런타임 UI 생성으로 우회하지 않는다.

신규 기호/이동 표시를 기존 6개 숫자 팝업에 억지로 연결하지 않는다. 필요한 이동 주사위 View/DTO와 UI prefab 등록 경계의 정확한 파일은 P2/P3 실행 PLAN에서 확정한다.

## 7. 필수 Finding과 해결 조건

이 항목들은 새 전환 설계의 계약/순서 문제다. 기존 게임의 신규 버그를 발견했다는 뜻이 아니다.

| ID | 근거·영향 | 해결 조건 | 상태 |
|---|---|---|---|
| SC-C1 | GDD 3.2의 기본기 전부와 3.3의 처치 중단, 11.1.6의 순열별 효과 동일성이 충돌 | 5절 D1로 판정/계획과 실제 적용·사망 생략을 구분 | 설계 해결: D1 사용자 승인. 구현 검증 미실행 |
| SC-C2 | `CombatRules.cs:33–35`의 카드당 적 보호막 만료를 여러 기본기에 반복 적용하면 방어 기본기가 적 보호막을 제거 | 5절 D2의 턴 묶음 수명과 첫 기술 Block/다중 공격 오라클 | 설계 해결: D2 사용자 승인. 구현 검증 미실행 |
| SC-P1 | GDD P1/P2/P4가 P5에 놓인 저장·출전·정산 기반에 선행 의존 | P1a 선행 및 순수 코어/저장 통합 완료 구분 | 제안에 반영; 실제 구현/검증 미실행 |
| SC-P2 | 기존 `legacyRun`을 모드로 재사용하면 정상 v1 정산을 0으로 만들 수 있음 | 모드와 이관 출처 분리, 정상 v1/이관 v1/v2 각각 정산 오라클 | 제안에 반영; 실제 구현/검증 미실행 |
| SC-P3 | start fingerprint는 현재 bool뿐이며 JSON/요청은 구체 v1 타입 | 새 요청 mode fingerprint, 옛 journal/receipt 호환, 명시 codec 라우팅 | 제안에 반영; 실제 구현/검증 미실행 |

## 8. 필수 검증 계획

- 전투: 네 기호의 1~3개 순서 있는 결과 총 84개를 독립 멀티셋 오라클로 검증한다. 정확 조합/미등록/중복 눈/등록 순서/사망 예외/보호막/적 반격 0 또는 1회/턴 +1을 검사한다. 확률 검사는 실제 면 분포·가중치로 별도 계산한다.
- 효과: v1 독립 수치 오라클과 반올림을 유지한다. v2는 v1 카드 등급 변경에 불변이고 기본/조합 모두 태그 장비 효과를 받는지 검사한다.
- 저장: 후보 저장 실패에서 HP/RNG/눈/위치/턴/로그/보상 ID/sequence가 모두 불변. SO·반환 snapshot·체크포인트 인자 변조와 저장 재개에서도 독립성 유지.
- 보드: 위치 0의 1/6 이동, 위치 9에서 5 이동 시 완주, 일반 타일 미실행, 지역 5유형 존재, 전투 RNG 변경에도 지도 동일, 보상 앞/뒤 재개 및 중복 입력.
- 프로필/요청: mode 없는 v1 저장·백업·pending 읽기, 미지원 보존, 신규 기호 지급 1회, 진행/미정산 교체 차단, mode 다른 같은 requestId 거부, 옛 fingerprint 재시도, 복구 후 낡은 세션/요청 차단.
- 정산: 지역 보상 ID와 영구 receipt를 각각 검사한다. 정상 v1 frozen policy, 이관 v1 지급 0, v2 frozen 지역/승리 정책, 패배/명시 포기, 같은/다른 ID 재시도와 응답 유실.
- UI: 실제 입력으로 제목→로비→3지역→보스→결과→정산→재출발 및 패배. 720×1280·1080×2400, 취소/timeout/닫기/재바인딩, 표시 RNG 비소비.
- 최종: 실제 전체 EditMode/PlayMode 및 Android 개발 빌드. 기기 검사와 Editor/빌드를 구별하며 미실행은 NOT_RUN. 현재 또는 과거 PASS를 신형 모드의 PASS로 전용하지 않는다.

기존 `ReplayBaseline.json`은 재생성하지 않는다. 현재 SHA256은 `59A69CA30D8A85F0BB3EF87F34B08FA6E73BF989365117930AD24BDF0728045B`. v2 재연 fixture는 별도다. v1 비교에서 신규 필드를 제외해야 한다면 존재·타입·의미상 구형 기본값을 먼저 검사하고 기존 필드를 숨기지 않는다.

## 9. 실제 수행·검증·남은 작업

- 수행: 원문 전체 확인, Git/Unity 버전 읽기, 기존 PLAN/CodeMap/실제 소스 대조, 전투와 저장의 독립 읽기 전용 검토 2건 통합, 본 문서와 PLAN 작성.
- 보조 위임: combat_review / persistence_review, 동시 최대 3명(메인 포함), 누적 2건, 재귀 0. 쓰기는 메인만 수행.
- 실제 제품 코드/Scene/Prefab/SO/설정/패키지/CodeMap/INDEX 변경 없음. 문서만 변경하여 1:1 요약 갱신은 비적용.
- Unity 실행·EditMode·PlayMode·Android·실기기: 이번 작업 NOT_RUN. DOCUMENT 검토에는 비적용이며 구현의 검증 PASS를 주장하지 않는다.
- `RECOVERY_STABILITY_REPORT.md`의 317/317·228/228·Android 성공은 과거 기록으로만 확인했다. 새 모드의 증거가 아니다.
- 실제 사용자 저장·백업을 열거나 변경하지 않았다. 기존 staged 없음, 사용자 자산/미추적 문서·디렉터리는 PLAN의 기준대로 보호한다.
- 최초 통합 검토 1/1, 최초 보고 당시 보완 1/2. 후속 승인 반영과 P0 계획 self-review까지 누적 보완 2/2다. 별도 보조 검토 예산은 사용하지 않았다.
- 추천 A와 첫 MVP 범위, D1/D2 전투 경계는 승인됐다. 현재 남은 결정은 P0 실행 계획 검토와 실행 방식이다. P1 이후 상세 실행 계획은 각 실제 구현 범위를 정할 때 작성한다.
- 최종 주사위 상한·출시 밸런스·온라인 기능 등 보류는 현재 문서 검토를 차단하지 않는다. 이미 승인된 설계를 다시 묻거나 기존 완료 작업을 재개방하지 않는다.

최종 확인: 소스 참조/디렉터리 약어 16개가 존재하고 인용 라인이 파일 범위 안에 있음을 확인했다. 초기 검사기는 디렉터리 약어를 파일로 오인해 조건 보정 후 통과했다. 두 문서의 후행 공백/미완성 표식 검사와 `git diff --check`를 수행했다. 기존 제외 파일 13개의 시작/종료 해시가 같고, Git 출력에서 공백 때문에 별도 확인한 폰트도 PLAN의 시작 해시와 같다. 첨부 GDD와 기존 재연 fixture의 해시도 확인했다. Git 변경 목록은 기존 변경에 이번 PLAN/REPORT 두 개가 추가된 상태이며 staged는 비어 있다.

## 10. 사용자 승인과 P0 계획 / 2026-10-20

사용자 “좋아”는 직전 질문의 추천 모드 추가 방식, 12칸·3지역·1→2→3개·최종 보스 시험 범위, D1/D2의 승인으로 기록했다. 직전 예고대로 기존 PLAN 안에 P0 상세 계획을 작성했으며 별도 Goal 문서나 제품 코드를 생성하지 않았다.

P0 계획은 (1) 실제 변경 전 검사와 보호 사본, (2) 합성 v1의 8묶음 저장/백업/pending 캡처 및 독립 재연, (3) 새 기호·보드 규칙의 시험 입력과 완료 증거로 구성했다. 신규 Editor 도구 1개, EditMode 테스트 1개, JSON fixture 2개와 해당 .meta/CodeMap·INDEX가 향후 실행 범위다. 기존 제품 C#·Scene·Prefab·SO 변경은 포함하지 않는다. MVP 시험 입력의 피해/보호 수치는 독립 경계 검증용이며 실제 밸런스 선택이 아니다.

자료 조회는 기존 persistence_review에게 public API/fixture 생성 방식만 추가 요청했다. 쓰기/Unity 실행/문서 self-review 위임 없이 누적 위임3/3, 고유 보조2명을 유지했다. 메인이 형식·단계·API/파일·검증 초점·승인 범위를 self-review했다.

실제 읽기 확인: CLI 1.0.0-beta.8, Unity 6000.6.0f1, Pipeline 0.6.0-exp.1, 해당 Editor 인스턴스1개. `unity pipeline list --format json`은 실행 중이지만 서버 `isReachable=false`, `apiUrl=null`을 반환했다. Safe Mode 여부는 null이므로 원인을 단정하지 않는다. 테스트 인자는 설치된 Pipeline `Editor/Commands/TestCommands.cs`에서 확인했다. Editor를 새로 시작하거나 패키지/설정을 바꾸지 않았다.

P0 실행 계획은 **메인 순차 실행(Native)**을 권장한다. 실제 compile·EditMode·PlayMode·fixture 생성은 모두 NOT_RUN이며 연결 확인 전 실제 실행 상태는 WAIT_VALIDATION이다. 문서 작성 완료와 P0 실행 완료를 구분한다.

## 11. P0 순차 실행 / 2026-10-20

사용자 “순차적으로 진행해줘”로 P0 상세 계획과 메인 순차 실행이 승인됐다. 현재 IMPLEMENT 진행 중이며 P1 제품 구현이나 Git 쓰기는 범위에 포함하지 않는다. P0 최초 산출물 검증은 미실행, 수정·재검증 0/2다.

- 보호: `Temp/symbol-loop-migration/p0/preflight.json`, `protected-before.json`에 Git `-z` 목록과 686개 파일 해시를 기록했다. 동적 폰트 원본 bytes 사본은 같은 경로의 `Pretendard-Effects SDF.asset.before`에 보관했고 시작 SHA256과 일치한다.
- 연결 문제: 로그의 Pipeline 시작 기록과 실제 Unity 창은 정상이었지만 descriptor가 없었다. 메뉴 Stop/Start 후 샌드박스 CLI 호출에서는 다시 파일이 사라졌다. 한 번 더 재시작하고 정상 사용자 환경에서만 CLI를 호출하자 descriptor 유지와 `editor_status=ready`가 확인됐다. 샌드박스 프로세스 감지 제한이 유력한 원인이며 삭제 내부 분기는 직접 추적하지 않았다. 이후 Unity CLI는 `require_escalated`로 실행한다.
- 사용자 지시 반영: 최초 진단 과정에서 시작한 Orca 연결은 종료됐다. “오르카는 실행하지말고 해결해봐” 이후에는 Orca를 실행하지 않았다. Codex Windows 도구로 기존 Unity의 Pipeline 메뉴를 제어하여 연결을 복구했다. Editor 재실행·중복 batchmode·패키지/설정 변경은 하지 않았다.
- 실행 전 상태: 씬 dirty=false, Play stopped, `test_status=no_tests`. 현재 실제 목록 EditMode317/PlayMode228.
- EditMode baseline: **PASS 317/317**, 실패/skip/inconclusive 0, 47.35초. 증거: `Temp/symbol-loop-migration/p0/baseline-edit.json`.
- PlayMode baseline: 실행 중. 신규 테스트/캡처/fixture는 아직 작성하지 않았다.
