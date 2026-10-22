# SHARED_WORLD_FLOW_COMBO_03 REPORT

2026-10-22 · IMPLEMENT · 완료. 기준 `c9e8a57`. 구현 완료 후 사용자 후속 승인에 따라 로컬 커밋 대상으로 확정했다.

## 사용자 결정과 구현

사용자는 모험 종료 버튼 누락과 3주사위의 2눈 조합 미발동을 지적했다. 종료 의미를 확인한 결과 **현재 모험은 보존하며, 별도 새 게임 버튼의 경고 이후 초기화**로 확정했다.

- 진행 중 `모험 종료`는 저장된 상태를 보존하고 공동 로비로 돌아간다. `경기 이어하기`는 같은 경기 ID·위치·사건·명령/RNG를 재개한다. 기존 `모험 포기`는 참가자 탈락 명령으로 별개다.
- 로비의 `새 게임`은 기존 경기가 있으면 경고와 취소/확정을 표시한다. 취소는 파일 불변이다. 확인 후 최신 프로필 조회와 새 경기 원자 저장에 성공해야 공개 상태를 교체한다. 새 ID·초기 위치·1개 주사위·빈 경기 명령/영수증으로 시작하고 현재 영구 성장 전력을 반영한다. 영구 재화/성장과 기존1인 기록은 초기화하지 않는다.
- Session.Restart는 기존 Start의 활성 경기 교체 금지와 별도다. 두 경로 모두 revision/generation을 비교하고, Store 잠금 안에서 다시 CAS와 명시 교체 여부를 확인한다. 교체 전에 활성 원문은 `.restarted-*`, 완료 원문은 `.completed-*`에 보존한다. 기존 백업도 유지한다. 프로필 조회 실패·저장 실패·오래된 세션은 기존 진행을 유지한다.
- 새 공동 경기 `shared-world-preview-2`는 전체 3눈 조합을 우선한다. 없으면 콘텐츠 순서상 첫 2눈 조합 하나를 선택하고 남은 눈 기본기→조합 순서로 실행한다. 각 눈 인덱스는 한 번만 소비한다. **지팡이(비전)·지팡이·검 → 마력탄(비전 기본기)·검기**, 지팡이·지팡이·불 → 마력탄·화염탄이다. 사람과 AI는 같은 버전 판정 경로를 사용한다.
- 기존 SymbolLoop v1 및 이미 시작한 SharedWorld v1은 정확한 전체 조합만 적용한다. 저장된 경기 정의를 소급 변경하지 않으며 새 규칙은 새 게임부터 적용한다. 콘텐츠 파일 경로/GUID, 기존 골든 벡터와 서버 v1 콘텐츠를 보존한다. 기존 2인자 SymbolCombat API도 유지한다.
- 혼합 로그를 순서대로 재생하고 각 기술의 SKILL/COMBO 제목을 따로 결정한다. 기호 주사위 회전·피해/보호·반격1회·처치 뒤 기술 생략은 유지한다. Scene/Prefab은 수정하지 않았다.

## 실제 검증

| 대상 | 결과 | 증거 |
|---|---|---|
| 조합 최초 RED | 3개 중 사용자 예시/신규 버전 2개 실패, 구형 판정 1개 통과 | [RED TRX](Evidence/SharedWorldFlowCombo/combo-red.trx) |
| 종료 UI 최초 RED | 1개 실행/1개 실패, 모험 종료 버튼 누락 | [RED](Evidence/SharedWorldFlowCombo/ui-red.json) |
| .NET 코어·저장·골든 | 38/38 PASS | [TRX](Evidence/SharedWorldFlowCombo/competition-final.trx) |
| Unity 컴파일 | completed, failed=false, 오류0 | [컴파일](Evidence/SharedWorldFlowCombo/compile.json) |
| Unity EditMode | 121/121 PASS, SharedWorld38+SymbolLoop83 | [결과](Evidence/SharedWorldFlowCombo/editmode-final.json) |
| Unity PlayMode | 15/15 PASS | [결과](Evidence/SharedWorldFlowCombo/playmode-final.json) |
| 보호·요약 감사 | 보호15파일 동일(사용자8 포함), C#47개 CodeMap/meta 누락0, staged0 | [감사](Evidence/SharedWorldFlowCombo/final-audit.json) |
| Editor 최종 상태 | Title 저장, Play 정지, 컴파일 중 아님 | [상태](Evidence/SharedWorldFlowCombo/editor-final.json) |

도메인 회귀는 실제 combat 명령의 사용자 예시, 화염탄/수호 베기·중복 눈·3눈 우선·후보 경합·각 눈1회 소비, 장비/보호막/반격 수치, 기본기 처치 후 조합 생략, 구형 API/골든 불변, v2 인간 명령의 AI 포함 전체 상태 재현과 직렬화 재개를 확인했다. 기존 골든의 기대값을 새 결과로 교체하지 않았다.

저장 회귀는 활성 Start 거절, 명시 Restart의 원문/백업 보존·초기 상태·입력 프로필 불변·재로드, 실패 주입·동시 세션 CAS·실제 임시 파일 독점 잠금 시 기존 파일/공개 상태 유지를 확인했다. 화면 회귀는 실제 포인터와 프레임으로 종료→재개, 경고→취소/확정, 초기화 후 재진입, 새 게임 직전 프로필 조회 실패 후 이어하기를 확인했다. 3주사위 전투는 격리 경기의 고정 면을 사용해 마력탄 SKILL→검기 COMBO 두 기술과 확정 로그를 확인했다.

PlayMode는 메모리 백엔드와 고유 임시 저장을 사용했다. 실제 사용자 계정/DB를 검사에 사용하지 않았다. 이번 변경에서 실제 HTTP 서버와 신규 UI의 전체 통합 실행 및 Android 장치 검증은 NOT_RUN이다. 사용자 요청으로 실행한 기본 Mock 서버는 변경하거나 종료하지 않았다. 실제 Firebase, C6 서버 정산/영구 보상, C7 밸런스/부하는 범위 밖이다.

## 문서·검토·화면

GDD/README/CodeMap 색인, 수정 C#의 1:1 요약과 CompetitionPlayback의 직접 표시 관계를 동기화했다. 신규 CompetitionPartialComboTests와 meta/요약을 추가했다. 이전 완료 REPORT는 당시 증거로 보존한다. 최종 검증 대상14파일(콘텐츠·C#·신규 meta)의 [SHA256](Evidence/SharedWorldFlowCombo/source-hashes.json)을 기록했다.

[새 게임 로비](Evidence/SharedWorldFlowCombo/main-lobby.png), [초기화 경고](Evidence/SharedWorldFlowCombo/new-game-warning.png), [3주사위 검기 발동](Evidence/SharedWorldFlowCombo/pair-combo.png)을 육안 확인했다. 경고 문구·확정/취소 버튼, 남은 기본기+조합 안내·검기 제목·모험 종료 버튼이 화면 안에 표시된다. 스킬 전체 순서는 PlayMode assertion으로 확인했고 정지 이미지는 검기 발동 시점이다.

소스·최종 증거/문서의 최초 검토1/1묶음을 종료했으며 미해결 필수 Finding0이다. 최초 산출물 이후 수정·재검증0/2이며 의도한 RED 재현은 수정 회차로 세지 않는다. 코드/문서 diff 검사 통과, 문서 링크 누락0, 기존 meta 변경0과 신규 GUID 유일성을 확인했다. 구현 완료 시점에는 커밋/push하지 않았다.

## 사용자 승인 커밋 범위

2026-10-22 후속 요청으로 이번 코드·문서·증거44파일을 로컬 커밋 대상으로 확정했다. 기존 사용자 변경8개는 제외하고 원문을 유지한다. staging 전 staged0, 검증 대상14파일과 보호15파일의 SHA256 일치, 전체 변경 목록이 대상44개와 제외8개로만 구성됨을 확인했다. 기존 검증 결과는 .NET38/38·EditMode121/121·PlayMode15/15이며 동일 소스의 테스트를 반복하지 않는다. 정확한 파일만 staging하고 커밋 후 실제 변경 목록·제외 파일 해시·남은 상태를 대조한다. push는 수행하지 않는다.
