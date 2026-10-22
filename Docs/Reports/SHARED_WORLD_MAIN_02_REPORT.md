# SHARED_WORLD_MAIN_02 REPORT

2026-10-22 · IMPLEMENT · 완료. 새 기준 `73020d1`. 사용자 후속 승인에 따른 별도 로컬 커밋 대상.

## 선행 커밋·현재 범위

사용자 요청대로 C0~C5 공동 세계 구현·문서·증거118파일을 먼저 `73020d1 feat: add shared-world adventure competition through C5`로 커밋했다. 기존 사용자 변경8개는 제외했으며 push하지 않았다. 검증 당시 소스30개 SHA256과 보호18파일이 동일함을 확인했다.

후속 변경은 기존1인 모험판을 제거하고 공동 모험을 Title의 메인으로 만드는 작업이다. C6 신규 서버 정산·영구 보상과 C7 밸런스는 추가하지 않는다. 사용자의 추가 요청에 따라 이 변경과 문서·검증 증거를 별도 로컬 커밋으로 기록한다. 기존 사용자 변경8개는 제외하고 push하지 않는다.

## 결정·구현

- Title의 구형 App 오브젝트와 구형 화면 등록을 Editor API로 제거하고 CompetitionApp/CompetitionScreen 하나만 연결한다. 기존 Scene GUID와 다른 Scene 오브젝트/공용 UI/전투Config를 보존한다.
- 새 로비에서 서버 조회, 출전 주사위·무기 선택, 공동 모험 시작/이어하기/로비 복귀를 제공한다. 신규 계정 연결이 기존 LoopStore/LoopSession/Coordinator/HTTP를 사용하며 구형 UI 컴포넌트를 서비스로 남기지 않는다.
- **사용자 확정:** 기존1인 active run은 기록만 보존하고 해당 계정의 영구 성장 구매를 보류한다. 자동 종료/정산/재화 지급/저장 삭제 없음. 공동 모험 출전 선택·시작·재개는 가능하도록 출전 값을 별도로 저장한다.
- 구형 App/Screen/Authoring/화면Prefab/전용PlayMode와 대응meta·요약을 삭제한다. 공유전투/표시/Config, 기존 서버v1/계정/복구/재현검사는 보존한다.
- 새 계정 Runtime이 서버 순수 검사의 wildcard 링크에 들어가지 않도록 .NET 검사 프로젝트는 CompetitionSession/CompetitionStore2개만 명시한다. 서버 제품 소스·프로토콜·DB는 변경하지 않는다.

## 실제 검증

| 대상 | 결과 | 근거 |
|---|---|---|
| 새 주흐름 최초 RED | 1실행/1실패, 로비 출전 선택 미노출 | [RED](Evidence/SharedWorldMain/main-lobby-red.json) |
| 신규 .NET 코어/저장/골든 | 23/23 PASS, 링크 변경 후 재실행 | [TRX](Evidence/SharedWorldMain/competition.trx) |
| 최종 Unity 컴파일 | completed, failed=false, errors0 | [컴파일](Evidence/SharedWorldMain/compile-final.json) |
| Unity EditMode | 106/106 PASS, SharedWorld23+SymbolLoop83 | [EditMode](Evidence/SharedWorldMain/editmode-status.json) |
| Unity PlayMode 최초 실행 | 11/12 PASS, 신규 활성 버튼의 같은 프레임 클릭1실패 | [최초 결과](Evidence/SharedWorldMain/playmode-initial.json) |
| 최종 Unity PlayMode | 12/12 PASS, 9.08초 | [최종 결과](Evidence/SharedWorldMain/playmode-final.json) |
| Title·자산 | App1/UI1/missing0, Scene/Prefab GUID 유지, 구형7자산과 meta·빈폴더3개 제거 | [작성 후 확인](Evidence/SharedWorldMain/authored-main.json), [삭제](Evidence/SharedWorldMain/asset-removal.json) |
| 보호 감사·CodeMap | 보호18파일 SHA256 동일(사용자8포함), C#46개 요약 누락0, staged0 | [감사](Evidence/SharedWorldMain/final-audit.json) |
| Editor·기본 서버 | Title 저장·Play 정지, 기존5087 health=ok | [Editor](Evidence/SharedWorldMain/editor-final.json), [서버](Evidence/SharedWorldMain/mock-server-health.json) |

구형 화면 전용7개 검사는 제거한 흐름의 검사이므로 새 PASS 수에 포함하지 않는다. 신규 메인 로비/계정/경기 복귀·비동기 회귀로 대체한다. 실제 사용자 저장과 기본5087서버 DB로 검사를 수행하지 않는다. 사용자가 켜 달라고 요청한 기본 Mock 서버는 유지한다.

## 검토·문서·한계

최초 검토1묶음 종료, 수정·재검증1/2, 미해결 필수 Finding0. 조사에서 기존 active run의 출전/성장제약을 확인했고 사용자에게 기록보존/성장보류 결정을 받았다. 계정 조회/성장/재시도는 finally에서 디스크 pending을 다시 읽어 서버 승인 후 로컬 적용 실패에도 같은 ID 복구를 제공한다.

MAIN-01은 검사에서 메뉴 활성화와 무기 클릭을 한 프레임에 수행한 문제다. [진단 재현](Evidence/SharedWorldMain/playmode-diagnostic.json)에서 위치·active/interactable/raycast·상위 CanvasGroup은 정상이고 Graphic.depth/absoluteDepth=-1이었다. uGUI의 렌더 이전 제외 조건과 일치했다. 해당 검사만 다음 프레임 후 클릭하도록 보정하고 포인터 hit·출전 값·저장 재개 assertions를 유지했다. 제품 코드는 이 수정에서 변경하지 않았고 최종12개 전체가 통과했다. 원인 진단1회는 실패 재현이며 별도 수정회차로 세지 않는다.

PlayMode는 새 계정 직접 진입·최신 성장과 출전 동결·로비 복귀/재생성 후 재개·비활성 중 지연 응답·서버 실패/명시 재조회·성장 응답 유실·승인 후 파일 잠금에 의한 로컬 적용 실패·같은 ID 재시도·old active 원문 보존/성장 보류·손상 및 기존 pending 보존을 확인한다. 이동/기호/스킬/AI 표시와 두 모바일 비율·안전영역도 포함한다. 실 사용자 계정 대신 주입 backend와 고유 임시 저장을 사용했다. HTTP 경로/본문/헤더와 응답 처리는 EditMode의 주입 HttpMessageHandler로 확인했다. 이번 메인 전환에서 실제 서버와 새 로비의 전체 통합 실행 및 사용자 계정의 Title Play는 수행하지 않았다. 기본 서버는 health 조회로 실행 유지 상태만 확인했다.

[메인 로비](Evidence/SharedWorldMain/main-lobby.png), [출전 선택](Evidence/SharedWorldMain/loadout.png), [이전 기록으로 성장 보류](Evidence/SharedWorldMain/legacy-growth-held.png), [공동 보드](Evidence/SharedWorldMain/board-720x1280.png), [720×1600 안전영역](Evidence/SharedWorldMain/safe-area-720x1600.png)을 육안 확인했다. 주요 버튼·문구·네 참가자와 장소가 화면 안에 표시된다.

GDD/README/서버안내/CodeMap 색인, 수정한 C#과 직접 계정·표시 관계를 동기화했다. 삭제된4개 C# 요약을 제거하고 신규 CompetitionAccount 요약을 추가했다. 이전 완료 REPORT는 당시의 증거로 유지한다. 최종 검증 대상9파일의 [SHA256](Evidence/SharedWorldMain/source-hashes.json)을 재확인했다. 코드·문서 diff 검사는 통과했으며 Prefab YAML의 빈 문자열 뒤 공백은 Unity Editor 직렬화 출력으로 보존했다.

Android/실기기, 실제Firebase, 신규 경기 서버권한·정산/영구보상, 대량밸런스/부하검증은 이번 범위가 아니다.

## 사용자 승인 커밋 범위

2026-10-22 후속 요청으로 메인 전환 코드·자산·문서·증거71파일을 별도 로컬 커밋 대상으로 확정했다. 기존 사용자 변경8파일은 제외하며 staging 이전 staged0과 전체 변경 목록의 일치를 확인했다. 검증 대상 소스9파일과 보호18파일의 SHA256이 검증 당시와 동일하므로 테스트를 반복하지 않았다. 저장된 증거는 EditMode106/106, PlayMode12/12, .NET23/23이다. 커밋 전후 정확한 파일 집합과 제외 파일 해시를 대조하며 push는 수행하지 않는다.
