# FateChoiceUIData.cs

- 역할: 지도 위 운명 선택 popup의 표시 데이터/명시적 사용자 요청 경계다. RunUIData의 공개 context/hud를 상속한다.
- 입력/출력: FateOfferUIData[] offers, Action<string> confirm, Action close. offers의 id/type/grade만 카드를 구성하고 기존 offer.clicked는 popup의 탭에서 실행하지 않는다. hud.dieClicked가 있는 정확한 여섯 dice는 재굴림 UI를 구성한다.
- 관계: RunUIController와 UIWorkbenchPreview가 공급하고 FateChoiceUI가 소비하도록 확정한 공유 계약이다. BaseUI의 typed Data로 바인딩 수명 동안 보관된다. 저장 모델/ScriptableObject가 아니다.
- 상태/검수: 실제 EventDefinition/RunState/RunSession/저장소/RNG를 보유하지 않는다. 미리보기는 confirm/close/dieClicked를 비워 표시만 만들 수 있다. 직접 통합/실행은 메인과 PROCEDURAL_CAMPAIGN_REPORT를 따른다.
