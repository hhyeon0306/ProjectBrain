> **과거 기록 — 현재 작업 지침이 아님.** 아래 본문은 당시 상태·결정을 보존한다. 현재 기준은 [작업표](../task.md), [제품 명세](../product_spec.md), [인계](../session_handoff.md)다. 이관 전 경로·미구현 상태·자체 서버 검토안·시작 절차를 현재 지시로 적용하지 않는다.

# Unity 연결 도구 조사

조사일: 2026-09-04. 설치·교체는 수행하지 않음.

## 사용 중인 도구 기록

사용자는 [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)를 이용해 Codex에서 Unity Editor를 제어하고 있다고 설명했다. 이를 기존 사용 환경과 통합 후보로 기억한다. 이 프로젝트에 설치되었다거나 현재 세션에서 연결이 검증되었다는 의미는 아니다.

## 공식 제품의 구분

- 기존 AI Assistant 패키지 `com.unity.ai.assistant`의 Editor 내부 MCP 서버: 공식 문서 검색 결과는 deprecated 및 Unity CLI로의 이전을 안내한다. [이전 안내](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli). 해당 페이지 본문은 이번 웹 추출에서 비어 있어 세부 이전 조건은 추가 확인이 필요하다.
- 새 Unity CLI: 독립 실행 파일 `unity`. Editor·모듈·프로젝트 관리와 자동화를 제공한다. 예전 Hub headless 명령 및 Unity Editor의 batchmode 인자와 구분한다.
- `com.unity.pipeline`: 실행 중 Editor를 CLI에서 조작하도록 하는 Unity 패키지. Unity 공식 소개에 따르면 Unity 6.0 LTS 이상 지원이며 experimental이다. `[CliCommand]`로 사용자 명령을 노출할 수 있다.
- CLI 내장 MCP: 공식 릴리스 노트에 `unity mcp`가 stdio MCP 서버를 시작하고 연결된 Editor 명령을 MCP 도구로 노출한다고 명시되어 있다. 따라서 공식 CLI와 MCP 모드는 양자택일이 아니다.

공식 경로의 개념 구조:

```text
셸/자동화 → unity command ─┐
                          ├→ Unity Pipeline → 실행 중 Editor
AI MCP → unity mcp ────────┘
```

CLI는 JSON 등 구조화 출력을 지원하며 AI도 사용할 수 있다. MCP는 같은 기능을 AI 도구 규격으로 노출한다. 어느 쪽도 그 자체로 Project Brain의 문서 갱신과 검증 완료 조건을 구현하지 않는다.

## Project Brain에 미치는 영향

MCP를 AI 인터페이스로 선택한 방향과 별개로 **MCP 서버·Unity 통신 기반까지 새로 만들 필요는 재검토**한다. 공식 Pipeline의 사용자 명령 또는 기존 IvanMurzak 도구 확장으로 Brain의 서비스만 노출할 가능성이 있다.

현 시점 권고: 사용 중인 도구를 즉시 교체하지 않는다. 먼저 기존 도구의 설치 버전과 사용자 도구 확장을 확인하고, 공식 경로는 작은 연결 실험으로 비교한다. Brain의 핵심 가치는 그래프·설계 문서·변경 감지·검증 상태이며 범용 Editor 원격 제어 재구현은 피한다. 사용자가 이번에 요청한 것은 조사와 기억이므로 통합 도구 채택은 아직 변경하지 않았다.

현재 새 프로젝트는 Unity 6000.3.8f1. 공식 소개의 버전 범위에는 들어가지만 실제 패키지 호환성은 미검증이다. 확인한 Packages/manifest.json에는 Pipeline 의존성이 없다.

## 근거

- [Unity 공식 CLI 소개](https://unity.com/blog/meet-the-unity-cli): CLI, Pipeline, 사용자 명령, Unity 버전 범위, experimental 상태.
- [Unity 공식 CLI 릴리스 노트](https://docs.unity.com/en-us/hub/release-notes): 2026-06-25 항목의 내장 MCP와 stdio 지원. 일부 docs 페이지는 검색 색인에는 본문이 있으나 직접 열기에서 빈 결과가 반환되었다.
- [Unity 공식 CLI 스킬 저장소](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md): 공식 조직이 제공하는 CLI 사용 자료.
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP): 별도 커뮤니티 오픈소스. 공식 제품의 사용 중단 안내와 혼동하지 않는다.

가격·AI 구독 조건은 이번에 원문 확인이 충분하지 않아 확정하지 않는다. 성능·토큰 우위도 실제 동일 작업 비교 전에는 확정하지 않는다.
