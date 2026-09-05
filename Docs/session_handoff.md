# Brain 세션 인계
Updated: 2026-09-05

## Current state
T1 진행 중. 본문·이미지·관련 코드 문서 UI 완료, schema v2 저장 및 v1 호환. 선택 코드의 1단계 수동 관계 그래프와 7개 문서로 구성한 데모 관계망 추가. 작업/검증 업무 흐름 미구현.

## Decisions
UI Toolkit 사용. 이미지는 Assets의 Texture2D GUID 참조이며 문서당 첨부 목록으로 표시. 문서는 스크립트당 하나, 관련 코드 GUID로 문서 연결. 본문은 일반 텍스트(서식 렌더링 미구현). 사용자 기존 문서는 검증에서 변경하지 않음.

## Next action
열려 있는 BrainDocumentSample 문서에서 `저장된 문서 다시 읽기`를 눌러 중심 노드와 6개 관계 노드를 시각 확인한다. 다음은 실제 UI 확인 후 T1 작업 저장 및 T2 변경 감지.

## Verification
Unity MCP script_execute로 DocumentStoreChecks.RunInEditor: 13개 통과. 별도 임시 저장소에서 BrainDocumentSample + 실제 URP.png + Readme 관계를 저장/재읽기하고 임시 UI 창에서 이미지 미리보기, 본문 편집·dirty·저장 확인 성공. UI 미부착 테스트는 이벤트 미발생으로 실패하여 실제 창에 부착해 재검증. 전체 그래프/AI 사용 흐름 검증 아님.

## Limitations
전체 그래프 탐색·Markdown 렌더링·독립 문서·자동 코드 관계 분석은 아직 없음. 이미지 파일을 먼저 Assets로 가져와야 함. 사용자 창의 전체 시각 확인은 사용자 검토 대기. NuGet meta, ProjectSettings.asset 및 기존 생성 Scripts meta는 사용자/Unity 변경으로 이번 커밋 제외. 구 경로 Project Brain에는 삭제 정책에 차단된 빈 .git 껍데기만 남음.
2026-09-05 검증: Logs/brain-graph-checks.log에 저장 검사 13개 통과, 컴파일 오류 없음. 관계 그래프 시각/클릭은 수동 확인 대기.

2026-09-05 데모: BrainDocumentSample 중심으로 GameFlow/Input/Movement/Health/HUD/Save 문서와 관계를 추가했다. Unity가 파일을 가져왔고 Editor.log에 새 컴파일 오류가 없으며 Editor는 정상 응답 중이다. 현재 열린 창은 저장된 문서 다시 읽기가 필요하다.

2026-09-05 탐색 수정: Sample→Flow처럼 한쪽 문서에만 저장된 관계도 Flow에서 Sample로 돌아갈 수 있도록 들어오는 관계를 함께 조회한다. dotnet 컴파일 오류 0/기존 참조 경고 3. 열린 Unity가 아직 외부 변경을 가져오지 않아 Unity 내 동작 검증은 재컴파일 후 필요하다.
