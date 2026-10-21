# SYMBOL_LOOP_PRESENTATION · IMPLEMENT

2026-10-21. 사용자 요청: 부루마블형 칸별 말 이동, 숫자·기호 주사위 굴림 화면, 실제 스킬 발동 연출 및 문서 갱신.
기존 SYMBOL_LOOP_REPLACEMENT는 규칙 MVP 완료 이력으로 유지한다. 이번 작업은 새 표시 계약이다.

## 기준과 보호
- HEAD `82b4b7befd0a90f2e37a9a24be4612714040a2ce`, main. staged 없음. 기존 unstaged 삭제 572 / 수정 12 / untracked 항목 10. 상세 `Temp/symbol-loop-presentation/git-before.txt`.
- 기존 전면 교체 산출물을 이어 사용한다. 현재 대상 원본/해시를 `Temp/symbol-loop-presentation/before/`, `before.json`에 보관한다.
- Domain/Runtime/Config 규칙·저장 형식·난수·보상과 공통 UI, 기존 사용자 설정/폰트 변경을 보호한다. Orca, Git 변경·커밋, 새 패키지 도입 없음.

## 필수 AC / 설계
1. 12칸 사각 보드의 말이 숫자 D6 굴림 후 한 칸씩 이동한다. 출발 통과는 0번에서 멈추고 초과 이동을 버린다. 도착 이벤트는 착지 이후 표시한다.
2. 숫자 및 1~3개 기호 주사위를 면이 있는 입체형 Canvas 메시로 실제 회전·튕김·감속시킨다. 정지 면은 저장된 결과와 정확히 같다. 물리 시뮬레이션으로 결과를 다시 뽑지 않는다.
3. 전투 로그의 적용된 스킬마다 검격/마법/화염/방어 색·기호·충격·수치·체력 변화를 순서대로 표시한다. 전체 조합은 1회, 기본기는 원래 순서. 처치 후 생략 스킬/없는 반격은 재생하지 않는다.
4. 명령 저장 성공 후에만 재생한다. 재생 중 입력을 잠그고, 중단/화면 닫기/백그라운드/재표시에서는 최종 저장 상태로 복귀한다. 재추첨·중복 효과·보상 지급 없음.
5. 720×1280, 720×1600 및 축소 safe area에서 말/주사위/선택 버튼을 표시한다. 로비/이벤트/결과 흐름 유지.
6. 수정 C# 1:1 CodeMap, 기능 색인, 현행 GDD, README, REPORT를 동기화한다.

## 허용 경로 / 책임
- 주 작성자: 메인 Codex. `Assets/_Project/Features/SymbolLoop/Presentation/*.cs` 및 meta, `Editor/SymbolLoopAuthoring.cs`, `Tests/PlayMode/SymbolLoopFlowTests.cs`.
- 자산은 Editor API로만 `Assets/_Project/Features/SymbolLoop/Prefabs/SymbolLoopScreen.prefab`, `Assets/_Project/Scenes/Title.unity` 재작성 허용. GUID 유지. Build()의 기존 Title 단일 빌드 목록은 동일하게 유지하며 Config 기존 값은 변경하지 않는다.
- 문서는 위 AC 6 및 이 PLAN/REPORT만. 증거는 `Temp/symbol-loop-presentation/`.
- 구현/Unity 실행은 순차. 독립 읽기 전용 검토를 최대 1명에게 1회 위임할 수 있다(동시 총 2명, 누적 1명, 재귀 없음). 입력은 최종 변경 소스/계약/검증, 산출물은 근거 있는 필수 결함 목록. 쓰기 경계 없음. 공통 계약·색인·PLAN/REPORT는 메인만 수정.

## 검증과 상한
- 새 연출 경로를 실제 프리팹 PlayMode 포인터 흐름으로 검증: 칸별 방문/확정 눈/중복 클릭/중단 복귀/스킬 로그 순서/반격/완주. 실제 저장 대신 격리 임시 경로 사용.
- Unity 컴파일, 해당 PlayMode 묶음, 필요한 기존 21개 규칙·저장 회귀 1회. 프레임 캡처와 실제 이미지 확인. Android 장치 검증은 미실행으로 명시.
- 최초 통합 검토 1회, 이후 필수 수정·재검증 총 2회. 착수 당시 0/1, 0/2; 최종 1/1, 2/2. 테스트 결과 미실행을 PASS로 기록하지 않는다.

## 진행
- 계약/기준/대상 백업 기록 완료. 최종 구현/검증/문서 완료.

- 최초 통합 검토 1/1: PlayMode 3/4(84.4초), 실제 캡처 및 읽기 전용 보조 검토 통합.
- SL-P01: custom Graphic authored CanvasRenderer 누락, 캡처에 말/주사위/아이콘 미표시. 생성 시 필수 렌더러 보장 후 픽셀/메시 재확인 필요.
- SL-P02: 마지막 보상 선택의 pointer raycast 실패. 이전 fixture의 프레임 yield를 연출 대기로 바꾸며 동기 선택은 프레임 없이 연속 실행됨. 새 활성 Graphic의 absoluteDepth 등록 이후 다음 이벤트를 보내도록 프레임 경계를 복구한다.
- 수정·재검증 1/2 진행: 위 두 건과 직접 표시 회귀만 확인. 나머지 계약 재설계 없음.

- 1차 재검증 PASS: PlayMode 4/4, 105.33초. 실제 렌더 메시/중간 캡처에서 말·주사위·스킬 표시 확인. SL-P01/P02 해결.
- 직접 시각 검사 SL-P03: 칸 배치가 안내의 시계 방향과 반대였음. SL-P04: 보상/결과에도 주사위 굴리기 안내가 남음.
- 수정·재검증 2/2: 칸 좌표를 시계 방향으로 정렬하고 상태별 안내 및 결과의 주사위 트레이 숨김 적용. 변경 영향인 이동/세로·결과 표시만 재검증하며 규칙·저장 회귀는 예정된 1회 실행한다.

- 최종 완료: 전체 화면 4/4 + 마지막 변경 영향 이동 1/1·배치/결과 1/1, 규칙·저장 21/21. 개별/assembly 테스트 0개 선택 도구 현상은 스크립트 도메인 리로드로 해결했으며 미실행은 PASS에서 제외했다.
- 이미지와 GIF 확인, C# 18개 CodeMap/meta 누락 0, 보호된 사용자 설정·폰트 해시 유지. 남은 필수 Finding 없음. Android 실기기/최종 아트·음향은 범위 밖/미실행. 상세는 대응 REPORT.

## 2026-10-21 사용자 승인 커밋 정리 · DOCUMENT

- 승인: “지금까지 내용 정리해서 깃 커밋해줘”. 현재 main에서 이전 전면 교체와 화면 구현을 하나의 로컬 커밋으로 기록한다. push/branch 변경은 범위 밖이다.
- 포함: 승인된 구형 9개 기능/씬/자산 삭제, SymbolLoop 소스·설정·프리팹·meta, Title/UIRoot/빌드 씬, 현행 문서와 v1 원문 보존 이력.
- 제외: 기존 사용자 변경인 RP 설정 2개, Pretendard-Effects SDF, ProjectSettings/Packages의 설정 2개. 기존 staged 없음 확인. exact file manifest로만 staging.
- 정리: README와 본 PLAN/REPORT에 현재 범위·검증을 요약하고, 임시 폴더 소실에 대비해 최종 증거 10개를 Docs/Reports/Evidence/SymbolLoop/에 복사한다: edit-final.json, play-repair1.json, play-final-movement.json, play-final-layout.json, source-audit.json, board-720x1280.png, board-safe-area-720x1600.png, result-final.png, skill-combo.png, gameplay-preview.gif.
- 필수 확인: 포함/제외 경로, CodeMap/meta 1:1, 저장된 실제 결과 JSON, staged snapshot/최종 commit 파일 목록, 커밋 뒤 제외 변경 보존. 제품 코드는 변경하지 않으며 이미 완료한 테스트를 반복 실행하지 않는다.
- 주 작성자 메인, 추가 위임 0. 기존 구현 검토 예산은 재개방하지 않는다.
