# Brain 최소 사용 흐름 — A2/W1/M2a

2026-09-06 구현·검증 기준. Unity 프로젝트는 `C:/Dev/nexontutorial/ProjectBrain`이다. 전체 완료 규칙과 WF-B 연동은 아직 없다.

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

검증한 실제 순서: begin → update_task(revision 1→2) → record_basis(설명/관계) → context current → 실제 코드 주석 변경 → assets-refresh/도메인 재로딩 → begin(taskId) 재개 → status modified/allowed → context stale → 오래된 revision update 거절 → 요약 갱신/Explorer 저장. 현재 작업 ID는 `a48a9ba7-be69-4985-a648-ed5d2ac5442f`, revision 4이며 완료/교체 API가 없으므로 활성 상태로 유지한다.

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
