# 외부 의존성과 보존 자산

2026-10-20 SymbolLoop 전면 교체 기준.

현재 게임의 직접 의존은 Unity uGUI, Input System, Newtonsoft.Json, 공용 UI, Pretendard Regular다.
`SymbolLoop.Core`는 Unity 의존이 없다. 새 Runtime/Editor asmdef는 FEEL·Text Animator·All In 1 Sprite Shader·TMP를 직접 참조하지 않는다.
구형 ComboRibbon 재료·전투 연출 코드는 정리했다.

사용자가 설치한 외부 패키지·폰트·플러그인 원본과 샘플은 이번 범위에서 삭제하지 않았다. 기존 패키지 설정도 변경하지 않았다.
보존과 새 게임에서의 직접 사용은 다르다. 후속 외부 패키지 제거는 라이선스/다른 에셋 참조를 별도로 확인할 작업이다.

이전 v1에서 쓰던 버전·설치 이력은 [원문 기록](../Archive/v1/ThirdParty/DICE_PRESENTATION_DEPENDENCIES.md)에 내용 그대로 보존했다.
Pretendard 출처·배포 라이선스는 [폰트 기록](PRETENDARD.md)을 따른다.
