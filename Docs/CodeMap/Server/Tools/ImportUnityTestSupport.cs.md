# ImportUnityTestSupport.cs

- 역할: 빌드된 Authority의 Editor 전용 Unity 회귀 DLL 배치.
- 핵심 동작: Tests/Support 폴더를 Editor API로 생성→정확한 Authority DLL 복사→PluginImporter에서 AnyPlatform false/Editor true/명시 참조 설정.
- 입출력: Server/Authority/bin/Debug/netstandard2.1 DLL→Assets 테스트 전용DLL/meta. Scene/Prefab/다른 플러그인은 편집하지 않는다.
- 사용하는 대상/쪽: AssetDatabase/PluginImporter/File; Unity CLI run_script와 개발자 재생성 절차.
- 관계 근거: 기존 P1~P3 테스트만 두 test asmdef의 precompiledReferences로 사용한다. 플레이어 의존성은 VerifyUnityBoundary로 확인한다.
- 상태/수명: 명시 실행 한 번. 기존 meta/GUID 유지, 같은 소스 재빌드 시 DLL만 갱신.
- 검수: Unity6000.6의 IsExplicitlyReferenced는 internal property이므로 확인된 이름을 reflection으로 설정한다. 없으면 실패하고 임의 설정을 우회하지 않는다.
