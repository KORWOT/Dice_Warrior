# Symbol Loop 전면 교체 PLAN

유형 IMPLEMENT · 2026-10-20 · 완료(전면 교체 MVP, 필수 AC1~AC7).
승인 근거: 사용자 전면 교체·코드/에셋/문서 정리·순차 진행 지시. [기획](../Dice_Warrior_GDD_v0.3.md).
기존 `SYMBOL_COMBAT_LOOP_MIGRATION`은 이력으로 보존하며 더 진행하지 않는다.

## 기준과 보호

HEAD `82b4b7befd0a90f2e37a9a24be4612714040a2ce`, staged 없음.
기존 변경인 `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`,
`Assets/_Project/Shared/UI/Fonts/Pretendard/Pretendard-Effects SDF.asset`와 `ProjectSettings/Packages/`를 보호한다.
사용자 미추적 문서 3개 및 CodeMap artifacts 5개 폴더는 삭제하지 않고 같은 내용으로 Archive/v1에 보존한다.
기준 해시·상태는 `Temp/symbol-loop-migration/p0/protected-before.json`, 새 문서 이동 해시는 `Temp/symbol-loop-replacement/docs-before.json`.
실제 저장 데이터, .git, 패키지와 제3자 에셋은 수정하지 않는다. Orca 실행/사용 금지. Unity 기존 인스턴스 하나만 직렬 사용한다.

## 순서·쓰기 범위

1. 문서 정리: 기존 Docs 루트 기획, Plans, Reports, CodeMap을 `Docs/Archive/v1/`로 내용 보존 이동. ThirdParty 유지.
   새 README/GDD/본 PLAN/REPORT와 실제 새 C#의 1:1 CodeMap/INDEX를 작성한다. 공통 UI 요약은 현행 관계로 동기화한다.
   ThirdParty 라이선스는 유지하고 구형 연출 설치 안내만 원문 사본을 이력에 보존한 뒤 새 직접 의존으로 갱신한다.
2. 정리: `Assets/_Project/Features/{Combat,Dice,Exploration,Fate,Growth,Meta,Run,Save,Shop}` 전체와
   `Assets/_Project/Features/FateDice.Runtime.asmdef`를 삭제한다. 구형 테스트·fixture·Editor 도구도 포함한다.
   먼저 정확한 파일 목록/해시와 복구 사본을 Temp에 보관하고 외부 참조를 확인한다.
3. Scene/Prefab: Editor API로 `Assets/_Project/Scenes/Title.unity`를 재작성(기존 GUID 유지).
   `Lobby.unity`, `InGame.unity`, `FateDicePrototype.unity`는 삭제한다.
   구형 기능 폴더의 모든 Prefab/SO/Material은 2번과 함께 삭제한다. Shared UI prefab/font는 유지한다.
   직접 참조 검사에서 UIRoot.prefab의 UIManager.prefabs가 구형 화면을 참조함을 확인했다.
   `Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab`의 이 등록 배열만 비운다. GUID/레이어/폰트는 유지한다.
   `Assets/_Project/Shared/UI/Art/UiMappingProbe.asset`은 구형 매핑 진단 전용으로 삭제한다.
   새 `Assets/_Project/Features/SymbolLoop/Prefabs/SymbolLoopScreen.prefab`,
   `Configs/SymbolLoopConfig.asset`을 Editor API로 작성한다.
   EditorBuildSettings의 scenes만 Title 하나로 변경하고 configObjects는 보존한다.
4. 구현: `Assets/_Project/Features/SymbolLoop/` 아래 Domain, Runtime, Configs, Presentation, Editor, Tests의 필요한 C#/asmdef/meta.
   숫자 이동/정확 조합/장비/성장/사건/완주/보스/새 저장/실제 조작 UI를 한 런으로 통합한다.
5. 검증: 새 계약의 핵심 경계와 실제 연결만 확인하고 REPORT/CodeMap을 마친다.

삭제/이동 절대 경로가 프로젝트 하위인지 먼저 검사한다. 자산 삭제·작성은 AssetDatabase/PrefabUtility/EditorSceneManager로 한다.
기존 Scene 미저장 변경이 있으면 덮어쓰지 않는다. `Temp/symbol-loop-replacement/`는 실행 증거와 복구 사본만 쓴다.
Scene 실행 중인 게임 객체에 새 기능을 몰래 생성해 직렬화 연결을 우회하지 않는다.

## 필수 AC

- AC1: 현행 문서에서 v1 병행·카드·족보·분기 지도 요구를 제거. 이력 원문 해시 보존.
- AC2: 구형 기능 코드·전용 자산·테스트 제거, 보존 대상 해시 유지, 새 활성 씬/프리팹에 Missing Script 없음.
- AC3: 전체 조합 1개/fallback N개, D1/D2, 반격 1회, 태그 장비를 독립 기대값으로 검증.
- AC4: 12칸·5종 고정 보드, 착지만 발동, 완주 초과 소멸, 1→2→3개, 지역3 보상 후 보스, 성공/패배/정산.
- AC5: 저장 왕복, 저장 실패 시 상태/RNG 불변, 동일 요청 재시도, 중복 보상/정산 방지, 백업 확인/복원과 세대 차단.
- AC6: authored 화면의 로비→새 런→보드→전투/선택→완주→결과→로비 연결과 재개. 상태 확정 후 표시.
- AC7: 현재 C# 1:1 CodeMap, INDEX, REPORT의 검증/미검증 구분.

## 담당·검증 예산

메인이 순차 작성·Unity 실행·통합을 담당한다. 추가 구현 위임 0, 중복/재귀 위임 0.
이번 계약은 v1 병행에서 전면 교체로 실제 변경된 계약이다. 과거 문서 검토 횟수는 Archive에 유지한다.
최초 통합 검토 1회, 후속 필수 수정·재검증 최대 2회. 컴파일/작성 중 개발 피드백과 최종 산출물 검토를 구별하되 반복 테스트로 목표를 지연하지 않는다.
구형 전체 테스트/fixture 캡처는 실행하지 않는다. 새 규칙·저장 EditMode와 새 연결 PlayMode만 실행한다.
Android 빌드/장치 실플레이는 이번 필수 AC가 아니며 실행하지 않았다면 NOT_RUN으로 명시한다.
Git stage/commit/branch/push는 범위 밖이다.

## 진행

- [x] 변경 원문과 최신 사용자 지시 대조, 구형 P0 중단.
- [x] 구형 문서 198개 내용 보존 이관, 현행 기획 작성.
- [x] 사용자가 구체적인 삭제·Title 교체 목록을 “목록대로 삭제·교체 승인”으로 확인했다.
- [x] 정확한 삭제 목록/사본과 외부 참조 확인, Editor API 정리.
- [x] 새 게임 도메인·저장·화면 구현.
- [x] 새 핵심 검증과 직접 회귀 확인.
- [x] 요약/관계/실제 결과 갱신.

## 최초 통합 검토와 보완

최초 1/1: 새 EditMode 19 PASS, PlayMode 2 PASS 및 실제 PNG/소스/참조 검토.
보완 1/2: F1~F3 적용, 새 EditMode 20/20 PASS.
보완 2/2 완료: 새 EditMode 21/21, PlayMode 2/2 PASS. F1~F5 해결, 남은 필수 Finding 없음.
기본 Config/시드 33의 실제 포인터 완주, 최종 PNG, 안전 영역 축소, Title Missing Script 0 및 보호/문서 대조를 확인했다.
추가 일반 개선 검토를 열지 않는다.

| ID | 근거·영향 | 해결 조건 |
|---|---|---|
| F1 | 720×1280 board PNG의 양옆 타일 사각형이 겹침 | 작성 도구에서 타일 높이를 62→48로 줄이고 새 PNG 확인 |
| F2 | App.Update가 화면 크기만 검사하여 같은 크기의 safe area 변화 누락 | safeArea 변화도 비교하고 Android Portrait 요청 |
| F3 | 저장 검증에 지역/완주 ID/정산액/확정 로그 간 관계 검사 부족 | 후보/로드 검증에 관계 추가, 새 저장·완주 검사 재실행 |
| F4 | 고정 절대 배치가 짧아진 safe area/긴 세로 비율에서 잘릴 수 있음 | authored 720×1280 content를 가용 rect에 균일 축소, 두 비율과 축소 safe area 확인 |
| F5 | 손상 pending을 취소할 때 다시 파싱하여 취소가 막힐 수 있음 | 명시 취소는 원문을 고유 사본으로 이동, 새 입력 가능성과 사본 내용 확인 |

높은 HP/위력의 연결 fixture 결과와 실제 기본 규칙의 완주 가능성을 구분하기 위해 고정 시드 기본 규칙 경로를 추가 확인한다.
