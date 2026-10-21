# Dice Warrior 문서

현재 게임은 **기호 조합 자동 전투 + 12칸 순환 캠페인**을 사용한다. 전면 교체 MVP에 칸별 말 이동·숫자/기호 주사위 회전·스킬/반격 연출을 연결하고 전용 검사와 실제 캡처로 확인했다.

- [현행 기획 v0.3](Dice_Warrior_GDD_v0.3.md): 게임 규칙과 시험 수치.
- [교체 PLAN](Plans/SYMBOL_LOOP_REPLACEMENT_PLAN.md): 정리 대상·보호 대상·구현 순서·완료 기준.
- [교체 REPORT](Reports/SYMBOL_LOOP_REPLACEMENT_REPORT.md): 실제 진행과 검증 상태.
- [화면 PLAN](Plans/SYMBOL_LOOP_PRESENTATION_PLAN.md): 칸별 이동·입체 주사위·스킬 연출 계약.
- [화면 REPORT](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md): 화면 구현과 실제 검증 증거.
- [현행 CodeMap](CodeMap/INDEX.md): 현재 소스의 역할과 직접 관계.
- [v1 이력](Archive/v1/README.md): 폐기된 규칙과 과거 검증. 현행 요구사항이 아니다.
- [서드파티 출처](ThirdParty/PRETENDARD.md): 유지하는 폰트 라이선스.

2026-10-20 사용자 지시로 기존 v1 병행 모드와 호환성 P0는 중단했다. 과거 테스트 통과는 새 게임의 검증 근거로 사용하지 않는다.

실행: `Assets/_Project/Scenes/Title.unity`를 열고 Play. 로비에서 시작 주사위·무기를 고른 뒤 새 여정을 시작한다.
새 저장은 `persistentDataPath/symbol-loop-v1/player.json`을 사용하며 기존 v1 저장은 읽거나 이관하지 않는다.
이전 프로젝트와 달리 Title 한 씬에서 전체 여정을 진행한다. 현재 화면/수치는 MVP이며 최종 아트·밸런스·장치 검증은 별도다.

## 현재 체크포인트

- 기존 카드·족보·분기 지도 코드를 기호 조합 자동 전투와 3지역 순환 캠페인으로 교체했다.
- 로비 준비, 12칸 이동·사건·선택, 주사위 1→2→3개 성장, 최종 보스, 정산과 새 저장을 연결했다.
- 말의 칸별 이동, 숫자/기호 주사위 회전, 조합/기본기·피해/보호막·반격 연출을 구현했다.
- 완료된 검사 기록은 규칙·저장 21/21, 화면 4/4, 최종 이동/배치 각 1/1이다. [구체적인 증거와 미검증 범위](Reports/SYMBOL_LOOP_PRESENTATION_REPORT.md#사용자-승인-커밋-체크포인트)를 함께 보존한다.

[실제 화면 미리보기](Reports/Evidence/SymbolLoop/gameplay-preview.gif)
