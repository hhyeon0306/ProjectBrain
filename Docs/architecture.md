# Project Brain 데이터 및 구조 설계

## 계층

| 종류 | 의미 | 예시 |
|---|---|---|
| Project | 전체 Unity 제품 | Game Client |
| Domain | 큰 업무·게임 영역 | Player, Combat, UI |
| Feature | 사용자가 인식하는 기능 | Movement, Health |
| Code | Unity 코드 자산 | DemoPlayerMovement.cs |
| Document | 설계·운영 문서 | Movement Design |
| Image | 다이어그램·스크린샷 | Movement Architecture |
| Evidence | 컴파일·테스트 결과 | EditMode 12 passed |
| Activity | 작업 및 AI 변경 이력 | Movement refactor |
| Reference | 외부·내부 참고 자료 | Unity Manual |

Project, Domain, Feature는 아키텍처의 뼈대다. Code와 Document는 구현·설명 자료이며 Evidence와 Activity는 특정 시점의 사실이다. Reference는 설계 근거로 연결한다.

## 노드 계약

`BrainNode` 필드:

- `schemaVersion`: 지원 데이터 버전.
- `id`: 전역 고유 ID.
- `type`: Project/Domain/Feature/Code/Document/Image/Evidence/Activity/Reference.
- `title`, `summary`, `body`: 목록 이름·짧은 설명·상세 내용.
- `assetGuid`, `lastKnownPath`: Unity 자산에 연결되는 노드만 사용.
- `tags`: 검색과 필터용 분류.
- `status`: 노드 종류에 맞는 상태. 근거 없는 성공 상태를 만들지 않는다.
- `updatedUtc`: 마지막 저장 시각.

ID 규칙:

- Unity 자산: `asset:<Unity GUID>`.
- 프로젝트·도메인·기능: `project:<slug>`, `domain:<slug>`, `feature:<domain>/<slug>`.
- 시점 기록: `evidence:<UUID>`, `activity:<UUID>`.
- 문서·참고 자료: 안정적인 slug 또는 UUID. 파일명은 ID가 아니다.

## 관계 계약

`BrainRelation`은 `id`, `from`, `to`, `type`, `source`, `createdUtc`를 가진다.

| 관계 | 방향 예시 |
|---|---|
| contains | Player → Movement |
| depends_on | Movement → Input |
| implemented_by | Movement → PlayerMovement.cs |
| documented_by | Movement → Movement Design |
| illustrated_by | Movement Design → Architecture Image |
| verified_by | Movement 또는 Code → Test Result |
| worked_on_in | Feature 또는 Code → Activity |
| references | Feature 또는 Document → Unity Manual |

그래프 탐색은 들어오는 관계와 나가는 관계를 모두 보여주지만 화살표와 관계 종류는 유지한다. 현재 v2의 `relatedScriptGuids`는 관계 종류가 없는 임시 데이터다.

## 작업·검증 기록

`BrainTask`는 ID, 목적, 대상 노드, 시작 스냅샷, 허용 경로, 변경 목록, 문서 확인 상태, 상태를 가진다. MVP에서는 활성 작업 하나만 허용한다.

`VerificationRecord`는 실행 ID, 종류(compile/editmode/playmode), 대상 코드 해시 집합, 시작·종료 시각, 결과, 요약, 로그 경로를 가진다. 로그 전체를 기본 맥락에 넣지 않는다.

`Activity`는 누가 무엇을 왜 바꿨는지 기록하는 제품 데이터다. `Docs/work_log.md`는 Project Brain 자체 개발 과정 기록이므로 서로 다른 개념이다.

## 저장 구조

```text
.projectbrain/
├─ nodes/<safe-node-id>.json
├─ relations.json
├─ tasks/active.json
├─ evidence/<verification-id>.json
└─ activities/<activity-id>.json
```

- 노드 본문을 개별 파일로 나눠 작은 변경과 제한된 AI 조회를 지원한다.
- 관계는 MVP 규모에서 하나의 원자 저장 파일로 관리한다.
- 증거와 활동은 시점별 불변 기록으로 저장하고 정정 시 새 기록을 추가한다.
- 저장 전 스키마·ID·참조 대상을 검증한다. 임시 파일에 쓴 뒤 교체하며 손상된 기존 파일을 덮어쓰지 않는다.

## v2 스크립트 문서 이관

현재 `.projectbrain/docs/<Unity GUID>.json`은 삭제하거나 즉시 덮어쓰지 않는다.

1. 기존 문서를 v2 형식으로 검증해 읽는다.
2. `asset:<GUID>` Code 노드와 연결된 Document 데이터를 만든다.
3. `relatedScriptGuids`는 임시 `depends_on` 관계로 가져오되 UI에서 관계 종류를 수정할 수 있게 한다.
4. `imageGuids`는 Image 노드와 `illustrated_by` 관계로 변환한다.
5. 새 구조 저장과 재읽기 성공 후 `migration.json`에 원본 버전과 결과를 기록한다.
6. 이관 실패 시 원본을 유지하고 어떤 문서와 필드가 실패했는지 표시한다.

이관은 여러 번 실행해도 중복 노드·관계를 만들지 않아야 한다. 원본 v2 문서 제거는 포트폴리오 작업 범위에 포함하지 않는다.

## 구현 경계

- Core: 노드·관계·작업·검증 규칙과 저장소. Unity UI와 MCP 타입에 의존하지 않는다.
- Unity Adapter: AssetDatabase GUID/경로, 컴파일·테스트 실행, Editor 메인 스레드 처리.
- UI Toolkit: 그래프와 상세 패널. Core 서비스를 통해서만 상태를 바꾼다.
- MCP Adapter: 기존 Unity-MCP에 얇은 Tool로 노출하고 Core 서비스를 호출한다.

UI와 MCP가 JSON 파일을 각각 직접 수정하지 않는다. 같은 검증·저장 서비스를 사용한다.

## A1 저장소 구현 규칙

노드 파일명은 전체 ID의 UTF-8 SHA256 소문자 hex를 사용한다. 저장소는 IBrainJson에 의존하며 UnityBrainJson이 직렬화를 담당한다. schemaVersion=1, 상태는 unreviewed/missing/recorded만 허용한다. Evidence/Activity 노드는 불변이며 정정은 새 ID로 추가한다. 관계 ID와 from/type/to 조합은 각각 유일해야 한다. 참조 대상이 없거나 손상되면 저장·조회가 실패하며 기존 파일은 유지한다. A1 저장소는 단일 Editor 작성자를 전제로 한다.

## A1 이관 구현 및 재실행

BrainMigration.Run은 기존 DocumentStore로 원본 전체를 검증하고 Code/Document/Image 노드와 관계를 생성한다. Document body에 역할·설계 의도·주의사항·본문·원본 코드 해시를 보존하되 기존 해시는 현재 검증으로 인정하지 않는다. AssetDatabase GUID 해석은 주입된 함수로 처리하며 없는 자산은 missing으로 표시한다. 기존 관련 코드 GUID는 source=v2-migration인 depends_on 관계로 저장한다. 종류 편집 UI는 아직 없다.

migration.json에는 schemaVersion=1, completedUtc, 원본 GUID/버전/SHA256, 생성 노드 ID 및 관계 ID를 기록한다. 목적지 충돌은 쓰기 전에 거절하고, 개별 파일 원자 저장과 재읽기 후에만 완료 기록을 쓴다. 다중 파일 트랜잭션은 아니며 중단 시 정확히 일치하는 부분 결과를 재사용한다. 완료 후 재실행은 원본 해시와 결과 존재를 검사하고 목적지 편집을 보존한다. 원본 수정은 명시적 재조정이 필요하며 자동 덮어쓰지 않는다.

Feature ID는 feature:<domain>/<slug>, Evidence/Activity ID는 종류 접두사와 UUID를 사용한다. A1은 노드 종류·ID·참조의 구조적 유효성을 검사하며 완료 조건과 증거의 진실성은 후속 W/V 단계에서 검증한다.
