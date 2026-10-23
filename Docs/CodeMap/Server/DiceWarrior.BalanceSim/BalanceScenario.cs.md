# BalanceScenario.cs

- 역할: 비권위 B0/C7 기준선의 입력·정본·시드·출력 경계를 정의한다.
- 핵심 동작: 원본 v4 바이트 SHA256을 고정 검증한 뒤 CompetitionContent로 읽고 직렬화 지문도 고정한다. Validate는 변경된 정의/정책/프로필/모험가·0/중복/누락 시드·명령/시간 범위를 거절한다. xorshift32(13,17,5)로 고유 nonzero 시드를 생성하고 LF decimal 목록 해시를 제공한다.
- 입출력: LoadDefinition(path)→CompetitionDefinition; Validate(definition,seeds); GenerateSeeds(master,count)→uint[]; PrepareOutput(repo,path)→신규 절대경로. 기준은 neutral-v1, growth0/balanced/blade, 3모험가 전체다.
- 사용하는 대상: CompetitionContent, LoopJson, Profile, BackendProtocol.MaxCommands, SHA256/파일 API. 사용하는 쪽: Program, MatchRunner, ScenarioTests 및 MatchRunnerTests. 관계는 직접 호출 근거다.
- 상태/수명: 시나리오 인스턴스별 DefinitionHash만 보관한다. 출력은 artifacts/balance-sim 하위 신규 디렉터리로 제한하고 기존 경로/심볼릭 링크·junction을 거절한다.
- 검수 주의: 원본 해시 변경을 편의상 허용하지 않는다. 게임 상태에 별도 난수를 섞지 않는다. 새 정책/성장 실험은 별도 계약이 필요하다.
