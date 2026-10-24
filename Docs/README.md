# Dice Warrior 문서

현재 메인은 **고정12지명·인간1+AI3의 공동 모험**이다. 기존1인 모험판은 제거하고 전투·계정·과거 서버 기록에 필요한 공용 코드만 보존한다. 새 경기 순위는 시험 결과이며 영구 재화는 지급하지 않는다.

**v5 반영 상태(2026-10-24):** 규칙·저장·화면·프리팹과 Title 연결을 완료했다. 새 게임은 v5를 사용하며 기존 v1~v4 경기는 당시 규칙으로 이어간다. .NET184개·Unity EditMode184개·신규 화면9개·기존 화면25개가 모두 통과했다. [검증 결과와 화면](Reports/V5_DICE_COMBAT_09_REPORT.md)을 참고한다.

- [현행 기획 v0.3](Dice_Warrior_GDD_v0.3.md): 게임 규칙과 시험 수치.
- [v5 주사위 전투 PLAN](Plans/V5_DICE_COMBAT_09_PLAN.md) / [REPORT](Reports/V5_DICE_COMBAT_09_REPORT.md): 굴림·보관·리롤·실행, 직업 전용면/꽝, 예상 기술·6면 전개도. 자산 연결과 실제 검증 상태는 REPORT를 따른다.
- [B0/C7 시뮬레이션 PLAN](Plans/BALANCE_SIMULATION_07_PLAN.md) / [REPORT](Reports/BALANCE_SIMULATION_07_REPORT.md): v4 규칙을 변경하지 않는 순수 .NET 기준선 실험·결정적 재현·통계/미계측 보고.
- [Q0/Q1 검증 PLAN](Plans/NEXT_PHASE_06_VALIDATION_BALANCE_GOD_POWER_PLAN.md) / [REPORT](Reports/NEXT_PHASE_06_VALIDATION_BALANCE_GOD_POWER_REPORT.md): 최종 Unity 회귀와 격리 Mock HTTP·v4 재개/권한 경계 검증.
- [태그·6스탯·속성 PLAN](Plans/TAG_STAT_ELEMENT_05_PLAN.md) / [REPORT](Reports/TAG_STAT_ELEMENT_05_REPORT.md): v4 출처 보존 태그·6스탯·4피해타입·상성·UI/AI 연결.
- [모험가 성장 PLAN](Plans/GOD_ADVENTURER_GROWTH_04_PLAN.md) / [REPORT](Reports/GOD_ADVENTURER_GROWTH_04_REPORT.md): v3 RGB 전투·기본 제공 모험가3종/패시브·런 성장·선택/능력치 상세.
- [종료·새 게임·부분 조합 PLAN](Plans/SHARED_WORLD_FLOW_COMBO_03_PLAN.md) / [REPORT](Reports/SHARED_WORLD_FLOW_COMBO_03_REPORT.md): 진행 보존, 경고 후 새 경기, 3주사위의 2눈 조합과 남은 기본기.
- [메인 전환 PLAN](Plans/SHARED_WORLD_MAIN_02_PLAN.md) / [REPORT](Reports/SHARED_WORLD_MAIN_02_REPORT.md): 공동 모험 직접 진입·로비·구형 모험판 제거·이전 기록 보존.
- [공동 세계 PLAN](Plans/SHARED_WORLD_COMPETITION_01_PLAN.md): 승인된 C0~C5·경기/AI/저장/화면 계약과 보호 범위.
- [공동 세계 REPORT](Reports/SHARED_WORLD_COMPETITION_01_REPORT.md): 실제 검증·실행 방법·C6/C7 후속 경계.
- [교체 PLAN](Plans/SYMBOL_LOOP_REPLACEMENT_PLAN.md): 정리 대상·보호 대상·구현 순서·완료 기준.
- [교체 REPORT](Reports/SYMBOL_LOOP_REPLACEMENT_REPORT.md): 실제 진행과 검증 상태.
- [화면 PLAN](Plans/SYMBOL_LOOP_PRESENTATION_PLAN.md): 칸별 이동·입체 주사위·스킬 연출 계약.
- [화면 REPORT](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md): 화면 구현과 실제 검증 증거.
- [백엔드 PLAN](Plans/SERVER_BACKEND_FOUNDATION_PLAN.md): P0+P1 권한 분리와 확정된 정산 정책.
- [백엔드 REPORT](Reports/SERVER_BACKEND_FOUNDATION_REPORT.md): Mock·저장 복구·검증 증거와 후속 범위.
- [재현 검증 PLAN](Plans/SERVER_RUN_REPLAY_VALIDATION_PLAN.md): P2 로그 재생·새 제출 지급·구형 대기 보존 계약.
- [재현 검증 REPORT](Reports/SERVER_RUN_REPLAY_VALIDATION_REPORT.md): 골든 벡터·악성 입력·중복 지급 검사 증거.
- [정산 안전성 PLAN](Plans/SERVER_SETTLEMENT_SAFETY_PLAN.md): P3 중복 정산·비동기 복구·동시 세션 계약.
- [정산 안전성 REPORT](Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md): 실제 동시 실행·과거 백업·응답 유실 복구 증거.
- [독립 서버 PLAN](Plans/STANDALONE_MOCK_SERVER_01_PLAN.md): 승인된 M0~M3 서버 분리·SQLite·HTTP 계약.
- [독립 서버 REPORT](Reports/STANDALONE_MOCK_SERVER_01_REPORT.md): 프로세스 간 정산·복구·Unity 연동 증거.
- [M4 계측 PLAN](Plans/STANDALONE_MOCK_METRICS_04_PLAN.md): 로컬 실측·오프라인 비용·대시보드 계약.
- [M4 계측 REPORT](Reports/STANDALONE_MOCK_METRICS_04_REPORT.md): 실제 검증·가격 근거·화면·내보내기 증거.
- [서버 실행 안내](../Server/README.md): 실행 명령, Unity 접속 설정, API와 저장 경계.
- [로컬 서버 실행 도우미 PLAN](Plans/LOCAL_SERVER_LAUNCHER_08_PLAN.md) / [REPORT](Reports/LOCAL_SERVER_LAUNCHER_08_REPORT.md): 프로젝트 최상위 CMD 더블클릭 실행과 격리 검증.
- 서버 이전 원안: 사용자가 제공한 `Dice_Warrior_Standalone_Mock_Server_Firebase_Migration_Plan_v0.1.md`를 참고했다. M0~M3와 추가 승인된 M4는 각각의 PLAN에 기록하고 M5 규모 부하 시험은 후속으로 둔다. 원본 파일은 사용자 변경 제외 요청에 따라 로컬에 보존한다.
- [현행 CodeMap](CodeMap/INDEX.md): 현재 소스의 역할과 직접 관계.
- [v1 이력](Archive/v1/README.md): 폐기된 규칙과 과거 검증. 현행 요구사항이 아니다.
- [서드파티 출처](ThirdParty/PRETENDARD.md): 유지하는 폰트 라이선스.

2026-10-20 사용자 지시로 Archive/v1의 이전 구조와 호환성 P0는 중단했다. 이는 현재 보존하는 SymbolLoop 규칙/정산 v1과 별개다. 과거 테스트 통과는 새 게임의 검증 근거로 사용하지 않는다.

실행: 프로젝트 최상위 `Start-MockServer.cmd`를 더블클릭해 서버를 켜고 실행 창을 유지한 뒤 `Assets/_Project/Scenes/Title.unity`를 열고 Play. 기존 PowerShell 명령 `./Server/Start-MockServer.ps1`도 사용할 수 있다. 공동 모험 로비에서 출전을 고른 뒤 경기를 시작하거나 이어간다. 이전1인 모험 시작/복귀 화면은 제공하지 않는다.
`모험 종료`는 진행을 보존하고 로비로 돌아간다. `경기 이어하기`로 같은 위치·사건을 재개한다. 별도의 `새 게임`은 기존 경기가 있으면 초기화 경고를 표시하며, 확인 후 새 저장에 성공해야 교체한다. 영구 재화·성장은 유지하고 이전 진행 원문은 복구 사본에 남긴다.
v4와 새 v5는 힘·민첩·체질·지능·정신·행운과 물리·화염·냉기·번개를 사용한다. 모험가3종과 기존 계정 HP 보정을 유지하고 HP는 `30+체질×6+계정보정`으로 계산한다. `능력치 상세`에서 스탯·장비·태그 출처를 확인한다. 영구 성장 구매/프로필 확인은 `계정 관리`에 있다.
v5 전투는 최초 굴림 후 주사위를 탭해 보관/해제하고 남은 주사위만1회 리롤한 뒤 실행한다. 전용면과 꽝을 포함한 정확한6면을 상세창에서 확인하며, 꽝만 나와도 실행할 수 있다. 3눈 우선, 없으면2눈 하나와 남은단일로 조합한다. v1~v4는 당시 판정·화면을 보존하므로 v5 적용은 경고 후 `새 게임`으로 한다. 출전 모험가 선택은 별도 파일에 저장하며 진행 중 경기에는 소급 적용하지 않는다.
이번은 1차 플레이 개편이다. 신격·영구 권능 투자·업적 해금·공동 경기 서버 검증(C6)은 후속이며 새 경기의 영구 보상 지급은 계속 0이다.
기본 서버는 `http://127.0.0.1:5087`, 권한 DB는 `Server/data/authority.db`다. 새 체크포인트는 `persistentDataPath/symbol-loop-backend-v1/http-mock/<endpoint+계정 SHA256>/checkpoint.json`이다. 이전 `mock/checkpoint.json`·`authority.json`·`symbol-loop-v1/player.json`은 보존하고 자동 이관하지 않는다.
공동 세계는 위 계정별 폴더 아래 `shared-world-preview-v1/checkpoint.json`에 별도 저장한다. 프로필 조회·영구 성장 구매·기존 미결 복구는 기존 서버를 사용하고 새 경기/AI 명령·순위는 로컬 기록이다. 새 경기 서버 재현·정산 C6과 대량 시뮬레이션 C7은 아직 포함하지 않는다.
이전1인 active run은 기록만 보존하고 해당 계정의 영구 성장 구매를 보류한다. 공동 모험 출전 선택/시작/재개는 가능하다. 서버 정산 전까지 공동 모험의 순위로 성장 재화를 지급하지 않는다.
이전 프로젝트와 달리 Title 한 씬에서 전체 여정을 진행한다. 현재 화면/수치는 MVP이며 최종 아트·밸런스·장치 검증은 별도다.

## 게임플레이 체크포인트 (44e6c31)

- 기존 카드·족보·분기 지도 코드를 기호 조합 자동 전투와 3지역 순환 캠페인으로 교체했다.
- 로비 준비, 12칸 이동·사건·선택, 주사위 1→2→3개 성장, 최종 보스, 정산과 새 저장을 연결했다.
- 말의 칸별 이동, 숫자/기호 주사위 회전, 조합/기본기·피해/보호막·반격 연출을 구현했다.
- 완료된 검사 기록은 규칙·저장 21/21, 화면 4/4, 최종 이동/배치 각 1/1이다. [구체적인 증거와 미검증 범위](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md#사용자-승인-커밋-체크포인트)를 함께 보존한다.

[실제 화면 미리보기](Reports/Evidence/SymbolLoop/gameplay-preview.gif)

## 보존하는 서버 계약: 독립 Mock 서버와 M4 계측

아래는 기존 v1 런과 계정을 위한 서버 계약이다. 공동 모험은 프로필·성장·미결 복구를 사용하고 신규 경기의 시작/정산은 아직 서버에 제출하지 않는다. v1 런 시작·성장은 독립 Mock 백엔드가 승인하며, 새 테스트 계정에는 20재화를 최초 1회 지급한다. P2의 새 제출은 저장된 시작 티켓과 명령 로그를 독립 상태에 재현한 뒤 `완료 지역×10 + 최종 승리 20`을 한 번 지급한다. 승리·패배·포기 모두 같은 산식을 사용한다. 검증이 거절되면 영구 재화와 활성 런을 유지한다.

사용자 결정에 따라 **이미 제출된 P1 검증 대기 기록은 지급 0으로 보존**한다. 기존 미제출 런의 정상 로그는 새 제출 시 검증한다. 고정 시드 4개의 121명령과 단계별 상태는 향후 검증기 이식의 비교 기준이다.

P3는 통신 단절·응답 유실·취소 뒤에도 같은 요청을 보존해 재확인한다. 과거 로컬 백업은 서버 영수증과 최신 재화로 복구하며 교체되는 진행 원문을 별도로 보존한다. 동시 정산은 한 번만 지급하고, 같은 revision의 성장/정산 경합에서는 한 변경만 승인한다. 늦은 응답은 최신 런을 덮지 못한다. [P3 실제 검증과 한계](Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md)를 따른다.

P3의 프로세스 내부 검증 이후, M0~M3에서 권한 서비스·검증기를 Unity Assets 밖 `Server/`로 옮기고 SQLite와 실제 HTTP를 연결했다. Unity는 로컬 진행을 처리하며 프로필·시작·정산·성장을 서버에서 확인한다. 서버 미응답 시 로컬 지급으로 우회하지 않는다. 같은 DB를 여는 두 서버 프로세스의 100동시 정산과 10구매 경합, 응답 유실·서버 재시작 복구를 검사한다. 구체적인 최신 결과는 독립 서버 REPORT를 따른다.

M4는 실제 HTTP/SQLite·재현 시간·프로세스/파일 측정과 가상 Firestore 비용을 분리한 로컬 대시보드를 추가했다. 서버 실행 후 `http://127.0.0.1:5087/dev/dashboard`에서 조회한다. 서울 Standard/USD·무료 미차감이 기본이며 DAU/빈도/보관/CPU·메모리 가정을 바꾸고 JSON/CSV로 내보낼 수 있다. 사용자/런은 가명 키로만 보관한다. 미산정·기록 누락을 무료0으로 표시하지 않는다. [실행과 비용 가정](../Server/README.md#m4-계측과-비용-대시보드)을 참고한다.

서버 계측 구현은 **M4까지**다. 이후 게임은 C0~C5와 공동 모험 메인 전환을 진행했다. M5 규모별 부하 시험, 실제 Firebase·두 기기·Android 검증은 후속이다. 개발 계정 헤더는 loopback/Development에서만 허용하며 운영 인증이 아니다.
