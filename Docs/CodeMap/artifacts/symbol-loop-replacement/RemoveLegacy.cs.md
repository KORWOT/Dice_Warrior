# RemoveLegacy.cs (실행 산출물)

- 원본: `Temp/symbol-loop-replacement/RemoveLegacy.cs`, 일회성 Editor run_script, 제품 assembly에 포함하지 않는다.
- 역할: 승인한 delete-roots.txt 14개 경로를 Editor API로 정리하고 Title을 새 구현 연결 전 빈 씬으로 만든다.
- 입출력: 검토 목록·현재 Editor/AssetDatabase → 삭제와 빌드 씬 목록. 392개 파일의 사전 사본은 before 아래.
- 관계: 기존 UIRoot의 구형 UIManager.prefabs 등록을 먼저 비운 뒤 모든 보존 Scene/Prefab/asset의 의존을 검사한다.
- 상태/수명: Play/compile/dirty 씬이면 중단, 절대 경로가 프로젝트 하위인지 확인, 메인 단일 호출.
- 검수: 1차 실행은 외부 참조 발견으로 삭제 전 중단, 직접 참조 해제 후 실행 성공. 새 실행 승인 없이 반복 삭제할 도구로 사용하지 않는다.
