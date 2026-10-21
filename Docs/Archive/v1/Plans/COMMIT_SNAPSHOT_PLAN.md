# COMMIT_SNAPSHOT_PLAN

상태: DOCUMENT / COMPLETE (commit-ready). 사용자 2026-09-08 누적 내용 정리와 Git 커밋 요청.

- 목표: 초기 체크인 이후 승인된 게임 구현·UI/씬/로비/한글화/작업실/피드백 및 설치된 프로젝트 지원 파일을 현재 main에 한 개의 일관된 로컬 커밋으로 기록한다.
- 기준: D:/UnityProject/Dice_Warrior, main, 433edddc872433ead6db52a872ea0fcac659d92e. 시작 시 index 비어 있음.
- 포함: Assets/_Project와 meta, Pretendard 배포용 StreamingAssets와 meta, Packages manifest/lock, 씬 목록과 현재 프로젝트/렌더링 설정, 완료된 HubForceResolve 삭제, .agents 공식 CLI/Pipeline 문서, AGENTS, GDD/CodeMap/기존 PLAN·REPORT/아트 참고/폰트 출처. 기존 프로젝트 식별자와 패키지 define을 포함하는 설정도 현재 상태로 보존한다.
- 제외: 기본 값만 담은 AI Assistant/Code Coverage 에디터 Settings.json 2개와 원본 bytes가 같은 URP.png의 기존 LFS clean 표시 차이. 이 파일들은 삭제·복원하지 않는다. Library/Temp/Logs/UserSettings/빌드/기본 저장·로컬 실행 증거는 추가하지 않는다.
- 정확한 파일 명세: Codex 작업 폴더 artifacts/commit-snapshot/manifest.txt. 모든 staging은 이 개별 경로의 NUL pathspec을 사용하고 실제 index 목록과 완전 일치를 확인한다. git add -A/commit -a를 사용하지 않는다.
- 쓰기 범위: Docs/Plans/COMMIT_SNAPSHOT_PLAN.md, Docs/Reports/COMMIT_SNAPSHOT_REPORT.md 및 승인된 Git index/objects/로컬 main commit. 게임 소스·자산·패키지·설정의 의미적 수정, 브랜치 전환, push/merge 없음.
- 검증: 현재 Editor 전체 PlayMode/EditMode, 범위 내 코드/설정/자산 bytes와 저장의 테스트 전후 일치, C# 68개 대응 CodeMap/meta, manifest/lock JSON, index 공백 및 LFS 객체, 커밋 후 경로 목록/index/HEAD 확인. 신규 기능 검토는 재개방하지 않는다.
- 위임: 기존 보조 1명만 읽기 전용으로 커밋 포함/제외와 폰트 라이선스 확인. Main만 Unity 실행·문서·Git 쓰기. 동시 2, 누적 위임 1, 재귀 0.
- 종료: 허용 파일만 commit되고 요약/검증/제외 파일이 보고되면 완료. 실패 시 실제 사유 기록. 새 기능 수정·검토 예산은 재개방하지 않는다.
최종 정리: PlayMode 77/77, EditMode 128/128, 후보 파일/기본 저장 무변경 확인. 문서 작업 완료 및 커밋 실행 준비. 최종 commit 성공/식별자와 index 확인은 Git 로그 및 artifacts/commit-snapshot의 실행 영수증으로 확인한다.

커밋 공백 검사 보정: 기존 C#/CodeMap 50개에서 EOF 빈 줄만 제거했다. 정확한 경로와 원본은 artifacts/commit-snapshot/eof-only-proof.json 및 eof-backup에 기록했다. 파일 본문의 모든 byte와 기존 줄바꿈 형식은 보존했고 최종 줄바꿈 1개만 남겼다. 기능 수정·일반 재검토는 아니다.
