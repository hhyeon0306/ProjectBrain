# Brain 세션 인계
Updated: 2026-09-04

## Current state
T0 준비 완료, T1 구현 대기. Unity 6000.3.8f1 / Unity-MCP 0.90.0. 이전 MCP 씬 조회 성공(SampleScene, 루트 3개). Brain 자체 코드 미구현.

## Decisions
기존 Unity-MCP의 사용자 도구로 Brain 서비스 제공. 자체 서버·제품 CLI 개발 안 함. 핵심 로직 → MCP 연결 → 검증 → UI. 현재 기준 product_spec, 과거 조사 archive.

## Next action
설치된 MCP asmdef/사용자 도구 선언만 확인하고 Packages/com.projectbrain.editor 골격 및 .projectbrain 데이터 저장 구현(T1). 이후 해시 감지와 완료 거절(T2/T3).

## Verification
관리 검사는 scripts/verify.ps1. 실제 컴파일/테스트는 Unity MCP로 별도 실행. 이번 변경은 관리 인프라이므로 Unity 빌드 미실행. 서버 설정은 상위/하위 동일 연결 유지, 로컬 Git 제외.

관리 검사 결과: 2026-09-04 verify/session-start 통과, 기록 누락 거절 시험 통과. 구현 작업과는 별도 검증이다.
