# Brain 세션 인계
Updated: 2026-09-05

## Current state
T1 진행 중. 본문·이미지·관련 코드 문서 UI 완료, schema v2 저장 및 v1 호환. Unity MCP 재연결 실제 조회 성공. 선택 코드의 1단계 수동 관계 그래프 추가. 작업/검증 업무 흐름 미구현.

## Decisions
UI Toolkit 사용. 이미지는 Assets의 Texture2D GUID 참조이며 문서당 첨부 목록으로 표시. 문서는 스크립트당 하나, 관련 코드 GUID로 문서 연결. 본문은 일반 텍스트(서식 렌더링 미구현). 사용자 기존 문서는 검증에서 변경하지 않음.

## Next action
사용자는 기존 Brain 창에서 본문/이미지 추가/관련 코드 추가 확인. 현재 그래프 노드 클릭으로 문서 전환 구현. 다음은 실제 UI 확인 후 T1 작업 저장 및 T2 변경 감지. 이후 T1 작업 데이터와 T2 변경 감지로 복귀. 큰 시각 효과보다 작은 실제 흐름 우선.

## Verification
Unity MCP script_execute로 DocumentStoreChecks.RunInEditor: 13개 통과. 별도 임시 저장소에서 BrainDocumentSample + 실제 URP.png + Readme 관계를 저장/재읽기하고 임시 UI 창에서 이미지 미리보기, 본문 편집·dirty·저장 확인 성공. UI 미부착 테스트는 이벤트 미발생으로 실패하여 실제 창에 부착해 재검증. 전체 그래프/AI 사용 흐름 검증 아님.

## Limitations
전체 그래프 탐색·Markdown 렌더링·독립 문서·자동 코드 관계 분석은 아직 없음. 이미지 파일을 먼저 Assets로 가져와야 함. 사용자 창의 전체 시각 확인은 사용자 검토 대기. NuGet meta, ProjectSettings.asset 및 기존 생성 Scripts meta는 사용자/Unity 변경으로 이번 커밋 제외. 구 경로 Project Brain에는 삭제 정책에 차단된 빈 .git 껍데기만 남음.
2026-09-05 검증: Logs/brain-graph-checks.log에 저장 검사 13개 통과, 컴파일 오류 없음. 관계 그래프 시각/클릭은 수동 확인 대기.
