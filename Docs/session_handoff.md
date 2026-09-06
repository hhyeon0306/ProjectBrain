# Brain 세션 인계
Updated: 2026-09-06 (세션 종료)

## Current state

최신: Computer Use로 HTTP 전환/Start/Codex Reconfigure 완료, 양쪽 URL 설정 동기화. 직접 HTTP MCP에서 38개 도구 조회 및 씬/로그 조회 성공(RootCount=2, IsDirty=false). Unity가 단일 서버를 관리한다. 현재 대화 내장 연결은 구 stdio 상태여서 새 설정 로딩 후 재시험 필요. 아래 장애 기록은 전환 전 이력이다.

15:33 Codex 단독 재실행 후 재시험: 현재 세션 Transport closed 재발. 다른 MCP 프로세스 PID 36240은 Unity 연결/tools-list 완료했으나 동시 서버 시작은 포트 충돌로 실패. 아래 통신 복구는 직전 시험 상태다. 단순 재시작으로 해결되지 않았으며 중복 시작 경로 확인 필요. 설정/서버 변경 없음.

최신 확인(2026-09-06 재실행 후): MCP resources/list·templates/list 정상 응답으로 서버 통신 복구. 초기 tools/list가 Unity 연결 전에 재시도 소진했고 이후 15:31:54 Unity handshake 성공. 현재 세션 Unity 도구는 여전히 미노출. Unity 연결을 유지한 채 Codex MCP 재로딩 후 씬 조회 필요. 아래 Transport closed/포트 충돌은 재실행 전 상태다.

2026-09-06 재개: W1/M2a의 작업 저장·충돌 없는 재개·revision 갱신·제한된 context·최신성 최소 계약을 architecture에 확정했다. 현재 세션 도구 목록에 Ivan MCP가 없으며 검색 도구도 없다. Unity 실행과 양쪽 설정 일치는 읽기로 확인했다. 실제 연결·컴파일·테스트는 미확인이고 제품 코드는 변경하지 않았다.

최종 운영: MCP 중심 개발·최종 검증, CLI는 필요 시 환경 점검만 기본 사용한다. task의 WF-B는 Brain 관련 기능 구축 후 현재 파이프라인과 연동하고 양쪽 AGENTS.md/사용법을 실제 호출 계약으로 갱신하는 후속 작업이다. 현재는 연동하지 않는다.

독립 워크플로우 시연: [대시 예제](workflow-development-demo.md)와 실제 NUnit 테스트 9개를 Assets/WorkflowDemo에 추가했다. MCP 최초 9/9, 의도적 경계 결함 7/9, 수정 후 9/9 통과. Brain 기능/연동은 수정하지 않았다. 최종 결정에 따라 CLI 배치/Editor 종료 시연은 진행하지 않는다. 데모 meta의 자동 생성 후행 공백을 정리했다.

기초 설계 검토 완료. 사용자의 질문은 무결함 여부가 아니라 원하는 AI Second Brain을 현재 구조로 구현할 수 있는지였다. 가능하다고 판단했으며 전면 재설계·추가 전면 검토 없이 구현으로 진행한다. 이번에는 문서만 정리하고 세션을 종료했다. 다음 세션에서 구현을 재개한다.

A1 범용 저장소·v2 비파괴 이관과 기존 문서/그래프 시제품은 구현됐다. 마지막 확인 데이터는 Code 7·Document 7·Image 1, 관계 26개다. 새 계층 UI, Brain 전용 MCP, 작업 기억·맥락 조회·최신성 관리·완료 규칙은 미구현이다. 제품 코드 마지막 구현은 7fd9d66, 후속 검토 기록은 d41f9f1이다.

## Decisions

- 최종 목업과 노드/관계 구조, UI와 AI의 공통 서비스, 기존 Unity-MCP 재사용을 유지한다.
- 핵심 목표는 매 세션 전체 문서를 다시 읽지 않고 작업을 복원하고 현재 코드의 관련 맥락을 찾는 것이다. 그래프 UI만으로 달성했다고 판단하지 않는다.
- 작업 기억·관련 맥락 선별·최신성 계약을 구현 과정에서 구체화한다. 상세는 product_spec의 Second Brain 절과 architecture의 작업 기록 절이다.
- 알려진 결함은 A1-R/F2로 보완한다. 과거 리뷰의 ‘기반 재판정’은 최종 설계 결론이 아니다. 리뷰와 work_log는 당시 실행 근거로 보존한다.

## Next action

1. task → 이 인계 → product_spec/architecture 관련 절 → 최신 work_log만 읽는다. 전체 리뷰·archive 재독은 필요 없다. 개발 시 실제 MCP 읽기 호출로 ProjectBrain 연결을 확인한다.
2. architecture의 W1/M2a 확정 최소 계약을 사용한다. MCP 복구 후 다음 결함 보완부터 진행하며 계약 검토를 반복하지 않는다.
3. A1-R의 누락 JSON 필드 거절·Git 줄바꿈 재현성, F2의 UI 오류 경계를 보완하고 관련 회귀 검사 및 새 clone 데이터 이관을 확인한다. 재현 세부가 필요할 때만 [기반 코드 검토](reviews/2026-09-06-project-review.md)와 [증거](reviews/2026-09-06-project-review-evidence.json)를 읽는다.
4. A2 공통 계층 서비스·최소 탐색과 W1/M2a를 연결해 작업 등록/요약 저장 → 새 세션 재개 → 관련 맥락 조회 → 변경 후 오래된 자료 표시를 먼저 검증한다. 이후 W2/M1 거절, A3/V1 성공, P1 기록으로 진행한다. UI 장식은 후속이다.

지원 일정은 유지한다. 9/6 서류·PPT 시간 확보, 9/7 새 기능 착수 금지, 내부 제출 목표 15:00/공식 마감 16:00 KST. 다음 개발 단위 전 날짜와 상위 제출 준비도를 확인한다.

## Verification

현재 연결 진단: resources/list 및 templates/list 모두 MCP startup failed: Transport closed. 25766은 기존 codex.exe 자식 MCP PID 40524가 점유하며 서버 로그(15:28:03)에 address already in use 확인. 실제 Unity 도구는 미노출이라 씬 조회/제품 검증 미실행. 기존 서버 종료·설정 변경 없음.

이번 종료 작업은 문서 정리이며 Unity 제품 검사를 재실행하지 않았다. 이전 검토에서 Unity 자체 검사 25/18/14 통과, 실제 MCP 코드 실행 성공, 실데이터 자산 누락 0을 확인했다. 자체 57항목은 Test Runner·플레이·UI 검증 수가 아니다. JSON 누락 수용·clone 이관 거절·UI 예외 누락도 별도로 재현됐으며 아직 미수정이다. 이번 문서 관리 검증 결과는 최신 work_log에 기록한다.

## Limitations

기초 설계 준비 완료는 제품 완성·무결함·효율 측정 완료가 아니다. 자동 코드 의존 분석과 토큰 절감은 구현/측정되지 않았다. 실제 UI 시각/마우스·플레이·클린 Unity import/빌드는 미검증이다.

기존 사용자/Unity 변경은 보존하고 커밋하지 않는다: Assets/Plugins/NuGet/System.Runtime.CompilerServices.Unsafe.dll.meta, ProjectSettings/ProjectSettings.asset. 양쪽 .codex/config.toml은 같은 서버의 정상 설정이며 단순 중복으로 삭제하지 않는다. 커밋 후 기존 변경만 남아 문서 갱신 검사가 거절될 수 있다. 구현·실제 데이터·씬·설정은 이번 종료 작업에서 변경하지 않았다.
