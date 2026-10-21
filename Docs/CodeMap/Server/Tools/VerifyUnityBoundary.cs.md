# VerifyUnityBoundary.cs

- 역할: Unity 테스트 DLL 배포 경계와 서버/Config 콘텐츠 동치 확인 및 HTTP 시험 주소 설정.
- 핵심 동작: Editor 전용·명시 참조·Android 비호환·플레이어 어셈블리 참조 부재를 검사한다. Config 하나를 서버 JSON과 비교하고 공유 JSON 어셈블리/규칙 hash를 반환한다.
- 입출력: PluginImporter/CompilationPipeline/Config/서버JSON/Temp 시험 주소→검사 결과와 현재 Editor 프로세스 환경 변수. ClearTestContext는 변수만 제거한다.
- 사용하는 대상/쪽: AssetDatabase/CompilationPipeline/LoopJson; Unity CLI run_script 및 통합 검증 절차.
- 관계 근거: Unity 자체가 생성하는 플레이어 compilation graph로 권한 DLL 누출을 확인한다. 런타임 객체/Scene 작성은 없다.
- 상태/수명: 한 번의 검증과 시험 주소의 일시적 프로세스 설정. 테스트 종료 때 ClearTestContext를 실행한다.
- 검수: 실제 Android 빌드/IL2CPP 실행이 아니며 계정/기존 저장을 읽지 않는다.
