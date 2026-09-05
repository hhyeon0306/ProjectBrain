# Brain 작업 이력

## 2026-09-06 목업 기준 문서·작업 구조 재편
- Goal: 코드 중심 초기 계획을 최종 목업의 도메인·기능·증거 구조와 일치시킨다.
- Changes: 제품 명세를 현재 목표 중심으로 재작성하고 범용 노드·관계·저장·v2 이관 계약을 architecture에 분리. 작업표를 기반/아키텍처/업무 흐름/MCP/검증/UI/포트폴리오 단계로 재구성하고 인계·AGENTS·문서 색인을 동기화.
- Files: AGENTS.md, Docs/{README,task,product_spec,architecture,session_handoff,work_log}.md.
- Verification: 상위 scripts/verify.ps1 -IncludeBrain 통과. 제품 코드는 변경하지 않았으며 새 모델은 구현·검증 결과가 아니다.
- Decisions: Project→Domain→Feature→Artifact/Evidence/Activity/Reference 계층, 의미와 방향이 있는 관계, 비파괴·반복 가능한 v2 이관. 제품 Activity와 개발 work_log를 분리.
- Next: A1 범용 노드·관계 저장소와 이관 검사 구현 후 Player 도메인 데모 구성.
- Limitations: 마감 전 필수 수직 흐름을 우선하며 목업 전체 시각 효과는 권장 범위. 기존 사용자/Unity 변경은 보존.

## 2026-09-05 최종형 UI 목업
- Goal: 도메인 아키텍처와 코드·문서·검증·작업 기록을 함께 탐색하는 완성형 화면을 먼저 정의한다.
- Changes: 게임 클라이언트 도메인 그래프, 기능별 구현/문서/테스트/로그/참고 자료, 상세 패널, AI 맥락 표시를 포함한 고해상도 목업 생성 및 제품 기준 기록.
- Files: Docs/assets/project-brain-final-mockup.png, Docs/product_spec.md, Docs/session_handoff.md, Docs/work_log.md.
- Verification: 생성 이미지를 시각 확인하고 요구 요소와 계층 구조 포함 여부를 확인.
- Decisions: 광활함은 무작위 노드 수가 아니라 도메인 계층과 의미 있는 관계로 표현. 목업 수치는 예시로만 사용.
- Next: 목업을 기준으로 Domain/Feature/Code/Document/Test/WorkLog/Reference 노드 모델을 설계한다.
- Limitations: 목업이며 현재 Unity UI 구현 상태를 나타내지 않는다. 토큰 절감 및 테스트 수치는 측정 전 사용 금지.

## 2026-09-05 관계 역방향 탐색 수정
- Goal: Sample에서 Flow로 이동한 뒤 Sample로 돌아오지 못하는 그래프 탐색 문제를 해결한다.
- Changes: 문서 저장소 전체 조회와 들어오는 관계 검색 추가. 그래프는 선택 문서가 등록한 관계와 다른 문서가 선택 문서를 가리키는 관계를 합쳐 표시한다. 저장 JSON을 강제로 양쪽 수정하지 않는다.
- Files: Editor/{DocumentStore,ScriptDocumentService,DocumentGraphView,BrainDocumentWindow,DocumentStoreChecks}.cs, Docs/{product_spec,session_handoff,work_log}.md.
- Verification: ProjectBrain.Editor.csproj 컴파일 오류 0, 기존 의존성 참조 경고 3. 들어오는 관계 검사 코드를 추가했으나 열린 Unity가 아직 새 코드를 가져오지 않아 Unity 실행 검증은 대기.
- Decisions: 관계는 저장은 단방향, 탐색은 양방향으로 정의. 사용자가 같은 관계를 두 문서에 반복 입력할 필요가 없게 한다.
- Next: Unity가 재컴파일된 뒤 Flow에서 Sample 노드 표시와 클릭 복귀 확인.
- Limitations: 손상된 문서 하나가 있으면 전체 관계 조회를 거절한다. 자동 코드 의존성 분석은 아직 없음.

## 2026-09-05 관계도 데모 데이터
- Goal: 사용자가 관계 그래프의 다중 노드와 문서 이동을 바로 확인할 수 있게 한다.
- Changes: GameFlow, Input, Movement, Health, HUD, Save 역할의 데모 스크립트 6개와 문서 7개를 만들고 실제 책임에 맞춰 관계를 연결. 중심 문서에 기존 URP 이미지를 첨부.
- Files: Assets/Scripts/{Core,Gameplay,UI,Infrastructure}/Demo*.cs(.meta), .projectbrain/docs/*.json, Docs/{task,session_handoff,work_log}.md.
- Verification: 문서 JSON 7개 파싱 및 관계 수 확인. Unity가 파일을 가져온 뒤 새 C# 컴파일 오류가 Editor.log에 없고 Editor 응답 정상. 그래프 시각과 노드 클릭은 사용자 창에서 확인 대기.
- Decisions: BrainDocumentSample을 중심 진입점으로 사용. 자동 분석으로 가장하지 않고 시연용 수동 관계임을 문서에 명시. savedCodeHash는 검토된 저장으로 오인하지 않도록 비워 둠.
- Next: 열린 문서에서 저장된 문서 다시 읽기 후 그래프 시각 확인. 다음 구현은 작업 저장과 변경 감지.
- Limitations: 데모 스크립트는 구조 시연용이며 씬에 배치하거나 플레이 동작을 검증하지 않음. 현재 그래프는 선택 노드의 1단계 관계만 표시.

## 2026-09-05 중단 복구 및 관계 그래프
- Goal: 이전 커밋 상태를 확인하고 문서 관계 그래프 구현을 이어간다.
- Changes: UI Toolkit 분할 화면, 선택 코드 중심 1단계 관계 그래프, 노드 클릭 문서 전환 및 미저장 보호. 스크립트 선택 후 남던 초기 안내도 갱신.
- Files: Editor/DocumentGraphView.cs, Editor/BrainDocumentWindow.cs, Docs/product_spec.md, Docs/task.md, Docs/session_handoff.md, Docs/work_log.md.
- Verification: Unity 배치 컴파일 및 기존 저장 검사 실행 결과는 아래 기록. 이전 문서 확장은 d2905d1에 완료돼 재구현하지 않음.
- Decisions: 고정 방사형 소규모 관계부터 구현. 현재 MCP 도구 미노출, 사용자 Unity 종료 확인 후 배치 실행.
- Next: 그래프 UI 수동 확인 후 작업 저장 및 변경 감지로 진행.
- Limitations: 확대/이동/검색, 자동 참조 분석 및 검증 상태 연결 미구현. 사용자 변경 파일은 보존.

## 2026-09-04 본문·이미지·관련 문서 확장
- Goal: 그래프에 사용할 문서/관계를 저장하고 이미지 포함 문서를 다시 연다.
- Changes: schema v2와 v1 메모리 마이그레이션, 본문·이미지 GUID, UI Toolkit 이미지 미리보기/관계 편집/관련 문서 이동. Editor 종료 없는 검사 진입점 추가.
- Files: Packages/com.projectbrain.editor/Editor/{ScriptDocument,DocumentStore,BrainDocumentWindow,DocumentStoreChecks}.cs, Docs/{product_spec,task,session_handoff,work_log}.md.
- Verification: Unity에서 저장/마이그레이션/손상 보호 13개 통과. 실제 샘플/이미지/관련 코드 임시 저장 후 UI 미리보기·본문 편집·dirty·저장 통과. 테스트 GUID 길이 오류와 미부착 UI 이벤트 테스트 실패를 수정 후 재검증.
- Decisions: 이미지 외부 복사 대신 Assets GUID 연결. 현재 문서는 스크립트당 하나. 원본 사용자 문서/씬은 테스트에서 변경하지 않음.
- Next: 작은 관계 그래프 선택 및 문서 표시 후 작업 추적 기능 구현.
- Limitations: 서식 렌더링·그래프·AI 전용 도구 미구현. 검증용 임시 문서는 OS temp에 보존. 전체 UI 시각 검토 대기.

## 2026-09-04 Unity 프로젝트 공백 경로 제거
- Goal: Unity-MCP 초기화 시 경로 공백 오류를 해결한다.
- Changes: Unity 종료 확인 후 Project Brain 폴더를 ProjectBrain으로 이동. 양쪽 MCP 실행 경로와 프로젝트 식별자 cbd0af11 동기화, 현재 경로 지침 및 검사 스크립트 수정.
- Files: AGENTS.md, scripts/verify.ps1, Docs/session_handoff.md, Docs/work_log.md, 양쪽 .codex/config.toml(로컬); 상위 .gitignore/task와 하위 unity_mcp_usage.
- Verification: 기존 경로 SHA256이 실제 이전 MCP 식별자와 일치함을 확인. 이동 및 관리 검사 결과는 아래에 기록.
- Decisions: 표시 이름 Project Brain 유지, 포트 25766 유지. Library/PackageCache 소스 수정 없음.
- Next: Unity Hub에서 새 경로를 열고 Codex 재시작 후 MCP 실제 씬 조회로 연결 확인.
- Limitations: 현재 세션 MCP 도구 미노출. 이동 후 Unity 실행과 재연결은 아직 검증하지 않음.

## 2026-09-04 MCP 프로젝트 경로 공백 오류 조사
- Goal: 반복 출력되는 경로 공백 오류의 실제 조건 확인.
- Changes: 설치된 플러그인 Startup.cs 정적 생성자 확인. 코드 변경 없음.
- Files: Docs/work_log.md, Docs/session_handoff.md.
- Verification: Startup.cs 32~33에서 Application.dataPath.Contains(" ")만 검사해 Debug.LogError 호출. 예외 throw/return 없이 초기화 계속. InitializeOnLoad로 재컴파일의 도메인 재로드 시 반복될 수 있음. 현재 Editor.log에 동일 문구 2건 확인.
- Decisions: 이 메시지만으로 연결 실패를 판단하지 않는다. 폴더명을 ProjectBrain으로 변경하는 것이 권장 해결안이나 아직 실행하지 않음. 플러그인 원본 수정으로 숨기지 않는다.
- Next: 사용자와 경로 변경 방향 확정 후 미저장 씬 보존, Editor 종료, 폴더 변경, MCP 경로/프로젝트 식별자 재생성 및 상위 설정·Git 제외 경로·현재 문서 갱신.
- Limitations: 실제 공백 관련 명령 실패는 이번 조사에서 재현하지 않음. 기존 연결 성공 이력은 있음.


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

이동 검증 보완: 두 MCP 설정의 새 경로/식별자와 서버 실행 파일 존재 확인. 상위 verify -IncludeBrain 통과. 실제 Unity/MCP 재연결은 재시작 후 확인 필요. 구 경로의 빈 .git 삭제는 자동 안전 정책에 차단되어 빈 껍데기를 보존함.

검증 결과: Unity 6000.3.8f1 배치 컴파일 성공, PROJECT_BRAIN_CHECKS_PASSED=13. 관리 검사 통과. 그래프 시각/클릭 동작은 아직 수동 검증 전이며 저장 검사와 구분한다. 종료 시 Unity 임시 메모리 경고 기록됨.
