# Brain 문서 기준

## 문서별 책임

- [product_spec.md](product_spec.md): 최종 사용자 경험, 마감 전 시연, 완료 규칙과 현재 구현 구분
- [architecture.md](architecture.md): 현재 데이터 계약·저장 보장, 계획/미결정 계약
- [task.md](task.md): 단계별 상태·의존성·완료 기준
- [session_handoff.md](session_handoff.md): 최근 확인 결과와 다음 시작점
- [work_log.md](work_log.md): 시점별 작업·검증 이력. 과거의 다음 행동은 현재 지시가 아님
- [unity_mcp_usage.md](unity_mcp_usage.md): 설치된 Unity-MCP 호출·검증 절차
- [문서 전면 검토](reviews/2026-09-06-document-review.md): 2026-09-06 발견·조치·재현 근거
- [archive](archive/README.md): 폐기·이전 판단의 보존본. 현재 지침이 아님

AGENTS는 작업 절차, product_spec은 제품 행동, architecture는 데이터 계약을 각각 담당한다. 실제 코드와 문서가 다르면 차이를 확인·기록하고 목표 문구만으로 구현됐다고 판단하지 않는다.

## 시작과 검증

scripts/session-start.ps1 → task → handoff → 관련 명세 → 최신 work_log 순서로 읽는다. 현재 지침에 따라 하위에서 실행하고 상위 작업 루트와 Unity 루트를 구분한다.

scripts/verify.ps1은 지정된 필수 문서의 존재·일부 링크·기록 구조와 Git 공백을 검사한다. 모든 문서/링크나 .projectbrain의 의미를 검사하지 않는다. 기존 사용자 변경만 남은 커밋 후에는 기록 갱신 요구로 실패할 수 있다. 문서 검사 통과는 제품 검증이 아니다.

A1의 25/18/14는 Unity에서 직접 실행한 자체 검사 항목 수다. Test Runner 케이스·UI·플레이 테스트 수가 아니다. 새 코드에는 과거 검증 결과를 재사용하지 않는다.
