# Brain 세션 인계
Updated: 2026-09-04

## Current state
T1 진행 중: 스크립트 문서 저장·조회 구현. Unity 6000.3.8f1에서 컴파일과 저장 검사 9개 통과. 작업 데이터 저장과 T2 이후는 미구현.

## Decisions
기존 Unity-MCP 기반 유지. 새 Editor 패키지는 Packages/com.projectbrain.editor. .projectbrain/docs/<GUID>.json을 사용. 문서 저장은 검토 승인/테스트 성공이 아니다.

## Next action
Unity에서 Window/Project Brain/Script Document 열기 → Assets의 C# 스크립트 지정 → 역할/설계 의도/주의사항 저장·다시 읽기. UI 수동 검토 후 작업 저장과 변경 감지를 진행한다. 이번 세션 Unity는 배치 실행 후 종료, MCP 도구 미로드 상태였다.

## Verification
Unity batchmode -executeMethod ProjectBrain.DocumentStoreChecks.RunBatch: PROJECT_BRAIN_CHECKS_PASSED=9. 로그 Logs/brain-document-checks.log(로컬). UI 시각 확인 및 이동·삭제 시연 미실행. 관리 검사는 scripts/verify.ps1로 별도 실행한다.

Unity-MCP 사용 지침: AGENTS와 Docs/unity_mcp_usage.md에 정리. 현재 사용자 문서 UI 검토 중이며 Editor 조작 없이 문서만 갱신. 다음 세션 실제 도구 노출부터 확인.
