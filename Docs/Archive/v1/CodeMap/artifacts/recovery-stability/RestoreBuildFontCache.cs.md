# RestoreBuildFontCache.cs

- 역할: Android 빌드가 자동 정리한 동적 글꼴 캐시를 빌드 직전 검증된 사본으로 되돌린다.
- 입출력: PLAN의 정확한font asset·snapshot·원본 SHA256과 검사 직후 현재 해시 → Editor FileUtil.ReplaceFile/AssetDatabase.ImportAsset → 복원 해시/.meta 불변 확인.
- 관계: 기존 TMP_FontAsset과 Editor API만 사용한다. Runtime과 빌드 제품에 포함되지 않는다.
- 수명/검수: Play/compile/build/PrefabStage/dirty scene/font를 거부한다. 현재 hash 또는 snapshot이 달라지면 쓰지 않는다. 빌드 직전0688B5CE… 복원이며 전체 Task 시작90BE8855…와의 이전 차이는 별도 보존 항목이다. 사용자 원문·다른 자산 쓰기 없음.
