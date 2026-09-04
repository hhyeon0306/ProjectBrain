# Project Brain — 현재 상태

갱신: 2026-09-04

## 위치
상위 C:/Dev/nexontutorial은 넥토리얼 지원 전체(서류/PPT/프로젝트/면접)의 작업 공간이다. Project Brain은 그중 하나인 Unity 포트폴리오다. 개발 지침은 Project Brain/AGENTS.md, 일반 문서는 Docs에 있다.

## 완료
- Unity 6000.3.8f1 생성, Unity-MCP 0.90.0 설치.
- Codex 재시작 후 MCP scene_list_opened 실제 호출 성공: Assets/Scenes/SampleScene.unity, 루트 3개.
- 상위 및 하위 .codex 설정은 같은 ai-game-developer 서버, 포트 25766, 프로젝트 식별자 569f468d를 가리킨다. 상위는 현재 대화용, 하위는 Unity 생성 설정이다.
- 기존 일반 Markdown 4개를 Docs로 이동. README와 IMPLEMENTATION_TASKS 작성. 상위 지원 지침과 하위 Brain 개발 지침 분리.

## 최종 결정
- IvanMurzak/Unity-MCP 위에 Brain 전용 Tool 추가. 자체 MCP 서버 및 독립 제품 CLI를 만들지 않는다.
- 핵심 로직은 UI/MCP와 분리. 작업 등록, 변경 감지, 완료 거절부터 구현한다.
- 다음으로 context/apply/document/verify를 연결한다. AGENTS로 기본 사용 절차를 안내하고 외부 변경도 감지한다. 모든 편집을 차단한다고 주장하지 않는다.
- 그래프 UI는 핵심 흐름 검증 후 구현한다.

## 미구현 및 다음 행동
Brain C# 코드, 데이터 저장, 전용 도구, 검증 실행기, 검사 스크립트, UI는 모두 미구현이다. 기존 포폴 버전 비교·적용도 미실행이다. 리셋권은 사용하지 않았다.
IMPLEMENTATION_TASKS의 T1부터 시작한다: MCP asmdef/확장 API 최소 확인 → 패키지 골격·저장 → 해시 변경 감지 → begin/status/complete 실제 시연.

초기 PROJECT_PLAN/INTERFACE_DECISION의 자체 서버·CLI 구현안은 폐기된 방향이다. 최신 결정은 이 문서와 IMPLEMENTATION_TASKS를 따른다. 전체 재조사하지 않는다.

## Git 및 작업 추적
- Unity 프로젝트 자체를 로컬 Git 저장소(main)로 초기화. 상위 지원 공간과 분리.
- Docs/TASKS.md로 T0–T7 상태와 작업 기록 추적. 다음 작업 T1.
- 생성 캐시, 로컬 .codex 및 자동 생성 .agents 제외. Assets/Packages/ProjectSettings와 문서 추적.
- 초기 커밋 이후 실제 이력은 git log로 확인. 원격 업로드 미실행.
