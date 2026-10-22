# GOD_ADVENTURER_GROWTH_04 REPORT

2026-10-22 · IMPLEMENT · 완료. 기준 `000662b7dd6fa9c743d082b827603c046948cb81`. commit/push는 수행하지 않았다.

## 확정 범위와 구현

사용자는 성장 방향 GDD v0.2를 확인하고 **1차 플레이 개편**, **기본 제공3종과 시험값**을 승인했다. 원문의 확정 방향/제안/후속을 구분하고 실제 계약은 [PLAN](../Plans/GOD_ADVENTURER_GROWTH_04_PLAN.md)에 동결했다. 신격·권능 트리·영구 포인트·업적 해금·재굴림·C6 정산은 구현 범위 밖이다.

- 방랑 검사 HP60/무력6/수호4/비전4, 강철 태그 피해+1. 룬술사60/4/4/6, 비전·불 태그 피해+1. 성채의 수호자60/4/6/4, 보호막 발생 기술 보호+1. 세 종류를 업적 조건 없이 기본 제공한다.
- v3 전투에서 물리 계열은 무력, 마법 계열은 비전, 화염 검기는 두 값의 평균을 한 번 내림해 기본 피해에 더한다. 보호는 수호를 더한다. 기존 태그 장비와 패시브를 적용하며 기본 효과0에 새 효과를 만들지 않는다. 기존 부분 조합/적 보호막/처치 뒤 생략/반격1회·64턴 제한은 유지한다.
- 모험가 XP 필요량=현재레벨×8, 레벨마다 최대/현재HP+6·세 스탯+1. 기존 계정 성장당 시작HP+5를 유지한다. AI의 검사/룬술사/수호자도 같은 계정HP/시작 주사위·무기를 사용하고 개인 레벨·장비·사건은 각자 성장한다. 신의 영구 포인트를 지급하지 않는다.
- 로비에서 다음 출전 모험가를 고르고 별도 체크섬/revision 파일에 원자 저장한다. 진행 중 경기는 그대로이며 새 게임에서만 선택을 반영한다. 실패·손상·오래된 선택 revision은 원문을 보존한다. 선택 저장 오류에도 기존 경기 이어하기는 가능하다. 이 파일은 출전 선호이며 해금/권한 저장이 아니다.
- `출전 능력치`와 `능력치 상세`는 RGB 색/기호와 기본·계정HP·레벨·합계, 장비의 태그 보정을 보여준다. `주사위·스킬`은 현재 보유6면과 실제 가능한 기술/계수를 계산해 표시한다. 상세 화면은 경기/RNG를 변경하지 않는다. 영구 성장 구매/프로필 재확인은 v3의 `계정 관리`에 있다.
- 신규 규칙/콘텐츠는 `shared-world-preview-3`의 별도 JSON이다. 기존 SymbolLoop 및 SharedWorld v1/v2 콘텐츠·골든·직렬화·계정 프로토콜을 보존한다. 구형 경기 상세에는 기존 위력/수호를 표시하고 v3 전환은 경고 후 새 게임에서만 한다. 원문은 `.restarted-*`에 보존된다.
- 승인된 CompetitionScreen.prefab/Title.unity만 Editor API로 갱신했다. 프리팹에 상세 패널·카드3개·RGB3행을 작성하고 Title의 콘텐츠 참조를 v3로 바꿨다. 런타임 임의 오브젝트 생성으로 자산 연결을 우회하지 않았다.

## 실제 검증

| 대상 | 결과 | 증거 |
|---|---|---|
| 도메인 RED | 18개 중 신규17 FAIL, 기존 보호1 PASS | [TRX](Evidence/GodAdventurerGrowth/growth-red.trx) |
| .NET 도메인·저장·골든 | 59/59 PASS | [TRX](Evidence/GodAdventurerGrowth/competition-final.trx) |
| Unity 컴파일 | completed, failed=false, 오류0 | [결과](Evidence/GodAdventurerGrowth/compile-final.json) |
| Unity EditMode | 142/142 PASS: SharedWorld59+SymbolLoop83 | [결과](Evidence/GodAdventurerGrowth/editmode-final.json) |
| Unity PlayMode 최초/표시 보정 후 | 각19/19 PASS | [최초](Evidence/GodAdventurerGrowth/playmode-initial.json), [최종](Evidence/GodAdventurerGrowth/playmode-final.json) |
| 보호·문서 감사 | 보호16파일 동일(사용자9 포함), C#52개 요약/meta 누락0, 신규6GUID 유일, 링크 오류0, staged0 | [감사](Evidence/GodAdventurerGrowth/final-audit.json) |
| Editor 종료 상태 | Title, dirty=false, Play 정지, 컴파일 중 아님 | [상태](Evidence/GodAdventurerGrowth/editor-final.json) |

9기술×3모험가의 리터럴 피해/보호 기대값, 혼합 계수 합산 후 내림, 장비/패시브·처치/반격, 실제 사건 XP로 레벨업, 새 경기 성장 초기화, 구형 null 필드 생략과 고정 골든, v3 인간 명령과 AI 전체 상태 재현/중간 직렬화 재개를 확인했다. 기대 골든을 새 결과로 교체하지 않았다. 선택 저장은 실제 고유 임시 파일로 CAS·잠금 실패·손상/크기/백업 원문 보존을 확인했다.

PlayMode는 authored UI의 실제 Raycast/포인터로 선택→예상 수치→경기→상세/스킬→종료→선택 변경→재개/새 게임을 확인했다. 기존 v2 상태 불변과 v3로 명시 재시작, 손상 선택 파일에도 경기 재개, Lv.3/장비3칸/주사위3개에서9기술 표시·색상·텍스트 높이·스냅샷/RNG 불변을 검사했다. 기존15개 계정 실패/오프라인/저장 복구/이동·주사위·부분 조합 연출 회귀도 함께 통과했다. 720×1280과720×1600 inset safe area를 사용했다.

검사는 격리된 메모리 백엔드와 고유 임시 저장을 사용했다. 실제 사용자 저장/DB를 읽거나 시험에 사용하지 않았다. 신규 화면과 실제 HTTP 서버의 전체 실행, Android 장치 검증, 장기 밸런스/부하 검증은 **NOT_RUN**이다. 실제 Firebase·C6 공동 경기 서버 정산/영구 보상은 범위 밖이며 새 경기의 영구 지급은 계속0이다. 기본 Mock 서버의 실행 상태/DB는 변경하지 않았다.

CLI status의 인스턴스 검색0과 달리 pipeline list 및 editor_status에서 기존 Editor PID44732/port7801의 연결을 확인해 같은 Editor만 사용했다. 첫 복합 어셈블리 필터는 검사0개여서 PASS로 세지 않았다. 네임스페이스 필터로 실제142개를 실행했고 EditMode→PlayMode 사이 도메인 새로고침 뒤 실제19개를 실행했다.

## 문서·검토·보존

GDD/README/CodeMap 색인과 수정16개 C#의 1:1 요약, 직접 관계를 동기화했다. 신규 C#5개·콘텐츠1개에는 고유 meta를 추가했다. 원본 제공 기획서와 이전 완료 REPORT는 보존한다. 최종 검증 대상은 [소스 SHA256](Evidence/GodAdventurerGrowth/source-hashes.json)에 기록한다.

[모험가 선택](Evidence/GodAdventurerGrowth/growth-adventurers.png), [시작 예상값](Evidence/GodAdventurerGrowth/growth-preview.png), [레벨/장비 상세](Evidence/GodAdventurerGrowth/growth-level-equipment.png), [3주사위 기술](Evidence/GodAdventurerGrowth/growth-three-dice.png), [세로 safe area](Evidence/GodAdventurerGrowth/growth-safe-area.png)를 보존한다. 최초 캡처의 장비 효과 문구 중복과 카드 강조색·계정 보정 문구는 표시 보정1회로 수정했고 최종 캡처에서 잘림 없이 확인했다.

최초 검토1묶음은 domain/계정/저장/화면을 나눠 확인했다. 미해결 필수 Finding0. 최초 통합 이후 수정·재검증 **1/2회**(표시 보정)이며 도메인/저장 소스는 최초 통합 PASS 이후 변경하지 않아 해당 검사를 반복하지 않았다. 코드/문서 diff 검사는 통과했다. Unity가 작성한 빈 YAML 필드의 끝 공백은 해당 검사에서 제외했고 자산 원문을 수작업으로 정리하지 않았다. 감사 도구의 CRLF GUID 검색과 문서 EOF/결과파일 생성 순서를 보정한 뒤 최종 감사를 통과했다. 새로 결정받아야 할 필수 항목은 없다.
