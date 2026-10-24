# V5DiceNetView.cs

- 역할: 로비 출전/경기 보유/완주 후보 주사위의 실제 6면 전개도와 전투 정보 모달을 표시한다. V5DiceNetData는 표시 정의·모험가·슬롯/현재 결과와 UI 복귀 콜백을 담는다.
- 핵심 동작: Cell(0..5)은 (0,1),(1,1),(2,1),(3,1),(1,0),(1,2)의 연결된6면 배치다. Faces API의 순서/중복을 보존한다. instanceId+faceIndex로 이번 굴림을 강조하고 조회 중 면은 별도 문구/선택 표시다. 초기/탭 선택은 해당 주사위 결과면에서 시작한다.3슬롯/후보 탭과 면 클릭은 선택/페이지 상태만 바꾼다. 전투 카드와 같은 번호·종류를 DieLabel로 표시한다.
- 입출력: Bind(V5DiceNetData), Unbind, SelectedSlot/SelectedFace. V5DiceNetData는 selectedSlot/selectedFace(-1은현재결과)/detailPage/scrollPosition을 UI로컬 상태로 보존한다. Face 상세는 공용/전용/꽝, 소유 모험가 제한, 단독/조합, 기본 수치·계수·피해 타입·사용자용 태그를 기술당페이지로 표시한다. 정보 모드는 authored scroll 안에 전체 예상/로그를 표시하고 능력치 조회와 별도 모험 메뉴(로비로·진행유지/내모험포기)를 구분한다.
- 사용하는 대상: CompetitionV5Content.Faces/Face, v5.skills.effect, CompetitionElements 표기 API, CompetitionV5UiText.Friendly의 등록태그명, V5DiceAreaView.DieLabel, CommonButtonView/V5FaceGraphic/uGUI ScrollRect.
- 사용하는 쪽·관계 근거: CompetitionApp가 읽기 전용 데이터를 만들고 다른 상세 진입 중 이를 보관/복원하며 CompetitionScreen이 Bind/Unbind한다. CompetitionAuthoring.BuildV5Net과 CompetitionCombatUiAuthoring.Upgrade가6셀/3탭/배경/버튼/scroll을 연결한다. CompetitionV5FlowTests/CompetitionCombatUiTests가 실제 포인터와 상태 불변을 확인한다.
- 상태/수명: 모달 배경 Image.raycastTarget이 뒤 입력을 가로막는다. 선택/페이지/scroll은 data 안의 UI로컬 상태이며 Session/RNG/리롤을 바꾸지 않는다. 같은 data를 재바인딩하면 조회를 복원한다. ScrollRect의 소유 listener만 매Bind/Unbind에서 해제한다. 닫기는 전달받은 callback에 맡긴다.
- 검수 주의: 6면을 유니크 집합으로 줄이지 않는다. 신규 V5 SkillId를 v4 심볼 기술 목록에서 조회하지 않는다. 런타임 객체 생성 없이 authored 참조가 없으면 오류다.

- UI12: authored motion 버튼은 information(주사위 목록 없음)일 때만 표시한다. DTO motion callback/reducedMotion은 화면 로컬 보기 설정을 전달한다. stats/motion 두 버튼을 같은 관리 행에 배치하고 기존6면 조회에는 추가 버튼을 숨긴다. Unbind가 motion 콜백도 해제한다.
