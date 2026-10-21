# RuleTests.cs

- 원본: `Assets/_Project/Features/Run/Tests/EditMode/RuleTests.cs`
- 기능/어셈블리: Run Tests / FateDice.EditMode.Tests (Editor)
- Task: FATE_DICE_PROTOTYPE M1
- SHA256: `12417082B8C40E022256C1CAFD672A49A56BB5F9FB229C35A4D4F51F05D47020`

게임 화면과 분리된 규칙 및 설정 유효성을 실제 공개 API로 확인하는 NUnit 테스트다. 모의 구현이나 무조건 성공 검증을 사용하지 않는다.

## 입력·기대값·흐름
- 기본 6D6의 46,656개 순서 있는 결과를 전수열거한다. 테스트 오라클은 정렬된 빈도 파티션/서로 다른 숫자 문자열을 사용하며 생산 코드는 숫자별 카운터로 판정한다.
- 별도 고정 집계(우선순위 Pair,TwoPairs,Triple,Straight,FullHouse,ThreePairs,FourKind,FullStraight,FiveKind,SixKind):
  - 단독 성립: 45936,25950,17136,4320,7950,1800,2436,720,186,6.
  - 최고 패: 7200,16200,7200,3600,7500,1800,2250,720,180,6. 합계 46656.
- GDD 7경계·패 우선순위/운명력 분리·잘못된 입력·xorshift 고정열/0 거부·샘플 소비수·6개 주사위 별도 정의를 검사한다.
- 탐험/전투 분리, 파워 행 경계, 상한으로 확률 모으기, 카드별 독립 등급, 선택 유형 1장 보장/나머지 편향, 사전 내용 확정, 빈 사건 풀 오류를 검사한다.
- 빈 행동 등급은 전체 소유 풀 균등·중복 허용·역할 보장 없음·원본 불변을 검사한다. 생성기는 rngState 외 상태 불변과 스냅샷 재현을 확인한다.
- 빈 설정·기본 작성 데이터·음수/NaN/합계0·중복 ID·행동 참조 누락·제시 수 범위 오류를 GameConfigData.Validate로 검사한다.

## 직접 관계
RuleTests는 DiceRules/FateCardRules 공개 메서드, GameConfigData.Validate/Event, UnityEngine.JsonUtility, FateDice.Editor.PrototypeAuthoring.CreateDefaults를 직접 호출한다. 독립 Fixture는 규칙에 필요한 설정만 만들며 SO 자산이나 사용자 설정 파일을 변경하지 않는다.
NUnit runner가 테스트를 호출하고, 어셈블리는 FateDice.Runtime 및 FateDice.Editor를 참조한다. UI와 저장 파일·Unity 에디터 조작은 하지 않는다.

## 상태·검수
모든 테스트가 로컬 입력과 고정 seed를 소유한다. 시간·애니메이션·전역 Random을 사용하지 않는다. 확률 검증은 고정 표본과 명시한 오차로 검증하며 확률이 정확히 기대 빈도로 실현된다고 주장하지 않는다.
현재 실행 증거: 메인 `artifacts/fate-dice-prototype/m1-rule-red.json`에서 total31/passed0/failed31/skipped0, 미구현 예외로 초기 RED를 확인했다. 초기 구현 GREEN 이력: artifacts/fate-dice-prototype/m1-rule-green.json에서 completed, total31/passed31/failed0/skipped0/inconclusive0, 4.53초. 최초 테스트 기준을 변경하지 않고 통과했다. 이 파일은 실제 게임 화면·저장·전투 순서 완료를 증명하지 않는다.

M1 마지막 수정묶음 2/2에서 FiniteHugeNodeWeightsPreserveRelativeChoicesSourceAndRngContract 1개를 추가했다(기존31건 유지). 유효한 float.MaxValue 노드 가중치와 bias3/float.MaxValue에서 비율을 축소한 입력과 동일 카드/RNG, 정확8회 소비, 원본·다른 상태 불변, Infinity 오류 시 RNG 보존을 검사한다. 메인 m1-weight-overflow-red.json에서 해당 1건 RED를 확인했다. 수정 후 총32건이 최종 통합 회귀에서 모두 통과했다. 위 31건 GREEN은 변경 전 이력이다.

### M1 최종 검증
- 증거 루트: C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/fate-dice-prototype/.
- m1-m2-editmode-regression.json: completed, 41/41 PASS(RuleTests32 + RunTests9), 실패/건너뜀/미확정0, 5.5초. 원본 결과의 테스트 이름으로 분류를 직접 확인했다.
- m2-gui-regression.json: completed, PlayMode2/2 PASS, 실패/건너뜀/미확정0, 0.85초. 이 GUI 결과는 M2 검증 범위이며 M3 이후 전체 게임 완료를 의미하지 않는다.
- M1 최초 검토1/1, 수정2/2. 위 오버플로를 포함한 해당 필수 결함은 최종 회귀에서 해결됐다. 코드/테스트 추가 수정 없이 결과와 직접 관계를 동기화했다.
### 최종 연결 및 M5 검증
- m4-save-regression.json: EditMode completed, 98/98 PASS, 실패/건너뜀/미확정0, 6.9초. RuleTests32 원본 코드는 변경되지 않았다.
- m5-gui-acceptance-check.json: PlayMode completed, 8/8 PASS, 실패/건너뜀/미확정0, 29.4초. 위 M2 GUI 결과는 초기 검증 이력이다.
- m5-so-edit-evidence.json: 실제 SO의 탐험/전투 등급 행을 제어된 100% Legendary 가중치로 변경하자 새 런 등급이 Uncommon,Common,Uncommon에서 Legendary,Legendary,Legendary로 바뀌었다. 이전 저장 스냅샷과 동일 RNG 재실행 상태가 유지됐고 SO 원본 복원도 확인됐다. 이 결과는 한 번의 표본으로 일반 확률 분포를 추정한 결과가 아니다.
- M1 검토1/1·수정2/2·필수 결함 해결 상태를 유지한다. 이번 동기화는 위 3개 규칙/테스트 C#의 변경 없이 실제 연결과 후속 검증 증거만 반영했다. Android 빌드·기기 검증 범위는 이 문서의 증거로 판단하지 않는다.