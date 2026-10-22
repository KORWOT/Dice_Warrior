# 현재 코드 색인

기준: [GDD v0.3](../Dice_Warrior_GDD_v0.3.md), [교체 PLAN](../Plans/SYMBOL_LOOP_REPLACEMENT_PLAN.md).
구형 기능 코드 요약은 [v1 이력](../Archive/v1/CodeMap/INDEX.md)으로 이동했다.
Unity 프로젝트 소유 C# 46개, Server C# 28개와 일회성 정리 도구 1개의 요약을 유지한다. 독립 서버 경계는 [M0~M3 PLAN](../Plans/STANDALONE_MOCK_SERVER_01_PLAN.md), 계측·비용은 [M4 PLAN](../Plans/STANDALONE_MOCK_METRICS_04_PLAN.md), 공동 세계 규칙은 [C0~C5 PLAN](../Plans/SHARED_WORLD_COMPETITION_01_PLAN.md), 현재 메인 진입은 [MAIN_02 PLAN](../Plans/SHARED_WORLD_MAIN_02_PLAN.md)을 따른다.

## 공동 모험 메인 SharedWorld

| 기능 | 스크립트 / 요약 | 현재 역할 |
|---|---|---|
| 경기 자료 | [CompetitionData](Assets/_Project/Features/SharedWorld/Domain/CompetitionData.cs.md) | 세계·참가자·사건·점수·명령 DTO |
| 버전 콘텐츠 | [CompetitionContent](Assets/_Project/Features/SharedWorld/Domain/CompetitionContent.cs.md) | JSON 장소/후보/정책의 참조·조건 검증 |
| 분리 난수 | [CompetitionRandom](Assets/_Project/Features/SharedWorld/Domain/CompetitionRandom.cs.md) | 고정 문자열 혼합과 개인별4스트림 |
| 경기 규칙 | [CompetitionRules](Assets/_Project/Features/SharedWorld/Domain/CompetitionRules.cs.md) | 동일 시작 전력·성장·상태/순위 검사 |
| 사건 | [CompetitionEncounterRules](Assets/_Project/Features/SharedWorld/Domain/CompetitionEncounterRules.cs.md) | 착지 제안 고정·조건/비용·전투·보상 |
| 전투 재사용 | [CompetitionCombat](Assets/_Project/Features/SharedWorld/Domain/CompetitionCombat.cs.md) | 기존 SymbolCombat 임시 어댑터 |
| AI | [CompetitionAi](Assets/_Project/Features/SharedWorld/Domain/CompetitionAi.cs.md) | 동일 합법 명령을 사용하는 결정적3정책 |
| 경기 명령 | [CompetitionCommandRules](Assets/_Project/Features/SharedWorld/Domain/CompetitionCommandRules.cs.md) | 대상·인간명령·AI라운드·완주/종료 |
| 세션 | [CompetitionSession](Assets/_Project/Features/SharedWorld/Runtime/CompetitionSession.cs.md) | 후보·중복/세대 검사·저장 후 공개 |
| 별도 저장 | [CompetitionStore](Assets/_Project/Features/SharedWorld/Runtime/CompetitionStore.cs.md) | schema1·CAS·백업/원문·완료기록 보존 |
| 계정 연결 | [CompetitionAccount](Assets/_Project/Features/SharedWorld/Runtime/CompetitionAccount.cs.md) | HTTP 권한·기존 기록 보존·별도 출전 선택·성장/미결 복구 |
| 화면 흐름 | [CompetitionApp](Assets/_Project/Features/SharedWorld/Presentation/CompetitionApp.cs.md) | 직접 메인 로비·출전/성장·경기 재개/복구 화면 |
| 화면 | [CompetitionScreen](Assets/_Project/Features/SharedWorld/Presentation/CompetitionScreen.cs.md) | 12지명·네말·성적·사건·AI요약 |
| 연출 | [CompetitionPlayback](Assets/_Project/Features/SharedWorld/Presentation/CompetitionPlayback.cs.md) | 인간 칸이동/숫자·기호/스킬·AI간략 표시 |
| 자산 연결 | [CompetitionAuthoring](Assets/_Project/Features/SharedWorld/Editor/CompetitionAuthoring.cs.md) | 신규프리팹·기존Title 보존 연결 |
| 도메인 검사 | [CompetitionRulesTests](Assets/_Project/Features/SharedWorld/Tests/EditMode/CompetitionRulesTests.cs.md) | 공정성·지리/사건독립·끝조건·84기호동치 |
| 고정 재현 검사 | [CompetitionGoldenTests](Assets/_Project/Features/SharedWorld/Tests/EditMode/CompetitionGoldenTests.cs.md) | 별도2벡터48인간명령50전체상태/RNG |
| 저장 검사 | [CompetitionStoreTests](Assets/_Project/Features/SharedWorld/Tests/EditMode/CompetitionStoreTests.cs.md) | 실패/응답유실·동시경합·AI원자성·복구 |
| UI 검사 | [CompetitionFlowTests](Assets/_Project/Features/SharedWorld/Tests/PlayMode/CompetitionFlowTests.cs.md) | 포인터·연출·재진입·두비율·로비연결 |

직접 흐름: Title → CompetitionApp → CompetitionSession → CompetitionCommandRules → 참가자별 사건/AI/SymbolCombat → CompetitionStore. 계정은 CompetitionAccount → GameBackendCoordinator → LoopSession/LoopStore/HttpGameBackend로 분리한다. SharedWorld.Runtime만 SymbolLoop.Runtime의 표시/계정 기능을 참조하고 역참조는 없다. 서버 DiceWarrior.Competition.Shared는 Domain8개를 동일 소스로 링크하며 어셈블리 이름은 Unity와 같은 SharedWorld.Core다. 새 권한 정산 API는 C6 후속이다.

## 기존 SymbolLoop 및 독립 서버

| 기능 | 스크립트 / 요약 | 현재 역할 |
|---|---|---|
| 데이터 | [LoopData](Assets/_Project/Features/SymbolLoop/Domain/LoopData.cs.md) | 규칙·런·프로필·로그·명령·영수증 |
| 전투 | [SymbolCombat](Assets/_Project/Features/SymbolLoop/Domain/SymbolCombat.cs.md) | 정확한 전체 조합, 순서대로 기본기, 턴 보호막/반격 |
| 캠페인 | [LoopRules](Assets/_Project/Features/SymbolLoop/Domain/LoopRules.cs.md) | 12칸/3지역/완주/성장/선택/보스와 검증 |
| 공통 명령 규칙 | [RunCommandRules](Assets/_Project/Features/SymbolLoop/Domain/RunCommandRules.cs.md) | 세션/권한 재현의 순수 런 명령·대상 규칙 |
| 백엔드 계약 | [BackendContracts](Assets/_Project/Features/SymbolLoop/Domain/BackendContracts.cs.md) | 버전·요청·응답·시작 티켓·명령 로그와 IGameBackend |
| 공유 JSON | [LoopJson](Assets/_Project/Features/SymbolLoop/Domain/LoopJson.cs.md) | Unity/서버 공통 직렬화·복제·SHA256 |
| 백엔드 조정 | [GameBackendCoordinator](Assets/_Project/Features/SymbolLoop/Backend/GameBackendCoordinator.cs.md) | 동일 요청 재시도·과거 정산/새 활성 런 복구·단절 진단 |
| HTTP 전송 | [HttpGameBackend](Assets/_Project/Features/SymbolLoop/Backend/HttpGameBackend.cs.md) | 실제 HTTP6개·결과 불명·원 요청 보존 |
| 개발 연결 설정 | [HttpBackendSettings](Assets/_Project/Features/SymbolLoop/Backend/HttpBackendSettings.cs.md) | 루프백·ID·제한 시간·체크포인트 격리 |
| 생성 지점 | [GameBackendFactory](Assets/_Project/Features/SymbolLoop/Backend/GameBackendFactory.cs.md) | HttpMock 선택, 자동 로컬 전환 없음 |
| HTTP 전송 검사 | [HttpGameBackendTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/HttpGameBackendTests.cs.md) | 경로·JSON·취소/시간 초과·미결 보존·설정 경계 |
| 백엔드 검증 | [BackendFoundationTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/BackendFoundationTests.cs.md) | 권한 분리·재시도·동시 구매·손상/백업 복구 |
| 재현 검증 검사 | [RunReplayTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/RunReplayTests.cs.md) | 고정 4시드·121명령 상태 비교·변조/상한 |
| 정산 통합 검사 | [BackendReplayIntegrationTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/BackendReplayIntegrationTests.cs.md) | 지급 1회·구형 대기 유지·응답 유실/저장 실패·빈도 |
| 정산 안전성 검사 | [BackendSettlementSafetyTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/BackendSettlementSafetyTests.cs.md) | 동시10정산·두 세션 구매·과거 백업·단절/취소·늦은 응답 |
| 저장 경합 검사 | [LoopStoreConcurrencyTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/LoopStoreConcurrencyTests.cs.md) | 동시 Commit·복원 세대·pending ID 보호·원문 보존 |
| 명령 | [LoopSession](Assets/_Project/Features/SymbolLoop/Runtime/LoopSession.cs.md) | 로컬 후보 저장 후 공개, 백엔드 승인 적용 |
| 저장 | [LoopStore](Assets/_Project/Features/SymbolLoop/Runtime/LoopStore.cs.md) | 체크섬·원자 교체·백업·세대·미결 입력 |
| 설정 | [SymbolLoopConfig](Assets/_Project/Features/SymbolLoop/Configs/SymbolLoopConfig.cs.md) | MVP 콘텐츠 SO와 새 런 복사 |
| 연출 재생 | [SymbolLoopPlayback](Assets/_Project/Features/SymbolLoop/Presentation/SymbolLoopPlayback.cs.md) | 칸별 이동·기호 굴림·스킬/반격 순차 표시 |
| 입체 주사위 | [LoopDieGraphic](Assets/_Project/Features/SymbolLoop/Presentation/LoopDieGraphic.cs.md) | 6면 회전·감속·결과 면 투영 |
| 벡터 아트 | [LoopGlyphGraphic](Assets/_Project/Features/SymbolLoop/Presentation/LoopGlyphGraphic.cs.md) | 기호·말·수호자·칸·효과 그림 |
| 규칙/저장 검증 | [SymbolLoopTests](Assets/_Project/Features/SymbolLoop/Tests/EditMode/SymbolLoopTests.cs.md) | 조합·완주·원자성·복구 |
| 공통 UI | [BaseUI](Assets/_Project/Shared/UI/Presentation/BaseUI.cs.md) | typed 화면 바인딩·닫기 수명 |
| 공통 UI | [UIManager](Assets/_Project/Shared/UI/Presentation/UIManager.cs.md) | 명시 등록·캐시·레이어·입력 |
| 공통 UI | [UIRoot](Assets/_Project/Shared/UI/Presentation/UIRoot.cs.md) | Canvas·safe area·레이어 |
| 공통 UI | [CommonButtonView](Assets/_Project/Shared/UI/Presentation/CommonButtonView.cs.md) | 로컬 버튼 표시·콜백 |
| 권한 저장 계약 | [IAuthorityRepository](Server/DiceWarrior.Authority/IAuthorityRepository.cs.md) | 사용자별 원자 변경·불변 티켓/영수증 |
| 구형 파일 회귀 대역 | [MockAuthorityStore](Server/DiceWarrior.Authority/MockAuthorityStore.cs.md) | Editor 검사 전용 파일 저장·최초20·원자 교체 |
| 서버 권한 서비스 | [MockGameBackend](Server/DiceWarrior.Authority/MockGameBackend.cs.md) | 시작·성장·재현 정산·멱등·빈도 |
| 서버 재현 검증 | [MockRunVerifier](Server/DiceWarrior.Authority/MockRunVerifier.cs.md) | 티켓 독립 재생·불법 로그 거절·지급액 |
| SQLite 권한 저장 | [SqliteAuthorityRepository](Server/DiceWarrior.Storage.Sqlite/SqliteAuthorityRepository.cs.md) | WAL·스냅샷/CAS·유니크 키·원자 확정 |
| 서버 진입점 | [Program](Server/DiceWarrior.MockServer/Program.cs.md) | Development 전용 HTTP 호스트 |
| 서버 설정/콘텐츠 | [MockServerSettings](Server/DiceWarrior.MockServer/MockServerSettings.cs.md) | 루프백·DB·콘텐츠 해시·명시 시험 옵션 |
| 개발 신원 | [DevelopmentIdentityResolver](Server/DiceWarrior.MockServer/DevelopmentIdentityResolver.cs.md) | 루프백 개발 헤더·계정 제한 |
| HTTP API | [AuthorityEndpoints](Server/DiceWarrior.MockServer/AuthorityEndpoints.cs.md) | 6API·입력/신원 경계·오류·시험 장애 |
| 안전 진단 계약 | [AuthorityDiagnostic](Server/DiceWarrior.Authority/AuthorityDiagnostic.cs.md) | 선택 observer·예외 무영향·실제 replay/duplicate/SQL 관측 |
| 계측 DTO | [MeasurementContracts](Server/DiceWarrior.Metrics/MeasurementContracts.cs.md) | HTTP/권한 결과 분리·표본/보관·현재 호스트 게이지 |
| 계측 저장 | [MetricsCollector](Server/DiceWarrior.Metrics/MetricsCollector.cs.md) | 가명 키·유한 journal·누락/손상 표시·fail-open |
| 계측 집계 | [MetricsAggregation](Server/DiceWarrior.Metrics/MetricsAggregation.cs.md) | UTC 일별/API/런 집계·nearest-rank 지연 |
| 가상 문서 계획 | [FirebaseOperationModel](Server/DiceWarrior.Metrics/FirebaseOperationModel.cs.md) | SQL과 독립된 정상/중복/오류/미산정 가정 |
| 비용 계산 | [CostEstimator](Server/DiceWarrior.Metrics/CostEstimator.cs.md) | 최소/기준/최악·일별 무료 구간·저장/전송/Cloud Run |
| 요금표 | [PricingCatalog](Server/DiceWarrior.Metrics/PricingCatalog.cs.md) | 서울 Standard/USD JSON·공식 근거·누락은 미산정 |
| HTTP bytes | [MeasuredHttpStream](Server/DiceWarrior.MockServer/MeasuredHttpStream.cs.md) | 실제 엔터티 read/write bytes, 본문 미보관 |
| 계측 화면/API | [MetricsEndpoints](Server/DiceWarrior.MockServer/MetricsEndpoints.cs.md) | 로컬 dashboard·집계/JSON/CSV·오프라인 비용 |
| 계측 저장 검사 | [MetricsCollectorTests](Server/DiceWarrior.Metrics.Tests/MetricsCollectorTests.cs.md) | 병렬·재시작·보관/개인정보·장애 |
| SQL 계측 검사 | [SqliteMetricsTests](Server/DiceWarrior.Metrics.Tests/SqliteMetricsTests.cs.md) | 실제 SQL/행·observer 예외에도 지급1회 |
| 비용 검사 | [CostEstimatorTests](Server/DiceWarrior.Metrics.Tests/CostEstimatorTests.cs.md) | 손계산·무료/단위/가격 경계·가상 작업 구분 |
| M4 HTTP 검사 | [TelemetryHttpTests](Server/DiceWarrior.HttpTests/TelemetryHttpTests.cs.md) | 실제 계측·응답유실·미산정·대시보드/내보내기 |
| 서버 골든 검사 | [GoldenReplayTests](Server/DiceWarrior.Tests/GoldenReplayTests.cs.md) | 4벡터/125상태·직렬화 문화권 동치 |
| SQLite 검사 | [SqliteAuthorityTests](Server/DiceWarrior.Tests/SqliteAuthorityTests.cs.md) | 동시 정산·구매·사용자 격리·원자 실패/재시작 |
| HTTP 통합 검사 | [HttpAuthorityTests](Server/DiceWarrior.HttpTests/HttpAuthorityTests.cs.md) | 실제 프로세스·두 호스트 경합·복구·ID 경계 |
| 시험 호스트 수명 | [ServerProcess](Server/DiceWarrior.HttpTests/ServerProcess.cs.md) | 고유 임시 DB·포트·자신의 서버 종료 |
| 테스트 DLL import | [ImportUnityTestSupport](Server/Tools/ImportUnityTestSupport.cs.md) | Editor API로 DLL 전용·명시 참조 설정 |
| Unity 경계 확인 | [VerifyUnityBoundary](Server/Tools/VerifyUnityBoundary.cs.md) | 플레이어 권한 참조 없음·Config 동치·시험 URL |
| 정리 증거 | [RemoveLegacy](artifacts/symbol-loop-replacement/RemoveLegacy.cs.md) | 승인된 구형 자산 삭제와 직접 참조 검사 |

보존된 v1 경로: GameBackendCoordinator → LoopSession → RunCommandRules → LoopRules/SymbolCombat. 기존 서버 기록 재현/계정 복구를 유지하며 새1인 모험 UI는 없다. 후보 검증 후 LoopStore에 저장한다.
권한 흐름: App → HttpGameBackend → 실제 HTTP → DevelopmentIdentityResolver/AuthorityEndpoints → MockGameBackend → IAuthorityRepository/SqliteAuthorityRepository. Session의 profile은 캐시이며 영구 재화를 직접 확정하지 않는다.
계측 흐름: AuthorityEndpoints → RequestMeasurementScope → 안전 AuthorityTelemetry observer → 별도 MetricsCollector journal → MetricsAggregation/Firestore 가상 작업 모델 → /dev API → wwwroot 대시보드. CostEstimator는 명시 시나리오/가격 JSON만 사용하며 실제 Firebase에 연결하지 않는다. 권한 저장과 계측 파일은 별도 경계다.
새 제출 흐름: 서버 MockGameBackend → MockRunVerifier → 공유 RunCommandRules/LoopRules/SymbolCombat → SQLite 지급 영수증/프로필 원자 저장. 시작 티켓과 골든 기대값으로 재현하며 클라이언트 최종 결과는 받지 않는다.
P3 복구 흐름: Coordinator → GetRunStatusAsync → Session.ReconcileSubmission/RestoreActiveRun. 과거 로컬 상태는 Store.PreserveCheckpoint로 보존하고 서버 영수증/명령에서 복구한다. authority revision과 로컬 revision/generation이 늦은 응답·동시 저장을 차단한다.
화면은 CompetitionApp → UIManager → CompetitionScreen → CommonButtonView로 연결한다.
CompetitionPlayback → SymbolLoopPlayback → LoopDieGraphic/LoopGlyphGraphic으로 확정 전투를 재생한다. Screen은 Playback.Snap으로 저장 상태를 바로 표시한다.
Core assembly는 Unity 의존이 없고 서버 Shared가 같은 Domain 소스를 링크한다. Runtime은 Core/SharedUI/uGUI/InputSystem을 참조한다. 서버 권한 소스는 Assets 밖이며 Editor 테스트만 명시 참조 DLL을 사용한다. 플레이어 그래프에는 권한 DLL/SQLite가 없다.
구형1인 모험판은 제거했다. 남은 SymbolLoop 계약은 공유전투와 기존 계정·서버 기록 재현/복구에 사용하며 별도 게임 화면으로 진입하지 않는다.
