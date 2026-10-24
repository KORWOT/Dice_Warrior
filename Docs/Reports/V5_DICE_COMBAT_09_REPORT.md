# V5 주사위 선택 전투 REPORT

2026-10-24 · IMPLEMENT · **완료**. [PLAN](../Plans/V5_DICE_COMBAT_09_PLAN.md)의 계약과 사용자 승인된20턴/명령 예약을 적용했다. 실제 자산 연결·Unity UI·구형 회귀와 보호 검증을 마쳤다. 커밋/푸시는 하지 않았다.

## 범위와 보호

새 v5 경기의 독립 굴림/보관/해제/리롤/실행, 물리6면과 직업 전용면/꽝, 3→2→1 기술 계획·예상, 동등 AI, 두 영역·6면 상세 UI와 저장 복구가 대상이다. v1~v4·2048명령/4MiB·계정 권한 경계는 유지한다. Firebase·C6 영구 지급·세계관·v4 밸런스 변경·커밋/푸시는 제외한다.

기준 `bd9f32a671e7b7b6934aad8902ff7a5a08f151e5`, staged0. 기존18변경과 보호 대상154파일의 해시를 `artifacts/v5-dice-combat-09/run-01/{baseline,dirty-before,tracked-before}.json`에 보관했다. 실제 사용자 저장·Mock DB·5087 서버는 검사하지 않는다. 기존 실행 도우미5파일의 미커밋 변경도 유지한다.

## 구현과 직접 관계

- Domain: v5 명시 DTO/필수 필드, face/6면/owner 검증, 선택 상태·명령 예약·AI, 공통 효과 계산을 사용하는 계획/프리뷰를 추가했다. 기존 Symbol enum/구형 정본/골든은 유지한다.
- 저장: Session의 후보 저장 후 공개/영수증 재시도 경계를 그대로 사용한다. Store는 v5 actionNumber와 실제 명령 수의 일치를 검사한다. 새 경기는 명시 Restart에서만 적용하고 이전 원문을 보존한다.
- 화면 소스: 전용 굴림·보관 영역과6면 modal, 확정 상태 기반 연출/Snap, 스탯/기술/상성/태그/효과 출처 상세를 작성했다. `CompetitionAuthoring.UpgradeV5` Editor API로 프리팹에 연결했고 Title의 새 경기 정본을 v5로 바꿨다. 기존 v1~v4 저장은 그대로 재개하며, v5 전환은 경고 후 명시적인 새 게임으로 한다. 새 주사위9개에 CanvasRenderer를 연결하고 상세창 배경은 alpha1로 차폐했다.
- 문서: GDD·Docs/Server README·CodeMap 색인과 수정/신규 C#26개의1:1 요약을 동기화했다. 원본 기획서는 수정하지 않았다.

## 실제 증거

| 구분 | 현재 결과 | 근거 |
|---|---|---|
| 초기 .NET RED | 예상대로3실패, 미지원 v5 정의 거절 | `artifacts/v5-dice-combat-09/run-01/v5-red.trx` |
| 통합 .NET/구형 회귀 | **184/184 PASS, skip0** | [TRX](Evidence/V5DiceCombat09/competition.trx), 기존119+신규65 |
| v4 동결9경기 state/RNG/command 해시 비교 | **9/9 PASS, 27해시 동일** | [대조](Evidence/V5DiceCombat09/v4-hash-comparison.json), [신규 실행](Evidence/V5DiceCombat09/v4-verification.json)을 기존 `Evidence/BalanceSimulation07/s9/verification.json`과 비교 |
| 화면/Editor/PlayMode 소스 외부 컴파일 | 최초 및 수정1회차 각3어셈블리 PASS | [요약](Evidence/V5DiceCombat09/validation.json). 실제 Unity DLL 참조, CS1701 버전 통합 및 CS2023 response 옵션 경고. 실제 Unity import/실행 검증과 별개 |
| Unity 자산/컴파일 | **PASS** | [자산 감사](Evidence/V5DiceCombat09/assets-audit.json), [컴파일](Evidence/V5DiceCombat09/unity-compile.json), [최종 읽기](Evidence/V5DiceCombat09/unity-final-readback.json): Title v5, 신규meta11, 기존GUID/object ID보존 |
| Unity SharedWorld EditMode | **184/184 PASS, skip0** | [실제 결과](Evidence/V5DiceCombat09/unity-editmode.json) |
| 신규 v5 PlayMode | **9/9 PASS, skip0** | [최종 결과](Evidence/V5DiceCombat09/unity-playmode-v5.json), 최초6PASS/3FAIL 이후 지정 Finding만 수정 |
| 기존 PlayMode | **25/25 PASS, skip0** | [최종 결과](Evidence/V5DiceCombat09/unity-playmode-legacy.json), 도메인 재로드 후 실제 실행 |
| 두비율·전개도 시각 확인 | **PASS** | [720×1280](Evidence/V5DiceCombat09/net-safe-720x1280.png), [720×1600](Evidence/V5DiceCombat09/net-safe-720x1600.png), 배경 차폐·물리면·safe area 확인 |
| 최종 보호 해시/CodeMap/패치 검사 | PASS | [보호 감사](Evidence/V5DiceCombat09/protected-audit.json), 보호16파일 해시 동일, 추적154 중 허용18파일만 변경, CodeMap26/26, 신규meta11/11, staged0. 패치 공백 경고는 아래 명시 |

Unity는 사용자 직접 재시작 후 연결됐고 현재6000.6.0f1에서 검증했다. 구형 PlayMode의 연속 실행0건은 Test Framework 캐시 문제로 확인했다. PlayerTestAssemblyProvider는 도메인 리로드 없이 Play에 진입할 때 static목록을 Clear하고, 다음 LoadAssemblies는 null이 아니라서 다시 로드하지 않는다. 제품·패키지·프로젝트 설정을 바꾸지 않고 EditorUtility.RequestScriptReload 후25개를 첫 실행해 통과했다. 발견만25개/실행0개인 이전 시도는 PASS가 아니다.

`git diff --check` 원시 종료코드는2다.90건 모두 Editor가 저장한 CompetitionScreen.prefab의 빈 m_Name/m_Text/iconId 뒤 공백이며 그 외 경고는0이다. 자산 보호 규칙에 따라 YAML을 수작업으로 재작성하지 않았다. [원시 경고와 보호 결과](Evidence/V5DiceCombat09/protected-audit.json)를 보존한다.

신규 실제 파일 경기(룬술사/seed456/growth2)는92명령에서 끝났고 AI 포함 전체 상태를 매 명령 재생과 대조했다. 실제 envelope 최대88,409바이트다. 별도의2048명령 형식 경계 fixture는590,326바이트였으며 마지막3명령 roll/reroll/execute로 종료했다. 이 fixture의 앞2045명령은 형식 검사용 과거 로그이고 실제2048단계 재현 경기라고 주장하지 않는다. 모험가3종의 전체 로그 재현은 각각64/92/231명령이며 중간 JSON 복원·전체 participant/RNG 해시를 검사했다.

## 요구사항별 검증 매트릭스

| ID | 검증 대상 | 상태 | 근거/방법 |
|---|---|---|---|
| V5-T01 | 최초1개 굴림/영역/리롤1 | PASS | Domain + authored PlayMode |
| V5-T02 | 탭 보관·면 불변 | PASS | Domain + 실제 포인터 |
| V5-T03 | 해제·RNG 불변 | PASS | Domain + 실제 포인터 |
| V5-T04 | 1개 보관·나머지 리롤 | PASS | 3슬롯 RNG/화면 |
| V5-T05 | 전부 리롤 | PASS | RNG/횟수 |
| V5-T06 | 전부 보관 버튼 상태 | PASS | Domain + UI |
| V5-T07 | 2회째 리롤 거절·실행 가능 | PASS | Domain + UI |
| V5-T08 | 리롤 없이 실행·반격1회 | PASS | CompetitionV5Tests |
| V5-T09 | 꽝만 실행·반격1회 | PASS | Domain + blank 실행/취소 실제 화면 |
| V5-T10 | 검/비전/꽝의2눈 조합 | PASS | CompetitionV5Tests planner |
| V5-T11 | 3눈 우선·중복 발동 없음 | PASS | CompetitionV5Tests planner |
| V5-T12 | 중복면의 물리ID/인덱스 | PASS | Domain + 전개도 |
| V5-T13 | 모험가 전용면/기술 권한 | PASS | 3모험가 + 손상 정의 |
| V5-T14 | 정확한6면·탭 상세 | PASS | authored PlayMode |
| V5-T15 | 정보 조회 불변 | PASS | 실제 포인터/저장 비교 |
| V5-T16 | 선택 단계 저장·재개 | PASS | 실제 고유 임시 파일 |
| V5-T17 | 보관 저장 실패 | PASS | fault store + 실제 오류 피드백 |
| V5-T18 | 리롤 실패·동일ID 재시도 | PASS | 실제 저장/응답 유실 |
| V5-T19 | 실행 연타1회 | PASS | 영수증 + 실제 UI |
| V5-T20 | 연출 중단·재개 | PASS | PlayMode Snap |
| V5-T21 | AI 같은 규칙·현재 눈만 | PASS | 정책/전체 경기 재현 |
| V5-T22 | 전체 로그·중간 복원 해시 | PASS | 전 참가자/RNG 비교 |
| V5-T23 | v4 그대로 재개 | PASS | Store + 신규 legacy resume + 기존25개 UI |
| V5-T24 | 명시 v5 새 게임·원문 보존 | PASS | 실제 원본 파일 비교 |
| V5-T25 | v1~v4 골든 | PASS | 기존119검사 + 동결 v4 9경기 |
| V5-T26 | 두 해상도/1~3개/안전영역 | PASS | 렌더링·포인터·bounds |
| V5-T27 | 턴/명령 상한·보관 연타 | PASS | 20턴/12토글/2048경계 |
| V5-T28 | 누락/타입/owner 오류 거절 | PASS | 필수 JSON 필드 검사 |
| V5-T29 | 순수 프리뷰·실행 동치 | PASS | 상태/RNG/피해/흡수 |
| V5-T30 | 전투 종료 선택 정리 | PASS | 다음 사건/전투 상태 |

## 검토·수정 이력과 잔여 범위

최초 통합 검토1묶음의 Domain/Store 필수 Finding은0건이었다. 누적 수정·재검증은 **4/4회**이며 사용자가 F05와 상세창 차폐에 각각 추가1회를 승인했다.

| 회차 | 지정 Finding과 해결 | 실제 결과 |
|---|---|---|
| 1 | F01 v5 효과/장비/태그/속성 상세 복구, F02 저장 로그 단계별 HP바·숫자·보호 일치 | 외부3어셈블리 컴파일PASS, 재연결 후 실제 UI 확인 |
| 2 | F03 새 UI의 실제 프레임 후 포인터 입력, F04 신규9개 Graphic의 authored CanvasRenderer | 컴파일에서 F05 GetMesh API 불일치 발견; 완료 판정하지 않음 |
| 3 (추가 승인) | F05 인자 없는 GetMesh()로 변경하고 Unity 소유 메시를 삭제하지 않음 | 실제 신규9/9 PASS, 캡처에서 V5-NET-VIS-01 배경 비침 발견 |
| 4 (추가 승인) | v5 상세창 alpha0.98→1, 차폐 단언 및 두비율 캡처 | 실제 신규9/9 PASS, 두비율 시각 확인PASS |

F01~F05와 V5-NET-VIS-01은 CLOSED, 남은 필수 Finding은 없다. 구형 검사25개는 위 채널 복구 후 통과했으며 별도 제품 변경은 없었다. [전체 검증 요약](Evidence/V5DiceCombat09/validation.json), [최종 소스/자산 해시](Evidence/V5DiceCombat09/source-hashes.json), [Editor 상태](Evidence/V5DiceCombat09/unity-status.json)를 보존한다.

최종 화면: [전투720×1280](Evidence/V5DiceCombat09/combat-safe-720x1280.png) · [전투720×1600](Evidence/V5DiceCombat09/combat-safe-720x1600.png) · [실제6면](Evidence/V5DiceCombat09/six-face-net.png) · [상세 출처](Evidence/V5DiceCombat09/v5-restored-details.png) · [1개](Evidence/V5DiceCombat09/dice-1.png) / [2개](Evidence/V5DiceCombat09/dice-2.png) / [3개](Evidence/V5DiceCombat09/dice-3.png).

Android 실기기·Firebase·C6 서버 정산·v5 대량 밸런스 실험은 미실행/후속이다. HTTP 계약을 바꾸지 않아 서버가 필요한 Explicit5개는 이번 UI 회귀에 포함하지 않았다. 실제 사용자 저장·Mock DB·5087 서버를 검사에 쓰지 않았고 커밋·푸시는 하지 않았다.

## 사용자 승인 커밋 체크포인트 (2026-10-24)

위 구현 완료 이후 사용자가 v5 구현·검증·문서만 로컬 커밋하도록 승인했다. 대상은98파일이며 코드/콘텐츠·프리팹/Title·신규meta·CodeMap·GDD/README·PLAN/REPORT·검증 증거25파일을 포함한다. 검증된 소스/자산58개 해시를 현재 파일과 다시 대조했고 변경이 없었다. 따라서 Unity나 실제 서버 검사를 다시 실행하지 않고 기존 실행 결과를 보존했다.

사용자 원문·폰트·설정13파일과 별도 서버 실행 도우미5파일의 변경은 제외한다. 혼합된 Docs/README·Server/README는 v5 부분만 index에 반영하고 실행 도우미 변경은 작업 트리에 보존한다. 나머지16개 제외 파일과 두 README 원문은 커밋 전후 해시로 보존을 확인한다. 정확한 목록과 제외 파일 사전 해시는 ignored `artifacts/v5-dice-combat-09/run-01/commit-files.txt`, `commit-excluded-before.json`에 기록한다. 커밋 결과는 Git 이력을 기준으로 하며 푸시는 요청되지 않았다.

신규 파일을 포함하는 staged 공백 검사는 기존 prefab90건에 새 JSON.meta의 빈 userData/assetBundleName/assetBundleVariant3건을 합쳐93건이다. 모두 Unity 직렬화의 빈 필드 공백이며 다른 경고는 없다. 코드·콘텐츠·자산을 추가 수정하지 않았다.
