# Brain 작업 이력

## 2026-09-04 Scripts 폴더와 UI Toolkit 전환
- Goal: 역할별 폴더·샘플 제공 및 문서 창 UI Toolkit 전환.
- Changes: Core/Gameplay/UI/Infrastructure/Utilities/Tests 폴더, 문서 연결용 이동 샘플 추가. IMGUI OnGUI를 UI Toolkit CreateGUI로 교체. 미저장 경고 및 재컴파일 중 편집 상태 직렬화.
- Files: Assets/Scripts/, Packages/com.projectbrain.editor/Editor/BrainDocumentWindow.cs, Docs/product_spec.md, Docs/work_log.md, Docs/session_handoff.md.
- Verification: dotnet build ProjectBrain.Editor.csproj --no-restore 성공(오류 0, 기존 플러그인 참조 버전 충돌 경고 3). 이는 Unity 런타임/화면 검증과 다름. 사용자 화면 확인 예정. 관리 검사 실행.
- Decisions: 역할 기준 최소 폴더 구조, 자동 테스트와 수동 샘플 구분. 열려 있는 사용자 씬 조작/저장 안 함.
- Next: 샘플을 Brain 문서 창에 지정해 저장 확인, 이후 변경 감지.
- Limitations: 현재 세션 MCP 도구 미노출. UI 시각 QA 미실행. 관련 없는 플러그인 meta 변경은 커밋 제외.


## 2026-09-04 Unity-MCP 사용 지침
- Goal: 다음 세션에서 설치된 오픈소스 MCP를 정확하고 적은 출력으로 사용.
- Changes: AGENTS에 연결·호출·컴파일·로그·테스트·장애 대응 규칙 추가. 상세 안내 분리.
- Files: AGENTS.md, Docs/unity_mcp_usage.md, Docs/work_log.md, Docs/session_handoff.md.
- Verification: 설치된 .agents/skills의 tool-list/assets-refresh/tests-run/console-get-logs 안내 확인. 관리 verify 실행. 현재 세션 MCP 도구 미노출이며 실제 MCP 호출이나 Unity 조작은 하지 않음.
- Decisions: 실제 도구 스키마 우선, 생성 예제 값 그대로 사용 금지. batch 전용 종료형 검사를 열린 Editor에서 실행하지 않음.
- Next: 사용자 문서 저장 UI 확인 후 변경 감지 구현.
- Limitations: 문서 작성이며 신규 MCP 연결 검증은 아님. 사용자 확인 중 Editor 상태 유지.


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
