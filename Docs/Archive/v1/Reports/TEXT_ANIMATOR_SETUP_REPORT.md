# TEXT_ANIMATOR_SETUP_REPORT

상태: COMPLETE — Text Animator 3.14.2 기본 콘텐츠 설치 복구. 2026-09-10.

## 확인한 문제
- 설치 창에 Reset Built-in Effects가 표시됐지만 InstallationData.asset에 버전만 기록돼 있었다. 기본 콘텐츠 37개와 TextAnimatorSettings.asset은 없었다.
- 기존 최신 로그에는 Unity AI NoSubscription/라이선스 조회 오류와 uGUI Selectable.OnDisable의 IndexOutOfRangeException이 있었다. 이 오류를 Text Animator 설치 실패의 원인으로 단정할 증거는 없다.
- 원본 BuiltIn.unitypackage는 정상적으로 읽혔고 프로젝트의 기존 자산과 GUID 충돌도 없었다. 에디터는 ready/stopped, compiling=false였다.

## 수행한 조치
- Unity CLI로 실행 중인 Dice_Warrior 에디터에 연결하여 AssetDatabase.ImportPackage의 비대화형 가져오기를 한 번 호출했다.
- 원본: Packages/com.febucci.text-animator-unity/Data~/BuiltIn.unitypackage.
- 생성 위치: Assets/Plugins/Febucci/Text Animator for Unity 아래 Actions, Curves, Effects, Playbacks, Resources, Settings, Timings 및 .meta.
- CLI 응답은 Main thread operation timed out after 5000ms였지만 가져오기는 계속 진행돼 완료됐다. 응답 실패를 설치 실패로 간주해 재실행하지 않고 파일/에디터 로드로 판정했다.

## 실제 검증
- 파일: 37/37 존재, 누락 0, 원본 GUID 불일치 0.
- Editor API: 37/37 ScriptableObject 로드, 무효 자산 0.
- Resources.Load<TextAnimatorSettings>: 성공.
- 효과 데이터베이스: 14개 태그 조회, shake/bounce/wave 포함.
- 액션/플레이백 데이터베이스: 현재 패키지 타입으로 로드. 스타일/기본 플레이백/기본 곡선 참조 정상.
- 검증 eval: diagnostics/errors/warnings 없음.
- 기존 InstallationData와 패키지 원본/manifest/lock을 포함한 보호 파일 해시 변경 없음.

증거: artifacts/text-animator-setup/baseline.json, filesystem-check.json, editor-verification.json, log-observations.json.

## 범위와 남은 항목
- 게임 코드/씬/프리팹/저장/폰트 연결은 변경하지 않았다. 공급사 C# 및 DLL 수정 없음. 새 C# 없음으로 CodeMap/코드 관계 갱신은 비적용.
- 사용자 기존 패키지/샘플/TMP 임포트 변경을 보존했다. 커밋하지 않았다.
- 설치 창 버튼 무반응의 내부 원인은 미확정이며 버튼 자체의 수정 완료를 주장하지 않는다. 기본 콘텐츠는 별도 API 경로로 정상 설치됐다.
- 기존 Unity AI 구독 오류와 uGUI 재로딩 예외는 본 설치 복구 범위 밖 후속 확인 대상이다.
- 실제 텍스트 애니메이션 시각 확인/한글 폰트 연결/게임 회귀 테스트/빌드는 미실행이며 설치 AC에 포함하지 않았다.
- 최초 검증 묶음 1회, 자산 수정/재검증 0/2회, 필수 미해결 Finding 0. 단일 에이전트/단일 에디터로 수행했다.
