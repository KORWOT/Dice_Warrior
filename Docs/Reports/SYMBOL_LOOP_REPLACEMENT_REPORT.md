# Symbol Loop 전면 교체 REPORT

상태: **완료 — 전면 교체 MVP, AC1~AC7 충족**. 출시 아트/밸런스/장치 검증 완료를 의미하지 않는다.
현재 계약은 [PLAN](../Plans/SYMBOL_LOOP_REPLACEMENT_PLAN.md)과 [GDD v0.3](../Dice_Warrior_GDD_v0.3.md).

## 실제 수행

- v1 병행 보존/P0 호환성 작업 중단. 구형 EditMode 317 PASS는 이력만 남기고 구형 PlayMode는 취소했다.
- 기존 기획·PLAN·REPORT·CodeMap 총 198개를 Archive/v1로 이동하고 이동 전후 SHA256 일치를 확인했다.
- 사용자 원문 GDD는 수정하지 않았다. 현행 v0.3은 최신 전면 교체 지시와 기존 D1/D2를 반영한다.
- 사용자 구체 승인 후 구형 9개 기능 폴더의 C# 133개, 전용 프리팹 15개, 설정/진단 자산 5개, 구형 씬 3개와 관련 material/asmdef/fixture/meta를 제거했다.
- 삭제·교체 전 관련 파일 392개를 `Temp/symbol-loop-replacement/before/`에 복사하고 해시를 확인했다. 공용 UIRoot도 별도 사본을 남겼다.
- 보존 자산의 의존을 검사해 UIRoot의 구형 화면 등록을 먼저 해제했다. 나머지 자산에 삭제 대상 의존이 없었다.
- SymbolLoop 기능 폴더에 새 C# 11개, assembly 5개, 설정 SO와 화면 프리팹을 작성했다. 공용 UI C# 4개는 재사용했다.
- 전체 기호 조합/기본기 자동 전투, 12칸/3지역, 착지 선택, 장비/주사위 교체, XP/레벨, 완주 보상/최종 보스, 영구 정산/성장을 연결했다.
- Title 씬을 유일한 제품 진입점으로 교체했다. Title/UIRoot GUID를 유지했고 EditorBuildSettings의 configObjects는 보존했다.
- 새 저장 전용 경로를 사용한다. 후보 저장 후 공개, 요청 fingerprint/receipt, 버전/복구 세대, 정상 백업, 미결 입력 재시도/명시 취소를 구현했다. 실제 v1 저장은 접근하지 않았다.
- 현재 프로젝트 소유 C# 15개에 1:1 CodeMap을 맞추고 INDEX의 직접 관계를 새 구조로 교체했다.
- 외부 연출 설치 안내 원문도 추가 사본으로 보존하고 현행 안내는 실제 직접 의존으로 갱신했다. 폰트 라이선스 문서는 유지했다.

## 실제 검증

| 항목 | 결과 / 증거 |
|---|---|
| 최종 컴파일 | 완료, failed=false, errors=[] |
| 새 EditMode | **21/21 PASS**, failed/skip/inconclusive 0, 0.61초. [원본 결과](../../Temp/symbol-loop-replacement/edit-final.json) |
| 새 PlayMode | **2/2 PASS**, failed/skip/inconclusive 0, 0.85초. [원본 결과](../../Temp/symbol-loop-replacement/play-final.json) |
| 조합/턴 | 1~3개 주사위의 84 ordered outcomes, D1 적 사망 생략, D2 묶음 보호막, 태그/수호 계산 |
| 캠페인/성장 | 착지 전용, 초과 이동 소멸, 1→2→3개, 지역3 보상 후 보스, 장비/주사위/비용, 결과/정산/성장 |
| 원자성/복구 | 파일 왕복, 실패 시 상태/RNG/receipt 불변, 같은 입력 재시도, ID 내용 충돌, stale 버전, 규칙 동결, 손상 원본/미결 입력 보존, 복구 세대 |
| 실제 UI | 실제 Config 기본값(HP60/위력5)과 시드33, raycast로 가려짐 확인 후 pointerClick: 새 런→3지역→보스 승리→정산50→로비. 재로드/재표시 상태 불변 |
| 화면 | 720×1280/720×1600 및 축소 safe area. PNG를 직접 확인, 타일 겹침 수정 |
| 활성 자산 | Title preview의 Missing Script 0, App/Config/UIManager/화면 연결, 삭제 기능 참조 없음 |
| 문서·보호 | 기존 문서198개 해시 동일, 보호 파일537개 변경 없음, CodeMap 누락0, 새 feature 파일18개 meta 누락0. [대조 결과](../../Temp/symbol-loop-replacement/final-audit.json) |
| Git/환경 | staged 없음, commit/branch/push 없음. Unity ready / playMode stopped. 패키지/설정의 비허용 변경 없음 |

새 핵심 검사만 실행했다. 최초 연결 검사의 HP/위력 상향 fixture와 최종 기본 Config 검증을 구별한다.
정산·성장 EditMode 일부는 높은 HP/위력으로 제어한 fixture이며 밸런스 승률 근거로 사용하지 않는다.
구형 EditMode317 PASS/PlayMode 취소는 Archive의 이력일 뿐이다.

화면 증거: [보드](../../Temp/symbol-loop-replacement/board-720x1280.png), [보스 전투](../../Temp/symbol-loop-replacement/combat-720x1280.png),
[완주·정산](../../Temp/symbol-loop-replacement/result-720x1280.png), [축소 안전 영역](../../Temp/symbol-loop-replacement/board-safe-area-720x1600.png).

## 검토·보완

최초 통합 검토 1/1, 후속 수정·재검증 2/2. PLAN의 F1~F5 모두 해결했다. 남은 필수 Finding 없음.
Unity 작성 YAML의 빈 값 뒤 공백은 Editor 직렬화 결과이며 수동 수정하지 않았다. C#/문서 범위 `git diff --check`는 종료0이다.
자동 승인 검토가 최초 광범위 삭제를 거절했으며, 사용자가 정확한 목록에 “목록대로 삭제·교체 승인”을 준 후 실행했다. 현재 승인 차단은 없다.

## 미실행·후속 범위

- Android 빌드, 실제 기기 터치/회전/중단·재개/파일 교체: **NOT_RUN**.
- 경제·난이도 분포·장시간 플레이·최종 일러스트/전투 연출: 이번 MVP 완료 기준 밖이며 확정하지 않았다.
- v1 저장 이관·이어하기, 온라인/결제/클라우드: 비목표. 기존 파일은 그대로 남고 새 게임은 새 프로필로 시작한다.
- 사용자가 설치한 외부 플러그인·샘플은 보존했다. 새 게임의 직접 의존이 아닌 항목의 별도 제거는 수행하지 않았다.

현재 시작 방법과 현행 문서 진입점은 [README](../README.md)다.
