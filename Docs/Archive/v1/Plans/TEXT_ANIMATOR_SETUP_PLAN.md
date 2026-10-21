# TEXT_ANIMATOR_SETUP_PLAN

상태: IMPLEMENT / COMPLETE. 2026-09-10 기본 콘텐츠 설치 복구와 필수 검증 완료. 초기 설치 버튼 무반응의 내부 원인은 미확정이다.

## 계약
- 대상: D:/UnityProject/Dice_Warrior, Unity 6000.6.0f1, Text Animator 3.14.2.
- 기준 commit: d8df4999174ad63bb73e0727ff0149cb55624db2. 기존 변경과 보호 파일 해시는 artifacts/text-animator-setup/baseline.json에 기록한다.
- 목표: 기본 콘텐츠 가져오기가 실행되지 않는 지점을 확인하고 제공된 BuiltIn.unitypackage의 데이터만 Unity Editor API로 설치한다.
- 필수 AC: 기본 자산 37개 및 원본 GUID 존재; 설정/효과/액션/플레이백 정상 로드; shake/bounce/wave 조회; 이번 작업으로 발생한 직접 오류 없음.
- 허용 자산: BuiltIn.unitypackage의 pathname 목록에 포함된 Assets/Plugins/Febucci/Text Animator for Unity 아래 기본 콘텐츠와 .meta/상위 폴더 .meta. 정확한 37개 파일 목록은 baseline.json의 expected_assets로 고정한다.
- 기존 InstallationData.asset, 패키지 원본/잠금/설정, 씬/프리팹/게임 코드/세이브/다른 에셋은 보존한다. Scene/Prefab 쓰기 허용 없음.
- 허용 문서/증거: 본 PLAN, Docs/Reports/TEXT_ANIMATOR_SETUP_REPORT.md, artifacts/text-animator-setup/. 검증용 스크립트가 필요하면 AgentScripts/TextAnimatorSetupProbe.cs 및 대응 1:1 CodeMap만 추가한다.
- 압축 내용은 원본 검증용으로만 읽고 자산 생성은 Editor API로 한다. 공급사 코드/라이브러리는 수정하지 않는다.
- 최초 검증 후 수정/재검증 최대 2회. API 비대화형 가져오기 한 번으로 재현하고 실패 시 근거가 확인된 입력/경로 보정만 한 번 수행한다. 메인 1명, 보조 0명, Unity 실행 직렬.
- 조합 연출 구현, TMP 폰트 연결, 구독 설정 변경, 기존 게임 전체 테스트, 커밋은 범위 밖이다.
- Docs/AI/CODEX_WORKFLOW.md가 현재 없어 AGENTS.md를 적용한다. 기존 COMBAT_FEEDBACK_PLAN에도 같은 부재가 기록돼 있다.

## 사전 증거
- 에디터 ready, compiling=false, playMode=stopped. 샌드박스 밖 CLI 읽기로 연결 확인.
- 기본 자산 37개와 설정이 없고 동일 GUID의 다른 자산도 없다. 공급 압축 파일은 정상 읽힌다.
- Unity AI NoSubscription 및 uGUI Selectable.OnDisable 예외가 있으나 설치 실패와 인과관계는 미확정.

## 진행
- [x] 현재 에디터/기존 변경/누락 및 패키지 원본 확인.
- [x] Editor API 가져오기 재현과 설치 복구.
- [x] 기본 자산/GUID/설정 참조/주요 효과 검증.
- [x] 보호 파일/직접 로그/REPORT 정리.

## 결과
- AssetDatabase.ImportPackage(원본 절대 경로, false)를 한 번 실행했다. CLI는 메인 스레드 응답 5초 초과를 보고했지만 작업은 완료되어, 재요청 없이 후속 결과로 확인했다.
- 37/37 파일과 원본 GUID, 37/37 ScriptableObject 로드, 설정/액션/플레이백/스타일/기본 곡선 연결, 효과 태그 14개를 확인했다.
- 최초 검증 묶음 1회, 자산 수정/재검증 0/2회. 보호 파일 변경 없음. 별도 공급사 코드 수정/게임 코드/씬/프리팹 변경 없음.
- 기본 콘텐츠 설치는 완료이며, 설정 창 버튼 동작 자체와 기존 Unity AI/uGUI 오류는 후속 확인 대상으로 구분한다.
