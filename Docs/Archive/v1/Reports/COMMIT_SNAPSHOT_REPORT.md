# 누적 구현 및 Git 커밋 정리

날짜: 2026-09-08. 대상: D:/UnityProject/Dice_Warrior, main.
범위: 초기 체크인 433eddd 이후의 승인된 누적 구현을 하나의 로컬 Git 커밋으로 보존한다. 새 게임 동작 수정이나 원격 push는 포함하지 않는다.

## 현재 구현

- 기본 로그라이크 루프: 새 여정/이어하기, 주사위 조합과 운명 카드, 분기 탐험과 사건, 전투·보상·장비·여정 성장, 보스·승패·재시작 및 로컬 저장.
- UI 구조: typed 데이터와 BaseUI, 화면/팝업 수명·레이어·캐시를 관리하는 UIManager, 개별 화면과 공용 버튼/카드 프리팹. 화면 원본을 Unity Editor에서 편집할 수 있다.
- 씬과 로비: Title → Lobby(출전 준비) → InGame(캠페인 지도·전투). 로비의 캐릭터·세팅·성장 안내를 분리했다. 현재 캐릭터는 방랑자 1명이며 영구 성장 경제는 미구현이다.
- 캠페인 지도: 아래에서 위로 진행하는 노드·연결선, 현재 위치·선택 가능 경로·완료 경로 표시. 노드 도착 후 별도 창에서 버튼을 눌러 주사위 6개를 굴린다.
- 한글화: 게임 표시와 작업실 명칭, Pretendard Regular 원본 및 배포용 OFL 저작권·라이선스 포함. 시작 세팅은 와일드 카드로 표기한다.
- 전투 피드백: 조합명·조합 단계·운명력 표시와 결과 0.9초 유지, 선택 카드 확대/강조, 공격·적 반응의 지연, 전투 영역 흔들림/점멸, 실제 체력 피해·막힘·수호 숫자 표시.
- 플레이 작업실: 테스트 상황/시드/등급 상한/와일드 카드/세로 비율, Scene 미리보기·원본 편집·격리 플레이 및 제작 씬 복원.
- 개발 지원: Memory Profiler·Code Coverage 패키지, 공식 Unity CLI/Pipeline 프로젝트 스킬, AGENTS, GDD와 68개 C#의 CodeMap, 이전 단계 계획·검증 기록 및 UI 참고 이미지.

## 검증

| 항목 | 결과 |
|---|---|
| 현재 커밋 후보 전체 PlayMode | 77/77 PASS, 182.71초 |
| 현재 커밋 후보 전체 EditMode | 128/128 PASS, 12.58초 |
| 검사 전후 파일과 저장 | 테스트 직후 기존 후보 349개 SHA-256 동일, 기본 저장 동일. 이후 EOF만 정리한 파일은 본문 byte 동일 증거 별도 기록 |
| 구조 | 프로젝트 C# 68개와 대응 CodeMap/meta 확인, Packages manifest/lock JSON 유효 |
| 직접 버튼 관찰 | 앞선 작업에서 Unity native 클릭으로 로비→지도→노드 도착→굴림→전투→보상과 피드백 확인. 이번 커밋 단계에서는 재클릭하지 않음 |

이번 커밋 단계의 실제 실행 JSON 및 고정 파일 명세는 Codex 작업 폴더 `artifacts/commit-snapshot/`에 보관한다. 이전 구현의 상세 증거는 각 REPORT에 기록했다. Android/iOS 실기기 및 현재 최종 트리의 APK/IPA 빌드는 이 단계에서 실행하지 않았다.

## Git 범위

고정 명세는 354개 변경 경로(추가/수정 351, 삭제 3)이며, 정확한 개별 pathspec만 staging한다. 기존 index는 비어 있었다. Stage 후 실제 경로 목록 일치·공백 검사·LFS pointer/원본 해시를 확인한 뒤 커밋한다. HEAD의 최종 식별자는 이 파일을 포함하는 Git 커밋 로그에서 확인한다.

기존 설정과 자동 저장 변경은 새로운 전투 기능 수정과 구분해 현재 프로젝트 상태로 포함한다. 여기에는 프로젝트 식별자/패키지 define, 씬 목록, URP 빌드 직렬화 값·Graphics/Quality 설정·UnityConnect enabled 값, 기존 HubForceResolve 자기 정리 결과가 있다. 해당 과거 내역은 FATE_DICE_PROTOTYPE_REPORT에 기록되어 있다. 이번 단계에서는 해당 값을 변경하지 않았다.

커밋 제외로 그대로 남기는 파일:

- `Assets/TutorialInfo/Icons/URP.png`: HEAD와 실제 원본 bytes의 Git blob hash가 모두 6194a807e27158f864a7c7677f4cbf62d8b94503으로 동일하다. 기존 LFS 필터에 따른 표시 차이만 있어 이미지 재등록을 하지 않는다.
- `ProjectSettings/Packages/com.unity.ai.assistant/Settings.json`: 기본 에디터 옵션.
- `ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json`: 빈 설정 사전.

Library/Temp/Logs/UserSettings, 로컬 저장, 테스트 산출물은 Git에 추가하지 않는다. 다른 작업의 파일을 reset/stash하거나 삭제하지 않으며 브랜치 전환·원격 push·merge는 수행하지 않는다.

스테이징 과정의 보정: Git이 기존 Editor.meta 삭제와 신규 폴더 meta를 rename으로 묶어 경로 수가 달라 보였으므로 --no-renames로 삭제/추가를 분리하여 고정 명세와 비교한다. 공백 검사에서 확인한 기존 C#/CodeMap 50개의 파일 끝 빈 줄만 제거했다. 파일 본문 bytes와 줄바꿈 형식은 동일하며 검증 원본은 eof-backup, 경로·비교 결과는 eof-only-proof.json에 보관한다. 기능 코드 변경 없이 재컴파일과 공백 검사를 확인한다.

필수 미해결 사항 없음. 새 기능 검토/수정 예산은 재개방하지 않았으며, 이번 정리는 커밋 범위 확인 1회와 최종 트리 검증 1회다.