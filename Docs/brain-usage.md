# U1 A2 화면 사용 (2026-09-07)

## R1-01/02/03 현재 계약 (2026-09-07)

아래 계약이 이전 단계 설명보다 우선한다. Brain MCP는14도구다.

- 검증 원본의 terminal 결과를 발행 재개의 근거로 사용한다. Republish(id)는 같은 내용의 Evidence 노드/관계를 재사용하고 누락만 채우며 충돌은 보존·거절한다. Editor 재로드 시 활성 작업의 기록을 확인하고 일시 실패는10초 간격 최대5회 재시도한다. 실패가 계속되면 경고와 원본 ID를 남긴다. 현재 통과 결과의 연결 누락도 완료 조건에 verification-publication으로 표시한다. 결과를 다시 실행하거나 덮어써서 복구하지 않는다.
- brain_read_edit의 documentFormat이 script-document이면 structuredContent와 expectedDocumentVersion을 읽는다. brain_update_script_document(taskId, expectedRevision, nodeId, expectedHash, expectedContextHash, expectedDocumentVersion, role, designIntent, cautions, body)로 수정한다. 기존 첨부/코드 연결을 보존하며 BrainDocumentSync를 통해 docs/nodes/relations에 반영하고 실제 프로젝트의 열린 화면에 저장 이벤트를 알린다.
- 구조화 원본이 있는 문서는 기존 brain_update_document로 수정할 수 없다. 일반 Document 노드의 summary/body 편집은 기존 계약을 유지한다. 각 경로는 허용 코드·revision·문서/맥락/원본 버전을 검사하며 초안 충돌을 거절한다. 저장은 사람 확인/검증 승인이 아니다.
- brain_context는 기본depth2. 의미 노드의 탐색을 먼저 끝낸 뒤 Evidence/Activity를 최신 최대2개 포함한다. 분량이 부족하면 뒤쪽부터 줄여 코드/설계 문서를 우선 보존한다. omittedByHistory로 이력 제한을 명시하며 전체 이력은 후속 ID로 조회한다. 깊이/노드 수/문자 수 상한은 유지한다.
- BrainRepairChecks를 현재4탭 UI의 인라인 오류/재시도/초안 보존 기준으로 갱신했다. 검사 수는 NUnit 케이스 수와 구분한다.


## U1-3 · Script Document 저장 → Explorer 자동 반영 (2026-09-07)

현재 문서 작성 경로는 Script Document다. 저장하면 역할/설계 의도/주의사항/본문을 기존 document:<GUID> 노드에 투영하고, 이미지/직접 연결 코드도 nodes/relations에 함께 반영한다. 열려 있는 Explorer는 저장 이벤트로 다시 읽으며 선택/검색/필터와 작업 요약 초안을 유지한다. 새로 읽기 버튼을 따로 누를 필요가 없다.

쓰기 전 문서 원본 bytes·대상 노드·의미 관계의 버전을 비교한다. 오래된 편집은 거절하며 초안을 보존한다. 기존 노드 제목/태그와 수동 관계는 유지한다. source=script-document 및 해당 문서의 v2-migration 연결만 관리하며 연결 제거 시 공유 자산 노드를 삭제하지 않는다. 저장과 사람 확인/검증은 구분한다. 내용/의미 관계가 바뀌면 기존 확인·검증은 원래 해시 정책에 따라 무효화된다.

.projectbrain/document-sync/pending.json에 쓰기 의도와 각 파일의 전후 bytes(base64)를 먼저 남긴다. 파일 단위 원자 교체 후 history/<UUID>.json으로 보관한다. 중단 후 저장소를 다시 열면 전후 bytes를 검사하고 남은 쓰기를 재개한다. 별도 변경/손상과 충돌하면 보존하고 거절한다. 단일 Editor 작성자 전제이며 다중 프로세스 동시 쓰기의 OS 트랜잭션은 아니다. 이력 정리/용량 제한은 아직 없다.

초기 7문서 중 Movement의 Explorer 쪽 설명이 더 최신이었다. 1회 명시적 구조 변환으로 최신 설명을 Script Document 필드에 옮겼고 기존 양쪽 bytes를 동기화 이력에 보존했다. 일반 동기화는 Markdown을 역파싱하지 않는다. 최초 migration.json은 역사 기록으로 유지하며 재실행하지 않는다.

Script Document → Explorer 투영을 유지하며 R1-02에서 에이전트도 brain_update_script_document로 같은 BrainDocumentSync 저장 경로를 사용한다. 기존 nodes 전용 API는 구조화 원본이 있는 문서를 거절한다. 외부 파일 자동 감시·Markdown 역파싱·양방향 자동 병합은 지원하지 않는다. 저장은 사람 확인과 구분한다.


Window → Project Brain → Explorer에서 실제 관계 그래프를 연다. 배경을 드래그하면 이동, 노드를 드래그하면 화면 내 위치 변경, 휠 또는 하단 +/−로 확대·축소한다. `맞춤`은 표시 중인 노드를 다시 배치한다. 이름 위에 마우스를 두면 전체 제목·설명·ID를 확인할 수 있다.

상단 검색은 제목·요약·ID에 적용되며 종류 필터 및 `선택 주변`(2단계)과 함께 적용된다. 검색 결과가 없으면 검색어/필터를 해제한다. 노드 클릭은 오른쪽 아래 상세를 열며, 관계 링크는 방향·종류와 툴팁의 출처를 유지한다. 긴 본문과 연결 목록은 상세 안에서 스크롤한다.

`작업 기억`에서 기존 요약 편집·검증을 실행하고 `작업 관리`에서 범위·종료·이력을 관리한다. Script Document의 저장은 Explorer에 자동 반영된다. 문서 저장·최신성·사람 확인·실제 검증은 서로 다른 상태다.

배치·필터는 저장된 그래프를 변경하지 않는다. 선택/검색/주변 모드는 창 재생성 때 유지하지만 드래그 위치와 종류 필터는 초기화된다. UI의 검증 기록 제목은 과거 실행이며 현재 통과 여부는 기록 상세/완료 조건에서 확인한다.

## 이전 단계별 호출·검증 이력

아래의 도구 수·revision·미구현 설명은 해당 단계 당시 기록이다. 현재 UI는 위 U1 절, 최신 도구는 W1b 절을 따른다.

# Brain 최소 사용 흐름 — A2/W1/M2a/W2/M1

2026-09-07 구현·검증 기준. Unity 프로젝트는 `C:/Dev/nexontutorial/ProjectBrain`이다. 완료 거절은 구현했고 완료 성공·WF-B 연동은 아직 없다.

## Explorer

Unity 메뉴 `Window → Project Brain → Explorer`를 연다. Project Brain 데모 → Player → Movement/Health → Code → Document로 탐색한다. 관계 버튼의 화살표·종류·출처와 역방향 복귀를 유지한다. 기존 문서/이미지 목록은 Player 전용 자료가 아니며 URP 이미지는 기존 문서 연결 예제다.

Explorer는 `.projectbrain/nodes`와 `relations.json`을 읽는다. 기존 Script Document 창은 `docs`를 편집하며 두 사본은 자동 동기화되지 않는다. 새 Explorer는 노드/본문 읽기 탐색만 제공한다. 실제 코드 열기·이미지 표시, 문서 없음/자산 없음과 unknown/stale 상태를 표시한다.

작업 기억 패널은 작업 목적을 저장하거나 기존 요약을 읽는다. 요약 저장은 revision 검사 후 진행·결정/근거·미해결·다음 행동만 갱신한다. 자료 새로 읽기와 재컴파일에서 미저장 요약 초안을 보존한다. 다른 호출이 revision을 갱신한 경우 저장은 충돌로 거절되며 초안은 남는다. 파일 교체 실패도 성공으로 표시하지 않는다. 최초 UI 저장에서 파일 교체 오류 1회가 발생했고 원본 revision 3 보존 후 명시적 재시도는 revision 4로 성공했다. 일시적 오류의 원인은 확정하지 않았다.

## 확인된 MCP 호출 순서

기존 Unity 관리 HTTP 서버 `http://localhost:25766/p/cbd0af11`을 사용한다. 별도 서버를 만들지 않는다. 실제 `tools/list`로 아래 스키마를 확인한 뒤 `tools/call`을 호출했다. 현재 Codex 내장 도구 목록은 이번 동적 추가를 반영하지 않아 직접 HTTP MCP로 검증했으며, 새 세션에서는 내장 노출을 다시 확인한다. 내장 `script-execute`로 메서드를 호출한 것과 실제 Brain 도구 호출을 구분한다.

| 도구 | 입력/동작 |
|---|---|
| brain_begin | 최초: purpose, targetNodeIds[], allowedPaths[]. 재개: taskId만 지정 가능. 활성 작업이 있으면 기존 목적/기준선을 반환하고 새 입력으로 덮어쓰지 않음. 다른 명시적 taskId는 거절 |
| brain_update_task | taskId, expectedRevision, progress, decisions, unresolved, nextAction, references[]. references는 존재하는 노드 ID. 충돌/잘못된 참조/손상 기록은 쓰기 전 거절 |
| brain_status | 입력 없음. 현재 active 작업과 추가/수정/삭제·허용 경로 밖 변경·coverage 제한을 재계산 |
| brain_context | rootNodeId, depth=1(0~2), maxNodes=12(1~30), maxChars=8000(1000~20000). 구조화된 JSON 반환 |
| brain_record_basis | targetId(설명 노드 또는 관계), codeNodeIds[](1~16). 현재 payload/코드 bytes 기준선을 명시적으로 기록. 검토·테스트 승인 아님 |

검증한 실제 순서: begin → update_task(revision 1→2) → record_basis(설명/관계) → context current → 실제 코드 주석 변경 → assets-refresh/도메인 재로딩 → begin(taskId) 재개 → status modified/allowed → context stale → 오래된 revision update 거절 → 요약 갱신/Explorer 저장. 현재 작업 ID는 `a48a9ba7-be69-4985-a648-ed5d2ac5442f`, 최신 revision 5이며 완료 성공/교체 API가 없으므로 활성 상태로 유지한다.

## 저장과 검사 범위

- `tasks/active.json`: 작업 UUID/revision, 목적·대상·요약·원본 노드 참조·허용 경로·기준선·UTC 시각. MCP 응답은 기준선 240개 파일 목록 대신 파일 수·해시·원본 참조를 반환한다.
- 감시 범위는 실제 `Assets/`, `Packages/`, `ProjectSettings/` 파일과 meta다. allowedPaths의 끝 `/`는 하위 경로 허용, 그 외는 정확한 파일 경로다. `.projectbrain`, Docs, Library/PackageCache 등은 이 코드 기준선 범위 밖이다.
- begin 이전 변경도 그대로 기준선에 포함한다. Git dirty는 자동 조회하지 않고 not-queried로 명시한다. 세션의 별도 Git 상태 확인을 대체하지 않는다. 이름 변경은 삭제+추가, GUID 이동 추정은 미구현이다.
- 링크·읽기 실패·file: 패키지는 coverage 제한을 반환한다. 전체 프로젝트/외부 의존성의 검증 완료 근거가 아니다. 단일 Editor 작성자 전제이며 다중 프로세스 잠금·전원 장애 복구는 보장하지 않는다.
- `freshness/<SHA256(target ID)>.json`: payload 해시와 근거 코드 ID/경로/bytes SHA256. current는 일치, stale은 변경, missing은 근거 코드 없음, unknown은 기록 없음. begin/요약 저장/context는 이 기준선을 자동 갱신하지 않는다. 오래됐다는 이유만으로 record_basis를 재실행해 차이를 숨기지 않는다.

## context 분량 계약

양방향 BFS이며 같은 깊이 전체에서 관계 종류 순서와 relation ID ordinal로 정렬한다. 방문 집합으로 순환 확장을 막고 from/to/type/source를 보존한다. 노드 요약·node JSON 참조·현재 자산 경로·최신성 기록 참조를 반환한다. 본문·이미지 bytes·로그 원문은 자동 포함하지 않는다.

문자 예산은 Unicode escape를 포함한 실제 compact JSON과 Ivan `result` 래퍼의 UTF-16 코드 단위 수다. MCP 프로토콜 외부 봉투는 제외한다. 최종 실제 호출은 6,988자/8노드/9관계였고 `chars` 보고값과 정확히 일치했다. 응답이 달라지면 조회량도 달라지며 이 수치를 고정 보장하지 않는다.

생략은 관측한 depth/nodes/char 제한만 세고 미탐색 전체 개수를 추정하지 않는다. 제목/요약 잘림도 char 생략으로 표시한다. 다음 조회용 ID는 최대 5개이며 예산이 작으면 줄어들 수 있다. 루트 최소 정보가 맞지 않으면 budget-too-small로 거절한다. 검색어 조회·자동 코드 분석·토큰 절감 측정은 이번 범위 밖이다.

## 검증 근거와 남은 작업

기존 자체 검사 118항목 + BrainWorkflowChecks 39항목 통과. 별도 부착 창의 미저장 초안 보존 검사, 실제 UI 탐색·역방향·이미지·1000×680/760×500 크기 확인, 실제 MCP 작업 재개·revision 거절·current→stale 확인을 수행했다. 자체 체크 수를 NUnit 케이스 수로 표현하지 않는다.

[실행 근거](reviews/2026-09-06-workflow-evidence.json), [Explorer 화면](reviews/2026-09-06-explorer.png).

다음은 W2/M1 문서 확인·완료 거절 → A3/V1 실행 결과·완료 성공 → WF-B 전체 연동이다. `brain_apply`, `brain_update_document`, `brain_verify`, `brain_complete`는 아직 없으며 이 흐름을 완료된 것으로 기록하지 않는다. 최종 목업 전체 스타일·편집·검색·전체 포트폴리오 시연도 후속이다.

## W2/M1 구현 계약 (2026-09-07)

고정 정책 w2-human-review-v1-pending: 작업 대상의 contains/implemented_by 하위와 감시 변경에 매핑된 Code, 그 소유 Feature 및 형제 Code를 검사한다. Code마다 직접 documented_by 문서가 1개 이상 필요하며 연결된 문서는 모두 필수다. Feature 직접 문서도 포함한다. 연결 없는 Code/문서, 미매핑 변경, 허용 밖 변경, coverage 제한은 거절한다. 삭제 파일은 lastKnownPath도 매핑에 사용한다. 자동 의존 분석은 아니다.

확인 기록은 .projectbrain/reviews/SHA256(taskId + 개행 + documentId).json이다. schemaVersion/taskId/documentId/snapshotHash/actor=human-ui/reviewedUtc를 저장한다. snapshotHash는 문서 전체 payload, 정렬된 전체 관계, 연결 Code 노드와 파일 경로/bytes를 포함한다. 전체 관계 변경도 보수적으로 stale 처리한다. 바이트를 정확히 되돌리면 current로 복원된다. missing-code/unreviewed/current/stale을 구분하며 손상 기록은 덮어쓰지 않는다.

Explorer에서 현재 본문과 연결 코드를 읽었다는 체크 후 사람 확인 기록 버튼을 누른다. 화면 문서가 디스크와 다르거나 작업 ID/revision·확인 대상 해시가 바뀌면 거절한다. AI basis 저장은 사람 확인이 아니며 MCP에 승인/정책 낮추기 입력은 없다. 이 경로는 사용자 확인 진술 기록이며 인증·외부 파일 변조 방지 보안 경계는 아니다. 실제 프로젝트의 사람 확인은 이번에 수행하지 않았다.

brain_status.completion은 고정 정책 판정을 포함한다. brain_complete(taskId, expectedRevision)는 현재 파일을 다시 검사하여 completed=false, documents, reasons(code/target/nextAction)를 반환한다. V1 미구현이므로 verification-unavailable은 항상 포함하고 작업/기준선/상태를 변경하지 않는다. begin 응답의 completion은 null이며 status에서 조회한다. 다중 파일 원자 스냅샷/동시 편집 보장은 없다. 실제 검증 성공·작업 교체·archive는 후속이다.

실제 검증: begin 재개 → status의 문서 2개 unreviewed → complete 거절 → 오래된 revision 거절 → 원본 보존 확인 → 요약만 revision 4→5 갱신(기준선 유지). 상세: reviews/2026-09-07-completion-evidence.json.

## A3/V1 현재 흐름 (9/7 추가)

brain_verify(taskId, expectedRevision, kind=compile 또는 editmode) → 실행 ID 즉시 반환 → brain_status.completion.verification에서 terminal 상태 확인 → 다른 종류 실행 → status → brain_complete. UI의 compile/editmode 검증 실행 버튼도 같은 실행기를 사용한다. 결과 노드는 Feature/Code의 verified_by를 통해 탐색한다. 기존 진행 중 요청을 새 요청으로 덮어쓰지 않는다.

ready는 현재 완료 조건 충족 여부, completed는 complete가 Activity를 실제 기록한 경우만 true다. 전체 EditMode의 발견된 9개 테스트는 WorkflowDemo 대시 테스트로, Movement/Brain 전체 기능을 검증했다는 뜻이 아니다. 실제 문서 사람 확인과 허용 밖 변경이 남아 실작업의 최종 완료는 아직 거절된다. 성공 분기는 격리 fixture로 검증한다.

이 절과 architecture의 A3/V1 계약이 앞선 W2의 verification-unavailable/완료 성공 미지원 설명을 대체한다. PlayMode·Player 빌드·자동 작업 교체/아카이브는 미지원이다.

## WF-B 연결
현재 실제 호출 순서는 ../../Docs/unity-workflow.md의 WF-B 절을 따른다. begin/context/status, 일반 편집 감지, verify 결과 연결 및 변경 후 무효화를 실제 검증했다. 전체 사용 중심 전환·실제 사람 확인/완료 성공과는 구분한다. 요약 revision은 7이다. 증거 reviews/2026-09-07-wfb-evidence.json.

## M2b 편집 계약 (2026-09-07)

brain_read_edit(taskId, expectedRevision, nodeId)로 기존 Code/Document 하나의 content·summary·expectedHash·expectedContextHash·writable을 읽는다. Code 해시는 파일 bytes SHA256이고 Document 해시는 UnityBrainJson으로 직렬화한 전체 노드 payload SHA256이다. 서로 다른 해시 의미를 혼용하지 않는다. 문서 context 해시는 본문·연결 코드·의미 관계 기준이며 코드/관계 변경도 갱신 충돌로 거절한다.

brain_apply는 nodeId에 연결된 기존 프로젝트 .cs 하나만 허용한다. 작업 허용 경로·revision·예상 해시가 맞아야 쓰며 경로는 GUID에서 해석한다. 생성/삭제/이동/meta/바이너리는 미지원. UTF-8 128KiB 이하, NUL 불허. 읽기 결과의 BOM 문자와 줄바꿈은 호출자가 유지한다. 변경 후 needsAssetRefresh=true이면 Ivan assets-refresh를 실행한다. 자동 재컴파일을 시작해 응답을 잃지 않도록 apply 자체는 refresh하지 않는다.

brain_update_document는 nodes Document의 summary/body만 갱신한다. 연결 코드 경로가 모두 허용 범위여야 하고 expectedHash와 expectedContextHash를 함께 검사한다. ID/type/title/관계·작업 기준선은 변경하지 않는다. 본문이 바뀌면 status=unreviewed, updatedUtc를 갱신한다. 사람 review나 freshness는 자동 변경하지 않는다. 작성한 내용과 코드 근거를 확인한 뒤 brain_record_basis를 명시적으로 호출할 수 있으며 이는 사람 승인이 아니다. 기존 docs 사본과 자동 동기화하지 않는다.

편집 이력은 .projectbrain/edits/<UUID>.json의 actor=agent, 작업/revision/노드/작업 종류, 전후 해시/시각/state다. 쓰기 전 prepared를 저장하고 파일과 최종 이력 저장까지 성공한 뒤 applied를 반환한다. 중간 실패는 prepared를 남기고 경로가 포함된 오류를 반환한다. 일부 내용이 이미 바뀌었을 수 있으므로 재조회·해시 확인 뒤 재시도하며 원본 복원을 자동 강제하지 않는다. 동일 내용은 unchanged로 반환하고 이력을 만들지 않는다. 파일+이력의 다중 파일 트랜잭션이나 외부 편집기와의 OS 수준 CAS는 보장하지 않는다. prepared의 자동 복구/정리 및 이력 요약 UI는 후속이다.

실제 호출: read_edit Code/Document → apply(이동 방식 주석) → 오래된 Code 해시 거절 → 오래된 Document context 거절 → 다시 읽고 update_document → 첫 파일 교체 실패/원본 보존 → 다시 읽고 재시도 성공 → assets-refresh → verify. 사람 확인 상태는 unreviewed 유지.

## W1b 작업 범위·종료 계약 (2026-09-07)

현재 MCP는 13개다. brain_set_scope(taskId, expectedRevision, allowedPaths, reason)는 사용자에게 승인된 범위를 명시적으로 바꾼다. 자동 범위 확장/실패 우회 용도가 아니다. 목적/대상 노드/최초 baseline은 유지하고 revision을 올리며 이전/새 경로·이유·시각을 active.json.scopeChanges에 함께 원자 저장한다. 동일 경로 배열은 무변경이다. 기존 작업의 생략된 scopeChanges는 빈 배열로 읽으며 명시적 null/잘못된 형식은 거절한다. 범위 변경은 미매핑·문서 확인·검증을 승인하지 않는다.

brain_close_task(taskId, expectedRevision, disposition, reason)는 completed 또는 abandoned를 요구한다. completed는 현재 고정 완료 정책을 다시 검사하고 Activity를 저장해야 종료한다. abandoned는 미완료 종료이며 성공을 만들지 않고 미해결 사유를 보존한다. 막힌 작업을 숨기기 위해 자동 abandoned로 전환하지 않는다. 검증 running 중에는 범위 변경/종료를 거절한다.

종료 기록 .projectbrain/tasks/archive/<taskId>.json은 전체 작업/기준선·범위 이력·종료 이유/시각·변경 목록/감시 한계·완료 판정을 보존한다. 이 단일 파일이 종료 표시이며 active.json bytes는 복구용으로 남긴다. 유효한 종료 사본일 때 Load는 활성 작업 없음으로 처리한다. 이후 brain_begin에 새 목적/대상/허용 경로를 주면 새 ID와 현재 baseline을 만든다. 기존 작업의 결과가 새 작업의 성공으로 이전되지는 않는다. 종료한 ID를 begin으로 재개하는 기능은 없다.

동일 ID/revision/종류/이유의 종료 재시도는 기존 결과를 반환한다. 새 작업이 시작된 뒤 이전 종료를 재시도해도 새 작업은 닫히지 않는다. 종료 기록은 덮어쓰지 않으며 손상·active 사본 불일치는 자동 덮어쓰기를 막는다. brain_task_history(taskId)는 현재/종료 요약·범위 이력과 archivePath를 반환한다. 종료 판정은 그 시점의 역사이며 현재 검증이 아니다. 전체 baseline은 파일에 둔다. 활성 작업이 없을 때 status/edit/update는 명시적 오류를 반환하므로 history 또는 begin을 사용한다.

최소 구현은 MCP/API 흐름이다. Explorer의 기존 새 작업 시작은 종료 후 사용할 수 있으나 범위/종료 전용 화면과 전체 작업 목록·재개 UI는 후속이다. 완료 Activity와 archive의 여러 파일 트랜잭션/외부 편집기와의 OS CAS는 없다. Activity 저장 후 archive 실패 시 활성 작업은 남을 수 있으며 성공 종료로 처리하지 않고 history/status를 재조회한다. abandoned 이후 새 기준선에 포함되는 기존 변경은 옛 archive에 남으며 자동 해결된 것으로 설명하지 않는다.

## W1b-UI 사람용 작업 관리 (2026-09-07)
Explorer의 작업 관리 버튼 또는 Window > Project Brain > Task Management로 전용 창을 연다. 현재 목적/버전/기준 파일 수/허용 경로를 표시한다. 경로를 한 줄씩 입력하고 변경 이유와 함께 범위를 저장한다. MCP와 같은 BrainTaskLifecycle을 사용하므로 기준선·사람 확인 정책이 동일하다.

완료 조건 확인·완료 종료·미완료 종료를 구분한다. 종료 이유는 필수이며 미완료 종료는 체크를 직접 선택해야 버튼이 활성화된다. 저장하지 않은 범위 입력이 있으면 종료를 거절한다. 실제 사람 문서 확인은 Explorer에서 별도로 진행한다.

입력 초안은 프로젝트별 Unity SessionState에 저장되어 창 재생성/재컴파일/같은 Editor 세션의 재열기에서 유지된다. Editor 종료를 넘는 보존은 보장하지 않는다. 다시 읽기는 수정 중인 입력을 유지하며 ID/버전이 바뀌면 오래된 입력임을 안내하고 저장을 거절한다. 입력 버리기는 최신 작업으로 명시적으로 다시 시작한다. 범위 저장 시 별도로 작성한 종료 이유도 보존한다.

이전 작업은 ID로 조회하거나 최근 종료20개 목록에서 선택한다. 범위 변경 이유·진행/결정/미해결/다음 행동·종료 당시 사유를 표시한다. 종료 기록은 현재 검증 결과가 아니다. 손상된 기록은 파일명과 오류를 표시하며 다른 기록 조회는 계속한다. 종료한 작업을 재개하는 기능은 여전히 없다. 새 작업은 Explorer에서 선택한 대상으로 시작한다.

검증: 실제 렌더 780×800/580×540에서 줄바꿈·스크롤 확인, Unity 내부 실제 버튼 콜백9항목과 격리 Lifecycle28항목 통과. UI 무변경 저장/완료 거절/오래된 요청 후 실제 active bytes 유지. computer-use의 창 활성화 실패로 물리 마우스 입력 검증은 미완료. 실제 프로젝트의 사람 확인/종료 성공을 대신하지 않았다.

2026-09-07 W1b-UI 마우스 재검증: 사용자 요청 후 computer-use 실제 마우스로 새로 읽기, 완료 조건 조회, 창 제목줄 드래그, 휠 스크롤, 범위 이력 펼치기, 기존 ID 기록 조회를 확인했다. 최초 오래된 버전 안내 후 새로 읽기로 revision10을 표시했고 완료 조건은 허용 밖27/미매핑27/사람미확인2를 표시했다. 이번 활성화 오류 없음. 앞선 실패 원인은 확정하지 않는다. 범위 저장·종료·사람 확인 버튼은 실행하지 않았다.

## U1-2 · 문서 구조와 가독성 보완 (2026-09-07)

Script Document는 문서 내용/첨부 이미지/연결 코드/관계도 네 탭으로 구성한다. 역할·설계 의도·주의사항·상세 본문은 제목을 위에 둔 줄바꿈 입력창이다. 저장 상태·작성 항목 수·다시 읽기/코드 열기/저장은 고정 하단에 표시하고 GUID는 문서 정보에 접는다. 작성 수는 내용 검증을 뜻하지 않는다.

관계도는 직접 들어오고 나가는 연결의 노드들을 좌우로 배치한다. 많은 연결은 세로 스크롤로 확인한다. 문서 전환 시 미저장 변경 확인을 유지하며, 탭 전환·화면 재생성에서 초안을 보존한다. 이미지/코드 연결 제거는 파일 삭제가 아니다. 기존 docs와 Explorer nodes 사본은 자동 동기화되지 않는다.

Explorer/작업 관리의 긴 입력도 줄바꿈한다. 검증 상태·사람 확인 상태는 쉬운 한국어로 표시하고 원본 검증 기록은 펼쳐 확인한다. 전체 그래프에서 겹치는 이름만 일시 생략하고 노드/관계는 유지한다. 생략 수를 표시하며 노드 선택 시 이름이 우선 나타난다. 확대·필터로 확인 범위를 좁힐 수 있다.


## U1-4 · 프로젝트 구조도 탐색 (2026-09-07)

Explorer 창과 메뉴 이름은 프로젝트 구조도다. 왼쪽 도메인·하위 기능을 누르면 등록된 포함/구현/문서/이미지/기록 관계를 따라 해당 범위만 표시하고 화면에 맞춘다. depends_on은 범위를 확장하지 않는다. 범위 밖 연결은 접힌 목록에 외부 노드별로 묶으며 방향·종류·출처는 툴팁과 상세에서 확인한다. 전체 구조 버튼으로 모든 범위로 돌아간다. 도메인 소속을 파일명으로 추측하거나 데이터를 재분류하지 않는다.

검증·작업 기록 노드는 기본 숨김이며 필터로 다시 표시한다. 한 번 클릭은 선택과 상단 요약, CS 노드 더블클릭은 해당 스크립트를 선택한 Script Document를 연다. 같은 문서를 다시 열면 초안을 유지하고 다른 문서 전환은 미저장 확인을 거친다. 우클릭 코드 열기는 IDE를 연다. 도메인·기능 더블클릭은 범위 탐색이다.

하단 우측에 떠 있던 상세 창을 제거하고 상단 상세 보기로 여는 오른쪽 전체 높이 패널로 바꿨다. 사람 문서 확인·검증 결과·관계 탐색은 이 패널에 유지한다. 문서 저장의 Explorer 자동 반영(U1-3)은 그대로이며 저장과 사람 확인은 별개다.
