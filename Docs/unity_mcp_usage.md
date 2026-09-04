# 설치된 Unity-MCP 사용법

## 대상과 현재 상태
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP), 설치 버전 0.90.0.
- Unity 프로젝트: C:/Dev/nexontutorial/ProjectBrain, Editor 6000.3.8f1.
- 연결 설정은 상위 및 프로젝트 .codex/config.toml. 현재 서버 이름 ai-game-developer, stdio, 로컬 포트 25766, 프로젝트 식별자 cbd0af11. 재설정 후 달라질 수 있으므로 파일에서 재확인한다.
- 실행 파일은 프로젝트 Library/mcp-server/win-x64/gamedev-mcp-server.exe. Library 삭제 후에는 플러그인의 서버 준비가 필요할 수 있다.
- stdio는 Codex가 서버를 시작하는 구성이다. Unity 창의 Start를 별도로 누르거나 다른 서버를 중복 실행하는 것을 기본 절차로 삼지 않는다.
- 과거 scene-list-opened 성공 이력은 있으나 현재 세션에서 도구가 노출되는지 별도 확인해야 한다.

## 도구 확인과 호출
실제 제공된 도구 스키마가 기준이다. 이 프로젝트의 `.agents/skills/<도구명>/SKILL.md`는 설치 버전의 보조 설명이다. 필요한 파일만 읽는다. 자동 생성 예제의 `string_value`, `maxEntries: 0` 등을 그대로 호출하지 않는다.

Codex 도구는 `mcp__ai_game_developer__scene_list_opened`처럼 노출된 이력이 있다. 이름은 실제 목록에서 확인한다. functions 환경에서는 ALL_TOOLS에서 해당 이름의 설명/스키마만 찾고 tools의 실제 메서드로 호출한다. 도구가 없으면 이름을 만들어 호출하지 않는다.

| 목적 | 플러그인 도구명 | 주의 |
|---|---|---|
| 연결·씬 확인 | scene-list-opened | 먼저 읽기 호출, 미저장 여부 확인 |
| Editor 상태 | editor-application-get-state | 활성화되어 있을 때만 사용 |
| 외부 파일 변경 반영 | assets-refresh | 재컴파일 완료와 구분 |
| 오류 확인 | console-get-logs | maxEntries 5~10, 필터, 스택 최소화 |
| 테스트 | tests-run | EditMode 우선, 범위 지정, 실제 최종 결과 확인 |
| 씬/오브젝트 작업 | scene-get-data, gameobject-find 등 | 먼저 대상 조회 후 변경 |
| 사용 가능한 도구 검색 | unity-tool-list | 제공될 때 regexSearch로 범위 제한 |

console-get-logs 입력 예: `{"maxEntries":5,"logTypeFilter":"Error","includeStackTrace":false,"lastMinutes":5}`. 성공 응답의 structuredContent가 있으면 필요한 결과만 읽고 content의 중복 JSON 및 전체 스택을 동시에 출력하지 않는다.

## 코드 작업 순서
1. 기존 사용자 변경과 열린 씬 상태 확인.
2. C# 코드 수정. 현재 Brain 저장 기능은 자동으로 이 수정을 기록하지 않는다.
3. 제공되면 assets-refresh 호출. 컴파일 중이면 완료를 기다리고 새 오류 확인.
4. 관련 검증만 실행하고 결과를 work_log 및 handoff에 기록.
5. 문서/Git 검사 후 관련 변경만 커밋.

tests-run은 미저장 씬에서 실패하도록 구현되어 있다. 사용자가 작업 중인 씬을 도구 실행만을 위해 임의 저장하지 않는다. 재컴파일로 Processing을 반환하면 최종 결과를 확인할 때까지 통과라고 기록하지 않는다. 테스트 0개 실행도 의도한 검증의 성공 증거가 아니다.

## 연결 장애
- 도구 미노출: 호스트가 제공한 목록 → 상위 config의 서버 항목 → 실행 파일 존재 → Unity 실행 상태 순으로 확인.
- 호출 실패: 프로젝트 대상/포트 일치, 최근 연결 오류 확인. 인증값이나 전체 프로세스 명령줄을 출력하지 않는다.
- 설정을 바꾼 경우 필요한 MCP 재로딩/앱 재시작만 안내한다. 사용자의 현재 UI 작업을 끊지 않는다.
- 현재 환경에서 MCP를 사용할 수 없고 Editor도 꺼져 있다면 설치된 Unity 배치 실행으로 검증할 수 있다. 실제 배치 로그와 종료 상태를 확인한다.
- `ProjectBrain.DocumentStoreChecks.RunBatch`는 검사 후 EditorApplication.Exit를 호출하므로 **사용자가 열어둔 Editor에서 실행하지 않는다.** 전용 batchmode 프로세스에서만 사용한다.

## 확장 경계
향후 Brain의 C# 서비스를 사용자 정의 MCP Tool에 연결한다. 설치된 패키지의 AiToolType/AiTool 및 메인 스레드 실행 패턴을 확인한 뒤 작성한다. Library/PackageCache의 원본을 수정하지 않는다. 별도 자체 MCP 서버·제품 CLI는 만들지 않는다.

이 문서는 설치된 자동 생성 안내를 읽어 정리한 것이다. 비활성 도구 활성화나 신규 연결 검증을 이번 문서 작업에서 수행한 것은 아니다.
