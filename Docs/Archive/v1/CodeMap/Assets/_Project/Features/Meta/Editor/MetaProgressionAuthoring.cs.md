# MetaProgressionAuthoring.cs

- 역할: 메타 설정과 편집 가능한 로비 컨테이너 작성.
- 핵심 동작·입출력: Apply는 Editor 상태/dirty scene/prefab을 확인하고 DefaultMetaProgression SO를 없는 경우 생성한다. MenuUI의 3개 보유품/성장 컨테이너를 추가하고 GameApplication.metaConfig를 연결한다. metaLayoutVersion1 재적용은 중복 생성하지 않는다.
- 사용하는 대상·사용하는 쪽·관계 근거: Unity Editor API/PrefabUtility만 사용한다. MetaProbe.Author 또는 Fate Dice 메뉴에서 호출한다.
- 상태·수명·검수 주의: 고정 쓰기 대상은 PLAN의 MenuUI/GameApplication prefab/DefaultMetaProgression뿐이다. 기존 배치·GUID·사용자 설정을 초기화하지 않는다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
