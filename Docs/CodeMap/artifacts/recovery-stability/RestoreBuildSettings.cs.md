# RestoreBuildSettings.cs

- 역할: InputSystem 빌드 후 메모리에서 복구됐지만 파일에 남은 preloadedAssets 변경을 작업 시작 상태로 복원한다.
- 입출력: 명시 ProjectSettings.asset, 시작 SHA256과 일치하는 사본, 검사한 빌드 후 SHA256 → Editor FileUtil.ReplaceFile → 원본 해시/메모리 빈 목록/실제 빌드 옵션.
- 관계: PlayerSettings/BuildReport/FileUtil만 사용한다. 전역 AssetDatabase 저장·설정 정책 변경·Git 쓰기는 없다.
- 수명/검수: 빌드·컴파일·Play 중에는 거부하고 메모리 원상 복구 이후만 실행한다. 현재 bytes 또는 원본 사본이 달라지면 쓰지 않는다. Runtime 소스가 아닌 일회성 보존 도구다.
