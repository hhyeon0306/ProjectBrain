# MCP·CLI 개발 시연: 대시 쿨다운

2026-09-06 사용자 요청에 따른 실제 개발 형태의 예제다. Brain 기능/연동과 독립이며 Assets/WorkflowDemo에 한정한다.

## 구현

- DashCooldown: 첫 사용 허용, 2초 쿨다운, 반복 입력에 타이머 연장 없음, 정확한 경계 시점에 재사용.
- DashMover: 입력 방향을 정규화하여 월드 공간 3단위 이동. 0 방향은 쿨다운을 소모하지 않음.
- 실제 NUnit EditMode 테스트 9개. 이동 검사는 Preview Scene에서 오브젝트를 만들고 TearDown에서 정리한다.
- 데모용 순간 이동이며 입력/UI/충돌/애니메이션은 제공하지 않는다. 시간은 호출자가 단조 증가하는 유한 값을 제공한다.

## 실제 수정·검증 사이클

1. 코드 작성 → MCP assets-refresh → tests-run: 9/9 통과.
2. 의도적으로 `now < readyAt`를 `now <= readyAt`로 변경.
3. MCP 재컴파일·테스트: 7 통과/2 실패. 정확히 2초 경계와 이동 재사용 검사에서 실패.
4. 조건을 원복해 수정 → MCP 재검증: 9/9 통과.
5. 원래 열린 씬의 RootCount=2, IsDirty=false 유지.

원본 결과는 [최초](workflow-demo-initial.json), [결함 주입](workflow-demo-regression.json), [수정 후](workflow-demo-fixed.json). 의도한 결함은 최종 코드에 남기지 않았다.

## CLI 단계

실제 배치 검증을 위해 이름 없는 현재 씬의 보존·Editor 종료/재실행 동의를 요청했다. 응답 전에는 종료하지 않는다. CLI 실행 상태는 후속 기록에 보완한다.

사전 점검에서 기존 래퍼가 lock 파일의 embedded 패키지까지 외부 file 패키지로 오인해 거절함을 발견했다. 상위 래퍼에서 Packages 트리에 포함된 embedded 패키지는 허용하고 외부 local 패키지는 계속 차단하도록 보완했다. Brain 패키지 자체는 수정하지 않았다.
