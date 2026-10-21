# LOBBY_MAP_DICE_REPORT

상태: IMPLEMENT / COMPLETE. 2026-09-08, `D:/UnityProject/Dice_Warrior`.

준비 로비와 캠페인 지도를 분리하고, 노드 도착 후 별도 창에서 여섯 주사위를 굴리는 흐름을 구현했다. Unity Editor의 실제 버튼으로 로비 → 지도 → 노드 도착 → 굴림 → 카드 → 전투 → 보상 → 지도 → 로비를 확인했다. 영구 성장의 재화·강화 규칙은 사용자 답변에 따라 다음 설계 범위로 남겼다.

## 동작과 편집 위치

- 씬 역할은 `Title → Lobby(출전 준비) → InGame(캠페인 지도와 전투 UI)`다. 전투 전용 환경이나 별도 로딩 요구가 없어 Battle 씬은 추가하지 않았다. 기존 세 씬의 파일과 연결은 유지했다.
- 준비 로비는 캐릭터·세팅·성장 안내 탭과 새 여정·이어하기를 제공한다. 현재 구현된 캐릭터는 기존 방랑자 1명이다. 출발 능력치·기본 행동·주사위 6개 정보를 표시하고, 기존 시련·탐험 등급 상한을 출전 설정으로 사용한다. 시드는 일반 로비에서 숨기고 작업실에서 편집한다. 성장 탭은 현재 여정의 성장과 추후 영구 성장 안내다.
- 캠페인 지도는 실제 노드 ID와 연결 관계를 사용해 아래에서 위로 진행한다. 선택 가능한 갈림길, 미래 노드, 완료 경로, 현재 위치를 구분하고 공유 자식은 한 번만 표시한다. 완료 경로는 `resolvedEventIds` 순서로 `nodeHistory`에서 표시용 정보만 추출하며 버린 가지는 완료 경로에 포함하지 않는다. 기존 2단계 공개 규칙과 게임 난수는 바꾸지 않았다.
- 노드를 누르면 선택 결과를 먼저 저장하고 도착 연출 후 주사위 창을 연다. 창의 `굴리기`를 눌러야 실제 굴림이 한 번 실행·저장된다. 여섯 면의 눈금·회전·움직임을 표시하고 실제 결과로 고정한 뒤 카드 선택으로 이어진다. 이동·굴림 중 중복 입력과 창 아래 입력을 차단한다. 창을 다시 열거나 저장을 재개해도 무료 재굴림이 생기지 않는다. 전투 턴 굴림도 같은 창을 사용한다.
- `Fate Dice > 플레이 작업실`: 시작 지점·시드·시련·등급 상한·세로 비율을 고른 뒤 `상황 미리보기`, `6주사위 창 미리보기`, `UI 원본 편집`, `주사위 창 원본 편집`, `격리 플레이 시작`을 사용한다. 미리보기는 Scene 탭에서 보며 배치 변경은 원본 프리팹에서 저장한다. 격리 플레이는 테스트 전용 저장을 사용하고 중지하면 제작 씬을 복원한다.

| 대상 | 프로젝트 상대 경로 | 편집 내용 |
|---|---|---|
| 준비 로비 | `Assets/_Project/Features/Run/Prefabs/MenuUI.prefab` | 탭·패널·글자·출전 버튼 |
| 지도 화면 | `Assets/_Project/Features/Run/Prefabs/ExplorationUI.prefab` | CampaignMapView의 간격·색상·영역·현재 위치 |
| 지도 노드 | `Assets/_Project/Features/Exploration/Prefabs/ExplorationNodeView.prefab` | 노드 모양·표시·터치 영역 |
| 여섯 주사위 창 | `Assets/_Project/Features/Run/Prefabs/DiceRollUI.prefab` | 창·안내·3×2 주사위 면·굴리기 버튼 |
| 화면 등록 | `Assets/_Project/Shared/UI/Prefabs/UIRoot.prefab` | 기존 목록에 DiceRollUI popup 1개 추가 |

## 실행 검증

증거 폴더: `C:/Users/coli4/OneDrive/문서/ChatGPT/Dice_Warrior/artifacts/lobby-map-dice/`. 이 폴더의 원본 JSON과 스크린샷을 기준으로 기록했다. Unity 6000.6.0f1의 이미 실행 중인 Editor와 CLI Pipeline을 사용했으며 동일 프로젝트를 중복 실행하지 않았다.

| 검증 | 실제 결과 | 증거 파일 |
|---|---|---|
| 초기 자산 계약 RED | 주사위 popup 부재로 예상 실패 1건 | `red-status.json` |
| 최종 전체 EditMode | 128/128 PASS, 12.57초 | `edit-final.json` |
| 최종 전체 PlayMode | 63건 중 62 PASS, 외부 서비스 로그로 1 FAIL, 103.9초 | `play-final.json` |
| 외부 로그 영향 항목 단독 재실행 | 1/1 PASS, 1.41초 | `play-isolated.json` |
| 자산 작성 재실행 | 지정 프리팹 5개 바이트 동일, 정지 상태·제작 씬 dirty=false | `authoring-idempotence.json` |
| 보호 경계 | 기준 241개 중 허용된 기존 파일 18개만 변경, 보호 대상·기본 저장 보존 | `boundary-final.json` |
| 소스·문서·렌더러 | C# 66개 모두 CodeMap 존재, 변경 소스 meta 누락 없음, 주사위 6개 CanvasRenderer 실제 직렬화 | `boundary-final.json` |
| 직접 클릭 완료 경로 | 사건 1회 완료, 골드 17, 노드 13개 중 완료 노드 표시·재선택 불가, 이어하기 전후 RNG 동일 | `native-completed-map.json`, `completed-map.png` |

전체 PlayMode의 유일한 최종 실패는 `CombatLayoutTests.PaidDieAndOfferedCardClicksMatchTheRealCommandAndStoredState` 실행 중 기존 Unity AI ModelSelector가 `generators.ai.unity.com` 관련 Unknown 로그를 남겨 발생했다. 로그 무시나 패키지 변경 없이 같은 테스트를 스크립트 재로드 후 단독 실행해 통과했다. 서로 다른 PlayMode 63개 항목에 모두 통과 증거가 있지만, 단일 전체 실행 63/63 통과라고 주장하지 않는다. Pipeline의 테스트 검색 캐시 때문에 completed/0을 반환한 호출은 미실행으로 취급했고, 스크립트 재로드 후 실제 실행 결과를 사용했다.

자동 검사는 720×1280·720×1600에서 준비 로비 탭·세팅·새 여정·이어하기, 실제 포인터와 raycast, 지도 분기/합류·완료 경로, 노드 이동 checkpoint, 주사위 창과 6면, 중복 입력·하부 입력 차단, 결과/RNG/저장 일치, 재개 및 기존 유료 재굴림, 원본 프리팹 편집 전파·캐시 재사용·원본 바이트 복원을 포함한다.

직접 클릭은 Unity 창만 조작했다. 준비 로비 미리보기와 격리 플레이, 세팅·성장 탭, 이어하기, 전투 노드 이동과 도착, 여섯 주사위 굴리기, 운명 카드 선택, 전투 2턴의 굴림·강공격, 승리 보상 수령, 완료 지도와 로비 복귀를 확인했다. 마지막에는 앞선 직접 플레이에서 생성한 완료 저장을 테스트 전용 저장으로 연결한 후 직접 이어하기를 눌러 완료 경로를 다시 확인했다. 이 저장 연결만 CLI로 준비했고, 이후 전환은 native 클릭이었다. 새 여정·유료 재굴림은 자동 포인터 검사로 확인했으며 이번 native 검사로 대신 주장하지 않는다.

## Finding과 수정 횟수

최초 검토 1묶음, 수정·재검증 2묶음을 사용했다. 마지막 외부 로그 재실행은 코드 변경이나 일반 재검토 없이 동일 검증 항목만 다시 실행했다. 미해결 필수 Finding은 없다.

| ID | 발견 근거·영향 | 해결 및 확인 |
|---|---|---|
| LMD-B01 / P1 | 여섯 custom Graphic의 CanvasRenderer가 실제 prefab에 빠져 주사위 표시 및 공통 버튼 원본 편집 중 경고 발생 | 필수 컴포넌트 선언과 새 면 생성 시 추가. 최종 수정에서 기존 popup도 LoadPrefabContents/SaveAsPrefabAsset으로 저장해 import 시 자동 보완된 renderer까지 디스크에 보존. 원본 편집 회귀 및 실제 직렬화 6개 확인 |
| LMD-C01 / P1 | 정지 미리보기의 초기 폭이 100인 시점에 지도 배치되어 작은 노드·문자 잘림 발생 | 최종 Canvas layout 이후 지도 RefreshLayout 및 rebuild. 두 비율 EditMode 글자·미리보기 검사 통과 |
| LMD-C02 / P2 | 완료한 경로가 지도에 남지 않아 진행 이력을 읽기 어려움 | 표시 DTO에 실제 완료 노드만 전달하고 지도에 연결. 두 사건 완료·버린 가지 제외·ID 유일성·저장 재개 회귀 및 직접 완료 노드 확인 |
| LMD-A01 / P1 | 기존 UiStructureTests가 옛 TRIAL 경로를 직접 찾아 새 로비에서 NullReference | MenuUI의 실제 typed 참조와 세팅 탭으로 접근하도록 검사 어댑터 갱신. 원본 수정 전파·캐시·상태·정확한 바이트 복원 assertion 유지 및 재오픈 글자 assertion 추가. 최종 PlayMode 통과 |

## 변경 범위와 문서

- 기준 Git: `main`, `433edddc872433ead6db52a872ea0fcac659d92e`. 작업 전 상태는 `baseline-git-status.txt`, 원본 241개 해시는 `baseline-hashes.json`, 원본 백업은 `backup/`에 보관했다. 커밋·스테이징·브랜치 변경은 수행하지 않았다.
- C# 기존 14개 수정·7개 추가, 기존 프리팹 4개 수정·주사위 창 1개 추가. 프리팹은 허용된 Editor API로 작성했다. `RunState`, `RunSession`, 게임 규칙·저장 스키마·설정 원본, 제작 씬, 패키지, ProjectSettings, 기존 폰트·이미지는 변경하지 않았다.
- 변경 C# 21개 CodeMap과 직접 관계 6개(RunScreenView/UIManager/SceneFlowController/RunSession/ExplorationRules/SceneStructureAuthoring)를 동기화했다. 변경 대상 PlayWorkbenchSession의 CodeMap도 포함했다. INDEX의 프로젝트 소스 수는 66개이며 새 7개 색인과 PLAN/REPORT 연결을 추가했다.
- 기존 기본 저장 `C:/Users/coli4/AppData/LocalLow/DefaultCompany/Dice_Warrior/FateDiceLocal/run.json`의 SHA-256은 시작/종료 모두 `04A0190BC3D666697D3E329807A08025519C6AAF910B3BF0833FA525D54EAFF9`다.
- 종료 시 플레이를 중지하고 원래 Title 제작 씬을 복원했다. 이어서 Scene 탭에 저장되지 않는 준비 로비 미리보기를 열어두었다. 원본 제작 씬은 dirty=false다.

## 남은 범위

Android/iOS 실기기·APK/IPA 빌드·노치 및 실제 터치 성능은 이번에 실행하지 않았다. 새 캐릭터·영구 성장 경제는 사용자와 다음에 정한다. 그림·아이콘을 새로 추가하지 않았으므로 노드와 캐릭터는 기존 글자·도형 중심 표현이며, 아트 완성도를 최종 구현으로 주장하지 않는다.
