# KoreanText.cs
- 역할: 기본 플레이 콘텐츠와 이전 영문 저장 알림을 한국어로 바꾸는 표시 전용 정적 경계다.
- 입력/출력: Content(string)는 정확히 일치하는 기본 명칭·설명을 번역하고 null/사용자 편집 문구는 그대로 반환한다. Grade/Node/Slot/Tag/Match/Source/Value는 enum·태그의 한국어 표시를 반환한다. Notice(string)는 기존 RunSession/CombatRules 알림의 완전한 템플릿과 Event resolved 접미사를 처리한다.
- 핵심 동작: 정확한 문자열 사전과 시작·끝이 고정된 정규식으로 알려진 기본 문구만 처리한다. 사용자 문구의 일부 문자열을 무차별 치환하지 않는다. 원본 enum/ID/tag/RNG/RunState/저장 파일을 수정하지 않는다.
- 사용하는 대상: 기존 FateDice enum 타입, System.Text.RegularExpressions; Unity 객체·서비스·저장소 의존 없음.
- 사용하는 쪽/근거: RunUIController가 화면 데이터/알림/장비 설명을 생성할 때 호출하며 FateDiceWidgets는 최종 표시 태그/노드에 사용한다. ActionCardView/FateCardView는 등급·공개 유형 표시만 사용한다. KoreanUiAuthoring은 정확한 기본 fallback 글리프를 생성한다.
- 상태/수명: 읽기 전용 정적 사전, 캐시나 세션 상태 없음.
- 검수 주의: 새 기본 콘텐츠를 추가할 때 번역 사전과 새 알림 템플릿도 추가한다. 사용자 지정 영문 콘텐츠는 의도적으로 유지한다. KoreanUiTests가 기존 영문 snapshot·전체 상태·디스크 bytes 불변과 사용자 문구 보존을 확인한다.
- 실행 증거: Docs/Reports/KOREAN_UI_REPORT.md.

## COMBAT_FEEDBACK
- HandStage(RunState)는 실제 저장 config의 현재 hand.label을 한국어로 표시하고, 그 정의보다 priority가 작은 행의 수 + 1을 조합 단계로 표시한다. 분모는 실제 정의 수다. enum 순서나 운명력으로 순위를 추정하지 않는다.
- HandSummary(RunState, bool multiline=false)는 동일한 조합 단계에 실제 state.fatePower를 붙인다. multiline=true는 조합 이름 / 단계와 운명력의 두 줄이며, null state 또는 아직 dice가 없는 state는 주사위를 굴려 확인을 반환한다.
- 읽는 직접 대상은 RunState 및 GameConfigData의 HandDefinition이다. RunUIController와 UIWorkbenchPreview가 플레이/에디터의 동일 표시를 요청한다. 사용자 지정 label을 유지하며, 배열 정렬·주사위 재판정·RNG·저장 호출을 하지 않는다. 단계가 카드 등급을 보장하거나 확률을 뜻하지 않는다.
- 실제 새 검증 증거와 완료 판정은 Docs/Reports/COMBAT_FEEDBACK_REPORT.md를 따른다.
