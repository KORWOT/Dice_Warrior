# COMBAT_LAYOUT_PLAN

상태: IMPLEMENT / COMPLETE. 사용자 승인 2026-09-08: 전투 화면을 첨부 참고처럼 구조·스타일 변경, 새 그림/아이콘 등록 없이 진행.
대상 D:/UnityProject/Dice_Warrior, main433edddc872433ead6db52a872ea0fcac659d92e, Unity6000.6.0f1/7801. 기존 승인 결과를 유지하는 actual checkout. baseline 상태/hash 저장. 커밋/staging/branch/패키지 변경 없음. 이전 UI/SCENE 완료 계약·예산은 재개방하지 않는다.

## 시각 계약
참고2 중앙 게임 영역(외곽 주석 제외)을 기준으로 검정/먹색, 얇은 테두리, 붉은 HP/공격, 푸른 보호/주사위, 큰 중앙 연출 공간, 낮은 주사위, 하단 세로형 행동 카드 기본3장 가로열을 구현한다. 참고1은 톤 참고이며 탐험 지도 변경은 제외한다. 새 이미지/아이콘/font/매핑 등록 없음. 기존 공개 이미지 slot을 유지하며 없을 때 단순 UI 도형 실루엣/음영으로 공간을 표시한다.
위→아래: compact Battle Header(menu/누적턴), 적 이름/HP, flexible Arena(적 예고 우상단, player HP/shield 좌하단), Dice+hand/fate/reroll, Actions 가로열/roll버튼, Notice. 카드3/주사위6은720x1280/1600에서 동시에 보인다. 설정된 카드가4~5장이면 버리지 않고 가로 스크롤로 모두 접근한다.
실제 데이터만: stage=누적 events+1, turn은 RUN TURN(전투별 턴 없음); HP/shield/IntentAmount/hand/fatePower/rerolls. 출혈/독/새 자원/기술/지도/보상 변경 없음.

## API/책임 동결
Main: RunScreenView.OnBind의 기존 HUD 부분을 protected virtual BindHUD(TData data)로 추출; default동작동일. CombatUI만 override.
RunUIData CombatUIData 추가 display fields: string battleLabel,turnLabel,enemyName,enemyHealth,enemyShield,intentLabel,intentValue,playerName,playerHealth,playerShield,playerAttributes,handLabel,fatePowerLabel,rerollLabel; float enemyHealth01,playerHealth01. Controller는 기존 계산값으로 채우며 HUD/Action offeredID/기존 effect문자열 유지.
CombatUI public fields: 기존 artworkRoot/artwork/artworkFallback/rollChoices/actionChoices 및 layout/group; 추가 RectTransform arena; Text turn,enemyName,enemyHealth,enemyShield,intentValue,playerName,playerShield,playerAttributes,fatePower,rerolls; Image enemyHealthFill,playerHealthFill; CommonButtonView menuButton,diePrefab; ScrollRect actionScroll; GridLayoutGroup actionGrid; CombatStageGraphic stageGraphic; int layoutVersion. layout.header는battleLabel, stats는실제playerHealth, situation은실제Intent label, fate는handLabel, notice는실제안내, gear는비활성. layout.body=actionChoices(content), layout.scroll=actionScroll, layout.footer=menuButton parent.
CombatUI BindHUD는 표시값/HPfillanchor/menuButton 소유 callback/key등록/주사위호출. BindScreen은 공개 artwork 및 primitive fallback, 실제roll/actions전부생성. LateUpdate는 viewport너비 기준3개가 보이도록 GridLayoutGroup.cellSize.x=(width-2spacing)/3 (카드수1~2는count)만 조정. 닫을 때 소유 menuButton unbind. Screen skeleton은 runtime생성하지 않는다.
Widgets: Choice(parent,choice,CommonButtonView prefab=null,ButtonAppearance appearance=null); ShowDice(values,clicked=null,CommonButtonView prefab=null,ButtonAppearance appearance=null); ActionCard(parent,card,ButtonAppearance appearance=null). 기존 호출기본값동일. Combat은 own diePrefab/blue appearance, 공격red/defenseblue frame색을 사용; gradeLabel은기존등급색그대로. 모든게임key/콜백/이미지원본참조보존.
B: 새 Editor/CombatLayoutAuthoring.cs public const CombatPath/ActionPath/DiePath, public static string Apply(). stop/dirty/prefabstageguard. 기존 CombatUI component/GUID 보존하고 자식배치만 재작성, ActionCardView는기존원본을portrait로재배치(기존CommonButton nested/variant상속/fontStyle전파 보존), CombatDie.prefab는CommonButton variant로생성. 기존UIRoot/config/catalog/다른화면수정없음. layoutVersion=1을마지막Combatprefab저장에찍고재실행시변경없음. prefab API만.
C: 새 Combat/Presentation/CombatStageGraphic.cs : UnityEngine.UI.Graphic. 자체mesh만으로 어두운바닥/음영과 익명 적·플레이어 실루엣 표시. 외부sprite/texture/게임규칙없음, raycastTarget=false, public bool showFigures=true (공개artwork있으면false). 조용한darkcharcoal/blue/red accent, 정보/버튼보다낮은명도. Editor에서prefab직렬화 가능. 새 Tests/PlayMode/CombatLayoutTests.cs는 아래통합AC검사. Main만Unity실행.

## 쓰기 경계
변경 C#: Features/Run/Presentation/{RunScreenView,RunUIData,RunUIController,FateDiceWidgets,CombatUI}.cs; Tests/PlayMode/FateDiceGuiTests.cs (공개artwork typed참조/portrait 카드 비겹침/실제scroll조상 검사만). UiStructureTests.cs는 필요 시전투배치 관련가정만수정허용. 모두 Assets/_Project/ 하위.
신규 C#: Assets/_Project/Features/Run/Editor/CombatLayoutAuthoring.cs; Assets/_Project/Features/Combat/Presentation/CombatStageGraphic.cs; Assets/_Project/Features/Run/Tests/PlayMode/CombatLayoutTests.cs.
정확한 asset허용: Assets/_Project/Features/Run/Prefabs/CombatUI.prefab; Assets/_Project/Features/Combat/Prefabs/ActionCardView.prefab; Assets/_Project/Features/Combat/Prefabs/CombatDie.prefab(신규). 기존GUID/meta 보존, 신규meta는Unity생성. 테스트의 기존 temporary original/catalog probes는finally복원.
Main: 모든CodeMap 1:1 신규/변경 및 직접관계(ActionCardView/RunScreenLayout/UiStructureAuthoring/UiPrototypeAuthoring), INDEX, Docs/Plans/COMBAT_LAYOUT_PLAN.md, Docs/Reports/COMBAT_LAYOUT_REPORT.md. cwd artifacts/combat-layout staging/evidence.

## AC / 순서 / 예산
1. reference 전투구조의missing RED→구현/자산. 테스트는behavior검증이며배치mirror만으로PASS주장하지않는다.
2. 실제 CombatRoll/CombatCards 두 portrait의 HP fill/intent/state값, arena 공간, dice/card동시표시/비겹침/안전영역/48px 입력/실제raycast. 카드원본ID·offeredID·실제effect·중복행동·reroll·보상/저장동일성유지. 5장설정도누락없이접근.
3. full Edit122/Play46 및새전투tests. 실제productionInGame전투캡처두비율/roll/cards검수. 기본사용자저장에쓰기없음; 격리store. 컴파일/런타임오류없음. APK/device NOT_RUN.
4. 최초검토1묶음(A정적규칙/경계 +Main실행/시각/자산/docs), 수정재검증최대2묶음. 동시4석 inclMain, 기존A/B/C3석재사용, 누적unique3, 재귀위임없음. Bauthoring/Cgraphic+tests/Mainfeature+기존test+docs 한작성자경계. Unity직렬화. 기존완료계약수정예산유지.

## 완료 기록
2026-09-08: 최종 Edit122/122, Play51/51 PASS. 실제 두 portrait/두 phase 캡처, 카드5 drag, 저장/원본/자산 경계 검증 완료. 최초 검토1/1, 수정재검증2/2(COMBAT-LAYOUT-01 uint,02 CanvasRenderer) CLOSED. C#51/CodeMap51, 필수 미해결0. 상세 증거와 NOT_RUN은 COMBAT_LAYOUT_REPORT 참조.

