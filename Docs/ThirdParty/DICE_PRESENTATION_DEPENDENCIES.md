# 주사위 연출의 외부 설치 항목

2026-09-11 현재 작업 환경에서 확인한 의존성이다. 게임 코드·프로젝트 프리팹과 재료는 저장소에 포함하지만 아래 소유 에셋의 공급사 원본과 샘플은 로컬 설치로 유지한다. 새 checkout은 해당 에셋을 설치하기 전에는 컴파일·연출 재현이 완료되지 않는다.

| 항목 | 확인한 버전 / 위치 | 사용하는 부분 |
|---|---|---|
| FEEL | 6.1 / Assets/Feel | MoreMountains.Tools 어셈블리, 주사위 결과·카드 선택 확대 연출 |
| Text Animator for Unity | 3.14.2 / Packages/com.febucci.text-animator-unity | Febucci.TextAnimatorForUnity.Runtime, Febucci.TextAnimatorForUnity.TMP.Runtime, 한글 조합 글자 등장 |
| Text Animator 기본 콘텐츠 | 위 버전의 Assets/Plugins/Febucci/Text Animator for Unity | TextAnimatorSettings 및 효과·스타일·재생 데이터베이스. 패키지 외에 기본 콘텐츠 설치도 필요 |
| All In 1 Sprite Shader | 4.68 / Assets/Plugins/AllIn1SpriteShader | ComboRibbon.mat의 UI mask/shine 셰이더 및 원본 텍스처 |
| TextMesh Pro | 현재 Unity UI 패키지의 Unity.TextMeshPro | Assets/TextMesh Pro Essential Resources. 해당 리소스와 동반 폰트 라이선스는 커밋에 포함 |

자신이 소유한 원본 패키지에서 위 버전을 설치하고 기존 GUID를 유지한다. Text Animator는 Setup의 기본 콘텐츠도 설치해야 한다. 이 프로젝트에서 검증했던 기본 콘텐츠는 37개 자산이며 상세 설치 결과는 [TEXT_ANIMATOR_SETUP_REPORT](../Reports/TEXT_ANIMATOR_SETUP_REPORT.md)에 있다. Quick Start 등의 샘플은 게임 동작에 필수인 기본 콘텐츠와 별개다.

All In 1은 셰이더뿐 아니라 참조 텍스처 fire2.png, rainbow.png, seamlessNoise.png, GradientTextures/palette-downwell.png도 원본 GUID와 함께 필요하다.

Packages/packages-lock.json의 embedded 항목은 설치 상태 기록이며 패키지를 자동 다운로드하지 않는다. 공급사 원본을 제거하거나 대체 구현한 커밋이 아니므로 새 환경에서 이 의존성을 먼저 준비해야 한다. 이 커밋 단계에서 새 checkout 빌드나 모바일 빌드는 실행하지 않았다.

버전 근거: Assets/Feel/readme.txt, AllIn1ShaderWindow.cs, Text Animator package.json과 기존 설치 REPORT. 프로젝트 소유 재료·프리팹의 외부 GUID를 임의로 바꾸지 않았다.
