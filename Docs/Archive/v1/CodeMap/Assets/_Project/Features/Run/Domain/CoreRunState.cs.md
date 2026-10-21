# CoreRunState.cs

- 역할/소속: FateDice.Core Domain의 실제 런 상태. RunStateData를 상속하고 config에 RunRulesCatalog만 보관한다. 표시 설정·SO·파일 기술이 없다.
- 입출력: Rules는 config를 반환한다. DeepCopy()는 RunStateCopy.ToCore(this)를 통해 규칙·가변 상태를 모두 독립 복제한 CoreRunState를 반환한다.
- 직접 관계: RunApplication이 소유하고 명령 후보를 생성한다. RunStateCopy는 Runtime RunState를 같은 순수 상태로 변환한다. 규칙 5개와 RunStateValidator는 RunStateData/Rules 경계를 사용한다.
- 상태/수명: 외부 DTO와 카탈로그를 공유하지 않는 세션 소유 객체다. 후보 저장 성공 전에는 공개 상태가 교체되지 않는다. 클래스 자체는 RNG 소비나 저장을 수행하지 않는다.
- 검수: config null은 복제에서 그대로 보존하고 유효성 판단은 검증기에서 한다. 시간·RNG·후보 ID를 복제 과정에서 새로 생성하지 않는다. 전체 실행 결과는 메인 REPORT를 따른다.
