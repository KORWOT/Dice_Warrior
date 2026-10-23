# 최종 Q1 · R3 PASS

사용자가 승인한 추가2회 중1회를 사용한 결과다. 기존 실패 결과는 상위 Q1 디렉터리에 보존한다.

- `R3-*-final.json`: 신규 경로 코드의 컴파일과 journey/profile/legacy/prepare/resume 각각1/1 PASS.
- `j`: 일반 v4 경기45명령·전원완료/전투 재개·네 피해 타입·서버 프로필 불변과 화면. `metrics-after.json`은 이 시나리오만 끝난 시점(profile7회, 쓰기0)이다.
- `p`: before-abort와 after-delay 이후 기존 진행 보존·명시 재조회 회복.
- `l`: 구형 active/미결 정산 보존, 성장 보류. 유일한 start POST는 기존 v1 기록 준비용이다.
- `r`: 같은 계정/폴더/endpoint/DB에서 Prepare→서버 재시작→Resume. 네 파일 해시/pending/revision/동일 본문 재전송·성장1회 적용 증거.
- `final-metrics.json`, `dashboard-overview.*`: 전체4시험 계정의 실제25요청과 가상 Firestore 모델. 성장 POST2회는 최초승인+동일요청중복1회다. 제출명령/로그0이다. 비용은 실제 Firebase 청구가 아니다.
- `path-budget.json`: 각 사례에서 account183자, 경기207자, 실제 저장/보존 접미사까지 최대259자(<260)를 확인한다.
- `q1-server.ps1.txt`, `unity-check.ps1.txt`: 재현 스크립트 사본. 프로젝트 `artifacts/next-phase-06/`에 .ps1로 두어 사용한다. 서버는 새로운 짧은 q1 root/임시포트/DB를 만들며 기존 사례 폴더는 재사용하지 않는다. Q1은 개별 exact name에 `--include_explicit true`가 필요하다. 독립 검사 사이 검색이0개면 Editor 도메인을 재로드하고 실제1개 실행을 확인한다.

현재 시험 서버와 환경변수는 정리됐다. 서버를 재시작할 때는 같은 state의 restart를 사용한다. 새로운 전체 검증은 start로 새 DB/root를 만들어야 하며 기존 실제 서버5087이나 사용자 저장을 사용하지 않는다. 이번 통과가 Android 실기기/Firebase/C6 구현을 의미하지 않는다.
