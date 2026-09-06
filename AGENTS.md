# Project Brain 개발 지침

## 작업 기준
- Codex 작업 루트는 `C:/Dev/nexontutorial`이다. Unity 프로젝트 루트는 그 아래 `ProjectBrain`이다. 두 경로를 혼동하지 않는다.
- 세션 시작 시 `Docs/task.md`, `Docs/session_handoff.md` 순서로 읽고 `Docs/architecture.md`와 `Docs/product_spec.md`의 관련 절만 확인한다.
- 사용자 최종 결정: IvanMurzak/Unity-MCP 0.90.0에 Brain 전용 MCP Tool을 추가한다. 독립 MCP 서버나 제품 CLI를 새로 만들지 않는다.
- 상위 `../.codex/config.toml`은 현재 상위 작업의 연결 설정이다. 이 프로젝트의 `.codex/config.toml`은 Unity 플러그인이 생성한 설정이다. 현재 둘은 동일 서버를 가리킨다. 재설정 시 차이를 확인하고 필요한 항목만 동기화한다. 중복 서버 이름을 추가하거나 전역 설정을 덮어쓰지 않는다.
- Unity가 생성한 `.agents/skills`는 외부 도구 사용 자료다. 프로젝트 문서와 함께 옮기거나 전체를 세션에 읽지 않는다.

## 현재 단계: Brain 자체 개발
### Unity-MCP 사용 규칙
- 설치 제품은 IvanMurzak/Unity-MCP 0.90.0이다. Unity 공식 CLI/Pipeline과 혼동하거나 교체하지 않는다. 상세 절차: [Docs/unity_mcp_usage.md](Docs/unity_mcp_usage.md).
- 매 세션 실제 제공된 MCP 도구 이름과 입력 스키마를 먼저 확인한다. 설정 파일 존재나 과거 연결 성공만으로 현재 연결을 가정하지 않는다.
- 연결 확인은 scene-list-opened 등 읽기 호출로 한다. 현재 프로젝트가 Project Brain인지 연결 설정과 함께 확인한다. 사용자가 UI를 확인 중이면 씬 저장·재생·테스트·Editor 재시작을 임의 실행하지 않는다.
- C# 파일 직접 편집 후 assets-refresh가 제공되면 호출하고, 컴파일 완료를 확인한 뒤 필요한 검증만 실행한다. Processing/재연결 대기는 성공이 아니다.
- 씬·프리팹·GameObject는 MCP로 조회한 뒤 변경한다. 범용 script-execute/reflection 호출보다 목적별 도구를 우선한다.
- 로그는 maxEntries 5~10, 시간/심각도 필터부터 사용한다. 반환 객체에 긴 스택이 있으면 전체 출력하지 말고 필요한 메시지만 추출한다.
- tests-run은 대상 테스트를 좁혀 실행한다. 미저장 씬이 있으면 사용자 변경을 임의 저장하거나 버리지 않는다. 테스트 결과가 실제로 끝났는지 확인한다.
- 도구 미노출 시 현재 목록/설정/Editor 실행 상태를 확인한다. CLI 자동 설치·재설정 루프를 돌리지 않는다. 배치 검증은 같은 프로젝트 Editor가 열려 있지 않을 때만 사용한다.
- 현재 Brain 전용 MCP 도구는 미구현이다. 기존 MCP로 코드 편집이 자동 추적된다고 주장하지 않는다.

- Brain 문서 저장·관계 그래프 시제품과 A1 범용 노드·관계 저장소/v2 비파괴 이관은 구현됐다. 새 모델 UI 연결과 작업·검증 도구는 아직 미구현이다. 존재하지 않는 Brain 도구를 필수 호출로 요구하지 않는다.
- A1은 완료됐고 현재 다음 목표는 Player 도메인 데모와 새 모델 UI 연결 → 작업 등록·변경 감지 → 미검토/미검증 완료 거절 순서다. UI 장식보다 작동하는 흐름을 우선한다.
- Brain 핵심 규칙, 저장소, Unity 검증 실행기, MCP 연결 코드를 분리한다. 기존 오픈소스 및 Library/PackageCache를 수정하지 않는다.
- 코드 검증은 실제 결과로 판단한다. 컴파일 성공을 테스트 통과로 표현하지 않고, 이전 코드에 대한 검증을 현재 결과로 재사용하지 않는다.
- Unity 씬·프리팹 조작은 연결된 MCP를 우선 사용한다. 로그는 필요한 항목과 요약만 출력하고 긴 스택 추적은 오류 분석에 필요할 때만 읽는다.
- 작업 하나마다 구현, 필요한 검증, 문서 갱신을 끝낸다. 사용자 요청 없는 하위 에이전트 사용, 반복 조사, 과도한 전체 파일 읽기를 하지 않는다.
- 구현 결과를 `Docs/session_handoff.md`와 작업 체크리스트에 기록한다. 미실행·미검증 사항과 다음 행동을 분명히 남긴다.

## 완료 후 사용 지침으로 전환
- product_spec의 전환 조건을 실제로 만족하면 이 파일을 Brain 사용 중심으로 수정한다. 아직 없는 기능을 이미 사용 가능한 것으로 적지 않는다.
- 전환 후 코드 작업은 Brain begin/context → apply → 문서 갱신 → verify → complete 순서를 기본으로 한다.
- Brain 거절을 일반 파일 수정으로 우회하지 않는다. 원인을 해결한다. Brain 장애 시 상태를 알리고 변경·검증 이력을 보존하며 사용자 지시에 따른다.
- 지침만으로 모든 호출이 강제된다고 주장하지 않는다. 외부 수정 감지 및 완료 조건 검사로 누락을 드러낸다.


## 개발 기록 및 검사
- 시작 시 scripts/session-start.ps1 실행. task → handoff → architecture/product_spec 관련 절 → work_log 최신 기록만 읽는다.
- 작업별로 Docs/work_log.md에 Goal/Changes/Files/Verification/Decisions/Next/Limitations를 남기고 Docs/session_handoff.md를 갱신한다. 실패·미실행도 기록한다.
- 기능·데이터·도구 계약 변경은 product_spec, 단계 상태는 task, 절차는 AGENTS를 수정한다. 과거 자료는 archive에 보존한다.
- 관리 검사: scripts/verify.ps1. 실제 Unity 컴파일·테스트는 MCP/Unity 실행기로 따로 확인한다. 관리 검사 통과를 구현 검증으로 대체하지 않는다.
- 파일 수정 시각은 검증 근거로 쓰지 않는다. 현재 스크립트는 Git 변경과 기록 유무를 검사하며 의미적 정확성까지 보장하지 않는다.
- 구현·검증·작업 기록을 한 작업 단위로 커밋한다. Brain 전체 사용 흐름 검증 완료 후 이 지침을 Brain 사용 중심으로 개정한다.
