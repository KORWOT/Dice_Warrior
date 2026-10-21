# FATE_DICE_PROTOTYPE_REPORT

최신 추가 UI 결과: 재사용 원본4개·Inspector 매핑 연결 완료, EditMode122/122·PlayMode21/21 PASS. 상세는 맨 아래 추가 UI 절에 있다. 아래 기존 APK는 추가 UI 이전 빌드다.

기준일: 2026-09-08. **COMPLETE — 로컬 프로토타입과 필수 AC01~09 완료. Android 개발 APK 빌드 성공, 실기기 NOT_RUN.**

실제 Game View에서 새 런 → 분기 탐험 → 전투·사건·보물·상점·휴식 → 성장/장비/주사위 선택 → 일반 사건 10회 → 대운명 보스 → 결과 → 재시작과 디스크 이어하기를 구현했다. Unity 6000.6.0f1의 연결된 Editor에서 실행했다. Android 개발 APK 빌드는 성공했다. 연결된 Android 기기는 없어 설치·터치·성능·중단/복귀 실기기 검증은 NOT_RUN이다.

사용자 Goal(c6fb0644), AGENTS v0.5, Fate_Dice_GDD_v0.1을 기준으로 했다. 2026-09-08 사용자의 직접 승인으로 이전 환경 권한 차단 F-ENV-01이 해소되었으며, 그 이력과 고정 검토 예산은 [PLAN](../Plans/FATE_DICE_PROTOTYPE_PLAN.md)에 유지했다. 새 계약·수정 예산·별도 운영 문서를 추가하지 않았다.

## 실행과 조작

- 프로젝트: `D:/UnityProject/Dice_Warrior`
- 시작 Scene: `Assets/_Project/Scenes/FateDicePrototype.unity`
- Inspector 조정 자산: `Assets/_Project/Features/Fate/Configs/DefaultFateDice.asset`
- Scene의 `Fate Dice` 객체에 `FateDiceScreen`이 있으며 `config`와 내장 `LegacyRuntime.ttf` 폰트가 직접 연결되어 있다. Scene을 열고 Play하면 된다. 기존 SampleScene과 빌드 Scene 목록은 변경하지 않았다.

1. 메뉴에서 Fireball 또는 Bastion 시험 와일드카드 하나를 선택한다. 둘 다 Rare이며 시작 행동 3개와 함께 실제 소유 행동이 된다. 탐험 등급 상한과 0이 아닌 Seed를 정하고 `NEW JOURNEY`를 누른다. 기본 Seed는 33이다.
2. 지도에서 연결선과 다음 노드 타입을 보고 경로를 선택한다. 지도 영역은 세로 스크롤된다. `ROLL SIX DICE` 후 카드 3장 중 하나를 고른다. 선택 전에는 타입·등급만 보이며 첫 슬롯은 선택한 경로 타입을 보장한다. 중복 타입은 정상이다.
3. 전투는 적의 이번 의도와 수치를 보고 굴린 다음, 제시된 행동 중 하나를 선택한다. 모든 제시 행동을 즉시 사용할 수 있다. 카드 밖 기본 공격, MP, 쿨다운, 공격/방어 보장 슬롯은 없다. 적이 죽었으면 반격하지 않는다.
4. Event의 대가와 보상, Treasure의 장비, Shop의 가격·구매 결과, Rest의 회복 또는 훈련을 실제로 선택한다. `CLAIM AND CONTINUE` 후 장비 수락/포기와 획득 주사위 교체/포기까지 끝나야 사건이 완료된다. 상점은 보상을 수령하면 돌아와 다른 상품을 살 수 있고 `LEAVE SHOP`으로 종료한다.
5. 재굴림은 Event·상점·훈련 보상으로 먼저 획득한다. 카드 선택 단계에서 잔여 횟수가 있으면 주사위 하나를 누른다. 비용 1, 해당 주사위 한 개의 새 눈·새 카드·RNG·잔여 횟수가 함께 저장된다. 교체 주사위를 얻었을 때는 6개 중 교체할 위치를 고르거나 포기한다.
6. 일반 사건 완료당 진행도가 정확히 1 증가한다. 10번째 사건의 보상과 선택을 끝내면 다음 경로가 모두 Boss가 된다. 보스 승리 또는 HP 0은 결과 화면으로 이어진다. 결과 화면에는 사건 수·전투 턴·선택 등급·시간·Seed가 표시된다. `START ANOTHER JOURNEY`로 메뉴에 돌아온다.
7. `MENU`와 `CONTINUE SAVED JOURNEY`로 재개한다. 새 런 버튼에는 기존 저장을 교체한다는 문구가 표시된다. 새 런은 현재 저장 런을 교체하고 마지막 완료/패배 기록 한 건을 유지한다. 전체 런 이력 보관 기능은 아니다.

화면은 영어 단문과 Unity 내장 폰트를 사용한 임시 uGUI다. 한국어 안내는 이 보고서에 있다. 9:16에서 아래 장비 정보 등은 스크롤로 확인한다. 버튼 중복 입력은 연출 동안 잠그며, 구매 불가 등 개별 비활성 상태는 잠금 해제 후에도 유지된다.

## 저장과 설정 적용

Editor 기본 저장 위치는 `C:/Users/coli4/AppData/LocalLow/DefaultCompany/Dice_Warrior/FateDiceLocal/run.json`이다. Android는 해당 앱의 `Application.persistentDataPath/FateDiceLocal/run.json`을 사용한다. 자동화 검증은 개별 임시/증거 슬롯을 주입하여 기본 사용자 저장 슬롯을 쓰지 않았다.

새 런은 SO를 검증하고 전체 설정을 깊게 복제한다. 안정 ID로 참조하며 SO를 나중에 바꾸어도 진행 중·저장된 런은 원래 스냅샷으로 이어간다. 저장에는 실제 설정, 단계, HP/XP/장비/주사위, 분기 노드, 확정 카드와 콘텐츠, 규칙 RNG, 보상/구매/처리 식별자, 시간, 마지막 결과를 포함한다. 연출과 화면 시간은 규칙 RNG를 소비하지 않는다.

명령은 이전 상태 복제 → 규칙 적용 → 체크포인트 저장 → 화면 상태 확정 순서다. 저장 실패 시 이전 상태와 자원을 유지한다. UTF-8 임시 파일 flush 후 같은 폴더의 파일 교체/이동으로 저장한다. schema·체크섬·설정·상태를 검증한다. 손상·지원하지 않는 형식은 원인을 표시하고 새 런/이어하기로 덮어쓰지 않는다. 사용자가 `PRESERVE BROKEN FILE AND CLEAR SLOT`을 눌러야 원본을 고유 `.bak`으로 옮기고 새 슬롯을 사용할 수 있다. 자동 삭제나 조용한 초기화는 없다. 체크섬은 오류 탐지이며 변조 방지·계정 보안 장치가 아니다.

## 채택한 임시 수치와 조정 위치

아래 위치는 실제 SO의 `data` 하위 필드다. 변경 후 저장하고 **새 런**을 시작해야 규칙 변경이 적용된다. 기존 런은 저장 스냅샷을 유지한다. 잘못된 ID·누락 풀·유효하지 않은 수치는 경로가 포함된 오류가 되며 대체 기본값을 몰래 사용하지 않는다. `PrototypeAuthoring.CreateDefaults`는 최초 자산 생성용이며 기존 SO를 자동 재설정하지 않는다.

| 채택값 / 규칙 | 이유와 다음 조정 방향 | 수정 위치 | 적용 시점 |
|---|---|---|---|
| 캐릭터 Wanderer, HP 90 / Power 12 / Guard 10 / Coins 12 | 한 구간에서 생존·성장·구매를 관찰할 시작점. 실제 플레이 난이도에 따라 조정 | `growth.starting*`, `character*` | 새 런 |
| XP 비용 12/18/26/40/60; 단계별 HP +8/+8/+10/+10/+12, Power +2/+2/+2/+3/+3, Guard +1/+1/+2/+2/+2 | 초반 성장을 빨리 확인. 누적 XP에서 각 비용을 소비하며 최대 HP 증가분만 회복 | `growth.levels` | 새 런 |
| Strike Common: 피해계수 1.1; Guard Common: 보호 1.5; Heavy Uncommon: 피해 1.8; Fireball Rare: 피해 2.3; Bastion Rare: 보호 2.5 | 공격·방어·시험 와일드카드의 차이 확인 | `combat.actions`, `startingActionIds`, `trialActionIds` | 새 런 |
| Road bandit HP26/Power6/Guard5; Stone sentry 36/7/8; Fate keeper 130/13/9 | 일반 적 2종과 보스 1종의 시험 곡선 | `combat.enemies` | 새 런 |
| Attack/Defend/Heavy 계수 1/1.5/1.5, 의도 가중치 적 순서로 55/25/20, 40/35/25, 50/20/30 | 다음 의도를 보고 방어를 선택할 여지 | `combat.enemies[].intents` | 새 런 |
| 피해=Power, 보호=Guard를 기반으로 카드계수×제시등급/원본등급 배율×(1+일치 보정 합); 마지막 AwayFromZero 반올림, 실제 효과 최솟값1 | Guard가 피해 감소/공격력으로 새지 않는 단순한 계산. 최솟값은 수치 조정 가능 | `combat.minimumEffect`, 카드·등급·장비 필드; 계산 규칙 `CombatRules` | 새 런(수치) |
| 보호막은 다음 상대 행동 후 만료. 적이 죽으면 적 행동·새 의도 추첨 없음 | 행동 순서와 방어 유효 시간을 예측 가능하게 유지 | `CombatRules.Resolve` 규칙 | 코드 변경 시 |
| 일반 사건10, 카드3, 분기3, 미리보기 깊이2, 보스1 | 요청된 한 구간 로컬 루프 시작값 | `world.eventsToBoss/offeredCards/branchCount/previewDepth` | 새 런 |
| 타입 Combat/Event/Treasure/Shop/Rest 가중치34/22/17/12/15, 선택 타입 편향3배, 슬롯0 타입 보장 | 선택 경로의 의미를 주면서 다른 선택지 유지 | `fate.nodeWeights`, `selectedTypeBias` | 새 런 |
| Common→Legendary 수치 배율1/1.2/1.5/1.9/2.4; 탐험 상한 보상 배율1.35/1.25/1.15/1.05/1 | 등급 차이 및 낮은 탐험 상한의 Gold/XP 보정 확인 | `fate.gradeMultipliers`, `capRewardMultipliers` | 새 런; 상한 선택은 지도에서 변경 가능 |
| Plain D6 각 면 균등; Ember D6 눈1~6 가중치1/1/1/1/2/3; 재굴림1회 비용1 | 획득 주사위 차이와 자원 선택 검증 | `dice.dice`, `growth.rerollCost` | 새 런 |
| 최초 재굴림 권한/횟수 없음; 획득 자원은 새 런에 이월하지 않음 | 실제 보상으로 기능을 얻는 루프 | `RunSession.New`, 사건/상점 보상 | 새 런/보상 시 |
| 본문25, 제목38, 버튼102, 기준720×1280; 행동0.18초/굴림0.35초 | 세로 터치 배치와 중복 방지 확인 | `presentation` 색상·크기·시간 필드 | 새 런의 표시 스냅샷 |

탐험 등급표와 전투 등급표는 별개다. 행은 운명력이 해당 최소값 이상인 마지막 행을 사용한다. 열 순서는 Common/Uncommon/Rare/Epic/Legendary이며 값은 상대 가중치다.

| 최소 운명력 | 탐험 가중치 | 전투 가중치 |
|---:|---|---|
| 1 | 65 / 25 / 8 / 1.8 / 0.2 | 58 / 31 / 9 / 1.8 / 0.2 |
| 3 | 45 / 32 / 18 / 4 / 1 | 43 / 34 / 18 / 4 / 1 |
| 5 | 25 / 32 / 28 / 12 / 3 | 25 / 32 / 30 / 10 / 3 |
| 7 | 10 / 20 / 35 / 25 / 10 | 12 / 23 / 35 / 22 / 8 |
| 9 | 3 / 12 / 30 / 35 / 20 | 5 / 15 / 35 / 30 / 15 |

수정 위치는 `fate.exploration`/`fate.combat`이다. 탐험에서만 상한 초과 가중치를 선택 상한에 합친다. 카드는 각각 독립적으로 등급을 뽑는다. 행동은 해당 등급의 소유 풀에서 뽑고, 비어 있으면 전체 소유 행동을 균등하게 뽑아 숫자만 등급 배율 비율로 보정한다. 원본 카드·태그·추첨 확률은 변조하지 않는다.

패의 강약 우선순위와 운명력은 별개 필드다. 다음 순위는 시험용이며 희소도 순위와 같지 않다. `dice.hands`에서 변경한다.

| 약→강 우선순위 | 운명력 | 균등 6D6에서 최고 패가 되는 경우 수 |
|---|---:|---:|
| Pair | 1 | 7,200 |
| TwoPairs | 2 | 16,200 |
| Triple | 3 | 7,200 |
| Straight | 4 | 3,600 |
| FullHouse | 5 | 7,500 |
| ThreePairs | 5 | 1,800 |
| FourKind | 6 | 2,250 |
| FullStraight | 7 | 720 |
| FiveKind | 8 | 180 |
| SixKind | 9 | 6 |

합계 46,656. 숫자는 독립 오라클로 모든 6D6 결과를 열거한 최고 패 분류이며, 여러 패가 동시에 성립하는 개별 지지 집합의 크기와 다르다. 운명력 범위는 1~9다.

### 사건·보상·가격

실제 `world.events`에는 5타입×5등급 25개 콘텐츠가 있다. 아래는 생성된 기본값의 근거이며 런타임은 SO에 저장된 숫자를 읽는다. 등급 배율로 환산한 정수 보상이 자산에 들어 있다. `world.events[].reward`를 직접 조정하면 된다. 탐험 상한 보정은 이후 Gold/XP에만 적용한다.

| 타입 / 상품 | 채택 보상·가격 | 이유 / 조정 위치 |
|---|---|---|
| Combat | Gold 5×등급배율, XP 8×등급배율; Rare 이상 Stone sentry, 그 아래 Road bandit | 난이도와 성장 동시 확인 / 사건의 `enemyId`, `reward` |
| Event | HP -4, Gold 5×배율, XP 8×배율, 재굴림2 | 비용을 치르는 성장/권한 획득 / 해당 사건 보상 |
| Treasure | Gold 12×배율, XP 4×배율, 등급 순번%3 장비; Rare 이상 Ember die | 장비 교체와 주사위 획득 노출 / 해당 사건 보상 |
| Shop | 입장 자동 보상 없음 | 실제 구매 선택 / `world.shop` |
| Rest | HP 24×배율, XP 4×배율; 훈련 선택은 XP12와 재굴림1 | 회복과 성장 선택 / 사건 보상, `world.restTraining` |
| Healing draught | 10 Coins / HP25 | 소액 회복 / `world.shop[potion]` |
| Reroll training | 12 Coins / 권한과3회 | 시작 자금으로 재굴림 구매 가능 / `world.shop[reroll]` |
| Ember die | 16 Coins / 한 위치 교체 | 편향 성장 / `world.shop[die]` |
| Ember blade | 18 Coins / Weapon 교체 | 화염 태그 성장 / `world.shop[blade]` |
| Boss | Gold50 / XP30 | 한 구간 완료 보상 / `world.bossReward` |

장비는 Weapon/Armor/Accessory 각각 한 칸이다. 장비 교체 시 파생 스탯을 다시 계산하며 기존 장비 보정이 기본 스탯에 누적되지 않는다. 포기는 현재 장비를 유지한다. `growth.equipment`에서 수치·슬롯·태그·modifier를 조정한다.

- Ember blade: Power+3, 장비 태그 fire. **ANY / Card / fire / Damage +25%**.
- Guard plate: Guard+3, 장비 태그 steel. **ALL / Card / defense / Block +20%**.
- Traveler charm: 최대HP+5, 장비 태그 luck. **NONE / Card / magic / Damage +10%**, **ALL / Equipment / luck / Block +10%**.

ANY는 하나 이상, ALL은 요구 태그 모두, NONE은 요구 태그가 하나도 없어야 한다. Card 태그와 장착 Equipment 태그 집합은 분리된다. 장비 태그를 카드에 전파하거나 추첨에 편향을 주지 않는다. Damage 보정과 Block 보정은 지정된 값에만 작용한다.

## 수용 기준과 실제 실행 증거

증거 루트: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-prototype/`. 아래 파일은 그 디렉터리 기준이다. 모든 통과 수는 실제 테스트 실행 결과이며 0건 발견이나 구조 확인을 실행 PASS로 바꾸지 않았다.

| 기준 | 결과와 검증 내용 | 증거 |
|---|---|---|
| AC01 컴파일·자산 | PASS. 현재 코드 EditMode 98/98, 실제 SO 유효·25사건·5행동. 전용 Scene config/font 직접 연결 | `m5-final-edit-check.json`, `m5-final-asset-inspection.json`, `m5-final-live-inspection.json` |
| AC02 정상 완주 | PASS. 원본 SO/Seed33, 실제 버튼·포인터로 일반10사건→보스 승리→결과→새 런, 마지막 결과 보존 | `m5-gui-acceptance-check.json`의 RealButtonsCompleteSectionAndRestart |
| AC03 사건·전투·성장 | PASS. 전투 의도/생존 순서·패배·모든 행동 사용, 5사건 실제 효과, XP/장비/태그, 획득 재굴림/주사위 교체 | EditMode RunTests34, GUI의 시드 고정 사건·패배·재굴림 검사 |
| AC04 전수·확률·진행 | PASS. 독립 46,656 패 열거, 확률/가중치 경계, 등급 독립·타입 보장·빈 행동 풀 보정, 보상 중복·사건+1·다음 보스 경로 | EditMode RuleTests32 + RunTests34 |
| AC05 저장 | PASS. SaveTests32, 노드·카드·전투·보상·성장·재굴림의 디스크 왕복/동일 RNG, 실패 원자성/손상 보존, 실제 Scene 재로드 이어하기 | `m5-final-edit-check.json`, `m5-gui-acceptance-check.json` |
| AC06 실제 SO 편집 | PASS. 적HP26→52, Strike 실제피해13→26, 등급표→전부Legendary, 임계값10→1. 새 런 변화/기존 저장 원상태·RNG 유지/자산 복원 모두 참 | `m5-so-edit-evidence.json`, `so-verification-run.json` |
| AC07 실제 GUI·세로비 | PASS. 720×1280/720×1600, 실제 SafeArea 경계·텍스트·48이상 버튼·스크롤 도달·레이캐스트·중복 잠금·Seed 입력 검사, 아래 최종 캡처 육안 확인 | GUI TwoPortraitRatios/SeedField 등, `m5-final-map-720x1280.png`, `m5-final-map-720x1600.png`, `m5-final-fates-720x1280.png`, `m5-final-combat-720x1280.png` |
| AC08 실행 오류·참조 | PASS. 최종 실제 Scene 전체 MissingScript0, config/font 참조 확인. Console cursor1319 이후 최종 지도→탐험→전투 실행 오류0, dropped=false | `m5-final-live-inspection.json`, `m5-final-console-before.json`, `m5-final-console-after.json` |
| AC09 문서·관계 | PASS. 신규 C#16개↔CodeMap16개 일치, 누락0/고아0. 직접 호출/상태 수명 갱신, 한국어 INDEX16행, 기존 PLAN/이 REPORT 사용 | `Docs/CodeMap/INDEX.md`와 대응 요약 |

EditMode 98개는 RuleTests32 + RunTests34 + SaveTests32다. GUI 최초 최종 수용 실행은 **8/8 PASS**(`m5-gui-acceptance-check.json`)다. 빌드 뒤 Android 활성 대상에서는 GUI 추가 실행이 0건으로 완료되어 두 시도 모두 NOT_RUN으로 보존했다(`m5-post-android-gui-check.json`, `m5-post-android-gui-retry-check.json`). 이름 필터 제거로 해결되지 않았으며 목록 조회는 8개를 발견했다. 작업 전 StandaloneWindows64 대상으로 복귀한 뒤 실제 **8/8 PASS, 29.44초**를 확인했다(`m5-final-gui-check.json`). Android 대상에서 0건을 반환한 도구 내부 원인은 확정하지 않았다.

GUI 검증은 실제 Scene의 버튼을 화면 안으로 스크롤하고 GraphicRaycaster hit 확인 후 pointerDown/up/click을 전달한다. 정상 완주 시험은 원본 설정을 사용한다. 5유형을 빠짐없이 거치는 검사와 확정 패배 검사는 명시적인 시드/설정 스냅샷 fixture를 사용한다. 둘을 자연 분포 플레이나 Android 실기기 터치와 동일시하지 않는다. SafeArea 검사는 Editor가 반환한 전체 직사각형 영역에서 수행했으며 실제 노치 기기 검증은 NOT_RUN이다. 최종 수동 캡처의 버튼 호출은 테스트 8개의 포인터 검증을 대체하지 않는다.

최종 캡처 중 비활성 Game View가 오래된 프레임을 반환하여 Game View를 표시하고 검증 Play 세션에서만 `Application.runInBackground=true`로 실행했다. 실제 화면/상태 일치 확인 후 false로 되돌리고 Play를 종료했다. 소스와 PlayerSettings의 백그라운드 실행 값을 변경하지 않았다.

### 관찰한 TDD 실패와 해결된 Finding

규칙 stub31건 RED→31 PASS, 성장/탐험 stub14건 RED→PASS, 저장 stub26건 RED→실제 구현/회귀 PASS, 디스크 GUI 미연결 RED→연결 후 GUI PASS를 실행 증거로 남겼다. 테스트 파일 자체의 컴파일 준비 오류는 실행된 기능 RED라고 주장하지 않는다.

| Finding | 실제 문제와 수정 | 원래 단계 / 수정 묶음 |
|---|---|---|
| F-M1-01 | 고정 높이 LayoutElement의 flexibleHeight 기본값으로 주사위 행 확장. 0 명시 후 실제92px 확인; 초기 운명력1~9 정렬 | M1 / 1 |
| F-M1-02 | 재귀 노드 DTO의 Unity 직렬화 깊이 경고. childIds 기반 평탄 노드 그래프 | M1 / 2 |
| F-M1-03 | 큰 타입 가중치×편향의 float overflow. double 곱과 공통 정규화, 관찰 RED→PASS | M1 / 2 |
| F-M3-01 | Common 상한 훈련 표시XP12와 실제XP16 불일치. UI/명령의 TrainingReward 계산 공유, RED→PASS | M3 / 1 |
| F-M5-01 | JsonUtility가 inline null DTO를 빈 객체로 복원하여 정상 Map 저장 Load11건 실패. 단계와 필드의 의미상 부재 검증, 오염/파일 교체 실패/재굴림 회귀 추가 후 Save32 PASS | M5 / 1 |

| 단계 | 최초 검토 | 수정·재검증 누적 | 상태 |
|---|---:|---:|---|
| M1 | 1/1 | 2/2 | 완료, 추가 수정 여유 없음, 필수 Finding 해결 |
| M2 | 1/1 | 0/2 | 완료 |
| M3 | 1/1 | 1/2 | 완료 |
| M4 | 1/1 | 0/2 | 완료 |
| M5 | 1/1 | 1/2 | 완료 |

총 수정 묶음4/10. 단계·세션·보조별로 예산을 초기화하지 않았다. 보조3/3, 재귀 위임 없음. 마무리 문서·지정 수용 실행은 새 결함 수정 묶음이 아니다. 미해결 필수 코드 Finding은 없다.

## Android 빌드와 환경 변경

기존 Android 모듈/SDK/NDK r27c/JDK17.0.18만 사용했다. 외부 설치·계정 로그인·서명키 변경은 하지 않았다. SDK 기기 목록은 0개다. CLI build에 Android, Development/DetailedBuildReport와 프로토타입 Scene을 명시했다.

- 결과: **Succeeded**, 오류0, 경고984, 약347.64초. `m5-android-dispatch.json`, `m5-android-status.json`.
- APK: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-prototype/Android/FateDiceDevelopment.apk`
- 실제 APK 크기: **71,356,267 bytes (약68.05 MiB)**. BuildReport의 `totalSizeBytes=1,445,378,520`는 보고서 파일 총계이며 APK 크기로 표시하지 않았다.
- APK SHA256: `069C96BB4EFD79FEF23CEDE7E0155ABCE0BA3DAE12E1C8A24310C66BCFCBF1B7`.
- 패키지 ID는 기존 템플릿 `com.UnityTechnologies.com.unity.template.urpblank`, 개발용 서명이다. 배포용 ID/키/성능 최적화는 후속 범위다.
- 빌드가 Editor의 activeBuildTarget을 Android로 바꾸었고, APK 생성 후 CLI로 작업 전 StandaloneWindows64 대상으로 복귀했다(`m5-restore-editor-target-status.json`: completed/success=true). 기존 활성 Scene 목록에는 SampleScene만 남아 있다. 이후 Android 빌드도 대상과 프로토타입 Scene을 명시해야 한다.

| 빌드 경고 출처 | 원본 건수 | 의미 |
|---|---:|---|
| AI Inference/Sentis 셰이더 | 978 | 기존 패키지 shader/compute 변형의 컴파일 경고. 반복 변형 포함 |
| Pipeline 런타임 설정 없음 | 1 | Player 빌드에서 Pipeline 원격 제어가 비활성. Editor CLI 연결은 정상 |
| Pipeline IL2CPP 대형 메서드 | 2 | 생성 C++ 파일 분리 안내 |
| Core DebugOccluder/DebugOcclusionTest | 2 | 일치하는 RenderPipeline이 없는 디버그 셰이더 제외 |
| Diagnostics debug symbols | 1 | 개발 크래시 스택 심볼 설정 안내 |

전체984개 경고와 buildSteps Warning984개가 일치한다. `Assets/_Project` 경로·해당16개 소스 파일명·C# 경고코드는 발견되지 않았다. 경고를 제거하기 위한 무관한 패키지/프로젝트 설정 변경은 하지 않았다. APK 빌드 성공은 실기기 실행 성공을 뜻하지 않는다. **Android 설치·실제 입력·노치·성능·백그라운드 복귀·Player CLI 연결은 NOT_RUN**이다.

빌드 전후 보호 파일 7개 해시를 비교했다(`pre-android-protected-hashes.json`, `post-android-protected-hashes.json`). manifest/lock/QualitySettings/EditorBuildSettings/AGENTS/GDD 6개는 동일하다. ProjectSettings.asset은 Input System의 프로젝트 공용 InputActionAsset(GUID `052faaac586de48259a63d0c4782560b`) 사전 로드 항목 한 개만 추가되었다. 그 항목만 제거한 **검증용 복사본**(`settings-without-build-preload.txt`) SHA256이 빌드 전 해시와 정확히 일치한다. 원본을 텍스트 편집하거나 기존 사용자 설정을 되돌리지 않았다. 근거는 설치된 `com.unity.inputsystem`의 `ProjectWideActionsBuildProvider.OnPreprocessBuild`와 `BuildProviderHelpers.PreProcessSinglePreloadedAsset`이다.

최종 StandaloneWindows64 복귀와 GUI 실행 뒤 다시 계산한 `final-protected-hashes.json`에서는 **7개 모두 빌드 전 해시와 동일**하다. Input System의 사전 로드 항목도 환경에서 자동 정리되어 원래 ProjectSettings 바이트로 돌아왔다. 따라서 앞 문단의 Input System 차이는 빌드 직후 관찰 이력이며 최종 잔여 변경이 아니다. 프로젝트 전체 불변을 뜻하지는 않으며, 아래 App UI 항목 및 해시 대상 밖 5개 파일은 최종 Git 차이에 남아 있다.

게임 작업 중 별도로 확인된 `EditorBuildSettings.asset`의 `com.unity.dt.app-ui` 등록은 기존 AI Inference의 `Editor/Visualizer/App UI Settings.asset` 참조다. 설치된 `com.unity.dt.app-ui/Runtime/Core/AppUIManager.cs`의 `ApplySettings`가 자동 등록하는 항목으로 확인했다. 기존 Scene 목록을 추가/변경한 결과가 아니다. 이 두 자동 환경 변경은 숨기거나 사용자 변경과 함께 reset하지 않았다.


최종 Git 확인에서는 위 7개 해시 대상 밖의 다음 5개 기존 파일도 빌드 시작 시각(06:32:54~55)에 저장된 것을 확인했다. `final-environment-changes.diff`에 전체 차이를 보존했다. `DefaultVolumeProfile.asset`의 Bloom filter 필드, `PC_RPAsset.asset`의 프리필터/새 직렬화 필드, `UniversalRenderPipelineGlobalSettings.asset`의 런타임 설정 목록, `GraphicsSettings.asset`의 새 직렬화 필드가 추가·갱신되었다. `UnityConnectSettings.asset`은 `m_Enabled`가 0→1로 저장되었다. GraphicsSettings의 새 필드와 마지막 UnityConnect 항목의 내부 호출 원인은 확정하지 않았으며 렌더링·서비스 설정이 전부 불변이라고 주장하지 않는다. 별도 로그인·계정 연결·서비스 활성화 명령은 실행하지 않았다. 기존 사용자 데이터와 GUID를 유지하고 일괄 복원하지 않았다. 빌드 뒤 실제 Scene 캡처와 테스트 증거를 따로 확인했다.


설치 패키지의 직접 근거: URP `Editor/BuildProcessors/URPPreprocessBuild.cs:29,47` → Core `Editor/Volume/VolumeProfileUtils.cs:220-236,267`이 누락 override를 추가하고 프로필을 Dirty로 만든다. 실제 빌드 로그에도 Default Volume Profile 변경 메시지가 있다. URP `Editor/ShaderBuildPreprocessor.cs:597-601` → `Runtime/Data/UniversalRenderPipelineAssetPrefiltering.cs:321-371`은 PC_RPAsset의 프리필터를 계산해 저장한다. Core `Runtime/RenderPipeline/RenderPipelineGraphicsSettingsContainer.cs:38-43`은 빌드 직렬화 때 `m_RuntimeSettings`를 채운다. URP 버전 경로는 `com.unity.render-pipelines.universal@8457e85b8184`, Core는 `com.unity.render-pipelines.core@2d66c71e606e`다. 확인되지 않은 두 설정 파일의 원인을 이 코드로 대신 설명하지 않았다.

## 변경 범위와 후속 범위

신규 구현은 `Assets/_Project` 아래 C#16개, asmdef4개, Scene1개, SO1개 및 Unity 생성 .meta다. CodeMap16개와 INDEX, 기존 PLAN/REPORT를 갱신했다. 직접 관계의 자세한 내용은 [CodeMap 색인](../CodeMap/INDEX.md)에 있다.

Git 기준은 main, HEAD `433edddc872433ead6db52a872ea0fcac659d92e`다. 기존 HubForceResolve 삭제, URP.png 수정, Packages 변경, ProjectSettings/QualitySettings 변경 및 `.agents`, AGENTS, Docs, ProjectSettings/Packages의 기존 사용자 작업을 보존했다. AGENTS/GDD는 수정하지 않았다. commit·staging·push·merge·reset·stash는 실행하지 않았다.

임시 수치는 최종 밸런스가 아니다. 실제 재미/다양한 시드 난이도·장기 진행·아트/음향·접근성·현지화·실기기 성능은 후속 플레이테스트가 필요하다. Firebase·영구 계정·안전한 서버 권위·리더보드·출시 보안·배포 설정은 구현하지 않았다. 현재 산출물은 로컬 한 구간 프로토타입과 그 실행 증거다.
## 추가 UI 완료 — 재사용 원본과 이미지 매핑

2026-09-08 추가 요구(f91fba13)와 직접 승인에 따른 구현이다. 기존 uGUI를 유지했고, 필수 확인 1~9를 아래 실행 증거로 확인했다. 앞의 Android APK는 **이 추가 UI 이전 빌드**이며 새 UI가 포함된 APK/실기기 실행은 NOT_RUN이다.

| 실제 원본 | 실제 사용 화면 |
|---|---|
| Assets/_Project/Shared/UI/Prefabs/CommonButtonView.prefab | 메뉴, 선택/구매/이동 버튼, 주사위·등급 입력 및 기능 View 공통 프레임 |
| Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab | 탐험의 선택 가능한 루트, 공개된 미래 노드, 선택 강조와 도달 불가 fade |
| Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab | 전투에서 제시된 행동카드 3장 |
| Assets/_Project/Features/Fate/Prefabs/FateCardView.prefab | 선택 전 공개 유형·등급의 운명카드 3장 |

세 기능 원본은 공통 버튼의 구조 Variant다. Scene의 FateDiceScreen.uiPrefabs/visuals에 직접 연결했고 화면이 실제 Instantiate한다. 스킬/사건마다 원본을 복제하지 않는다.

표시 설정은 **Assets/_Project/Features/Run/Configs/DefaultFateDiceVisuals.asset**의 Inspector에서 편집한다.

| 필드 | 연결 기준과 용도 |
|---|---|
| actions[].contentId / visual.icon / visual.artwork | 기존 원본 행동 ID(strike 등)의 작은 아이콘/카드 그림 |
| events[].contentId / visual.icon / visual.artwork | 실제 사건 ID의 공개 이후 그림 |
| nodes[].type / visual | NodeType별 지도 공개 아이콘 |
| fates[].type / visual | NodeType별 선택 전 공개 운명 그림. 사건 ID를 받지 않는다 |
| grades[].grade / border / badge | 현재 제시 등급의 테두리·배지 |
| buttons[].purpose / icon / normal / selected / pressed / disabled | 공통 버튼 용도별 외형·선택/눌림/비활성 색 |
| defaultAction / defaultEvent / visual.fallbackGlyph / tint | 선택 이미지 누락 시 기본 표시 |
| nodeFadeSeconds | 게임 RNG와 무관한 표시 페이드 시간 |

등급 색상은 기존 DefaultFateDice.asset의 data.presentation.gradeColors를 재사용한다. 별도 색상표를 중복 저장하지 않았다. 잘못된/중복 ID는 오류이며 이미지 누락으로 숨기지 않는다. 유효 콘텐츠의 미등록 이미지와 동일 Sprite 공유는 허용한다.

이미지가 없으면 X/?/$/+/Z/* 같은 공개 유형 문자, 행동 이름 첫 글자, 텍스트와 색으로 표시한다. 카드 큰 그림은 artwork→icon→문자, 노드는 icon→artwork→문자 순서다. Image.preserveAspect와 한정된 영역을 사용해 글자·효과·클릭 영역을 침범하지 않는다.

이미지 필드를 바꾸고 해당 화면을 다시 열거나 UI를 재생성하면 반영된다. 실행 중 자동 핫리로드는 요구하지 않는다. 기존 콘텐츠 외형 교체에는 C#·Scene 인스턴스별 수정·원본 복제·게임 규칙 변경이 필요 없다. 새로운 스킬 효과나 사건 메커니즘을 추가하는 작업은 별도의 게임 정의/규칙/검증이 필요하다.

| 필수 확인 | 실제 결과와 근거 |
|---|---|
| 1. 같은 노드 원본, 다른 유형/선택 상태 | PASS. 실제 합류 fixture의 Rest/Combat/Shop/Treasure/Event, 선택 후 현재 경로·미래 경로·비활성 구분 |
| 2. 같은 행동 원본, 다른 스킬/중복 카드 | PASS. 실제 원본으로 strike Common/Epic + guard Rare 표시, 개별 제시 ID 선택과 효과 비교 |
| 3. 이미지 매핑만 편집 | PASS. catalog 자산을 실제 저장하고 재진입, null→기존 프로젝트 이미지 적용. C#/규칙 변경 없음 |
| 4. null 이미지 대체 표시와 선택 | PASS. 실제 운명/행동 원본의 문자 대체와 포인터 입력, 재사용 View 단위 검사 |
| 5. 효과·대상·RNG·진척 불변 | PASS. 동일 런 복제의 ChooseNode/Roll/ChooseFate/Roll/ChooseAction 결과와 전체 JSON 비교(경과 시간만 제외) |
| 6. 비공개 사건 그림 | PASS. 선택 전 전용 Sprite·사건 이름 없음, 선택 후 공개 영역에는 사건 Sprite 표시 |
| 7. 재Bind 정리 | PASS. own listener만 해제, 외부 listener 보존, 기존 ID/그림/태그/강조/입력/alpha 초기화, 지연 fade 취소 |
| 8. 원본 편집 전파 | PASS. Common prefab의 fontStyle을 Bold로 저장한 뒤 실제 메뉴/노드/운명/행동 화면에 반영, 원본 복원 |
| 9. 경로·합류·fade·저장 복귀 | PASS. A→C,D / B→C,E에서 B/E만 숨김, 두 C 표시와 ID 유지, 이력 디스크 복원, 이후 C/D 선택 |

실행 증거 폴더: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-ui.

- EditMode **122/122 PASS**, 실패/건너뜀0, 7.25초: initial-implementation-edit.json. Rule32 + Run39 + Save32 + VisualCatalog19.
- PlayMode **21/21 PASS**, 실패/건너뜀0, 37.55초: final-play.json. 기존 GUI8 + 신규 GUI6 + 재사용 View7.
- 초기 UI 실행20/21의 F-UI-01은 테스트가 화면 등록 전 같은 프레임에 클릭한 문제였다. 실제 raycast 기준을 유지하고 2프레임 대기 후 위 결과를 얻었다. 제품 코드는 바꾸지 않았다. 기존 M5 마지막 수정 묶음을 사용했고 누적 예산은 PLAN에 기록했다.
- 실제 720×1280/720×1600 SafeArea·48 이상 버튼·스크롤·레이캐스트 회귀 PASS. initial-menu.png, initial-map-720x1280.png, initial-map-720x1600.png, initial-fates.png, combat-fallback.png, combat-mapped.png를 육안 확인했다. 긴 목록은 기존 ScrollRect로 이동한다.
- 기존 URP.png에서 다른 비율의 Sprite 2개를 UiMappingProbe.asset으로 만들었다. 원본 이미지/Importer 변경이나 외부 아트 작업은 하지 않았다. 매핑/프리팹 검증 변경은 복원했고 final-assets.json 및 capture-mapping-restored.json이 이를 확인한다.
- Scene/원본 MissingScript0, 직접 참조 유효, Scene dirty=false/Play 종료, background 설정 복원. 초기 준비 단계 마지막 오류 cursor1361 이후 새 오류0, dropped=false: final-console-since-preparation.json. 초기 타입 준비/잘못된 명령 오류 이력은 final-console.json에 보존했다.
- C#24↔CodeMap24, 누락/고아0. 기존 게임 설정 자산 SHA256 87A6C83DB6B91D3F5327CB1A140CF4F0A30A8ACE50A4AEC38D3A8DD789962CF1 유지. 보호 대상7개도 기존 해시와 동일(final-protected-hashes.json). Git stage/commit은 하지 않았다.

노드 fade가 런 기록을 삭제하지 않도록 nodeHistory를 schema 1의 호환 필드로 추가했다. 합류는 허용하되 실제 cycle·ID 겹침·누락 연결은 저장 시 거부한다. 보관 이전의 기존 보스 경로 변환 규칙은 유지했다. Android 새 UI 빌드, 실기기 터치·노치·성능·앱 중단/복귀는 이번 추가 작업에서 NOT_RUN이다.

