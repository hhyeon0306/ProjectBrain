# Brain 세션 인계
Updated: 2026-09-06

## Current state

A1 범용 노드·관계 저장소와 v2 비파괴 이관 완료. 실제 기존 문서 7개를 Code 7개·Document 7개·Image 1개와 관계 26개로 이관했다. 원본 docs는 그대로 유지한다. UI는 기존 Code 중심 시제품이며 범용 모델과의 연결은 A2다.

## Decisions

Project→Domain→Feature→Code/Document/Image/Evidence/Activity/Reference 계층을 사용한다. Core 저장소는 UI/MCP/Unity 타입에 독립적이고 UnityBrainJson만 JsonUtility를 사용한다. v2 이관은 명시적으로 실행하며 기존 UI 저장과 자동 동기화되지 않는다. 완료 후 원본이 바뀌면 재실행을 거절해 기존 노드 편집을 보호한다. 제품 Activity와 개발 work_log는 별개다.

## Next action

A2 Player 도메인 데모와 BrainStore 기반 UI 탐색을 구현한다. 9/6 서류·PPT 시간을 확보하고 9/7에는 새 기능을 시작하지 않는다. 공식 마감 9/7 16:00, 제출 완료 목표 15:00을 우선한다.

## Verification

- 현재 Unity MCP scene-list-opened 및 console-get-logs 실제 호출 성공. ProjectBrain/Assets 경로 확인, isCompiling=false.
- 최종 Unity 검사: BrainStoreChecks 25개, BrainMigrationChecks 18개, DocumentStoreChecks 14개 통과. 전용 임시 저장소에서 실행하며 씬 변경이나 Editor 종료 없음.
- 실제 이관: 원본 7개 SHA256 일치, 노드 15개·관계 26개 재읽기 성공, 반복 실행으로 완료 시각 유지 및 중복 없음. git diff -- .projectbrain/docs 비어 있음.
- 관리 검사: 상위 scripts/verify.ps1 -IncludeBrain 커밋 전 통과. Unity 검증과 별개다.

## Limitations

UI는 아직 새 노드/관계 데이터를 사용하지 않는다. 이관 관계의 종류를 수정하는 UI도 A2 이후다. 작업 등록·변경 감지·완료 거절·Brain 전용 MCP·실제 검증 증거 저장은 미구현이다. 이관의 missing 상태는 실행 시 자산 해석 결과이며 실시간 감시는 없다. 여러 파일의 이관은 전체 트랜잭션이 아니며 중단 시 같은 원본의 정확한 부분 결과에서 재실행한다. 기존 사용자/Unity 변경 ProjectSettings.asset과 NuGet meta는 보존하고 커밋 제외한다.
