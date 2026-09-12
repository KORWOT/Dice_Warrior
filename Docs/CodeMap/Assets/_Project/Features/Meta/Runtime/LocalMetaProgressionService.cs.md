# LocalMetaProgressionService.cs

- 역할: 개발용 메타 권한과 프로필→런→정산 흐름.
- 핵심 동작·입출력: 소유 설정 저장, 검증된 런 생성, 시작 시 영구 성장/정산 정책 복제, 런 ID별 지급 및 요청 fingerprint 중복 방지, 성장 비용 차감을 한 후보에 적용한 뒤 IPlayerDataStore에 확정한다. 진행 중 새 시작은 명시적 포기, 미정산 결과 교체는 거부한다. 구형 저장은 읽기 전용 사본으로 이관하고 지급 0.
- 사용하는 대상·사용하는 쪽·관계 근거: Meta config/content/seed/store를 명시 주입받는다. RunSession/RunUIController가 내부 Checkpoints IRunStore를 사용한다. 값 변경만으로 UI나 scene을 조작하지 않는다.
- 상태·수명·검수 주의: 명령 실패는 Task 실패이며 저장 이전에 원본을 publish하지 않는다. 저장 후 응답 유실은 같은 요청 ID로 재시도한다. LocalDevelopment는 실제 계정 보안 검증이 아니다.
- META-R1 수정: Checkpoints는 새 명령의 sequence가 정확히 +1이고 시간이 감소하지 않는지 검사한다. 같은 sequence이면 시간 및 최종 기록 시간 외 상태가 같아야 한다. 따라서 합법적 autosave/확정 후 동일 명령 재시도는 허용하고 같은 n+1의 상충 세션은 거부한다.
- 구현 계약: Docs/Plans/META_PROGRESSION_PLAN.md. 실제 검증 상태는 Docs/Reports/META_PROGRESSION_REPORT.md와 기록된 실행 증거를 따른다.
