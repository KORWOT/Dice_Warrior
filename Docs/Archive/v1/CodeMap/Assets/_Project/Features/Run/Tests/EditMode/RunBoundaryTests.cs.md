# RunBoundaryTests.cs

- 역할: RA-B의 단일 상태 소유, 저장 확정 순서, 명령 토큰, 누적 시간, 후보 검증을 검사하는 EditMode NUnit 테스트다. 매개변수 확장 21개이며 실제 실행 결과는 메인의 ROGUELIKE_ARCHITECTURE_REPORT를 따른다.
- 입력/출력: PrototypeAuthoring.CreateDefaults와 실제 RunSession 명령으로 유효한 맵·탐험 후보·전투 후보·보상·상점 상태를 만든다. 필요한 재굴림 권한과 이전 기록은 독립 RunState 입력에 준비하며 실행 세션을 외부에서 변경하지 않는다.
- 소유권: 생성자 입력, State/ReadSnapshot 출력의 HP·중첩 설정/가중치/태그·노드/선택 노드·후보 카드·보상·장비/주사위·최근 기록 배열의 수정이 확정 상태를 바꾸지 않는지 전체 JSON으로 비교한다. New는 설정/이전 결과를 복제하고 주입 저장소에 초기 상태를 1회 저장한다.
- 저장 실패: 재굴림·행동·보상·상점 구매 각각 저장 예외 전후의 전체 JSON(시간 포함), 저장 내용, HP/자원/처리 ID/제시 ID/RNG를 확인한다. pending 시간은 실패 후 유지하고 같은 토큰 재시도로 정확히 1회 확정한다. 독립 oracle에서 동일 ID·명령을 실행해 성공 후 전체 상태와 비교한다.
- 저장 경계: callback은 확정 전 이전 상태를 볼 수 있고 후보 복사만 받는다. callback이 전달 DTO를 보관/변경해도 세션 메모리에는 별칭이 없어야 한다. 악의적 저장소가 자기 저장 데이터를 변경한 경우 디스크 내용까지 자동 복구한다는 계약은 검사하지 않는다. 재진입 명령/SaveCheckpoint는 false이며 외부 명령 1회만 진행한다.
- 토큰/검증: 다른 run ID와 이전 sequence를 거절한다. 맵 단계가 계속 일치해도 오래된 cap 입력은 저장/RNG를 진행하지 않는다. 유효 sequence=int.MaxValue의 증가로 무효 후보가 되면 RunStateValidationException을 저장 전 발생시켜 원본을 유지한다. 잘못된 생성자 입력도 저장 호출 전 거절한다.
- 시간: 100회 frame-style RecordElapsed 입력은 저장/RNG/sequence 없이 표시 시간에 누적된다. 명시 checkpoint의 실패/재시도, clean 재저장 생략, 음수/NaN/무한값 거절, Result 시간 중단을 확인한다. 시간 flush만으로 기존 command token이 오래된 것으로 바뀌지 않는다.
- 직접 사용하는 대상: RunSession/RunState/RunCommandToken/RunStateValidationException/IRunStore, LocalRunStore, PrototypeAuthoring, JsonUtility, NUnit, System.IO. ProbeStore는 저장 장애/별칭/재진입만 대체하며 규칙을 대신 계산하지 않는다.
- 사용하는 쪽/관계 근거: Unity EditMode Runner가 Test/TestCase attribute로 실행한다. 제품 코드는 테스트를 참조하지 않는다. 기존 EditMode asmdef 경계를 사용한다.
- 상태/수명: 디스크 왕복은 고유 Temp/FateDiceBoundaryTests 경로만 사용하고 정규화한 자기 루트 아래만 정리한다. 다른 테스트는 메모리 저장소다. 기본 사용자 저장·SO·Scene·Prefab·설정을 수정하지 않는다.
- 검수 주의점: 위임 에이전트는 staging 작성만 수행했고 Unity/Git 실행은 메인 담당이다. 테스트 개수나 정적 확인은 실행 PASS 증거가 아니다.

## RA-C 시간 경계
ProbeStore와 준비용 Copy는 RunState.DeepCopy를 사용한다. 인메모리 소유권 경계에 파일 파서의 double 반올림을 섞지 않으며 기존 모든 전체상태/실패/시간 assertion을 유지한다. 실제 파일의 시간 bit 보존은 CoreBoundaryTests가 별도로 검사한다.
