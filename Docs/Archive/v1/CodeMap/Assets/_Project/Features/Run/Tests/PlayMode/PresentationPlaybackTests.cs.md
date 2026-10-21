# PresentationPlaybackTests.cs

- 역할: 연출 시퀀스가 자연 종료·취소·예외·실시간 제한을 서로 구분하고 중첩 대기를 정리하는지 검사한다.
- 입력/출력: NUnit가 Fake clock 및 독립 IEnumerator fixture를 실행한다. 사용자 저장·게임 RNG·씬·프리팹을 생성하거나 수정하지 않는다.
- 최초 RED: runtime 타입이 없는 상태에서 reflection 존재 검사 1건을 먼저 작성했다. 메인이 artifacts/presentation-lifecycle/playback-red.json에서 실제 missing-type FAIL을 확인한 뒤 typed 검사와 제품을 작성했다.
- 관계: FateDice.PlayMode.Tests가 이 fixture를 발견하며 PresentationPlayback의 공개 상태·시각·이벤트·반환 iterator를 검사한다. 제품 코드가 테스트에 의존하지 않는다.
- 핵심 동작: 현재16개 독립 행위 검사다. 동기 즉시 완료/불필요 프레임 없음, null프레임 및 중첩 안쪽부터 finally, fake clock9.999→10 정확한 경계, 실제 WaitForSecondsRealtime의 감독 유지, MoveNext/Current 실패와 cleanup 실패, opaque WaitForSeconds의 명시 거부를 검사한다.
- 취소/재사용: 대기 중 Cancel의 즉시 정리와 늦은 MoveNext 무효, 반환 iterator Dispose의 취소, 새 Play가 이전 것을 취소하고 기존 runner가 새 상태를 덮지 않는지, 내부 MoveNext 중 취소의 안전한 후속 정리, 무한 동기 child chain의 프레임 양보, null입력의 기존 재생 보존을 확인한다.
- 추가 경계: 동기 MoveNext에서 시계가10초에 도달했을 때 Completed로 성공 오판하지 않으며, Ended observer가 시작한 재생을 바깥 교체 요청이 조용히 덮어쓰지 않는지 확인한다. 이벤트 횟수·시각·Error 객체 및 실제 finally 부수효과는 제품 helper로 기대값을 계산하지 않고 리터럴과 대조한다.
- 검증 상태: 메인이 playback-initial.json에서16개 중14PASS 및 추가 경계2건의 기대 FAIL을 실행 확인한 뒤 직접 원인을 수정했다. 수정 후 GREEN은 메인 통합 검사를 기다린다. 보조의 Unity 실행은 NOT_RUN이다. 최종 실제 결과는 PRESENTATION_LIFECYCLE_REPORT에 기록한다. Fake clock 검사가 모바일 프레임·실제10초·컨트롤러 입력/복구 검사를 대신하지 않는다.
