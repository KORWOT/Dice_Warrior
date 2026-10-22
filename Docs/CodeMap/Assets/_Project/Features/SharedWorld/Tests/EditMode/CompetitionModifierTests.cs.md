# CompetitionModifierTests.cs

- 역할: v4 Modifier의 중복/중첩 정책과 정수 피해 계산·최종 반올림 계약을 고정 수치로 검사한다.
- 핵심 동작:100→138/98 상성, 분수 스탯 항의 합산과 중간 반올림 금지,6스탯 참여, 동일 identity1회/충돌 거절, UniqueBySource/HighestWins, PercentAdd 합산/FinalMultiply 그룹별 곱, 적용 순서와 유한 상한을 확인한다.
- 입출력: NUnit18개 사례. 작은 메모리 Modifier/AttributeValues/Enemy fixture만 사용하며 실제 콘텐츠/저장/DB를 변경하지 않는다.
- 사용하는 대상: CompetitionModifiers.Normalize/Evaluate/EvaluateScaled, CompetitionElements.Affinity, CompetitionV4Data와 LoopJson, NUnit/LINQ.
- 사용하는 쪽: Unity SharedWorld.EditMode.Tests 및 Server/DiceWarrior.Competition.Tests가 같은 소스를 공유 컴파일한다.
- 관계 근거: 공개 숫자/정규화 API 직접 호출. expected 값은 수동 계산한 리터럴이며 구현의 계산 함수를 재사용하지 않는다. LoopJson은 입력 불변/복사 분리를 비교할 때만 사용한다.
- 상태/수명: 각 테스트가 새 입력을 만든다. Normalize 반환 사본을 변조하거나 실패 입력을 처리해도 호출자 값이 유지되는지 검사한다. 과대 스트림은65번째에서 중단하며64개 배율의 큰 결과가 int로 돌아가거나 조용히 clamp되지 않아야 한다.
- 검수: ID/sourceKind/sourceId/target/stackGroup의 구분자와 길이 오류가 문자열 결합 키에 진입하지 않도록 거절해야 한다. 10.5→11, 11.5×1.15×0.85→11로 최종 AwayFromZero를 구분한다. 실제 실행·RED/GREEN 증거는 메인이 직렬로 실행해 Task REPORT/TRX에 기록한다.
