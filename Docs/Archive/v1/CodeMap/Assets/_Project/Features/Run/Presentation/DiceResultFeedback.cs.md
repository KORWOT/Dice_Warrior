# DiceResultFeedback.cs
- 역할: 이미 확정된 조합 이름과 색을 TMP/FEEL/All In 1 UI ribbon으로 표시한다.
- 입력/출력: Show(DiceRollUIData), ResetFeedback(), IsPlaying. 작성 참조 comboName/textAnimator/reveal/ribbon/catalog 및 entranceSeconds.
- 동작: FEEL은 banner scale, Text Animator는 glyph mesh, 로컬 재질은 shine 위치를 담당한다. priority 순위는 확대 강도만 조정한다. TMP font material은 유지한다.
- 관계 근거: DiceRollUI의 Bind/CompleteRoll/Unbind가 호출한다. DicePresentationAuthoring이 prefab 참조를 작성한다. catalog.Resolve의 색/태그를 사용한다.
- 수명: inactive Bind에서는 OnEnable까지 재생 보류. hold 0/정지 미리보기는 static 표시. 종료/취소/OnDisable은 player 정지와 원래 scale/color/material 복원, glyph 효과 정지, 생성 재질 파괴. 게임/난수/저장 접근 없음.
- 검수: 닫기·재바인딩·0초·한국어 mesh·원본 재질 보존. 실행 결과는 DICE_PRESENTATION_EFFECTS_REPORT.

- DPE-R01 보완: 모션은 min(entranceSeconds, holdSeconds)에서 정착한다. 이후 이름/색은 유지 종료까지 읽을 수 있다.
- DPE-R03 보완: EditMode에서는 Text Animator의 즉시 메시 생성을 호출하지 않고 TMP.text/Render만 설정한다. 비활성 Canvas의 Bind 이후 정상 OnEnable에서 메시를 만든다. 런타임 경로는 유지하며 PlayWorkbenchTests가 두 세로 비율의 실제 정지 메시와 원본 보존을 검사한다.
- 오라 확장: dieAuras/crest는 작성 참조. Show가 확정 hand/values를 DiceComboHighlights.Groups로 표시용 그룹화하고 catalog.color/accentColor/alternateGroups 및 comboStrength를 전달한다. 새 규칙 판정이나 RNG 소비는 없다. Update의 짧은 등장 뒤 phase1을 유지하며 Reset/빈 이름/닫기는 참여·메시를 지운다. 0초·정지 preview는 phase1을 즉시 표시한다.


## 등급별 연출 수명 (2026-09-10)

Show는 확정 DTO 강도로 tier를 선택해 Entrance→Flourish→Reading→Exit→Completed를 진행한다. IsPresenting은 전체 수명, IsPlaying은 모션, Phase/StartedAt/EndedAt/Error는 표시 상태다. FEEL IsPlaying 종료 전에는 등장 다음 단계로 넘어가지 않는다. 일반은 중앙 Crest가 없고 상위는 순차 점등·회전·다중 파동, 마지막은 alpha fade다. hold0/Editor는 정지 표시, 읽기는 max(authored read, hold)다. Update 오류는 Error로 DiceRollUI 감독 coroutine에 전달한다. Reset/비활성은 외부 효과·재질·pose를 정리한다.
실제 증거/판정은 PRESENTATION_LIFECYCLE_REPORT를 따른다. 위의 이전 고정 대기 계약은 이번 사용자 요청 범위에서 대체한다.
