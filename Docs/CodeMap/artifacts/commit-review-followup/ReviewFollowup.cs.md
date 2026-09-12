# ReviewFollowup.cs

- 역할: CR-01의 제한된 Editor API 수정과 상태 확인용 로컬 보조다. Assets 밖에서 Pipeline run_script로 호출한다.
- 입력/출력: Inspect는 열린 씬·dirty·입력 컴포넌트·Play/PrefabStage·실제 저장 경로를 반환한다. RemoveTitleInput은 이미 열린 clean Title에서 별도 EventSystem을 제거하고 그 씬만 저장한 결과를 반환한다. ApplyDiscounts는 clean 기본 SO의 기존 증가 배율을 확인하고 새 기본 배율 [1,.95,.9,.85,.8]만 작성한다. 예상 밖 배율이면 사용자 변경으로 간주해 중단한다.
- 핵심 동작: MainStage/stopped/compiled/clean을 요구하고 입력 오브젝트 수·이름·root·자식 없음·세 컴포넌트 유형을 검증한다. 예상 밖 상태에서는 쓰기 전에 실패한다. 입력이 이미 없으면 무변경 종료한다. Undo 삭제와 EditorSceneManager.SaveScene을 사용한다.
- 관계: UnityEditor/SceneManager/EventSystem/InputSystemUIInputModule 및 FateDiceConfig.Snapshot/Validate/ShopRules.Price를 사용한다. Main의 CLI 검증이 호출하며 게임 런타임은 사용하지 않는다. 할인 후보를 복사/검증한 뒤 Undo.RecordObject와 SaveAssetIfDirty로 그 SO만 저장하고 다섯 등급의 실제 가격을 반환한다. 공용 UIRoot·저장 데이터는 수정하지 않는다.
- 수명/검수: 런타임 컴파일이나 게임 배포 대상이 아니다. 코드 자체는 에디터 호출 전 작성되며 실행 증거는 COMMIT_REVIEW_8C76732_FOLLOWUP_REPORT와 artifacts의 JSON을 따른다.
- 도구 진단: InspectFilter는 TestRunnerApi의 실제 PlayMode 목록과 설치 Pipeline의 선택 결과를 반환한다. RunDrag/RunPriceRed는 기존 Pipeline 실행기를 같은 필터로 호출하는 진단 경로다. 공급사 패키지를 수정하지 않으며 0건 결과를 PASS로 간주하지 않는다. 테스트 선택·실행 완료는 별도 실제 결과 JSON으로 확인한다.
- 빌드 부작용 보존: RestoreBuildFontCache는 TMP 빌드 전처리가 비운 Pretendard-Effects SDF.asset만 작업 시작 hash와 일치하는 원본 바이트로 복원한다. stopped/compiled/MainStage/no-build 및 clean 글꼴, 백업 hash와 직전 확인 대상 hash를 요구하며 FileUtil.ReplaceFile/ImportAsset으로 적용한다. .meta hash와 원본 바이트를 재확인하고 문자/글리프 수·아틀라스 폭을 반환한다. 새 글꼴 저작이나 설정 변경 경로가 아니다.
- RestoreBuildSettingsFile은 InputSystem 후처리로 메모리 preloaded 배열이 원래 0개로 복원됐지만 디스크에만 추가 참조가 남은 경우를 보존 처리한다. 시작 원본 및 확인된 빌드 후 hash가 정확할 때에만 Editor FileUtil로 ProjectSettings.asset 원본 바이트를 복원한다. 메모리 설정을 변경하거나 전체 SaveAssets를 호출하지 않는다.
