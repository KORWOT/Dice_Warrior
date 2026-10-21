# SCENE_STRUCTURE_REPORT

상태: IMPLEMENT / COMPLETE — 2026-09-08.
대상: D:/UnityProject/Dice_Warrior, Unity6000.6.0f1, main433edddc872433ead6db52a872ea0fcac659d92e. 사용자 요청 "좋아 다음 씬구조 정리"를 구현했다. 커밋/staging/branch 변경 없음.

## 결과와 사용
- `Assets/_Project/Scenes/Title.unity`를 열고 Play → ENTER LOBBY → NEW JOURNEY 또는 CONTINUE. InGame의 MENU 및 결과 화면의 다시 시작은 Lobby로 이동한다.
- **Title**: 필수 참조/설정/UI 초기화가 끝난 뒤 진입 화면. 로그인/서버 로딩은 아직 구현 범위가 아니다.
- **Lobby**: 기존 MenuUI로 trial/grade cap/seed와 새 런/이어하기/손상 저장 보관을 담당한다.
- **InGame**: 기존 탐험→전투/사건→보상→장비→결과 전체 루프. 콘텐츠 단계마다 씬을 다시 만들지 않는다.
- 세 씬에서 직접 Play 가능. InGame은 유효 저장이 있으면 읽어 복귀하고, 없음/손상이면 저장을 덮어쓰지 않고 Lobby로 이동한다.
- 공통 `Run/Prefabs/GameApplication.prefab`가 RunUIController/SceneFlowController와 UIRoot/세션을 지속 소유한다. 씬마다 SceneEntry와 Main Camera/AudioListener만 있으며 앱/UI 중복 생성은 없다.
- 빌드 씬: Title, Lobby, InGame enabled 순서. SampleScene은 disabled, 기존 FateDicePrototype은 보존된 개발/회귀 fixture다. 기존 패키지/설정 SO/게임 규칙/저장 스키마 및 일곱 화면 배치를 변경하지 않았다.

## 구조와 변경
`SceneEntry → GameApplication → SceneFlowController ↔ RunUIController → typed UIData → UIManager → 기존 7개 화면 + TitleUI`.
SceneEntry는 씬 수명, 앱/UI/런은 앱 수명이다. View에는 SceneManager/store/global app 조회가 없다. SceneFlowController가 실제 Single 비동기 로드/진입 수락/전환 입력 잠금을 담당하고, RunUIController가 기존 게임 명령/저장을 맡는다. 새 런 저장 성공 후 InGame으로 들어가며, 메뉴 복귀 전 시간 저장 실패 시 이동을 중단한다.

새 C#7개: Flow/SceneNavigation, GameApplication, SceneEntry, SceneFlowController; Presentation/TitleUI; Editor/SceneStructureAuthoring; Tests/PlayMode/SceneStructureTests. 기존 RunUIController 수정. 새 자산은 3씬 및 2프리팹, 기존 UIRoot registry에는 TitleUI만 추가했다. EditorBuildSettings.scene list를 Editor API로 갱신했다. 생성기 재실행은 기존 자산 bytes를 바꾸지 않는다.

## 실행 검증
증거 루트: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/scene-structure/`.

| 검증 | 실제 결과 / 증거 |
|---|---|
| 초기 계약 RED | 2/2 예상 실패: Title 없음 및 SampleScene-first. red-play.json |
| 전체 EditMode | **122/122 PASS**, 8.93초. final-edit.json |
| 새 제작용 씬 통합 | **9/9 PASS**, 17.43초. production-play.json |
| 전체 PlayMode | **46/46 PASS**(기존37 + 새9), 55.34초. final-play.json |
| Cold Title/Lobby/InGame | Current=null에서 각 씬 Edit open→Play→Stop. 각각 앱1/Canvas1/EventSystem1/Camera1, 종료 뒤 Current=null. InGame missing save→Lobby. cold-Title.json, cold-Lobby.json, cold-InGame.json |
| 실제 포인터 조작 | GraphicRaycaster의 첫 도달 대상 확인 후 pointer down/up/click으로 Title→Lobby→새 런→InGame→Lobby. visual-flow.json |
| 화면 | Title720×1280 및720×1600, Lobby/InGame720×1280 실캡처 확인. title.png, title-1600.png, lobby.png, ingame.png. Title 글자 잘림 없음/버튼 안전영역 내부 |
| 자산 | 각 SceneEntry role/prefab, Camera, 앱 config/catalog/UIRoot, 등록8종, 누락 script0. 생성기 재실행 변경0. asset-verification.json |
| 문서/보호 파일 | 프로젝트 C#48개와 CodeMap48개 대응, 신규 meta/색인, 기존 보호 bytes 검사. document-verification.json / baseline-diff.json |

새 통합 검증은 반복 메뉴/이어하기의 전체 상태(시간만 정규화), 정확한 RNG/카드ID/주사위/재굴림, 로드 시 디스크 bytes 불변, 손상 보존→명시 보관, 중복 로드 잠금, 잘못된 경로 오류 복구, 실제 완주/장비/결과 및 다음 런의 최근 결과 보존을 포함한다. 게임 조작은 모두 격리 LocalRunStore를 사용했다. Cold Play는 기본 저장 없음 조건을 사전 확인하고 게임 시작 없이 수행했으며 전후 기본 저장은 존재하지 않는다. 현재 domain/scene reload 비활성 설정(options3)을 유지한 상태의 실측이다.

## 검토와 수명
- 최초 검토1/1: A의 bootstrap/flow/controller/tests 읽기 검토 + Main의 자산/실행/문서 확인. initial-review.md.
- 수정 묶음1/2: **SCENE-01 해결**. Unity는 prefab 작성용 untitled scene을 둔 상태의 다음 untitled additive scene을 거부했다. 생성기가 임시 씬을 먼저 닫고 각 제작용 씬을 개별 생성하도록 수정했으며 actual authoring/repeat/full tests로 확인했다. authoring.json(실패), authoring-fixed.json(성공).
- 사전 Bootstrap 테스트의 cold SceneEntry 분기 누락은 별도 실제 직접 Play로 확인했다. 남은 필수 Finding0, 추가 검토/수정 묶음 없음. 이전 완료 UI/게임 작업의 예산과 보고서는 재개방하지 않았다.
- 각 변경/새 스크립트 1:1 CodeMap 및 직접 관계/INDEX 동기화. 코드는 실제 프로젝트에 적용되어 있으며 staging 소스와 실행 증거는 위 artifacts에 보존했다.

## 미실행과 후속 경계
이번 씬 구성으로 Android APK를 다시 빌드하거나 실제 기기에서 실행하지 않았다(NOT_RUN). 이전 APK 성공을 이번 씬 구성의 빌드 성공으로 주장하지 않는다. 화면 미술 개선, 서버 로그인, 콘텐츠별 추가 씬, Addressables/로딩 연출은 포함하지 않는다. 마지막 Editor 상태는 stopped/clean Title 씬, runInBackground=false이며 실제 사용자 저장을 생성하지 않았다.
