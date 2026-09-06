# Brain 세션 인계
Updated: 2026-09-06

## Current state

A1-R 저장 안전성·Git 재현성과 F2 오류 경계 보완 완료. 기존 자체 검사 57항목과 신규 BrainRepairChecks 61항목 통과. 신규 계층 UI·Brain MCP·작업 기억·맥락·최신성·완료 규칙은 미구현이다. 실데이터는 15노드/26관계이며 원본 데이터는 수정하지 않았다.

HTTP MCP 읽기 연결 및 대상 C:/Dev/nexontutorial/ProjectBrain/Assets 확인. Unity 관리 단일 서버와 양쪽 URL 설정을 유지한다. 과거 stdio 장애는 해결된 이력이며 재조사를 반복하지 않는다.

## Decisions

- W1/M2a 최소 계약은 architecture에 확정했다. 전면 재검토 없이 기존 모델과 공통 서비스로 진행한다.
- JSON 입력은 기본값 보충 전에 필수 필드/형식/중복 키를 확인한다. 명시적 v1 문서 호환 유지.
- .projectbrain/**는 Git -text로 bytes를 보존한다. 기존 원본 해시 계약과 receipt를 변경하지 않는다.
- 관계 조회 실패 시 오류 파일 경로와 재조회 동작을 표시하고 현재 문서·미저장 내용을 유지한다.
- MCP로 개발·컴파일·제품 검증. CLI는 필요한 환경 점검만 기본 사용. WF-B 연동은 선행 기능 구축 후 진행한다.

## Next action

1. 날짜·제출 준비도를 확인한다. 9/6 서류/PPT 시간 확보, 9/7 새 기능 착수 금지. 내부 제출 목표 15:00, 공식 마감 16:00 KST.
2. A2 공통 계층 서비스·최소 탐색과 W1/M2a를 연결한다: 작업 등록/요약 저장 → 새 세션 재개 → 관련 맥락 조회 → 변경 후 오래된 자료 표시.
3. 이후 W2/M1 거절 → A3/V1 성공 → P1 증거를 진행한다. F2 화면/실제 마우스 QA는 UI 검증 시 함께 확인한다.

## Verification

- assets-refresh 이후 MCP script-execute로 컴파일 완료 상태 확인. BrainStoreChecks 25, BrainMigrationChecks 18, DocumentStoreChecks 14, BrainRepairChecks 61 통과. NUnit/Test Runner 케이스 수가 아닌 자체 검사 체크 수다.
- 신규 검사는 누락/잘못된 JSON의 읽기·덮어쓰기 거절과 원문 보존, 명시적 빈 배열·v1 호환, 격리 UI 오류 안내·미저장 유지·복구를 확인한다.
- 검증 스냅샷 22ad756989b6e32f8a94ee6455d714d612f9f034의 새 clone(core.autocrlf=true)에서 이관 2회 실행. 원본 7개/노드 15개/관계 26개와 receipt 포함 전체 데이터 bytes 불변, Git 변경 0 확인. 열린 Editor의 구현으로 clone 데이터만 검사했다.
- 관리 검사 결과와 실행 근거는 최신 work_log 및 reviews/2026-09-06-repair-evidence.json 참조.

## Limitations

최종 UI의 시각/마우스 QA, 새 clone의 Unity import/빌드, 실제 플레이는 미실행. System.Text.Json 8은 기존 프로젝트 DLL을 사용하며 새 설치는 없다. 자동 복구·동시 쓰기 보장은 없다. 이미 줄바꿈이 바뀐 과거 clone은 자동 수정하지 않는다.

기존 사용자/Unity 변경은 보존하고 커밋하지 않는다: Assets/Plugins/NuGet/System.Runtime.CompilerServices.Unsafe.dll.meta, ProjectSettings/ProjectSettings.asset. 커밋 후 이 두 변경만 남아 관리 검사에서 기록 갱신을 요구할 수 있으며 제품 실패와 구분한다.
