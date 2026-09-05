# Brain 세션 인계
Updated: 2026-09-06

## Current state

Unity-MCP 연결, 스크립트 문서 v2 저장, 이미지·관련 코드 UI, 다중 노드 관계 그래프와 양방향 탐색이 구현됐다. 7개 문서의 게임 구조 데모와 최종 UI 목업이 있다. 현재 구현은 Code 중심 시제품이며 Domain/Feature/Evidence/Activity/Reference 범용 모델은 미구현이다.

## Decisions

최종 제품은 Project→Domain→Feature→Code/Document/Image/Evidence/Activity/Reference 계층을 사용한다. 관계는 종류와 방향을 저장하고 양방향으로 탐색한다. 제품 Activity와 개발 기록 `Docs/work_log.md`를 구분한다. 기존 v2 문서는 보존한 채 새 구조로 이관한다. 상세 계약은 `Docs/architecture.md`, 제품 동작은 `Docs/product_spec.md`가 기준이다.

## Next action

한국시간 9/6 일요일 14:00경 새 세션에서 재개한다. `Packages/com.projectbrain.editor/Editor` 아래에서 `BrainNode`, `BrainRelation`, 저장소와 저장·재읽기 검사를 먼저 구현한다. 이 단위를 검증·기록·커밋한 뒤에만 v2 비파괴 이관을 시작한다. 이어서 Player 도메인 데모를 구성한다. 공식 마감은 9/7 월요일 16:00이며 15:00까지 제출을 마무리하도록 상위 일정 전환 지시를 우선한다. 세부 계약은 `Docs/architecture.md`, 순서는 `Docs/task.md`의 A1을 따른다.

## Verification

- 기존 문서 저장 검사: Unity MCP `DocumentStoreChecks.RunInEditor` 13개 통과.
- 기존 그래프: Unity 6000.3.8f1 배치 컴파일과 저장 검사 통과.
- 관계 역방향 탐색 수정: 생성 프로젝트 파일 기준 컴파일 오류 0, 기존 의존성 참조 경고 3. Unity 내 재검증은 남음.
- 문서 재구성: 상위 `scripts/verify.ps1 -IncludeBrain` 통과. 새 데이터 모델은 아직 코드가 아니므로 제품 검증 결과가 아니다.
- 종료 점검: 2026-09-06 상위·하위 `session-start.ps1` 및 상위 `verify.ps1 -IncludeBrain` 통과.

## Limitations

작업 등록, 변경 감지, 완료 거절, Brain 전용 MCP Tool, 실제 Unity 검증 기록은 아직 없다. 최종 목업의 테스트 수와 토큰 절감량은 예시다. 사용자/Unity 변경인 `ProjectSettings.asset`과 NuGet meta는 건드리지 않는다. 구 `Project Brain` 경로에는 자동 삭제 정책에 막힌 빈 `.git` 껍데기만 남아 있다.
