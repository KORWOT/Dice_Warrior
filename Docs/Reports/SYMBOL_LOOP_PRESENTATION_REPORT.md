# SYMBOL_LOOP_PRESENTATION 보고

2026-10-21 · IMPLEMENT · 완료 (화면/연출 범위)

## 구현
- 12칸 사각 순환 보드, 유형 아이콘, 전사 말. 숫자 주사위가 정지한 뒤 말이 칸별로 이동/착지하며 출발점에서 초과 칸을 버린다.
- 숫자/기호 주사위의 여섯 면을 Canvas에 입체 투영하고 회전·감속·튕김을 재생한다. 기호 1~3개, 실제 설정 면과 저장된 결과가 연결된다.
- 전사/수호자 무대, 체력 바, 보호막, 조합 안내, 스킬 이름/기호/색/충격/피해 숫자, 실제 반격/방어를 연결했다. applied 로그 순서만 재생한다.
- 명령 저장 후 재생, 입력 잠금, 화면 닫기·앱 비활성·백그라운드·재표시 시 최종 확정 상태 복귀. 연출은 명령/난수를 다시 실행하지 않는다.
- Editor API로 Screen 프리팹과 Title 참조 작성. 기존 Scene/Prefab GUID·Config 수치·공통 UI·보호 설정 유지.

## 실제 검증
- 최초 컴파일 통과.
- 최초 PlayMode 3/4, 84.4초: `Temp/symbol-loop-presentation/play-initial.json`. 이동/중단·전투 로그·세로 배치 통과, 마지막 보상 버튼 pointer 검사 실패.
- 최초 이미지 캡처에서 custom Graphic이 보이지 않는 결함을 별도로 발견했다. 테스트 통과를 시각적 완료로 간주하지 않았다.
- 읽기 전용 보조 Codex 1명이 최초 검토에 참여. 구현·Unity 실행은 메인만 순차 수행.
- 수정 1차: CanvasRenderer 필수 컴포넌트 명시, authored 자산 재생성, 실제 메시 단언. 포인터 fixture에 화면 갱신 프레임 경계를 복구.
- 1차 재검증 PlayMode 4/4, 105.33초: `play-repair1.json`. 실제 Canvas 메시와 회전 중·정지·스킬/반격 이미지에서 표시 확인.
- 직접 시각 검사로 칸 진행 방향/안내 불일치(SL-P03)와 결과에도 굴림 안내가 남는 문제(SL-P04)를 발견해 2차 수정. 시계 방향 좌표와 Phase별 안내/결과 트레이 숨김 반영.
- 최종 개별 이동 검사 1/1, 4.23초: `play-final-movement.json`.
- 규칙·저장 전용 검사 21/21, 0.52초: `edit-final.json`. 실행 1회.
- 개별 TallPortrait 선택은 등록 목록에 존재하지만 완료 0개로 반환했다. PASS로 계산하지 않는다. 동기 PlayMode 호출도 도구가 거절해 실행되지 않았다. 패키지·전역 설정 수정 없이 assembly 선택과 실행기 직접 호출도 0개를 반환했다. 읽기 전용 TestRunnerApi/필터 조회는 해당 검사를 Runnable 및 선택 1개로 반환했고, 연결된 Editor는 1개(나머지는 임포트 워커)임을 확인했다. 스크립트 도메인 리로드 후 동일 검사 1/1 PASS(0.29초)로 복구됐다: `play-final-layout.json`. 도구의 0개 선택 시도는 검증 횟수/통과 건수에 포함하지 않는다.

## 문서와 관계
- GDD v0.3의 표시 계약, README, CodeMap INDEX 갱신.
- 신규 Glyph/Die/Playback 3개 요약, 수정 App/Screen/Authoring/FlowTests 4개 요약 동기화. 프로젝트 소유 C# 18개와 요약 1:1 유지.
- Domain/Runtime 저장 스키마·난수·보상·전투 계산 변경 없음.

## 검토 예산 / 미검증
- 최초 검토 1/1, 수정·재검증 2/2 사용. SL-P01 렌더러, SL-P02 포인터 프레임, SL-P03 방향, SL-P04 상태 안내 모두 해결. 남은 필수 Finding 없음.
- Android 실기기 터치·OS 재개/성능, 물리 Rigidbody 주사위, 최종 캐릭터 아트·음향은 미실행 또는 범위 밖이다.
- 구현 종료 시 staged/commit/branch 변경 없음. 이후 사용자 승인 커밋 정리는 아래 기록을 따른다. 원래 사용자가 변경한 RP 설정 2개와 폰트 자산의 SHA256 유지. `source-audit.json`: 프로젝트 C# 18개, CodeMap/meta 누락 0. 패키지 변경 없음.

## 최종 증거 / 실행
- 모든 파일은 `Temp/symbol-loop-presentation/` 아래에 보관한다.
- `play-repair1.json`: 화면 전체 4종 4/4(실제 기본값 캠페인 완주 포함). 최종 표시 보정 이후 직접 영향받는 이동 1/1 및 배치/결과 1/1을 별도 재검증했다.
- `edit-final.json`: 규칙·저장·프리팹 전용 21/21. 최종 코드 컴파일 오류 0.
- `board-720x1280.png`, `board-720x1600.png`, `board-safe-area-720x1600.png`, `result-final.png`: 최종 정지 화면. 말·기호·주사위와 버튼이 잘리지 않는지 직접 이미지 확인.
- `travel-frames/` 23장, `combat-frames/` 32장: 실제 Unity Canvas 카메라의 이동/회전/스킬 중간 프레임. `gameplay-preview.gif`는 이 캡처를 나란히 축소한 미리보기이며 새 그림으로 대체하지 않았다. 100ms 프레임으로 인코딩해 재생 타이밍이 원본과 미세하게 다를 수 있다.
- `board-initial-invisible.png`, `play-initial.json`: 최초 실패 보존. 최종 증거와 구분한다. 최초 캡처로 남아 있는 `result-720x1280.png`의 구형 안내는 최종 `result-final.png`가 대체한다.
- 실행: `Assets/_Project/Scenes/Title.unity` → Play → 새 여정 → 이동 주사위/기호 주사위. 구현은 입체 투영 기반 회전 연출이며 주사위 물리로 게임 결과를 재판정하지 않는다.

## 사용자 승인 커밋 체크포인트

2026-10-21 사용자 요청으로 전면 교체·캠페인·저장·화면 연출·문서 보존 이력을 하나의 로컬 커밋 범위로 정리했다. 기존 사용자 RP 설정 2개·폰트 변경·ProjectSettings/Packages의 설정 2개는 포함하지 않는다. 제품 코드와 검증 완료 기준은 변경하지 않았다.

최종 증거는 임시 경로와 별도로 저장소에 보존한다. [EditMode 21/21](Evidence/SymbolLoop/edit-final.json), [화면 4/4](Evidence/SymbolLoop/play-repair1.json), [최종 이동 1/1](Evidence/SymbolLoop/play-final-movement.json), [최종 배치 1/1](Evidence/SymbolLoop/play-final-layout.json), [요약·meta·보호 파일 확인](Evidence/SymbolLoop/source-audit.json).

[보드](Evidence/SymbolLoop/board-720x1280.png), [축소 safe area](Evidence/SymbolLoop/board-safe-area-720x1600.png), [조합 스킬](Evidence/SymbolLoop/skill-combo.png), [결과](Evidence/SymbolLoop/result-final.png), [실제 캡처 미리보기](Evidence/SymbolLoop/gameplay-preview.gif).

과거 교체 작업의 Temp 증거 경로는 일시 보관 위치이며 커밋에는 포함하지 않는다. 새 체크아웃에서는 이 절의 저장소 증거와 Git 이전 커밋의 구형 소스를 참조한다. Android 장치 검증은 미실행 상태로 유지한다.
