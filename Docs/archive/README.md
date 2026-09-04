# 문서 안내

- `PROGRESS.md`: 다음 세션이 먼저 읽을 현재 상태.
- `IMPLEMENTATION_TASKS.md`: 최종 구현 순서, 작업 계약, 완료 기준.
- `PROJECT_PLAN.md`: 초기 목적, 마감 일정, 범위 및 평가 계획.
- `INTERFACE_DECISION.md`: CLI/MCP 조사 당시 결정과 최신 정정.
- `UNITY_INTEGRATION_RESEARCH.md`: Unity 공식 도구 및 오픈소스 조사 기록.

최신 구현 방향은 PROGRESS와 IMPLEMENTATION_TASKS를 따른다. 조사 문서는 이전 판단을 포함할 수 있다. 일반 문서는 이 Docs에 두고 Codex가 읽는 AGENTS.md는 작업 루트에 둔다.

## 디렉터리 기준
```text
C:/Dev/nexontutorial/                 Codex 대화·작업 루트
  AGENTS.md                          지원 준비 전체 지침
  .codex/config.toml                  이 작업의 MCP 설정
  Project Brain/                     Unity 프로젝트
    .codex/config.toml                Unity 플러그인 생성 설정
    .agents/skills/                   플러그인 생성 도구 안내
    AGENTS.md                        Brain 개발 지침, 완료 후 사용 지침으로 전환
    Docs/                            Brain 프로젝트 문서
    Assets/ Packages/ ProjectSettings/
```

두 config는 같은 ai-game-developer 서버와 25766 포트, 동일 프로젝트 식별자를 사용한다. 상위 파일은 현재 대화 연결을 위해 복사했다. 두 파일 존재만으로 서버가 두 개 실행된다고 단정하지 않는다. Unity의 Reconfigure가 하위만 바꿀 수 있으므로 설정 변경 후 동기화 여부를 확인한다. 현재 정상 연결은 유지하며 삭제·재시작은 하지 않는다.
