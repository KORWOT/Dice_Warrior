# SHARED_WORLD_MAIN_02 PLAN

IMPLEMENT · 2026-10-22 · 완료. 사용자 후속 요청에 따라 메인 전환을 별도 로컬 커밋으로 기록한다.

## 요청·기준

- 사용자 요청: 현재까지 먼저 커밋하고 기존 모험판을 삭제, AI 공동 모험을 메인으로 전환한다. 선행 C0~C5는 `73020d1`에 정확한118파일로 커밋했다. push 없음.
- 새 기준 `73020d1`, staged0. 기존 사용자 변경8개는 이전 보호 목록/해시와 동일하며 편집·staging하지 않는다. 실행 중인 기본 Mock 서버/실제 DB/사용자 저장도 보존한다.
- 새 작업은 기존 Title/공동 세계 흐름의 전환이다. 기존 C0~C5 완료 판정은 재개방하지 않는다. 선행 커밋 이후 변경은 사용자의 2026-10-22 추가 승인에 따라 코드·자산·문서·증거를 별도 커밋한다. 기존 사용자 변경8개 제외, push 없음.

## 계약·필수 AC

- Title에서 공동 세계 로비가 직접 열린다. 기존1인 모험 시작/이어하기/복귀와 보드 화면은 제거한다. 인간1+AI3, 개인3바퀴/최종시련/30라운드/공동순위 규칙은 유지한다.
- 새 로비에서 시작 주사위·무기 선택, 서버 프로필 재조회·기존 영구 성장 구매를 유지한다. 경기 시작 시 최신 서버 성장과 선택을 네 참가자에게 동결한다. 진행 중 경기는 로비 복귀/재시작 후 이어진다.
- SymbolLoopApp을 신규 게임의 필수 서비스로 남기지 않는다. 필요한 LoopSession/Store/GameBackendCoordinator/HttpGameBackend와 전투 Config/Playback/Graphic은 재사용한다. 이전 run·pending·영수증은 별도 계정 저장에 보존하며 신규 경기로 자동 변환/제출하지 않는다.
- 서버 미접속·설정 오류·프로필/체크포인트 손상·미결 요청은 명시 오류/재확인·기존 복구 정책으로 처리한다. 임의 초기화 없음. 저장 성공 후만 경기 명령을 표시하고 비활성/지연 응답/연속 입력에 안전해야 한다.
- 사용자 추가 확정: 이전1인 active run은 기록만 보존하고 해당 계정의 영구 성장 구매를 보류한다. 자동 종료·정산·재화 지급/기록 삭제 없음. 새 공동 모험 출전 선택은 별도 값으로 적용하여 v1 RequireLobby 제약과 분리하며 시작/재개는 가능하다. 화면에 성장 보류 이유를 표시하고 active run fixture로 확인한다.
- 새 경기 서버 정산/영구 보상은 C6 후속 유지. 기존 프로토콜/서버 공유 v1 재현/권한/골든/사용량 계측은 제거하지 않는다. C6/Firebase/대량 밸런스/실기기는 비목표.

## 쓰기·삭제 경계

- 수정: SharedWorld/Presentation/CompetitionApp.cs, 필요 시 같은 기능 Runtime의 계정 연결 코드, SharedWorld/Editor/CompetitionAuthoring.cs, SharedWorld/Tests/PlayMode/CompetitionFlowTests.cs, 관련 asmdef, SymbolLoop/Tests/EditMode/SymbolLoopTests.cs의 authored 구성 검사 및 Editor asmdef 참조.
- 자산 허용: `Assets/_Project/Scenes/Title.unity`의 구형 App 오브젝트·구형 UI 등록 제거/신규 연결, `Assets/_Project/Features/SharedWorld/Prefabs/CompetitionScreen.prefab`의 기본 문구. Editor API만 사용하고 다른 오브젝트/자산/GUID는 보호한다.
- 삭제 대상: SymbolLoop/Presentation/{SymbolLoopApp,SymbolLoopScreen}.cs, SymbolLoop/Editor/SymbolLoopAuthoring.cs와 SymbolLoop.Editor.asmdef, SymbolLoop/Prefabs/SymbolLoopScreen.prefab, SymbolLoop/Tests/PlayMode/SymbolLoopFlowTests.cs와 SymbolLoop.PlayMode.Tests.asmdef. 각각의 meta/해당 CodeMap도 정리한다. 폴더가 비면 그 폴더와 meta만 Editor API로 정리한다. 구형 UI 전용 테스트7개는 삭제된 흐름을 실행하므로 신규 주 흐름의 실제 포인터/비동기/복귀 검사로 대체한다.
- 보존: SymbolLoop Domain/Backend/Persistence/Config/공유 표시 Graphic·Playback, 서버 전부, 기존 순수규칙/저장/HTTP/재현 검사. 필요하지 않은 전체 폴더 이동/대량 삭제는 하지 않는다.
- 직접 빌드 영향: Server/DiceWarrior.Competition.Tests.csproj의 Runtime/*.cs 링크를 순수 CompetitionSession/CompetitionStore2개로 명시한다. 신규 클라이언트 계정 연결이 서버 순수 코어 검사에 포함되지 않도록 하는 경계 수정이며 서버 제품 코드는 불변이다. 해당 .NET23 검사는 이 링크 변경으로 재실행한다.
- 문서: 이 PLAN/REPORT, GDD/README/Server README/CodeMap 색인, 수정 C# 1:1 요약과 직접 영향받는 관계만 갱신. 이전 완료 REPORT는 역사로 보존한다.

## 담당·상한

- 메인: 계약, Editor authoring/실제 자산 변경·정확한 삭제, 기존 EditMode 구성 검사/asmdef, 문서 색인/GDD/PLAN/REPORT, Unity 직렬 실행·통합·최종 판정.
- 보조 presentation: CompetitionApp과 필요 시 계정 Runtime, CompetitionFlowTests 및 해당 CodeMap. 구형 타입 의존 제거/직접 메인 진입/로비/계정 오류 복구/실제 회귀 검사를 구현한다. Editor/Scene/Prefab/구형 파일 삭제/Git/Unity/공유 문서 쓰기 금지.
- 보조 review: 삭제 참조·보존 경계와 계정/비동기 회귀를 읽기 전용으로 검토하고 최초 한 검토 묶음의 Finding을 보고한다. 구현/Unity/Git 쓰기 금지.
- 동시 최대3명(메인 포함), 누적 보조2명, 재귀 위임 금지. 파일당 한 작성자. 공통 API 변경은 메인과 공유한다.

## 검증·종료

- 컴파일/Title 신규 App1개·신규 UI1개/구형 컴포넌트·프리팹·진입참조0/기존 Scene GUID 보존.
- 새 실제 포인터 주흐름: 직접 로비→출전 선택/성장 조회→시작/AI 진행→로비→재개, 비활성/지연 응답/재시도, 서버 장애/저장 오류 관련 직접 경계. 두 모바일 비율/안전영역 검사 유지.
- SharedWorld EditMode23 및 수정한 SymbolLoop EditMode83 실행. 공유 규칙/서버 코드는 불변이면 완료된 이전 .NET 검사를 반복하지 않고 소스 해시로 확인한다. 삭제한 구형 UI 검사 수는 새 PASS로 세지 않는다.
- 실 사용자 DB 대신 주입 backend 또는 고유 임시 서버/저장만 검증에 사용한다. 사용자 요청으로 켜 둔5087 서버는 종료하지 않는다.
- 최초 검토1묶음, 수정·재검증 기본 총2회. 현재 검토1/1, 수정·재검증1/2. 필수 Finding 해결·실제 검증·CodeMap/문서·보호 감사 완료 후 종료.

## 직접 회귀 기록

- MAIN-01: 최초 PlayMode12개 중 로컬 주입 로비의 출전 선택1개가 raycast null로 실패했다. 진단1개 재현에서 버튼 active/interactable/raycast 및 상위 CanvasGroup은 정상이고 Graphic.depth/absoluteDepth만 -1이었다. 같은 프레임의 메뉴 전환·클릭으로 새 활성 버튼이 아직 렌더링되지 않아 uGUI가 제외했다. 실제 계정 로비 흐름은 통과했다.
- 수정1회: 해당 검사의 메뉴 전환 뒤 다음 프레임에 클릭한다. 실제 raycast·선택 값·저장·재개 assertions는 유지하며 제품 코드 변경은 없다. 최초 실패와 진단을 별도 보존했고 최종12/12 PASS로 MAIN-01을 해결했다.
- 완료: 컴파일, EditMode106/106(SharedWorld23+SymbolLoop83), PlayMode12/12, .NET23/23 PASS. Title 신규 App1/UI1·missing0·기존 GUID 보존, 구형7자산/meta 삭제, C#46개 CodeMap 누락0, 보호18파일 SHA256 동일. 최초 검토1묶음 종료·미해결 필수 Finding0. 구체적인 증거와 미검증 범위는 REPORT에 기록했다.
- 커밋 감사: 메인은 정확한 파일 목록·staged 집합·검증 소스 및 사용자 변경 해시를 확인한다. 기존 보조 review는 파일 경계·기록 증거만 읽기 전용으로 교차 확인하며 완료 구현을 재검토하거나 Unity를 실행하지 않는다. 동시2명·추가 보조 생성0명. 소스가 검증 당시와 동일하면 검사를 반복하지 않는다.
