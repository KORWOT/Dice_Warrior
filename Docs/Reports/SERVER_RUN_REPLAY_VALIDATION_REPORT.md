# SERVER_RUN_REPLAY_VALIDATION (P2) REPORT

2026-10-21 · IMPLEMENT · **완료**. 기준 [PLAN](../Plans/SERVER_RUN_REPLAY_VALIDATION_PLAN.md).

## 구현 결과와 사용자 결정

새 제출은 권한 저장소의 시작 티켓으로 별도 Run을 만들어 로그를 순차 재현한다. 합법적으로 결과 단계에 도달한 경우에만 완료 지역/승리에서 지급액을 계산하고, 프로필·영수증·런 종료·요청 결과를 함께 원자 저장한다. 기본 지급은 `완료 지역×10 + 최종 승리 20`이다.

사용자 결정 **“P2는 새 제출부터 적용하고, 기존 대기 기록은 그대로 보존”**을 적용했다. 기존 P1 PendingVerification 기록은 지급 0과 원 영수증을 유지한다. 읽기만으로 파일을 이관하거나 재검증하지 않는다. 미제출 활성 런의 정상 P1 로그는 새 제출 시 P2로 검증한다. 최초 20 Mock 재화 정책은 유지한다.

## 변경 범위

- `RunCommandRules`: Session/검증기가 공유하는 순수 명령 실행과 불변 문화권 targetId. 각 호출은 서로 다른 상태를 소유한다.
- `MockRunVerifier`: 권한 티켓 복제 → 연속 순번·고유 ID·대상·단계·비용·장비/주사위 선택 검증 → 모든 명령 재현 → 상태/종료 검사 → checked 지급액. 클라이언트 최종 승리/HP/눈/재화는 지급 입력이 아니다.
- `MockGameBackend`/`MockAuthorityStore`: 새 제출의 Approved 영수증/1회 지급, 실패 시 자산·활성 런 유지, 구형 pending 보존, 60초 창의 시작 10/새 제출 20/승인 포기 10회 제한. 같은 ID 재시도와 기존 영수증 조회는 추가 빈도를 쓰지 않는다. 시계 역행은 새 창을 열지 않는다.
- `BackendContracts`/`LoopRules`/`LoopSession`: 검증 결과 DTO, 검증 버전 `symbol-loop-replay-1`, 승인 영수증의 서버 결과/지급액과 저장 검증. 요청 형식·protocol1·rulesVersion `symbol-loop-mvp-1`은 유지한다.
- `SymbolLoopApp`: 새 기록 검증·지급, 기존 P1 대기, 실패/결과 불명 표시를 구분한다. 승인 뒤 결과 요약은 서버 영수증을 사용한다. 기존 이동·기호 주사위·스킬 연출은 그대로다.
- 신규 EditMode 두 파일과 골든 JSON, 관련 기존 검사 기대값 및 테스트 asmdef의 기존 Newtonsoft DLL 참조를 갱신했다. 새 패키지 설치는 없다.

입력 한도는 2048명령/512KiB, 명령 ID/대상 128자, 인수 1024자다. 재현은 명령당 한 번 수행하며 복사/실행 루프마다 취소를 확인한다. 오류에는 실패 코드·명령 순번이 포함된다. 같은 요청 ID로 본문을 바꾸면 거절하며 정정 제출은 새 ID가 필요하다. 검증 실패 원문은 Coordinator의 기존 보존 경로에 남긴다.

## 실제 검증

기존 Unity 6000.6.0f1 Editor 한 개를 Unity CLI/Pipeline으로 직렬 사용했다. Orca·중복 batchmode·실제 사용자 저장은 사용하지 않았다.

| 검증 | 결과 | 근거 |
|---|---|---|
| P2와 직접 영향 EditMode 전체 | **49/49 PASS**, 실패/스킵 0, 10.83초 | [실행 JSON](Evidence/RunReplay/edit-final.json) |
| 실제 포인터/연출/재개 PlayMode 전체 | **6/6 PASS**, 실패/스킵 0, 105.9초 | [실행 JSON](Evidence/RunReplay/play-final.json) |
| 고정 벡터 | **4개, 121명령/125상태 일치** | [생성 기록](Evidence/RunReplay/golden-generation.json), [고정 JSON](../../Assets/_Project/Features/SymbolLoop/Tests/EditMode/ReplayGoldenVectors.json) |
| 보호 파일·요약·meta | **719개 해시 불변**, 소유 C# 28개 요약/meta 누락 0 | [소스 감사](Evidence/RunReplay/source-audit.json) |
| 독립 소스 검토 | 필수 Finding **0건**, 문서 직접 관계 1곳 수정 | 아래 검토 기록 |

EditMode는 로그 순서/이동 누락/중복 ID/대상 위조/종료 이후 명령, 전투 단계·눈 인수 위조, 장비/슬롯/비용, 미완료 로그, 미지원 규칙/손상 티켓/상한/취소를 확인했다. 알 수 없는 최종 결과 JSON 필드는 디코드 단계에서 거절된다. 실패 뒤 프로필 재화20·활성 런은 유지되고 정정된 새 ID 제출은 승인된다.

같은 요청 10회 및 새 ID의 같은 런 10회 재제출, 최신 설정이 바뀐 경우의 시작 티켓 규칙 지급, 지급 응답 유실 뒤 로컬 저장 실패와 재시작을 확인했다. 구형 P1 fixture는 읽기 전후 파일 내용과 0지급 영수증이 일치했다. 빈도 창 영속성·동일 ID 예외·시계 역행/재개방도 검사했다.

PlayMode는 실제 기본 Config/seed33을 포인터로 완주하여 50지급·잔액70, 성장 구매 후 잔액50·다음 런 HP65를 확인했다. 말 칸별 이동, 기호 주사위/조합/기본기/처치 생략, 재생 중단 후 저장 유지, 세로 배치, 미결 시작 복구와 지연 응답 재개도 통과했다. [승인 결과 캡처](Evidence/RunReplay/result-verified.png)를 직접 확인했다.

## 골든 벡터와 이식 계약

| 시나리오 | 시드 | 명령 / 상태 | 최종 결과 | 지급 |
|---|---:|---:|---|---:|
| victory | 33 | 35 / 36 | 3지역·HP49·승리 | 50 |
| defeat | 51 | 41 / 42 | 3지역·HP0·보스 패배 | 30 |
| abandon-after-region | 19 | 13 / 14 | 1지역 뒤 포기 | 10 |
| shop-equipment-resume | 1 | 32 / 33 | 상점 구매2회·주사위/장비 교체·승리 | 50 |

모든 벡터는 시작 티켓, 명령, 초기/각 명령 뒤 RNG·보드·단계·능력치·주사위/장비·적·면/기술/반격 결과를 저장한다. 중간 Run 직렬화 재개를 포함하며 1/2/3개 주사위와 2/3기호 조합, 런 레벨 성장을 관찰한다. 테스트는 기대 파일을 생성하지 않는다. 명시적 작성 도구도 기존 파일을 덮어쓰지 않는다. 최초 후보 seed7은 승리하는 것으로 확인되어 패배 seed51을 선정한 후 한 번 생성했다.

RNG는 uint32 xorshift(0일 때 0x9e3779b9, 좌13→우17→좌5 XOR)다. 보드 RNG는 `seed XOR unchecked(region*0x85ebca6b)`로 독립 셔플하고 버린다. 이동 RNG는 seed, 전투 RNG는 `seed XOR 0xa511e9b3`에서 시작한다. 이동은 1회, 전투 진입 의도는 1회, 매 턴은 주사위 슬롯 순서대로 1회씩, 양측 생존 시 다음 의도에 1회 소비한다. UI 재생은 소비하지 않는다. 다른 언어 검증기는 이 순서와 모든 고정 상태/효과를 일치시켜야 한다.

JSON SHA256: `863de8b00364abbe6d99107c1bf191e6c8060d26a3332e7b9b78bcc6ae30090e`. 해시는 파일 동일성용이며 권한 판정은 상태 재현이다.

## 검토·문서·보호 상태

최초 통합 검토 **1/1**, 검토 후 필수 수정·재검증 **0/2**, 남은 필수 Finding 없음. 구현 담당 1명과 읽기 검토 담당 1명에 유한 위임했고 메인이 통합·Editor 실행·문서를 담당했다. 검토자는 중복 지급/불법 로그/구형 pending/빈도/결과 적용의 소스와 직접 호출자를 확인했으며 Unity 실행은 하지 않았다.

검증 준비에서 테스트 어셈블리의 Newtonsoft DLL 참조 누락과 번들 NUnit에 없는 Assert.Multiple을 수정했다. `up_to_date` 상태만으로 새 타입 로드를 확정하지 않고 Console 오류 및 실제 타입 로드를 확인했다. 그 뒤 통합 EditMode/PlayMode 묶음을 각각 한 번 실행해 위 결과를 얻었으며 이후 코드 변경은 없다. 문서 검사 담당 표기만 BackendReplayIntegrationTests로 바로잡았다.

1:1 CodeMap 신규4개 및 변경9개, 직접 관계 Coordinator 요약, INDEX(28 C#), README, GDD, PLAN/본 REPORT를 동기화했다. 과거 P1 PLAN/REPORT/증거와 Archive, 원본 로드맵은 보존했다. 시작 때 이미 변경되어 있던 RP/폰트/ProjectSettings 및 사용자 설정도 719개 보호 해시에 포함해 불변을 확인했다. Scene/Prefab/기존 GUID/패키지 수정 없음. Git HEAD는 `44e6c31faf0cb7b54ed94c5a65421759ab8b399d`, staging/commit/push 없음. 기존 P1 미커밋 변경과 이번 P2 변경이 함께 작업 트리에 있다.

## 한계와 다음 범위

필수 P2 결정은 모두 반영되어 추가 사용자 결정은 없다. P3 전체 다중 기기/동시 정산·복구, P4 Firebase/Auth/Functions/Emulator, P5 Android 기기/IL2CPP 검증은 **NOT_RUN/후속**이다. 운영 데이터 이관은 하지 않았다.

Mock은 프로세스 내부 개발 대역이다. 합법적인 다른 로그 구성·자동 플레이·미래 예측을 차단하거나 관찰하지 않은 모든 행동 누락을 증명하지 못한다. 골든 생성과 재현은 같은 C# 규칙을 사용하므로 별도 언어 구현의 독립 일치 검증은 아직 수행하지 않았다. 기존 전투/경제 수치·3/4눈 설계·아트는 이번 기능 검증으로 확정하지 않는다.
