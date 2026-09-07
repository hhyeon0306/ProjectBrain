# Brain 작업 이력

## 2026-09-07 U1-3 문서 저장 자동 반영
- Goal: Script Document에서 저장한 설명과 자료를 Explorer가 즉시 읽도록 연결한다.
- Changes: 공통 Sync 투영/소유 관계 갱신/버전 비교, pending→history 재개 이력과 이전 bytes 보존, 저장 이벤트로 Explorer 갱신. 초기 Movement 설명을 명시적으로 조정해7문서 일치.
- Files: BrainDocumentSync/Checks(.cs/meta), ScriptDocumentService/DocumentStore/BrainStore/DocumentWindow/ExplorerWindow, .projectbrain 변경·sync history, Docs 계약/사용법/인계/작업표/검증 및 AGENTS.
- Verification: Sync26/DocumentStore14/Store25/Edit22/Completion32(총119 자체 검사). 실제7문서 일치/LoadOrCreate 성공. 실제 Script Document SaveChanges → 열린 Explorer graph 교체 및 읽기 전용 본문 일치, 샘플 설명 원문 유지. 최초 실창 확인은 선택 문서가 없어 쓰기하지 않고 정상 선택 후 재검증. 최종 Unity 실행은 reviews/2026-09-07-u1-3-evidence.json 참조.
- Decisions: 저장 시 단방향 반영. 기존 노드 메타/수동·들어오는 관계 보존, 기존 사람 확인 대행 없음. 초기 Movement 기존 docs/node bytes를 이력에 보존하고 최신 Explorer 설명을 사용했다.
- Next: 사용자가 툴별로 지적하는 항목 보정, 서류/PPT 준비도와 P1 증거.
- Limitations: 다중 프로세스 동시 쓰기/외부 자동 감시/역방향 자동 병합/이력 보존 용량 정책 미구현. 기존 사용자5경로 변경 보존.


## 2026-09-07 U1-2 문서 구조와 툴 텍스트 보완
- Goal: 사용자가 지적한 문서 폼의 단순한 구조, 본문 잘림, 그래프 이름 겹침을 수정한다.
- Changes: 문서4탭/고정 저장바/저장상태/작성 수/접힌 메타, 초안 보존, 전체 폭 문서 관계도. 공통 입력 줄바꿈/작업 정보 접기, 검증·확인 상태 한국어/원본 로그 접기. 전체 그래프 이름 겹침 시 이름만 생략하고 선택 우선 표시.
- Files: Editor BrainDocumentWindow/DocumentGraphView/BrainTheme.cs·uss/BrainTaskWindow/BrainExplorerWindow/BrainMapView; Docs 계약/사용법/작업표/인계/증거와 Brain 실제 검증기록.
- Verification: DocumentStoreChecks14, BrainMapChecks11 통과. 실제 UI콜백으로 4탭 초안·CreateGUI·미저장 상태 유지 및 전체 docs bytes 보존. 720x560 긴 한글/개행 렌더, 1000x920 문서7노드 겹침·범위 검사, 780x850 작업창 렌더. 1000x800 전체40노드 중26이름 표시 상태에서 이름 교차0·생략 노드 선택 표시·노드 수 보존. 실제 마우스 문서 탭 전환 상태0 확인. 최종 Unity 결과는 reviews/2026-09-07-u1-2-evidence.json 참조.
- Decisions: 저장소·MCP·사람 승인 규칙은 유지. 이름 생략 수와 확대/선택 안내를 표시해 전체 데이터가 줄어든 것으로 오해하지 않게 한다.
- Next: 실제 사용 피드백, P1 증거·서류/PPT 준비.
- Limitations: 큰 그래프의 모든 이름을 축소 화면에 동시에 표시하지 않는다. 전체 작업 종료/사람 확인은 대행하지 않았다. 기존4개 외 DemoPlayerMovement.cs 괄호 변경이 추가로 관찰돼 보존·커밋 제외했다.


## 2026-09-07 U1 A2 그래프 중심 디자인 적용
- Goal: 사용자가 승인한 A2 시안대로 넓은 관계 그래프와 작은 상세/하단 도구를 실제 Unity UI로 구현한다.
- Changes: BrainMapView/Painter2D 벡터 글리프·선·배치/간격 보정, 검색·종류 필터·주변2단계·줌/드래그·맞춤. Explorer 재구성/문서 바로가기/작업 기억 서랍, Theme.cs/uss 및 기존 작업·문서 창 공통 스타일.
- Files: Editor BrainMapView/Checks/Theme(.uss)/Explorer/TaskWindow/DocumentWindow/DocumentGraphView와 meta; 승인 목업/실제 화면/검증 JSON; Docs 계약·사용법·작업표·인계, .projectbrain 검증·요약 기록.
- Verification: Map11, 실제 UI콜백11, Workflow39, Lifecycle28. 실제 마우스 선택/주변/휠/배경 드래그 및1320x850/900x640 렌더. 최종compile 8db0deaf-8c1d-4774-b97c-155506cc69bd 오류0/compiled0/cache74. 실제EditMode 7e785569-bb05-4ee7-abe2-bbd377857f57 9/9(WorkflowDemo). 첫 컴파일 증거 a8c817d9-ae8a-4842-b4d1-f000dae3fa85는 뒤의 meta 공백 정리 전 스냅샷으로 보존한다.
- Decisions: 사용자 적용 승인으로 U1을 우선. 실제33노드에서 시작하고 검증기록을 누적 표시(화면 증거는36노드 시점); 예시 데이터 미삽입. 저장/MCP/확인·완료 규칙 유지. 기존 docs/nodes 분리 안내.
- Next: 실제 사용 피드백에 따른 디자인 보정, 서류/PPT 준비도와 P1 증거.
- Limitations: 초기 Color32 변환 오류와 레이아웃 전 NaN 발생은 수정 후 재컴파일/회귀 통과. 검색 키보드 자동 주입은 값 변경을 확인 못해 UI콜백 검증으로 구분한다. 대규모 성능 미측정, 조밀한 그래프는 확대·필터 필요. 요약 File.Replace 실패1회 후 기존rev10 보존 확인/재시도11 성공. 기존 사용자4개 변경 보존. 전체 작업 완료·사람 확인 대행 없음.


## 2026-09-07 W1b-UI 마우스 재검증
- Goal: 이전에 남은 물리 마우스 상호작용 확인.
- Changes: 코드 변경 없이 마우스 검증 결과 갱신.
- Files: 작업 인계/검증 기록과 하위 사용법/작업표/증거.
- Verification: 실제 클릭 새로 읽기·완료 조건·기록 조회, 창 드래그·휠 스크롤·이력 펼치기 성공.
- Decisions: 이번 활성화 오류 없음. 이전 실패 원인은 미확정.
- Next: 서류/PPT 준비도 및 실제 사람 확인/완료 시연 준비.
- Limitations: 데이터 쓰기/실제 종료/사람 확인은 실행하지 않음. 코드 변경 없어 Unity 검증 재실행 불필요.

## 2026-09-07 W1b-UI 사람용 작업 관리
- Goal: 에이전트 API의 범위/종료/이력을 사람이 쉽게 조작하고 확인한다.
- Changes: 전용 UI Toolkit 창·Explorer 진입 버튼, 현재 범위/변경 이유/종료 방식·최근20개 이력/ID조회. SessionState 초안·오래된 작업 거절·종료 전 미저장 범위 보호.
- Files: BrainTaskWindow.cs/meta, BrainExplorerWindow.cs, Docs 계약/사용법/작업표/인계/검증 기록, .projectbrain 요약 및 실제 검증 기록.
- Verification: Unity 내부 실제 버튼 콜백9항목, Lifecycle28 회귀 통과. 실제 active bytes 보존. 780×800/580×540 렌더·줄바꿈/스크롤 확인. 최종 compile/EditMode 결과는 후속 기록.
- Decisions: MCP와 동일 서비스 사용, 별도 승인 정책 없음. 사람 확인/실제 성공 종료를 대행하지 않는다. 이번 사용자 요청 단위이며 일정 원칙 유지.
- Next: 서류·PPT 준비도 확인, 실제 사람 확인/완료 시연 준비 및 P1 증거 정리.
- Limitations: computer-use 창 활성화 실패로 물리 마우스 검증 미완료. UI 종료 성공은 실제 작업에서 수행하지 않았으며 서비스 성공은 격리 fixture에서 확인. SessionState는 Editor 종료 이후 초안 보존을 보장하지 않음. 전체 U1 스타일/종료 작업 재개는 후속. 기존 사용자 변경4개 보존·업로드 없음.


## 2026-09-07 W1b 작업 범위·종료 관리
- Goal: 원래 기준선과 미해결 기록을 보존하며 작업 범위를 바꾸고 다음 작업으로 전환한다.
- Changes: scopeChanges 원자 저장·레거시 호환, 완료/미완료 archive 종료 표시, 종료 뒤 새 begin, 기록 조회. MCP set_scope/close_task/task_history 추가로 총13도구.
- Files: BrainTaskLifecycle/Checks.cs 및 meta, TaskService, UnityBrainJson, BrainTools; .projectbrain/tasks/evidence/nodes/relations, AGENTS.md와 Docs 계약/사용법/인계/증거.
- Verification: 실제 Unity MCP 자체 Lifecycle28+Edit22+Completion32 통과(총82). 격리 fixture에서 범위 변경/기준선 보존·미완료 종료/교체·정책 충족 완료·중복 요청·손상 보호 확인. 실제 HTTP13도구 등록·history/무변경 scope·revision 충돌/완료 조건 거절 및 active bytes 보존 확인. compile 최신 상태74/errors0, 실제 EditMode9/9(WorkflowDemo만) 통과. 검증 ID는 reviews/2026-09-07-lifecycle-evidence.json 참조.
- Decisions: 실제 기존 작업은 범위를 자동 확장하거나 미완료 종료하지 않는다. fixture의 사람 확인/검증 데이터는 실제 프로젝트 승인으로 사용하지 않는다. 사용자 진행 요청에 따른 이번 단위이며 9/7 마감 원칙은 유지.
- Next: 서류·PPT 9/7 15:00 준비도 확인. 실제 사람 문서 확인과 완료 시연 준비, 전용 범위/종료 UI 및 U1 전체 스타일은 후속.
- Limitations: 최소 API 구현이며 범위/종료 전용 UI·목록/재개는 후속. 다중 파일 트랜잭션/외부 변조 방지 없음. 기존 사용자 변경4개 보존·원격 업로드 없음. 실제 문서 확인/완료 성공은 아직 하지 않았다.


## 2026-09-07 M2b 에이전트 편집 최소 흐름
- Goal: 허용된 코드와 연결 문서를 에이전트가 충돌을 확인하며 수정한다.
- Changes: read_edit/apply/update_document 3개 도구, 해시·revision·문서 근거 확인, 편집 영수증. 실제 Movement 주석과 설명 문서 갱신, 작업 요약 revision8.
- Files: BrainEditService/Checks.cs 및 meta, BrainTools.cs, DemoPlayerMovement.cs, .projectbrain 노드/edits/freshness/evidence/relations/tasks, AGENTS.md와 Docs 계약/사용법/인계/기록.
- Verification: 격리 Edit22+Completion32 자체 검사 통과. 실제 HTTP 코드 적용·오래된 코드 해시 및 문서 맥락 거절·문서 저장 확인. 실제 EditMode9/9(WorkflowDemo). 최종 컴파일 재실행 compiled0/up-to-date74/errors0 통과(87d44a45-5334-44fe-b0f5-3c50178b9ee1).
- Decisions: 기존 .cs 128KiB와 연결 Document만 지원. 문서 최신성은 현재 코드에 맞춰 작성한 뒤 명시적으로 기록하며 사람 확인과 분리. 사용자 요청에 따른 이번 단위이며 9/7 새 기능 금지 원칙의 전면 해제 아님.
- Next: 서류·PPT 9/7 15:00 준비도 확인. 다음 개발은 활성 작업 범위·종료/교체 계약 및 실제 사람 확인/완료 시연. U1 전체 디자인은 핵심 흐름 이후.
- Limitations: 문서 File.Replace 1회 실패 시 원본 보존 확인 후 재시도 성공. prepared 기록 fdc097c0-6119-429e-8c58-7d4f5279f424는 보존. 자동 복구·다중 파일 트랜잭션 없음. 최초 컴파일 종료 기록은 interrupted로 복구되어 성공으로 계산하지 않음. 사람 문서2개 미확인/범위 밖 변경으로 실제 완료는 거절. 기존 사용자 변경4개 보존, 원격 업로드 없음.


## 2026-09-07 WF-B 현재 API 연동
- Goal: 실제 Brain 도구를 MCP 중심 작업 절차에 연결하고 사용 순서를 검증한다.
- Changes: 상위/하위 AGENTS와 사용법에 7개 도구의 실제 순서를 반영. 작업표와 인계 갱신, 요약 revision6→7. 제품 C# 변경은 남기지 않음.
- Files: AGENTS.md, Docs/task.md, brain-usage.md, session_handoff.md, work_log.md, reviews/2026-09-07-wfb-evidence.json; .projectbrain/tasks/active.json 및 새 실제 Evidence/노드/관계. 상위 상세 사용법은 ../Docs/unity-workflow.md.
- Verification: 실제 MCP 연결/7개 스키마 확인, begin 재개·context7,326자/9노드. DemoPlayerMovement 임시 주석 뒤 verification-compile/editmode 거절, 원본 bytes/작업 기준선 보존 복원 후 기존 결과 재유효. assets-refresh 후 실제 EditMode9/9. complete는 문서 미확인/허용 밖 사유로 거절. 문서 구조 검사 통과. 전체 verify는 기존 사용자 SampleScene.unity:242 공백으로 중단됐으며 해당 씬은 보존하고 이번 staged 변경 검사는 통과했다.
- Decisions: 현재 API의 WF-B 연동 완료와 전체 Brain 사용 중심 전환을 구분한다. apply/update_document·실제 사람 확인/완료 성공은 남아 있다. 자동 승인·기준선 변경·CLI 중복 검증 없음.
- Next: 서류·PPT 준비도 확인. 실제 문서/범위 정리, M2b와 활성 작업 범위/교체 계약. 이후 U1 디자인.
- Limitations: 9개는 WorkflowDemo 검사이며 Brain 전체 검증 아님. 상태 응답 요약/상세 분리 미구현·토큰 절감 미측정. 실제 완료 Activity는 생성하지 않았다. 기존 사용자 변경4개 보존, 원격 업로드 없음.


## 2026-09-07 A3/V1 최소 검증 연결
- Goal: 실제 Unity 결과를 현재 스냅샷 및 완료 조건에 연결한다.
- Changes: brain_verify, 공유 Ivan TestRunner/CompilationPipeline 어댑터, 재로딩 복원/중단/timeout, Evidence 상세/노드, 성공 Activity 분기, UI 결과 표시. 작업 요약 revision6. 디자인 아트 직접 제작 승인 기록.
- Files: BrainVerification.cs/meta, CompletionService/Checks, Tools, ExplorerWindow, asmdef; .projectbrain/evidence/nodes/relations/tasks; AGENTS.md와 Docs 계약/사용법/인계/검증 증거.
- Verification: MCP assets-refresh 컴파일 확인. 격리 자체 Completion32+Workflow39 통과. 실제 마지막 compile은 compiled0/up-to-date74/errors0, 실제 EditMode9/9. 둘 다 동일 스냅샷. HTTP status/complete는 문서 미확인·범위 밖으로 거절. UI Evidence current=True/9/9 표시. 성공 Activity는 synthetic fixture에서만 검사. 문서 구조 검사 통과. 전체 verify는 기존 사용자 SampleScene.unity:242 공백에서 중단되어 씬은 보존하고 이번 staged 변경 검사는 통과했다.
- Decisions: 고정 compile+전체 EditMode 정책. 생성 Evidence/Activity 및 관련 링크는 의미 스냅샷/문서 확인에서 제외. 활성 작업 기준선/사람 미확인 유지. 자동 아카이브 없음.
- Next: 서류·PPT 준비도, 실제 문서 확인·범위 정리/WF-B. 이후 디자인 아트 제작/U1.
- Limitations: 초기 미관측 종료는 오래됨, up-to-date 미수집은 실패로 남김. 상태/콜백 복원 및 up-to-date 처리 보완. 실제 Finish File.Replace IOException(00:23:17) 1회는 비통과 상태 보존, 재실행9/9 정상 저장. 원인은 미확정. 9개는 기존 WorkflowDemo 대시 검사이며 전체 Brain/Movement 검증 아님. PlayMode/Player 빌드·실제 사람 확인/최종 완료·전체 WF-B 미검증. 기존 사용자 변경4개 보존.


## 2026-09-07 W2/M1 문서 확인·완료 거절
- Goal: 문서 확인과 현재 코드 기준을 연결하고 미검증 완료를 거절한다.
- Changes: 고정 필수 문서 정책, 사람 UI 확인 기록/해시 무효화/손상 보호, status completion·brain_complete 6번째 MCP, 거절 이유 UI. 요약 revision 4→5, 기준선 보존.
- Files: BrainCompletionService/Checks.cs 및 meta, BrainTools/TaskView/ExplorerWindow.cs, .projectbrain/tasks/active.json, AGENTS.md와 Docs 계약/인계/사용법/증거.
- Verification: Ivan assets-refresh 후 컴파일 완료. 격리 Completion22+Workflow39 자체 검사 통과. 실제 HTTP begin/status/complete 미확인·범위 밖·미검증 거절, revision 충돌 거절과 원본 보존. Computer Use로 문서 unreviewed·확인 버튼 비활성 및 완료 버튼 거절 표시 확인. 전체 verify -IncludeBrain은 기존 사용자 SampleScene.unity:242의 줄 끝 공백으로 중단됐다. 문서 구조 검사는 통과했으며 이번 변경의 staged diff --check는 통과했다.
- Decisions: V1 전 completed는 항상 false. AI basis와 사람 확인 분리. 전체 관계 변경도 보수적으로 stale. 사용자 최신 진행 요청으로 이번 한 단위 수행; 서류·PPT 15:00 마감 유지.
- Next: 서류·PPT 준비도 확인. 개발상 A3/V1 실제 결과 연결, WF-B.
- Limitations: 첫 검사에서 Windows 중첩 경로 길이 실패 → flat SHA256 경로로 수정, SafeId 유효성 오류 → Hash로 수정 후 통과. 실제 사람 확인 진술은 자동으로 기록하지 않음. 인증/변조 방지·동시 다중 파일 스냅샷·V1·완료 성공·NUnit/Player 빌드는 이번 범위 아님. 기존 사용자 변경 네 파일 보존.


## 2026-09-06 A2/W1/M2a 최소 흐름 구현
- Goal: Player 계층 탐색·작업 기억 재개·제한된 맥락·최신성의 첫 수용 흐름을 구현한다.
- Changes: 공통 계층 검증, Explorer, 4개 계층 노드/6개 관계, 작업 저장/revision/전체 감시와 허용 범위 분리, freshness 기록, BFS/응답 예산, 기존 Ivan Brain 도구 5개 추가. 이동 코드의 입력 공급 의존성 주석 명시. 사용자 서류/PPT 9/7 15:00 마감 반영.
- Files: Packages/com.projectbrain.editor/Editor의 BrainWorkspace/GraphService/TaskService/TaskView/FreshnessService/ContextService/Wire/Tools/ExplorerWindow/PlayerSetup/WorkflowChecks 및 meta, UnityBrainJson; .projectbrain 계층/작업/근거 데이터; Assets/Scripts/Gameplay/DemoPlayerMovement.cs; AGENTS.md 및 Docs 계약/인계/사용법/증거.
- Verification: MCP 컴파일 완료. 기존 자체 25+18+14+61 및 새 39항목 통과. 실제 HTTP 등록/호출, begin→summary→basis/current→코드 주석 변경/도메인 reload→동일 작업/기준선 resume→modified/stale·revision 충돌 거절. UI 역방향·본문·이미지·두 창 크기와 부착 창 초안 보존 확인. 최종 context 실제 6,988자/8노드/9관계로 보고값 일치. UI 저장 재시도 revision4 확인. 커밋 전 상위 scripts/verify.ps1 -IncludeBrain 문서/Git 검사 통과.
- Decisions: typed DTO 응답으로 이중 JSON 인코딩 방지. 같은 BFS 깊이 전체에서 관계 우선순위 정렬. 원래 baseline/freshness는 유지하며 최신성 차이를 승인으로 덮지 않는다. W2/V1 미구현이므로 작업은 active 유지하고 전체 WF-B 완료로 표시하지 않는다.
- Next: 제출 준비도 점검 후 W2/M1 문서 확인·완료 거절, 이후 A3/V1/WF-B/P1.
- Limitations: JsonElement 반환형은 Reflector schema 등록 오류가 발생해 DTO로 교체하고 UI Refresh 1회로 복구. 부착되지 않은 UI fixture는 이벤트가 발생하지 않아 부착 창에서 재검증. UI 저장 첫 시도 File.Replace 오류는 revision3 보존, 명시적 재시도 revision4 성공이며 원인은 미확정. 기존 두 사용자 파일 보존. NUnit/Player 빌드/실제 플레이/전체 검증 결과 연동은 이번 범위 아님. Git dirty 자동 조회·GUID 이동 추정·토큰 절감 측정 없음.
## 2026-09-06 A1-R/F2 보완 완료
- Goal: JSON 손상 보호·Git clone 이관 실패·문서 UI 오류 경계의 확정 결함 3건을 보완한다.
- Changes: JSON 구조 선검증과 오류 경로 전달, Git 데이터 bytes 보존 정책, 관계 실패 시 편집/미저장 상태 유지·재조회, 격리 회귀 검사 추가. 인계의 과거 장애 설명은 이력에 남기고 현재 상태로 정리.
- Files: .gitattributes, AGENTS.md, Packages/com.projectbrain.editor/Editor/{UnityBrainJson,DocumentStore,BrainDocumentWindow,BrainRepairChecks}.cs 및 신규 meta, Docs/{task,architecture,product_spec,session_handoff,work_log}.md, reviews/2026-09-06-repair-evidence.json.
- Verification: MCP 읽기 연결/실제 프로젝트 경로 확인, assets-refresh 이후 isCompiling=false. 기존 자체 검사 25/18/14 및 신규 61항목 통과. core.autocrlf=true 새 clone의 스냅샷 22ad756989b6e32f8a94ee6455d714d612f9f034에서 이관 2회, 전체 데이터 bytes 보존, Git 변경 0. 실데이터 15노드/26관계 조회. 커밋 전 상위 scripts/verify.ps1 -IncludeBrain 문서/Git 검사 통과.
- Decisions: 기존 System.Text.Json DLL을 입력 검사에 사용(설치 없음), v1 호환 유지. 원본 SHA256/receipt는 변경하지 않으며 Git만 bytes 보존. F2 오류 경계 완료와 전체 시각 QA를 구분한다.
- Next: 제출 준비도 점검 후 A2 최소 탐색·공통 서비스와 W1/M2a 작업 재개 흐름.
- Limitations: NUnit/Test Runner 실행 수가 아닌 자체 검사 수. clone의 Unity import/빌드 및 실제 UI 시각/마우스 QA는 미실행. 초기 Newtonsoft 가용성 프로브는 동적 컴파일 실패했으며 사용하지 않았다. 기존 사용자 변경 두 파일은 보존, 제품 데이터·씬·MCP 설정 변경 없음.
## 2026-09-06 HTTP 복구 후 세션 종료
- Goal: 사용자 요청으로 이번 세션을 종료하고 다음 세션의 재개 지점을 보존한다.
- Changes: HTTP 내장 MCP 복구 완료를 유지하고 다음 작업을 Brain A1-R/F2 보완으로 명시.
- Files: 양쪽 Docs/session_handoff.md, Docs/work_log.md.
- Verification: 이번 세션 내장 MCP 도구 38개 노출 및 씬/도구 목록 조회 성공. 종료 시 제품 검사는 반복하지 않음. verify -IncludeBrain 문서/Git 검사 통과.
- Decisions: 이번에는 새 제품 구현을 시작하지 않는다. Unity 관리 HTTP 서버와 양쪽 설정 유지.
- Next: 다음 세션 시작 절차 후 날짜/제출 준비도 확인, MCP 읽기 연결 1회 확인, 확정된 W1/M2a 계약을 사용하여 A1-R/F2 보완부터 진행. 전면 재설계·연결 진단 반복 불필요.
- Limitations: Brain JSON 필수 필드/clone 이관/UI 오류 경계 결함은 아직 미해결. 최종 UI/Brain MCP 기능 미구현. 기존 사용자 변경 두 파일 보존.
## 2026-09-06 HTTP 내장 MCP 연결 복구 확인
- Goal: Codex 재실행 후 현재 대화의 내장 Unity MCP를 검증한다.
- Changes: HTTP 전환 후 내장 연결 검증 대기를 완료로 갱신.
- Files: 양쪽 AGENTS.md, Docs/session_handoff.md, Docs/work_log.md; 상위 Docs/unity-workflow.md; 하위 Docs/unity_mcp_usage.md.
- Verification: 현재 ALL_TOOLS에 Unity 도구 38개 노출. 내장 scene-list-opened 성공(유효/로드됨, RootCount=2, IsDirty=false). unity-tool-list로 assets-refresh/scene-list-opened/script-execute/tests-run 등록 확인. verify -IncludeBrain 문서/Git 검사 통과.
- Decisions: HTTP 직접 호출뿐 아니라 현재 Codex 내장 MCP 연결도 검증 완료. Unity 관리 단일 HTTP 서버를 유지한다.
- Next: Brain A1-R/F2 보완부터 재개. 제품 코드 변경 뒤 관련 컴파일/테스트는 별도 수행.
- Limitations: 이번 검사는 연결·읽기 도구 검증이며 제품 테스트 통과가 아니다. 씬/제품 코드/기존 사용자 변경은 수정하지 않았다.
## 2026-09-06 Computer Use로 HTTP 전환·실제 호출
- Goal: 사용자 요청대로 Unity UI에서 HTTP 설정을 변경하고 연결/도구를 테스트한다.
- Changes: computer-use sky로 http/Start/Reconfigure 클릭. 기존 stdio MCP만 종료하고 Unity 유지. 하위 생성 설정을 상위에 동기화. 운영 지침/사용법 갱신.
- Files: 양쪽 AGENTS.md, Docs/session_handoff.md, Docs/work_log.md; 상위 Docs/unity-workflow.md; 하위 Docs/unity_mcp_usage.md; 양쪽 .codex/config.toml(로컬 Git 제외).
- Verification: HTTP initialize 및 38개 tools/list, scene-list-opened/console-get-logs isError=false. 씬 RootCount=2, IsDirty=false. HTTP 서버 PID 35192는 Unity PID 38848의 자식. 양쪽 설정 동일 URL/timeout. 상위 verify -IncludeBrain 문서/Git 검사 통과.
- Decisions: 단일 Unity 관리 HTTP 서버 공유. 설치/업데이트/제품 코드 수정 없음. 직접 HTTP MCP 검증과 Codex 내장 연결을 구분한다.
- Next: Unity/HTTP 서버를 유지하고 Codex 새 설정 로딩 후 내장 도구 노출/씬 조회 재확인. 이후 Brain A1-R/F2 재개.
- Limitations: 현재 내장 MCP는 종료된 stdio 연결로 Transport closed/도구 미노출. 컴파일/제품 테스트는 이번 연결 시험 범위가 아니다. 전환 시 연결 오류 로그는 보존. 기존 사용자 변경 두 파일 유지.
## 2026-09-06 Codex 단독 재실행 후 재시험
- Goal: 사용자 재실행 후 MCP 연결/도구를 재시험한다.
- Changes: 재실행만으로 해결되지 않은 MCP 포트 충돌 상태 기록.
- Files: 양쪽 Docs/session_handoff.md, Docs/work_log.md.
- Verification: 현재 resources/list는 Transport closed로 실패, Unity 도구 미노출. 15:33:30 로그에 25766 address already in use. 살아 있는 서버 PID 36240(부모 codex PID 39436)은 15:33:36 Unity handshake 및 15:33:37 tools/list 완료. 전역 config에는 해당 서버 항목 없음. 상위/하위 설정은 동일하고 보존했다.
- Decisions: 다른 연결의 tools/list 완료를 현재 세션 성공으로 보지 않는다. 단순 재시작 권고를 반복하지 않는다.
- Next: 같은 Codex에서 발생하는 MCP 중복 시작/세션 소유 관계를 확인하고 현재 세션 도구 노출 복구 후 씬 조회.
- Limitations: 중복 시작의 정확한 원인은 미확정. 씬 조회·컴파일·제품 테스트 미실행. 서버 종료·설정 변경 없음.
## 2026-09-06 재실행 후 MCP 재시험
- Goal: Codex/Unity 재실행 후 연결 및 도구 사용 가능 여부를 확인한다.
- Changes: 연결 진단 상태를 초기화 실패에서 서버 통신 복구·Unity 도구 미노출로 갱신.
- Files: Docs/session_handoff.md, Docs/work_log.md (상위/하위).
- Verification: 현재 resources/list와 resources/templates/list 정상 응답(빈 목록). 서버 PID 39872. 로그 15:31:41 tools/list 시작 → 15:31:51 Unity 미연결 재시도 소진 → 15:31:54 Unity 0.90.0/Editor 6000.3.8f1 handshake 성공. 현재 ALL_TOOLS에는 Unity 도구 없음. verify -IncludeBrain 문서/Git 검사 통과.
- Decisions: 리소스 응답 성공과 Unity 도구 성공을 구분. 과거 씬 조회 결과를 재사용하지 않음. 프로세스/설정 변경 없음.
- Next: Unity 연결을 유지한 상태에서 Codex MCP를 다시 로드하고 도구 목록·scene-list-opened 확인.
- Limitations: 현재 도구 재로딩 API가 제공되지 않아 실제 씬 조회·컴파일·테스트 미실행.
## 2026-09-06 재개 계약·MCP 연결 진단
- Goal: 작업 재개 후 사용자 요청에 따라 MCP 연결·도구 테스트를 우선한다.
- Changes: Brain W1/M2a 최소 계약을 문서화한 뒤 제품 작업 중단. 현재 세션의 MCP 초기화 실패와 포트 충돌을 읽기 진단했다.
- Files: Docs/session_handoff.md, Docs/work_log.md; 하위 Docs/{architecture,product_spec,task,session_handoff,work_log}.md.
- Verification: ai-game-developer resources/list 및 resources/templates/list 모두 MCP startup failed: Transport closed. 도구 목록에 Unity 도구 없음. 실행 파일 존재, Unity 실행, 양쪽 설정 일치 확인. 25766 소유 PID 40524(gamedev-mcp-server), 부모 codex.exe 확인. 15:28:03 로그에 address already in use. 상위 scripts/verify.ps1 -IncludeBrain 통과(문서/Git 검사).
- Decisions: 기존 서버·Unity 종료, 설정 변경, CLI Editor 우회를 수행하지 않는다. TCP 리스닝이나 과거 도구 성공을 이번 연결 성공으로 보지 않는다.
- Next: Codex MCP 연결 재로딩으로 포트 소유 충돌을 해소한 뒤 도구 노출·scene-list-opened부터 재시험. 이후 A1-R/F2 구현.
- Limitations: 현재 세션의 실제 Unity 도구 호출·컴파일·제품 테스트 미실행. 포트 충돌 원인은 확인했으나 연결 복구 미완료. 기존 사용자 변경 두 파일 보존.
## 2026-09-06 MCP 중심 정책·WF-B 후속 작업 등록
- Goal: MCP 중심 운영과 Brain 구축 후 연동/AGENTS 갱신 요구를 유지한다.
- Changes: 하위 AGENTS와 task의 WF-B에 관련 기능 구축 → 현재 파이프라인 연결 → 최신성/완료 검증 → 상위/하위 AGENTS 및 사용법 갱신 조건 명시. CLI 시연 대기는 종료.
- Files: AGENTS.md, Docs/{task,session_handoff,workflow-development-demo,work_log}.md.
- Verification: 문서 변경만 수행. 상위 verify -IncludeBrain 통과. 제품 테스트 재실행 없음.
- Decisions: 개발·최종 검증은 MCP, CLI는 환경 점검만 기본 사용. 없는 Brain 도구 호출을 요구하지 않음.
- Next: 관련 Brain 기능 구축 후 WF-B 수행. 대시 시연의 추가 CLI 실행은 기본 후속 작업에서 제외.
- Limitations: 현재 연동 미구현. 기존 제품 코드 및 사용자 변경 보존.

## 2026-09-06 독립 대시 개발 워크플로우 시연
- Goal: Brain 연동 없이 현재 프로젝트에서 실제 코드 작성·결함 재현·수정·검증을 시연한다.
- Changes: WorkflowDemo의 DashCooldown/DashMover 및 NUnit EditMode 검사 9개 추가. 의도한 경계 결함 재현 후 수정, 원본 결과 저장.
- Files: Assets/WorkflowDemo 및 meta, Docs/{workflow-development-demo,workflow-demo-initial,workflow-demo-regression,workflow-demo-fixed,task,session_handoff,work_log} 문서/JSON.
- Verification: 실제 MCP NUnit 최초 9/9 → 의도적 결함 7/9 → 수정 9/9. 원래 씬 RootCount=2/IsDirty=false 유지. 상위 verify -IncludeBrain 통과. 이후 staging 검사에서 새 Unity meta의 후행 공백을 발견해 정리했다. 최종 verify -IncludeBrain 재실행 통과. CLI 실제 배치는 동의 응답 대기.
- Decisions: Brain 제품 코드는 수정하지 않고 독립 예제만 추가. 임의 Editor 종료/씬 저장을 하지 않는다.
- Next: 사용자 응답 후 허용된 범위에서 CLI 배치 검증. 워크플로우 결과와 제품 완성은 구분한다.
- Limitations: 데모는 입력/UI/충돌/애니메이션이 없는 즉시 대시 모델. 기존 사용자 변경 두 파일 보존. CLI 배치는 아직 미실행.

## 2026-09-06 세션 종료·Second Brain 기초 설계 결론 확정
- Goal: 사용자 의도와 최종 결론을 보존하고 다음 세션에서 구현을 이어가게 한다.
- Changes: 기초 설계 검토 완료·기존 구조로 구현 가능 판정 반영. 작업 기억·관련 맥락 조회·최신성 관리의 초기 수용 기준과 다음 순서 정리. 알려진 코드 결함은 A1-R/F2에 유지하고 과거 리뷰의 재판정 문구와 현재 결론을 구분했다.
- Files: 상위 Docs/{task,plan,session_handoff,work_log}.md; 하위 AGENTS.md 및 Docs/{task,product_spec,architecture,session_handoff,work_log}.md.
- Verification: 커밋 전 scripts/verify.ps1 -IncludeBrain 및 양쪽 git diff --check 통과. 이번에는 제품 코드 수정·Unity 검사·MCP 재접속 검증 없음.
- Decisions: 추가 전면 검토·재설계 없이 구현으로 진행 가능. 이번 세션은 문서 정리로 종료하며 개발은 다음 세션에 재개한다. 지원 일정은 유지한다.
- Next: 하위 인계에서 W1/M2a 최소 계약 → A1-R/F2 보완 → 최소 탐색과 begin/context 재개·최신성 흐름부터 진행. 제출 준비도 먼저 확인.
- Limitations: 제품 완성·무결함 판정이 아니다. 핵심 AI 흐름·효율 측정은 미구현/미측정. 기존 사용자/Unity 변경 두 파일 보존 및 커밋 제외.

## 2026-09-06 프로젝트 기반 코드 검토
- Goal: 프로젝트 시작점을 확실하게 검토하려는 요청에 따라 환경·구현·데이터·실패 조건을 확인한다.
- Changes: 제품 C# 12개·데모 C# 7개와 패키지/설정/데이터 검토. 확정 결함 3건과 정상 기반·검증 한계를 보고서/실행 증거에 기록. A1-R을 JSON 필수 필드 및 Git 재현성 보완으로 확장하고 F2 오류 경계 보완을 명시했다.
- Files: AGENTS.md, Docs/{README,task,architecture,product_spec,session_handoff,work_log}.md, Docs/reviews/2026-09-06-project-review.md 및 project-review-evidence.json. 제품 코드·실제 데이터·설정 수정 없음.
- Verification: 현재 Unity에서 자체 검사 25/18/14 재통과. 필수 버전 누락 수용, relations={} 정상 빈 목록 수용 및 덮어쓰기, 손상 문서의 UI 밖 예외를 OS temp 격리로 재현. 새 로컬 clone은 Git 변경 0이지만 이관 재실행 거절. 숨겨진 독립 창의 이동·복귀 콜백 통과. 실데이터 15노드/26관계·자산 누락 0·meta 93개 GUID 중복 0. 커밋 전 verify -IncludeBrain 통과, Markdown 23개·로컬 링크 66개·코드 펜스 및 실행 증거 JSON 구문 확인 통과.
- Decisions: 전면 재작성 대신 A1-R/F2 결함 수정 후 A2 착수 판단. 검토 요청 범위에서 제품 코드를 수정하지 않았다. 57항목 통과와 새 실패 재현을 합쳐 전체 통과로 표현하지 않는다.
- Next: JSON 누락 필드/손상 보호, Git 줄바꿈·해시, UI 오류 경계 보완 및 회귀 검사.
- Limitations: 실제 UI 시각/마우스·플레이·클린 Unity 최초 import/빌드·전체 보안 검토는 미실행. 현재 사용자 씬과 기존 사용자/Unity 변경 두 파일은 보존·커밋 제외.

## 2026-09-06 A2 전 문서 전면 검토
- Goal: 이전 작성 문서를 실제 구현·검증·최종 목표와 대조하고 A2 착수 전 모순과 누락을 정리한다.
- Changes: 현재/목표/미결정 계약 분리, A1-R 재현성 항목, A2와 A3/V1 의존성 정정, 필수 context와 후속 편집 흐름 분리, 저장소 보장·자체 검사 범위 명시, 사용 전환 조건 복원, archive 비권위 안내. 검토 보고서에 발견·조치·미해결을 기록했다.
- Files: AGENTS.md, Docs/{README,task,product_spec,architecture,session_handoff,unity_mcp_usage,work_log}.md, Docs/archive/*.md 상단 안내, Docs/reviews/2026-09-06-document-review.md. 제품 C#·실제 JSON·씬·설정 변경 없음.
- Verification: 기존 문서 21개 및 스크립트 6개·관련 코드 대조. 실제 MCP의 격리 저장소에서 contains 순환/잘못된 verified_by 대상 허용 및 저장 시각 유지 확인. 임시 Git 체크아웃의 LF→CRLF로 해시 변경, 이관 복제본에서 원본 변경 거절 재현. 실데이터는 읽기만 수행해 15노드/26관계 확인. 커밋 전 verify -IncludeBrain 통과. 수정 후 Markdown 22개·로컬 링크 52개와 코드 펜스 확인 통과.
- Decisions: A1은 기본 범위 완료이며 체크아웃 재현성 보완은 미해결 A1-R이다. A2/코드 수정을 이번 리뷰에서 시작하지 않는다. 최종 목업과 Unity-MCP 재사용 결정 유지. 과거 로그 본문은 보존한다.
- Next: A1-R 보완 후 A2 최소 계층 탐색을 권장. 상세 계층·문서 편집 계약과 제출 준비도 확인.
- Limitations: 전체 테스트 재실행·UI 시각·게임 플레이·모든 코드 품질 검토는 아님. 자체 57항목을 NUnit 테스트 또는 이번 실행 성과로 표현하지 않는다. 기존 사용자/Unity 변경 두 파일 보존·커밋 제외.

## 2026-09-06 A1 비파괴 이관 완료
- Goal: 기존 문서 데이터를 보존하며 범용 모델로 이관하고 A1을 완료한다.
- Changes: 명시적 이관, 원본 버전·해시 완료 기록, 충돌 사전 검사, 중단 후 재실행, 누락 자산 표시, 완료 후 사용자 편집 보호. Feature 및 Evidence/Activity ID 계약도 검증한다.
- Files: Editor/{BrainMigration,BrainMigrationChecks,BrainStore,BrainStoreChecks}.cs 및 새 meta, .projectbrain/{nodes,relations.json,migration.json}, AGENTS.md, Docs/{task,architecture,product_spec,session_handoff,work_log}.md.
- Verification: 현재 Unity 컴파일 완료 상태에서 저장소 25개·이관 18개·기존 문서 14개 검사 통과. 실제 원본 7개→노드 15개/관계 26개, 재읽기·원본 해시·반복 실행 검증. docs Git diff 없음. 관리 검사 scripts/verify.ps1 -IncludeBrain 통과.
- Decisions: 원본 보존, 기존 UI와 자동 동기화하지 않음. 이관 완료 뒤 원본이 바뀌면 거절하고 목적지 사용자 편집을 보존한다. 첫 저장소 단위는 f96e0b4에 독립 커밋했다.
- Next: A2 Player 도메인 구성과 BrainStore 기반 UI 탐색.
- Limitations: 전체 다중 파일 트랜잭션·동시 작성자 제어·실시간 자산 감시는 없음. UI 및 Brain 작업·검증 MCP는 다음 단계다. 기존 사용자/Unity 변경 두 파일은 커밋 제외.

## 2026-09-06 A1 범용 노드·관계 저장소
- Goal: 도메인 중심 그래프를 위한 노드·관계의 안전한 저장과 조회를 구현한다.
- Changes: BrainNode/BrainRelation, SHA256 ID 파일명, 원자 저장, 스키마·참조·중복 검증, 양방향 탐색, 증거·활동 덮어쓰기 거절. JSON 어댑터만 Unity에 의존한다.
- Files: Editor/{BrainNode,BrainStore,UnityBrainJson,BrainStoreChecks}.cs 및 meta, Docs/{session_handoff,work_log,architecture,task}.md.
- Verification: 실제 MCP 씬 조회 성공. Application.dataPath=ProjectBrain/Assets, isCompiling=false. Unity에서 저장소 21개와 기존 문서 14개 검사 통과. 관리 검사는 커밋 전 별도 실행.
- Decisions: A1 상태는 unreviewed/missing/recorded만 허용하며 검증 성공을 임의 생성하지 않는다. 노드 ID 변경과 증거·활동 수정은 새 ID를 사용한다.
- Next: 이 단위 커밋 후 v2 비파괴 이관 구현·검증.
- Limitations: UI는 기존 저장소를 사용한다. 작업·검증 완료 규칙 및 다중 작성자 동시성 제어는 미구현. 기존 사용자/Unity 변경 두 파일은 보존하고 커밋 제외.

## 2026-09-06 종료 및 재개 점검
- Goal: 새 세션이 범용 모델 구현의 정확한 첫 단위에서 재개되도록 한다.
- Changes: 지침의 문서 읽기 순서를 통일하고 인계에 구현 위치, 저장·재읽기 검사, 이관 착수 조건을 명시했다.
- Files: AGENTS.md, Docs/{session_handoff,work_log}.md.
- Verification: 양쪽 session-start와 상위 scripts/verify.ps1 -IncludeBrain 통과.
- Decisions: 노드·관계 저장과 검사를 먼저 독립 완료하고 기존 v2 이관은 다음 작업 단위로 진행한다.
- Next: 한국시간 9/6 14:00경 Packages/com.projectbrain.editor/Editor에서 A1 저장소 구현. 9/7 15:00 제출 완료 목표를 우선한다.
- Limitations: Unity 제품 코드는 변경하지 않았고 기존 Unity 생성 변경 두 파일은 보존한다.

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
- Verification: 기존 경로 SHA256이 실제 이전 MCP 식별자와 일치함을 확인. 이동 및 커밋 전 상위 scripts/verify.ps1 -IncludeBrain 문서/Git 검사 통과.
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

M2b 관리 검사: scripts/verify.ps1 -IncludeBrain의 양쪽 문서 구조 검사 통과. 전체 Git 검사는 기존 사용자 Assets/Scenes/SampleScene.unity:242 공백으로 실패. 해당 씬을 보존하고 이번 변경만 별도 staged 검사한다.

M2b 최종 범위 검사: 양쪽 git diff --cached --check 통과. 기존 사용자 변경4개는 staged 대상에서 제외했다.

W1b 최종 검증: 컴파일 compiled0/up-to-date74/errors0 및 실제 EditMode9/9 통과. scripts/verify.ps1 -IncludeBrain 양쪽 문서 구조 검사 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존하고 이번 변경만 staged 검사한다.

W1b 범위 검사: 양쪽 git diff --cached --check 통과. 기존 사용자 변경4개는 제외했다.

W1b-UI 최종 Unity 결과: 컴파일 compiled0/up-to-date74/errors0, 실제 EditMode9/9 통과. 현재 작업 요약 revision10. 검증 상세는 하위 reviews/2026-09-07-task-ui-evidence.json.

W1b-UI 관리 검사: verify -IncludeBrain 양쪽 문서 구조 검사 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패. 기존 변경4개를 제외하고 이번 변경만 staged 검사한다.

W1b-UI 최종 범위 검사: 양쪽 git diff --cached --check 통과. 기존 사용자 변경4개 제외.

마우스 재검증 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 씬242행 공백으로 실패하며 해당 사용자 변경은 보존한다.

U1 A2 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패. 이번 변경만 양쪽 staged 검사 통과. 자체 EOF 공백을 정리한 최종 스냅샷으로 compile/EditMode를 다시 기록했다. 기존 사용자 변경4개 보존.

U1-2 최종 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하므로 해당 변경을 보존하고 이번 변경만 staged 검사한다. 최종 컴파일 오류0/캐시74, EditMode9/9 통과.

U1-2 범위 검사: 양쪽 git diff --cached --check 통과. 기존4개 및 이동 스크립트 변경은 제외했다.

U1-3 최종 검증: 실제 Explorer 작업 초안/필터 유지 확인. compile ebd69120-3bee-43fa-a00a-e44db38b0fe7 오류0/캐시74, EditMode3769fd29-1f4c-4ec7-804e-50571dcd8371 9/9. 활성 요약 revision13.

U1-3 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패했으며 원본을 보존한다. 이번 변경만 별도 staged 검사한다.

U1-3 범위 검사: 양쪽 git diff --cached --check 통과. 사용자5개 경로는 staged에서 제외했다.


## 2026-09-07 U1-4 · 구조도 탐색 개선
- Goal: 사용자가 지적한 이름·도메인 구분·하단 상세 가독성·CS 더블클릭을 보완한다. 제안의 적절성을 판단해 필수 기능은 보존한다.
- Changes: 프로젝트 구조도 이름, 도메인/기능 사이드바와 범위 맞춤, 경계 연결 묶음, 기본 기록 숨김, 상단 요약/접는 우측 상세, CS 문서 전환 및 우클릭 IDE 열기. 작은 창에서 잘리던 도구 위치/작업 기억 폭 보완.
- Files: BrainExplorerWindow/BrainMapView/BrainDocumentWindow/BrainTheme.uss, BrainMapScope/BrainMapScopeChecks 및 meta, Docs 계약/인계/증거.
- Verification: Map11/Scope10 자체 검사 통과. 실제 창의 ClickEvent(clickCount=2) 발송으로 DemoPlayerInput 문서 연결 확인; 동일 문서 재열기의 임시 초안 보존 후 원상복원, 저장하지 않음. 기록 기본 숨김/상세가 그래프와 형제 패널임 확인. 실제 도킹 화면과 임시860×600 창 확인; 작은 창 도구 잘림을 수정하고 경계 수치 재확인. 물리 마우스 더블클릭은 이번 검사에 포함하지 않음. 초기 scope fixture는 자산 GUID 누락으로 실패해 fixture를 수정한 뒤10항목 통과. 최종 Unity 결과는 reviews/2026-09-07-u1-4-evidence.json 참조.
- Decisions: 정보 패널을 완전히 없애면 사람 확인·검증 경로를 잃으므로 필요할 때 펼치는 상세로 이동한다. 의존 관계 전체를 따라가면 도메인 필터가 무의미해지므로 경계 목록으로 제공한다.
- Next: 사용자 추가 지적을 한 단위씩 판단·보정. 서류/PPT15:00 준비도 확인.
- Limitations: 등록된 소속 기준이며 실제 Player 도메인은 기록 제외9노드다. 실제 작업의 사람 확인·허용 범위 문제를 자동 해결하지 않음. 사용자5경로 변경 보존.

U1-4 관리 검사: verify -IncludeBrain 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존하고 이번 변경만 별도 staged 검사한다.


## 2026-09-07 U1-5 · 세부 레이아웃 보정
- Goal: 닫기/문서 밀착·확대 조작 위치 및 실제 화면의 세부 간격을 점검한다.
- Changes: 상세 간격20px/확인 구역 구분선, 체크박스 앞 배치/폭 보정, 우측 조작부/필터 정렬, 노드 안내 한 줄/툴팁, 상단 그래프 가림, 맞춤 배치 여백, 문서 창 표시 이름.
- Files: BrainExplorerWindow/BrainMapView/BrainTaskWindow/BrainDocumentWindow/BrainTheme.uss 및 Docs.
- Verification: computer-use로 도킹된 세 툴 관찰, 문서 탭 클릭,1100×820/860×600 임시 창 관찰, 상세 휠 스크롤, 필터 열기, 확대20→24%, 상세 닫기 실제 마우스 확인. 작은 창 수치: 닫기-본문20px/조작부 오른쪽16px/체크 문장 폭258px 안에236px. Map11 자체 검사 통과. 툴팁을 창으로 잘못 선택한1회 입력은 bounds 오류로 거절되어 재관찰 후 창 크기로 올바른 영역 선택. 최종 Unity 증거는 reviews/2026-09-07-u1-5-evidence.json 참조.
- Decisions: A2 스타일/데이터/확인 정책을 유지하며 실제 발견한 레이아웃만 보정. 상단 제목과 그래프 이름 겹침은 맞춤 여백과 불투명 헤더로 해결.
- Next: 사용자 추가 지적 판단·보정, 서류/PPT15:00 준비도 확인.
- Limitations: 모든 해상도/데이터 조합의 전수 검증 아님. 사람 확인·저장·작업 종료 버튼 실행 안 함. 임시 검사 창 닫음. 사용자5경로 변경 보존.

U1-5 관리 검사: verify -IncludeBrain 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존하고 이번 변경만 별도 staged 검사한다.

## 2026-09-07 U1-6 · 그래프 연결선 보정
- Goal: 긴 직선의 거친 인상을 줄이고 선택 관계를 따라 읽기 쉽게 한다.
- Changes: 제한된 곡률의 Bezier 연결선, 둥근 끝/꺾임, 배경선 대비 완화, 선택선 마지막 렌더, 노드/선택 링 간격과 곡선 접선 화살표.
- Files: BrainMapView.cs, Docs 및 실제 compile Evidence.
- Verification: Unity compile2806010a-d8ff-4808-b5ae-e30daf9c0572 passed(errors0/up-to-date74). Map11 자체 검사. computer-use Player/연결 많은 DemoGameFlow 선택 화면 확인. 컴파일 후 그래프 갱신으로 첫 클릭은 선택되지 않아 새 화면에서 재선택 확인. reviews/2026-09-07-u1-6-evidence.json 및 edges.jpg 참조.
- Decisions: 과한 발광/곡률을 피하고 화면 공간 최대38px 제어점 편차로 제한. 관계 데이터/방향 유지.
- Next: 사용자 추가 지적 및 서류/PPT15:00 준비도 확인.
- Limitations: 자동 교차 회피/라벨 우회 라우팅은 구현하지 않았다. 렌더만 변경하여 EditMode 재실행은 하지 않음. 사용자5경로 보존.

U1-6 관리 검사: 문서 구조 검사 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존하고 이번 변경만 staged 검사한다.

## 2026-09-07 U1-7 · 직선/곡선 절충 및 노드 위계
- Goal: 강제 곡선의 복잡함을 줄이고 루트/도메인 기준점을 강조한다.
- Changes: 기본 직선, 역방향 관계가 있는 쌍만 최대18px 제어점 편차 곡선. 루트 지름22/글자16 Bold, 도메인18/14 Bold. 이름 표시 우선순위/폭·간격/클릭 반경/선 끝·선택 링 조정.
- Files: BrainMapView.cs/BrainTheme.uss 및 Docs/검증 기록.
- Verification: Map11 자체 검사, 실제 UI 스타일 root16 Bold/domain14 Bold 조회, computer-use 화면과 DemoGameFlow 선택 확인. compile09611484-13b0-47a5-9405-e2bea4bd47d8 passed(errors0/up-to-date74). reviews/2026-09-07-u1-7-evidence.json 및 graph.jpg.
- Decisions: 직선의 단순함 유지, 양방향 구별에만 곡선 사용. 루트는 Project 타입으로 판단하며 데이터 재분류 없음.
- Next: 사용자 추가 지적·서류/PPT15:00 준비도 확인.
- Limitations: 모든 관계의 교차/라벨 우회 라우팅은 구현하지 않았다. 표시 변경으로 EditMode 재실행은 하지 않았다. 사용자5경로 보존.

U1-7 관리 검사: 양쪽 문서 구조 통과. 기존 사용자 SampleScene.unity:242 공백으로 전체 Git 검사 실패, 해당 변경 보존 후 이번 변경만 staged 검사.

## 2026-09-07 U1-8 · 직접 연결 노드 강조
- Goal: 선만 강조되어 연결 끝을 찾기 어려운 문제를 보완한다.
- Changes: 선택/직접 연결/나머지 3단계 아이콘·이름 대비, 은은한 원형 배경/이름 배경, 관련 이름 표시 우선순위. 필터로 선택 노드가 숨으면 강조/흐림 해제.
- Files: BrainMapView.cs/BrainTheme.uss 및 Docs/compile Evidence.
- Verification: Map11 자체 검사. 별도 비표시 뷰에서 관련6노드 일치·선택 전환·선택 숨김·선택 해제4항목 검사. computer-use 실제 DemoGameFlow와 연결6노드 강조 화면 확인. compile b162ac01-b548-41f9-b8c1-29d5a0719699 passed(errors0/up-to-date74). reviews/2026-09-07-u1-8-evidence.json/focus.jpg 참조.
- Decisions: 직접 연결까지만 강조해 중심 유지. 선택 링은 선택 노드만, 관련 노드는 색/배경/조금 굵은 아이콘. 이름 배경으로 선의 글자 가로지름 감소.
- Next: 추가 지적 판단 및 서류/PPT15:00 준비도 확인.
- Limitations: 밀집 시 이름 충돌 생략은 유지하며 관련 이름만 우선. EditMode 재실행 없음. 사용자5경로 변경 보존.

U1-8 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존, 이번 변경만 staged 검사.

## 2026-09-07 R1 · 통합 설계 점검
- Goal: 사용자 요청에 따라 기존 설계/구현 전체를 검토하고 마감 전 보강 우선순위를 정한다.
- Changes: 검증 발행 복구·문서 쓰기 분기·맥락 누락·배치 안정성·완료 지원 범위·관계 의미·검사/문서 정합성8개 항목 정리. 보고서/격리 재현 코드/결과 저장. task/handoff 최신화, 활성 작업 revision19 기록. 제품 코드는 수정하지 않음.
- Files: Docs/reviews/2026-09-07-r1-* 5개, Docs/task.md/session_handoff.md/product_spec.md/work_log.md, 실제 EditMode Evidence/node/관계와 tasks/active.json.
- Verification: Ivan MCP 연결·실제7문서 일치. 자체10묶음225항목 통과. BrainRepairChecks 첫 UI assertion 실패, 현재4탭 구조용 오류 안내/초안 보존/복구/본문 재편집4항목 별도 통과. 실제 WorkflowDemo EditMode7cb3d5c0-a2bc-4697-8142-ab654d318f51 9/9. 기존compile와 snapshot동일, C# 변경 없어 반복 안 함. probe 초안의 IReadOnlyList.Length 컴파일 오류를 Count로 고쳐 실행 성공. 가상 저장소의 passed fixture는 실제 검증 결과가 아님.
- Decisions: 기존 구조 유지, R1-01/02/03 먼저 최소 보강하고 검사 정리. 최대60분 개발 예산 후 P1/서류/PPT로 전환 권고. 원본 기록 보존, 작업 baseline/허용 경로/사람 확인은 변경하지 않음.
- Next: 보고서 우선순위로 보강 판단, P1 실제 증거·서류/PPT15:00 준비도 확인.
- Limitations: 실제 검증6건 연결 누락 미복구, 전체 완료는48/48/2로 불가. 제품 코드 보강 미실시. 새 마우스/전해상도/Player빌드/fresh clone 검증 없음. 기존 사용자5경로와 씬242행 공백 보존.

R1 관리 검사: scripts/verify.ps1 -IncludeBrain 실행. 양쪽 문서 구조 검사 통과. 전체 Git 검사는 기존 사용자 Assets/Scenes/SampleScene.unity:242 공백으로 실패했다. 해당 파일은 보존하고 이번 기록만 별도 staged 검사한다.

## 2026-09-07 S1 · 개발 종료 시각과 설명 우선순위
- Goal: 사용자 확정 일정과 이해/설명 중심 수정 기준을 인계한다.
- Changes: 13시 개발 종료,13~13:30 확인,13:30~14:30 PPT,14:30~15시 응답 작성 포함 지원. 사용자 이해를 막는 UI/기능부터 설명하고 보완.
- Files: Docs/task.md/session_handoff.md/work_log.md.
- Verification: 상위 확정 일정과 대조. 제품 코드·데이터 변경/Unity 검사 없음.
- Decisions: 이전60분 권고 대체. R1의 통합 문제와 후속 확장을 구분한다.
- Next: 기존 완료 보고에서 부족했던 구분을 설명하고 구체 지적에 맞춰 수정한다.
- Limitations: 이번 단위는 일정·기준 기록이며 R1 보강 코드를 구현한 것은 아니다. 기존 사용자5경로 보존.

S1 관리 검사: 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존, 이번 문서만 staged 검사.

## 2026-09-07 R1-01/02/03 · 통합 보강
- Goal: 사용자 승인 순서대로 검증 발행 복구·문서 쓰기 일치·핵심 맥락 우선순위를 구현한다.
- Changes: 불변 결과 기반 Republish/재로드 및 제한 재시도, 누락 연결 완료 조건. 구조화 문서14번째 MCP와 원본/노드 공통Sync, generic 경로 거절, 합산128KiB 제한. semantic BFS 후 이력최대2개·기본depth2. Repair UI 검사 현재4탭으로 갱신.
- Files: BrainVerification/CompletionService/EditService/ContextService/Tools/ScriptDocumentService/RepairChecks, 새IntegrationChecks/meta, 계약 문서와 evidence.
- Verification: Integration29/Edit22/Workflow39/Completion32/Sync26/Lifecycle28/Repair61=237항목. 실제 누락6건 복구, Movement context Document2. HTTP14도구/구조화읽기/버전거절. 최종compile346cbf61 오류0, EditMode7bdba846 9/9(WorkflowDemo), 동일snapshot664b9017. 최초 테스트도우미 IOException 처리 수정 후 통과.
- Decisions: 원본 검증 결과·사람 확인은 변경하지 않는다. 새 API는 기존 첨부/관계를 유지하며 임의 역파싱하지 않는다. R1 검증 후 G1 샘플 교체.
- Next: G1 GAS 개념 Unity 프레임워크, 현재 파일 복구본과 기존 미완료 이력 보존부터.
- Limitations: 현재 작업51/51/2로 완료 불가. 대규모 성능/다중 프로세스 트랜잭션/원격 네트워크 구현 없음. 기존 사용자5경로는 이번R1커밋 제외.

R1 보강 관리 검사: 최초 상위 handoff 기록 누락을 보완한 뒤 양쪽 문서 구조 통과. 전체 Git 검사는 기존 사용자 SampleScene.unity:242 공백으로 실패하여 보존. 이번 변경만 staged 검사.

## 2026-09-07 G1 · GAS 개념 프레임워크 교체 진행/Computer Use 중단
- Goal: R1 완료 후 기존 샘플을 면접에서 설명 가능한 Unity 프레임워크와 Brain 템플릿으로 교체.
- Changes: 현재 변경 포함162파일 백업 후 기존 샘플 제거. Runtime6/Unity4/Test1, 능력·효과5종 에셋과 AbilityArena 씬. Brain11코드/11문서 연결. 좁은 Game 창 캐릭터 가림 보정.
- Files: Assets/AbilityFramework, Assets/Scenes/AbilityArena.unity, EditorBuildSettings, .projectbrain, Docs/archive/templates/reviews/session_handoff.
- Verification: 실제 G1 NUnit31/31(run02252d03) 뒤 UI/문서 보정하여 최종 재검증 남음. Play 기능 확인 및 실제 Computer Use 화염탄/초기화/회복 클릭. 원본·문서·코드해시11개 일치. 최종 재생 종료 클릭은 사용자 ESC로 결과 미확인.
- Decisions: 사용자 순서R1→G1. 기존 작업은 교체 사유 abandoned로 기록, 새G1기준선 보존. GAS 전체/네트워크 구현이라고 표현하지 않는다.
- Next: 설명 문서/최종검증/관리검사/커밋.13시 개발 종료 일정 유지.
- Limitations: Computer Use ESC중단 후 추가 입력 없음. 일괄 등록 File.Replace 실패는 pending 보존·개별 저장 재개 후11개 확인. 관련 없는 기존 사용자3경로 보존. G1 최종 완료 아직 아님.

G1 최신 결정: 사용자 지적 우선으로 전환. 역할이 겹쳐 보이던 도메인/기능 명칭을 구분하고 task revision2에 진행/한계 기록. 추가 확장/광범위 검증은 사용자 요청 전 중지.

## 2026-09-07 U1-9 · 구조도 성능 빠른 점검
- Goal: 사용자가 느낀 구조도 지연의 원인을 빠르게 좁힌다.
- Changes: 소스 경로와 실제 표시 맵의 동기 처리 시간을 측정. 제품 수정 없음.
- Files: Docs/reviews/2026-09-07-u1-9-performance.md, Docs/work_log.md, Docs/session_handoff.md.
- Verification: 표시34/저장35노드63관계, 갱신0.725ms·Fit1.557ms·글자측정0.235ms 평균. 그래프로드83.56ms·맵생성127.27ms 단일 관측. 측정 전 뷰 복원. 최초 진단 스크립트 enum 한정 누락 수정 후 실행.
- Decisions: 심한 지연 원인 미확정. 사용자 시간 제한에 따라 프로파일링/제품 수정 확대 없이 보류.
- Next: 사용자가 지적하는 다음 Brain 항목 우선.
- Limitations: 다음 프레임 UI 레이아웃/벡터 테셀레이션/GPU·실제 포인터 연속 입력은 미측정. 후보를 확정 원인으로 표현하지 않는다.

## 2026-09-07 U1-10 · 사람 검토 중심 화면과 역할 분리
- Goal: 사람이 핵심 설명·변경·검증 근거를 짧게 읽도록 세 도구의 역할을 분리하고 읽기 화면의 시각적 완성도를 보강한다.
- Changes: 구조도 타입별 상세/루트 강조; 설계 문서 라이브러리·아키텍처 도식·설계 읽기/기존 편집·의존 관계; 작업 요약/작업/오류·미해결/검증/설정 메뉴. 고정 표시 번호, 검색·상태 필터·20건 페이지, 실제 편집 기록과 변경 파일, 실행 결과 차트/현재 유효성. 원본·초안·사람 확인 정책 보존.
- Files: Editor/BrainPresentation·BrainArchitectureView·BrainRecordBrowser·BrainVerificationChart·BrainPresentationChecks 신규, 기존 세 창/맵/Theme/RepairChecks 수정. .projectbrain 작업 번호/상태·아키텍처 본문, Docs/templates 및 현재 기록.
- Verification: Unity refresh 후 compile457f1789 오류0/75개 최신 어셈블리. Presentation10/Repair61 자체 항목 통과. 최초 중복번호 거절 검사에서 예외 타입을 잘못 잡은 테스트 코드를 수정한 뒤 통과. 200개 번호 안정성은 동시 사용자/부하 검증이 아니다. 실제 읽기 화면 확인 중 사용자에게 시각 검증 인계.
- Decisions: 일정은 사용자 관리, 시간 때문에 시각 품질을 낮추지 않는다. 이후 화면 검증은 사용자 담당으로 자동 화면 조작 중지. 문서는 현재 구조, 작업관리는 변경·오류·검증의 원본. 코드에서 들어오면 같은 기록을 범위 필터로 조회한다.
- Next: 사용자 화면 피드백부터 필요한 부분만 보완. 추가 기능/프레임워크 검증/자동 의존 분석 착수 없음.
- Limitations: 자동 의존 분석·개별 테스트 로그/커버리지·런타임 오류 전수 수집·분산 협업/100~200명 동시 사용 미구현/미검증. 전체 작업 complete는 미매핑114/사람 미확인11/현재EditMode1로 거절, 우회 없음. 이전 G1 NUnit31을 현재 최종 결과로 재사용하지 않는다. 템플릿 설명 보존 변경은 재가져오기 미실행.

U1-10 커밋 전 보완: 신규 Unity YAML/meta29개에 생성된 줄 끝 공백을 MCP 파일 처리로 정리했다. 사용자 기존3경로는 제외했다. Git 줄바꿈 정규화 뒤 staged diff 검사 통과. 데이터 값·GUID는 변경하지 않았으며 snapshot이 바뀌어 compile73c1ea51을 다시 요청했다.
최종 보완 검증: compile73c1ea51 passed, 최신75/오류0. 양쪽 관리 검사와 staged 공백 검사 통과.
