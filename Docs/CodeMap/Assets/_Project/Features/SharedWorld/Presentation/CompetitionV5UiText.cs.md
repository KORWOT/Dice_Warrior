# CompetitionV5UiText.cs

- UI13: 사건 설명이 선택 제목으로 시작하면 그 접두어와 구분자만 제거하고 비용/조건/추가 효과는 그대로 표시한다. 결과 참가자 사이 빈 줄로 순위 묶음을 구분한다. App의 완주 후보는 Lap 설명만 사용하여 그 안에 포함된 Die/Gear의6면·장비비교를 중복하지 않는다. 지급 투영과 수치는 변경하지 않는다.

- 역할: v5 카드/상세/결과 화면의 순수 한국어 설명 생성. Unity API나 외부 저장/서버를 사용하지 않는다.
- 핵심 동작: Die는 실제 6면의 물리 순서를, Gear는 같은 슬롯의 현재 장비와 후보 및 등록 보정을 표시한다. Encounter는 실제 HP/골드 소모·최소 HP·필요 장비·기본/행운 골드·XP/명예/회복/장비/주사위 효과와 전투 적의 공격·상성을 설명한다. Lap은 공유 지급 투영으로 정확한 레벨/HP/골드/주사위 전후와 다음 AI 또는 시련 단계를 설명한다. Results는 기존 Rankings 순서/저장 rank·개인 상태/종료 이유와 공동 순위 기준을 표시한다.
- 입출력: public static string Die(d,hero,dieId), Gear(d,p,gearId), Encounter(d,p,choice), Lap(state,choiceId), Results(state), Friendly(d,text). v5의 유효한 동결 정의/상태와 ID를 입력받는다. Gear는 출전 조회에서 p=null을 허용한다. 이 경우 ‘다음 모험 시작 무기’와 후보 효과만 표시하고 현재 장착·교체 정보는 만들지 않는다. p가 있으면 기존의 실제 슬롯 비교를 유지한다.
- 사용하는 대상: V5Content.Faces, EncounterRules.RewardGold, CommandRules.PreviewLapReward, CompetitionRules.Human/Rankings, Elements.TypeName, Attributes.Target/Label. Friendly는 등록 태그·면·모험가·기술·주사위·장비 이름과 명시된 종료/표시 상태를 경계가 맞는 토큰에서만 치환한다.
- 사용하는 쪽: CompetitionApp.V5Ui의 로비/출전/사건/완주/결과, v5 전투 표시의 읽기 정보, CompetitionV5UiProjectionTests. .NET 테스트 프로젝트는 이 Unity 비의존 파일만 Compile Link로 포함한다.
- 관계 근거: 각 public 메서드는 기존 순수 조회와 보상 투영을 직접 호출한다. 피해/조합/보상 산식을 다시 구현하지 않는다. 장비 binding의 operation/condition은 계산이 아닌 문자열 표현이다.
- 상태/수명: 상태/정의/원본 난수를 변경하지 않는다. Lap 조회의 임시 사본만 생성하며 선택 저장이나 지급을 수행하지 않는다.
- 검수 주의점: HP 비용은 최소 조건과 별도로 표시한다. 레벨업 HP와 회복 상한은 실제 투영값을 사용한다. 랭킹은 현재 상태에 이미 계산된 rank를 읽는다. 임의 진단 ID의 의미를 추측해 바꾸지 않으며 v1~v4 화면에 적용하지 않는다. 화면의 줄바꿈/스크롤/터치 검증은 별도 Unity 검증이다.
