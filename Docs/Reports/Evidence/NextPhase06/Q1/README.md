# Q1 실행 증거와 현재 한계

최종 Q1은 **PASS**다. 사용자 추가 승인 후 경로를 단축하고 [R3](R3/)에5개 통과·자연 종료·오류 복구·서버 재시작 증거를 보존했다. 이 디렉터리 바로 아래의 `Q1-journey-final.json` 1FAIL과 `Q1-storage-diagnostic.json`은 수정 전287자 경로 실패 이력이며 최종 결과가 아니다.

- `Q1-final-compile-result.json`: 새 C#까지 import 후 컴파일 오류0.
- `Q1-discovery-final.json`: 일반25+명시 HTTP5=30개 발견. 목록의 leaf Explicit 표시와 별개로 클래스 Explicit는 Pipeline 필터가 처리한다.
- `dashboard-overview.*`: 프로필 실제 HTTP2회(실패 여정의 조회1+진단 조회1)와 가상 Firestore 표시 구분.0건 쓰기를 v4 완주 검증으로 주장하지 않는다.
- `dashboard-cost.*`: 실제 청구 아님/운영 실측 없음/미차감 기본값의 기존 M4 화면. 가격 최신성 재조사나 Firebase 연결을 하지 않았다.
- `q1-server-history.jsonl`: 소유 서버 시작·종료 기록. 같은 서버 재시작은 미실행.
- `q1-server.ps1.txt`, `unity-check.ps1.txt`: 최초 실패 당시 실행 스크립트 사본. 수정된 재실행용은 R3 하위 사본이며 프로젝트 `artifacts/next-phase-06/`에 .ps1로 두는 경로 계약이다.

Q1은 `run_tests --mode playmode --filter <exact-name> --filter_type testName --include_explicit true --async_tests true`로 하나씩 실행하고, test_status에서 실제1개 성공을 확인한다. Prepare 성공 후에만 소유 PID를 같은 endpoint/DB로 재시작하고 Resume를 호출한다. 일반 회귀에는 include_explicit을 주지 않는다. 자동 순서/병렬 실행을 사용하지 않는다.

최종 시험 환경변수는 해제했고 최초PID13240과 R3의17652/20704는 종료했다. DB/시험 파일은 ignored artifacts에 보존하며 사용자DB/실제 계정과 별개다. 실제 서버 재시작 기록은 R3의 server-history에 포함된다.
