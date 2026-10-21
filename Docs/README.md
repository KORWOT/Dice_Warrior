# Dice Warrior 문서

현재 게임은 **기호 조합 자동 전투 + 12칸 순환 캠페인**을 사용한다. 전면 교체 MVP에 칸별 말 이동·숫자/기호 주사위 회전·스킬/반격 연출을 연결하고 전용 검사와 실제 캡처로 확인했다.

- [현행 기획 v0.3](Dice_Warrior_GDD_v0.3.md): 게임 규칙과 시험 수치.
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
- [현행 CodeMap](CodeMap/INDEX.md): 현재 소스의 역할과 직접 관계.
- [v1 이력](Archive/v1/README.md): 폐기된 규칙과 과거 검증. 현행 요구사항이 아니다.
- [서드파티 출처](ThirdParty/PRETENDARD.md): 유지하는 폰트 라이선스.

2026-10-20 사용자 지시로 기존 v1 병행 모드와 호환성 P0는 중단했다. 과거 테스트 통과는 새 게임의 검증 근거로 사용하지 않는다.

실행: `Assets/_Project/Scenes/Title.unity`를 열고 Play. 로비에서 시작 주사위·무기를 고른 뒤 새 여정을 시작한다.
새 Mock 체크포인트는 `persistentDataPath/symbol-loop-backend-v1/mock/checkpoint.json`, 권한 프로필은 같은 폴더 `authority.json`을 사용한다. 기존 `symbol-loop-v1/player.json`은 보존하고 이관하지 않는다.
이전 프로젝트와 달리 Title 한 씬에서 전체 여정을 진행한다. 현재 화면/수치는 MVP이며 최종 아트·밸런스·장치 검증은 별도다.

## 게임플레이 체크포인트 (44e6c31)

- 기존 카드·족보·분기 지도 코드를 기호 조합 자동 전투와 3지역 순환 캠페인으로 교체했다.
- 로비 준비, 12칸 이동·사건·선택, 주사위 1→2→3개 성장, 최종 보스, 정산과 새 저장을 연결했다.
- 말의 칸별 이동, 숫자/기호 주사위 회전, 조합/기본기·피해/보호막·반격 연출을 구현했다.
- 완료된 검사 기록은 규칙·저장 21/21, 화면 4/4, 최종 이동/배치 각 1/1이다. [구체적인 증거와 미검증 범위](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md#사용자-승인-커밋-체크포인트)를 함께 보존한다.

[실제 화면 미리보기](Reports/Evidence/SymbolLoop/gameplay-preview.gif)

## 현재 단계: P3 정산·복구·동시 실행 안전성

런 시작·성장은 독립 Mock 백엔드가 승인하며, 새 테스트 계정에는 20재화를 최초 1회 지급한다. P2의 새 제출은 저장된 시작 티켓과 명령 로그를 독립 상태에 재현한 뒤 `완료 지역×10 + 최종 승리 20`을 한 번 지급한다. 승리·패배·포기 모두 같은 산식을 사용한다. 검증이 거절되면 영구 재화와 활성 런을 유지한다.

사용자 결정에 따라 **이미 제출된 P1 검증 대기 기록은 지급 0으로 보존**한다. 기존 미제출 런의 정상 로그는 새 제출 시 검증한다. 고정 시드 4개의 121명령과 단계별 상태는 향후 검증기 이식의 비교 기준이다.

P3는 통신 단절·응답 유실·취소 뒤에도 같은 요청을 보존해 재확인한다. 과거 로컬 백업은 서버 영수증과 최신 재화로 복구하며 교체되는 진행 원문을 별도로 보존한다. 동시 정산은 한 번만 지급하고, 같은 revision의 성장/정산 경합에서는 한 변경만 승인한다. 늦은 응답은 최신 런을 덮지 못한다. [P3 실제 검증과 한계](Reports/SERVER_SETTLEMENT_SAFETY_REPORT.md)를 따른다.

이번 작업은 **P3에서 종료**한다. 동시성 검증은 같은 프로세스 안의 두 독립 로컬 세션과 공유 Mock 권한 저장소를 대상으로 한다. 별도 프로세스의 동일 파일 공유, 실제 두 기기·Firebase(P4)·Android 기기(P5)는 미구현/미검증이며 로컬 Mock은 운영 보안 서버가 아니다.
