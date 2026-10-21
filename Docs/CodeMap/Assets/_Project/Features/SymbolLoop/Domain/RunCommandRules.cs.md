# RunCommandRules.cs

- 역할: 로컬 세션과 권한 재현이 공유하는 Unity 비종속 런 명령 규칙.
- 핵심 동작: travel은 Map에서 이동 RNG 한 번으로 D6를 정하고, combat은 전투 주사위를 굴린다. choose/lap은 등록된 선택과 단계 검사를 거치며 abandon은 결과 단계로 끝낸다. 종료 후 추가 명령과 알 수 없는 종류/잘못된 인수는 예외다.
- 입출력: Apply(Run, kind, arg)는 전달받은 Run만 변경한다. Target(Run)은 InvariantCulture의 `region:position:Phase:turn` 문자열이다. start/grow/settle/loadout은 처리하지 않는다.
- 사용하는 대상: LoopRules, Phase, FormattableString.Invariant. 사용하는 쪽: LoopSession.Execute, MockRunVerifier.Verify, RunReplayTests와 통합 검사.
- 관계 근거: Session과 검증기의 직접 호출. ID/순번/권한 티켓/영구 지급은 호출자의 책임이다.
- 상태/수명: 저장소·UI·전역 RNG가 없으며 런 소유의 RNG/규칙만 사용한다. 호출자는 별도 복제 상태에 실행하고 성공 후 확정한다.
- 검수: 지역 1/2 lap의 기존 aegis 인수도 유지하되 등록되지 않은 장비 ID는 거절한다. 엔진 공유가 최종 클라이언트 상태 신뢰를 의미하지 않는다.

- 서버 관계: MockRunVerifier는 Server/DiceWarrior.Authority에 있으며 같은 Domain 소스를 링크 컴파일하여 Unity 세션과 동일 명령 규칙을 사용한다. 두 실행 상태는 별도다.
