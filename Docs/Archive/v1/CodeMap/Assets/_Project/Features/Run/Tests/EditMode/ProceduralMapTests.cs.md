# ProceduralMapTests.cs

- 역할: 절차 지도 버전1의 생성·저장·검증·명령 원자성 계약을 실제 Core/Runtime 경계에서 검사하는 EditMode fixture. 신규 공개 필드는 reflection으로 조회해 구현 이전에도 컴파일하며 필수 계약 부재를 명시 FAIL로 확인한다.
- 입력/출력: PrototypeAuthoring의 독립 설정을 버전1로 바꾸고 여러 seed/1·2·10·100 일반층을 생성한다. 성공 그래프는 층·lane·보스·도달성·교차·가변 분기/합류를 검사한다. 실패는 설정·그래프 변조·checkpoint IO 실패를 주입한다.
- 직접 사용하는 대상: RunSession/RunState/RunStateValidator, DiceRules.Roll, PrototypeAuthoring, LocalRunStore, JsonUtility, NUnit. 반환값·상태와 고유 임시 저장 파일만 관찰하며 실제 사용자 슬롯을 사용하지 않는다.
- 사용하는 쪽: Unity Test Runner의 FateDice.Tests.ProceduralMapTests 필터. 메인 에이전트만 Editor 실행을 소유한다.
- 상태/수명: 각 테스트는 독립 설정·런을 생성한다. WithStore는 OS 임시 경로 아래 고유 GUID 폴더를 만들고 해당 폴더만 finally에서 정리한다.
- 검수 주의: Shape는 runId를 제외한 노드 좌표·유형·간선을 비교한다. 생성기 자체 도우미를 기대값으로 쓰지 않는다. SmallMap의 A→C/B→D→Z 수기 fixture로 교차/잘못된 층/진척을 검증한다. schema1 누락 필드 시험은 체크섬을 포함한 실제 구형 envelope를 작성한다.
- 실행 상태: 메인의 artifacts/procedural-campaign/core-red.json에서 신규 mapGenerationVersion 부재로 26/26 FAIL을 확인한 뒤 생산 구현했다. GREEN 실행 대기이며 이 문서는 실행 PASS의 증거가 아니다. 실제 결과는 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
