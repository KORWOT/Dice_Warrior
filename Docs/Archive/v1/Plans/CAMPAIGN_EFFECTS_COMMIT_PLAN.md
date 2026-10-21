# 캠페인·주사위 연출 현재 상태 커밋

상태: DOCUMENT / 명세·검사 완료, 커밋 실행. 2026-09-11 사용자의 “좋아 일단 깃 커밋해줘”에 따라 현재 누적 구현을 로컬 main에 기록한다. 이전 기능의 보완 상한을 초기화하거나 미해결 검사를 수정하지 않는다.

- 기준: main / d8df4999174ad63bb73e0727ff0149cb55624db2. 시작 index는 비어 있다. 기존 작업 변경은 artifacts/campaign-effects-commit/baseline.json에 기록한다.
- 포함: 현재 Assets/_Project의 소스·원본·meta, TMP Essential Resources와 라이선스, 관련 CodeMap/INDEX/GDD 및 Text Animator 설치·조합 연출·수명·절차 지도·복귀 개선의 PLAN/REPORT, Packages/packages-lock.json의 embedded 의존성 기록. 사용자 승인 저장의 기존 Title EventSystem 변경도 현재 상태로 기록하고 알려진 실패로 명시한다.
- 제외: 소유한 FEEL, All In 1 Sprite Shader, Text Animator 공급사 원본/샘플과 개인 실행 artifacts. 해당 로컬 파일은 보존한다. .gitignore에 정확한 외부 원본 경로와 /artifacts/를 기록하고 별도 설치 의존성을 Docs/ThirdParty/DICE_PRESENTATION_DEPENDENCIES.md에 설명한다. 공급사 패키지가 없는 새 checkout의 빌드 성공을 주장하지 않는다.
- 쓰기 범위: 이 PLAN, 대응 REPORT, 의존성 안내, .gitignore, Git index/objects/현재 main commit. 의미적 게임·Scene/Prefab·패키지 수정, 브랜치 전환, push 없음. 기존 staged를 포함하지 않는다.
- 검증: 개별 경로 manifest와 실제 staged 목록의 완전 일치, 공백·JSON/asmdef·C# CodeMap/meta·LFS 검증, 커밋 후 HEAD/manifest/index 확인. 최신 실제 PlayMode196=189PASS/7FAIL, 신규11PASS를 기록하며 이번 커밋만을 위해 전체 테스트를 반복하지 않는다.
- 위임: 기존 effect_tests를 읽기 전용 포함/제외·외부 의존성 확인 담당으로 재사용한다. Main만 문서/Git 쓰기. 동시2, 누적1, 재귀0. 기능 검토를 다시 열지 않는다.
- 완료: 알려진 실패와 외부 의존성을 명시한 현재 상태 커밋이 성공하고 정확한 hash와 미포함 파일을 보고한다. 기능 완료 판정과 커밋 완료 판정을 구분한다.

고정 manifest는250경로이며 index와 완전 일치했다. 커밋 정리 과정에서 CampaignPathGraphic/MapNodeGraphic/FateChoiceAuthoring의 CodeMap3개 끝 빈 줄만 제거했다. 게임 C#은 수정하지 않았다. Unity 작성 YAML의 빈 값 뒤 공백127건은 원본 유지 대상으로 기록한다. LFS 객체 검사는 PASS이며 기존 HEAD의 URP.png 비pointer 예외는 이번 경로에 포함되지 않는다.
