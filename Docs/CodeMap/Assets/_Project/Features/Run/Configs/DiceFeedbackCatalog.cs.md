# DiceFeedbackCatalog.cs
- 역할: 조합 ID별 색·Text Animator 태그·확대량을 편집하는 표시 전용 SO. DiceHandFeedbackStyle도 같은 파일에 정의한다.
- 입력/출력: styles 배열과 Resolve(HandKind). 미등록 조합은 예외, 다른 등급으로 대체하지 않는다.
- 관계 근거: DiceResultFeedback.Show/PlayMotion이 Resolve 결과를 사용한다. DicePresentationAuthoring이 기본 자산을 작성한다.
- 상태/수명: 공유 설정 읽기만. RNG·게임 규칙·저장 상태에 접근하지 않는다.
- 검수: 모든 HandKind 매핑과 색, 사용자 변경 보존, 효과 태그 설치 여부. 실행 결과는 DICE_PRESENTATION_EFFECTS_REPORT.
- 오라 확장: accentColor는 링/문양의 보조색, alternateGroups는 홀수 참여 그룹을 보조색으로 표시한다. auraVersion은 표시 SO 초기 migration 표시다. ApplyAuras는 이전 기본색과 같은 값만 새 참고색으로 바꾸고 이미 편집한 대표색은 보존한다. 규칙 priority와 색상 ID는 계속 분리한다.


## 등급별 연출 수명 (2026-09-10)

DiceEffectComplexity/DiceEffectTimeline을 함께 정의한다. tiers는 저장 priority 순위의 최대 강도 경계와 등장·강조·읽기·퇴장 시간을 제공한다. ResolveTimeline은 증가 경계/마지막1/유한 비음수를 검증한다. Duration(minimumRead)는 기존 hold를 최소 읽기 시간으로 반영한다. timelineVersion1 작성은 기존 조합 색·태그를 보존한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
