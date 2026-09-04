# Brain 작업 이력

## 2026-09-04 스크립트 문서 저장 첫 구현
- Goal: 스크립트 하나와 설계 문서 하나를 GUID로 연결해 저장·조회.
- Changes: Editor 전용 임베디드 패키지, 문서 모델/원자적 JSON 저장소/Unity GUID 서비스, 최소 편집 창 구현. 기존 오픈소스 코드 수정 없음.
- Files: Packages/com.projectbrain.editor/, Packages/packages-lock.json, AGENTS.md, Docs/task.md, Docs/product_spec.md, Docs/session_handoff.md, Docs/work_log.md.
- Verification: Unity 6000.3.8f1 batchmode 컴파일 성공. DocumentStoreChecks.RunBatch에서 저장 전 미생성, 한글/개행 복원, GUID·해시, 덮어쓰기, 잘못된 GUID, 손상 JSON, 손상 원본 보존, 미래 스키마, 임시파일 잔류 검사 9개 통과(PROJECT_BRAIN_CHECKS_PASSED=9). 로그 Logs/brain-document-checks.log. UI 시각·수동 조작 검토는 미실행.
- Decisions: 저장 위치 .projectbrain/docs/<GUID>.json. 저장 해시는 승인·검증 증거가 아니다. Assets의 C#만 대상. 테스트 문서는 OS 임시 폴더에 격리.
- Next: 창에서 실제 사용 확인 후 작업 저장 및 변경 감지(T1 잔여/T2). MCP 전용 도구는 이후.
- Limitations: 관련 GUID 필드는 있으나 편집 UI 없음. 외부 변경 알림, 작업 상태, AI용 도구 및 검토 승인 미구현. Unity 종료 시 임시 allocator 경고가 있으나 검사 실패는 없음. 파일 이동·삭제의 실제 시연은 미실행.


## 2026-09-04 GamePlanner 기반 관리 인프라
- Goal: 다음 세션 재개와 작업·검증 기록을 시스템적으로 보완.
- Changes: 현재 명세 product_spec, 작업표 task, 인계 session_handoff, 누적 work_log로 역할 분리. 초기 문서 archive 이동. 관리 검사 스크립트와 AGENTS 연결.
- Files: AGENTS.md, Docs/, scripts/.
- Verification: scripts/verify.ps1 실행 결과는 아래 보완 기록 참조. Unity 컴파일/테스트는 미실행.
- Decisions: 공통 관리 스크립트를 하위에도 독립 배치. Git 변경 시 기록 누락 검사. Brain 업무 검증 도구는 별도 T2/T5/T6에서 구현.
- Next: T1 Editor 패키지 및 데이터 저장.
- Limitations: 기록 필드가 존재한다고 내용 정확성이 보장되지는 않는다. 자동 커밋 차단 훅은 설치하지 않음.

검증 보완: 2026-09-04 관리 verify 및 session-start 실행 통과. 임시 Git 저장소에서 AGENTS만 수정하고 작업 기록을 누락한 경우 docs-check가 예상대로 거절함. 제품 빌드·테스트는 미실행.

관리 검사 보완: Unity 생성 meta의 줄 끝 공백으로 staged diff 검사가 실패하여 값 변경 없이 공백 정리 후 재검사.
