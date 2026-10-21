# 주사위 기본 조합 연출 결과

상태: 구현 및 지정 검사 완료. 정지 미리보기의 마지막 육안 확인은 사용자의 Esc로 중단됐으며, 후속 DICE_COMBO_AURA 작업에서 확장 결과를 확인한다. 이 문서는 이전 작업의 실제 증거를 정리한다.

- 주사위 6개 한 행, TMP 한글 조합 이름/색, FEEL 배너 확대/카드 선택 강조, Text Animator 글자 등장, All In 1 배너 shine을 연결했다. 등장 모션은 짧게 정착하고 기존 hold 시간 동안 결과를 유지한다.
- 게임 규칙/조합 판정/RNG/저장/확정 횟수와 0초 계약은 유지했다. 사용자 승인에 따라 미저장 Title 씬을 저장했으며 그 이후 해당 씬은 기능 편집 대상에서 제외했다.
- 실제 Unity 6000.6.0f1 / Pipeline 0.6.0-exp.1에서 컴파일 성공, 지정 검사 67개 통과. 근거는 artifacts/dice-presentation-effects/ 아래의 presentation-tests-final.json(9), seed-tests.json(18), combat-tests.json(14), boundary-tests.json(5), core-replay-tests.json(14), workbench-tests.json(7). 필터로 0개 실행된 초기 결과는 PASS 근거에 포함하지 않는다.
- Unity 실제 버튼 입력으로 720×1280의 공격 13/적 HP 26→13 및 투 페어, 720×1600의 방어 선택과 원 페어 결과를 확인했다. native-combat-1280, native-dice-1280, native-dice-1600의 PNG와 같은 번호 txt에 기록했다. 최종 정지 preview는 OpenDice 실행 및 실제 TMP mesh 검사로 통과했지만 그 마지막 native screenshot 요청은 Esc로 중단됐다.
- FEEL 6.1은 소유한 로컬 캐시에서 core 517항목을 선별해 Unity로 import했다. 사용자가 Missing signature의 Import Anyway를 직접 승인했다. feel-import-manifest.json/feel-verify.json이 정확한 경로와 비교 근거다. 960 payload/meta 비교 중 12 PNG.meta만 Unity TextureImporter가 갱신했고 GUID는 보존했다. vendor C#/DLL은 수정하지 않았다.
- 선별 목록의 pathname 메타데이터를 보정한 결과 framework SO6/Scene6/Prefab2가 포함된 것을 확인했다. 기존 초기 '0개' 추정은 정정하며 정확한 목록은 feel-verify.json.serialized_assets. 이 예제 씬은 제작 씬/build 목록에 연결하지 않았다.
- DPE-R01 긴 hold 중 글자 모션 지속과 DPE-R02 전역 자산 저장은 보완1/2로 해결. DPE-R03 비활성 Canvas 정지 preview의 TMP NullReference는 보완2/2로 해결했다. 관련 재검사 9+7개 통과. 남은 필수 코드 Finding 없음. 보완 예산은 2/2 사용 완료하며 후속 작업에서 초기화하지 않는다.
- 코드 1:1 CodeMap과 직접 관계/INDEX, 검증 도구 PresentationProbe.cs의 요약을 갱신했다. final-verification.json은 마지막 보호 hash/저장/HEAD 불변 검사다. 기존 lock 변경과 Plugins/Samples/TMP/사용자 문서는 보존했다. 커밋/스테이징 없음.
- 모바일 기기 빌드/실기기 성능은 실행하지 않았다. 시각 후속 요청: 주사위별 링/빛과 조합별 큰 오라는 DICE_COMBO_AURA_PLAN에서 별도 명시 확장한다.
