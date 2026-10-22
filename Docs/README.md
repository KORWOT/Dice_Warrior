# Dice Warrior 문서

현재 게임은 **기호 조합 자동 전투**를 공유하는 기존 순환 캠페인과 **고정12지명·인간1+AI3 공동 세계 시험 모드**를 제공한다. 새 모드의 순위는 시험 결과이며 영구 재화는 지급하지 않는다.

- [현행 기획 v0.3](Dice_Warrior_GDD_v0.3.md): 게임 규칙과 시험 수치.
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
- 서버 이전 원안: 사용자가 제공한 `Dice_Warrior_Standalone_Mock_Server_Firebase_Migration_Plan_v0.1.md`를 참고했다. M0~M3와 추가 승인된 M4는 각각의 PLAN에 기록하고 M5 규모 부하 시험은 후속으로 둔다. 원본 파일은 사용자 변경 제외 요청에 따라 로컬에 보존한다.
- [현행 CodeMap](CodeMap/INDEX.md): 현재 소스의 역할과 직접 관계.
- [v1 이력](Archive/v1/README.md): 폐기된 규칙과 과거 검증. 현행 요구사항이 아니다.
- [서드파티 출처](ThirdParty/PRETENDARD.md): 유지하는 폰트 라이선스.

2026-10-20 사용자 지시로 Archive/v1의 이전 구조와 호환성 P0는 중단했다. 이는 현재 보존하는 SymbolLoop 규칙/정산 v1과 별개다. 과거 테스트 통과는 새 게임의 검증 근거로 사용하지 않는다.

실행: 저장소 루트 PowerShell에서 `./Server/Start-MockServer.ps1`로 서버를 켠 뒤 `Assets/_Project/Scenes/Title.unity`를 열고 Play. 로비에서 시작 주사위·무기를 고른 뒤 **공동 세계 시험 → 시험 경기 시작**을 누른다. 기존 1인 여정도 로비에서 계속 선택할 수 있다.
기본 서버는 `http://127.0.0.1:5087`, 권한 DB는 `Server/data/authority.db`다. 새 체크포인트는 `persistentDataPath/symbol-loop-backend-v1/http-mock/<endpoint+계정 SHA256>/checkpoint.json`이다. 이전 `mock/checkpoint.json`·`authority.json`·`symbol-loop-v1/player.json`은 보존하고 자동 이관하지 않는다.
공동 세계는 위 계정별 폴더 아래 `shared-world-preview-v1/checkpoint.json`에 별도 저장한다. 시작 성장 확인만 기존 서버를 사용하고 새 경기/AI 명령·순위는 로컬 기록이다. 새 경기 서버 재현·정산 C6과 대량 시뮬레이션 C7은 아직 포함하지 않는다.
이전 프로젝트와 달리 Title 한 씬에서 전체 여정을 진행한다. 현재 화면/수치는 MVP이며 최종 아트·밸런스·장치 검증은 별도다.

## 게임플레이 체크포인트 (44e6c31)

- 기존 카드·족보·분기 지도 코드를 기호 조합 자동 전투와 3지역 순환 캠페인으로 교체했다.
- 로비 준비, 12칸 이동·사건·선택, 주사위 1→2→3개 성장, 최종 보스, 정산과 새 저장을 연결했다.
- 말의 칸별 이동, 숫자/기호 주사위 회전, 조합/기본기·피해/보호막·반격 연출을 구현했다.
- 완료된 검사 기록은 규칙·저장 21/21, 화면 4/4, 최종 이동/배치 각 1/1이다. [구체적인 증거와 미검증 범위](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md#사용자-승인-커밋-체크포인트)를 함께 보존한다.

[실제 화면 미리보기](Reports/Evidence/SymbolLoop/gameplay-preview.gif)

## 현재 단계: 독립 Mock 서버와 M4 계측

런 시작·성장은 독립 Mock 백엔드가 승인하며, 새 테스트 계정에는 20재화를 최초 1회 지급한다. P2의 새 제출은 저장된 시작 티켓과 명령 로그를 독립 상태에 재현한 뒤 `완료 지역×10 + 최종 승리 20`을 한 번 지급한다. 승리·패배·포기 모두 같은 산식을 사용한다. 검증이 거절되면 영구 재화와 활성 런을 유지한다.

사용자 결정에 따라 **이미 제출된 P1 검증 대기 기록은 지급 0으로 보존**한다. 기존 미제출 런의 정상 로그는 새 제출 시 검증한다. 고정 시드 4개의 121명령과 단계별 상태는 향후 검증기 이식의 비교 기준이다.

P3는 통신 단절·응답 유실·취소 뒤에도 같은 요청을 보존해 재확인한다. 과거 로컬 백업은 서버 영수증과 최신 재화로 복구하며 교체되는 진행 원문을 별도로 보존한다. 동시 정산은 한 번만 지급하고, 같은 revision의 성장/정산 경합에서는 한 변경만 승인한다. 늦은 응답은 최신 런을 덮지 못한다. [P3 실제 검증과 한계](Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md)를 따른다.

P3의 프로세스 내부 검증 이후, M0~M3에서 권한 서비스·검증기를 Unity Assets 밖 `Server/`로 옮기고 SQLite와 실제 HTTP를 연결했다. Unity는 로컬 진행을 처리하며 프로필·시작·정산·성장을 서버에서 확인한다. 서버 미응답 시 로컬 지급으로 우회하지 않는다. 같은 DB를 여는 두 서버 프로세스의 100동시 정산과 10구매 경합, 응답 유실·서버 재시작 복구를 검사한다. 구체적인 최신 결과는 독립 서버 REPORT를 따른다.

M4는 실제 HTTP/SQLite·재현 시간·프로세스/파일 측정과 가상 Firestore 비용을 분리한 로컬 대시보드를 추가했다. 서버 실행 후 `http://127.0.0.1:5087/dev/dashboard`에서 조회한다. 서울 Standard/USD·무료 미차감이 기본이며 DAU/빈도/보관/CPU·메모리 가정을 바꾸고 JSON/CSV로 내보낼 수 있다. 사용자/런은 가명 키로만 보관한다. 미산정·기록 누락을 무료0으로 표시하지 않는다. [실행과 비용 가정](../Server/README.md#m4-계측과-비용-대시보드)을 참고한다.

이번 범위는 **M4까지**다. M5 규모별 부하 시험, 실제 Firebase·두 기기·Android 검증은 후속이다. 개발 계정 헤더는 loopback/Development에서만 허용하며 운영 인증이 아니다.
