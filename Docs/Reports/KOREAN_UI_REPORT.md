# KOREAN_UI_REPORT
상태: IMPLEMENT / COMPLETE · 2026-09-08 · D:/UnityProject/Dice_Warrior · Unity6000.6.0f1, CLI7801.

## 결과
기본 플레이 흐름의 Title/로비/탐험 지도·주사위·카드/전투/사건·상점/보상/장비 교체/승리·패배·지난 결과 및 저장·입력·이동 실패 안내를 한국어로 표시한다. 브랜드는 ‘운명의 주사위’, 등급은 일반/고급/희귀/영웅/전설, 전투 용어는 체력/위력/방어력/수호/운명력/재굴림으로 통일했다.

KoreanText가 알려진 기본 영문 콘텐츠47개와 기존 RunSession/CombatRules 알림을 표시 경계에서 번역한다. 기존 영문 save의 config/message 자체는 변경하지 않으며 사용자 편집 영문·혼합 명칭/설명은 그대로 표시한다. 내부 enum/tag/명령 키/원본·제시 ID/RNG/schema/수치·효과는 유지했다. DTO의 raw defense 태그도 그대로이므로 방어 카드 색상 분기를 보존한다.

## 폰트
Pretendard Regular v1.3.9 공식 원본 TTF를 프로젝트에 포함했다. SIL OFL1.1은 상업용 소프트웨어 포함·배포를 허용하며 저작권·라이선스 원문을 유지해야 한다. 폰트 자체의 단독 판매와 변경판의 Reserved Font Name 조건은 공식 원문을 따른다.
- [공식 라이선스](https://github.com/orioncactus/pretendard/blob/v1.3.9/LICENSE)
- [고정 배포 버전](https://github.com/orioncactus/pretendard/releases/tag/v1.3.9)
- [프로젝트 출처·SHA256·배포 조건](../ThirdParty/PRETENDARD.md)
- 파일: Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Regular.ttf, 2,725,828bytes, SHA256 6D0AF5258997AEC7354A6E340FC2325BA321C410CA48B3AF858C8C3D6E92A324.
- 저작권/OFL 원문: Assets/StreamingAssets/ThirdParty/Pretendard-OFL.txt, SHA256 D31DDD9F2BED32FD7E302A205CF2380BA0DE6529152D239EF99CFB6F261BFC04. Unity 빌드 포함 경로에 보존했다.
- Unity dynamic/includeFontData=true, OS 글꼴 설치 의존 없음. 폰트 바이너리 변형·서브셋·추가 이미지·언어 패키지는 없다.

## 실제 검증
증거 루트: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/korean-ui/.

| 검증 | 결과 | 증거 파일 |
|---|---|---|
| 폰트 적용 전 RED | 1건 예상 실패: 기본 Unity font 참조 | red-play.json |
| 새 한국어 통합6건 | 실제 제작 UI·글리프·영어잔존·커스텀문구·저장불변 PASS | korean-play-green.json, final-play-green.json |
| 최종 EditMode | 122/122 PASS, 8.09초 | final-edit-green.json |
| 최종 PlayMode | 57/57 PASS, 63.64초; 신규6건 포함 | final-play-green.json |
| 최종 컴파일 | completed/failed=false/errors0 | final-compile.json |
| Title/로비/전투720x1280·720x1600 | 실제 캡처6장, missing glyph0/height overflow0/invisible text mesh0 | title-*.png, lobby-*.png, combat-*.png, visual-checks.json |
| 기존 영문 전투 save 표시 | bytes SHA256 전후21FE248C0054A651C47C11B909F721F94C19BD50E1D787C6D2F419C454F54E1E 동일 | capture-save-unchanged.json |
| authoring 재실행 | 저장·폰트·글리프·크기·씬·import 모두0, 자산205파일hash변화0 | authoring-idempotence.json |
| 파일 경계/문서 | 허용 밖 변경0, C#54/CodeMap54/INDEX 누락0 | boundary-verification.json |
| 최종 Editor | Play 정지, clean Title, app instance 없음, background=false | final-editor-state.json |

신규6개 테스트는 실제 GameApplication을 임시 LocalRunStore로 bootstrap하고 제작씬을 사용한다. 노드/Title 버튼은 Raycaster와 PointerEvent로 조작한다. 승패는 실제 RunSession 규칙으로 생성하고 화면 전환이 보상이나 진행을 재적용하지 않는지 검사한다. playedSeconds만 제외한 전체 RunState와 config/카드ID/RNG, 표시 전후 파일 bytes를 비교했다. 실제 사용자 저장은 테스트·캡처 대상이 아니며 기존 파일의 마지막 수정시각12:57:09는 이번 baseline 작성13:13:51 이전으로 유지됐다.

## Finding 및 수정 예산
최초 검토1묶음(보조 읽기 검토와 Main 실행/캡처). KUI-01 수정·재검증1/2묶음 사용, 미해결 필수Finding0.

KUI-01: 초기 자동 검사는 2px 높이 허용오차로 PASS였으나 실제 두 전투 캡처에서 ‘체력90/90’이 보이지 않았다. Pretendard19pt의 preferredHeight23이 기존 칸22보다 커서 Text Truncate가 한 줄 전체를 숨겼다. 검사를0.1px 허용오차와 실제 CanvasRenderer 메시 정점 수로 강화하여 RED(23>22.1)를 확인했다. KoreanUiAuthoring에서 이 기존 기본19pt만18pt로 보정한 뒤 전체 Play57 및 Edit122와 두 비율 캡처를 통과했다. HP 값·막대 배치는 바꾸지 않았다.

강화 검사 작성 중 GetMesh(mesh) 시그니처의 컴파일 실패1회가 있었다. 설치된 Unity6000.6 reflection으로 GetMesh()를 확인해 테스트를 바로잡았다. 반환 Mesh는 Unity 소유이므로 수정·삭제하지 않는다. 이 컴파일 실패와 비동기 run_tests의 초기0-case 응답은 성공 근거가 아니다.

## 변경 경계 및 유지보수
기존 C#9개는 표시 문구·직접 테스트 기대만 변경했고 새 C#은 KoreanText/KoreanUiAuthoring/KoreanUiTests3개다. 고유 기존 prefab12개에서 Text 폰트81참조/controller font2참조와 고정 문구8개를 갱신하고 기본 glyph42개 및 legacy scene의uiFont를 갱신했다. KUI-01은 같은 CombatUI prefab의 글자 크기1개 추가 변경이다. 공통 원본 상속을 유지해 불필요한 variant override는 만들지 않았다.

기존 자산meta/GUID, DefaultFateDice 규칙SO, Title/Lobby/InGame scene, Packages/ProjectSettings 및 기존 사용자 변경은 baseline hash와 동일하다. 변경·신규 C#12개와 직접 관계6개의 CodeMap을 동기화하고 INDEX를54개로 갱신했다. 이전 완료 PLAN/REPORT를 재개방하지 않았다. commit/staging/branch/global 설정 변경 없음.

새 기본 콘텐츠/알림을 추가할 때 KoreanText의 정확한 기본 문구 번역도 추가한다. 기존 생성 도구로 UI 원본을 다시 만들었다면 Editor의 Fate Dice/Apply Korean UI 또는 CLI의 KoreanUiAuthoring.Apply()로 폰트와 고정 문구를 적용한다. 신규 화면·새 언어·자동 번역 시스템은 이번 작업에 포함되지 않는다.

Android 실기기/APK 빌드 NOT_RUN. 실제 검증 범위는 연결된 Unity Editor와 위 제작 플레이 흐름이다.
