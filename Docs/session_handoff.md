# Brain 세션 인계
Updated: 2026-09-06

## Current state

A2 최소 Explorer·W1 작업 저장/재개·M2a 제한 맥락/최신성 구현·검증 완료. 기존 118항목+BrainWorkflowChecks 39항목 자체 검사 통과. 실데이터 19노드/32관계, 원본 docs 7개와 migration receipt 보존. W2/M1 문서 확인·완료 거절 및 A3/V1 실제 검증 연결은 아직 없다.

기존 Ivan HTTP 서버에 Brain 도구 5개 등록 및 직접 tools/list/tools/call 검증. 현재 Codex 내장 카탈로그에는 동적 추가가 반영되지 않았으므로 다음 세션에서 내장 노출을 별도 확인한다. 서버 재설정은 하지 않았다. [현재 사용법](brain-usage.md)을 따른다.

## Decisions

- 작업 기억은 tasks/active.json, 최신성 근거는 freshness에 저장한다. UI와 MCP가 같은 서비스를 쓴다. 기존 docs UI와 Explorer nodes는 자동 동기화하지 않는다.
- 현재 활성 작업 ID는 a48a9ba7-be69-4985-a648-ed5d2ac5442f, revision 4. 완료/교체 기능 전까지 유지한다. 새 begin은 원래 작업을 반환하며 다른 명시적 ID는 거절한다.
- 실제 DemoPlayerMovement의 입력 공급 의존성 주석만 바꿨다. 게임 동작은 바꾸지 않았다. 원래 설명/관계 기준은 stale로 남겼으며 승인처럼 재기록하지 않았다.
- begin 이후 추가 UI/맥락 코드 보완은 허용 밖 변경으로 표시된다. 이 기록을 없애려고 기준선을 덮어쓰지 않는다.
- WF-B 전체 연동은 선행 W2/V1 이후다. 기존 MCP 개발·컴파일·검증 운영은 유지하며 없는 complete/verify를 요구하지 않는다.

## Next action

1. 날짜·지원 준비도를 확인한다. 사용자 확정 서류·PPT 준비 마감은 9/7 15:00 KST, 내부 제출 목표도 15:00, 공식 마감은 16:00이다. 9/7 새 기능 착수 금지는 유지한다.
2. W2/M1 문서 확인·완료 거절을 구현한다. 이후 A3/V1 실제 결과 연결·완료 성공 → WF-B 파이프라인 연동/지침 전환 → P1 증거 정리.
3. 전면 설계 검토를 반복하지 않는다. 현재 API/제한은 brain-usage, 실행 증거는 reviews/2026-09-06-workflow-evidence.json을 참조한다.

## Verification

MCP 컴파일 완료 상태 확인. 자체 검사 Store25/Migration18/Document14/Repair61/Workflow39 통과. 실제 HTTP begin/update/basis/context current → 주석 변경·assets-refresh/도메인 재로딩 → 동일 작업/기준선 재개 → modified/allowed·document/relation stale 확인. 오래된 revision 거절과 원본 보존 확인.

최종 context 실제 JSON 6,988자/8노드/9관계이며 chars 보고값과 일치했다. 관측 생략과 원본/자산 경로를 포함한다. 전체 조회 성능·토큰 절감 측정이 아니다.

Computer Use로 Feature→Code→Document 및 역방향, 이미지, 두 크기(1000×680/760×500)의 표시/스크롤을 확인했다. 부착된 별도 창에서 미저장 초안의 화면 재구성 보존 확인. 실제 UI 저장은 revision 3→4, 기준선 유지. 첫 시도 파일 교체 실패는 원본을 보존했고 명시적 재시도 성공, 원인은 미확정이다.

관리 검사 결과는 최신 work_log를 따른다. 이미지 근거는 reviews/2026-09-06-explorer.png.

## Limitations

W2/V1/전체 WF-B, 전체 최종 UI·검색·편집, Player 빌드/실제 플레이, 외부 의존성 전체 coverage는 미검증/미구현이다. Git dirty 자동 조회·GUID 이동 추정은 없다. 임시 JsonElement 반환형은 Reflector schema 오류를 일으켜 typed DTO로 수정했으며 등록 복구를 위해 UI Refresh 1회 사용했다. 현재 도구는 정상 호출됐다.

사용자 기존 변경 Assets/Plugins/NuGet/System.Runtime.CompilerServices.Unsafe.dll.meta 및 ProjectSettings/ProjectSettings.asset은 보존하고 커밋하지 않는다. 커밋 후 이 두 파일만 남으면 관리 검사가 기록 갱신을 요구할 수 있다. 제품 실패와 구분한다.
