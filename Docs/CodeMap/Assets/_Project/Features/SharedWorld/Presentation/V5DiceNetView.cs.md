# V5DiceNetView.cs

- 역할: 로비 출전/경기 보유/완주 후보 주사위의 실제 6면 전개도와 전투 정보 모달을 표시한다. V5DiceNetData는 표시 정의·모험가·슬롯/현재 결과와 UI 복귀 콜백을 담는다.
- 핵심 동작: Cell(0..5)은 (0,1),(1,1),(2,1),(3,1),(1,0),(1,2)의 연결된 6면 배치다. Faces API가 반환한 순서/중복을 보존하고 현재 눈은 instanceId+faceIndex로 한 칸만 강조한다. 3슬롯/후보 탭과 각 면 클릭은 선택/페이지 상태만 바꾼다.
- 입출력: Bind(V5DiceNetData), Unbind, SelectedSlot/SelectedFace. Face 상세는 공용/전용/꽝, 소유 모험가 제한, 단독/조합, 기본 수치와 effect 계수/피해 타입/태그를 한 기술씩 페이지로 표시한다. 정보 모드는 전체 예상/저장 로그와 종료/포기/능력치 진입을 표시한다.
- 사용하는 대상: CompetitionV5Content.Faces/Face, v5.skills.effect, CompetitionElements 표기 API, CommonButtonView/V5FaceGraphic/uGUI.
- 사용하는 쪽·관계 근거: CompetitionApp가 읽기 전용 데이터를 만들고 CompetitionScreen이 Bind/Unbind한다. CompetitionAuthoring.BuildV5Net이 6셀/3탭/배경/버튼을 연결하며 CompetitionV5FlowTests가 실제 포인터와 상태 불변을 확인한다.
- 상태/수명: 모달 배경 Image.raycastTarget이 뒤 화면의 입력을 가로막는다. 선택/페이지는 UI 로컬 상태이며 Session/RNG/리롤 횟수를 바꾸지 않는다. 새 데이터면 첫 슬롯/면/페이지로 돌아가고 닫기 콜백으로 원래 화면에 복귀한다.
- 검수 주의: 6면을 유니크 집합으로 줄이지 않는다. 신규 V5 SkillId를 v4 심볼 기술 목록에서 조회하지 않는다. 런타임 객체 생성 없이 authored 참조가 없으면 오류다.
