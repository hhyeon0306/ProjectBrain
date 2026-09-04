# Brain 문서 기준

- [task.md](task.md): 현재 작업 상태
- [product_spec.md](product_spec.md): 기능·데이터·검증 계약의 단일 기준
- [session_handoff.md](session_handoff.md): 다음 세션용 현재 상태
- [work_log.md](work_log.md): 누적 작업 이력
- archive/: 초기 기획·조사 기록. 현재 지침이 아니며 필요할 때만 확인.

세션 시작: scripts/session-start.ps1. 문서 검증: scripts/verify.ps1. 실제 Unity 컴파일/테스트는 별도 실행하고 결과를 작업 기록에 남긴다. 문서 검사 통과를 코드 검증 성공으로 표현하지 않는다.
