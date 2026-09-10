# 캠페인·연출 작업 상태 커밋

2026-09-11. 사용자 요청에 따라 main의 d8df499 이후 누적 구현을 로컬 커밋으로 기록한다. 이 기록은 기능 미해결 항목을 완료로 바꾸는 판정이 아니다. 게임 코드·자산을 추가 수정하거나 원격 push하지 않는다.

## 포함 내용
- 주사위6개 한 행, 조합별 색·오라·등급별 연출, Text Animator/FEEL/All In 1 연동.
- 연출 시작/종료 소유권·취소 복원과 실시간10초 watchdog, 게임 명령 재실행 없는 실패 복구.
- 절차적 분기·합류 지도, 공개 노드 정보와 저장 좌표 유지, 별도 운명 선택 팝업.
- 현재 노드 복귀 초점, 노드 탭 즉시 이동, 전투 진입 연출 종료 후 주사위 창.
- 관련 테스트·CodeMap/INDEX/GDD·작업 PLAN/REPORT와 TMP Essential Resources/폰트 라이선스.
- 기존 사용자 승인으로 저장된 Title의 EventSystem 추가도 현재 상태로 포함한다. 이번 커밋 단계에서 해당 씬을 수정한 것은 아니다.

## 검사 상태
직전 실제 실행 artifacts/campaign-flow-polish/play-final.json은 전체 PlayMode196개 중189PASS/7FAIL, 신규 CampaignFlowPolishTests11개 모두PASS다. 이번 커밋 단계에서 Unity 검사를 다시 실행하거나 실패를 숨기지 않는다.

남은7개: Title/UIRoot EventSystem 중복에 따른 기존6개, LobbyCampaignFlowTests.cs:72의 이전 layoutVersion2 기대값1개(CFP-T02). 실제 원본은3이다. 추가 보완 승인 요청 이후 사용자가 현재 상태 커밋을 우선 요청했으므로 기대값을 그대로 보존한다. 해당 구현의 보완 횟수2/2와 대기 상태도 유지한다.

## 커밋 경계
공급사 FEEL/Text Animator/All In 1 원본·샘플 및 로컬 artifacts는 제외하고 삭제하지 않는다. 필요한 버전과 Text Animator 기본 콘텐츠는 [의존성 안내](../ThirdParty/DICE_PRESENTATION_DEPENDENCIES.md)에 기록했다. 제외한 패키지가 없는 새 checkout의 컴파일 성공을 주장하지 않는다. 작업과 무관한 사용자 루트 설계 문서2개 및 로컬 도구 CodeMap은 그대로 둔다.

정확한 경로 명세와 Git 실행 영수증은 artifacts/campaign-effects-commit/에 보관한다. 시작 index는 비어 있고 개별 파일 pathspec만 stage한다. index 경로 일치·공백·JSON/CodeMap/meta·LFS 확인 후 commit하며 최종 식별자는 이 문서를 포함하는 Git 로그에서 확인한다. push/브랜치 전환·기존 파일 reset/stash 없음.

커밋 전 확인: 고정250경로와 실제 index 일치. 변경 JSON/asmdef 유효, 프로젝트 C#115개의 CodeMap/meta 누락0. 새 폰트의 staged LFS pointer SHA256/size는 실제 원본과 일치하고 `git lfs fsck --objects` PASS다. `--pointers`는 기존 HEAD의 Assets/TutorialInfo/Icons/URP.png가 일반 Git blob인 기존 예외1개를 보고했다. 이 파일을 바꾸거나 이번 커밋에 넣지 않는다.

소스·문서 공백 검사의 CodeMap3개 EOF 빈 줄만 제거했으며 본문은 보존했다. Unity Editor가 직렬화한 YAML 빈 필드 뒤 공백127개는 원본 형식으로 남겼다. 직전 기능 검증 이후 게임 C#·자산은 변경하지 않았으므로 커밋 정리로 실제 기능 검사 결과를 덮어쓰지 않는다. 기본 에디터 설정 JSON2개·별도 사용자 설계 문서2개·로컬 도구 CodeMap5개는 미포함 파일로 유지한다.
