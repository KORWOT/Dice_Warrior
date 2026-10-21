# KOREAN_UI_PLAN
상태: IMPLEMENT / COMPLETE · 사용자2026-09-08 승인: 플레이 언어 전체 한글화 및 무료 상업 이용 한글 폰트 확보/적용.
대상 D:/UnityProject/Dice_Warrior, main433edddc872433ead6db52a872ea0fcac659d92e, Unity6000.6.0f1/7801. 기존 untracked 구현을 포함한 현재 checkout에서 진행하고 baseline hash/Git 상태를 기록한다. COMBAT/UI/SCENE 완료 계약·예산은 재개방하지 않는다.

## 고정 계약
현재 모든 기본 플레이 흐름 Title/Lobby/Map/ExplorationRoll/Cards/CombatRoll/Cards/Encounter/Shop/Reward/EquipmentChoice/Result, 저장·시드·이동 오류를 한글로 표시한다. 코드 식별자·enum·태그 키·원본/제시 ID·RNG·효과·저장 schema·SO 규칙·기존 snapshot/message 문자열은 바꾸지 않는다. 알려진 기본 영문 콘텐츠/저장 알림을 표시 경계에서 번역하며 사용자 편집 콘텐츠 이름은 그대로 보존한다.
용어: 대운명, 일반/고급/희귀/영웅/전설, 체력, 위력(Power), 방어력(Guard stat), 수호(Shield), 운명력, 재굴림. 영문 브랜드도 운명의 주사위로 표시. 새 언어 선택기/Localization 패키지/게임 규칙/이미지 등록 없음.
폰트: Pretendard v1.3.9의 unmodified static Pretendard-Regular.ttf를 공식 github 배포에서 확보한다. OFL1.1 copyright/LICENSE를 그대로 보존해 StreamingAssets에 포함하고 Docs/ThirdParty/PRETENDARD.md에 URL/version/hash/라이선스 조건을 기록한다. 신규 폰트는 dynamic/import includeFontData로 외부 OS 폰트에 의존하지 않는다.
기존폰트81직렬화참조와 공통원본 상속을 따라 모든 authored Text/InputField에 적용한다. font/style 외 기존 배치는 유지하되 한글 줄바꿈에 필요한 기존 Text overflow/font size와 해당 LayoutElement 높이만 좁게 조정 가능하다. 검사한 텍스트가 target과 이미 같으면 prefab save/override를 추가하지 않는다.

## API/담당/파일
A: 신규 Assets/_Project/Features/Run/Presentation/KoreanText.cs only staged A. public static Content(string) exact default label/description mapping(custom passthrough), Grade(Grade), Node(NodeType), Slot(EquipmentSlot), Tag(string), Match(MatchMode), Source(TagSource), Value(ModifiedValue), Notice(string) for complete existing RunSession/CombatRules legacy messages. 저장 수정/전역검색 없음.
Main: 기존 RunUIController.cs의 UI문구/실제값format/Content/Notice/enum표시만 변경; TitleUI.cs enter버튼; FateDiceWidgets.cs 노드label/표시tags; ActionCardView.cs gradeLabel, FateCardView.cs publictype/grade. 모두 기존 Features/.../Presentation 위치. 런타임총5개기존+KoreanText신규. 변경Rules/RunState/Save0. runtime exceptions의 원인은 기존diagnostic로그로 남기고 플레이어 문구는 한글 안내.
B: 신규 Features/Run/Editor/KoreanUiAuthoring.cs stagedB only. public const FontPath; public static string Apply(). stop/dirty/prefabstage guards, Common먼저 font갱신 후 inherited자산새로 load하고 차이만 save, App uiFont 및 legacy scene직접참조 갱신. 기본Menu heading/seedplaceholder/Title초기문구 한글. catalogfallback만 default영문/문자기호와정확히일치시대응한글글자표시로변경;customglyphpreserve. ID/색/이미지참조수정없음. 동일상태재실행hash불변.
C: stagedC only 기존 FateDiceGuiTests.cs,CombatLayoutTests.cs,SceneStructureTests.cs,ReusableViewTests.cs의 직접 표시문구 기대만 번역(필요시 UiStructureTests.cs의 literal기대도 동범위); 원본ID·게임/저장oracle·입력·geometry assert유지. 신규 Features/Run/Tests/PlayMode/KoreanUiTests.cs: font/한글glyph/all기본flow visible텍스트/legacy snapshot전부상태bytes불변/custom label유지. Main이Unity실행. RED부터실행.
Main: 모든 코드1:1 CodeMaps와 직접관계 UiPrototypeAuthoring,UiStructureAuthoring,SceneStructureAuthoring,RunScreenLayout,RunUIData,CombatUI,INDEX/PLAN/REPORT/폰트출처문서 및 검증artifact.
동시최대4 inclMain, 기존unique A/B/C3명재사용, 재귀0, 같은파일작성자1, Unity직렬화. 최초검토1묶음 및 필수Finding 수정재검증최대2묶음. 문서/asset/기존tests 별도예산없음.

## 정확한 자산 쓰기 허용
기존 prefix Assets/_Project/:
- Shared/UI/Prefabs/CommonButtonView.prefab, UIRoot.prefab(필요차이없으면no-op)
- Features/Run/Prefabs/TitleUI.prefab,MenuUI.prefab,ExplorationUI.prefab,CombatUI.prefab,EncounterUI.prefab,RewardUI.prefab,EquipmentUI.prefab,ResultUI.prefab,GameApplication.prefab
- Features/Combat/Prefabs/ActionCardView.prefab,CombatDie.prefab
- Features/Fate/Prefabs/FateCardView.prefab
- Features/Exploration/Prefabs/ExplorationNodeView.prefab
- Features/Run/Configs/DefaultFateDiceVisuals.asset(기본fallback glyph만)
- Scenes/FateDicePrototype.unity(uiFont만)
신규 Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf 및 Unity-generated meta/folder meta.
신규 Assets/StreamingAssets/ThirdParty/Pretendard-OFL.txt 및 Unity-generated meta/folder meta.
Unity Scene/Prefab/asset 쓰기는 Editor API만. 기존 GUID/meta보존. 기본 FateDiceConfig, Title/Lobby/InGame scene, Packages/ProjectSettings/원본외아트 수정없음.

## 실행/수용
1 baseline + 실제font/한글RED → 구현/import/Editor font·고정문구 적용.
2 최종 컴파일, 기존Edit122/Play51 및새테스트실제case PASS(0-case불가). 두 portrait720x1280/720x1600에서 타이틀/메뉴/전투 대표captured+기타phase실제표시검사, 한글missingglyph0/실제표시textoverflow0.
3 legacy영문save복제로한글표시해도 originalconfig/state/RNG/선택ID/저장bytes유지. rawtagsdefense보존으로방어카드파란색유지. 기본사용자save쓰기없음.
4 폰트다운로드URL/version/LICENSE/hash보존, includeFontData확인, authoredfixedfont모두target,authoring재실행무변경, boundaryhash/CodeMaps1:1/INDEX완료.
5 PLAN/REPORT결과/미검증/횟수/미해결갱신. APK/Android실기기 NOT_RUN. commit/staging/branch/package/global설정변경없음.

## 완료 기록
- 최종 Edit122/122, Play57/57, 컴파일 errors0. 한글 신규6건 포함. 실제 Title/Lobby/Combat 각720x1280/720x1600 캡처6장, 한글 누락/표시 높이 초과/빈 렌더링 메시0.
- 최초 검토1묶음. KUI-01은 19pt Pretendard의23px 글자가 기존22px 체력 칸에서 잘리는 실제 캡처 결함. 강화 검사 RED1건 후 해당fontSize18로 좁게 수정, 전체 회귀 GREEN. 수정/재검증1/2묶음 사용, 미해결 필수Finding0.
- 신규 검사의 CanvasRenderer.GetMesh API는 설치 Unity6000.6의 인자 없는 반환 Mesh 시그니처로 확인·수정한 뒤 컴파일/실행했다. 초기 API 컴파일 실패와 0-case 응답은 PASS로 세지 않았다.
- authoring 재실행 모든카운트0, 자산205파일hash변화0. C#54/CodeMap54/INDEX 일치. 이전 기존자산meta/GUID·규칙SO·제작씬·Packages/ProjectSettings baseline변화0.
- 상업용 배포 가능 Pretendard 원본 TTF/OFL 원문 SHA256 일치, dynamic/includeFontData=true. 기본사용자저장은 작업시작 전12:57:09 수정시각 그대로다. 격리 영어save는 표시 전후 bytes동일.
- 상세 결과와 증거: Docs/Reports/KOREAN_UI_REPORT.md. Android 실기기/APK는 이번 작업 범위 밖으로 NOT_RUN.
