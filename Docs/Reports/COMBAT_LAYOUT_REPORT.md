# COMBAT_LAYOUT_REPORT

상태: IMPLEMENT / COMPLETE · 2026-09-08. 사용자의 전투 화면 참고 배치·스타일 변경 요청을 실제 프로젝트에 반영했다. 기준 main `433edddc872433ead6db52a872ea0fcac659d92e`, Unity6000.6.0f1, 연결 포트7801. 이전 UI/SCENE 완료 계약은 재개방하지 않았다.

## 결과
- 참고2의 중앙 게임 영역을 기준으로 compact 전투 header/누적턴/메뉴, 적 이름/HP, 넓은 arena와 우상단 예고·좌하단 플레이어 상태, 주사위6, 하단 portrait 행동3장 가로열, 안내 순서로 변경했다. 참고 이미지의 외곽 설명 상자는 게임 UI가 아니므로 제외했다.
- 먹색 배경과 얇은 테두리, 붉은 체력·공격 카드, 푸른 보호 카드·주사위를 적용했다. 새 그림/아이콘/font/catalog 등록 없이 기존 공개 artwork slot과 fallback 문자, 중앙의 익명 UI mesh 실루엣을 사용한다. 실제 캐릭터/몬스터 그림·애니메이션과 동일한 시각 결과를 주장하지 않는다.
- HP/shield/IntentAmount/족보/fatePower/earned rerolls는 실제 런 값이다. EVENT=eventsResolved+1, RUN TURN=런 누적 combatTurns+1. 참고 속 stage2-3/출혈/독/자원1/3 등 현재 없는 규칙을 만들지 않았다.
- 기본 카드3장을 한 번에 보이며 설정4~5장도 버리지 않고 가로 스크롤한다. 실제 offered ID/원본 ID/효과/태그·재굴림 비용·명령·저장 경계를 유지했다. 탐험 화면과 씬 이동 구조는 변경하지 않았다.

## 구현과 자산
- C# 기존6개 변경: RunScreenView의 virtual BindHUD 추출; CombatUIData와 Controller의 표시 field 연결; Widgets의 optional prefab/appearance; CombatUI 전용 HUD/grid/menu 정리; FateDiceGuiTests의 공개 그림 typed 참조·portrait 비겹침 두 위치.
- 신규3개: CombatLayoutAuthoring, CombatStageGraphic, CombatLayoutTests.
- 정확한 자산3개: 기존 CombatUI.prefab, ActionCardView.prefab 변경 및 새 CommonButtonView variant CombatDie.prefab. 모두 Unity Editor API로 작성했다. 기존 root/component/GUID/meta와 CommonButtonView 원본 스타일 상속을 보존했다.
- 15개 프로젝트 prefab에 missing scripts0. ActionCardView/CombatDie 모두 Variant, typed menu/die/scroll/renderer 참조 확인. authoring 재실행 no-op과 세 prefab SHA256 불변 확인.
- 변경/신규 코드9개와 직접 관계4개의 CodeMap을 갱신하고 INDEX를51개로 동기화했다. 프로젝트 소유 C#51 ↔ CodeMap51, 누락0.

## 실행 증거
증거 폴더: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/combat-layout`.

| 검증 | 실제 결과 / 근거 |
|---|---|
| 구조 최초 RED | red-play.json:1개 실제 실행, Battle Header 부재로 의도한 실패 |
| 새 전투 통합 | combat-play-green.json:5/5. 두 화면비의 실제 데이터/HPfill, arena·카드3·주사위6 동시표시, 터치/레이캐스트, paid reroll/카드 선택/저장 oracle, 설정5개 전체 접근 |
| 최종 PlayMode | final-play-green.json:51/51 PASS,57.52s. 기존46개 + 신규5개 |
| 최종 EditMode | final-edit-green.json:122/122 PASS,7.31s |
| 실제 화면 | combat-cards-1280.png/1600.png 및 combat-roll-1280.png/1600.png를 열어 검수. visual-checks.json:두 화면비·두 phase의 text overflow0 |
| 실제 가로 drag | drag-result.json:5장, normalizedPosition0→1, 마지막 카드 완전 노출·레이캐스트 도달, 전체 상태(playedSeconds 제외)/저장 bytes 불변 |
| 원본/경계 | boundary-verification.json:기존변경19 + 신규13, 허용 밖0. 기존 meta/GUID, Scenes/UIRoot/다른 화면/CommonButton/config/catalog/Packages/ProjectSettings hash 유지 |
| Editor/컴파일 | final-inspect.json/final-compile.json/final-console.json:정지, clean Title, App static null, background false, 컴파일 오류0; 조회 Console10개 모두 Log |
| 기본 저장 | 테스트와 수동 확인 모두 고유 격리 LocalRunStore 사용. 종료 시 기본 FateDiceLocal/run.json 없음 |

비교 전 before-combat.png는 기존 배치로 촬영했다. 변경 후 카드 캡처는 같은 before-save 전투 상태를 별도 after-save로 복제해 촬영했다. pre-roll/카드5 검증은 별도 격리 fixture이며 기본 밸런스의 추가 주장이 아니다. async CLI 최초 응답의0개는 실행 결과로 사용하지 않고 완료 status의 실제 case 수로 판정했다.

## Finding / 예산
최초 검토1/1(A 정적 + Main 실행/시각/자산/문서), 수정·재검증2/2. 기존 B/C/A의 unique 보조3명, 최대동시4 inclMain, 재귀0, Unity 실행은 Main이 직렬화했다.
- COMBAT-LAYOUT-01 CLOSED (수정1): 새 테스트 해상도 height의 int→uint 컴파일 오류. 호출 양수1280/1600 확인 후 cast만 수정. assertions 유지, 재컴파일 및5/5·full51/122 PASS.
- COMBAT-LAYOUT-02 CLOSED (수정2): 실행 캡처에서 stage mesh가 비어 있었다. live probe에서 active/enabled/showFigures/rect/canvas는 정상이지만 CanvasRenderer가 없음을 확인. Graphic에 RequireComponent 추가, 기존 구조 테스트에 renderer 존재 검증, 기존 prefab 로드 시 Unity가 복구한 renderer를 Editor API로 명시 저장했다. 이후 바닥/실루엣을 실제 캡처로 확인하고 최종51/122 PASS.
- 필수 미해결0. 정적 검사 PASS만으로 화면 검수를 대신하지 않았다.

## 한계 / 인계
- 새 실제 아트/아이콘을 등록하지 않았으므로 중앙은 도형 실루엣, 카드 artwork fallback은 기존 문자다. 이후 그림을 연결할 기존 slot은 유지된다.
- APK 빌드·Android 기기 물리 터치: NOT_RUN(이번 범위 아님). Editor 두 portrait의 uGUI pointer/drag 검증이다.
- 커밋/staging/branch 변경 없음. 기존 사용자 변경을 보존했고 이전 완료 문서를 변경하지 않았다.

화면: [720x1280](<C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/combat-layout/combat-cards-1280.png>) · [720x1600](<C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/combat-layout/combat-cards-1600.png>).

